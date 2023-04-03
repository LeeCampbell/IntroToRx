---
title: Advanced error handling
---

# Advanced error handling				

Exceptions happen. Exceptions themselves are not bad or good, however the way we raise or catch them can. Some exceptions are predictable and are due to sloppy code, for example a `DivideByZeroException`. Other exceptions cannot be prevented with defensive coding, for example an I/O exception like `FileNotFoundException` or `TimeoutException`. In these cases, we need to cater for the exception gracefully. Providing some sort of error message to the user, logging the error or perhaps retrying are all potential ways to handle these exceptions.

The `IObserver<T>` interface and `Subscribe` extension methods provide the ability to cater for sequences that terminate in error, however they leave the sequence terminated. They also do not offer a composable way to cater for different `Exception` types. A functional approach that enables composition of error handlers, allowing us to remain in the monad, would be more useful. 
Again, Rx delivers.

## Control flow constructs				

Using marble diagrams, we will examine various ways to handle different control flows. Just as with normal .NET code, we have flow control constructs such as `try`/`catch`/`finally`. In this chapter we see how they can be applied to observable sequences.

### Catch								

Just like a catch in SEH (Structured Exception Handling), with Rx you have the option of swallowing an exception, wrapping it in another exception or performing some other logic.

We already know that observable sequences can handle erroneous situations with the `OnError` construct. A useful method in Rx for handling an `OnError` notification is the `Catch` extension method. Catch allows you to intercept a specific `Exception` type and then continue with another sequence.

Below is the signature for the simple overload of catch:

```csharp
public static IObservable<TSource> Catch<TSource>(
    this IObservable<TSource> first, 
    IObservable<TSource> second)
{
    ...
}
```

#### Swallowing exceptions			

With Rx, you can catch and swallow exceptions in a similar way to SEH. It is quite simple; we use the `Catch` extension method and provide an empty sequence as the second value.

We can represent an exception being swallowed like this with a marble diagram.

```
S1--1--2--3--X
S2            -|
R --1--2--3----|
```

Here `S1` represents the first sequence that ends with an error (`X`). `S2` is the continuation sequence, an empty sequence. `R` is the result sequence which starts as `S1`, then continues with `S2` when `S1` terminates.

```csharp
var source = new Subject<int>();
var result = source.Catch(Observable.Empty<int>());

result.Dump("Catch"););

source.OnNext(1);
source.OnNext(2);
source.OnError(new Exception("Fail!"));
```

Output:

```
Catch --> 1
Catch --> 2
Catch completed
```

The example above will catch and swallow all types of exceptions. 
This is somewhat equivalent to the following with SEH:

```csharp
try
{
    DoSomeWork();
}
catch
{
}
```

Just as it is generally avoided in SEH, you probably also want to limit your use of swallowing errors in Rx. You may, however, have a specific exception you want to handle. Catch has an overload that enables you specify the type of exception. Just as the following code would allow you to catch a `TimeoutException`:

```csharp
try
{
    //
}
catch (TimeoutException tx)
{
    //
}
```

Rx also offers an overload of `Catch` to cater for this.

```csharp
public static IObservable<TSource> Catch<TSource, TException>(
    this IObservable<TSource> source, 
    Func<TException, IObservable<TSource>> handler) 
    where TException : Exception
{
    ...
}
```

The following Rx code allows you to catch a `TimeoutException`. Instead of providing a second sequence, we provide a function that takes the exception and returns a sequence. This allows you to use a factory to create your continuation. In this example, we add the value -1 to the error sequence and then complete it.

```csharp
var source = new Subject<int>();
var result = source.Catch<int, TimeoutException>(tx=>Observable.Return(-1));

result.Dump("Catch");

source.OnNext(1);
source.OnNext(2);
source.OnError(new TimeoutException());
```

Output:

```
Catch-->1
Catch-->2
Catch-->-1
Catch completed
```

If the sequence was to terminate with an `Exception` that could not be cast to a `TimeoutException`, then the error would not be caught and would flow through to the subscriber.

```csharp
var source = new Subject<int>();
var result = source.Catch<int, TimeoutException>(tx=>Observable.Return(-1));

result.Dump("Catch");

source.OnNext(1);
source.OnNext(2);
source.OnError(new ArgumentException("Fail!"));
```

Output:

```
Catch-->1
Catch-->2
Catch failed-->Fail!
```

### Finally							

Similar to the `finally` statement with SEH, Rx exposes the ability to execute code on completion of a sequence, regardless of how it terminates. The `Finally` extension method accepts an `Action` as a parameter. This `Action` will be invoked if the sequence terminates normally or erroneously, or if the subscription is disposed of.

