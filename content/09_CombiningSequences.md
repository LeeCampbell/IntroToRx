---
title: Combining sequences
---

# Combining sequences

Data sources are everywhere, and sometimes we need to consume data from more than just a single source. Common examples that have many inputs include: price feeds, sensor networks, news feeds, social media aggregators, file watchers, multi touch surfaces, heart-beating/polling servers, etc. The way we deal with these multiple stimuli is varied too. We may want to consume it all as a deluge of integrated data, or one sequence at a time as sequential data. We could also get it in an orderly fashion, pairing data values from two sources to be processed together, or perhaps just consume the data from the first source that responds to the request.

Earlier chapters have also shown some examples of the _fan out and back in_ style of data processing, where we partition data, and perform processing on each partition to convert high-volume data into lower-volume higher-value events before recombining. This ability to restructure streams greatly enhances the benefits of operator composition. If Rx only enabled us to apply composition as a simple linear processing chain, it would be a good deal less powerful. Being able to pull streams apart gives us much more flexibility. So even when there is a single source of events, we often still need to combine multiple observable streams as part of our processing. Sequence composition enables you to create complex queries across multiple data sources. This unlocks the possibility to write some very powerful and succinct code.

We've already used [`SelectMany`](06_Transformation.md#selectmany) in earlier chapters. This is one of the fundamental operators in Rx—as we saw in the [Transformation chapter](06_Transformation.md), it's possible to build several other operators from `SelectMany`, and its ability to combine streams is part of what makes it powerful. But there are several more specialized combination operators available, which make it easier to solve certain problems than it would be using `SelectMany`. Also, some operators we've seen before (including `TakeUntil` and `Buffer`) have overloads we've not yet explored that can combine multiple sequences.


## Concat

`Concat` is arguably the simplest way to combine sequences. It does the same thing as its namesake in other LINQ providers: it concatenates two sequences. The resulting sequence produces all of the elements from the first sequence, followed by all of the elements from the second sequence. The simplest signature for `Concat` is as follows.

```cs
public static IObservable<TSource> Concat<TSource>(
    this IObservable<TSource> first, 
    IObservable<TSource> second)
```

Since of `Concat` is an extension method, we can invoke it as a method on any sequence, passing the second sequence in as the only argument:

```cs
IObservable<int> s1 = Observable.Range(0, 3);
IObservable<int> s2 = Observable.Range(5, 5);
IObservable<int> c = s1.Concat(s2);
IDisposable sub = c.Subscribe(Console.WriteLine, x => Console.WriteLine("Error: " + x));
```

This marble diagram shows the items emerging from the two sources, `s1` and `s2`, and how `Concat` combines them into the result, `c`:

TODO: draw properly
```
s1 0-1-2|
s2       5-6-7-8-9|
c  0-1-2-5-6-7-8-9|
```


Rx's `Concat` does nothing with its sources until something subscribes to the `IObservable<T>` it returns. So in this case, when we call `Subscribe` on `c` (the source returned by `Concat`) it will subscribe to its first input, `s1`, and each time that produces a value, the `c` observable will emit that same value to its subscriber. If we went on to call `sub.Dispose()` before `s1` completes, `Concat` would unsubscribe from the first source, and would never subscribe to `s2`. If `s1` were to report an error, `c` would report that same error to is subscriber, and again, it will never subscribe to `s2`. Only if `s1` completes will the `Concat` operator subscribe to `s2`, at which point it will forward any items that second input produces until either the second source completes or fails, or the application unsubscribes from the concatenated observable.

Although Rx's `Concat` has the same logical behaviour as the [LINQ to Objects `Concat`](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.concat), there are some Rx-specific details to be aware of. In particular, timing is often more significant in Rx than with other LINQ implementations. For example, in Rx we distinguish between [_hot_ and _cold_ source](02_KeyTypes.md#hot-and-cold-sources). With a cold source it typically doesn't matter exactly when you subscribe, but hot sources are essentially live, so you only get notified of things that happen while you are subscribed. This can mean that hot sources might not be a good fit with `Concat` The following marble diagram illustrates a scenario in which this produces results that have the potential to surprise:

TODO: create marble diagram like this:
```
cold              |--0--1--2-|
hot               |---A---B---C---D---E-|
Concat(cold, hot) |--0--1--2--C---D---E-|
```

Since `Concat` doesn't subscribe to its second input until the first has finished, it won't see the first couple of items that the `hot` source would deliver to any subscribers that been listening from the start. This might not be the behaviour you would expect: it certainly doesn't look like this concatenated all of the items from the first sequence with all of the items from the second one. It looks like it missed out `A` and `B` from `hot`.

### Marble Diagram Limitations

This last example reveals that marble diagrams gloss over a detail: they show when a source starts, when it produces values, and when it finishes, but they ignore the fact that to be able to produce items at all, an observable source needs a subscriber. If nothing subscribes to an `IObservable<T>`, then it doesn't really produce anything. `Concat` doesn't subscribe to its second input until the first completes, so arguably instead of the diagram above, it would be more accurate to show this:

```
cold              |--0--1--2-|
hot                          |C---D---E-|
Concat(cold, hot) |--0--1--2--C---D---E-|
```

This makes it easier to see why `Concat` produces the output it does. But since `hot` is a hot source here, this diagram fails to convey the fact that `hot` is producing items entirely on its own schedule. In a scenario where `hot` had multiple subscribers, then the first diagram would arguably be better because it correctly reflects every event coming out of `hot` (regardless of however many listeners might be subscribed at any particular moment). But although this convention works for hot sources, it doesn't work for cold ones, which typically start producing items upon subscription. A source returned by [`Timer`](03_CreatingObservableSequences.md#observabletimer) produces items on a regular schedule, but that schedule starts at the instant when subscription occurs. That means that if there are multiple subscriptions, there are multiple schedules. Even if I have just a single `IObservable<long>` returned by `Observable.Timer`, each distinct subscriber will get items on its own schedule—subscribers receive events at a regular interval _starting from whenever they happened subscribe_. So for cold observables, it typically makes sense to use the convention used by this second diagram, in which we're looking at the events received by one particular subscription to a source.

Most of the time we can get away with ignoring this subtlety, quietly using whichever convention suits us. To paraphrase [Humpty Dumpty: when I use a marble diagram, it means just what I choose it to mean—neither more nor less](https://www.goodreads.com/quotes/12608-when-i-use-a-word-humpty-dumpty-said-in-rather). But when you're combining hot and cold sources together, there might not be one obviously best way to represent this in a marble diagram. We could even do something like this, where we describe the events that `hot` represents separately from the events seen by a particular subscription to `hot`.

```
Concat subscription to cold  |--0--1--2-|
Events available through hot  ---A---B---C---D---E-
Concat subscription to hot              |C---D---E-|
Concat(cold, hot)            |--0--1--2--C---D---E-|
```

We're using a distinct 'lane' in the marble diagram to represent the events seen by a particular subscription to a source. With this technique, we can also show what would happen if you pass the same cold source into `Concat` twice:

```
Concat 1st subscription to cold |--0--1--2-|
Concat 2nd subscription to cold            |--0--1--2-|
Concat(cold, cold)              |--0--1--2----0--1--2-|
```

This highlights the fact that that being a cold source, `cold` provides items separately to each subscription. We see the same three values emerging from the same source, but at different times.

### Concatenating Multiple Sources

What if you wanted to concatenate more than two sequences? `Concat` has an overloads accepting multiple observable sequences as an array. This is annotated with the `params` keyword, so you don't need to construct the array explicitly—you can just pass any number of arguments, and the C# compiler will generate the code to create the array for you. There's also an overload taking an `IEnumerable<IObservable<T>>`, in case the observables you want to concatenate are already in some collection.

```cs
public static IObservable<TSource> Concat<TSource>(
    params IObservable<TSource>[] sources)

public static IObservable<TSource> Concat<TSource>(
    this IEnumerable<IObservable<TSource>> sources)
```

The `IEnumerable<IObservable<T>>` overload evaluates `sources` lazily. It won't begin to ask it for source observables until someone subscribes to the observable that `Concat` returns, and it only calls `MoveNext` again on the resulting `IEnumerator<IObservable<T>>` when the current source completes meaning it's ready to start on the text. To illustrate this, the following example is an iterator method that returns a sequence of sequences and is sprinkled with logging. It returns three observable sequences each with a single value [1], [2] and [3]. Each sequence returns its value on a timer delay.

```cs
public IEnumerable<IObservable<long>> GetSequences()
{
    Console.WriteLine("GetSequences() called");
    Console.WriteLine("Yield 1st sequence");

    yield return Observable.Create<long>(o =>
    {
        Console.WriteLine("1st subscribed to");
        return Observable.Timer(TimeSpan.FromMilliseconds(500))
            .Select(i => 1L)
            .Finally(() => Console.WriteLine("1st finished"))
            .Subscribe(o);
    });

    Console.WriteLine("Yield 2nd sequence");

    yield return Observable.Create<long>(o =>
    {
        Console.WriteLine("2nd subscribed to");
        return Observable.Timer(TimeSpan.FromMilliseconds(300))
            .Select(i => 2L)
            .Finally(() => Console.WriteLine("2nd finished"))
            .Subscribe(o);
    });

    Thread.Sleep(1000); // Force a delay

    Console.WriteLine("Yield 3rd sequence");

    yield return Observable.Create<long>(o =>
    {
        Console.WriteLine("3rd subscribed to");
        return Observable.Timer(TimeSpan.FromMilliseconds(100))
            .Select(i=>3L)
            .Finally(() => Console.WriteLine("3rd finished"))
            .Subscribe(o);
    });

    Console.WriteLine("GetSequences() complete");
}
```

We can call this `GetSequences` method and pass the results to `Concat`, and then use our `Dump` extension method to watch what happens:

```cs
GetSequences().Concat().Dump("Concat");
```

Here's the output:

```
GetSequences() called
Yield 1st sequence
1st subscribed to
Concat-->1
1st finished
Yield 2nd sequence
2nd subscribed to
Concat-->2
2nd finished
Yield 3rd sequence
3rd subscribed to
Concat-->3
3rd finished
GetSequences() complete
Concat completed
```

Below is a marble diagram of the `Concat` operator applied to the `GetSequences` method. 's1', 's2' and 's3' represent sequence 1, 2 and 3. Respectively, 'rs' represents the result sequence.

```
s1-----1|
s2      ---2|
s3          -3|
rs-----1---2-3|
```

You should note that once the iterator has executed its first `yield return` to return the first sequence, the iterator does not continue until the first sequence has completed. The iterator calls `Console.WriteLine` to display the text `Yield 2nd sequence` immediately after that first `yield return`, but you can see that message doesn't appear in the output until after we see the `Concat-->1` message showing the first output from `Concat`, and also the `1st finished` message, produced by the `Finally` operator, which runs only after that first sequence has completed. (The code also includes a 500ms delay so that if you run this, you can see that everything stops for a bit until that first source produces its single value then completes.) Once the first source completes, the `GetSequences` method continues (because `Concat` will ask it for the next item once the first observable source completes). When `GetSequences` provides the second sequence with another `yield return`, `Concat` subscribes to that, and again `GetSequences` makes no further progress until that second observable sequence completes. The third sequence is processed in the same fashion.

## StartWith

Another simple concatenation method is the `StartWith` extension method. It allows you to prefix values to a sequence. The method signature takes a `params` array of values so it is easy to pass in as many or as few values as you need.

```csharp
// prefixes a sequence of values to an observable sequence.
public static IObservable<TSource> StartWith<TSource>(
    this IObservable<TSource> source, 
    params TSource[] values)
{
    ...
}
```

Using `StartWith` can give a similar effect to a `BehaviorSubject<T>` by ensuring a value is provided as soon as a consumer subscribes. It is not the same as a `BehaviorSubject` however, as it will not cache the last value.

In this example, we prefix the values -3, -2 and -1 to the sequence [0,1,2].

```csharp
//Generate values 0,1,2 
var source = Observable.Range(0, 3);
var result = source.StartWith(-3, -2, -1);

result.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
```
Output:

```
-3
-2
-1
0
1
2
Completed
```


## Append

TODO



TODO: this has moved, and it needs some work to integrate. It's a bit of an odd one, because it doesn't really neatly fit into any of the categories. But this chapter feels less inappropriate than the rest. (It's kind of similar to thins like Concat or StartWith and Append because it generally emits everything that comes into it, but it may add something.)




## DefaultIfEmpty

The `DefaultIfEmpty` extension method will return a single value if the source sequence is empty. Depending on the overload used, it will either be the value provided as the default, or `Default(T)`. `Default(T)` will be the zero value for _struct_ types and will be `null` for classes. If the source is not empty then all values will be passed straight on through.

In this example the source produces values, so the result of `DefaultIfEmpty` is just the source.

```csharp
var subject = new Subject<int>();

subject.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Subject completed"));

var defaultIfEmpty = subject.DefaultIfEmpty();

defaultIfEmpty.Subscribe(
    b => Console.WriteLine("defaultIfEmpty value: {0}", b),
    () => Console.WriteLine("defaultIfEmpty completed"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);

subject.OnCompleted();
```

Output:

```
1
defaultIfEmpty value: 1
2
defaultIfEmpty value: 2
3
defaultIfEmpty value: 3
Subject completed
defaultIfEmpty completed
```

If the source is empty, we can use either the default value for the type (i.e. 0 for int) or provide our own value in this case 42.

```csharp
var subject = new Subject<int>();

subject.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Subject completed"));

var defaultIfEmpty = subject.DefaultIfEmpty();

defaultIfEmpty.Subscribe(
    b => Console.WriteLine("defaultIfEmpty value: {0}", b),
    () => Console.WriteLine("defaultIfEmpty completed"));

var default42IfEmpty = subject.DefaultIfEmpty(42);

default42IfEmpty.Subscribe(
    b => Console.WriteLine("default42IfEmpty value: {0}", b),
    () => Console.WriteLine("default42IfEmpty completed"));

subject.OnCompleted();
```

Output:

```
Subject completed
defaultIfEmpty value: 0
defaultIfEmpty completed
default42IfEmpty value: 42
default42IfEmpty completed
```





### Repeat

Another simple extension method is `Repeat`. It allows you to simply repeat a sequence, either a specified or an infinite number of times.

```csharp
// Repeats the observable sequence indefinitely and sequentially.
public static IObservable<TSource> Repeat<TSource>(
    this IObservable<TSource> source)
{...}

//Repeats the observable sequence a specified number of times.
public static IObservable<TSource> Repeat<TSource>(
    this IObservable<TSource> source, 
    int repeatCount)
{...}
```

If you use the overload that loops indefinitely, then the only way the sequence will stop is if there is an error or the subscription is disposed of. The overload that specifies a repeat count will stop on error, un-subscription, or when it reaches that count. This example shows the sequence [0,1,2] being repeated three times.

```csharp
var source = Observable.Range(0, 3);
var result = source.Repeat(3);

result.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
```

Output:

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
Completed
```


## Concurrent sequences

The next set of methods aims to combine observable sequences that are producing values concurrently. This is an important step in our journey to understanding Rx. For the sake of simplicity, we have avoided introducing concepts related to concurrency until we had a broad understanding of the simple concepts.

### Amb

The `Amb` method was a new concept to me when I started using Rx. It is a non-deterministic function, first introduced by John McCarthy and is an abbreviation of the word _Ambiguous_. The Rx implementation will return values from the sequence that is first to produce values, and will completely ignore the other sequences. In the examples below I have three sequences that all produce values. The sequences can be represented as the marble diagram below.

```
s1 -1--1--|
s2 --2--2--|
s3 ---3--3--|
r  -1--1--|
```

The code to produce the above is as follows.

```csharp
var s1 = new Subject<int>();
var s2 = new Subject<int>();
var s3 = new Subject<int>();

var result = Observable.Amb(s1, s2, s3);

result.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));

