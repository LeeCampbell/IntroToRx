---
title: Scheduling and threading
---

# Scheduling and Threading

Rx is primarily a system for working with _data in motion_ asynchronously. To effectively provide the level of asynchrony that developers require, some level of concurrency control is required. If we are dealing with multiple information sources, they may well generate data concurrently. We may want some degree of parallelism when processing data to achieve our scalability targets, but we will need control over this.

So far, we have managed to avoid any explicit usage of threading or concurrency. There are some methods that we have had to deal with timing to perform their jobs. (For example, `Buffer`, `Delay`, `Sample` must arrange for work to happen on a particular schedule.) Most of this however, has been kindly abstracted away from us. This chapter will look the Rx's scheduling system which offers an elegant system for managing these concerns.

## Rx, Threads and Concurrency

Rx does not impose constraints on which threads we use. An `IObservable<T>` is free to invoke its subscribers' `OnNext/Completed/Error` methods on any thread, perhaps a different thread for each call. Despite this free-for-all, there is one aspect of Rx that prevents chaos: observable sources must obey the [Fundamental Rules of Rx Sequences](02_KeyTypes.md#the-fundamental-rules-of-rx-sequences) under all circumstances.

When we first explored these rules, we focused on how they determine the ordering of calls into any single observer—there can be any number of calls to `OnNext`, but once either `OnError` or `OnCompleted` have been invoked, there must be no further calls. But now that we're looking at concurrency, a different aspect of these rules becomes more important: for any single subscription, an observable source must not make concurrent calls into that subscription's observer. So if a source calls `OnNext`, it must wait until that call returns before either calling `OnNext` again, or calling `OnError` or `OnComplete`.

The upshot for observers is that as long as your observer is involved in just one subscription, it will only ever be asked to deal with one thing at a time. If doesn't matter if the source to which it is subscribed is a long and complex processing chain involving many different operators. Even if you build that source by combining multiple inputs (e.g., using [`Merge`](09_CombiningSequences.md#merge)), the fundamental rules require that if you called `Subscribe` just once on a single `IObservable<T>`, that source is never allowed to make multiple concurrent calls into your `IObserver<T>` methods.

So although each call might come in on a different thread, the calls are strictly sequential (unless a single observer is involved in multiple subscriptions).

Rx operators that receive incoming notifications as well as producing them will notify their observers on whatever thread the incoming notification happened to arrive on. Suppose you have a sequence of operators like this:

```cs
source
    .Where(x => x.MessageType == 3)
    .Buffer(10)
    .Take(20)
    .Subscribe(x => Console.WriteLine(x));
```

When that call to `Subscribe` happens, we end up with a chain of observers—the Rx-supplied observer that will invoke our callback was passed to the observable returned by `Take`, which will in turn create an observer that subscribed to the observable returned by `Buffer`, which will in turn create an observer subscribed to the `Where` observable, which will have created yet another observer which is subscribed to `source`.

So when `source` decides to produce an item, it will invoke the `Where` operator's observer's `OnNext`. That will invoke the predicate, and if the `MessageType` is indeed 3, the `Where` observer will call `OnNext` on the `Buffer`'s observer, and it will do this on the same thread. The `Where` observer's `OnNext` isn't going to return until the `Buffer` observer's `OnNext` returns. Now if the `Buffer` observer determines that it has completely filled a buffer (e.g., it just received its 10th item), then it is also not going to return yet—it's going to invoke the `Take` observer's `OnNext`, and as long as `Take` hasn't already received 20 items, it's going to call `OnNext` on the Rx-supplied observer that will invoke our callback.

So for the source notifications that make it all the way through to that `Console.WriteLine` in the callback passed to subscribe, we end up with a lot of nested calls on the stack:

* `source` calls:
    * `Where` observer, which calls:
        * `Buffer` observer, which calls:
            * `Take` observer, which calls:
                * `Subscribe` observer, which calls our lambda

This is all happening on one thread. Most Rx operators don't have any one particular thread that they call home. They just do their work on whatever thread the call comes in on. This makes Rx pretty efficient—passing data from one operator to the next merely involves a method call, and those are pretty fast. (In fact, there are typically a few more layers. Rx tends to add a few wrappers to handle errors and early unsubscription. So the call stack will look a bit more complex than what I've just shown. But it's still typically all just method calls.)

You will sometimes hear Rx described as having a _free threaded_ model. All that means is that operators don't generally care what thread they use. As we will see, there are exceptions, but this direct calling by one operator of the next is the norm.

An upshot of this is that it's typically the original source that determine which thread is used. This next example verifies by creating a subject, then calling `OnNext` on various threads and reporting the thread id.

```cs
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
var subject = new Subject<string>();

subject.Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

object sync = new();
ParameterizedThreadStart notify = arg =>
{
    string message = arg?.ToString() ?? "null";
    Console.WriteLine(
        $"OnNext({message}) on thread: {Environment.CurrentManagedThreadId}");
    lock (sync)
    {
        subject.OnNext(message);
    }
};

notify("Main");
new Thread(notify).Start("First worker thread");
new Thread(notify).Start("Second worker thread");
```

Output:

```
Main thread: 1
OnNext(Main) on thread: 1
Received Main on thread: 1
OnNext(First worker thread) on thread: 10
Received First worker thread on thread: 10
OnNext(Second worker thread) on thread: 11
Received Second worker thread on thread: 11
```

Note that the handler passed to `Subscribe` was called back on the same thread that made the call to `OnNext`. This is straightforward and efficient. However, things are not always this simple.

## Timed invocation

Some notifications will not be the immediate result of a source providing an item. For example, Rx offers a [`Delay`] operator, which time shifts the delivery of items. This next example is based on the preceding one, with the main difference being that we no longer subscribe directly to the source. We go via `Delay`:

```cs
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");
var subject = new Subject<string>();

subject
    .Delay(TimeSpan.FromSeconds(0.25))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

object sync = new();
ParameterizedThreadStart notify = arg =>
{
    string message = arg?.ToString() ?? "null";
    Console.WriteLine(
        $"OnNext({message}) on thread: {Environment.CurrentManagedThreadId}");
    lock (sync)
    {
        subject.OnNext(message);
    }
};

notify("Main 1");
Thread.Sleep(TimeSpan.FromSeconds(0.1));
notify("Main 2");
Thread.Sleep(TimeSpan.FromSeconds(0.3));
notify("Main 3");
new Thread(notify).Start("First worker thread");
Thread.Sleep(TimeSpan.FromSeconds(0.1));
new Thread(notify).Start("Second worker thread");

Thread.Sleep(TimeSpan.FromSeconds(2));
```

This also waits for a while between sending source items, so we can see the effect of `Delay`. Here's the output:

```
Main thread: 1
OnNext(Main 1) on thread: 1
OnNext(Main 2) on thread: 1
Received Main 1 on thread: 12
Received Main 2 on thread: 12
OnNext(Main 3) on thread: 1
OnNext(First worker thread) on thread: 13
OnNext(Second worker thread) on thread: 14
Received Main 3 on thread: 12
Received First worker thread on thread: 12
Received Second worker thread on thread: 12
```

Notice that in this case every `Received` message is on thread id 12, which is different from any of the three threads on which the notifications were raised.

This shouldn't be entirely surprising. The only way Rx could have used the original thread here would be for `Delay` to block the thread for the specified time (a quarter of a second here) before forwarding the call. This would be unacceptable for most scenarios, so instead, the `Delay` operator arranges for a callback to occur after a suitable delay. As you can see from the output, these all seems to happen on one particular thread. No matter which thread calls `OnNext`, the delayed notification arrives on thread id 12. But this is not a thread created by the `Delay` operator. This is happening because `Delay` is using a _scheduler_.


## Schedulers

Schedulers do three things:

* determining the context in which to execute work (e.g., a certain thread)
* deciding when to execute work (e.g., immediately, or deferred)
* keeping track of time

Here's a simple example to explore the first two of those:

```cs
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");

Observable
    .Range(1, 5)
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));

Console.WriteLine("Subscribe returned");
Console.ReadLine();
```

It might not be obvious that this has anything to do with scheduling, but in fact, `Range` always uses a scheduler to do its work. We've just let it use its default scheduler. Here's the output:

```
Main thread: 1
Received 1 on thread: 1
Received 2 on thread: 1
Received 3 on thread: 1
Received 4 on thread: 1
Received 5 on thread: 1
Subscribe returned
```

Looking at the first two items in our list of what schedulers do, we can see that the context in which this has executed the work is the thread on which I called `Suscribe`. And as for when it has decided to execute the work, it has decided to do it all before `Subscribe` returns. So you might think that `Range` immediately produces all of the items we've asked for and then returns. However, it's not quite as simple as that. Let's look at what happens if we have multiple `Range` instances running simultaneously. This introduces an extra operator—a `SelectMany` that calls `Range` again:

```cs
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

The output shows that `Range` doesn't in fact necessarily produce all of its items immediately:

```
Received 10 on thread: 1
Received 11 on thread: 1
Received 20 on thread: 1
Received 12 on thread: 1
Received 21 on thread: 1
Received 30 on thread: 1
Received 13 on thread: 1
Received 22 on thread: 1
Received 31 on thread: 1
Received 40 on thread: 1
Received 14 on thread: 1
Received 23 on thread: 1
Received 32 on thread: 1
Received 41 on thread: 1
Received 50 on thread: 1
Received 24 on thread: 1
Received 33 on thread: 1
Received 42 on thread: 1
Received 51 on thread: 1
Received 34 on thread: 1
Received 43 on thread: 1
Received 52 on thread: 1
Received 44 on thread: 1
Received 53 on thread: 1
Received 54 on thread: 1
Subscribe returned
```

The first nested `Range` produces by the `SelectMany` callback produces a couple of values (10 and 11) but then the second one manages to get its first value out (20) before the first one produces its third (12). You can see there's some interleaving of progress here. So although the context in which work is executed continues to be the thread on which we invoked `Subscribe`, the second choice the scheduler has to make—when to execute the work—is more subtle than it first seems. This tells us that `Range` is not as simple as this naive implementation:

```cs
public static IObservable<int> NaiveRange(int start, int count)
{
    return System.Reactive.Linq.Observable.Create<int>(obs =>
    {
        for (int i = 0; i < count; i++)
        {
            obs.OnNext(start + i);
        }

        return Disposable.Empty;
    });
}
```

If `Range`` worked like that, this code would produce all of the items from the first range returned by the `SelectMany` callback before moving on to the next. In fact, Rx does provide a scheduler that would give us that behaviour if that's what we want. This example passes `ImmediateScheduler.Instance` to the nested `Observable.Range` call:

