---
title : Creating a sequence
---

# TODO: content that was originally in ch02 that really belongs in here

## Creating an IObservable<T> to Represent Events

Here's an overly simplified implementation of an `IObservable<int>` that produces a sequence of numbers:

```csharp
public class MySequenceOfNumbers : IObservable<int>
{
    public IDisposable Subscribe(IObserver<int> observer)
    {
        observer.OnNext(1);
        observer.OnNext(2);
        observer.OnNext(3);
        observer.OnCompleted();
        return Disposable.Empty;
    }
}
```

We can test this by constructing it, and then creating an instance of the console-based `IObserver<T>` defined earlier, and subscribing that to the `MySequenceOfNumbers` instance:

```csharp
var numbers = new MySequenceOfNumbers();
var observer = new MyConsoleObserver<int>();
numbers.Subscribe(observer);
```

This produces the following output:

```
Received value 1
Received value 2
Received value 3
Sequence terminated
```

This is a little too simple to be useful. For one thing, we typically use Rx when there are events of interest, but this is not really reactive at all—it just produces a fixed set of numbers immediately. Moreover, the implementation is blocking—it doesn't even return from `Subscribe` until after it has finished producing all of its values. This example illustrates the basics of how a source provides events to a subscriber, but for this example we might as well use an `IEnumerable<T>` implementation like a `List<T>` or an array.

Here's something a little more realistic. This is a wrapper around .NET's `FileSystemWatcher`, presenting filesystem change notifications as an `IObservable<FileSystemEventArgs>`.

```cs
// Represents filesystem changes as an Rx observable sequence.
// NOTE: this is an oversimplified example for illustration purposes.
//       It does not handle multiple subscribers efficiently, and it does not
//       use IScheduler.
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
        // which integrates well with Windows Forms, but which might prove
        // more complex here) nor does it promise to wait until we've
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
                }
            }
        };

        watcher.EnableRaisingEvents = true;

        return watcher;
    }
}
```

That got more complex fast. This illustrates that `IObservable<T>` implementations are responsible for obeying the `IObserver<T>` rules. This is generally a good thing: it keeps the messy concerns around concurrency contained in a single place. Any `IObserver<FileSystemEventArgs>` that I subscribe to this `RxFsEvents` is free from having to manage concurrency, because it can count on the `IObserver<T>` rules, which guarantee that it will only have to handle one thing at a time. If I hadn't been required to enforce these rules in the source, it might have made my `RxFsEvents` class simpler, but all of that complexity of dealing with overlapping events would have spread out into the code that handles the events. Concurrency is hard enough to deal with when its effects are contained. Once it starts to spread across multiple types, it can become almost impossible to reason about. Rx's `IObserver<T>` rules prevent this from happening.

There are a couple of issues with this code. One is that when `IObservable<T>` implementations produce events modelling real-life asynchronous activity (such as filesystem changes) applications will often want some way to take control over which threads notifications arrive on. For example, UI frameworks tend to have thread affinity requirements—you typically need to be on a particular thread to be allowed to update the user interface. So we would normally expect to be able to provide this sort of observer with an `IScheduler`, and for it to deliver notifications through that. We'll discuss schedulers in later chapters.

The other issue is that this does not deal with multiple subscribers efficiently. You're allowed to call `IObservable<T>.Subscribe` multiple times, and if you do that with this code, it will create a new `FileSystemWatcher` each time. That could happen more easily than you might think. Suppose we had an instance of this watcher, and wanted to handle different events in different ways. We might use the `Where` operator to define observable sources that split events up in the way we want:

```cs
IObservable<FileSystemEventArgs> configChanges =
    fs.Where(e => Path.GetExtension(e.Name) == ".config");
IObservable<FileSystemEventArgs> deletions =
    fs.Where(e => e.ChangeType == WatcherChangeTypes.Deleted);
```

When you call `Subscribe` on the `IObservable<T>` returned by the `Where` operator, it will call `Subscribe` on its input. So in this case, if we call `Subscribe` on both `configChanges` and `deletion`, that will result in _two_ calls to `Subscribe` on `rs`. So if `rs` is an instance of our `RxFsEvents` type above, each one will construct its own `FileSystemEventWatcher`, which is inefficient.