s1.OnNext(1);
s2.OnNext(2);
s3.OnNext(3);
s1.OnNext(1);
s2.OnNext(2);
s3.OnNext(3);
s1.OnCompleted();
s2.OnCompleted();
s3.OnCompleted();
```

Output:

```
1
1
Completed
```

If we comment out the first `s1.OnNext(1);` then s2 would produce values first and the marble diagram would look like this.

```
s1 ---1--|
s2 -2--2--|
s3 --3--3--|
r  -2--2--|
```

The `Amb` feature can be useful if you have multiple cheap resources that can provide values, but latency is widely variable. For an example, you may have servers replicated around the world. Issuing a query is cheap for both the client to send and for the server to respond, however due to network conditions the latency is not predictable and varies considerably. Using the `Amb` operator, you can send the same request out to many servers and consume the result of the first that responds.

There are other useful variants of the `Amb` method. We have used the overload that takes a `params` array of sequences. You could alternatively use it as an extension method and chain calls until you have included all the target sequences (e.g. s1.Amb(s2).Amb(s3)). Finally, you could pass in an `IEnumerable<IObservable<T>>`.

```csharp
// Propagates the observable sequence that reacts first.
public static IObservable<TSource> Amb<TSource>(
    this IObservable<TSource> first, 
    IObservable<TSource> second)
{...}
public static IObservable<TSource> Amb<TSource>(
    params IObservable<TSource>[] sources)
{...}
public static IObservable<TSource> Amb<TSource>(
    this IEnumerable<IObservable<TSource>> sources)
{...}
```

Reusing the `GetSequences` method from the `Concat` section, we see that the evaluation of the outer (IEnumerable) sequence is eager.

```csharp
GetSequences().Amb().Dump("Amb");
```

Output:

```
GetSequences() called
Yield 1st sequence
Yield 2nd sequence
Yield 3rd sequence
GetSequences() complete
1st subscribed to
2nd subscribed to
3rd subscribed to
Amb-->3
Amb completed
```

Marble:

```
s1-----1|
s2---2|
s3-3|
rs-3|
```

Take note that the inner observable sequences are not subscribed to until the outer sequence has yielded them all. This means that the third sequence is able to return values the fastest even though there are two sequences yielded one second before it (due to the `Thread.Sleep`).

### Merge

The `Merge` extension method does a primitive combination of multiple concurrent sequences. As values from any sequence are produced, those values become part of the result sequence. All sequences need to be of the same type, as per the previous methods. In this diagram, we can see `s1` and `s2` producing values concurrently and the values falling through to the result sequence as they occur.

```
s1 --1--1--1--|
s2 ---2---2---2|
r  --12-1-21--2|
```

The result of a `Merge` will complete only once all input sequences complete. By contrast, the `Merge` operator will error if any of the input sequences terminates erroneously.

```csharp
// Generate values 0,1,2 
var s1 = Observable.Interval(TimeSpan.FromMilliseconds(250))
    .Take(3);