```csharp
public static IObservable<TSource> Finally<TSource>(
    this IObservable<TSource> source, 
    Action finallyAction)
{
    ...
}
```

In this example, we have a sequence that completes. We provide an action and see that it is called after our `OnCompleted` handler.

```csharp
var source = new Subject<int>();
var result = source.Finally(() => Console.WriteLine("Finally action ran"));
result.Dump("Finally");
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);
source.OnCompleted();
```

Output:

```
Finally-->1
Finally-->2
Finally-->3
Finally completed
Finally action ran
```

In contrast, the source sequence could have terminated with an exception. In that case, the exception would have been sent to the console, and then the delegate we provided would have been executed.

Alternatively, we could have disposed of our subscription. In the next example, we see that the `Finally` action is invoked even though the sequence does not complete.

```csharp
var source = new Subject<int>();
var result = source.Finally(() => Console.WriteLine("Finally"));
var subscription = result.Subscribe(
    Console.WriteLine,
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);
subscription.Dispose();
```

Output:

```
1
2
3
Finally
```

Note that there is an anomaly in the current implementation of `Finally`. If there is no `OnError` handler provided, the error will be promoted to an exception and thrown. This will be done before the `Finally` action is invoked. We can reproduce this behavior easily by removing the `OnError` handler from our examples above.

```csharp
var source = new Subject<int>();
var result = source.Finally(() => Console.WriteLine("Finally"));
result.Subscribe(
    Console.WriteLine,
    // Console.WriteLine,
    () => Console.WriteLine("Completed"));
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);

// Brings the app down. Finally action is not called.
source.OnError(new Exception("Fail"));
```

Hopefully this will be identified as a bug and fixed by the time you read this in the next release of Rx. Out of academic interest, here is a sample of a `Finally` extension method that would work as expected.

```csharp
public static IObservable<T> MyFinally<T>(
    this IObservable<T> source, 
    Action finallyAction)
{
    return Observable.Create<T>(o =>
    {
        var finallyOnce = Disposable.Create(finallyAction);
        var subscription = source.Subscribe(
            o.OnNext,
            ex =>
            {
                try { o.OnError(ex); }
                finally { finallyOnce.Dispose(); }
            },
            () =>
            {
                try { o.OnCompleted(); }
                finally { finallyOnce.Dispose(); }
            });

        return new CompositeDisposable(subscription, finallyOnce);
    });
}
```

### Using							

The `Using` factory method allows you to bind the lifetime of a resource to the lifetime of an observable sequence. The signature itself takes two factory methods; one to provide the resource and one to provide the sequence. This allows everything to be lazily evaluated.

```csharp
public static IObservable<TSource> Using<TSource, TResource>(
    Func<TResource> resourceFactory, 
    Func<TResource, IObservable<TSource>> observableFactory) 
    where TResource : IDisposable
{
    ...
}
```

The `Using` method will invoke both the factories when you subscribe to the sequence. The resource will be disposed of when the sequence is terminated gracefully, terminated erroneously or when the subscription is disposed.