Rx offers a few ways to deal with this. It provides operators designed specifically to take an `IObservable<T>` that does not tolerate multiple subscribers and wrap it in an adapter that handles multiple subscribers for you:

```cs
IObservable<FileSystemEventArgs> fs =
    new RxFsEvents(@"c:\temp")
    .Publish()
    .RefCount();
```

But this is leaping ahead somewhat. If you want to build a type that is inherently multi-subscriber-friendly, all you really need to do is keep track of all your subscribers, and notify them 


This problem of implementing the interfaces should not concern us too much. You will find that when you use Rx, you do not have the need to actually implement these interfaces, Rx provides all of the implementations you need out of the box. Let's have a look at the simple ones.

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

In the above example the window was specified as 150 milliseconds. Values are published 100 milliseconds apart. Once we have subscribed to the subject, the first value	is 200ms old and as such has expired and been removed from the cache.

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




# TODO: end of content relocated from ch02. There follows the original text for this chapter.


# PART 2 - Sequence basics

So you want to get involved and write some Rx code, but how do you get started? We have looked at the key types, but know that we should not be creating our own implementations of `IObserver<T>` or `IObservable<T>` and should favor factory methods over using subjects. Even if we have an observable sequence, how do we pick out the data we want from it? We need to understand the basics of creating an observable sequence, getting values into it and picking out the values we want from them.

In Part 2 we discover the basics for constructing and querying observable sequences. We assert that LINQ is fundamental to using and understanding Rx. On deeper inspection, we find that _functional programming_ concepts are core to having a deep understanding of LINQ and therefore enabling you to master Rx. To support this understanding, we classify the query operators into three main groups. Each of these groups proves to have a root operator that the other operators can be constructed from. Not only will this deconstruction exercise provide a deeper insight to Rx, functional programming and query composition; it should arm you with the ability to create custom operators where the general Rx operators do not meet your needs.

# Creating a sequence				

In the previous chapters we used our first Rx extension method, the `Subscribe` method and its overloads. We also have seen our first factory method in `Subject.Create()`. We will start looking at the vast array of other methods that enrich `IObservable<T>` to make Rx what it is. It may be surprising to see that there are relatively few public instance methods in the Rx library. There are however a large number of public static methods, and more specifically, a large number of extension methods. Due to the large number of methods and their overloads, we will break them down into categories.

> Some readers may feel that they can skip over parts of the next few chapters. I would only suggest doing so if you are very confident with LINQ and functional composition. The intention of this book is to provide a step-by-step introduction to Rx, with the goal of you, the reader, being able to apply Rx to your software. 
> The appropriate	application of Rx will come through a sound understanding of the fundamentals of Rx. The most common mistakes people will make with Rx are due to a misunderstanding of the principles upon which Rx was built. With this in mind, I encourage you to read on.

It seems sensible to follow on from our examination of our key types where we simply constructed new instances of subjects. Our first category of methods will be _creational_ methods: simple ways we can create instances of `IObservable<T>` sequences. These methods generally take a seed to produce a sequence: either a single value of a type, or just the type itself. In functional programming this can be described as _anamorphism_ or referred to as an '_unfold_'.

## Simple factory methods			

### Observable.Return				

In our first and most basic example we introduce `Observable.Return<T>(T value)`. This method takes a value of `T` and returns an `IObservable<T>` with the single value and then completes. It has _unfolded_ a value of `T` into an observable sequence.

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

### Observable.Empty					

The next two examples only need the type parameter to unfold into an observable sequence. The first is `Observable.Empty<T>()`. This returns an empty `IObservable<T>` i.e. it just publishes an `OnCompleted` notification.

```csharp
var empty = Observable.Empty<string>();
// Behaviorally equivalent to
var subject = new ReplaySubject<string>();
subject.OnCompleted();
```

### Observable.Never					

The `Observable.Never<T>()` method will return infinite sequence without any notifications.