// Generate values 100,101,102,103,104 
var s2 = Observable.Interval(TimeSpan.FromMilliseconds(150))
    .Take(5)
    .Select(i => i + 100);
    
s1.Merge(s2)
    .Subscribe(
        Console.WriteLine,
        ()=>Console.WriteLine("Completed"));
```

The code above could be represented by the marble diagram below. In this case, each unit of time is 50ms. As both sequences produce a value at 750ms, there is a race condition and we cannot be sure which value will be notified first in the result sequence (sR).

```
s1 ----0----0----0| 
s2 --0--0--0--0--0|
sR --0-00--00-0--00|
```

Output:

```
100
0
101
102
1
103
104 // Note this is a race condition. 2 could be 
2   // published before 104. 
```

You can chain this overload of the `Merge` operator to merge multiple sequences. `Merge` also provides numerous other overloads that allow you to pass more than two source sequences. You can use the static method `Observable.Merge` which takes a `params` array of sequences that is known at compile time. You could pass in an `IEnumerable` of sequences like the `Concat` method. `Merge` also has the overload that takes an `IObservable<IObservable<T>>`, a nested observable. To summarize:

- Chain `Merge` operators together e.g. `s1.Merge(s2).Merge(s3)`
- Pass a `params` array of sequences to the `Observable.Merge` static method. e.g. `Observable.Merge(s1,s2,s3)`
- Apply the `Merge` operator to an `IEnumerable<IObservable<T>>`.
- Apply the `Merge` operator to an `IObservable<IObservable<T>>`.

Merge overloads:

```csharp
/// Merges two observable sequences into a single observable sequence.
/// Returns a sequence that merges the elements of the given sequences.
public static IObservable<TSource> Merge<TSource>(
    this IObservable<TSource> first, 
    IObservable<TSource> second)
{...}

