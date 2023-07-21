---
title: Leaving the monad
---

# Leaving the monad	

An observable sequence is a useful construct, especially when we have the power of LINQ to compose complex queries over it. Even though we recognize the benefits of the observable sequence, sometimes it is required to leave the `IObservable<T>` paradigm for another paradigm, maybe to enable you to integrate with an existing API (i.e. use events or `Task<T>`). You might leave the observable paradigm if you find it easier for testing, or it may simply be easier for you to learn Rx by moving between an observable paradigm and a more familiar one.

## What is a monad					

We have casually referred to the term _monad_ earlier in the book, but to most it will be a very foreign term. 
I am going to try to avoid overcomplicating what a monad is, but give enough of an explanation to help us out with our next category of methods. 
The full definition of a monad is quite abstract. 
[Many others](http://www.haskell.org/haskellwiki/Monad_tutorials_timeline) have tried to provide their definition of a monad using all sorts of metaphors from astronauts to Alice in Wonderland. 
Many of the tutorials for monadic programming use Haskell for the code examples which can add to the confusion.
For us, a monad is effectively a programming structure that represents computations.
Compare this to other programming structures:

<dl>
    <dt>Data structure</dt>
    <dd>
        Purely state e.g. a List, a Tree or a Tuple
    </dd>
    <dt>Contract</dt>
    <dd>
        Contract definition or abstract functionality e.g. an interface or abstract class
    </dd>
    <dt>Object-Orientated structure</dt>
    <dd>
        State and behavior together
    </dd>
</dl>

Generally a monadic structure allows you to chain together operators to produce a pipeline, just as we do with our extension methods.

<cite>Monads are a kind of abstract data type constructor that encapsulate program logic instead of data in the domain model. </cite>

This neat definition of a monad lifted from Wikipedia allows us to start viewing sequences as monads; the abstract data type in this case is the `IObservable<T>` type. When we use an observable sequence, we compose functions onto the abstract data type (the `IObservable<T>`) to create a query. This query becomes our encapsulated programming logic.

The use of monads to define control flows is particularly useful when dealing with typically troublesome areas of programming such as IO, concurrency and exceptions. This just happens to be some of Rx's strong points!

## Why leave the monad?				

There is a variety of reasons you may want to consume an observable sequence in a different paradigm. Libraries that need to expose functionality externally may be required to present it as events or as `Task` instances. In demonstration and sample code you may prefer to use blocking methods to limit the number of asynchronous moving parts. This may help make the learning curve to Rx a little less steep!

In production code, it is rarely advised to 'break the monad', especially moving from an observable sequence to blocking methods. Switching between asynchronous and synchronous paradigms should be done with caution, as this is a common root cause for concurrency problems such as deadlock and scalability issues.

In this chapter, we will look at the methods in Rx which allow you to leave the `IObservable<T>` monad.

## ForEach							

The `ForEach` method provides a way to process elements as they are received. The key difference between `ForEach` and `Subscribe` is that `ForEach` will block the current thread until the sequence completes.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1))
                       .Take(5);
source.ForEach(i => Console.WriteLine("received {0} @ {1}", i, DateTime.Now));
Console.WriteLine("completed @ {0}", DateTime.Now);
```

Output:

```
received 0 @ 01/01/2012 12:00:01 a.m.
received 1 @ 01/01/2012 12:00:02 a.m.
received 2 @ 01/01/2012 12:00:03 a.m.
received 3 @ 01/01/2012 12:00:04 a.m.
received 4 @ 01/01/2012 12:00:05 a.m.
completed @ 01/01/2012 12:00:05 a.m.
```

Note that the completed line is last, as you would expect. To be clear, you can get similar functionality from the `Subscribe` extension method, but the `Subscribe` method will not block. So if we substitute the call to `ForEach` with a call to `Subscribe`, we will see the completed line happen first.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1))
                       .Take(5);
source.Subscribe(i => Console.WriteLine("received {0} @ {1}", i, DateTime.Now));
Console.WriteLine("completed @ {0}", DateTime.Now);
```

Output:

```
completed @ 01/01/2012 12:00:00 a.m.
received 0 @ 01/01/2012 12:00:01 a.m.
received 1 @ 01/01/2012 12:00:02 a.m.
received 2 @ 01/01/2012 12:00:03 a.m.
received 3 @ 01/01/2012 12:00:04 a.m.
received 4 @ 01/01/2012 12:00:05 a.m.
```

Unlike the `Subscribe` extension method, `ForEach` has only the one overload; the one that take an `Action<T>` as its single argument. In contrast, previous (pre-release) versions of Rx, the `ForEach` method had most of the same overloads as `Subscribe`. Those overloads of `ForEach` have been deprecated, and I think rightly so. There is no need to have an `OnCompleted` handler in a synchronous call, it is unnecessary. You can just place the call immediately after the `ForEach` call as we have done above. Also, the `OnError` handler can now be replaced with standard Structured Exception Handling like you would use for any other synchronous code, with a `try`/`catch` block. This also gives symmetry to the `ForEach` instance method on the `List<T>` type.

```csharp
var source = Observable.Throw<int>(new Exception("Fail"));

try
{
    source.ForEach(Console.WriteLine);
}
catch (Exception ex)
{
    Console.WriteLine("error @ {0} with {1}", DateTime.Now, ex.Message);
}
finally
{
    Console.WriteLine("completed @ {0}", DateTime.Now);    
}
```

Output:

```
error @ 01/01/2012 12:00:00 a.m. with Fail
completed @ 01/01/2012 12:00:00 a.m.
```

The `ForEach` method, like its other blocking friends (`First` and `Last` etc.), should be used with care. 
I would leave the `ForEach` method for spikes, tests and demo code only. 
We will discuss the problems with introducing blocking calls when we look at concurrency.

<!--TODO: The  GetEnumerator, Latest, MostRecent and Next operators are not covered. These could be really useful.-->
<!--<a name="ObservableSequencesToEnumerators"></a>
    <h2>Observable sequences to enumerators</h2>
    <p></p>
    <a name="GetEnumerator"></a>
    <h3>GetEnumerator</h3>
    <p></p>
    <a name="Latest"></a>
    <h3>Latest</h3>
    <p></p>
    <a name="MostRecent"></a>
    <h3>MostRecent</h3>
    <p></p>
    <a name="Next"></a>
    <h3>Next</h3>
    <p></p>
-->

## ToEnumerable						

An alternative way to switch out of the `IObservable<T>` is to call the `ToEnumerable` extension method. 
As a simple example:

```csharp
var period = TimeSpan.FromMilliseconds(200);
var source = Observable.Timer(TimeSpan.Zero, period) 
                       .Take(5); 

var result = source.ToEnumerable();

foreach (var value in result) 
{ 
    Console.WriteLine(value); 
} 

Console.WriteLine("done");
```

Output:

```
0
1
2
3
4
done
```

The source observable sequence will be subscribed to when you start to enumerate the sequence (i.e. lazily). 
In contrast to the `ForEach` extension method, using the `ToEnumerable` method means you are only blocked when you try to move to the next element and it is not available. 
Also, if the sequence produces values faster than you consume them, they will be cached for you.

To cater for errors, you can wrap your `foreach` loop in a `try`/`catch` as you do with any other enumerable sequence:

```csharp
try 
{ 
    foreach (var value in result)
    { 
        Console.WriteLine(value); 
    } 
} 
catch (Exception e) 
{ 
    Console.WriteLine(e.Message);
} 
```

As you are moving from a push to a pull model (non-blocking to blocking), the standard warning applies.

## To a single collection			

To avoid having to oscillate between push and pull, you can use one of the next four methods to get the entire list back in a single notification. 
They all have the same semantics, but just produce the data in a different format. 
They are similar to their corresponding `IEnumerable<T>` operators, but the return values differ in order to retain asynchronous behavior.

### ToArray and ToList				

Both `ToArray` and `ToList` take an observable sequence and package it into an array or an instance of `List<T>` respectively. 
Once the observable sequence completes, the array or list will be pushed as the single value of the result sequence.

```csharp
var period = TimeSpan.FromMilliseconds(200); 
var source = Observable.Timer(TimeSpan.Zero, period).Take(5); 
var result = source.ToArray(); 

result.Subscribe( 
    arr => { 
        Console.WriteLine("Received array"); 
        foreach (var value in arr) 
        { 
            Console.WriteLine(value); 
        } 
    }, 
    () => Console.WriteLine("Completed")
); 

Console.WriteLine("Subscribed"); 
```