```csharp
var never = Observable.Never<string>();
// similar to a subject without notifications
var subject = new Subject<string>();
```

### Observable.Throw					

`Observable.Throw<T>(Exception)` method needs the type parameter information, it also need the `Exception` that it will `OnError` with. This method creates a sequence with just a single `OnError` notification containing the	exception passed to the factory.

```csharp
var throws = Observable.Throw<string>(new Exception()); 
// Behaviorally equivalent to
var subject = new ReplaySubject<string>(); 
subject.OnError(new Exception());
```

### Observable.Create				

The `Create` factory method is a little different to the above creation methods.
The method signature itself may be a bit overwhelming at first, but becomes quite natural once you have used it.

```csharp
// Creates an observable sequence from a specified Subscribe method implementation.
public static IObservable<TSource> Create<TSource>(
    Func<IObserver<TSource>, IDisposable> subscribe)
{...}
public static IObservable<TSource> Create<TSource>(
    Func<IObserver<TSource>, Action> subscribe)
{...}
```

Essentially this method allows you to specify a delegate that will be executed anytime a subscription is made. The `IObserver<T>` that made the subscription will be passed to your delegate so that you can call the `OnNext`/`OnError`/`OnCompleted` methods as you need. This is one of the few scenarios where you will need to concern yourself with the `IObserver<T>` interface. Your delegate is a `Func` that returns an `IDisposable`. This `IDisposable` will have its `Dispose()` method called when the subscriber disposes from their subscription.

The `Create` factory method is the preferred way to implement custom observable sequences. The usage of subjects should largely remain in the realms of samples and testing. Subjects are a great way to get started with Rx. They reduce the learning curve for new developers, however they pose several concerns that the `Create` method eliminates. Rx is effectively a functional programming paradigm. Using subjects means we are now managing state, which is potentially mutating. Mutating state and asynchronous programming are very hard to get right. Furthermore many of the operators (extension methods) have been carefully written to ensure correct and consistent lifetime of subscriptions and sequences are maintained. When you introduce subjects you can break this. Future releases may also see significant performance degradation if you explicitly use subjects.

The `Create` method is also preferred over creating custom types that implement the `IObservable` interface. There really is no need to implement the observer/observable interfaces yourself. Rx tackles the intricacies that you may not think of such as thread safety of notifications and subscriptions.

A significant benefit that the `Create` method has over subjects is that the sequence will be lazily evaluated. Lazy evaluation is a very important part of Rx. It opens doors to other powerful features such as scheduling and combination of sequences that we will see later. The delegate will only be invoked when a subscription is made.

In this example we show how we might first return a sequence via standard blocking eagerly evaluated call, and then we show the correct way to return an observable sequence without blocking by lazy evaluation.

```csharp
private IObservable<string> BlockingMethod()
{
    var subject = new ReplaySubject<string>();
    subject.OnNext("a");
    subject.OnNext("b");
    subject.OnCompleted();
    Thread.Sleep(1000);

    return subject;
}

private IObservable<string> NonBlocking()
{
    return Observable.Create<string>(
        (IObserver<string> observer) =>
        {
            observer.OnNext("a");
            observer.OnNext("b");
            observer.OnCompleted();
            Thread.Sleep(1000);

            return Disposable.Create(() => Console.WriteLine("Observer has unsubscribed"));
            // or can return an Action like 
            // return () => Console.WriteLine("Observer has unsubscribed"); 
        });
}
```

While the examples are somewhat contrived, the intention is to show that when a consumer calls the eagerly evaluated, blocking method, they will be blocked for at least 1 second before they even receive the `IObservable<string>`, regardless of if they do actually subscribe to it or not. The non blocking method is lazily evaluated so the consumer immediately receives the `IObservable<string>` and will only incur the cost of the thread sleep if they subscribe.