// Merges all the observable sequences into a single observable sequence.
// The observable sequence that merges the elements of the observable sequences.
public static IObservable<TSource> Merge<TSource>(
    params IObservable<TSource>[] sources)
{...}

// Merges an enumerable sequence of observable sequences into a single observable sequence.
public static IObservable<TSource> Merge<TSource>(
    this IEnumerable<IObservable<TSource>> sources)
{...}

// Merges an observable sequence of observable sequences into an observable sequence.
// Merges all the elements of the inner sequences in to the output sequence.
public static IObservable<TSource> Merge<TSource>(
    this IObservable<IObservable<TSource>> sources)
{...}
```

For merging a known number of sequences, the first two operators are effectively the same thing and which style you use is a matter of taste: either provide them as a `params` array or chain the operators together. The third and fourth overloads allow to you merge sequences that can be evaluated lazily at run time. The `Merge` operators that take a sequence of sequences make for an interesting concept. You can either pull or be pushed observable sequences, which will be subscribed to immediately.

If we again reuse the `GetSequences` method, we can see how the `Merge` operator works with a sequence of sequences.

```csharp
GetSequences().Merge().Dump("Merge");
```

Output:

```
GetSequences() called
Yield 1st sequence
1st subscribed to
Yield 2nd sequence
2nd subscribed to
Merge --> 2
Merge --> 1
Yield 3rd sequence
3rd subscribed to
GetSequences() complete
Merge --> 3
Merge completed
```

As we can see from the marble diagram, s1 and s2 are yielded and subscribed to immediately. s3 is not yielded for one second and then is subscribed to. Once all input sequences have completed, the result sequence completes.

```
s1-----1|
s2---2|
s3          -3|
rs---2-1-----3|
```

### Switch

Receiving all values from a nested observable sequence is not always what you need. In some scenarios, instead of receiving everything, you may only want the values from the most recent inner sequence. A great example of this is live searches. As you type, the text is sent to a search service and the results are returned to you as an observable sequence. Most implementations have a slight delay before sending the request so that unnecessary work does not happen. Imagine I want to search for "Intro to Rx". I quickly type in "Into to" and realize I have missed the letter 'r'. I stop briefly and change the text to "Intro ". By now, two searches have been sent to the server. The first search will return results that I do not want. Furthermore, if I were to receive data for the first search merged together with results for the second search, it would be a very odd experience for the user. This scenario fits perfectly with the `Switch` method.

In this example, there is a source that represents a sequence of search text. <!--When the user types in a new value, the source sequence OnNext's the value--> Values the user types are represented as the source sequence. Using `Select`, we pass the value of the search to a function that takes a `string` and returns an `IObservable<string>`. This creates our resulting nested sequence, `IObservable<IObservable<string>>`.

Search function signature:

```csharp
private IObservable<string> SearchResults(string query)
{
    ...
}
```

Using `Merge` with overlapping search:

```csharp
IObservable<string> searchValues = ....;
IObservable<IObservable<string>> search = searchValues.Select(searchText=>SearchResults(searchText));
                    
var subscription = search
    .Merge()
    .Subscribe(Console.WriteLine);
