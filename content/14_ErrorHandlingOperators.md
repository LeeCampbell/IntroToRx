---
title: Error Handling Operators
---

Exceptions happen. Some exceptions are inherently avoidable, occurring only because of bugs in our code. For example, we shouldn't normally put the CLR into a situation where it has to raise a `DivideByZeroException`. But there are plenty of exceptions that cannot be prevented with defensive coding. For example, exceptions relating to I/O or networking failures such as like `FileNotFoundException` or `TimeoutException` can be caused by environmental factors outside of our code's control. In these cases, we need to handle the exception gracefully. The kind of handling will depend on the context—it might be appropriate to provide some sort of error message to the user; in some scenarios logging the error might be a more appropriate response. If the failure is likely to be transient, we could try to recover by retrying the operation that failed.

The `IObserver<T>` interface defines the `OnError` method so that a source can report an error, but since this terminates the sequence, it provides no direct means of working out what to do next. However, Rx provides operators that provide a variety of error handling mechanisms.

## Catch

Rx defines a `Catch` operator. The name is deliberately reminiscent of C#'s `try`/`catch` syntax because it lets you handle errors from an Rx source in a similar way to exceptions that emerge from normal execution of code. It gives you the option of swallowing an exception, wrapping it in another exception or performing some other logic.

### Swallowing exceptions

The most basic (although rarely the best) way to handle an exception is to swallow it. In C#, we could write a `try` block with an empty `catch` block. We can achieve something similar with Rx's `Catch` operator. The basic idea with swallowing exceptions is that the process that caused the exception stops, but we act as though nothing had happened—we handle it in the same way as it the process had naturally reached an end. We can represent an exception being swallowed like this with a marble diagram.

```
S1--1--2--3--X
S2            -|
R --1--2--3----|
```

Here `S1` represents the first sequence that ends with an error (`X`). If we're swallowing the exception, we want to make it look like the sequence just came to a normal halt. So `S2` here is an empty sequence we will substitute when the first throws an exception. `R` is the result sequence which starts as `S1`, then continues with `S2` when `S1` fails. This code creates the scenario described in that marble diagram:

```cs
var source = new Subject<int>();
IObservable<int> result = source.Catch(Observable.Empty<int>());

result.Dump("Catch"););

source.OnNext(1);
source.OnNext(2);
source.OnNext(3);
source.OnError(new Exception("Fail!"));
```

Output:

```
Catch-->1
Catch-->2
Catch-->3
Catch completed
```

This is conceptually similar to the following code:

```cs
try
{
    DoSomeWork();
}
catch
{
}
```

This kind of catch-and-ignore everything handling is generally discouraged in C#, and you probably also want to limit your use of its equivalent in Rx.

### Swallowing only specific exception types

It's much more common to want to handle a specific exception like this:

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
`Catch` has an overload that enables you specify the type of exception:

```cs
public static IObservable<TSource> Catch<TSource, TException>(
    this IObservable<TSource> source, 
    Func<TException, IObservable<TSource>> handler) 
    where TException : Exception
```

This enables us to write the Rx equivalent to the more selective `catch`, where we only swallow a `TimeoutException`:

```cs
IObservable<int> result = source.Catch<int, TimeoutException>(_ => Observable.Empty());
```

If the sequence was to terminate with an `Exception` that could not be cast to a `TimeoutException`, then the error would not be caught and would flow through to the subscriber.

### Examining the exception

Notice that with the overload in the preceding example, we supplied a callback. If an exception of the specified type emerges, this overload of `Catch` will pass it to our callback so that if necessary, we can decide exactly what to return based on information in the exception. If you were to decide that, having inspected the exception, you don't want to swallow it after all, you can use `Observable.Throw` to return an observable that rethrows the exception. (This is effectively the Rx equivalent to a `throw;` statement inside a C# `catch` block.) The following example uses this to swallow all IO exceptions of type IOException or any type derived from that except for `FileNotFoundException`.

```cs
IObservable<int> result = source.Catch<int, IOException>(
    x => x is FileNotFoundException ? Observable.Throw(tx) : Observable.Empty());
```

### Replacing an exception

So far all the examples using `Catch` have either swallowed the exception by returning `Observable.Empty` or rethrown it with `Observable.Throw`. We can supply any observable we want to `Catch` (as long as its item type matches the source item type). We could use this to replace an exception with, say, a message:

```cs
IObservable<string> messages = stringSource.Catch<string, IOException>(
    x => Observable.Return(
        x is FileNotFoundException fnf
        ? $"Did not find {fnf.FileName}"
        : $"Was not expecting exception {tx.GetType().Name}"));
```

Alternatively, we could handle certain source exceptions by throwing a new exception, with the original one as an inner exception:

```cs
IObservable<int> result = source.Catch<int, IOException>(
    x => Observable.Throw<int>(
        x is FileNotFoundException fnf
        ? new InvalidOperationException("Config not available", fnf)
        : x));
```

No matter what your `Catch` does, remember that you'll see no new items from the source after it produces an error. The basic rules of Rx mean that once a source has called `OnError` on its subscriber, it must not make any further calls.

