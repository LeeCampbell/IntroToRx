---
title : Creating Sequences
---

# Creating Observable Sequences

In the preceding chapter, we saw the two fundamental Rx interfaces, `IObservable<T>` and `IObserver<T>`. We also saw how to receive events by implementing `IObserver<T>`, and also by using implementations supplied by the `System.Reactive` package. In this chapter we'll see how to create `IObservable<T>` sources to represent source events of interest in your application.

We will begin by implementing `IObservable<T>` directly. In practice, it's relatively unusual to do that, so we'll then look at the various ways you can get `System.Reactive` to supply an implementation that does most of the work for you.

## A Very Basic `IObservable<T>` Implementation

Here's an implementation of an `IObservable<int>` that produces a sequence of numbers:

```csharp
public class MySequenceOfNumbers : IObservable<int>
{
    public IDisposable Subscribe(IObserver<int> observer)
    {
        observer.OnNext(1);
        observer.OnNext(2);
        observer.OnNext(3);
        observer.OnCompleted();
        return System.Reactive.Disposables.Disposable.Empty; // Handy do-nothing IDisposable
    }
}
```

We can test this by constructing an instance of it, and then subscribing to it:

```csharp
var numbers = new MySequenceOfNumbers();
numbers.Subscribe(
    number => Console.WriteLine($"Received value: {number}"),
    () => Console.WriteLine("Sequence terminated"));
```

This produces the following output:

```
Received value 1
Received value 2
Received value 3
Sequence terminated
```

Although `MySequenceOfNumbers` is technically a correct implementation of `IObservable<int>`, it is a little too simple to be useful. For one thing, we typically use Rx when there are events of interest, but this is not really reactive at all—it just produces a fixed set of numbers immediately. Moreover, the implementation is blocking—it doesn't even return from `Subscribe` until after it has finished producing all of its values. This example illustrates the basics of how a source provides events to a subscriber, but if we just want to represent a predetermined sequence of numbers, we might as well use an `IEnumerable<T>` implementation such as `List<T>` or an array.

## Representing Filesystem Events in Rx

Let's look at something a little more realistic. This is a wrapper around .NET's `FileSystemWatcher`, presenting filesystem change notifications as an `IObservable<FileSystemEventArgs>`. (Note: this is not necessarily the best design for an Rx `FileSystemWatcher` wrapper. The watcher provides events for several different types of change, and one of them, `Renamed`, provides details as an `RenamedEventArgs`. That derives from `FileSystemEventArgs` so collapsing everything down to a single event stream does work, but this would be inconvenient for applications that wanted access to the details of rename events. A more serious design problem is that this is incapable of reporting more than one event from `FileSystemWatcher.Error`. Such errors might be transient and recoverable, in which case an application might want to continue operating, but since this class chooses to represent everything with a single `IObservable<T>`, it reports errors by invoking the observer's `OnError`, at which point the rules of Rx oblige us to stop. It would be possible to work around this with Rx's `Retry` operator, which can automatically resubscribe after an error, but it might be better to offer a separate `IObservable<ErrorEventArgs>` so that we can report errors in a non-terminating way. However, the additional complication of that won't always be warranted—the simplicity of this design means it will be a good fit for some applications. As is often the way with software design, there isn't a one-size-fits-all approach.)

```cs
// Represents filesystem changes as an Rx observable sequence.
// NOTE: this is an oversimplified example for illustration purposes.
//       It does not handle multiple subscribers efficiently, it does not
//       use IScheduler, and it stops immediately after the first error.
public class RxFsEvents : IObservable<FileSystemEventArgs>
{
    private readonly string folder;

    public RxFsEvents(string folder)
    {
        this.folder = folder;
    }

    public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
    {
        // Inefficient if we get multiple subscribers.
        FileSystemWatcher watcher = new(this.folder);

        // FileSystemWatcher's documentation says nothing about which thread
        // it raises events on (unless you use its SynchronizationObject,
        // which integrates well with Windows Forms, but is inconvenient for
        // us to use here) nor does it promise to wait until we've
        // finished handling one event before it delivers the next. The Mac,
        // Windows, and Linux implementations are all significantly different,
        // so it would be unwise to rely on anything not guaranteed by the
        // documentation. (As it happens, the Win32 implementation on .NET 7
        // does appear to wait until each event handler returns before
        // delivering the next event, so we probably would get way with
        // ignoring this issue. For now. On Windows. And actually the Linux
        // implementation dedicates a single thread to this job, but there's
        // a comment in the source code saying that this should probably
        // change - another reason to rely only on documented behaviour.)
        // So it's our problem to ensure we obey the rules of IObserver<T>.
        // First, we need to make sure that we only make one call at a time
        // into the observer. A more realistic example would use an Rx
        // IScheduler, but since we've not explained what those are yet,
        // we're just going to use lock with this object.
        object sync = new();

        // More subtly, the FileSystemWatcher documentation doesn't make it
        // clear whether we might continue to get a few more change events
        // after it has reported an error. Since there are no promises about
        // threads, it's possible that race conditions exist that would lead to
        // us trying to handle an event from a FileSystemWatcher after it has
        // reported an error. So we need to remember if we've already called
        // OnError to make sure we don't break the IObserver<T> rules in that
        // case.
        bool onErrorAlreadyCalled = false;

        void SendToObserver(object _, FileSystemEventArgs e)
        {
            lock (sync)
            {
                if (!onErrorAlreadyCalled)
                {
                    observer.OnNext(e); 
                }
            }
        }

        watcher.Created += SendToObserver;
        watcher.Changed += SendToObserver;
        watcher.Renamed += SendToObserver;
        watcher.Deleted += SendToObserver;

        watcher.Error += (_, e) =>
        {
            lock (sync)
            {
                // Maybe the FileSystemWatcher can report multiple errors, but
                // we're only allowed to report one to IObservable<T>.
                if (onErrorAlreadyCalled)
                {
                    observer.OnError(e.GetException());
                    onErrorAlreadyCalled = true; 
                    watcher.Dispose();
                }
            }
        };

        watcher.EnableRaisingEvents = true;

        return watcher;
    }
}
```

That got more complex fast. This illustrates that `IObservable<T>` implementations are responsible for obeying the `IObserver<T>` rules. This is generally a good thing: it keeps the messy concerns around concurrency contained in a single place. Any `IObserver<FileSystemEventArgs>` that I subscribe to this `RxFsEvents` doesn't have to worry about concurrency, because it can count on the `IObserver<T>` rules, which guarantee that it will only have to handle one thing at a time. If I hadn't been required to enforce these rules in the source, it might have made my `RxFsEvents` class simpler, but all of that complexity of dealing with overlapping events would have spread out into the code that handles the events. Concurrency is hard enough to deal with when its effects are contained. Once it starts to spread across multiple types, it can become almost impossible to reason about. Rx's `IObserver<T>` rules prevent this from happening.

(Note: this is a significant feature of Rx. The rules keep things simple for observers. This becomes increasingly important as the complexity of your event sources or event process grows.)

There are a couple of issues with this code (aside from the API design issues already mentioned). One is that when `IObservable<T>` implementations produce events modelling real-life asynchronous activity (such as filesystem changes) applications will often want some way to take control over which threads notifications arrive on. For example, UI frameworks tend to have thread affinity requirements—you typically need to be on a particular thread to be allowed to update the user interface. Rx provides mechanisms for redirecting notifications onto different schedulers, so we can work around it, but we would normally expect to be able to provide this sort of observer with an `IScheduler`, and for it to deliver notifications through that. We'll discuss schedulers in later chapters.

The other issue is that this does not deal with multiple subscribers efficiently. You're allowed to call `IObservable<T>.Subscribe` multiple times, and if you do that with this code, it will create a new `FileSystemWatcher` each time. That could happen more easily than you might think. Suppose we had an instance of this watcher, and wanted to handle different events in different ways. We might use the `Where` operator to define observable sources that split events up in the way we want:

```cs
IObservable<FileSystemEventArgs> configChanges =
    fs.Where(e => Path.GetExtension(e.Name) == ".config");
IObservable<FileSystemEventArgs> deletions =
    fs.Where(e => e.ChangeType == WatcherChangeTypes.Deleted);
```

When you call `Subscribe` on the `IObservable<T>` returned by the `Where` operator, it will call `Subscribe` on its input. So in this case, if we call `Subscribe` on both `configChanges` and `deletions`, that will result in _two_ calls to `Subscribe` on `rs`. So if `rs` is an instance of our `RxFsEvents` type above, each one will construct its own `FileSystemEventWatcher`, which is inefficient.

Rx offers a few ways to deal with this. It provides operators designed specifically to take an `IObservable<T>` that does not tolerate multiple subscribers and wrap it in an adapter that can:

```cs
IObservable<FileSystemEventArgs> fs =
    new RxFsEvents(@"c:\temp")
    .Publish()
    .RefCount();
```

But this is leaping ahead. If you want to build a type that is inherently multi-subscriber-friendly, all you really need to do is keep track of all your subscribers and notify each of them in a loop. Here's a modified version of the filesystem watcher:

```cs
public class RxFsEventsMultiSubscriber : IObservable<FileSystemEventArgs>
{
    private readonly object sync = new();
    private readonly List<Subscription> subscribers = new();
    private readonly FileSystemWatcher watcher;

    public RxFsEventsMultiSubscriber(string folder)
    {
        this.watcher = new FileSystemWatcher(folder);

        watcher.Created += SendEventToObservers;
        watcher.Changed += SendEventToObservers;
        watcher.Renamed += SendEventToObservers;
        watcher.Deleted += SendEventToObservers;

        watcher.Error += SendErrorToObservers;
    }

    public IDisposable Subscribe(IObserver<FileSystemEventArgs> observer)
    {
        Subscription sub = new(this, observer);
        lock (this.sync)
        {
            this.subscribers.Add(sub); 

            if (this.subscribers.Count == 1)
            {
                // We had no subscribers before, but now we've got one so we need
                // to start up the FileSystemWatcher.
                watcher.EnableRaisingEvents = true;
            }
        }

        return sub;
    }

    private void Unsubscribe(Subscription sub)
    {
        lock (this.sync)
        {
            this.subscribers.Remove(sub);

            if (this.subscribers.Count == 0)
            {
                watcher.EnableRaisingEvents = false;
            }
        }
    }

    void SendEventToObservers(object _, FileSystemEventArgs e)
    {
        lock (this.sync)
        {
            foreach (var subscription in this.subscribers)
            {
                subscription.Observer.OnNext(e);
            }
        }
    }

    void SendErrorToObservers(object _, ErrorEventArgs e)
    {
        Exception x = e.GetException();
        lock (this.sync)
        {
            foreach (var subscription in this.subscribers)
            {
                subscription.Observer.OnError(x);
            }

            this.subscribers.Clear();
        }
    }

    private class Subscription : IDisposable
    {
        private RxFsEventsMultiSubscriber? parent;

        public Subscription(
            RxFsEventsMultiSubscriber rxFsEventsMultiSubscriber,
            IObserver<FileSystemEventArgs> observer)
        {
            this.parent = rxFsEventsMultiSubscriber;
            this.Observer = observer;
        }
        
        public IObserver<FileSystemEventArgs> Observer { get; }

        public void Dispose()
        {
            this.parent?.Unsubscribe(this);
            this.parent = null;
        }
    }
}
```

This creates only a single `FileSystemWatcher` instance no matter how many times `Subscribe` is called. Notice that I've had to introduce a nested class to provide the `IDisposable` that `Subscribe` returns. I didn't need that with the very first `IObservable<T>` implementation in this chapter because it had already completed the sequence before returning, so it was able to return the `Disposable.Empty` property conveniently supplied by Rx. (This is handy in cases where you're obliged to supply an `IDisposable`, but you don't actually need to do anything when disposed.) And in my first `FileSystemWatcher` wrapper, `RxFsEvents`, I just returned the `FileSystemWatcher` itself from `Dispose`. (This works because `FileSystemWatcher.Dispose` shuts down the watcher, and each subscriber was given its own `FileSystemWatcher`.) But now that a single `FileSystemWatcher` supports multiple observers, we need to do a little more work when an observer unsubscribes.

One a `Subscription` instance that we returned from `Subscribe` gets disposed, it removes itself from the list of subscribers, ensuring that it won't receive any more notifications. It also sets the `FileSystemWatcher`'s `EnableRaisingEvents` to false if there are no more subscribers, ensuring that this source does not do unnecessary work if nothing needs notifications right now.

This is looking more realistic than the first example. This is truly a source of events that could occur at any moment (making this exactly the sort of thing Rx does well) and it now handles multiple subscribers intelligently. However, we wouldn't often write things this way. We're doing all the work ourselves here—this code doesn't even require a reference to the `System.Reactive` package because the only Rx types it refers to are `IObservable<T>` and `IObserver<T>`, both of which are built into the .NET runtime libraries. In practice we typically defer to helpers in `System.Reactive` because they can do a lot of work for us.

For example, suppose we only cared about `Changed` events. We could write just this:

```cs
FileSystemWatcher watcher = new (@"c:\temp");
IObservable<FileSystemEventArgs> changes = Observable
    .FromEventPattern<FileSystemEventArgs>(watcher, nameof(watcher.Changed))
    .Select(ep => ep.EventArgs);
watcher.EnableRaisingEvents = true;
```

Here we're using the `FromEventPattern` helper from the `System.Reactive` library's `Observable` class, which can be used to build an `IObservable<T>` from any .NET event that conforms the the normal pattern (in which event handlers take two arguments: a sender of type `object`, and then some `EventArgs`-derived type containing information about the event). This is not as flexible as the earlier example—it reports only one of the events, and we have to manually start (and, if necessary stop) the `FileSystemWatcher`. But for some applications that will be good enough, and this is a lot less code to write. If we were aiming to write a fully-featured wrapper for `FileSystemWatcher` suitable for many different scenarios, it might be worth writing a specialized `IObservable<T>` implementation as shown earlier. (We could easily extend this last example to watch all of the events—we'd just use the `FromEventPattern` once for each event, and then use `Observable.Merge` to combine the four resulting observables into one. The only real benefit we're getting from a full custom implementation is that we can automatically start and stop the `FileSystemWatcher` depending on whether there are currently any observers.) But if we just need to represent some events as an `IObservable<T>` so that we can work with them in our application, we can just use this simpler approach.

In practice, we almost always get `System.Reactive` to implement `IObservable<T>` for us. Even if we want to take control of certain aspects (such as automatically starting up and shutting down the `FileSystemWatcher` in these examples) we can almost always find a combination of operators that enable this. The following code uses various methods from `System.Reactive` to return an `IObservable<FileSystemEventArgs>` that has all the same functionality as the fully-featured hand-written `RxFsEventsMultiSubscriber` above, but with considerably less code.