```cs
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5, ImmediateScheduler.Instance))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

Here's the outcome:

```
Received 10 on thread: 1
Received 11 on thread: 1
Received 12 on thread: 1
Received 13 on thread: 1
Received 14 on thread: 1
Received 20 on thread: 1
Received 21 on thread: 1
Received 22 on thread: 1
Received 23 on thread: 1
Received 24 on thread: 1
Received 30 on thread: 1
Received 31 on thread: 1
Received 32 on thread: 1
Received 33 on thread: 1
Received 34 on thread: 1
Received 40 on thread: 1
Received 41 on thread: 1
Received 42 on thread: 1
Received 43 on thread: 1
Received 44 on thread: 1
Received 50 on thread: 1
Received 51 on thread: 1
Received 52 on thread: 1
Received 53 on thread: 1
Received 54 on thread: 1
Subscribe returned
```

By specifying `ImmediateScheduler.Instance` we've asked for a particular policy: this invokes all work on the caller's thread, and it always does so immediately, avoiding introducing any concurrency. There are a couple of reasons this is not `Range`'s default. (Its default is `Scheduler.CurrentThread`, which always returns an instance of `CurrentThreadScheduler`.) First, `ImmediateScheduler.Instance` can end up causing fairly deep call stacks—most of the other schedulers maintain work queues, so if one operator decides it has new work to do while another is in the middle of doing something (e.g., a nested `Range` operator decides to start emitting its values), instead of starting that work immediately (which will involve invoking the method that will do the work) that work can be put on a queue instead, enabling the work already in progress to finish before starting on the next thing. Using the immediate scheduler everywhere can cause stack overflows when queries become complex. The second reason `Range` does not use the immediate scheduler is so that when multiple observables are all active at once, they can all make some progress—`Range` produces all of its items as quickly as it can, so it could end up starving other operators of CPU time if it didn't use a scheduler that enabled operators to take it in turns.

Notice that the `Subscribe returned` message appears last. So although the `CurrentThreadScheduler` isn't quite as eager as the immediate scheduler, it still won't return to its called until it has completed all outstanding work. It maintains a work queue, enabling slightly more fairness, and avoiding stack overflows, but as soon as anything asks the `CurrentThreadScheduler` to do something, it won't return until it has drained its queue.

Not all schedulers have this characteristic. Here's a variation on the earlier example in which we have just a single call to `Range`, without any nested observables. This time I'm asking it to use the `TaskPoolScheduler`.

```cs
Observable
    .Range(1, 5, TaskPoolScheduler.Default)
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

This makes a different decision about the context in which to run work from the immediate and current thread schedulers, as we can see from its output:

```cs
Main thread: 1
Subscribe returned
Received 1 on thread: 12
Received 2 on thread: 12
Received 3 on thread: 12
Received 4 on thread: 12
Received 5 on thread: 12
```

Notice that the notifications all happened on a different thread (with id 12) than the thread on which we invoked `Subscribe` (id 1). That's because the `TaskPoolScheduler`'s defining feature is that it invokes all work through the Task Parallel Library's (TPL) task pool. That's why we see a different thread id: the task pool doesn't own our application's main thread. In this case, it hasn't seen any need to spin up multiple threads. That's reasonable, there's just a single source here providing item one at a time. It's good that we didn't get more threads in this case—the thread pool is at its most efficient when a single thread processes work items sequentially, because it avoids context switching overheads, and since there's no actual scope for concurrent work here, we would gain nothing if it had created multiple threads in this case.

There's one other very significant difference with this scheduler: notice that the call to `Subscribe` returned before _any_ of the notifications were made it through to our observer. That's because this is the first scheduler we've looked at that will introduce real parallelism. The `ImmediateScheduler` and `CurrentThreadScheduler` will never spin up new threads by themselves, no matter how much the operators executing might want to perform concurrent operations. And although the `TaskPoolScheduler` determined that there's no need for it to create multiple threads, the one thread it did create is a different thread from the application's main thread, meaning that the main thread can continue to run in parallel with this subscription. Since `TaskPoolScheduler` isn't going to do any work on the thread that initiated the work, it can return as soon as it has queued the work up, enabling the `Subscribe` method to return immediately.

What if we use the `TaskPoolScheduler` in the example with nested observables? This uses it just on the inner call to `Range`, so the outer one will still use the default `CurrentThreadScheduler`:

```cs
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5, TaskPoolScheduler.Default))
    .Subscribe(
    m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

Now we can see a few more threads getting involved:

```
Received 10 on thread: 13
Received 11 on thread: 13
Received 12 on thread: 13
Received 13 on thread: 13
Received 40 on thread: 16
Received 41 on thread: 16
Received 42 on thread: 16
Received 43 on thread: 16
Received 44 on thread: 16
Received 50 on thread: 17
Received 51 on thread: 17
Received 52 on thread: 17
Received 53 on thread: 17
Received 54 on thread: 17
Subscribe returned
Received 14 on thread: 13
Received 20 on thread: 14
Received 21 on thread: 14
Received 22 on thread: 14
Received 23 on thread: 14
Received 24 on thread: 14
Received 30 on thread: 15
Received 31 on thread: 15
Received 32 on thread: 15
Received 33 on thread: 15
Received 34 on thread: 15
```

Since we have only a single observer in this example, the rules of Rx require it to be given items one at a time, so in practice there wasn't really any scope for parallelism here, but the more complex structure would have resulted in more work items initially going into the scheduler's queue than in the preceding example, which is probably why the work got picked up by more than one thread this time. In practice most of these threads would have spent most of their time blocked in the code inside `SelectMany` that ensures that it delivers one item at a time to its target observer. It's perhaps a little surprising that the items are not more scrambled. The subranges themselves seem to have emerged in a random order, but it has almost produced the items sequentially within each subrange (with item 14 being the one exception to that). This is a quirk relating to the way in which `Range` interacts with the `TaskPoolScheduler`.

I've not yet talked about the scheduler's third job: keeping track of time. This doesn't arise with `Range` because it attempts to produce all of its items as quickly as it can. But for the `Delay` operator I showed in the [Timed Invocation](#timed-invocation) section, timing is obviously a critical element. In fact this would be a good point to show the API that schedulers offer:

```cs
public interface IScheduler
{
    DateTimeOffset Now { get; }
    IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action);
    IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action);
    IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action);
}
```

You can see that all but one of these is concerned with timing. Only the first `Schedule` overload is not—operators call this when they want to schedule work to run as soon as the scheduler will allow. That's the overload used by `Range`. (Strictly speaking, `Range` interrogates the scheduler to find out whether it supports long-running operations, in which an operator can temporary control of a thread for an extended period. It prefers to use that when it can because it tends to be more efficient than submitting work to the scheduler for every single item it wishes to produce. The `TaskPoolScheduler` does support long running operations, which explains the slightly surprising output we saw earlier, but the `CurrentThreadScheduler`, `Range`'s default choice, does not. So by default, `Range` will invoke that first `Schedule` overload once for each item it wishes to produce.)

`Delay` uses the second overload. The exact implementation is quite complex (mainly because of how it catches up efficiently when a busy source causes it to fall behind) but in essence, each time a new item arrives into the `Delay` operator, it schedules a work item to run after the configured delay, so that it can supply that item to its subscriber with the expected time shift.

Schedulers have to be responsible for managing time, because .NET has several different timer mechanisms, and the choice of timer is often determined by the context in which you want to handle a timer callback. Since schedulers determine the context in which work runs, that means them must also choose the timer type. For example, UI frameworks typically provide timers that invoke their callbacks in a context suitable for making updates to the user interface. Rx provides some UI-framework-specific schedulers that use these timers, but these would be inappropriate choices for other scenarios. So each scheduler uses a timer suitable for the context in which it is going to run work items.

There's a useful upshot of this: because `IScheduler` provides an abstraction for timing-related details, it is possible to virtualize time. This is very useful for testing. If you look at the extensive test suite in the [Rx repository](https://github.com/dotnet/reactive) you will find that there are many tests that verify timing-related behaviour. If these ran in real-time, the test suite would take far too long to run, and would also be likely to produce the odd spurious failure, because background tasks running on the same machine as the tests will occasionally change the speed of execution in a way that might confuse the test. Instead, these tests use a specialized scheduler that provides complete control over the passage of time.

Notice that all three `IScheduler.Schedule` methods require a callback. A scheduler will invoke this at the time and in the context that it chooses. A scheduler callback takes another `IScheduler` as its first argument. This enables 

Rx supplies several schedulers. The following sections describe the most widely used ones.

### ImmediateScheduler

`ImmediateScheduler` is the simplest scheduler Rx offers. As you saw in the preceding sections, whenever it is asked to schedule some work, it just runs it immediately. It does this inside its `IScheduler.Schedule` method.

This is a very simple strategy, and it makes `ImmediateScheduler` very efficient. For this reason, many operators default to using `ImmediateScheduler`. However, it can be problematic with operators that instantly produce multiple items, especially when the number of items might be large. For example, Rx defines the [`ToObservable` extension method for `IEnumerable<T>`](03_CreatingObservableSequences.md#from-ienumerablet). When you subscribe to an `IObservable<T>` returned by this, it will start iterating over the collection immediately, and if you were to tell it to use the `ImmediateScheduler`, `Subscribe` would not return until it reached the end of the collection. That would obviously be a problem for an infinite sequence, and it's why operators of this kind do not use `ImmediateScheduler` by default.

The `ImmediateScheduler` also has potentially surprising behaviour when you invoke the `Schedule` overload that takes a `TimeSpan`. This asks the scheduler to run some work after the specified length of time. The way it achieves this is to call `Thread.Sleep`. With most of Rx's schedulers, this overload will arrange for some sort of timer mechanism to run the code later, enabling the current thread to get on with its business, but `ImmediateScheduler` is true to its name here, in that it refuses to engage in such deferred execution. It just blocks the current thread until it is time to do the work. This means that time-based observables like those returned by `Interval` would work if you specified this scheduler, but at the cost of preventing the thread from doing anything else.

The `Schedule` overload that takes a `DateTime` is slightly different. If you specify a time less than 10 seconds into the future, it will block the calling thread like it does when you use `TimeSpan`. But if you pass a `DateTime` that is further into the future, it gives up on immediate execution, and falls back to using a timer.

### CurrentThreadScheduler

The `CurrentThreadScheduler` is very similar to the `ImmediateScheduler`. The difference is how it handles sources that logically produce multiple such as the [`ToObservable` extension method for `IEnumerable<T>`](03_CreatingObservableSequences.md#from-ienumerablet), or [`Observable.Range`](03_CreatingObservableSequences.md#observablerange)

These kinds of operators do not use normal `for` or `foreach` loops. They typically schedule a new work item for each iteration. Whereas the `ImmediateScheduler` will run such work immediately, the `CurrentThreadScheduler` checks to see if it is already processing a work item. That happens with this example from earlier:

```cs
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Range(i * 10, 5))
    .Subscribe(
        m => Console.WriteLine($"Received {m} on thread: {Environment.CurrentManagedThreadId}"));