Output:

```
Subscribed
Received array
0
1
2
3
4
Completed
```

As these methods still return observable sequences we can use our `OnError` handler for errors. Note that the source sequence is packaged to a single notification; you either get the whole sequence *or* the error. If the source produces values and then errors, you will not receive any of those values. All four operators (`ToArray`, `ToList`, `ToDictionary` and `ToLookup`) handle errors like this.

### ToDictionary and ToLookup	

As an alternative to arrays and lists, Rx can package an observable sequence into a dictionary or lookup with the `ToDictionary` and `ToLookup` methods. Both methods have the same semantics as the `ToArray` and `ToList` methods, as they return a sequence with a single value and have the same error handling features.

The `ToDictionary` extension method overloads:

```csharp
// Creates a dictionary from an observable sequence according to a specified key selector 
// function, a comparer, and an element selector function.
public static IObservable<IDictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector, 
    IEqualityComparer<TKey> comparer) 
{...} 

// Creates a dictionary from an observable sequence according to a specified key selector 
// function, and an element selector function. 
public static IObservable<IDictionary<TKey, TElement>> ToDictionary<TSource, TKey, TElement>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector) 
{...} 

// Creates a dictionary from an observable sequence according to a specified key selector 
// function, and a comparer. 
public static IObservable<IDictionary<TKey, TSource>> ToDictionary<TSource, TKey>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector,
    IEqualityComparer<TKey> comparer) 
{...} 

// Creates a dictionary from an observable sequence according to a specified key selector 
// function. 
public static IObservable<IDictionary<TKey, TSource>> ToDictionary<TSource, TKey>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector) 
{...} 
```

The `ToLookup` extension method overloads:

```csharp
// Creates a lookup from an observable sequence according to a specified key selector 
// function, a comparer, and an element selector function. 
public static IObservable<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector,
    IEqualityComparer<TKey> comparer) 
{...} 

// Creates a lookup from an observable sequence according to a specified key selector 
// function, and a comparer. 
public static IObservable<ILookup<TKey, TSource>> ToLookup<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    IEqualityComparer<TKey> comparer) 
{...} 

// Creates a lookup from an observable sequence according to a specified key selector 
// function, and an element selector function. 
public static IObservable<ILookup<TKey, TElement>> ToLookup<TSource, TKey, TElement>( 
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector)
{...} 

// Creates a lookup from an observable sequence according to a specified key selector 
// function. 
public static IObservable<ILookup<TKey, TSource>> ToLookup<TSource, TKey>( 
    this IObservable<TSource> source, 
    Func<TSource,
    TKey> keySelector) 
{...} 
```

Both `ToDictionary` and `ToLookup` require a function that can be applied each value to get its key. In addition, the `ToDictionary` method overloads mandate that all keys should be unique. If a duplicate key is found, it terminate the sequence with a `DuplicateKeyException`. On the other hand, the `ILookup<TKey, TElement>` is designed to have multiple values grouped by the key. If you have many values per key, then `ToLookup` is probably the better option.

## ToTask							

We have compared `AsyncSubject<T>` to `Task<T>` and even showed how to [transition from a task](04_CreatingObservableSequences.html#FromTask) to an observable sequence. The `ToTask` extension method will allow you to convert an observable sequence into a `Task<T>`. Like an `AsyncSubject<T>`, this method will ignore multiple values, only returning the last value.

```csharp
// Returns a task that contains the last value of the observable sequence. 
public static Task<TResult> ToTask<TResult>(
    this IObservable<TResult> observable) 
{...}

// Returns a task that contains the last value of the observable sequence, with state to 
// use as the underlying task's AsyncState. 
public static Task<TResult> ToTask<TResult>(
    this IObservable<TResult> observable,
    object state) 
{...} 

// Returns a task that contains the last value of the observable sequence. Requires a 
// cancellation token that can be used to cancel the task, causing unsubscription from 
// the observable sequence. 
public static Task<TResult> ToTask<TResult>(
    this IObservable<TResult> observable, 
    CancellationToken cancellationToken) 
{...} 

// Returns a task that contains the last value of the observable sequence, with state to 
// use as the underlying task's AsyncState. Requires a cancellation token that can be used
// to cancel the task, causing unsubscription from the observable sequence. 
public static Task<TResult> ToTask<TResult>(
    this IObservable<TResult> observable, 
    CancellationToken cancellationToken, 
    object state) 
{...} 
```

This is a simple example of how the `ToTask` operator can be used. 
Note, the `ToTask` method is in the `System.Reactive.Threading.Tasks` namespace.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1)) 
                       .Take(5);