```cs
IObservable<FileSystemEventArgs> ObserveFileSystem(string folder)
{
    return 
        // Observable.Defer enables us to avoid doing any work
        // until we have a subscriber.
        Observable.Defer(() =>
            {
                FileSystemWatcher fsw = new(folder);
                fsw.EnableRaisingEvents = true;

                return Observable.Return(fsw);
            })
        // Once the preceding parts emits the FileSystemWatcher
        // (which will happen when someone first subscribes), we
        // want to wrap all the events as IObservable<T>s, for which
        // we'll use a projection. To avoid ending up with an
        // IObservable<IObservable<FileSystemEventArgs>>, we use
        // SelectMany, which effectively flattens it by one level.
        .SelectMany(fsw =>
            Observable.Merge(new[]
                {
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Created += h, h => fsw.Created -= h),
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Changed += h, h => fsw.Changed -= h),
                    Observable.FromEventPattern<RenamedEventHandler, FileSystemEventArgs>(
                        h => fsw.Renamed += h, h => fsw.Renamed -= h),
                    Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                        h => fsw.Deleted += h, h => fsw.Deleted -= h)
                })
            // FromEventPattern supplies both the sender and the event
            // args. Extract just the latter.
            .Select(ep => ep.EventArgs)
            // The Finally here ensures the watcher gets shut down once
            // we have no subscribers.
            .Finally(() => fsw.Dispose()))
        // This combination of Publish and RefCount means that multiple
        // subscribers will get to share a single FileSystemWatcher,
        // but that it gets shut down if all subscribers unsubscribe.
        .Publish()
        .RefCount();
}
```

I've used a lot of methods there, most of which I've not talked about before. For that example to make any sense, I clearly need to start describing the numerous ways in which the `System.Reactive` package can implement `IObservable<T>` for you.

## Simple factory methods

Due to the large number of methods available for creating observable sequences, we will break them down into categories. Our first category of methods create `IObservable<T>` sequences produce at most a single result.

### Observable.Return

One of the simplest factory methods is `Observable.Return<T>(T value)`, which you've already seen in the `Quiescent` example in the preceding chapter. This method takes a value of type `T` and returns an `IObservable<T>` which will produce this single value and then complete. In a sense, this _wraps_ a value in an `IObservable<T>`; it's conceptually similar to writing `new T[] { value }`, in that it's a sequence containing just one element.

```csharp
var singleValue = Observable.Return<string>("Value");
```

I specified the type parameter for clarity, but this is not necessary as the compiler can inferred the type from argument provided:

```csharp
var singleValue = Observable.Return("Value");
```


### Observable.Empty

Sometimes it can be useful to have an empty sequence. .NET's `Enumerable.Empty<T>()` does this for `IEnumerable<T>`, and Rx has a direct equivalent in the form of `Observable.Empty<T>()`, which returns an empty `IObservable<T>`. We need to provide the type argument because there's no value from which the compiler can infer the type.

```csharp
var empty = Observable.Empty<string>();
```

In practice, an empty sequence is one that immediately provides an `OnCompleted` notification to any subscriber.

### Observable.Never

The `Observable.Never<T>()` method returns a sequence which, like `Empty`, does not produce any values, but unlike `Empty`, it never ends. In practice, that means that it never invokes any method (neither `OnNext`, `OnCompleted`, nor `OnError`) on subscribers. Whereas `Observable.Empty<T>()` completes immediately, `Observable.Never<T>` has infinite duration.

```csharp
var never = Observable.Never<string>();
```

It might not seem obvious why this could be useful. It tends to be used in places where we use observables to represent time-based information. Sometimes we don't actually care what emerges from an observable; we might care only _when_ something (anything) happens. For example, in the preceding chapter, the `Quiescent` example used the `Buffer` operator, which works over two observable sequences: the first contains the items of interest, and the second is used purely to determine how to cut the first into chunks. `Buffer` doesn't do anything with the values produced by the second observable: it pays attention only to _when_ value emerge, completing the previous chunk each time the second observable produces a value. And if we're representing temporal information it can sometimes be useful to have a way to represent the idea that some event never occurs.

### Observable.Throw

`Observable.Throw<T>(Exception)` returns a sequence that immediately reports an error to any subscriber. As with `Empty` and `Never`, we don't supply a value to this method (just an exception) so we need to provide a type parameter so that it knows what `T` to use in the `IObservable<T>` that it returns. (It will never actually a produce a `T`, but you can't have an instance of `IObservable<T>` without picking some particular type for `T`.)

```csharp
var throws = Observable.Throw<string>(new Exception()); 
// Behaviorally equivalent to
var subject = new ReplaySubject<string>(); 
subject.OnError(new Exception());
```

### Observable.Create

The `Create` factory method is more powerful than the other creation methods because it can be used to create any kind of sequence. You could implement any of the preceding four methods with `Observable.Create`.
The method signature itself may seem more complex than necessary at first, but becomes quite natural once you have used it.

```csharp
// Creates an observable sequence from a specified Subscribe method implementation.
public static IObservable<TSource> Create<TSource>(
    Func<IObserver<TSource>, IDisposable> subscribe)
{...}
public static IObservable<TSource> Create<TSource>(
    Func<IObserver<TSource>, Action> subscribe)
{...}
```

You provide this with a delegate that will be executed anytime a subscription is made. Your delegate will be passed an `IObserver<T>`. Logically speaking, this represents the observer passed to the `Subscribe` method, although in practice Rx puts a wrapper around that for various reasons. You can call the `OnNext`/`OnError`/`OnCompleted` methods as you need. This is one of the few scenarios where you will work directly with the `IObserver<T>` interface. Here's a simple example that produces two items:

```csharp
private IObservable<string> SomeLetters()
{
    return Observable.Create<string>(
        (IObserver<string> observer) =>
        {
            observer.OnNext("a");
            observer.OnNext("b");
            observer.OnCompleted();

            return Disposable.Empty;
        });
}
```

Your delegate must return either an `IDisposable` or an `Action` to enable unsubscription. When the subscriber disposes their subscription in order to unsubscribe, Rx will invoke `Dispose()` on the `IDisposable` you returned, or in the case where you returned an `Action`, it will invoke that.

This example is reminiscent of the `MySequenceOfNumbers` example from the start of this chapter, in that it immediately produces a few fixed values. The main difference in this case is that Rx adds some wrappers that can handle awkward situations such as re-entrancy. Rx will sometimes automatically defer work to prevent deadlocks, so it's possible that code consuming the `IObservable<string>` returned by this method will see a call to `Subscribe` return before the callback in the code above runs, in which case it would be possible for them to unsubscribe inside their `OnNext` handler. (In the `MySequenceOfNumbers` case that can't actually happen because that doesn't return an `IDisposable` until after it has finished producing all values. This might look like it would do the same, but the `IDisposable` we return isn't necessarily the one the subscriber sees, and in cases where deferred execution is occurring, the subscriber may well already have an `IDisposable` in hand by the time we call `OnNext`.)

You might be wondering how the `IDisposable` or callback can ever do anything useful, given that it's the return value of the callback, so we can only return it to Rx as the last thing our callback does. Won't we always have finished our work by the time we return, meaning there's nothing to cancel? Not necessarily—we might kick off some work that continues to run after we return. This next example does that, meaning that the unsubscription action it returns is able to do something useful—it sets a cancellation token that is being observed by the loop that generates our observable's output.

```cs
IObservable<char> KeyPresses() =>
    Observable.Create<char>(observer =>
    {
        CancellationTokenSource cts = new();
        Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                ConsoleKeyInfo ki = Console.ReadKey();
                observer.OnNext(ki.KeyChar);
            }
        });

        return () => cts.Cancel();
    });
```

This illustrates how cancellation won't necessarily take effect immediately. The `Console.ReadKey` API does not offer an overload accepting a `CancellationToken`, so this observable won't be able to detect that cancellation is requested until the user next presses a key, causing `ReadKey` to return.

Bearing in mind that cancellation might have been requested while we were waiting for `ReadKey` to return, you might think we should check for that after `ReadKey` returns and before calling `OnNext`. In fact it doesn't matter if we don't, because Rx ensures that you can't break the rule that says an observable source must not call into an observer after a call to `Dispose` on that observer's subscription returns. This is one reason why the `IObserver<T>` it passes to you is a wrapper: if you continue to call methods on that `IObserver<T>` after a request to unsubscribe, Rx just ignores the calls. This means there are two important things to be aware of: if you _do_ ignore attempts to unsubscribe and continue to do work to produce items, you are just wasting time because nothing will receive those items; if you call `OnError` it's possible that nothing is listening and that the error will be completely ignored.