```

Let's follow exactly what happens here. First, assume that this code is just running normally—perhaps inside the `Main` entry point of a program. When this code calls `Subscribe` on the `IObservable<int>` returned by `SelectMany`, that will in turn will call `Subscribe` on the `IObservable<int>` returned by `Observable.Range`, which will in turn schedule a work item for the generation of the first value in the range (`1`).

The `Range` operator uses the `CurrentThreadScheduler` by default, and that will ask itself "Am I already in the middle of handling some work item on this thread?" In this case the answer will be no, so it will run the work item immediately (before returning from the `Schedule` call made by the `Range` operator). The `Range` operator will then produce its first value, calling `OnNext` on the `IObserver<int>` that the `SelectMany` operator provided when it subscribed to the range.

The `SelectMany` operator's `OnNext` method will now invoke its lambda, passing in the argument supplied (the value `1` from the `Range` operator). You can see from the example above that this lambda calls `Observable.Range` again, returning a new `IObservable<int>`. `SelectMany` will immediately subscribe to this (before returning from its `OnNext`). This is the second time this code has ended up calling `Subscribe` on an `IObservable<int>` returned by a `Range` (but it's a different instance than the last time), and `Range` will once again default to using the `CurrentThreadScheduler`, and will once again schedule a work item to perform the first iteration.

So once again,the `CurrentThreadScheduler` will ask itself "Am I already in the middle of handling some work item on this thread?" But this time, the answer will be yes. And this is where the behaviour is different than `ImmediateScheduler`. The `CurrentThreadScheduler` maintains a queue of work for each thread that it gets used on, and in this case it just adds the newly scheduled work to the queue, and returns back to the `SelectMany` operators `OnNext`.

`SelectMany` has now completed its handling of this item (the value `1`) from the first `Range`, so its `OnNext` returns. At this point, this `Range` operator schedules another work item. Again, the `CurrentThreadScheduler` will detect that it is currently running a work item, so it just adds this to the queue.

Having scheduled the work item that is going to generate its second value (`2`), the `Range` operator returns. Remember, the code in the `Range` operator that was running at this point was the callback for the first scheduled work item, so it's returning to the `CurrentThreadScheduler`—we are back inside its `Schedule` method.

At this point, the `CurrentThreadScheduler` does not return from `Schedule` because it checks its work queue, and will see that there are now two items in the queue. (There's the work item that the nested `Range` observable scheduled to generate its first value, and there's also the work item that the top-level `Range` observable just scheduled to generate its second value.) The `CurrentThreadScheduler` will now execute the first of these—the nested `Range` operator now gets to generate its first value (which will be `10`), so it calls `OnNext` on the observer supplied thanks to the top-level call to `Subscribe` in the example. And that observer will just call the lambda we passed to `Subscribe`, causing our `Console.WriteLine` to run. After that returns, the nested `Range` operator will schedule another work item to generate its second item. Again, the `CurrentThreadScheduler` will realise that it's already in the middle of handling a work item on this thread, so it just puts it in the queue and then returns immediately from `Schedule`. The nested `Range` operator is now done for this iteration so it returns back to the scheduler. The scheduler will now pick up the next item in the queue, which in this case it the work item added by the top-level `Range` to produce the second item.

And so it continues. This queuing of work items when work is already in progress is what enables multiple observable sources to make progress in parallel.

By contrast, the `ImmediateScheduler` runs new work items immediately, which is why we don't see this parallel progress.

(To be strictly accurate, there are certain scenarios in which `ImmediateScheduler` can't run work immediately. In these iterative scenarios, it actually supplies a slightly different scheduler that the operators use to schedule all work after the first item, and this checks whether it's being asked to process multiple work items simultaneously. If it is, it falls back to a queuing strategy similar to `CurrentThreadScheduler`, except it's a queue local to the initial work item, instead of a per-thread queue. This prevents problems either due to multithreading, and it also avoids stack overflows that would otherwise occur when an iterative operator schedules a new work item inside the handler for the current work item. Since the queue is not shared across all work in the thread, this still has the effect of ensuring that any nested work queued up by a work item completes before the call to `Schedule` returns. So even when this queueing kicks in, we typically don't see interleaving of work from separate source like we do with `CurrentThreadScheduler`.)

### DefaultScheduler

The `DefaultScheduler` is intended for work that may need to be spread out over time, or where you are likely to want concurrent execution. These features mean that this can't guarantee to run work on any particular thread, and in practice it schedulers work via the CLR's thread pool. This is the default scheduler for all of Rx's time-based operators, and also for the `Observable.ToAsync` operator that can wrap a .NET method as an `IObservable<T>`.

Although this scheduler is useful if you would prefer work not to happen on your current thread—perhaps you're writing an application with a user interface and your code is running on the thread responsible for updating the UI and responding to user input—the fact that it can end up running work on any thread may make like complicated. What if you want all the work to happen on one thread, just not the thread you're on now? There's another scheduler for that.

### EventLoopScheduler

The `EventLoopScheduler` provides one-at-a-time scheduling, queuing up newly scheduled work items. This is similar to how the `CurrentThreadScheduler` operates if you use it from just one thread. The difference is that `EventLoopScheduler` creates a dedicated thread for this work instead of using whatever thread you happen to schedule the work from.

### HistoricalScheduler

The `HistoricalScheduler`. Should we do this one?

Many Rx operators provide overloads that accepts an `IScheduler`

### NewThreadScheduler

The `NewThreadScheduler` creates a new thread to execute every work item it is given. This is unlikely to make sense in most scenarios. However, it might be useful in cases where you want to execute some long running work, and represent its completion through an `IObservable<T>`. The `Observable.ToAsync` does exactly this, and will normally use the `DefaultScheduler`, meaning it will run the work on a thread pool thread. But it the work is likely to take more than second or two, the thread pool may not be a good choice, because it is optimized for short execution times, and its heuristics for managing the size of the thread pool are not designed with long-running operations in mind. The `NewThreadScheduler` may be a better choice in this case.

### SynchronizationContextScheduler

This invokes all work through a [`SynchronizationContext`](https://learn.microsoft.com/en-us/dotnet/api/system.threading.synchronizationcontext). This is useful in user interface scenarios—most .NET client-side user interface frameworks make a `SynchronizationContext` available that can be used to invoke callbacks in a context suitable for making changes to the UI. (Typically this involves invoking them on the correct thread, but individual implementations can decide what constitutes the appropriate context.)

### TaskPoolScheduler

Invokes all work through the TPL task pool. The TPL task pool is newer than the CLR thread pool. Rx's DefaultScheduler uses the older CLR thread pool for backwards compatibility reasons, but the TPL task pool can offer performance benefits in some scenarios.

### ThreadPoolScheduler

Invokes all work through the CLR thread pool.

TODO: it's not clear when you'd use this, not the DefaultScheduler.

### UI Framework Schedulers: ControlScheduler, DispatcherScheduler and CoreDispatcherScheduler

Although the `SynchronizationContextScheduler` will work for all widely used client-side UI frameworks in .NET, Rx offers more specialized schedulers. `ControlScheduler` is for Windows Forms applications, `DispatcherScheduler` for WPF, and `CoreDispatcherScheduler` for UWP.

These more specialized types offer two benefits. First, you don't necessarily have to be on the target UI thread to obtain an instance of these schedulers. Whereas with `SynchronizationContextScheduler` the only way you can generally obtain the `SynchronizationContext` this requires is by retrieving `SynchronizationContext.Current` while running on the UI thread. But these other UI-framework-specific schedulers can be passed a suitable `Control`, `Dispatcher` or `CoreDispatcher`, which it's possible to obtain from a non-UI thread. Second, `DispatcherScheduler` and `CoreDispatcherScheduler` provide a way to use the prioritisation mechanism supported by the `Dispatcher` and `CoreDispatcher` types.



## SubscribeOn and ObserveOn

So far, I've talked about why some Rx sources need access to schedulers—this is necessary for timing-related behaviour, and also for sources that produce items as quickly as possible. But remember, schedulers control three things:

* determining the context in which to execute work (e.g., a certain thread)
* deciding when to execute work (e.g., immediately, or deferred)
* keeping track of time

The discussion so far as mostly focused on the 2nd and 3rd features. When it comes to our own application code, we are most likely to use schedulers to control that first aspect. Rx defines two extension methods to `IObservable<T>` for this: `SubscribeOn` and `ObserveOn`. Both methods take an `IScheduler` and return an `IObservable<T>` so you can chain more operators downstream of these.

These methods do what their names suggest. If you use `SubscribeOn`, then when you call `Subscribe` on the resulting `IObservable<T>` it arranges to call the original `IObservable<T>`'s `Subscribe` method via the specified scheduler. Here's an example:

```cs
Console.WriteLine($"Main thread: {Environment.CurrentManagedThreadId}");