var result = source.ToTask(); //Will arrive in 5 seconds. 
Console.WriteLine(result.Result);
```

Output:

```
4
```

If the source sequence was to manifest error then the task would follow the error-handling semantics of tasks.

```csharp
var source = Observable.Throw<long>(new Exception("Fail!")); 
var result = source.ToTask();

try 
{ 
    Console.WriteLine(result.Result);
} 
catch (AggregateException e) 
{ 
    Console.WriteLine(e.InnerException.Message); 
}
```

Output:

```
Fail!
```

Once you have your task, you can of course engage in all the features of the TPL such as continuations.

## ToEvent&lt;T&gt;					

Just as you can use an event as the source for an observable sequence with [`FromEventPattern`](04_CreatingObservableSequences.html#FromEvent), you can also make your observable sequence look like a standard .NET event with the `ToEvent` extension methods.

```csharp
// Exposes an observable sequence as an object with a .NET event. 
public static IEventSource<unit> ToEvent(this IObservable<Unit> source)
{...} 

// Exposes an observable sequence as an object with a .NET event. 
public static IEventSource<TSource> ToEvent<TSource>(
    this IObservable<TSource> source) 
{...} 

// Exposes an observable sequence as an object with a .NET event. 
public static IEventPatternSource<TEventArgs> ToEventPattern<TEventArgs>(
    this IObservable<EventPattern<TEventArgs>> source) 
    where TEventArgs : EventArgs 
{...} 
```

The `ToEvent` method returns an `IEventSource<T>`, which will have a single event member on it: `OnNext`.

```csharp
public interface IEventSource<T> 
{ 
    event Action<T> OnNext; 
} 
```

When we convert the observable sequence with the `ToEvent` method, we can just subscribe by providing an `Action<T>`, which we do here with a lambda.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1))
                        .Take(5); 
var result = source.ToEvent(); 
result.OnNext += val => Console.WriteLine(val);
```

Output:

```
0
1
2
3
4
```

### ToEventPattern					

Note that this does not follow the standard pattern of events. Normally, when you subscribe to an event, you need to handle the `sender` and `EventArgs` parameters. In the example above, we just get the value. If you want to expose your sequence as an event that follows the standard pattern, you will need to use `ToEventPattern`.

The `ToEventPattern` will take an `IObservable<EventPattern<TEventArgs>>` and convert that into an `IEventPatternSource<TEventArgs>`. The public interface for these types is quite simple.

```csharp
public class EventPattern<TEventArgs> : IEquatable<EventPattern<TEventArgs>>
    where TEventArgs : EventArgs 
{ 
    public EventPattern(object sender, TEventArgs e)
    { 
        this.Sender = sender; 
        this.EventArgs = e; 
    } 
    public object Sender { get; private set; } 
    public TEventArgs EventArgs { get; private set; } 
    //...equality overloads
} 

public interface IEventPatternSource<TEventArgs> where TEventArgs : EventArgs
{ 
    event EventHandler<TEventArgs> OnNext; 
} 
```

These look quite easy to work with. So if we create an `EventArgs` type and then apply a simple transform using `Select`, we can make a standard sequence fit the pattern.

The `EventArgs` type:

```csharp
public class MyEventArgs : EventArgs 
{ 
    private readonly long _value; 
    
    public MyEventArgs(long value) 
    { 
        _value = value; 
    } 

    public long Value 
    { 
        get { return _value; } 
    } 
} 
```

The transform:

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1))
                        .Select(i => new EventPattern<MyEventArgs>(this, new MyEventArgs(i)));