To provide an example, we will reintroduce the `TimeIt` class from [Chapter 3](03_LifetimeManagement.html#IDisposable). I could use this handy little class to time the duration of a subscription. In the next example we create an observable sequence with the `Using` factory method. We provide a factory for a `TimeIt` resource and a function that returns a sequence.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1));
var result = Observable.Using(
    () => new TimeIt("Subscription Timer"),
    timeIt => source);
result.Take(5).Dump("Using");
```

Output:

```
Using --> 0
Using --> 1
Using --> 2
Using --> 3
Using --> 4
Using completed
Subscription Timer took 00:00:05.0138199
```

Due to the `Take(5)` decorator, the sequence completes after five elements and thus the subscription is disposed of. Along with the subscription, the `TimeIt` resource is also disposed of, which invokes the logging of the elapsed time.

This mechanism can find varied practical applications in the hands of an imaginative developer. The resource being an `IDisposable` is convenient; indeed, it makes it so that many types of resources can be bound to, such as other subscriptions, stream reader/writers, database connections, user controls and, with `Disposable.Create(Action)`, virtually anything else.

### OnErrorResumeNext				

Just the title of this section will send a shudder down the spines of old VB developers! In Rx, there is an extension method called `OnErrorResumeNext` that has similar semantics to the VB keywords/statement that share the same name. This extension method allows the continuation of a sequence with another sequence regardless of whether the first sequence completes gracefully or due to an error. Under normal use, the two sequences would merge as below:

```
S1--0--0--|
S2        --0--|
R --0--0----0--|
```

In the event of a failure in the first sequence, then the sequences would still merge:

```
S1--0--0--X
S2        --0--|
R --0--0----0--|
```

The overloads to `OnErrorResumeNext` are as follows:

```csharp
public static IObservable<TSource> OnErrorResumeNext<TSource>(
    this IObservable<TSource> first, 
    IObservable<TSource> second)
{
    ..
}

public static IObservable<TSource> OnErrorResumeNext<TSource>(
    params IObservable<TSource>[] sources)
{
    ...
}

public static IObservable<TSource> OnErrorResumeNext<TSource>(
    this IEnumerable<IObservable<TSource>> sources)
{
    ...
}
```

It is simple to use; you can pass in as many continuations sequences as you like using the various overloads. Usage should be limited however. Just as the `OnErrorResumeNext` keyword warranted mindful use in VB, so should it be used with caution in Rx. It will swallow exceptions quietly and can leave your program in an unknown state. Generally, this will make your code harder to maintain and debug.

### Retry							

If you are expecting your sequence to encounter predictable issues, you might simply want to retry. One such example when you want to retry is when performing I/O (such as web request or disk access). I/O is notorious for intermittent failures. The `Retry` extension method offers the ability to retry on failure a specified number of times or until it succeeds.

```csharp
// Repeats the source observable sequence until it successfully terminates.
public static IObservable<TSource> Retry<TSource>(
    this IObservable<TSource> source)
{
    ...
}

// Repeats the source observable sequence the specified number of times or until it 
//  successfully terminates.
public static IObservable<TSource> Retry<TSource>(
    this IObservable<TSource> source, int retryCount)
{
    ...
}
```

In the diagram below, the sequence (`S`) produces values then fails. It is re-subscribed, after which it produces values and fails again; this happens a total of two times. The result sequence (`R`) is the concatenation of all the successive subscriptions to (`S`).

```
S --1--2--X
            --1--2--3--X
                         --1
R --1--2------1--2--3------1
```

In the next example, we just use the simple overload that will always retry on any exception.

```csharp
public static void RetrySample<T>(IObservable<T> source)
{
    source.Retry().Subscribe(t=>Console.WriteLine(t)); //Will always retry
    Console.ReadKey();
}
```

Given the source [0,1,2,X], the output would be:

```
0
1
2
0
1
2
0
1
2
```

This output would continue forever, as we throw away the token from the subscribe method. As a marble diagram it would look like this:

```
S--0--1--2--x
             --0--1--2--x
                         --0--
R--0--1--2-----0--1--2-----0--
```

Alternatively, we can specify the maximum number of retries. In this example, we only retry once, therefore the error that gets published on the second subscription will be passed up to the final subscription. Note that to retry once you pass a value of 2. Maybe the method should have been called `Try`?

```csharp
source.Retry(2).Dump("Retry(2)"); 
```

Output:

```
Retry(2)-->0
Retry(2)-->1
Retry(2)-->2
Retry(2)-->0
Retry(2)-->1
Retry(2)-->2
Retry(2) failed-->Test Exception
```

As a marble diagram, this would look like:
    
```
S--0--1--2--x
             --0--1--2--x
R--0--1--2-----0--1--2--x
```

Proper care should be taken when using the infinite repeat overload. Obviously if there is a persistent problem with your underlying sequence, you may find yourself stuck in an infinite loop. Also, take note that there is no overload that allows you to specify the type of exception to retry on.

A useful extension method to add to your own library might be a "Back Off and Retry" method. The teams I have worked with have found such a feature useful when performing I/O, especially network requests. The concept is to try, and on failure wait for a given period of time and then try again. Your version of this method may take into account the type of `Exception` you want to retry on, as well as the maximum number of times to retry. You may even want to lengthen the to wait period to be less aggressive on each subsequent retry.

<!--TODO: Build BackOffRetry with the reader-->

Requirements for exception management that go beyond simple `OnError` handlers are commonplace. Rx delivers the basic exception handling operators which you can use to compose complex and robust queries. In this chapter we have covered advanced error handling and some more resource management features from Rx. We looked at the `Catch`, `Finally` and `Using` methods as well as the other methods like `OnErrorResumeNext` and `Retry`, that allow you to play a little 'fast and loose'. We have also revisited the use of marble diagrams to help us visualize the combination of multiple sequences. This will help us in our next chapter where we will look at other ways of composing and aggregating observable sequences.