```

<!--TODO: Show output here-->

If we were lucky and each search completed before the next element from `searchValues` was produced, the output would look sensible. It is much more likely, however that multiple searches will result in overlapped search results. This marble diagram shows what the `Merge` function could do in such a situation.

- `SV` is the searchValues sequence
- `S1` is the search result sequence for the first value in searchValues/SV
- `S2` is the search result sequence for the second value in searchValues/SV
- `S3` is the search result sequence for the third value in searchValues/SV
- `RM` is the result sequence for the merged (`R`esult `M`erge) sequences

```
SV--1---2---3---|
S1  -1--1--1--1|
S2      --2-2--2--2|
S3          -3--3|
RM---1--1-2123123-2|
```

Note how the values from the search results are all mixed together. This is not what we want. If we use the `Switch` extension method we will get much better results. `Switch` will subscribe to the outer sequence and as each inner sequence is yielded it will subscribe to the new inner sequence and dispose of the subscription to the previous inner sequence. This will result in the following marble diagram where `RS` is the result sequence for the Switch (`R`esult `S`witch) sequences

```
SV--1---2---3---|
S1  -1--1--1--1|
S2      --2-2--2--2|
S3          -3--3|
RS --1--1-2-23--3|
```

Also note that, even though the results from S1 and S2 are still being pushed, they are ignored as their subscription has been disposed of. This eliminates the issue of overlapping values from the nested sequences.

## Pairing sequences

The previous methods allowed us to flatten multiple sequences sharing a common type into a result sequence of the same type. These next sets of methods still take multiple sequences as an input, but attempt to pair values from each sequence to produce a single value for the output sequence. In some cases, they also allow you to provide sequences of different types.

### CombineLatest

The `CombineLatest` extension method allows you to take the most recent value from two sequences, and with a given function transform those into a value for the result sequence. Each input sequence has the last value cached like `Replay(1)`. Once both sequences have produced at least one value, the latest output from each sequence is passed to the `resultSelector` function every time either sequence produces a value. The signature is as follows.

```csharp
// Composes two observable sequences into one observable sequence by using the selector 
// function whenever one of the observable sequences produces an element.
public static IObservable<TResult> CombineLatest<TFirst, TSecond, TResult>(
    this IObservable<TFirst> first, 
    IObservable<TSecond> second, 
    Func<TFirst, TSecond, TResult> resultSelector)
{...}
```

The marble diagram below shows off usage of `CombineLatest` with one sequence that produces numbers (N), and the other letters (L). If the `resultSelector` function just joins the number and letter together as a pair, this would be the result (R):

```
N---1---2---3---
L--a------bc----
R---1---2-223---
    a   a bcc   
```

If we slowly walk through the above marble diagram, we first see that `L` produces the letter 'a'. `N` has not produced any value yet so there is nothing to pair, no value is produced for the result (R). Next, `N` produces the number '1' so we now have a pair '1a' that is yielded in the result sequence. We then receive the number '2' from `N`. The last letter is still 'a' so the next pair is '2a'. The letter 'b' is then produced creating the pair '2b', followed by 'c' giving '2c'. Finally the number 3 is produced and we get the pair '3c'.

This is great in case you need to evaluate some combination of state which needs to be kept up-to-date when the state changes. A simple example would be a monitoring system. Each service is represented by a sequence that returns a Boolean indicating the availability of said service. The monitoring status is green if all services are available; we can achieve this by having the result selector perform a logical AND. 
Here is an example.

```csharp
IObservable<bool> webServerStatus = GetWebStatus();
IObservable<bool> databaseStatus = GetDBStatus();

// Yields true when both systems are up.
var systemStatus = webServerStatus
    .CombineLatest(
        databaseStatus,
        (webStatus, dbStatus) => webStatus && dbStatus);
```

Some readers may have noticed that this method could produce a lot of duplicate values. For example, if the web server goes down the result sequence will yield '`false`'. If the database then goes down, another (unnecessary) '`false`' value will be yielded. This would be an appropriate time to use the `DistictUntilChanged` extension method. The corrected code would look like the example below.

```csharp
// Yields true when both systems are up, and only on change of status
var systemStatus = webServerStatus
    .CombineLatest(
        databaseStatus,
        (webStatus, dbStatus) => webStatus && dbStatus)
    .DistinctUntilChanged();
```

To provide an even better service, we could provide a default value by prefixing `false` to the sequence.

```csharp
// Yields true when both systems are up, and only on change of status
var systemStatus = webServerStatus
    .CombineLatest(
        databaseStatus,
        (webStatus, dbStatus) => webStatus && dbStatus)
    .DistinctUntilChanged()
    .StartWith(false);
```

### Zip

The `Zip` extension method is another interesting merge feature. Just like a zipper on clothing or a bag, the `Zip` method brings together two sequences of values as pairs; two by two. Things to note about the `Zip` function is that the result sequence will complete when the first of the sequences complete, it will error if either of the sequences error and it will only publish once it has a pair of fresh values from each source sequence. So if one of the source sequences publishes values faster than the other sequence, the rate of publishing will be dictated by the slower of the two sequences.

```csharp
// Generate values 0,1,2 
var nums = Observable.Interval(TimeSpan.FromMilliseconds(250))
    .Take(3);