```

Now that we have a sequence that is compatible, we can use the `ToEventPattern`, and in turn, a standard event handler.

```csharp
var result = source.ToEventPattern(); 
result.OnNext += (sender, eventArgs) => Console.WriteLine(eventArgs.Value);
```

Now that we know how to get back into .NET events, let's take a break and remember why Rx is a better model.

- In C#, events have a curious interface. Some find the `+=` and `-=` operators an unnatural way to register a callback
- Events are difficult to compose
- Events do not offer the ability to be easily queried over time
- Events are a common cause of accidental memory leaks
- Events do not have a standard pattern for signaling completion
- Events provide almost no help for concurrency or multithreaded applications. For instance, raising an event on a separate thread requires you to do all of the plumbing

The set of methods we have looked at in this chapter complete the circle started in the [Creating a Sequence](04_CreatingObservableSequences.html#TransitioningIntoIObservable) chapter. We now have the means to enter and leave the observable sequence monad. Take care when opting in and out of the `IObservable<T>` monad. Doing so excessively can quickly make a mess of your code base, and may indicate a design flaw.



...
This next content was originally in the intro to Part 3, which preceded a chapter called Side Effects. I don't think that is a chapter in its own right, because there are concerns around side effects in a few places.


# Side effects						

Non-functional requirements of production systems often demand high availability, quality monitoring features and low lead time for defect resolution. Logging, debugging, instrumentation and journaling are common non-functional requirements that developers need to consider for production ready systems. These artifacts could be considered side effects of the main business workflow. Side effects are a real life problem that code samples and how-to guides often ignore, however Rx provides tools to help.

In this chapter we will discuss the consequences of introducing side effects when working with an observable sequence. A function is considered to have a side effect if, in addition to any return value, it has some other observable effect. Generally the 'observable effect' is a modification of state. This observable effect could be

* modification of a variable with a wider scope than the function (i.e. global, static	or perhaps an argument)
* I/O such as a read/write from a file or network
<!--TODO:Validate that readers see the display as an I/O device or not?-->
* updating a display
<!--TODO: Are there other existing paradigms that allow you to modify state, either safely or explicitly-->

## Issues with side effects			

Functional programming in general tries to avoid creating any side effects. Functions with side effects, especially which modify state, require the programmer to understand more than just the inputs and outputs of the function. The surface area they are required to understand needs to now extend to the history and context of the state being modified. This can greatly increase the complexity of a function, and thus make it harder to correctly understand and maintain.

Side effects are not always accidental, nor are they always intentional. An easy way to reduce the accidental side effects is to reduce the surface area for change. The simple actions coders can take are to reduce the visibility or scope of state and to make what you can immutable. You can reduce the visibility of a variable by scoping it to a code block like a method. You can reduce visibility of class members by making them private or protected. By definition immutable data can't be modified so cannot exhibit side effects. These are sensible encapsulation rules that will dramatically improve the maintainability of your Rx code.

To provide a simple example of a query that has a side effect, we will try to output the index and value of the elements received by updating a variable (closure).

```csharp
var letters = Observable.Range(0, 3)
                        .Select(i => (char)(i + 65));

var index = -1;
var result = letters.Select(
    c =>
    {
        index++;
        return c;
    });

result.Subscribe(
    c => Console.WriteLine("Received {0} at index {1}", c, index),
    () => Console.WriteLine("completed"));
```

Output:

```
Received A at index 0
Received B at index 1
Received C at index 2
completed
```

While this seems harmless enough, imagine if another person sees this code and understands it to be the pattern the team is using. They in turn adopt this style themselves. For the sake of the example, we will add a duplicate subscription to our previous example.

```csharp
var letters = Observable.Range(0, 3)
                        .Select(i => (char)(i + 65));

var index = -1;
var result = letters.Select(
    c =>
    {
        index++;
        return c;
    });

result.Subscribe(
    c => Console.WriteLine("Received {0} at index {1}", c, index),
    () => Console.WriteLine("completed"));

result.Subscribe(
    c => Console.WriteLine("Also received {0} at index {1}", c, index),
    () => Console.WriteLine("2nd completed"));