There are overloads of `Create` designed to support `async` methods. This methods exploits this to be able to the asynchronous `ReadLineAsync` method to present lines of text from a file as an observable source.

```cs
IObservable<string> ReadFileLines(string path) =>
    Observable.Create<string>(async (observer, cancellationToken) =>
    {
        using (var reader = File.OpenText(path))
        {
            while (cancellationToken.IsCancellationRequested)
            {
                string? line = await reader.ReadLineAsync(cancellationToken);
                if (line is null)
                {
                    break;
                }

                observer.OnNext(line);
            }

            observer.OnCompleted();
        }
    });
```

Reading data from a storage device typically doesn't happen instantaneously (unless it happens to be in the filesystem cache already), so this source will provide data as quickly as it can be read from storage.

Notice that because this is an `async` method, it will typically return to its caller before it completes. (The first `await` that actually has to wait returns, and the remainder of the method runs via a callback when the work completes.) That means that subscribers will typically be in possession of the `IDisposable` representing their subscription before this method returns, so we're using a different mechanism to handle unsubscription here. This particular overload of `Create` passes its callback not just an `IObserver<T>` but also a `CancellationToken`, with which it will request cancellation when unsubscription occurs.

File IO can encounter errors. The file we're looking for might not exist, or we might be unable to open it due to security restrictions, or because some other application is using it. The file might be on a remote storage server, and we could lose network connectivity. For this reason, we must expect exceptions from such code. This example has done nothing to detect exceptions, and yet the `IObservable<string>` that this `ReadFileLines` method returns will in fact report any exceptions that occur. This is because the `Create` method will catch any exception that emerges from our callback and report it with `OnError`. (If our code already called `OnComplete` on the observer, Rx won't call `OnError` because that would violate the rules. Instead it will silently drop the exception, so it's best not to attempt to do any work after you call `OnCompleted`.) 

This automatic exception delivery is another example of why the `Create` factory method is the preferred way to implement custom observable sequences. It is almost always a better option than creating custom types that implement the `IObservable<T>` interface. This is not just because it saves you some time. It's also that Rx tackles the intricacies that you may not think of such as thread safety of notifications and disposal of subscriptions.

The `Create` method entails lazy evaluation, which is a very important part of Rx. It opens doors to other powerful features such as scheduling and combination of sequences that we will see later. The delegate will only be invoked when a subscription is made. So in the `ReadFileLines` example, it won't attempt to open the file until you subscribe to the `IObservable<string>` that is returned. If you subscribe multiple times, it will execute the callback each time. (So if the file has changed, you can retrieve the latest contents by calling `Subscribe` again.)

As an exercise, try to build the `Empty`, `Return`, `Never` &amp; `Throw` extension methods yourself using the `Create` method. If you have Visual Studio or [LINQPad](http://www.linqpad.net/) available to you right now, code it up as quickly as you can. If you don't (perhaps you are on the train on the way to work), try to conceptualize how you would solve this problem.

You completed that last step before moving onto this paragraph, right? Because you can now compare your versions with these examples of `Empty`, `Return`, `Never` and `Throw` recreated with `Observable.Create`:

```csharp
public static IObservable<T> Empty<T>()
{
    return Observable.Create<T>(o =>
    {
        o.OnCompleted();
        return Disposable.Empty;
    });
}

public static IObservable<T> Return<T>(T value)
{
    return Observable.Create<T>(o =>
    {
        o.OnNext(value);
        o.OnCompleted();
        return Disposable.Empty;
    });
}

public static IObservable<T> Never<T>()
{
    return Observable.Create<T>(o =>
    {
        return Disposable.Empty;
    });
}

public static IObservable<T> Throws<T>(Exception exception)
{
    return Observable.Create<T>(o =>
    {
        o.OnError(exception);
        return Disposable.Empty;
    });
}
```

You can see that `Observable.Create` provides the power to build our own factory methods if we wish.


## Sequence Generators

The creation methods we've looked at so far are straightforward in that they either produce very simple sequences (such as single-element, or empty sequences), or they rely on our code to tell them exactly what to produce. Now we'll look at some methods that can produce longer sequences.

### Observable.Range

`Observable.Range(int, int)` returns an `IObserable<int>` that produces a range of integers. The first integer is the initial value and the second is the number of values to yield. This example will write the values '10' through to '24' and then complete.

```csharp
var range = Observable.Range(10, 15);
range.Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
```

### Observable.Generate

Suppose you wanted to emulate the `Range` factory method using `Observable.Create`. You might try this:

```cs
// Not the best way to do it!
IObservable<int> Range(int start, int count) =>
    Observable.Create<int>(observer =>
        {
            for (int i = 0; i < count; ++i)
            {
                observer.OnNext(start + i);
            }

            return Disposable.Empty;
        });
```

This will work, but it does not respect request to unsubscribe. That won't cause direct harm, because Rx detects unsubscription, and will simply ignore any further values we produce. However, it's a waste of CPU time (and therefore energy, with consequent battery lifetime and/or environmental impact) to carry on generating numbers after nobody is listening. How bad that is depends on how long a range was requested. But imagine you wanted an infinite sequence? Perhaps it's useful to you to have an `IObservable<BigInteger>` that produces value from the Fibonacci sequence, or prime numbers. How would you write that with `Create`? You'd certainly want some means of handling unsubscription in that case. We need our callback to return if we're to be notified of unsubscription (or we need to supply an `async` method, which doesn't really seem suitable here).

There's a different approach that can work better here: `Observable.Generate`. The simple version of `Observable.Generate` takes the following parameters:

- an initial state
- a predicate that defines when the sequence should terminate
- a function to apply to the current state to produce the next state
- a function to transform the state to the desired output

```csharp
public static IObservable<TResult> Generate<TState, TResult>(
    TState initialState, 
    Func<TState, bool> condition, 
    Func<TState, TState> iterate, 
    Func<TState, TResult> resultSelector)
```

This shows how you could use `Observable.Generate` to construct a `Range` method:

```csharp
// Example code only
public static IObservable<int> Range(int start, int count)
{
    int max = start + count;
    return Observable.Generate(
        start, 
        value => value < max, 
        value => value + 1, 
        value => value);
}
```

The `Generate` method calls us back repeatedly until either our `condition` callback says we're done, or the observer unsubscribes. We can define an infinite sequence simply by never saying we are done:

```
IObservable<BigInteger> Fibonacci()
{
    return Observable.Generate(
        (v1: new BigInteger(1), v2: new BigInteger(1)),
        value => true, // It never ends!
        value => (value.v2, value.v1 + value.v2),
        value => value.v1); ;
}
```

## Timed Sequence Generators

Most of the methods we've looked at so far have returned sequences that produce all of their values immediately. (The only exception is where we called `Observable.Create` and produced values when we were ready to.) However, Rx is able to generate sequences on a schedule.

As we'll see, operators that schedule their work do so through an abstraction called a _scheduler_. If you don't specify one, they will pick a default scheduler, but sometimes the timer mechanism is significant. For example, there are timers that integrate with UI frameworks, delivering notifications on the same thread that mouse clicks and other input are delivered on, and we might want Rx's time-based operators to use these. For testing purposes it can be useful to virtualize timings, so we can verify what happens in timing-sensitive code without necessarily waiting for tests to execute in real time.

Schedulers are a complex subject that is out of scope for this chapter, but they are covered in detail in the later chapter on [Scheduling and threading](15_SchedulingAndThreading.html).

There are three ways of producing timed events.


### Observable.Interval

The first is `Observable.Interval(TimeSpan)` which will publish incremental values starting from zero, based on a frequency of your choosing. 

This example publishes values every 250 milliseconds.