Observable
    .Interval(TimeSpan.FromSeconds(1))
    .SubscribeOn(new EventLoopScheduler((start) =>
    {
        Thread t = new(start) { IsBackground = false };
        Console.WriteLine($"Created thread for EventLoopScheduler: {t.ManagedThreadId}");
        return t;
    }))
    .Subscribe(
        tick => Console.WriteLine($"{DateTime.Now}-{Environment.CurrentManagedThreadId}: Tick {tick}"));

Console.WriteLine($"{DateTime.Now}-{Environment.CurrentManagedThreadId}: Main thread exiting");
```

This calls `Observable.Interval` (which uses `DefaultScheduler` by default), but instead of subscribing directly to this, it first takes the `IObservable<T>` returned by `Interval` and invokes `SubscribeOn`. I've used an `EventLoopScheduler`, and I've passed it a factory callback for the thread that it will use to ensure that it is a non-background thread. (By default `EventLoopScheduler` creates itself a background thread, meaning that the thread won't force the process to stay alive. Normally that's what you'd want but I'm changing that in this example to show what's happening.)

When I call `Subscribe` on the `IObservable<long>` returned by `SubscribeOn`, it calls `Schedule` on the `EventLoopScheduler` that I supplied, and in the callback for that work item, it then calls `Subscribe` on the original `Interval` source. So the effect is that the subscription to the underlying source doesn't happen on my main thread, it happens on the thread created for my `EventLoopScheduler`. Running the program produces this output:

```
Main thread: 1
Created thread for EventLoopScheduler: 12
21/07/2023 14:57:21-1: Main thread exiting
21/07/2023 14:57:22-6: Tick 0
21/07/2023 14:57:23-6: Tick 1
21/07/2023 14:57:24-6: Tick 2
...
```

Notice that my application's main thread exits before the source begins producing notifications. But also notice that the thread id for the newly created thread is 12, and yet my notifications are coming through on a different thread, with id 6! What's happening?

This often catches people out. The scheduler on which you subscribe to an observable source doesn't necessarily have any impact on how the source behaves once it is up and running. Remember earlier that I said `Observable.Interval` uses `DefaultScheduler` by default? Well we've not specified a scheduler for the `Interval` here, so it will be using that default. It doesn't care what context we invoke its `Subscribe` method from. So really, the only effect of introducing the `EventLoopScheduler` here has been to keep the process alive even after its main thread exits. That scheduler thread never actually gets used again after it makes its initial `Subscribe` call into the `IObservable<long>` returned by `Observable.Interval`. It just sits patiently waiting for further calls to `Schedule` that never come.

Not all sources are completely unaffected by the context in which their `Subscribe` is invoked, though. If I were to replace this line:

```cs
    .Interval(TimeSpan.FromSeconds(1))
```

with this:

```cs
    .Range(1, 5)
```

then we get this output:

```
Main thread: 1
Created thread for EventLoopScheduler: 12
21/07/2023 15:02:09-12: Tick 1
21/07/2023 15:02:09-1: Main thread exiting
21/07/2023 15:02:09-12: Tick 2
21/07/2023 15:02:09-12: Tick 3
21/07/2023 15:02:09-12: Tick 4
21/07/2023 15:02:09-12: Tick 5
```

Now all the notifications are coming in on thread 12, the thread created for the `EventLoopScheduler`. Note that even here, `Range` isn't using that scheduler. The difference is that `Range` defaults to `CurrentThreadScheduler`, so it will generate its outputs from whatever thread you happen to call it from. So even though it's not actually using the `EventLoopScheduler`, it does end up using that scheduler's thread, because we used that scheduler to subscribe to the `Range`.

So this illustrates that `SubscribeOn` is doing what it promises: it does determine the context from which `Subscribe` is invoked. It's just that it doesn't always matter what context that is. If `Subscribe` does non-trivial work, it can matter. For example, if you use [`Observable.Create`](03_CreatingObservableSequences.md#observablecreate) to create a custom sequence, `SubscribeOn` determines the context in which the callback you passed to `Create` is invoked. But Rx doesn't have a concept of a 'current' scheduler—there's no way to ask "which scheduler was I invoked from?"—so Rx operators don't just their scheduler from the context on which they were subscribed.

When it comes to emitting items, most of the sources Rx supplies fall into one of three categories. First, operators that produce outputs in response to inputs from an upstream source (e.g., `Where`, `Select`, or `GroupBy) generally call their observers methods from inside their own `OnNext`. So whatever context their source observable was running in when it called `OnNext`, that's the context the operator will use when calling its observer. Second, operators that produce items either iteratively, or based on timing will use a scheduler (either explicitly supplied, or a default type when none is specified). Third, some sources just produce items from whatever context they like. For example, if an `async` method uses `await` and specifies `ConfigureAwait(false)` then it could be on more or less any thread and in any context after the `await` completes, and it might then go on to invoke `OnNext` on an observer.