// Generate values a,b,c,d,e,f 
var chars = Observable.Interval(TimeSpan.FromMilliseconds(150))
    .Take(6)
    .Select(i => Char.ConvertFromUtf32((int)i + 97));

// Zip values together
nums.Zip(chars, (lhs, rhs) => new { Left = lhs, Right = rhs })
    .Dump("Zip");
```

This can be seen in the marble diagram below. Note that the result uses two lines so that we can represent a complex type, i.e. the anonymous type with the properties Left and Right.

```
nums  ----0----1----2| 
chars --a--b--c--d--e--f| 
result----0----1----2|
          a    b    c|
```

The actual output of the code:

```
{ Left = 0, Right = a }
{ Left = 1, Right = b }
{ Left = 2, Right = c }
```

Note that the `nums` sequence only produced three values before completing, while the `chars` sequence produced six values. The result sequence thus has three values, as this was the most pairs that could be made.

The first use I saw of `Zip` was to showcase drag and drop. [The example](http://channel9.msdn.com/Blogs/J.Van.Gogh/Writing-your-first-Rx-Application) tracked mouse movements from a `MouseMove` event that would produce event arguments with its current X,Y coordinates. First, the example turns the event into an observable sequence. Then they cleverly zipped the sequence with a `Skip(1)` version of the same sequence. This allows the code to get a delta of the mouse position, i.e. where it is now (sequence.Skip(1)) minus where it was (sequence). It then applied the delta to the control it was dragging.

To visualize the concept, let us look at another marble diagram. Here we have the mouse movement (MM) and the Skip 1 (S1). The numbers represent the index of the mouse movement.

```
MM --1--2--3--4--5
S1    --2--3--4--5
Zip   --1--2--3--4
        2  3  4  5
```

Here is a code sample where we fake out some mouse movements with our own subject.

```csharp
var mm = new Subject<Coord>();
var s1 = mm.Skip(1);

var delta = mm.Zip(s1,
                    (prev, curr) => new Coord
                        {
                            X = curr.X - prev.X,
                            Y = curr.Y - prev.Y
                        });

delta.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));

mm.OnNext(new Coord { X = 0, Y = 0 });
mm.OnNext(new Coord { X = 1, Y = 0 }); //Move across 1
mm.OnNext(new Coord { X = 3, Y = 2 }); //Diagonally up 2
mm.OnNext(new Coord { X = 0, Y = 0 }); //Back to 0,0
mm.OnCompleted();
```

This is the simple Coord(inate) class we use.

```csharp
public class Coord
{
    public int X { get; set; }
    public int Y { get; set; }
    public override string ToString()
    {
        return string.Format("{0},{1}", X, Y);
    }
}
```

Output:

```
0,1
2,2
-3,-2
Completed
```

It is also worth noting that `Zip` has a second overload that takes an `IEnumerable<T>` as the second input sequence.

```csharp
// Merges an observable sequence and an enumerable sequence into one observable sequence 
// containing the result of pair-wise combining the elements by using the selector function.
public static IObservable<TResult> Zip<TFirst, TSecond, TResult>(
    this IObservable<TFirst> first, 
    IEnumerable<TSecond> second, 
    Func<TFirst, TSecond, TResult> resultSelector)
{...}
```

This allows us to zip sequences from both `IEnumerable<T>` and `IObservable<T>` paradigms!

### And-Then-When

If `Zip` only taking two sequences as an input is a problem, then you can use a combination of the three `And`/`Then`/`When` methods. These methods are used slightly differently from most of the other Rx methods. Out of these three, `And` is the only extension method to `IObservable<T>`. Unlike most Rx operators, it does not return a sequence; instead, it returns the mysterious type `Pattern<T1, T2>`. The `Pattern<T1, T2>` type is public (obviously), but all of its properties are internal. The only two (useful) things you can do with a `Pattern<T1, T2>` are invoking its `And` or `Then` methods. The `And` method called on the `Pattern<T1, T2>` returns a `Pattern<T1, T2, T3>`. On that type, you will also find the `And` and `Then` methods. The generic `Pattern` types are there to allow you to chain multiple `And` methods together, each one extending the generic type parameter list by one. You then bring them all together with the `Then` method overloads. The `Then` methods return you a `Plan` type. Finally, you pass this `Plan` to the `Observable.When` method in order to create your sequence.

It may sound very complex, but comparing some code samples should make it easier to understand. It will also allow you to see which style you prefer to use.

To `Zip` three sequences together, you can either use `Zip` methods chained together like this:

```csharp
var one = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
var two = Observable.Interval(TimeSpan.FromMilliseconds(250)).Take(10);
var three = Observable.Interval(TimeSpan.FromMilliseconds(150)).Take(14);