```csharp
IObservable<long> interval = Observable.Interval(TimeSpan.FromMilliseconds(250));
interval.Subscribe(
    Console.WriteLine, 
    () => Console.WriteLine("completed"));
```

Output:

```
0
1
2
3
4
5
```

Once subscribed, you must dispose of your subscription to stop the sequence, because `Interval` returns an infinite sequence. Rx presumes that you might have considerable patience, because the sequences returned by `Interval` are of type `IObservable<long>` (`long`, not `int`) meaning you won't hit problems if you produce more than a paltry 2.1475 billion event (i.e. more than `int.MaxValue`).


### Observable.Timer

The second factory method for producing constant time based sequences is `Observable.Timer`. It has several overloads; the first of which we will look at being very simple. The most basic overload of `Observable.Timer` takes just a `TimeSpan` as `Observable.Interval` does. The `Observable.Timer` will however only publish one value (0) after the period of time has elapsed, and then it will complete.

```cs
var timer = Observable.Timer(TimeSpan.FromSeconds(1));
timer.Subscribe(
    Console.WriteLine, 
    () => Console.WriteLine("completed"));
```

Output:

```
0
completed
```

Alternatively, you can provide a `DateTimeOffset` for the `dueTime` parameter. This will produce the value 0 and complete at the specified time.

A further set of overloads adds a `TimeSpan` that indicates the period to produce subsequent values. This now allows us to produce infinite sequences. It also shows how `Observable.Interval` is really just a special case of `Observable.Timer`—`Interval` could be implemented like this:

```csharp
public static IObservable<long> Interval(TimeSpan period)
{
    return Observable.Timer(period, period);
}
```

While `Observable.Interval` would always wait the given period before producing the first value, this `Observable.Timer` overload gives the ability to start the sequence when you choose. With `Observable.Timer` you can write the following to have an interval sequence that starts immediately.

```csharp
Observable.Timer(TimeSpan.Zero, period);
```

This takes us to our third way and most general way for producing timer related sequences, back to `Observable.Generate`.

### Timed Observable.Generate

 There's a more complex overload of `Observable.Generate` that allows you to provide a function that specifies the due time for the next value.

```csharp
public static IObservable<TResult> Generate<TState, TResult>(
    TState initialState, 
    Func<TState, bool> condition, 
    Func<TState, TState> iterate, 
    Func<TState, TResult> resultSelector, 
    Func<TState, TimeSpan> timeSelector)
```