As long as a source follows [the fundamental rules of Rx sequences](02_KeyTypes.md#the-fundamental-rules-of-rx-sequences), it's allowed to invoke its observer's methods from any context it likes. It can choose to accept a scheduler as input and to use that, but it's under no obligation to. And if you have such an unruly source that you'd like to tame, that's where the `ObserveOn` extension method comes in. Consider the following rather daft example:

```cs
Observable
    .Interval(TimeSpan.FromSeconds(1))
    .SelectMany(tick => Observable.Return(tick, NewThreadScheduler.Default))
    .Subscribe(
        tick => Console.WriteLine($"{DateTime.Now}-{Environment.CurrentManagedThreadId}: Tick {tick}"));
```

This deliberately causes every notification to arrive on a different thread, as this output shows:

```
Main thread: 1
21/07/2023 15:19:56-12: Tick 0
21/07/2023 15:19:57-13: Tick 1
21/07/2023 15:19:58-14: Tick 2
21/07/2023 15:19:59-15: Tick 3
...
```

(It's achieving this by calling `Observable.Return` for every single tick that emerges from `Interval`, and telling `Return` to use the `NewThreadScheduler`. Each such call to `Return` will create a new thread. This is a terrible idea, but it is an easy way to get a source that calls from a different context every time.) If I want to impose some order, I can add a call to `ObserveOn`:

```cs
Observable
    .Interval(TimeSpan.FromSeconds(1))
    .SelectMany(tick => Observable.Return(tick, NewThreadScheduler.Default))
    .ObserveOn(new EventLoopScheduler())
    .Subscribe(
        tick => Console.WriteLine($"{DateTime.Now}-{Environment.CurrentManagedThreadId}: Tick {tick}"));
```

I've created an `EventLoopScheduler` here because it creates a single thread, and runs every scheduled work item on that thread. The output now shows the same thread id (13) every time:

```
Main thread: 1
21/07/2023 15:24:23-13: Tick 0
21/07/2023 15:24:24-13: Tick 1
21/07/2023 15:24:25-13: Tick 2
21/07/2023 15:24:26-13: Tick 3
...
```

So although each new observable created by `Observable.Return` creates a brand new thread, `ObserveOn` ensures that my observer's `OnNext` (and `OnCompleted` or `OnError` in cases where those are called) is invoked via the specified scheduler.

### SubscribeOn and ObserveOn in UI applications

If you're using Rx in a user interface, `ObserveOn` is useful when you are dealing with information sources that don't provide notifications on the UI thread. You can wrap any `IObservable<T>` with `ObserveOn`, passing a `SynchronizationContextScheduler` (or a framework-specific type such as `DispatcherScheduler`), to ensure that your observer receives notifications on the UI thread, making it safe to update the UI.

`SubscribeOn` can also be useful in user interfaces as a way to ensure that any initialization work that an observable source does to get started does not happen on the UI thread.

Most UI frameworks designate one particular thread for receiving notifications from the user and also for updating the UI, for any one window. It is critical to avoid blocking this UI thread, as doing so leads to a poor user experience—if you are doing work on the UI thread, it will be unavailable for responding to user input until that work is done. As a general rule, if you cause a user interface to become unresponsive for longer than 100ms, users will become irritated, so you should not be perform any work that will take longer than this on the UI thread. When Microsoft first introduced its application store (which came in with Windows 8) they specified an even more stringent limit—if your application blocked the UI thread for longer than 50ms, it might not be allowed into the store. With the processing power offered by modern processors, you can achieve a lot of processing 50ms—even on the relatively low-powered processors in mobile devices that's long enough to execute millions of instructions. However, anything involving I/O (reading or writing files, or waiting for a response from any kind of network service) should not be done on the UI thread. The general pattern for creating responsive UI applications is:

- respond to some sort of user action
- if slow work is required, do this on a background thread
- pass the result back to the UI thread
- update the UI

This is a great fit for Rx: responding to events, potentially composing multiple events, passing data to chained method calls. With the inclusion of scheduling, we even have the power to get off and back onto the UI thread for that responsive application feel that users demand.

Consider a WPF application that used Rx to populate an `ObservableCollection<T>`. You could use `SubscribeOn` to ensure that the main work was not done on the UI thread, followed by `ObserveOn` to ensure you were notified back on the correct thread. If you failed to use the `ObserveOn` method, then your `OnNext` handlers would be invoked on the same thread that raised the notification. In most UI frameworks, this would cause some sort of not-supported/cross-threading exception. In this example, we subscribe to a sequence of `Customers`. I'm using `Defer` so that if `GetCustomers` does any slow initial work before returning its `IObservable<Customer>`, that won't happen until we subscribe. We then use `SubscribeOn` to call that method and perform the subscription on a task pool thread. Then we ensure that as we receive `Customer` notifications, we add them to the `Customers` collection on the `Dispatcher`.

```csharp
Observable
    .Defer(() => _customerService.GetCustomers())
    .SubscribeOn(TaskPoolScheduler.Default)
    .ObserveOn(DispatcherScheduler.Instance) 
    .Subscribe(Customers.Add);
```

Rx also offers `SubscribeOnDispatcher()` and `ObserveOnDispatcher()` extension methods to `IObservable<T>`, that automatically use the current thread's `Dispatcher` (and equivalents for `CoreDispatcher`). While these might be slightly more convenient they can make it harder to test your code. We explain why in the [Testing Rx](16_TestingRx.html) chapter.



## Concurrency pitfalls

Introducing concurrency to your application will increase its complexity. If your application is not noticeably improved by adding a layer of concurrency, then you should avoid doing so. Concurrent applications can exhibit maintenance problems with symptoms surfacing in the areas of debugging, testing and refactoring.

The common problem that concurrency introduces is unpredictable timing. Unpredictable timing can be caused by variable load on a system, as well as variations in system configurations (e.g. varying core clock speed and availability of processors). These can ultimately can result in race conditions. Symptoms of race conditions include out-of-order execution, [deadlocks](http://en.wikipedia.org/wiki/Deadlock), [livelocks](http://en.wikipedia.org/wiki/Deadlock#Livelock) and corrupted state.

In my opinion, the biggest danger when introducing concurrency haphazardly to an application, is that you can silently introduce bugs. These defects may slip past Development, QA and UAT and only manifest themselves in Production environments. Rx, however, does such a good job of simplifying the concurrent processing of observable sequences that many of these concerns can be mitigated. You can still create problems, but if you follow the guidelines then you can feel a lot safer in the knowledge that you have heavily reduced the capacity for unwanted race conditions.

In a later chapter, [Testing Rx](16_TestingRx.html), we will look at how Rx improves your ability to test concurrent workflows.

### Lock-ups

When working on my first commercial application that used Rx, the team found out the hard way that Rx code can most certainly deadlock. When you consider that some calls (like `First`, `Last`, `Single` and `ForEach`) are blocking, and that we can schedule work to be done in the future, it becomes obvious that a race condition can occur. This example is the simplest block I could think of. Admittedly, it is fairly elementary but it will get the ball rolling.

```csharp
var sequence = new Subject<int>();

Console.WriteLine("Next line should lock the system.");

var value = sequence.First();
sequence.OnNext(1);

Console.WriteLine("I can never execute....");
```

Hopefully, we won't ever write such code though, and if we did, our tests would give us quick feedback that things went wrong. More realistically, race conditions often slip into the system at integration points. The next example may be a little harder to detect, but is only small step away from our first, unrealistic example. Here, we block in the constructor of a UI element which will always be created on the dispatcher. The blocking call is waiting for an event that can only be raised from the dispatcher, thus creating a deadlock.

```csharp
public Window1()
{
    InitializeComponent();
    DataContext = this;
    Value = "Default value";
    
    // Deadlock! We need the dispatcher to continue to allow me to click the button to produce a value
    Value = _subject.First();
    
    // This will give same result but will not be blocking (deadlocking). 
    _subject.Take(1).Subscribe(value => Value = value);
}

private void MyButton_Click(object sender, RoutedEventArgs e)
{
    _subject.OnNext("New Value");
}

public string Value
{
    get { return _value; }
    set
    {
        _value = value;
        var handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs("Value"));
    }
}
```

Next, we start seeing things that can become more sinister. The button's click handler will try to get the first value from an observable sequence exposed via an interface.

```csharp
public partial class Window1 : INotifyPropertyChanged
{
    //Imagine DI here.
    private readonly IMyService _service = new MyService(); 
    private int _value2;

    public Window1()
    {
        InitializeComponent();
        DataContext = this;
    }

    public int Value2
    {
        get { return _value2; }
        set
        {
            _value2 = value;
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs("Value2"));
        }
    }

    #region INotifyPropertyChanged Members
    public event PropertyChangedEventHandler PropertyChanged;
    #endregion

    private void MyButton2_Click(object sender, RoutedEventArgs e)
    {
        Value2 = _service.GetTemperature().First();
    }
}
```

There is only one small problem here in that we block on the `Dispatcher` thread (`First` is a blocking call), however this manifests itself into a deadlock if the service code is written incorrectly.

```csharp
class MyService : IMyService
{
    public IObservable<int> GetTemperature()
    {
        return Observable.Create<int>(
            o =>
            {
                o.OnNext(27);
                o.OnNext(26);
                o.OnNext(24);
                return () => { };
            })
            .SubscribeOnDispatcher();
    }
}
```

This odd implementation, with explicit scheduling, will cause the three `OnNext` calls to be scheduled once the `First()` call has finished; however, `that` is waiting for an `OnNext` to be called: we are deadlocked.

So far, this chapter may seem to say that concurrency is all doom and gloom by focusing on the problems you could face; this is not the intent though. 
We do not magically avoid classic concurrency problems simply by adopting Rx. 
Rx will however make it easier to get it right, provided you follow these two simple rules.

- Only the final subscriber should be setting the scheduling
- Avoid using blocking calls: e.g. `First`, `Last` and `Single`

The last example came unstuck with one simple problem; the service was dictating the scheduling paradigm when, really, it had no business doing so. Before we had a clear idea of where we should be doing the scheduling in my first Rx project, we had all sorts of layers adding 'helpful' scheduling code. What it ended up creating was a threading nightmare. When we removed all the scheduling code and then confined it it in a single layer (at least in the Silverlight client), most of our concurrency problems went away. I recommend you do the same. At least in WPF/Silverlight applications, the pattern should be simple: "Subscribe on a Background thread; Observe on the Dispatcher".

## Advanced features of schedulers

We have only looked at the most simple usage of schedulers so far:

- Scheduling an action to be executed as soon as possible
- Scheduling the subscription of an observable sequence
- Scheduling the observation of notifications coming from an observable sequence

Schedulers also provide more advanced features that can help you with various problems.

### Passing state

In the extension method to `IScheduler` we have looked at, you could only provide an `Action` to execute. This `Action` did not accept any parameters. If you want to pass state to the `Action`, you could use a closure to share the data like this:

```csharp
var myName = "Lee";
Scheduler.NewThread.Schedule(() => Console.WriteLine("myName = {0}", myName));
```

This could create a problem, as you are sharing state across two different scopes. I could modify the variable `myName` and get unexpected results.

In this example, we use a closure as above to pass state. I immediately modify the closure and this creates a race condition: will my modification happen before or after the state is used by the scheduler?

```csharp
var myName = "Lee";
scheduler.Schedule(() => Console.WriteLine("myName = {0}", myName));
myName = "John"; // What will get written to the console?
```

In my tests, "John" is generally written to the console when `scheduler` is an instance of `NewThreadScheduler`. If I use the `ImmediateScheduler` then "Lee" would be written. The problem with this is the non-deterministic nature of the code.

A preferable way to pass state is to use the `Schedule` overloads that accept state. This example takes advantage of this overload, giving us certainty about our state.

```csharp
var myName = "Lee";
scheduler.Schedule(myName, 
    (_, state) =>
    {
        Console.WriteLine(state);
        return Disposable.Empty;
    });
myName = "John";
```

Here, we pass `myName` as the state. We also pass a delegate that will take the state and return a disposable. The disposable is used for cancellation; we will look into that later. The delegate also takes an `IScheduler` parameter, which we name "_" (underscore). This is the convention to indicate we are ignoring the argument. When we pass `myName` as the state, a reference to the state is kept internally. So when we update the `myName` variable to "John", the reference to "Lee" is still maintained by the scheduler's internal workings.

Note that in our previous example, we modify the `myName` variable to point to a new instance of a string. If we were to instead have an instance that we actually modified, we could still get unpredictable behavior. In the next example, we now use a list for our state. After scheduling an action to print out the element count of the list, we modify that list.

```csharp
var list = new List<int>();
scheduler.Schedule(list,
    (innerScheduler, state) =>
    {
        Console.WriteLine(state.Count);
        return Disposable.Empty;
    });
list.Add(1);
```

Now that we are modifying shared state, we can get unpredictable results. In this example, we don't even know what type the scheduler is, so we cannot predict the race conditions we are creating. As with any concurrent software, you should avoid modifying shared state.

### Future scheduling

As you would expect with a type called "IScheduler", you are able to schedule an action to be executed in the future. You can do so by specifying the exact point in time an action should be invoked, or you can specify the period of time to wait until the action is invoked. This is clearly useful for features such as buffering, timers etc.

Scheduling in the future is thus made possible by two styles of overloads, one thattakes a `TimeSpan` and one that takes a `DateTimeOffset`. These are the two most simple overloads that execute an action in the future.

```csharp
public static IDisposable Schedule(
    this IScheduler scheduler, 
    TimeSpan dueTime, 
    Action action)
{...}

public static IDisposable Schedule(
    this IScheduler scheduler, 
    DateTimeOffset dueTime, 
    Action action)
{...}
```

You can use the `TimeSpan` overload like this:

```csharp
var delay = TimeSpan.FromSeconds(1);
Console.WriteLine("Before schedule at {0:o}", DateTime.Now);

scheduler.Schedule(delay, () => Console.WriteLine("Inside schedule at {0:o}", DateTime.Now));
Console.WriteLine("After schedule at  {0:o}", DateTime.Now);
```

Output:

```
Before schedule at 2012-01-01T12:00:00.000000+00:00
After schedule at 2012-01-01T12:00:00.058000+00:00
Inside schedule at 2012-01-01T12:00:01.044000+00:00
```

We can see therefore that scheduling is non-blocking as the 'before' and 'after' calls are very close together in time. You can also see that approximately one second after the action was scheduled, it was invoked.

You can specify a specific point in time to schedule the task with the `DateTimeOffset` overload. If, for some reason, the point in time you specify is in the past, then the action is scheduled as soon as possible.

### Cancellation

Each of the overloads to `Schedule` returns an `IDisposable`; this way, a consumer can cancel the scheduled work. In the previous example, we scheduled work to be invoked in one second. We could cancel that work by disposing of the cancellation token (i.e. the return value).

```csharp
var delay = TimeSpan.FromSeconds(1);
Console.WriteLine("Before schedule at {0:o}", DateTime.Now);

var token = scheduler.Schedule(delay, () => Console.WriteLine("Inside schedule at {0:o}", DateTime.Now));
Console.WriteLine("After schedule at  {0:o}", DateTime.Now);

token.Dispose();
```

Output:

```
Before schedule at 2012-01-01T12:00:00.000000+00:00
After schedule at 2012-01-01T12:00:00.058000+00:00
```

Note that the scheduled action never occurs, as we have cancelled it almost immediately.

When the user cancels the scheduled action method before the scheduler is able to invoke it, that action is just removed from the queue of work. This is what we see in example above. If you want to cancel scheduled work that is already running, then you can use one of the overloads to the `Schedule` method that takes a `Func<IDisposable>`. This gives a way for users to cancel out of a job that may already be running. This job could be some sort of I/O, heavy computations or perhaps usage of `Task` to perform some work.

Now this may create a problem; if you want to cancel work that has already been started, you need to dispose of an instance of `IDisposable`, but how do you return the disposable if you are still doing the work? You could fire up another thread so the work happens concurrently, but creating threads is something we are trying to steer away from.

In this example, we have a method that we will use as the delegate to be scheduled. It just fakes some work by performing a spin wait and adding values to the `list` argument. The key here is that we allow the user to cancel with the `CancellationToken` via the disposable we return.

```csharp
public IDisposable Work(IScheduler scheduler, List<int> list)
{
    var tokenSource = new CancellationTokenSource();
    var cancelToken = tokenSource.Token;
    var task = new Task(() =>
    {
        Console.WriteLine();
   
        for (int i = 0; i < 1000; i++)
        {
            var sw = new SpinWait();
   
            for (int j = 0; j < 3000; j++) sw.SpinOnce();
   
            Console.Write(".");
   
            list.Add(i);
   
            if (cancelToken.IsCancellationRequested)
            {
                Console.WriteLine("Cancelation requested");
                
                // cancelToken.ThrowIfCancellationRequested();
                
                return;
            }
        }
    }, cancelToken);
   
    task.Start();
   
    return Disposable.Create(tokenSource.Cancel);
}
```

This code schedules the above code and allows the user to cancel the processing work by pressing Enter

```csharp
var list = new List<int>();
Console.WriteLine("Enter to quit:");

var token = scheduler.Schedule(list, Work);
Console.ReadLine();

Console.WriteLine("Cancelling...");

token.Dispose();

Console.WriteLine("Cancelled");
```

Output:

```
Enter to quit:
........
Cancelling...
Cancelled
CancelLation requested
```

The problem here is that we have introduced explicit use of `Task`. We can avoid explicit usage of a concurrency model if we use the Rx recursive scheduler features instead.

### Recursion

The more advanced overloads of `Schedule` extension methods take some strange looking delegates as parameters. Take special note of the final parameter in each of these overloads of the `Schedule` extension method.

```csharp
public static IDisposable Schedule(
    this IScheduler scheduler, 
    Action<Action> action)
{...}

public static IDisposable Schedule<TState>(
    this IScheduler scheduler, 
    TState state, 
    Action<TState, Action<TState>> action)
{...}

public static IDisposable Schedule(
    this IScheduler scheduler, 
    TimeSpan dueTime, 
    Action<Action<TimeSpan>> action)
{...}

public static IDisposable Schedule<TState>(
    this IScheduler scheduler, 
    TState state, 
    TimeSpan dueTime, 
    Action<TState, Action<TState, TimeSpan>> action)
{...}

public static IDisposable Schedule(
    this IScheduler scheduler, 
    DateTimeOffset dueTime, 
    Action<Action<DateTimeOffset>> action)
{...}

public static IDisposable Schedule<TState>(
    this IScheduler scheduler, 
    TState state, DateTimeOffset dueTime, 
    Action<TState, Action<TState, DateTimeOffset>> action)
{...}   
```

Each of these overloads take a delegate "action" that allows you to call "action" recursively. This may seem a very odd signature, but it makes for a great API. This effectively allows you to create a recursive delegate call. This may be best shown with an example.

This example uses the most simple recursive overload. We have an `Action` that can be called recursively.

```csharp
Action<Action> work = (Action self) =>
{
    Console.WriteLine("Running");
    self();
};

var token = s.Schedule(work);
    
Console.ReadLine();
Console.WriteLine("Cancelling");

token.Dispose();

Console.WriteLine("Cancelled");
```

Output:

```
Enter to quit:
Running
Running
Running
Running
Cancelling
Cancelled
Running
```

Note that we didn't have to write any cancellation code in our delegate. Rx handled the looping and checked for cancellation on our behalf. Brilliant! Unlike simple recursive methods in C#, we are also protected from stack overflows, as Rx provides an extra level of abstraction. Indeed, Rx takes our recursive method and transforms it to a loop structure instead.

#### Creating your own iterator

Earlier in the book, we looked at how we can use [Rx with APM](04_CreatingObservableSequences.html#FromAPM). In our example, we just read the entire file into memory. We also referenced Jeffrey van Gogh's [blog post](http://blogs.msdn.com/b/jeffva/archive/2010/07/23/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading.aspx), which sadly is now out of date; however, his concepts are still sound. Instead of the Iterator method from Jeffrey's post, we can use schedulers to achieve the same result.

The goal of the following sample is to open a file and stream it in chunks. This enables us to work with files that are larger than the memory available to us, as we would only ever read and cache a portion of the file at a time. In addition to this, we can leverage the compositional nature of Rx to apply multiple transformations to the file such as encryption and compression. By reading chunks at a time, we are able to start the other transformations before we have finished reading the file.

First, let us refresh our memory with how to get from the `FileStream`'s APM methods into Rx.

```csharp
var source = new FileStream(@"C:\some-file.txt", FileMode.Open, FileAccess.Read);
var factory = Observable.FromAsyncPattern<byte[], int, int, int>(source.BeginRead, source.EndRead);
var buffer = new byte[source.Length];

IObservable<int> reader = factory(buffer, 0, (int)source.Length);

reader.Subscribe(bytesRead =>  Console.WriteLine("Read {0} bytes from file into buffer", bytesRead));
```

The example above uses `FromAsyncPattern` to create a factory. The factory will take a byte array (`buffer`), an offset (`0`) and a length (`source.Length`); it effectively returns the count of the bytes read as a single-value sequence. When the sequence (`reader`) is subscribed to, `BeginRead` will read values, starting from the offset, into the buffer. In this case, we will read the whole file. Once the file has been read into the buffer, the sequence (`reader`) will push the single value (`bytesRead`) in to the sequence.

This is all fine, but if we want to read chunks of data at a time then this is not good enough. We need to specify the buffer size we want to use. Let's start with 4KB (4096 bytes).

```csharp
var bufferSize = 4096;
var buffer = new byte[bufferSize];
IObservable<int> reader = factory(buffer, 0, bufferSize);
reader.Subscribe(bytesRead => Console.WriteLine("Read {0} bytes from file", bytesRead));
```

This works but will only read a max of 4KB from the file. If the file is larger, we want to keep reading all of it. As the `Position` of the `FileStream` will have advanced to the point it stopped reading, we can reuse the `factory` to reload the buffer. Next, we want to start pushing these bytes into an observable sequence. Let's start by creating the signature of an extension method.

```csharp
public static IObservable<byte> ToObservable(
    this FileStream source, 
    int buffersize, 
    IScheduler scheduler)
{...}
```

We can ensure that our extension method is lazily evaluated by using `Observable.Create`. We can also ensure that the `FileStream` is closed when the consumer disposes of the subscription by taking advantage of the `Observable.Using` operator.

```csharp
public static IObservable<byte> ToObservable(
    this FileStream source, 
    int buffersize, 
    IScheduler scheduler)
{
    var bytes = Observable.Create<byte>(o =>
    {
        ...
    });

    return Observable.Using(() => source, _ => bytes);
}
```

Next, we want to leverage the scheduler's recursive functionality to continuously read chunks of data while still providing the user with the ability to dispose/cancel when they choose. This creates a bit of a pickle; we can only pass in one state parameter but need to manage multiple moving parts (buffer, factory, filestream). To do this, we create our own private helper class:

```csharp
private sealed class StreamReaderState
{
    private readonly int _bufferSize;
    private readonly Func<byte[], int, int, IObservable<int>> _factory;

    public StreamReaderState(FileStream source, int bufferSize)
    {
        _bufferSize = bufferSize;
        _factory = Observable.FromAsyncPattern<byte[], int, int, int>(
            source.BeginRead, 
            source.EndRead);
        Buffer = new byte[bufferSize];
    }

    public IObservable<int> ReadNext()
    {
        return _factory(Buffer, 0, _bufferSize);
    }

    public byte[] Buffer { get; set; }
}
```

This class will allow us to read data into a buffer, then read the next chunk by calling `ReadNext()`. In our `Observable.Create` delegate, we instantiate our helper class and use it to push the buffer into our observable sequence.

```csharp
public static IObservable<byte> ToObservable(
    this FileStream source, 
    int buffersize, 
    IScheduler scheduler)
{
    var bytes = Observable.Create<byte>(o =>
    {
        var initialState = new StreamReaderState(source, buffersize);

        initialState
            .ReadNext()
            .Subscribe(bytesRead =>
            {
                for (int i = 0; i < bytesRead; i++)
                {
                    o.OnNext(initialState.Buffer[i]);
                }
            });
        ...
    });

    return Observable.Using(() => source, _ => bytes);
}
```

So this gets us off the ground, but we are still do not support reading files larger than the buffer. Now, we need to add recursive scheduling. To do this, we need a delegate to fit the required signature. We will need one that accepts a `StreamReaderState` and can recursively call an `Action<StreamReaderState>`.

```csharp
public static IObservable<byte> ToObservable(
    this FileStream source, 
    int buffersize, 
    IScheduler scheduler)
{
    var bytes = Observable.Create<byte>(o =>
    {
        var initialState = new StreamReaderState(source, buffersize);

        Action<StreamReaderState, Action<StreamReaderState>> iterator;
        iterator = (state, self) =>
        {
            state.ReadNext()
                    .Subscribe(bytesRead =>
                        {
                            for (int i = 0; i < bytesRead; i++)
                            {
                                o.OnNext(state.Buffer[i]);
                            }
                            self(state);
                        });
        };
        return scheduler.Schedule(initialState, iterator);
    });

    return Observable.Using(() => source, _ => bytes);
}
```
    
We now have an `iterator` action that will:

- call `ReadNext()`
- subscribe to the result
- push the buffer into the observable sequence
- and recursively call itself.

We also schedule this recursive action to be called on the provided scheduler. Next, we want to complete the sequence when we get to the end of the file. This is easy, we maintain the recursion until the `bytesRead` is 0.

```csharp
public static IObservable<byte> ToObservable(
    this FileStream source, 
    int buffersize, 
    IScheduler scheduler)
{
    var bytes = Observable.Create<byte>(o =>
    {
        var initialState = new StreamReaderState(source, buffersize);

        Action<StreamReaderState, Action<StreamReaderState>> iterator;
        iterator = (state, self) =>
        {
            state.ReadNext()
                    .Subscribe(bytesRead =>
                        {
                            for (int i = 0; i < bytesRead; i++)
                            {
                                o.OnNext(state.Buffer[i]);
                            }
                            if (bytesRead > 0)
                                self(state);
                            else
                                o.OnCompleted();
                        });
        };
        return scheduler.Schedule(initialState, iterator);
    });

    return Observable.Using(() => source, _ => bytes);
}
```

At this point, we have an extension method that iterates on the bytes from a file stream. Finally, let us apply some clean up so that we correctly manage our resources and exceptions, and the finished method looks something like this:

```csharp
public static IObservable<byte> ToObservable(
    this FileStream source, 
    int buffersize, 
    IScheduler scheduler)
{
    var bytes = Observable.Create<byte>(o =>
    {
        var initialState = new StreamReaderState(source, buffersize);
        var currentStateSubscription = new SerialDisposable();
        Action<StreamReaderState, Action<StreamReaderState>> iterator =
        (state, self) =>
            currentStateSubscription.Disposable = state.ReadNext()
                    .Subscribe(
                    bytesRead =>
                    {
                        for (int i = 0; i < bytesRead; i++)
                        {
                            o.OnNext(state.Buffer[i]);
                        }

                        if (bytesRead > 0)
                            self(state);
                        else
                            o.OnCompleted();
                    },
                    o.OnError);

        var scheduledWork = scheduler.Schedule(initialState, iterator);
        return new CompositeDisposable(currentStateSubscription, scheduledWork);
    });

    return Observable.Using(() => source, _ => bytes);
}
```

This is example code and your mileage may vary. I find that increasing the buffer size and returning `IObservable<IList<byte>>` suits me better, but the example above works fine too. The goal here was to provide an example of an iterator that provides concurrent I/O access with cancellation and resource-efficient buffering.

<!--
<a name="ScheduledExceptions"></a>
<h4>Exceptions from scheduled code</h4>
<p>
    TODO:
</p>
-->

#### Combinations of scheduler features

We have discussed many features that you can use with the `IScheduler` interface. Most of these examples, however, are actually using extension methods to invoke the functionality that we are looking for. The interface itself exposes the richest overloads. The extension methods are effectively just making a trade-off; improving usability/discoverability by reducing the richness of the overload. If you want access to passing state, cancellation, future scheduling and recursion, it is all available directly from the interface methods.

```csharp
namespace System.Reactive.Concurrency
{
    public interface IScheduler
    {

    // Gets the scheduler's notion of current time.
    DateTimeOffset Now { get; }

    // Schedules an action to be executed with given state. 
    // Returns a disposable object used to cancel the scheduled action (best effort).
    IDisposable Schedule<TState>(
        TState state, 
        Func<IScheduler, TState, IDisposable> action);

    // Schedules an action to be executed after dueTime with given state. 
    // Returns a disposable object used to cancel the scheduled action (best effort).
    IDisposable Schedule<TState>(
        TState state, 
        TimeSpan dueTime, 
        Func<IScheduler, TState, IDisposable> action);

    // Schedules an action to be executed at dueTime with given state. 
    // Returns a disposable object used to cancel the scheduled action (best effort).
    IDisposable Schedule<TState>(
        TState state, 
        DateTimeOffset dueTime, 
        Func<IScheduler, TState, IDisposable> action);
    }
}
```
    
## Schedulers in-depth

We have largely been concerned with the abstract concept of a scheduler and the `IScheduler` interface. This abstraction allows low-level plumbing to remain agnostic towards the implementation of the concurrency model. As in the file reader example above, there was no need for the code to know which implementation of `IScheduler` was passed, as this is a concern of the consuming code.

Now we take an in-depth look at each implementation of `IScheduler`, consider the benefits and tradeoffs they each make, and when each is appropriate to use.

### ImmediateScheduler

The `ImmediateScheduler` is exposed via the `ImmediateScheduler.Instance` static property. This is the most simple of schedulers as it does not actually schedule anything. If you call `Schedule(Action)` then it will just invoke the action. If you schedule the action to be invoked in the future, the `ImmediateScheduler` will invoke a `Thread.Sleep` for the given period of time and then execute the action. In summary, the `ImmediateScheduler` is synchronous.

### CurrentThreadScheduler

Like the `ImmediateScheduler`, the `CurrentThreadScheduler` is single-threaded. It is exposed via the `Scheduler.Current` static property. The key difference is that the `CurrentThreadScheduler` acts like a message queue or a _Trampoline_. If you schedule an action that itself schedules an action, the `CurrentThreadScheduler` will queue the inner action to be performed later; in contrast, the `ImmediateScheduler` would start working on the inner action straight away. This is probably best explained with an example.

In this example, we analyze how `ImmediateScheduler` and `CurrentThreadScheduler` perform nested scheduling differently.

```csharp
private static void ScheduleTasks(IScheduler scheduler)
{
    Action leafAction = () => Console.WriteLine("----leafAction.");
    Action innerAction = () =>
    {
        Console.WriteLine("--innerAction start.");
        scheduler.Schedule(leafAction);
        Console.WriteLine("--innerAction end.");
    };
    Action outerAction = () =>
    {
        Console.WriteLine("outer start.");
        scheduler.Schedule(innerAction);
        Console.WriteLine("outer end.");
    };
    scheduler.Schedule(outerAction);
}

public void CurrentThreadExample()
{
    ScheduleTasks(Scheduler.CurrentThread);
    /*Output: 
    outer start. 
    outer end. 
    --innerAction start. 
    --innerAction end. 
    ----leafAction. 
    */ 
}

public void ImmediateExample()
{
    ScheduleTasks(Scheduler.Immediate);
    /*Output: 
    outer start. 
    --innerAction start. 
    ----leafAction. 
    --innerAction end. 
    outer end. 
    */ 
}
```

Note how the `ImmediateScheduler` does not really "schedule" anything at all, all work is performed immediately (synchronously). As soon as `Schedule` is called with a delegate, that delegate is invoked. The `CurrentThreadScheduler`, however, invokes the first delegate, and, when nested delegates are scheduled, queues them to be invoked later. Once the initial delegate is complete, the queue is checked for any remaining delegates (i.e. nested calls to `Schedule`) and they are invoked. The difference here is quite important as you can potentially get out-of-order execution, unexpected blocking, or even deadlocks by using the wrong one.

### DispatcherScheduler

The `DispatcherScheduler` is found in `System.Reactive.Window.Threading.dll` (for WPF, Silverlight 4 and Silverlight 5). When actions are scheduled using the `DispatcherScheduler`, they are effectively marshaled to the `Dispatcher`'s `BeginInvoke` method. This will add the action to the end of the dispatcher's _Normal_ priority queue of work. This provides similar queuing semantics to the `CurrentThreadScheduler` for nested calls to `Schedule`.

When an action is scheduled for future work, then a `DispatcherTimer` is created with a matching interval. The callback for the timer's tick will stop the timer and re-schedule the work onto the `DispatcherScheduler`. If the `DispatcherScheduler` determines that the `dueTime` is actually not in the future then no timer is created, and the action will just be scheduled normally.

I would like to highlight a hazard of using the `DispatcherScheduler`. You can construct your own instance of a `DispatcherScheduler` by passing in a reference to a `Dispatcher`. The alternative way is to use the static property `DispatcherScheduler.Instance`. This can introduce hard to understand problems if it is not used properly. The static property does not return a reference to a static field, but creates a new instance each time, with the static property `Dispatcher.CurrentDispatcher` as the constructor argument. If you access `Dispatcher.CurrentDispatcher` from a thread that is not the UI thread, it will thus give you a new instance of a `Dispatcher`, but it will not be the instance you were hoping for.

For example, imagine that we have a WPF application with an `Observable.Create` method. In the delegate that we pass to `Observable.Create`, we want to schedule the notifications on the dispatcher. We think this is a good idea because any consumers of the sequence would get the notifications on the dispatcher for free.

```csharp
var fileLines = Observable.Create<string>(o =>
{
    var dScheduler = DispatcherScheduler.Instance;
    var lines = File.ReadAllLines(filePath);

    foreach (var line in lines)
    {
        var localLine = line;
        dScheduler.Schedule(() => o.OnNext(localLine));
    }

    return Disposable.Empty;
});
```

This code may intuitively seem correct, but actually takes away power from consumers of the sequence. When we subscribe to the sequence, we decide that reading a file on the UI thread is a bad idea. So we add in a `SubscribeOn(Scheduler.NewThread)` to the chain as below:

```csharp
fileLines
    .SubscribeOn(Scheduler.ThreadPool)
    .Subscribe(line => Lines.Add(line));
```

This causes the create delegate to be executed on a new thread. The delegate will read the file then get an instance of a `DispatcherScheduler`. The `DispatcherScheduler` tries to get the `Dispatcher` for the current thread, but we are no longer on the UI thread, so there isn't one. As such, it creates a new dispatcher that is used for the `DispatcherScheduler` instance. We schedule some work (the notifications), but, as the underlying `Dispatcher` has not been run, nothing happens; we do not even get an exception. I have seen this on a commercial project and it left quite a few people scratching their heads.

This takes us to one of our guidelines regarding scheduling: <q>the use of `SubscribeOn` and `ObserveOn` should only be invoked by the final subscriber</q>. If you introduce scheduling in your own extension methods or service methods, you should allow the consumer to specify their own scheduler. We will see more reasons for this guidance in the next chapter.

### EventLoopScheduler

The `EventLoopScheduler` allows you to designate a specific thread to a scheduler. Like the `CurrentThreadScheduler` that acts like a trampoline for nested scheduled actions, the `EventLoopScheduler` provides the same trampoline mechanism. The difference is that you provide an `EventLoopScheduler` with the thread you want it to use for scheduling instead, of just picking up the current thread.

The `EventLoopScheduler` can be created with an empty constructor, or you can pass it a thread factory delegate.

```csharp
// Creates an object that schedules units of work on a designated thread.
public EventLoopScheduler()
{...}

// Creates an object that schedules units of work on a designated thread created by the 
// provided factory function.
public EventLoopScheduler(Func&lt;ThreadStart, Thread> threadFactory)
{...}
```

The overload that allows you to pass a factory enables you to customize the thread before it is assigned to the `EventLoopScheduler`. For example, you can set the thread name, priority, culture and most importantly whether the thread is a background thread or not. Remember that if you do not set the thread's property `IsBackground` to false, then your application will not terminate until it the thread is terminated. The `EventLoopScheduler` implements `IDisposable`, and calling Dispose will allow the thread to terminate. As with any implementation of `IDisposable`, it is appropriate that you explicitly manage the lifetime of the resources you create.

This can work nicely with the `Observable.Using` method, if you are so inclined. This allows you to bind the lifetime of your `EventLoopScheduler` to that of an observable sequence - for example, this `GetPrices` method that takes an `IScheduler` for an argument and returns an observable sequence.

```csharp
private IObservable&lt;Price> GetPrices(IScheduler scheduler)
{...}
```

Here we bind the lifetime of the `EventLoopScheduler` to that of the result from the `GetPrices` method.

```csharp
Observable.Using(() => new EventLoopScheduler(), els => GetPrices(els)).Subscribe(...)
```

### New Thread

If you do not wish to manage the resources of a thread or an `EventLoopScheduler`, then you can use `NewThreadScheduler`. You can create your own instance of `NewThreadScheduler` or get access to the static instance via the property `Scheduler.NewThread`. Like `EventLoopScheduler`, you can use the parameterless constructor or provide your own thread factory function. If you do provide your own factory, be careful to set the `IsBackground` property appropriately.

When you call `Schedule` on the `NewThreadScheduler`, you are actually creating an `EventLoopScheduler` under the covers. This way, any nested scheduling will happen on the same thread. Subsequent (non-nested) calls to `Schedule` will create a new `EventLoopScheduler` and call the thread factory function for a new thread too.

In this example we run a piece of code reminiscent of our comparison between `Immediate` and `Current` schedulers. The difference here, however, is that we track the `ThreadId` that the action is performed on. We use the `Schedule` overload that allows us to pass the Scheduler instance into our nested delegates. This allows us to correctly nest calls.

```csharp
private static IDisposable OuterAction(IScheduler scheduler, string state)
{
    Console.WriteLine("{0} start. ThreadId:{1}", state, Thread.CurrentThread.ManagedThreadId);

    scheduler.Schedule(state + ".inner", InnerAction);

    Console.WriteLine("{0} end. ThreadId:{1}", state, Thread.CurrentThread.ManagedThreadId);

    return Disposable.Empty;
}

private static IDisposable InnerAction(IScheduler scheduler, string state)
{
    Console.WriteLine("{0} start. ThreadId:{1}", state, Thread.CurrentThread.ManagedThreadId);
    
    scheduler.Schedule(state + ".Leaf", LeafAction);
    
    Console.WriteLine("{0} end. ThreadId:{1}", state, Thread.CurrentThread.ManagedThreadId);

    return Disposable.Empty;
}

private static IDisposable LeafAction(IScheduler scheduler, string state)
{
    Console.WriteLine("{0}. ThreadId:{1}", state, Thread.CurrentThread.ManagedThreadId);

    return Disposable.Empty;
}
```

When executed with the `NewThreadScheduler` like this:

```csharp
Console.WriteLine("Starting on thread :{0}", Thread.CurrentThread.ManagedThreadId);
Scheduler.NewThread.Schedule("A", OuterAction);
```

Output:

```
Starting on thread :9
A start. ThreadId:10
A end. ThreadId:10
A.inner start . ThreadId:10
A.inner end. ThreadId:10
A.inner.Leaf. ThreadId:10
```

As you can see, the results are very similar to the `CurrentThreadScheduler`, except that the trampoline happens on a separate thread. This is in fact exactly the output we would get if we used an `EventLoopScheduler`. The differences between usages of the `EventLoopScheduler` and the `NewThreadScheduler`start to appear when we introduce a second (non-nested) scheduled task.

```csharp
Console.WriteLine("Starting on thread :{0}", Thread.CurrentThread.ManagedThreadId);
Scheduler.NewThread.Schedule("A", OuterAction);
Scheduler.NewThread.Schedule("B", OuterAction);
```

Output:

```
Starting on thread :9
A start. ThreadId:10
A end. ThreadId:10
A.inner start . ThreadId:10
A.inner end. ThreadId:10
A.inner.Leaf. ThreadId:10
B start. ThreadId:11
B end. ThreadId:11
B.inner start . ThreadId:11
B.inner end. ThreadId:11
B.inner.Leaf. ThreadId:11
```

Note that there are now three threads at play here. Thread 9 is the thread we started on and threads 10 and 11 are performing the work for our two calls to Schedule.

### Thread Pool

The `ThreadPoolScheduler` will simply just tunnel requests to the `ThreadPool`. For requests that are scheduled as soon as possible, the action is just sent to `ThreadPool.QueueUserWorkItem`. For requests that are scheduled in the future, a `System.Threading.Timer` is used.

As all actions are sent to the `ThreadPool`, actions can potentially run out of order. Unlike the previous schedulers we have looked at, nested calls are not guaranteed to be processed serially. We can see this by running the same test as above but with the `ThreadPoolScheduler`.

```csharp
Console.WriteLine("Starting on thread :{0}", Thread.CurrentThread.ManagedThreadId);
Scheduler.ThreadPool.Schedule("A", OuterAction);
Scheduler.ThreadPool.Schedule("B", OuterAction);
```

The output

```
Starting on thread :9
A start. ThreadId:10
A end. ThreadId:10
A.inner start . ThreadId:10
A.inner end. ThreadId:10
A.inner.Leaf. ThreadId:10
B start. ThreadId:11
B end. ThreadId:11
B.inner start . ThreadId:10
B.inner end. ThreadId:10
B.inner.Leaf. ThreadId:11
```

Note, that as per the `NewThreadScheduler` test, we initially start on one thread but all the scheduling happens on two other threads. The difference is that we can see that part of the second run "B" runs on thread 11 while another part of it runs on 10.

### TaskPool

The `TaskPoolScheduler` is very similar to the `ThreadPoolScheduler` and, when available (depending on your target framework), you should favor it overthe later. Like the `ThreadPoolScheduler`, nested scheduled actions are not guaranteed to be run on the same thread. Running the same test with the `TaskPoolScheduler` shows us similar results.

```csharp
Console.WriteLine("Starting on thread :{0}", Thread.CurrentThread.ManagedThreadId);
Scheduler.TaskPool.Schedule("A", OuterAction);
Scheduler.TaskPool.Schedule("B", OuterAction);
```

Output:

```
Starting on thread :9
A start. ThreadId:10
A end. ThreadId:10
B start. ThreadId:11
B end. ThreadId:11
A.inner start . ThreadId:10
A.inner end. ThreadId:10
A.inner.Leaf. ThreadId:10
B.inner start . ThreadId:11
B.inner end. ThreadId:11
B.inner.Leaf. ThreadId:10
```

### TestScheduler

It is worth noting that there is also a `TestScheduler` accompanied by its base classes `VirtualTimeScheduler` and `VirtualTimeSchedulerBase`. The latter two are not really in the scope of an introduction to Rx, but the former is. We will cover all things testing including the `TestScheduler` in the next chapter, [Testing Rx](16_TestingRx.html).

## Selecting an appropriate scheduler

With all of these options to choose from, it can be hard to know which scheduler to use and when. Here is a simple check list to help you in this daunting task:

### UI Applications

- The final subscriber is normally the presentation layer and should control the scheduling.
- Observe on the `DispatcherScheduler` to allow updating of ViewModels
- Subscribe on a background thread to prevent the UI from becoming unresponsive
  - If the subscription will not block for more than 50ms then
      - Use the `TaskPoolScheduler` if available, or
      - Use the `ThreadPoolScheduler`
  - If any part of the subscription could block for longer than 50ms, then you shoulduse the `NewThreadScheduler`. 

### Service layer

* If your service is reading data from a queue of some sort, consider using a dedicated `EventLoopScheduler`. 
This way, you can preserve order of events
* If processing an item is expensive (>50ms or requires I/O), then consider using a `NewThreadScheduler`
* If you just need the scheduler for a timer, e.g. for `Observable.Interval` or `Observable.Timer`, then favor the `TaskPool`. 
Use the `ThreadPool` if the `TaskPool` is not available for your platform.

> The `ThreadPool` (and the `TaskPool` by proxy) have a time delay beforethey will increase the number of threads that they use. This delay is 500ms. Letus consider a PC with two cores that we will schedule four actions onto. By default,the thread pool size will be the number of cores (2). If each action takes 1000ms, then two actions will be sitting in the queue for 500ms before the thread pool size is increased. Instead of running all four actions in parallel, which would take one second in total, the work is not completed for 1.5 seconds as two of the actions sat in the queue for 500ms. For this reason, you should only schedule work thatis very fast to execute (guideline 50ms) onto the ThreadPool or TaskPool. Conversely,creating a new thread is not free, but with the power of processors today the creation of a thread for work over 50ms is a small cost.

Concurrency is hard. We can choose to make our life easier by taking advantage of Rx and its scheduling features. We can improve it even further by only using Rx where appropriate. While Rx has concurrency features, these should not be mistaken for a concurrency framework. Rx is designed for querying data, and as discussed in [the first chapter](01_WhyRx.html#Could), parallel computations or composition of asynchronous methods is more appropriate for other frameworks.

Rx solves the issues for concurrently generating and consuming data via the `ObserveOn`/`SubscribeOn` methods. By using these appropriately, we can simplify our code base, increase responsiveness and reduce the surface area of our concurrency concerns. Schedulers provide a rich platform for processing work concurrently without the need to be exposed directly to threading primitives. They also help with common troublesome areas of concurrency such as cancellation, passing state and recursion. By reducing the concurrency surface area, Rx provides a (relatively) simple yet powerful set of concurrency features paving the way to the [pit of success](http://blogs.msdn.com/b/brada/archive/2003/10/02/50420.aspx).


TODO: IScheduler.Catch extension method?