// lhs represents 'Left Hand Side'
// rhs represents 'Right Hand Side'
var zippedSequence = one
    .Zip(two, (lhs, rhs) => new {One = lhs, Two = rhs})
    .Zip(three, (lhs, rhs) => new {One = lhs.One, Two = lhs.Two, Three = rhs});

zippedSequence.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
```

Or perhaps use the nicer syntax of the `And`/`Then`/`When`:

```csharp
var pattern = one.And(two).And(three);
var plan = pattern.Then((first, second, third)=>new{One=first, Two=second, Three=third});
var zippedSequence = Observable.When(plan);

zippedSequence.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
```

This can be further reduced, if you prefer, to:

```csharp
var zippedSequence = Observable.When(
        one.And(two)
            .And(three)
            .Then((first, second, third) => 
                new { 
                    One = first, 
                    Two = second, 
                    Three = third 
                })
        );

zippedSequence.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));
```

The `And`/`Then`/`When` trio has more overloads that enable you to group an even greater number of sequences. They also allow you to provide more than one 'plan' (the output of the `Then` method). This gives you the `Merge` feature but on the collection of 'plans'. I would suggest playing around with them if this functionality is of interest to you. The verbosity of enumerating all of the combinations of these methods would be of low value. You will get far more value out of using them and discovering for yourself.

As we delve deeper into the depths of what the Rx libraries provide us, we can see more practical usages for it. Composing sequences with Rx allows us to easily make sense of the multiple data sources a problem domain is exposed to. We can concatenate values or sequences together sequentially with `StartWith`, `Concat` and `Repeat`. We can process multiple sequences concurrently with `Merge`, or process a single sequence at a time with `Amb` and `Switch`. Pairing values with `CombineLatest`, `Zip` and the `And`/`Then`/`When` operators can simplify otherwise fiddly operations like our drag-and-drop examples and monitoring system status.


TODO: Was in Inspection but I think this might go more naturally after Zip

    
## SequenceEqual

Finally `SequenceEqual` extension method is perhaps a stretch to put in a chapter that starts off talking about catamorphism and fold, but it does serve well for the theme of inspection. This method allows us to compare two observable sequences. As each source sequence produces values, they are compared to a cache of the other sequence to ensure that each sequence has the same values in the same order and that the sequences are the same length. This means that the result sequence can return `false` as soon as the source sequences produce diverging values, or `true` when both sources complete with the same values.

```csharp
var subject1 = new Subject<int>();

subject1.Subscribe(
    i=>Console.WriteLine("subject1.OnNext({0})", i),
    () => Console.WriteLine("subject1 completed"));

var subject2 = new Subject<int>();

subject2.Subscribe(
    i=>Console.WriteLine("subject2.OnNext({0})", i),
    () => Console.WriteLine("subject2 completed"));

var areEqual = subject1.SequenceEqual(subject2);

areEqual.Subscribe(
    i => Console.WriteLine("areEqual.OnNext({0})", i),
    () => Console.WriteLine("areEqual completed"));

subject1.OnNext(1);
subject1.OnNext(2);

subject2.OnNext(1);
subject2.OnNext(2);
subject2.OnNext(3);

subject1.OnNext(3);

subject1.OnCompleted();
subject2.OnCompleted();
```

Output:

```
subject1.OnNext(1)
subject1.OnNext(2)
subject2.OnNext(1)
subject2.OnNext(2)
subject2.OnNext(3)
subject1.OnNext(3)
subject1 completed
subject2 completed
areEqual.OnNext(True)
areEqual completed
```

This chapter covered a set of methods that allow us to inspect observable sequences. The result of each, generally, returns a sequence with a single value. We will continue to look at methods to reduce our sequence until we discover the elusive functional fold feature.




This brings us to a close on Part 2. The key takeaways from this were to allow you the reader to understand a key principal to Rx: functional composition. As we move through Part 2, examples became progressively more complex. We were leveraging the power of LINQ to chain extension methods together to compose complex queries.

We didn't try to tackle all of the operators at once, we approached them in groups.

- Creation
- Reduction
- Inspection
- Aggregation
- Transformation

On deeper analysis of the operators we find that most of the operators are actuallyspecialization of the higher order functional concepts. We named them the ABC's of functional programming:

- Anamorphism, aka:
  - Ana
  - Unfold
  - Generate
- Bind, aka:
  - Map
  - SelectMany
  - Projection
  - Transform
- Catamorphism, aka:
  - Cata
  - Fold
  - Reduce
  - Accumulate
  - Inject

Now you should feel that you have a strong understanding of how a sequence can be manipulated. What we have learnt up to this point however can all largely be applied to `IEnumerable` sequences too. Rx can be much more complex than what many people will have dealt with in `IEnumerable` world, as we have seen with the `SelectMany` operator. In the next part of the book we will uncover features specific to the asynchronous nature of Rx. With the foundation we have built so far we should be able to tackle the far more challenging and interesting features of Rx.



TODO: `Buffer`, `Window`?