```

Output

```
Received A at index 0
Received B at index 1
Received C at index 2
completed
Also received A at index 3
Also received B at index 4
Also received C at index 5
2nd completed
```

<!--TODO: Apply forward reference. Where do we show better ways of controlling workflow?-->

Now the second person's output is clearly nonsense. They will be expecting index values to be 0, 1 and 2 but get 3, 4 and 5 instead. I have seen far more sinister versions of side effects in code bases. The nasty ones often modify state that is a Boolean value e.g. `hasValues`, `isStreaming` etc. We will see in a later chapter far better ways of controlling workflow with observable sequences than using shared state.

In addition to creating potentially unpredictable results in existing software, programs that exhibit side effects are far more difficult to test and maintain. Future refactoring, enhancements or other maintenance on programs that exhibits side effects are far more likely to be brittle. This is especially so in asynchronous or concurrent software.

## Composing data in a pipeline		

The preferred way of capturing state is to introduce it to the pipeline. Ideally, we want each part of the pipeline to be independent and deterministic. That is, each function that makes up the pipeline should have its inputs and output as its only state. To correct our example we could enrich the data in the pipeline so that there is no shared state. This would be a great example where we could use the `Select` overload that exposes the index.

```csharp
var source = Observable.Range(0, 3);
var result = source.Select((idx, value) => new
             {
                 Index = idx,
                 Letter = (char) (value + 65)
             });

result.Subscribe(
    x => Console.WriteLine("Received {0} at index {1}", x.Letter, x.Index),
    () => Console.WriteLine("completed"));

result.Subscribe(
    x => Console.WriteLine("Also received {0} at index {1}", x.Letter, x.Index),
    () => Console.WriteLine("2nd completed"));
```

Output:

```
Received A at index 0
Received B at index 1
Received C at index 2
completed
Also received A at index 0
Also received B at index 1
Also received C at index 2
2nd completed
```

Thinking outside of the box, we could also use other features like `Scan` to achieve similar results. Here is an example.

```csharp
var result = source.Scan(
                new
                {
                    Index = -1,
                    Letter = new char()
                },
                (acc, value) => new
                {
                    Index = acc.Index + 1,
                    Letter = (char)(value + 65)
                });
```

The key here is to isolate the state, and reduce or remove any side effects like mutating state.

## Do								

We should aim to avoid side effects, but in some cases it is unavoidable. The `Do` extension method allows you to inject side effect behavior. The signature of the `Do` extension method looks very much like the `Select` method;

- They both have various overloads to cater for combinations of `OnNext`, `OnError` and `OnCompleted` handlers
- They both return and take an observable sequence

The overloads are as follows:

```csharp
// Invokes an action with side effecting behavior for each element in the observable 
// sequence.
public static IObservable<TSource> Do<TSource>(
    this IObservable<TSource> source, 
    Action<TSource> onNext)
{...}

// Invokes an action with side effecting behavior for each element in the observable 
// sequence and invokes an action with side effecting behavior upon graceful termination
// of the observable sequence.
public static IObservable<TSource> Do<TSource>(
    this IObservable<TSource> source, 
    Action<TSource> onNext, 
    Action onCompleted)
{...}

// Invokes an action with side effecting behavior for each element in the observable
// sequence and invokes an action with side effecting behavior upon exceptional 
// termination of the observable sequence.
public static IObservable<TSource> Do<TSource>(
    this IObservable<TSource> source, 
    Action<TSource> onNext, 
    Action<Exception> onError)
{...}

// Invokes an action with side effecting behavior for each element in the observable
// sequence and invokes an action with side effecting behavior upon graceful or
// exceptional termination of the observable sequence.
public static IObservable<TSource> Do<TSource>(
    this IObservable<TSource> source, 
    Action<TSource> onNext, 
    Action<Exception> onError, 
    Action onCompleted)
{...}