## Finally

Similar to a `finally` block in C#, Rx enables us to execute some code on completion of a sequence, regardless of whether it runs to completion naturally or fails. The `Finally` extension method accepts an `Action` as a parameter. This `Action` will be invoked when the sequence terminates, regardless of whether `OnCompleted` or `OnError` was called. It will also invoke the action if the subscription is disposed of before it completes.

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

Note that if the subscriber's `OnError` throws an exception, and if the source calls `OnNext` without a `try`/`catch` block, the CLR's unhandled exception reporting mechanism kicks in, and in some circumstances this can result in the application shutting down before the `Finally` operator has had an opportunity to invoke the callback. We can create this scenario with the following code:

```cs
var source = new Subject<int>();
var result = source.Finally(() => Console.WriteLine("Finally"));
result.Subscribe(
    Console.WriteLine,
    // Console.WriteLine,
    () => Console.WriteLine("Completed"));
source.OnNext(1);
source.OnNext(2);
source.OnNext(3);

// Brings the app down. Finally action might not be called.
source.OnError(new Exception("Fail"));
```

If you run this directly from the program's entry point, without wrapping it in a `try`/`catch`, you may or may not see the `Finally` message displayed, because exception handling works subtly differently in the case an exception reaches all the way to the top of the stack without being caught. (Oddly, it usually does run, but if you have a debugger attached, the program usually exits without running the `Finally` callback.)

This is mostly just a curiosity: application frameworks such as ASP.NET Core or WPF typically install their own top-of-stack exception handlers, and in any case you shouldn't be subscribing to a source that you know will call `OnError` without supplying an error callback. This problem only emerges because the delegate-based `Subscribe` overload in use here supplies an `IObserver<T>` implementation that throws in its `OnError`. However, if you're building console applications to experiment with Rx's behaviour you are quite likely to run into this. In practice, `Finally` will do the right thing in more normal situations. (But in any case, you shouldn't throw exceptions from an `OnError` handler.)

## Using

The `Using` factory method allows you to bind the lifetime of a resource to the lifetime of an observable sequence. The signature itself takes two callbacks; one to create the disposable resource and one to provide the sequence. This allows everything to be lazily evaluated—these callbacks are invoked when code calls `Subscribe` on the `IObservable<T>` this method returns.

```csharp
public static IObservable<TSource> Using<TSource, TResource>(
    Func<TResource> resourceFactory, 
    Func<TResource, IObservable<TSource>> observableFactory) 
    where TResource : IDisposable
{
    ...
}
```

The resource will be disposed of when the sequence terminates either with `OnCompleted` or `OnError`, or when the subscription is disposed.

## OnErrorResumeNext

Just the title of this section will send a shudder down the spines of old VB developers! (For those not familiar with this murky language feature, the VB language lets you instruct it to ignore any errors that occur during execution, and to just continue with the next statement after any failure.) In Rx, there is an extension method called `OnErrorResumeNext` that has similar semantics to the VB keywords/statement that share the same name. This extension method allows the continuation of a sequence with another sequence regardless of whether the first sequence completes gracefully or due to an error. Under normal use, the two sequences would merge as below:

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

## Retry

If you are expecting your sequence to encounter predictable failures, you might simply want to retry. For example, if you are running in a cloud environment, it's very common for operations to fail occasionally for no obvious reason. Cloud platforms often relocate services on a fairly regular basis for operational reasons, which means it's not unusual for operations to fail—you might make a request to a service just before the cloud provider decided to move that service to a different compute node—but for the exact same operation to succeed if you immediately retry it (because the retry request gets routed to the new node). Rx's `Retry` extension method offers the ability to retry on failure a specified number of times or until it succeeds. This works by resubscribing to the source if it reports an error.

This example uses the simple overload, which will always retry on any exception.

```csharp
public static void RetrySample<T>(IObservable<T> source)
{
    source.Retry().Subscribe(t => Console.WriteLine(t)); // Will always retry
    Console.ReadKey();
}
```

Given a source that produces the values 0, 1, and 2, and then calls `OnError`, the output would be the numbers 0, 1, 2 repeating over an over endlessly. This output would continue forever because this example never unsubscribes, and `Retry` will retry forever if you don't tell it otherwise.

We can specify the maximum number of retries. In this next example, we only retry once, therefore the error that gets published on the second subscription will be passed up to the final subscription. Note that we tell `Retry` the maximum number of attempts, so if we want it to retry once, you pass a value of 2—that's the initial attempt plus one retry.

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

<!--TODO: Build BackOffRetry with the reader-->

Requirements for exception management that go beyond simple `OnError` handlers are commonplace. Rx delivers the basic exception handling operators which you can use to compose complex and robust queries. In this chapter we have covered advanced error handling and some more resource management features from Rx. We looked at the `Catch`, `Finally` and `Using` methods as well as the other methods like `OnErrorResumeNext` and `Retry`, that allow you to respond to error scenarios with something more constructive than just terminating.