As an exercise, try to build the `Empty`, `Return`, `Never` &amp; `Throw` extension methods yourself using the `Create` method. If you have Visual Studio or [LINQPad](http://www.linqpad.net/) available to you right now, code it up as quickly as you can. If you don't (perhaps you are on the train on the way to work), try to conceptualize how you would solve this problem. When you are done move forward to see some examples of how it could be done...

Examples of `Empty`, `Return`, `Never` and `Throw` recreated with `Observable.Create`:

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

You can see that `Observable.Create` provides the power to build our own factory methods if we wish. You may have noticed that in each of the examples we only are able to return our subscription token (the implementation of `IDisposable`) once we have produced all of our `OnNext` notifications. This is because inside of the delegate we provide, we are completely sequential. It also makes the token rather pointless. Now we look at how we can use the return value in a more useful way. First is an example where inside our delegate we create a Timer that will call the observer's `OnNext` each time the timer ticks.

```csharp
// Example code only
public void NonBlocking_event_driven()
{
    var ob = Observable.Create<string>(
        observer =>
        {
            var timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (s, e) => observer.OnNext("tick");
            timer.Elapsed += OnTimerElapsed;
            timer.Start();
            
            return Disposable.Empty;
        });

    var subscription = ob.Subscribe(Console.WriteLine);
    Console.ReadLine();
    subscription.Dispose();
}

private void OnTimerElapsed(object sender, ElapsedEventArgs e)
{
    Console.WriteLine(e.SignalTime);
}
```

Output:

```
tick
01/01/2012 12:00:00
tick
01/01/2012 12:00:01
tick
01/01/2012 12:00:02
01/01/2012 12:00:03
01/01/2012 12:00:04
01/01/2012 12:00:05
```

The example above is broken. When we dispose of our subscription, we will stop seeing "tick" being written to the screen; however we have not released our second event handler "`OnTimerElasped`" and have not disposed of the instance of the timer, so it will still be writing the `ElapsedEventArgs.SignalTime` to the console after our disposal. The extremely simple fix is to return `timer` as the `IDisposable` token.

```csharp
// Example code only
var ob = Observable.Create<string>(
    observer =>
    {
        var timer = new System.Timers.Timer();
        timer.Interval = 1000;
        timer.Elapsed += (s, e) => observer.OnNext("tick");
        timer.Elapsed += OnTimerElapsed;
        timer.Start();
            
        return timer;
    });
```

Now when a consumer disposes of their subscription, the underlying `Timer` will be disposed of too.

`Observable.Create` also has an overload that requires your `Func` to return an `Action` instead of an `IDisposable`. In a similar example to above, this one shows how you could use an action to un-register the event handler, preventing a memory leak by retaining the reference to the timer.

```csharp
// Example code only
var ob = Observable.Create<string>(
    observer =>
    {
        var timer = new System.Timers.Timer();
        timer.Enabled = true;
        timer.Interval = 100;
        timer.Elapsed += OnTimerElapsed;
        timer.Start();
            
        return ()=>{
            timer.Elapsed -= OnTimerElapsed;
            timer.Dispose();
        };
    });
```

These last few examples showed you how to use the `Observable.Create` method. These were just examples; there are actually better ways to produce values from a timer that we will look at soon. The intention is to show that `Observable.Create` provides you a lazily evaluated way to create observable sequences. We will dig much deeper into lazy evaluation and application of the `Create` factory method throughout the book especially when we cover concurrency and scheduling.

## Functional unfolds				

As a functional programmer you would come to expect the ability to unfold a potentially	infinite sequence. An issue we may face with `Observable.Create` is that is that it can be a clumsy way to produce an infinite sequence. Our timer example above is an example of an infinite sequence, and while this is a simple implementation it is an annoying amount of code for something that effectively is delegating all the work to the `System.Timers.Timer` class. The `Observable.Create` method also has poor support for unfolding sequences using corecursion.

### Corecursion						

Corecursion is a function to apply to the current state to produce the next state. Using corecursion by taking a value, applying a function to it that extends that value and repeating we can create a sequence. A simple example might be to take the value 1 as the seed and a function that increments the given value by one. This could be used to create sequence of [1,2,3,4,5...].

Using corecursion to create an `IEnumerable<int>` sequence is made simple with the `yield return` syntax.

```csharp
private static IEnumerable<T> Unfold<T>(T seed, Func<T, T> accumulator)
{
    var nextValue = seed;
    while (true)
    {
        yield return nextValue;
        nextValue = accumulator(nextValue);
    }
}
```

The code above could be used to produce the sequence of natural numbers like this.

```csharp
var naturalNumbers = Unfold(1, i => i + 1);
Console.WriteLine("1st 10 Natural numbers");
foreach (var naturalNumber in naturalNumbers.Take(10))
{
    Console.WriteLine(naturalNumber);
}
```

Output:

```
1st 10 Natural numbers
1
2
3
4
5
6
7
8
9
10
```

Note the `Take(10)` is used to terminate the infinite sequence.

Infinite and arbitrary length sequences can be very useful. First we will look at some that come with Rx and then consider how we can generalize the creation of infinite observable sequences.

### Observable.Range					

`Observable.Range(int, int)` simply returns a range of integers. The first integer is the initial value and the second is the number of values to yield. This example will write the values '10' through to '24' and then complete.

```csharp
var range = Observable.Range(10, 15);
range.Subscribe(Console.WriteLine, ()=>Console.WriteLine("Completed"));
```

### Observable.Generate				

It is difficult to emulate the `Range` factory method using `Observable.Create`. It would be cumbersome to try and respect the principles that the code should be lazily evaluated and the consumer should be able to dispose of the subscription resources when they so choose. This is where we can use corecursion to provide a richer unfold. In Rx the unfold method is called `Observable.Generate`.

The simple version of `Observable.Generate` takes the following parameters:

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

As an exercise, write your own `Range` factory method using `Observable.Generate`.

Consider the `Range` signature `Range(int start, int count)`, which provides the seed and a value for the conditional predicate. You know how each new value is derived from the previous one; this becomes your iterate function. Finally, you probably don't need to transform the state so this makes the result selector function very simple.

Continue when you have built your own version...

Example of how you could use `Observable.Generate` to construct a similar `Range` factory method.

```csharp
// Example code only
public static IObservable<int> Range(int start, int count)
{
    var max = start + count;
    return Observable.Generate(
        start, 
        value => value < max, 
        value => value + 1, 
        value => value);
}
```
    
### Observable.Interval				

Earlier in the chapter we used a `System.Timers.Timer` in our observable to generate a continuous sequence of notifications. As mentioned in the example at the time, this is not the preferred way of working with timers in Rx. As Rx provides operators that give us this functionality it could be argued that to not use them is to re-invent the wheel. More importantly the Rx operators are the preferred way of working with timers due to their ability to substitute in schedulers which is desirable for easy substitution of the underlying timer. There are at least three various timers you could choose from for the example above:

- `System.Timers.Timer`
- `System.Threading.Timer`
- `System.Windows.Threading.DispatcherTimer`

By abstracting the timer away via a scheduler we are able to reuse the same code for multiple platforms. More importantly than being able to write platform independent code is the ability to substitute in a test-double scheduler/timer to enable testing. Schedulers are a complex subject that is out of scope for this chapter, but they are covered in detail in the later chapter on [Scheduling and threading](15_SchedulingAndThreading.html).

There are three better ways of working with constant time events, each being a further generalization of the former. The first is `Observable.Interval(TimeSpan)` which will publish incremental values starting from zero, based on a frequency of your choosing. 

This example publishes values every 250 milliseconds.

```csharp
    var interval = Observable.Interval(TimeSpan.FromMilliseconds(250));
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

Once subscribed, you must dispose of your subscription to stop the sequence. It is an example of an infinite sequence.

### Observable.Timer					

The second factory method for producing constant time based sequences is `Observable.Timer`. It has several overloads; the first of which we will look at being very simple. The most basic overload of `Observable.Timer` takes just a `TimeSpan` as `Observable.Interval` does. The `Observable.Timer` will however only publish one value (0) after the period of time has elapsed, and then it will complete.

    var timer = Observable.Timer(TimeSpan.FromSeconds(1));
    timer.Subscribe(
        Console.WriteLine, 
        () => Console.WriteLine("completed"));

Output:

```
0
completed
```

Alternatively, you can provide a `DateTimeOffset` for the `dueTime` parameter. This will produce the value 0 and complete at the due time.

A further set of overloads adds a `TimeSpan` that indicates the period to produce subsequent values. This now allows us to produce infinite sequences and also construct `Observable.Interval` from `Observable.Timer`.

```csharp
public static IObservable<long> Interval(TimeSpan period)
{
    return Observable.Timer(period, period);
}
```

Note that this now returns an `IObservable` of `long` not `int`. While `Observable.Interval` would always wait the given period before producing the first value, this `Observable.Timer` overload gives the ability to start the sequence when you choose. With `Observable.Timer` you can write the following to have an interval sequence that started immediately.

```csharp
Observable.Timer(TimeSpan.Zero, period);
```

This takes us to our third way and most general way for producing timer related sequences, back to `Observable.Generate`. This time however, we are looking at a more complex overload that allows you to provide a function that specifies the due time for the next value.

```csharp
public static IObservable<TResult> Generate<TState, TResult>(
    TState initialState, 
    Func<TState, bool> condition, 
    Func<TState, TState> iterate, 
    Func<TState, TResult> resultSelector, 
    Func<TState, TimeSpan> timeSelector)
```

Using this overload, and specifically the extra `timeSelector` argument, we can produce our own implementation of `Observable.Timer` and in turn, `Observable.Interval`.

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

This shows how you can use `Observable.Generate` to produce infinite sequences. I will leave it up to you the reader, as an exercise using `Observable.Generate`, to produce values at variable rates. I find using these methods invaluable not only in day to day work but especially for producing dummy data.

## Transitioning into IObservable&lt;T&gt;		

Generation of an observable sequence covers the complicated aspects of functional programming i.e. corecursion and unfold. You can also start a sequence by simply making a transition from an existing synchronous or asynchronous paradigm into the Rx paradigm.

### From delegates					

The `Observable.Start` method allows you to turn a long running `Func<T>` or `Action` into a single value observable sequence. By default, the processing will be done asynchronously on a ThreadPool thread. If the overload you use is a `Func<T>` then the return type will be `IObservable<T>`. When the function returns its value, that value will be published and then the sequence completed. If you use the overload that takes an `Action`, then the returned sequence will be of type `IObservable<Unit>`. The `Unit` type is a functional programming construct and is analogous to `void`. In this case `Unit` is used to publish an acknowledgement that the `Action` is complete, however this is rather inconsequential as the sequence is immediately completed straight after `Unit` anyway. The `Unit` type itself has no value; it just serves as an empty payload for the `OnNext` notification. Below is an example of using both overloads.

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

Note the difference between `Observable.Start` and `Observable.Return`; `Start` lazily evaluates the value from a function, `Return` provided the value eagerly. This makes `Start` very much like a `Task`. This can also lead to some confusion on when to use each of the features. Both are valid tools and the choice come down to the context of the problem space. Tasks are well suited to parallelizing computational work and providing workflows via continuations for computationally heavy work. Tasks also have the benefit of documenting and enforcing single value semantics. Using `Start` is a good way to integrate computationally heavy work into an existing code base that is largely made up of observable sequences. We look at [composition of sequences](12_CombiningSequences.html) in more depth later in the book.

### From events						

As we discussed early in the book, .NET already has the event model for providing a reactive, event driven programming model. While Rx is a more powerful and useful framework, it is late to the party and so needs to integrate with the existing event model. Rx provides methods to take an event and turn it into an observable sequence. There are several different varieties you can use. 

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

In other words,	a function that returns a single value in the future should return a `Task<T>`,	not an `IObservable<T>`. Then if you need to combine it with other observables, use `ToObservable()`.

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

The result of the call to `Observable.FromAsyncPattern` does _not_ return an observable sequence. It returns a delegate that returns an observable sequence. The signature for this delegate will match the generic arguments of the call to	`FromAsyncPattern`, except that the return type will be wrapped in an observable sequence.

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