// Invokes the observer's methods for their side effects.
public static IObservable<TSource> Do<TSource>(
    this IObservable<TSource> source, 
    IObserver<TSource> observer)
{...}
```

The `Select` overloads take `Func` arguments for their `OnNext` handlers and also provide the ability to return an observable sequence that is a different type to the source. In contrast, the `Do` methods only take an `Action<T>` for the `OnNext` handler, and therefore can only return a sequence that is the same type as the source. As each of the arguments that can be passed to the `Do` overloads are actions, they implicitly cause side effects.

<!--TODO: Maybe guide the user better here so they will follow the path that Actions=side effects-->

For the next example, we first define the following methods for logging:

```csharp
private static void Log(object onNextValue)
{
    Console.WriteLine("Logging OnNext({0}) @ {1}", onNextValue, DateTime.Now);
}
private static void Log(Exception onErrorValue)
{
    Console.WriteLine("Logging OnError({0}) @ {1}", onErrorValue, DateTime.Now);
}
private static void Log()
{
    Console.WriteLine("Logging OnCompleted()@ {0}", DateTime.Now);
}
```

This code can use `Do` to introduce some logging using the methods from above.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1))
                       .Take(3);

var result = source.Do(
    i => Log(i),
    ex => Log(ex),
    () => Log());

result.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("completed"));
```

Output:

```
Logging OnNext(0) @ 01/01/2012 12:00:00
0
Logging OnNext(1) @ 01/01/2012 12:00:01
1
Logging OnNext(2) @ 01/01/2012 12:00:02
2
Logging OnCompleted() @ 01/01/2012 12:00:02
completed
```