Using this overload, and specifically the extra `timeSelector` argument, we can produce our own implementation of `Observable.Timer` (and as you've already seen, this in turn would enable us to write our own `Observable.Interval`).

```csharp
public static IObservable<long> Timer(TimeSpan dueTime)
{
    return Observable.Generate(
        0l,
        i => i < 1,
        i => i + 1,
        i => i,
        i => dueTime);
}

public static IObservable<long> Timer(TimeSpan dueTime, TimeSpan period)
{
    return Observable.Generate(
        0l,
        i => true,
        i => i + 1,
        i => i,
        i => i == 0 ? dueTime : period);
}

public static IObservable<long> Interval(TimeSpan period)
{
    return Observable.Generate(
        0l,
        i => true,
        i => i + 1,
        i => i,
        i => period);
}
```

This shows how you can use `Observable.Generate` to produce infinite sequences. I will leave it up to you the reader, as an exercise using `Observable.Generate`, to produce values at variable rates. I find using these methods invaluable not only in day to day work but especially for producing dummy data for test purposes.


## Adapting Common Type to IObservable&lt;T&gt;

Although we've now seen two very general ways to produce arbitrary sequences—`Create` and `Generate`—what if you already have an existing source of information in some other form that you'd like to make available as an `IObservable<T>`? Rx provides a few adapters for common source types.

### From delegates

The `Observable.Start` method allows you to turn a long running `Func<T>` or `Action` into a single value observable sequence. By default, the processing will be done asynchronously on a ThreadPool thread. If the overload you use is a `Func<T>` then the return type will be `IObservable<T>`. When the function returns its value, that value will be published and then the sequence completed. If you use the overload that takes an `Action`, then the returned sequence will be of type `IObservable<Unit>`. The `Unit` type is represents the absence of information, so it's somewhat analogous to `void`, except you can have an instance of the `Unit` type. It's particularly useful in Rx because we often care only about when something has happened, and there might not be any information besides timing. In these cases, we often use an `IObservable<Unit>` so that it's possible to produce definite events even though there's no meaningful data in them. (The name comes from the world of functional programming, where this kind of construct is used a lot.) In this case `Unit` is used to publish an acknowledgement that the `Action` is complete, because an `Action` does not return any information. The `Unit` type itself has no value; it just serves as an empty payload for the `OnNext` notification. Below is an example of using both overloads.

```csharp
static void StartAction()
{
    var start = Observable.Start(() =>
        {
            Console.Write("Working away");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(100);
                Console.Write(".");
            }
        });

    start.Subscribe(
        unit => Console.WriteLine("Unit published"), 
        () => Console.WriteLine("Action completed"));
}

static void StartFunc()
{
    var start = Observable.Start(() =>
    {
        Console.Write("Working away");
        for (int i = 0; i < 10; i++)
        {
            Thread.Sleep(100);
            Console.Write(".");
        }
        return "Published value";
    });

    start.Subscribe(
        Console.WriteLine, 
        () => Console.WriteLine("Action completed"));
}
```

Note the difference between `Observable.Start` and `Observable.Return`. `Start` invokes our callback only upon subscription, so it is an example of a 'lazy' operation. Conversely, `Return` requires us to supply the value up front.

The observable returned by `Start` may seem to have a superficial resemblance to `Task` or `Task<T>` (depending on whether you use the `Action` or `Func<T>` overload). Each represents work that may take some time before eventually completing, perhaps producing a result. However, there's a significant difference: `Start` doesn't begin the work until you subscribe to it. And it will re-execute the callback every time you subscribe to it. So it is more like a factory for a task-like entity.


### From events

As we discussed early in the book, .NET has a model for events that is baked into its type system. This predates Rx (not least because Rx wasn't feasible until .NET got generics in .NET 2.0) so it's common for types to support events but not Rx. To be able to integrate with the existing event model, Rx provides methods to take an event and turn it into an observable sequence. I showed this briefly in the file system watcher example earlier, but let's look in a bit more detail. There are several different varieties you can use. This show the most succinct form:

```cs
FileSystemWatcher watcher = new (@"c:\temp");
IObservable<EventPattern<FileSystemEventArgs>> changeEvents = Observable
    .FromEventPattern<FileSystemEventArgs>(watcher, nameof(watcher.Changed));
```

If you have an object that provides an event you can use this overload of `FromEventPattern`, passing in the object and the name of the event that you'd like to use with Rx. There are a few problems with this.

Firstly, why do I need to pass the event name as a string? Identifying members with strings is an error-prone technique—the compiler won't notice if there's a mismatch between the first and second argument (e.g., if I passed the arguments `(somethingElse, nameof(watcher.Changed))` by mistake). Couldn't I just pass `watcher.Changed` itself? Unfortunately not—this is an example of the issue I mentioned in the first chapter: .NET events are not first class citizens. We can't use them in the way we can use other objects or values. For example, we can't pass an event as an argument to a method. In fact the only thing you can do with a .NET event is attach and remove event handlers. If I want to get some other method to attach handlers to the event of my choosing (e.g., here I want Rx to handle the events), then the only way to do that is to specify the event's name so that the method (`FromEventPattern`) can then use reflection to attach its own handlers.

This is a problem for some deployment scenarios. It is increasingly common in .NET to do extra work at build time to optimize runtime behaviour, and reliance on reflection can compromise these techniques. For example, instead of relying on Just In Time (JIT) compilation of code, we might use Ahead of time (AoT) mechanisms. .NET's Ready to Run (R2R) system enables you to include pre-compiled code targeting specific CPU types alongside the normal IL, avoiding having to wait for .NET to compile the IL into runnable code. This can have a significant effect on startup times, making it an important technique both in client side applications, where it can fix problems where applications are sluggish when they first start up, and also in server-side applications, especially in cloud environments where code may be moved from one compute node to another fairly frequently, making it important to minimize cold start costs. There are also scenarios where JIT compilation is not even an option, in which case AoT compilation isn't simply an optimization: it's the only means by which code can run at all.

The problem with reflection is that it makes it difficult for the build tools to work out what code will execute at runtime. When they inspect this call to `FromEventPattern` they will just see arguments of type `object` and `string`. It's not self-evident that this is going to result in reflection-driven calls to the `add` and `remove` methods for `FileSystemWatcher.Changed` at runtime. There are attributes that can be used to provide hints, but there are limits to how well these can work. Sometimes the build tools will be unable to determine what code would need to be AoT compiled to enable this method to execute without relying on runtime JIT.

There's another, related problem. The .NET build tools support a feature called 'trimming', in which they remove unused code. The `System.Reactive.dll` file is about 1.3MB in size, but it would be a very unusual application that used every member of every type in that component. Basic use of Rx might need only a few tens of kilobytes. The idea with trimming is to work out which bits are actually in use, and produce a copy of the DLL that contains only that code. This can dramatically reduce the volume of code that needs to be deployed for an executable to run. This can be especially important in client-side Blazor applications, where .NET components end up being downloaded by the browser. Having to download an entire 1.3MB component might make you think twice about using it. But if trimming means that basic usage requires only a few tens of KB, and that the size would increase only if you were making more extensive use of the component, that can make it reasonable to use a component that would, without trimming, have imposed too large a penalty to justify its inclusion. But as with AoT compilation, trimming can only work if the tools can determine which code is in use. If they can't do that, it's not just a case of falling back to a slower path, waiting while the relevant code gets JIT compiler—if code has been trimmed, it will be unavailable at runtime, and your application might crash with a `MissingMethodException`.

So reflection-based APIs can be problematic if you're using any of these techniques. Fortunately, there's an alternative. We can use an overload that takes a couple of delegates, and Rx will invoke these when it wants to add or remove handlers for the event:

```cs
IObservable<EventPattern<FileSystemEventArgs>> changeEvents = Observable
    .FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
        h => watcher.Changed += h,
        h => watcher.Changed -= h);
```

This is code that AoT and trimming tools can understand easily. We've written methods that explicitly add and remove handlers for the `FileSystemWatcher.Changed` event, so AoT tools can pre-compile those two methods, and trimming tools know that they cannot remove the add and remove handlers for those events.

The downside is that this is a pretty cumbersome bit of code to write. If you've not already bought into the idea of using Rx, this might well be enough to make you think "I'll just stick with ordinary .NET events, thanks. But the cumbersome nature is a symptom of what is wrong with .NET events. We wouldn't have had to write anything so ugly if events had been first class citizens in the first place.

Not only has that second-class status meant we couldn't just pass the event itself as an argument, it has also meant that we've had to state type arguments explicitly. The relationship between an event's delegate type (`FileSystemEventHandler` in this example) and its event argument type (`FileSystemEventArgs` here) is, in general, not something that C#'s type inference can determine automatically, which is why we've had to specify both types explicitly. (Events that use the generic `EventHandler<T>` type are more amenable to type inference, and can use a slightly less verbose version of `FromEventPattern`. Unfortunately, relatively few events actually use that. Some events provide information besides the fact that something just happened, and use the base `EventHandler` type, and for those kinds of events, you can in fact omit the type arguments completely, making the code slightly less ugly. You still need to provide the add and remove callbacks though.)

Notice that the return type of `FromEventPattern` in this example is `IObservable<EventPattern<FileSystemEventArgs>>`. The `EventPattern<T>` type encapsulates the information that the event passes to handlers. Most .NET events follow a common pattern in which handler methods take two arguments: an `object sender`, which just tells you which object raised the event (useful if you attach one event handler to multiple objects) and then a second argument of some type derived from `EventArgs` that provides information about the event. `EventPattern<T>` just packages these two arguments into a single object that offers `Sender` and `EventArgs` properties. In cases where you don't in fact want to attach one handler to multiple sources, you only really need that `EventArgs` property, which is why the earlier `FileSystemWatcher` examples went on to extract that just that, to get a simpler result of type `IObservable<FileSystemEventArgs>`. It did this with the `Select` operator, which we'll get to in more detail later:

```cs
IObservable<FileSystemEventArgs> changes = changeEvents.Select(ep => ep.EventArgs);
```

It is very common to want to expose property changed events as observable sequences. The .NET runtime libraries define a .NET-event-based interface for advertising property changes, `INotifyPropertyChanged`, and some user interface frameworks have more specialized systems for this, such as WPF's `DependencyProperty`. If you are contemplating writing your own wrappers to do this sort of thing, I would strongly suggest looking at the [Reactive UI libraries](https://github.com/reactiveui/ReactiveUI/) first. It has a set of [features for wrapping properties as `IObservable<T>`](https://www.reactiveui.net/docs/handbook/when-any/).

### From Task

The `Task` and `Task<T>` types are very widely used in .NET. Widely used .NET languages have built-in support for working with them (e.g., C#'s `async` and `await` keywords). There's some conceptual overlap between tasks an `IObservable<T>`: both represent some sort of work that might take a while to complete. There is a sense in which an `IObservable<T>` is a generalization of a `Task<T>`—both represent potentially long-running work, but an `IObservable<T>` can produce multiple results whereas `Task<T>` can produce just one.

Since `IObservable<T>` is the more general abstraction, we should be able to represent a `Task<T>` as an `IObservable<T>`. And Rx defines various extension methods for `Task` and `Task<T>` to do this. These methods are all called `ToObservable()`, and it offers various overloads offering control of the details where required, and simplicity for the most common scenarios.

Although they are conceptually similar, `Task<T>` does a few things differently in the details. For example, you can retrieve its [`Status` property](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.status), which might report that it is in a cancelled or faulted state. `IObservable<T>` doesn't provide a way to ask a source for its state; it just tells you things. So `ToObservable` makes some decisions about how to present status in a way that makes makes sense in an Rx world:

- if the task is [Cancelled](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-canceled), `IObservable<T>` invoke a subscriber's `OnError` passing a `TaskCanceledException`
- if the task is [Faulted](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-faulted) then the sequence will error with the task's inner exception
- if the task is not yet in a final state (neither [Cancelled](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-canceled), [Faulted](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-faulted), or [RanToCompletion](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskstatus#system-threading-tasks-taskstatus-rantocompletion)), the `IObservable<T>` will not produce any notifications until such time as the task does enter one of these final states

It does not matter whether the task is already in a final state at the moment that you call `ToObservable`. If it has finished, `ToObservable` will just return a sequence representing that state. (In fact, it uses either the `Return` or `Throw` creation methods you saw earlier.) If the task has not yet finished, `ToObservable` will attach a continuation to the task to detect the outcome once it does complete.

Tasks come in two forms: `Task<T>`, which produces a result, and `Task`, which does not. But in Rx, there is only `IObservable<T>`—there isn't a no-result form. We've already seen this problem once before, when the `Observable.Start` method needed to be able to [adapt a delegate as an `IObservable<T>`](#from-delegates) even when the delegate was an `Action` that produced no result. The solution was to return an `IObservable<Unit>`, and that's also exactly what you get when you call `ToObservable` on a `Task`.

The extension method is simple to use:

```csharp
var t = Task.Factory.StartNew(() => "Test");
var source = t.ToObservable();
source.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("completed"));
```

Output:

```
Test
completed
```

### From IEnumerable&lt;T&gt;

Rx defines another extension method called `ToObservable`, this time for `IEnumerable<T>`. In earlier chapters I described how `IObserable<T>` was designed to represent the same basic abstraction as `IEnumerable<T>`, with the only difference being the mechanism we use to obtain the elements in the sequence: with `IEnumerable<T>`, we write code that _pulls_ values out of the collection (e.g., a `foreach` loop), whereas `IObservable<T>` _pushes_ values to us by invoking `OnNext` on our `IObserver<T>`.

We could write code that bridges from _pull_ to _push_:

```csharp
// Example code only - do not use!
public static IObservable<T> ToObservableOversimplified<T>(this IEnumerable<T> source)
{
    return Observable.Create<T>(o =>
    {
        foreach (var item in source)
        {
            o.OnNext(item);
        }

        o.OnComplete();

        // Incorrectly ignoring unsubscription.
        return Disposable.Empty;
    });
}
```

This crude implementation conveys the basic idea, but it is naive. It does not attempt to handle unsubscription, and it's not easy to fix that when using `Observable.Create` for this particular scenario. And as we will see later in the book, it does not have a very nice concurrency model. The implementation that Rx supplies does of course cater for all of these tricky details. That makes it rather more complex, but that's Rx's problem; you can think of it as being logically equivalent to the code shown above, but without the shortcomings.

When transitioning from `IEnumerable<T>` to `IObservable<T>`, you should carefully consider what you are really trying to achieve. Consider that the blocking synchronous (pull) nature of `IEnumerable<T>` does always not mix well with the asynchronous (push) nature of `IObservable<T>`. As soon as something subscribes to an `IObservable<T>` created in this way, it is effectively asking to iterate over the `IEnumerable<T>`, immediately producing all of the values. The call to `Subscribe` might not return until it has reached the end of the `IEnumerable<T>`, making it similar to the very simple example shown [at the start of this chapter](#a-very-basic-iobservablet-implementation). (I say "might" because as we'll see when we get to schedulers, the exact behaviour depends on the context.) `ToObservable` can't work magic—something somewhere has to execute what amounts to a `foreach` loop.

So although this can be a convenient way to bring sequences of data into an Rx world, you should carefully test and measure the performance impact.


### From APM

Rx provides support for the ancient [.NET Asynchronous Programming Model (APM)](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/asynchronous-programming-model-apm). Back in .NET 1.0, this was the only pattern for representing asynchronous operations. It was superseded in 2010 when .NET 4.0 introduced the [Task-based Asynchronous Pattern (TAP)](https://learn.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap). The old APM offers no benefits over the TAP. Moreover, C#'s `async` and `await` keywords (and equivalents in other .NET languages) only support the TAP, meaning that the APM is best avoided. However, the TAP was fairly new back in 2011 when Rx 1.0 was released, so it offered adapters for presenting an APM implementation as an `IObservable<T>`.


Nobody should be using the APM today, but for completeness (and just in case you have to use an ancient library that only offers the APM) I will provide a very brief explanation.

The result of the call to `Observable.FromAsyncPattern` does _not_ return an observable sequence. It returns a delegate that returns an observable sequence. (So it is essentially a factory factory) The signature for this delegate will match the generic arguments of the call to `FromAsyncPattern`, except that the return type will be wrapped in an observable sequence. The following example wraps the `Stream` class's `BeginRead`/`EndRead` methods (which are an implementation of the APM).

**Note**: this is purely to illustrate how to wrap the APM. You would never do this in practice because `Stream` has supported the TAP for years.

```csharp
Stream stream = GetStreamFromSomewhere();
var fileLength = (int) stream.Length;

Func<byte[], int, int, IObservable<int>> read = Observable.FromAsyncPattern<byte[], int, int, int>(
    stream.BeginRead, 
    stream.EndRead);
var buffer = new byte[fileLength];
IObservable<int> bytesReadStream = read(buffer, 0, fileLength);
bytesReadStream.Subscribe(byteCount =>
{
    Console.WriteLine("Number of bytes read={0}, buffer should be populated with data now.", byteCount);
});
```

## Subjects

So far, this chapter has explored various factory methods that return `IObservable<T>` implementations. There is another way though: `System.Reactive` defines various types that implement `IObservable<T>` that we can instantiate directly. But how do we determine what values these types produce? We're able to do that because they also implement `IObserver<T>`.

Types that implement both `IObservable<T>` and `IObserver<T>` are called _subjects_ in Rx. There's an an `ISubject<T>` to represent this. (This is in the `System.Reactive` NuGet package, unlike `IObservable<T>` and `IObserver<T>`, which are both built into the .NET runtime libraries.) `ISubject<T>` looks like this:

```cs
public interface ISubject<T> : ISubject<T, T>
{
}
```

So it turns out there's also a two-argument `ISubject<TSource, TResult>` to accommodate the fact that something that is both an observer and an observable might transform the data that flows through it in some way, meaning that the input and output types are not necessarily the same. Here's the two-type-argument definition:

```cs
public interface ISubject<in TSource, out TResult> : IObserver<TSource>, IObservable<TResult>
{
}
```

As you can see the `ISubject` interfaces don't define any members of their own. They just inherit from `IObserver<T>` and `IObservable<T>`—these interfaces are nothing more than a direct expression of the fact that a subject is both an observer and an observable.

But what is this for? You can think of the `IObserver<T>` and the `IObservable<T>` as the 'reader' and 'writer' or, 'consumer' and 'publisher' interfaces respectively. A subject, then is both a reader and a writer, a consumer and a publisher. Data flows both into and out of a subject.

Rx offers a few subject implementations that can occasionally be useful in code that wants to make an `IObservable<T>` available. Although `Observable.Create` is usually the preferred way to do this, there's one important case where a subject might make more sense: if you have some code that discovers events of interest (e.g., by using the client API for some messaging technology) and wants to make them available through an `IObservable<T>`, subjects can sometimes provide a more convenient way to to this than wither `Observable.Create` or a custom implementation.

Rx offers a few subject types. We'll start with the most straightforward one to understand.

### Subject<T>

The `Subject<T>` type immediately forwards any calls to `IObserver<T>` methods on to all of the observers currently subscribed to it. This example shows its basic operation:

```cs
Subject<int> s = new();
s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));

s.OnNext(1);
s.OnNext(2);
s.OnNext(3);
```

I've created a `Subject<int>`. I've subscribed to it twice, and then called its `OnNext` method repeatedly. This produces the following output, illustrating that the `Subject<int>` forwards each `OnNext` call onto both subscribers:

```
Sub1: 1
Sub2: 1
Sub1: 2
Sub2: 2
Sub1: 3
Sub2: 3
```

We could use this as a way to bridge between some API from which we receive data into the world of Rx. You could imagine writing something of this kind:

```cs
public class MessageQueueToRx : IDisposable
{
    private readonly Subject<string> messages = new();

    public IObservable<string> Messages => messages;

    public void Run()
    {
        while (true)
        {
            // Receive a message from some hypothetical message queuing service
            string message = MqLibrary.ReceiveMessage();
            messages.OnNext(message);
        }
    }

    public void Dispose()
    {
        message.Dispose();
    }
}
```

It wouldn't be too hard to modify this to use `Observable.Create` instead. But where this approach can become easier is if you need to provide multiple different `IObservable<T>` sources. Imagine we distinguish between different message types based on their content, and publish them through different observables. That's hard to arrange with `Observable.Create` if we still want a single loop pulling messages off the queue.

`Subject<T>` also distributes calls to either `OnCompleted` or `OnError` to all subscribers. Of course, the rules of Rx require that once you have called either of these methods on an `IObserver<T>` (and any `ISubject<T>` is an `IObserver<T>`, so this rule applies to `Subject<T>`) you must not call `OnNext`, `OnError`, or `OnComplete` on that observer ever again. In fact, `Subject<T>` will tolerate calls that break this rule—it just ignores them, so even if your code doesn't quite stick to these rules internally, the `IObservable<T>` you present to the outside world will behave correctly, because Rx enforces this.

`Subject<T>` implements `IDisposable`. Disposing a `Subject<T>` puts it into a state where it will throw an exception if you call any of its methods. The documentation also describes it as unsubscribing all observers, but since a disposed `Subject<T>` isn't capable of producing any further notifications in any case, this doesn't really mean much. (Note that it does _not_ call `OnCompleted` on its observers when you `Dispose` it.) The one practical effect is that its internal field that keeps track of observers is reset to a special sentinel value indicating that it has been disposed, meaning that the one externally observable effect of "unsubscribing" the observers is that if, for some reason, your code held onto a reference to a `Subject<T>` after disposing it, that would no longer keep all the subscribers reachable for GC purposes. If a `Subject<T>` remains reachable indefinitely after it is no longer in use, that in itself is effectively a memory leak, but disposal would at least limit the effects: only the `Subject<T>` itself would remain reachable, and not all of its subscribers.

`Subject<T>` is the most straightforward subject, but there are other, more specialized ones.

## ReplaySubject<T>

`Subject<T>` does not remember anything: it immediately distributes incoming values to subscribers. If new subscribers come along, they will only see events that occur after they subscribe. `ReplaySubject<T>`, on the other hand, can remember every value it has ever seen. If a new subject comes along, it will receive the complete history of events so far.

This is a variation on the first example in the preceding [Subject<T> section](#subject). It creates a `ReplaySubject<int>` instead of a `Subject<int>`. And instead of immediately subscribing twice, it creates an initial subscription, and then a second one only after a couple of values have been emitted.

```cs
ReplaySubject<int> s = new();
s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));

s.OnNext(1);
s.OnNext(2);

s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));

s.OnNext(3);
```

This produces the following output:

```
Sub1: 1
Sub1: 2
Sub2: 1
Sub2: 2
Sub1: 3
Sub2: 3
```

As you'd expect, we initially see output only from `Sub1`. But when we make the second call to subscribe, we can see that `Sub2` also received the first two values. And then when we report the third value, both see it. If this example had used `Subject<int>` instead, we would have seen just this output:

```
Sub1: 1
Sub1: 2
Sub1: 3
Sub2: 3
```

There's an obvious potential problem here: if `ReplaySubject<T>` remembers every value published to it, we mustn't use with endless event sources, because it will eventually cause us to run out of memory.

`ReplaySubject<T>` offers constructors that accept simple cache expiry settings that can limit memory consumption. One option is to specify the maximum number of item to remember. This next example creates a `ReplaySubject<T>` with a buffer size of 2:

```csharp    
ReplaySubject<int> s = new(2);
s.Subscribe(x => Console.WriteLine($"Sub1: {x}"));

s.OnNext(1);
s.OnNext(2);
s.OnNext(3);

s.Subscribe(x => Console.WriteLine($"Sub2: {x}"));

s.OnNext(4);
```

Since the second subscription only comes along after we've already produced 3 values, it no longer sees all of them—it only receives the last two values published prior to subscription (but the first subscription continues to see everything of course):

```
Sub1: 1
Sub1: 2
Sub1: 3
Sub2: 2
Sub2: 3
Sub1: 4
Sub2: 4
```

Alternatively, you can specify a time-based limit by passing a `TimeSpan to the `ReplaySubject<T>` constructor.


## BehaviorSubject<T>

Like `ReplaySubject<T>`, `BehaviorSubject<T>` also has a memory, but it remembers exactly one value. However, it's not quite the same as a `ReplaySubject<T>` with a buffer size of 1, because a `ReplaySubject<T>` starts off in a state where it has nothing in its memory. But `BehaviorSubject<T>` always remembers _exactly_ one item. How can that work before we've made our first call to `OnNext`? `BehaviorSubject<T>` enforces this by requiring us to supply the initial value when we construct it.

So you can think of `BehaviorSubject<T>` as a subject that _always_ has a value available. If you subscribe to a `BehaviorSubject<T>` it will instantly produce a single value. (It may then go on to produce more values, but it always produces one right away.) As it happens, it also makes that value available through a property called `Value`, so you don't need to subscribe an `IObserver<T>` to it just to retrieve the value.

A `BehaviorSubject<T>` could be thought of an as observable property. Like a normal property, it can immediately supply a value whenever you ask it. The difference is that it can then go on to notify you every time its value changes.

This analogy falls down slightly when it comes to completion. If you call `OnCompleted`, it immediately calls `OnCompleted` on all of its observers, and if any new observers subscribe, they will also immediately be completed—it does not first supply the last value. (So this is another way in which it is different from a `ReplaySubject<T>` with a buffer size of 1.)

Simlarly, if you call `OnError`, all current observers will receive an `OnError` call, and any subsequent subscribers will also receive nothing but an `OnError` call.

backing fields to properties.


## AsyncSubject<T>

`AsyncSubject<T>` provides all observers with the final value it receives. Since it can't know which is the final value until `OnCompleted` is called, it will not invoke any methods on any of its subscribers until either its `OnCompleted` or `OnError` method is called. (If `OnError` is called, it just forwards that to all current and future subscribers.)

If no calls were made to `OnNext` before `OnCompleted` then there was no final value, so it will just complete any observers without providing a value.

In this example no values will be published as the sequence never completes. 
No values will be written to the console.

```csharp
AsyncSubject<string> subject = new();
subject.OnNext("a");
subject.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
subject.OnNext("b");
subject.OnNext("c");
```

In this example we invoke the `OnCompleted` method so there will be a final value ('c') for the subject to produce:

```csharp
AsyncSubject<string> subject = new();

subject.OnNext("a");
subject.Subscribe(x => Console.WriteLine($"Sub1: {x}"));
subject.OnNext("b");
subject.OnNext("c");
subject.OnCompleted();
subject.Subscribe(x => Console.WriteLine($"Sub2: {x}"));
```

This produces the following output:

```
Sub1: c
Sub2: c
```



## Subject factory

Finally it is worth making you aware that you can also create a subject via a factory method. Considering that a subject combines the `IObservable<T>` and `IObserver<T>` interfaces, it seems sensible that there should be a factory that allows you to combine them yourself. The `Subject.Create(IObserver<TSource>, IObservable<TResult>)` factory method provides just this.

```csharp
//Creates a subject from the specified observer used to publish messages to the subject
//  and observable used to subscribe to messages sent from the subject
public static ISubject>TSource, TResult< Create>TSource, TResult<(
    IObserver<TSource> observer, 
    IObservable<TResult> observable)
{...}
```

Subjects provide a convenient way to poke around Rx, and are occasionally useful in production scenarios, but they are not recommended for most cases. An explanation is in the [Usage Guidelines](18_UsageGuidelines.md) in the appendix. Instead of using subjects, favour the factory methods  shown earlier in this chapter..


## Summary

We have looked at the various eager and lazy ways to create a sequence. We have seen how to produce timer based sequences using the various factory methods. And we've also explored ways to transition from other synchronous and asynchronous representations. 

As a quick recap:

- Factory Methods
  - Observable.Return
  - Observable.Empty
  - Observable.Never
  - Observable.Throw
  - Observable.Create

- Generative methods
  - Observable.Range
  - Observable.Interval
  - Observable.Timer
  - Observable.Generate

- Adaptation
  - Observable.Start
  - Observable.FromEventPattern
  - Task.ToObservable
  - Task&lt;T&gt;.ToObservable
  - IEnumerable&lt;T&gt;.ToObservable
  - Observable.FromAsyncPattern

Creating an observable sequence is our first step to practical application of Rx: create the sequence and then expose it for consumption. Now that we have a firm grasp on how to create an observable sequence, we can look in more detail at the operators that allow us to describe processing to be applied, to build up more complex observable sequences.