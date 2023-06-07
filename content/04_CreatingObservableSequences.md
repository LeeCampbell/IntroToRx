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

As we discussed early in the book, .NET has a model for events that is baked into its type system. This predates Rx (not least because Rx wasn't feasible until .NET got generics in .NET 2.0) so it's common for types to support events but not Rx. To be able to integrate with the existing event model, Rx provides methods to take an event and turn it into an observable sequence. I showed this briefly in the file system watcher example earlier, but let's look in a bit more detail. There are several different varieties you can use. 

TODO: done up to here. Two of these next seem to be the same thing just applied arbitrarily to two different events. And none of these looks like a good choice of example in 2023.


Here is a selection of common event patterns.

```csharp
// Activated delegate is EventHandler
var appActivated = Observable.FromEventPattern(
        h => Application.Current.Activated += h,
        h => Application.Current.Activated -= h);

// PropertyChanged is PropertyChangedEventHandler
var propChanged = Observable.FromEventPattern
    <PropertyChangedEventHandler, PropertyChangedEventArgs>(
        handler => handler.Invoke,
        h => this.PropertyChanged += h,
        h => this.PropertyChanged -= h);
        
// FirstChanceException is EventHandler<FirstChanceExceptionEventArgs>
var firstChanceException = Observable.FromEventPattern<FirstChanceExceptionEventArgs>(
        h => AppDomain.CurrentDomain.FirstChanceException += h,
        h => AppDomain.CurrentDomain.FirstChanceException -= h);      
```

So while the overloads can be confusing, they key is to find out what the event's signature is. If the signature is just the base `EventHandler` delegate then you can use the first example. If the delegate is a sub-class of the `EventHandler`, then you need to use the second example and provide the `EventHandler` sub-class and also its specific type of `EventArgs`. Alternatively, if the delegate is the newer generic `EventHandler<TEventArgs>`, then you need to use the third example and just specify what the generic type of the event argument is.

It is very common to want to expose property changed events as observable sequences. These events can be exposed via `INotifyPropertyChanged` interface, a `DependencyProperty` or perhaps by events named appropriately to the Property they are representing. If you are looking at writing your own wrappers to do this sort of thing, I would strongly suggest looking at the Rxx library on [https://github.com/dotnet/reactive](https://github.com/dotnet/reactive) first. Many of these have been catered for in a very elegant fashion.

### From Task

Rx provides a useful, and well named set of overloads for transforming from other existing paradigms to the Observable paradigm. The `ToObservable()` method overloads provide a simple route to make the transition.

As we mentioned earlier, the `AsyncSubject<T>` is similar to a `Task<T>`. They both return you a single value from an asynchronous source. They also both cache the result for any repeated or late requests for the value. The first `ToObservable()` extension method overload we look at is an extension to `Task<T>`. The implementation is simple;

- if the task is already in a status of `RanToCompletion` then the value is added to the sequence and then the sequence completed
- if the task is Cancelled then the sequence will error with a `TaskCanceledException`
- if the task is Faulted then the sequence will error with the task's inner exception
- if the task has not yet completed, then a continuation is added to the task to perform the above actions appropriately

There are two reasons to use the extension method:

- From Framework 4.5, almost all I/O-bound functions return `Task<T>`
- If `Task<T>` is a good fit, it's preferable to use it over `IObservable<T>` - because it communicates single-value result in the type system. 

In other words, a function that returns a single value in the future should return a `Task<T>`, not an `IObservable<T>`. Then if you need to combine it with other observables, use `ToObservable()`.

Usage of the extension method is also simple.

```csharp
var t = Task.Factory.StartNew(()=>"Test");
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

There is also an overload that converts a `Task` (non generic) to an `IObservable<Unit>`.

### From IEnumerable&lt;T&gt;

The final overload of `ToObservable` takes an `IEnumerable<T>`. This is semantically like a helper method for an `Observable.Create` with a `foreach` loop in it.

```csharp
// Example code only
public static IObservable<T> ToObservable<T>(this IEnumerable<T> source)
{
    return Observable.Create<T>(o =>
    {
        foreach (var item in source)
        {
            o.OnNext(item);
        }

        // Incorrect disposal pattern
        return Disposable.Empty;
    });
}
```

This crude implementation however is naive. It does not allow for correct disposal, it does not handle exceptions correctly and as we will see later in the book, it does not have a very nice concurrency model. The version in Rx of course caters for all of these tricky details so you don't need to worry.

When transitioning from `IEnumerable<T>` to `IObservable<T>`, you should carefully consider what you are really trying to achieve. You should also carefully test and measure the performance impacts of your decisions. Consider that the blocking synchronous (pull) nature of `IEnumerable<T>` sometimes just does not mix well with the asynchronous (push) nature of `IObservable<T>`. Remember that it is completely valid to pass `IEnumerable`, `IEnumerable<T>`, arrays or collections as the data type for an observable sequence. If the sequence can be materialized all at once, then you may want to avoid exposing it as an `IEnumerable`. If this seems like a fit for you then also consider passing immutable types like an array or a `ReadOnlyCollection<T>`. We will see the use of `IObservable<IList<T>>` later for operators that provide batching of data.

### From APM

Finally we look at a set of overloads that take you from the [Asynchronous Programming Model](http://msdn.microsoft.com/en-us/magazine/cc163467.aspx) (APM) to an observable sequence. This is the style of programming found in .NET that can be identified with the use of two methods prefixed with `Begin...` and `End...` and the iconic `IAsyncResult` parameter type. This is commonly seen in the I/O APIs.

```csharp
class webrequest
{    
    public webresponse getresponse() 
    {...}

    public iasyncresult begingetresponse(
        asynccallback callback, 
        object state) 
    {...}

    public webresponse endgetresponse(iasyncresult asyncresult) 
    {...}
    ...
}
class stream
{
    public int read(
        byte[] buffer, 
        int offset, 
        int count) 
    {...}

    public iasyncresult beginread(
        byte[] buffer, 
        int offset, 
        int count, 
        asynccallback callback, 
        object state) 
    {...}

    public int endread(iasyncresult asyncresult) 
    {...}
    ...
}
```

> At time of writing .NET 4.5 was still in preview release. Moving forward with .NET 4.5 the APM model will be replaced with `Task` and new `async` and `await` keywords. Rx 2.0 which is also in a beta release will integrate with these features. .NET 4.5 and Rx 2.0 are not in the scope of this book.

APM, or the Async Pattern, has enabled a very powerful, yet clumsy way of for .NET programs to perform long running I/O bound work. If we were to use the synchronous access to IO, e.g. `WebRequest.GetResponse()` or `Stream.Read(...)`, we would be blocking a thread but not performing any work while we waited for the IO. This can be quite wasteful on busy servers performing a lot of concurrent work to hold a thread idle while waiting for I/O to complete. Depending on the implementation, APM can work at the hardware device driver layer and not require any threads while blocking. Information on how to follow the APM model is scarce. Of the documentation you can find it is pretty shaky, however, for more information on APM, see Jeffrey Richter's brilliant book <cite>CLR via C#</cite> or Joe Duffy's comprehensive <cite>Concurrent Programming on Windows</cite>. Most stuff on the internet is blatant plagiary of Richter's examples from his book. An in-depth examination of APM is outside of the scope of this book.

To utilize the Asynchronous Programming Model but avoid its awkward API, we can use the `Observable.FromAsyncPattern` method. Jeffrey van Gogh gives a brilliant walk through of the `Observable.FromAsyncPattern` in [Part 1](http://blogs.msdn.com/b/jeffva/archive/2010/07/23/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading.aspx) of his <cite>Rx on the Server</cite> blog series. While the theory backing the Rx on the Server series is sound, it was written in mid 2010 and targets an old version of Rx.

With 30 overloads of `Observable.FromAsyncPattern` we will look at the general concept so that you can pick the appropriate overload for yourself. First if we look at the normal pattern of APM we will see that the BeginXXX method will take zero or more data arguments followed by an `AsyncCallback` and an `Object`. The BeginXXX method will also return an `IAsyncResult` token.

```csharp
// Standard Begin signature
IAsyncResult BeginXXX(AsyncCallback callback, Object state);

// Standard Begin signature with data
IAsyncResult BeginYYY(string someParam1, AsyncCallback callback, object state);
```

The EndXXX method will accept an `IAsyncResult` which should be the token returned from the BeginXXX method. The EndXXX can also return a value.

```csharp
// Standard EndXXX Signature
void EndXXX(IAsyncResult asyncResult);

// Standard EndXXX Signature with data
int EndYYY(IAsyncResult asyncResult);
```

The generic arguments for the `FromAsyncPattern` method are just the BeginXXX data arguments if any, followed by the EndXXX return type if any. If we apply that to our `Stream.Read(byte[], int, int, AsyncResult, object)` example above we see that we have a `byte[]`, an `int` and another `int` as our data parameters for `BeginRead` method.

```csharp
    // IAsyncResult BeginRead(
    //   byte[] buffer, 
    //   int offset, 
    //   int count, 
    //   AsyncCallback callback, object state) {...}
    Observable.FromAsyncPattern<byte[], int, int ...
```

Now we look at the EndXXX method and see it returns an `int`, which completes the generic signature of our `FromAsyncPattern` call.

```csharp
    // int EndRead(
    // IAsyncResult asyncResult) {...}
    Observable.FromAsyncPattern<byte[], int, int, int>
```

The result of the call to `Observable.FromAsyncPattern` does _not_ return an observable sequence. It returns a delegate that returns an observable sequence. The signature for this delegate will match the generic arguments of the call to `FromAsyncPattern`, except that the return type will be wrapped in an observable sequence.

```csharp
var fileLength = (int) stream.Length;
// read is a Func<byte[], int, int, IObservable<int>>
var read = Observable.FromAsyncPattern<byte[], int, int, int>(
    stream.BeginRead, 
    stream.EndRead);
var buffer = new byte[fileLength];
var bytesReadStream = read(buffer, 0, fileLength);
bytesReadStream.Subscribe(byteCount =>
{
    Console.WriteLine("Number of bytes read={0}, buffer should be populated with data now.", byteCount);
});
```

Note that this implementation is just an example. For a very well designed implementation that is built against the latest version of Rx you should look at the Rxx project on [https://github.com/dotnet/reactive](https://github.com/dotnet/reactive).

This covers the first classification of query operators: creating observable sequences. We have looked at the various eager and lazy ways to create a sequence. We have introduced the concept of corecursion and show how we can use it with the `Generate` method to unfold potentially infinite sequences. We can now produce timer based sequences using the various factory methods. We should also be familiar with ways to transition from other synchronous and asynchronous paradigms and be able to decide when it is or is not appropriate to do so. 

As a quick recap:

- Factory Methods
  - Observable.Return
  - Observable.Empty
  - Observable.Never
  - Observable.Throw
  - Observable.Create

- Unfold methods
  - Observable.Range
  - Observable.Interval
  - Observable.Timer
  - Observable.Generate

- Paradigm Transition
  - Observable.Start
  - Observable.FromEventPattern
  - Task.ToObservable
  - Task&lt;T&gt;.ToObservable
  - IEnumerable&lt;T&gt;.ToObservable
  - Observable.FromAsyncPattern

Creating an observable sequence is our first step to practical application of Rx: create the sequence and then expose it for consumption. Now that we have a firm grasp on how to create an observable sequence, we can discover the operators that allow us to query an observable sequence.



## Subject<T>

TODO: move this into a separate chapter? I'm not convinced all the subject types are really intro stuff. `Subject<T>` is useful, but the rest?
    
I like to think of the `IObserver<T>` and the `IObservable<T>` as the 'reader' and 'writer' or, 'consumer' and 'publisher' interfaces. If you were to create your own implementation of `IObservable<T>` you may find that while you want to publicly expose the IObservable characteristics you still need to be able to publish items to the subscribers, throw errors and notify when the sequence is complete. Why that sounds just like the methods defined in `IObserver<T>`! While it may seem odd to have one type implementing both interfaces, it does make life easy. This is what [subjects](http://msdn.microsoft.com/en-us/library/hh242969(v=VS.103).aspx "Using Rx Subjects - MSDN") can do for you. [`Subject<T>`](http://msdn.microsoft.com/en-us/library/hh229173(v=VS.103).aspx "Subject(Of T) - MSDN") is the most basic of the subjects. Effectively you can expose your `Subject<T>` behind a method that returns `IObservable<T>` but internally you can use the `OnNext`, `OnError` and `OnCompleted` methods to control the sequence.

In this very basic example, I create a subject, subscribe to that subject and then publish values to the sequence (by calling `subject.OnNext(T)`).

```csharp
static void Main(string[] args)
{
    var subject = new Subject<string>();
    WriteSequenceToConsole(subject);

    subject.OnNext("a");
    subject.OnNext("b");
    subject.OnNext("c");
    Console.ReadKey();
}

// Takes an IObservable<string> as its parameter. 
// Subject<string> implements this interface.
static void WriteSequenceToConsole(IObservable<string> sequence)
{
    // The next two lines are equivalent.
    // sequence.Subscribe(value=>Console.WriteLine(value));
    sequence.Subscribe(Console.WriteLine);
}
```

Note that the `WriteSequenceToConsole` method takes an `IObservable<string>` as it only wants access to the subscribe method. Hang on, doesn't the `Subscribe` method need an `IObserver<string>` as an argument? Surely `Console.WriteLine` does not match that interface. Well it doesn't, but the Rx team supply me with an Extension Method to `IObservable<T>` that just takes an [`Action<T>`](http://msdn.microsoft.com/en-us/library/018hxwa8.aspx "Action(Of T) Delegate - MSDN"). The action will be executed every time an item is published. There are [other overloads to the Subscribe extension method](http://msdn.microsoft.com/en-us/library/system.observableextensions(v=VS.103).aspx "ObservableExtensions class - MSDN") that allows you to pass combinations of delegates to be invoked for `OnNext`, `OnCompleted` and `OnError`. This effectively means I don't need to implement `IObserver<T>`. Cool.

As you can see, `Subject<T>` could be quite useful for getting started in Rx programming. `Subject<T>` however, is a basic implementation. There are three siblings to `Subject<T>` that offer subtly different implementations which can drastically change the way your program runs.

<!--
    TODO: ReplaySubject<T> - Rewrite second sentence. -GA
-->

## ReplaySubject<T>

[`ReplaySubject<T>`](http://msdn.microsoft.com/en-us/library/hh211810(v=VS.103).aspx "ReplaySubject(Of T) - MSDN") provides the feature of caching values and then replaying them for any late subscriptions. Consider this example where we have moved our first publication to occur before our subscription

```csharp
static void Main(string[] args)
{
    var subject = new Subject<string>();

    subject.OnNext("a");
    WriteSequenceToConsole(subject);

    subject.OnNext("b");
    subject.OnNext("c");
    Console.ReadKey();
}
```

The result of this would be that 'b' and 'c' would be written to the console, but 'a' ignored. 
If we were to make the minor change to make subject a `ReplaySubject<T>` we would see all publications again.

```csharp
var subject = new ReplaySubject<string>();

subject.OnNext("a");
WriteSequenceToConsole(subject);

subject.OnNext("b");
subject.OnNext("c");
```

This can be very handy for eliminating race conditions. Be warned though, the default constructor of the `ReplaySubject<T>` will create an instance that caches every value published to it. In many scenarios this could create unnecessary memory pressure on the application. `ReplaySubject<T>` allows you to specify simple cache expiry settings that can alleviate this memory issue. One option is that you can specify the size of the buffer in the cache. In this example we create the `ReplaySubject<T>` with a buffer size of 2, and so only get the last two values published prior to our subscription:

```csharp    
public void ReplaySubjectBufferExample()
{
    var bufferSize = 2;
    var subject = new ReplaySubject<string>(bufferSize);

    subject.OnNext("a");
    subject.OnNext("b");
    subject.OnNext("c");
    subject.Subscribe(Console.WriteLine);
    subject.OnNext("d");
}
```

Here the output would show that the value 'a' had been dropped from the cache, but values 'b' and 'c' were still valid. The value 'd' was published after we subscribed so it is also written to the console.

```
Output:
b
c
d
```

Another option for preventing the endless caching of values by the `ReplaySubject<T>`, is to provide a window for the cache. In this example, instead of creating a `ReplaySubject<T>` with a buffer size, we specify a window of time that the cached values are valid for.

```csharp
public void ReplaySubjectWindowExample()
{
    var window = TimeSpan.FromMilliseconds(150);
    var subject = new ReplaySubject<string>(window);

    subject.OnNext("w");
    Thread.Sleep(TimeSpan.FromMilliseconds(100));
    subject.OnNext("x");
    Thread.Sleep(TimeSpan.FromMilliseconds(100));
    subject.OnNext("y");
    subject.Subscribe(Console.WriteLine);
    subject.OnNext("z");
}
```

In the above example the window was specified as 150 milliseconds. Values are published 100 milliseconds apart. Once we have subscribed to the subject, the first value is 200ms old and as such has expired and been removed from the cache.

```
Output:
x
y
z
```

## BehaviorSubject<T>

[`BehaviorSubject<T>`](http://msdn.microsoft.com/en-us/library/hh211949(v=VS.103).aspx "BehaviorSubject(Of T) - MSDN") is similar to `ReplaySubject<T>` except it only remembers the last publication. `BehaviorSubject<T>` also requires you to provide it a default value of `T`. This means that all subscribers will receive a value immediately (unless it is already completed).

In this example the value 'a' is written to the console:

```csharp
public void BehaviorSubjectExample()
{
    //Need to provide a default value.
    var subject = new BehaviorSubject<string>("a");
    subject.Subscribe(Console.WriteLine);
}
```

In this example the value 'b' is written to the console, but not 'a'.

```csharp
public void BehaviorSubjectExample2()
{
    var subject = new BehaviorSubject<string>("a");
    subject.OnNext("b");
    subject.Subscribe(Console.WriteLine);
}
```

In this example the values 'b', 'c' &amp; 'd' are all written to the console, but again not 'a'

```csharp
public void BehaviorSubjectExample3()
{
    var subject = new BehaviorSubject<string>("a");

    subject.OnNext("b");
    subject.Subscribe(Console.WriteLine);
    subject.OnNext("c");
    subject.OnNext("d");
}
```

Finally in this example, no values will be published as the sequence has completed. Nothing is written to the console.

```csharp
public void BehaviorSubjectCompletedExample()
{
    var subject = new BehaviorSubject<string>("a");
    subject.OnNext("b");
    subject.OnNext("c");
    subject.OnCompleted();
    subject.Subscribe(Console.WriteLine);
}
```

That note that there is a difference between a `ReplaySubject<T>` with a buffer size of one (commonly called a 'replay one subject') and a `BehaviorSubject<T>`. A `BehaviorSubject<T>` requires an initial value. With the assumption that neither subjects have completed, then you can be sure that the `BehaviorSubject<T>` will have a value. You cannot be certain with the `ReplaySubject<T>` however. With this in mind, it is unusual to ever complete a `BehaviorSubject<T>`. Another difference is that a replay-one-subject will still cache its value once it has been completed. So subscribing to a completed `BehaviorSubject<T>` we can be sure to not receive any values, but with a `ReplaySubject<T>` it is possible.

`BehaviorSubject<T>`s are often associated with class [properties](http://msdn.microsoft.com/en-us/library/65zdfbdt(v=vs.71).aspx). 
As they always have a value and can provide change notifications, they could be candidates for backing fields to properties.

## AsyncSubject<T>

[`AsyncSubject<T>`](http://msdn.microsoft.com/en-us/library/hh229363(v=VS.103).aspx "AsyncSubject(Of T) - MSDN") is similar to the Replay and Behavior subjects in the way that it caches values, however it will only store the last value, and only publish it when the sequence is completed. The general usage of the `AsyncSubject<T>` is to only ever publish one value then immediately complete. This means that is becomes quite comparable to `Task<T>`.

In this example no values will be published as the sequence never completes. 
No values will be written to the console.

```csharp
static void Main(string[] args)
{
    var subject = new AsyncSubject<string>();
    subject.OnNext("a");
    WriteSequenceToConsole(subject);
    subject.OnNext("b");
    subject.OnNext("c");
    Console.ReadKey();
}
```

In this example we invoke the `OnCompleted` method so the last value 'c' is written to the console:

```csharp
static void Main(string[] args)
{
    var subject = new AsyncSubject<string>();

    subject.OnNext("a");
    WriteSequenceToConsole(subject);
    subject.OnNext("b");
    subject.OnNext("c");
    subject.OnCompleted();
    Console.ReadKey();
}
```



## Implicit contracts

There are implicit contacts that need to be upheld when working with Rx as mentioned above. The key one is that once a sequence is completed, no more activity can happen on that sequence. A sequence can be completed in one of two ways, either by `OnCompleted()` or by `OnError(Exception)`.

The four subjects described in this chapter all cater for this implicit contract by ignoring any attempts to publish values, errors or completions once the sequence has already terminated.

Here we see an attempt to publish the value 'c' on a completed sequence. Only values 'a' and 'b' are written to the console.

```csharp
public void SubjectInvalidUsageExample()
{
    var subject = new Subject<string>();

    subject.Subscribe(Console.WriteLine);

    subject.OnNext("a");
    subject.OnNext("b");
    subject.OnCompleted();
    subject.OnNext("c");
}
```

## ISubject interfaces

While each of the four subjects described in this chapter implement the `IObservable<T>` and `IObserver<T>` interfaces, they do so via another set of interfaces:

```csharp
//Represents an object that is both an observable sequence as well as an observer.
public interface ISubject<in TSource, out TResult> 
    : IObserver<TSource>, IObservable<TResult>
{
}
```

As all the subjects mentioned here have the same type for both `TSource` and `TResult`, they implement this interface which is the superset of all the previous interfaces:

```csharp
//Represents an object that is both an observable sequence as well as an observer.
public interface ISubject<T> : ISubject<T, T>, IObserver<T>, IObservable<T>
{
}
```

These interfaces are not widely used, but prove useful as the subjects do not share a common base class. We will see the subject interfaces used later when we discover [Hot and cold observables](14_HotAndColdObservables.html).

## Subject factory

Finally it is worth making you aware that you can also create a subject via a factory method. Considering that a subject combines the `IObservable<T>` and `IObserver<T>` interfaces, it seems sensible that there should be a factory that allows you to combine them yourself. The `Subject.Create(IObserver<TSource>, IObservable<TResult>)` factory method provides just this.

```csharp
//Creates a subject from the specified observer used to publish messages to the subject
//  and observable used to subscribe to messages sent from the subject
public static ISubject>TSource, TResult< Create>TSource, TResult<(
    IObserver>TSource< observer, 
    IObservable>TResult< observable)
{...}
```

Subjects provide a convenient way to poke around Rx, however they are not recommended for day to day use. An explanation is in the [Usage Guidelines](18_UsageGuidelines.md) in the appendix. Instead of using subjects, favor the factory methods we will look at in [Part 2](04_CreatingObservableSequences.md).

The fundamental types `IObserver<T>` and `IObservable<T>` and the auxiliary subject types create a base from which to build your Rx knowledge. It is important to understand these simple types and their implicit contracts. In production code you may find that you rarely use the `IObserver<T>` interface and subject types, but understanding them and how they fit into the Rx eco-system is still important. The `IObservable<T>` interface is the dominant type that you will be exposed to for representing a sequence of data in motion, and therefore will comprise the core concern for most of your work with Rx and most of this book.



...

weird bit from Observable.Return

```csharp
var singleValue = Observable.Return<string>("Value");

// which could have also been simulated with a replay subject
var subject = new ReplaySubject<string>();
subject.OnNext("Value");
subject.OnCompleted();
```

Note that in the example above that we could use the factory method or get the same effect by using the replay subject. The obvious difference is that the factory method is only one line and it allows for declarative over imperative programming style. In the example above we specified the type parameter as `string`, this is not necessary as it can be inferred from the argument provided.

```csharp
singleValue = Observable.Return<string>("Value");
// Can be reduced to the following
singleValue = Observable.Return("Value");
```

...

From Observable.Create:

The usage of subjects should largely remain in the realms of samples and testing. Subjects are a great way to get started with Rx. They reduce the learning curve for new developers, however they pose several concerns that the `Create` method eliminates. Rx is effectively a functional programming paradigm. Using subjects means we are now managing state, which is potentially mutating. Mutating state and asynchronous programming are very hard to get right. Furthermore many of the operators (extension methods) have been carefully written to ensure correct and consistent lifetime of subscriptions and sequences are maintained. When you introduce subjects you can break this. Future releases may also see significant performance degradation if you explicitly use subjects.


..
A significant benefit that the `Create` method has over subjects is that the sequence will be lazily evaluated. Lazy evaluation is a very important part of Rx. It opens doors to other powerful features such as scheduling and combination of sequences that we will see later. The delegate will only be invoked when a subscription is made.