Note that because the `Do` is earlier in the query chain than the `Subscribe`, it will receive the values first and therefore write to the console first. I like to think of the `Do` method as a [wire tap](http://en.wikipedia.org/wiki/Telephone_tapping) to a sequence. It gives you the ability to listen in on the sequence, without the ability to modify it.

The most common acceptable side effect I see in Rx is the need to log. The signature of `Do` allows you to inject it into a query chain. This allows us to add logging into our sequence and retain encapsulation. When a repository, service agent or provider exposes an observable sequence, they have the ability to add their side effects (e.g. logging) to the sequence before exposing it publicly. Consumers can then append operators to the query (e.g. `Where`, `SelectMany`) and this will not affect the logging of the provider.

Consider the method below. It produces numbers but also logs what it produces (to the console for simplicity). To the consuming code the logging is transparent.

```csharp
private static IObservable<long> GetNumbers()
{
    return Observable.Interval(TimeSpan.FromMilliseconds(250))
        .Do(i => Console.WriteLine("pushing {0} from GetNumbers", i));
}
```

We then call it with this code.

```csharp
var source = GetNumbers();
var result = source.Where(i => i%3 == 0)
    .Take(3)
    .Select(i => (char) (i + 65));

result.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("completed"));
```

Output:

```
pushing 0 from GetNumbers
A
pushing 1 from GetNumbers
pushing 2 from GetNumbers
pushing 3 from GetNumbers
D
pushing 4 from GetNumbers
pushing 5 from GetNumbers
pushing 6 from GetNumbers
G
completed
```

This example shows how producers or intermediaries can apply logging to the sequence regardless of what the end consumer does.

One overload to `Do` allows you to pass in an `IObserver<T>`. In this overload, each of the `OnNext`, `OnError` and `OnCompleted` methods are passed to the other `Do` overload as each of the actions to perform.

Applying a side effect adds complexity to a query. If side effects are a necessary evil, then being explicit will help your fellow coder understand your intentions. Using the `Do` method is the favored approach to doing so. This may seem trivial, but given the inherent complexity of a business domain mixed with asynchrony and concurrency, developers don't need the added complication of side effects hidden in a `Subscribe` or `Select` operator.

## Encapsulating with AsObservable			

Poor encapsulation is a way developers can leave the door open for unintended side effects. Here is a handful of scenarios where carelessness leads to leaky abstractions. Our first example may seem harmless at a glance, but has numerous problems.

```csharp
public class UltraLeakyLetterRepo
{
    public ReplaySubject<string> Letters { get; set; }

    public UltraLeakyLetterRepo()
    {
        Letters = new ReplaySubject<string>();
        Letters.OnNext("A");
        Letters.OnNext("B");
        Letters.OnNext("C");
    }
}
```

In this example we expose our observable sequence as a property. The first problem here is that it is a settable property. Consumers could change the entire subject out if they wanted. This would be a very poor experience for other consumers of this class. If we make some simple changes we can make a class that seems safe enough.

```csharp
public class LeakyLetterRepo
{
    private readonly ReplaySubject<string> _letters;

    public LeakyLetterRepo()
    {
        _letters = new ReplaySubject<string>();
        _letters.OnNext("A");
        _letters.OnNext("B");
        _letters.OnNext("C");
    }
    
    public ReplaySubject<string> Letters
    {
        get { return _letters; }
    }
}
```

Now the `Letters` property only has a getter and is backed by a read-only field. This is much better. Keen readers will note that the `Letters` property returns a `ReplaySubject<string>`. This is poor encapsulation, as consumers could call `OnNext`/`OnError`/`OnCompleted`. To close off that loophole we can simply make the return type an `IObservable<string>`.

```csharp
public IObservable<string> Letters
{
    get { return _letters; }
}
```

The class now _looks_ much better. The improvement, however, is only cosmetic. There is still nothing preventing consumers from casting the result back to an `ISubject<string>` and then calling whatever methods they like. In this example we see external code pushing their values into the sequence.

```csharp
var repo = new ObscuredLeakinessLetterRepo();
var good = repo.GetLetters();
var evil = repo.GetLetters();
    
good.Subscribe(Console.WriteLine);

// Be naughty
var asSubject = evil as ISubject<string>;

if (asSubject != null)
{
    // So naughty, 1 is not a letter!
    asSubject.OnNext("1");
}
else
{
    Console.WriteLine("could not sabotage");
}
```

Output:

```
A
B
C
1
```

The fix to this problem is quite simple. By applying the `AsObservable` extension method, the `_letters` field will be wrapped in a type that only implements `IObservable<T>`.

```csharp
public IObservable<string> GetLetters()
{
    return _letters.AsObservable();
}
```

Output:

```
A
B
C
could not sabotage
```

While I have used words like 'evil' and 'sabotage' in these examples, it is more often than not an oversight rather than malicious intent that causes problems. The failing falls first on the programmer who designed the leaky class. Designing interfaces is hard, but we should do our best to help consumers of our code fall into [the pit of success](http://blogs.msdn.com/b/brada/archive/2003/10/02/50420.aspx) by giving them discoverable and consistent types. Types become more discoverable if we reduce their surface area to expose only the features we intend our consumers to use. In this example we reduced the type's surface area. We did so by removing the property setter and returning a simpler type via the `AsObservable` method.

## Mutable elements cannot be protected		

While the `AsObservable` method can encapsulate your sequence, you should still be aware that it gives no protection against mutable elements. Consider what consumers of a sequence of this class could do:

```csharp
public class Account
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

Here is a quick example of the kind of mess we can make if we choose to modify elements	in a sequence.

```csharp
var source = new Subject<Account>();

// Evil code. It modifies the Account object.
source.Subscribe(account => account.Name = "Garbage");

// unassuming well behaved code
source.Subscribe(
    account=>Console.WriteLine("{0} {1}", account.Id, account.Name),
    ()=>Console.WriteLine("completed"));

source.OnNext(new Account {Id = 1, Name = "Microsoft"});
source.OnNext(new Account {Id = 2, Name = "Google"});
source.OnNext(new Account {Id = 3, Name = "IBM"});
source.OnCompleted();
```

Output:

```
1 Garbage
2 Garbage
3 Garbage
completed
```

Here the second consumer was expecting to get 'Microsoft', 'Google' and 'IBM' but received just 'Garbage'.

Observable sequences will be perceived to be a sequence of resolved events: things that have happened as a statement of fact. This implies two things: first, each element represents a snapshot of state at the time of publication, secondly, the information emanates from a trustworthy source. We want to eliminate the possibility of tampering. Ideally the type `T` will be immutable, solving both of these problems. This way, consumers of the sequence can be assured that the data they get is the data that the source produced. Not being able to mutate elements may seem limiting as a consumer, but these needs are best met via the [Transformation](08_Transformation.html) operators which provide better encapsulation.

Side effects should be avoided where possible. Any combination of concurrency with shared state will commonly demand the need for complex locking, deep understanding of CPU architectures and how they work with the locking and optimization features of the language you use. The simple and preferred approach is to avoid shared state, favor immutable data types and utilize query composition and transformation. Hiding side effects into `Where` or `Select` clauses can make for very confusing code. If a side effect is required, then the `Do` method expresses intent that you are creating a side effect by being explicit.