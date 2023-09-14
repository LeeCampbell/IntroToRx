---
title: Combining sequences
---

# Combining sequences

Data sources are everywhere, and sometimes we need to consume data from more than just a single source. Common examples that have many inputs include: price feeds, sensor networks, news feeds, social media aggregators, file watchers, multi touch surfaces, heart-beating/polling servers, etc. The way we deal with these multiple stimuli is varied too. We may want to consume it all as a deluge of integrated data, or one sequence at a time as sequential data. We could also get it in an orderly fashion, pairing data values from two sources to be processed together, or perhaps just consume the data from the first source that responds to the request.

Earlier chapters have also shown some examples of the _fan out and back in_ style of data processing, where we partition data, and perform processing on each partition to convert high-volume data into lower-volume higher-value events before recombining. This ability to restructure streams greatly enhances the benefits of operator composition. If Rx only enabled us to apply composition as a simple linear processing chain, it would be a good deal less powerful. Being able to pull streams apart gives us much more flexibility. So even when there is a single source of events, we often still need to combine multiple observable streams as part of our processing. Sequence composition enables you to create complex queries across multiple data sources. This unlocks the possibility to write some very powerful and succinct code.

We've already used [`SelectMany`](06_Transformation.md#selectmany) in earlier chapters. This is one of the fundamental operators in Rx—as we saw in the [Transformation chapter](06_Transformation.md), it's possible to build several other operators from `SelectMany`, and its ability to combine streams is part of what makes it powerful. But there are several more specialized combination operators available, which make it easier to solve certain problems than it would be using `SelectMany`. Also, some operators we've seen before (including `TakeUntil` and `Buffer`) have overloads we've not yet explored that can combine multiple sequences.

## Sequential Combination

We'll start with the simplest kind of combining operator, which do not attempt concurrent combination. They deal with one source sequence at a time.

### Concat

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

#### Marble Diagram Limitations

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

#### Concatenating Multiple Sources

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

### Prepend

There's one particular scenario that `Concat` supports, but in a slightly cumbersome way. It can sometimes be useful to make a sequence that always emits some initial value immediately. Take the example I've been using a lot in this book, where ships transmit AIS messages to report their location and other information: in some applications you might not want to wait until the ship happens next to transmit a message. You could imagine an application that records the last known location of any vessel. This would make it possible for the application to offer, say, an `IObservable<IVesselNavigation>` which instantly reports the last known information upon subscription, and which then goes on to supply any newer messages if the vessel produces any.

How would we implement this? We want initially cold-source-like behaviour, but transitioning into hot. So we could just concatenate two sources. We could use [`Observable.Return`](03_CreatingObservableSequences.md#observablereturn) to create a single-element cold source, and then concatenate that with the live stream:

```cs
IVesselNavigation lastKnown = ais.GetLastReportedNavigationForVessel(mmsi);
IObservable<IVesselNavigation> live = ais.GetNavigationMessagesForVessel(mmsi);

IObservable<IVesselNavigation> lastKnownThenLive = Observable.Concat(
    Observable.Return(lastKnown), live);
```

This is a common enough requirement that Rx supplies `Prepend` that has a similar effect. We can replace the final line with:

```cs
IObservable<IVesselNavigation> lastKnownThenLive = live.Prepend(lastKnown);
```

This observable will do exactly the same thing: subscribers will immediately receive the `lastKnown`, and then if the vessel should emit further navigation messages, they will receive those too. By the way, for this scenario you'd probably also want to ensure that the look up of the "last known" message happens as late as possible. We can delay this until the point of subscription by using `Defer`:

```cs
public static IObservable<IVesselNavigation> GetLastKnownAndSubsequenceNavigationForVessel(uint mmsi)
{
    return Observable.Defer<IVesselNavigation>(() =>
    {
        // This lambda will run each time someone subscribes.
        IVesselNavigation lastKnown = ais.GetLastReportedNavigationForVessel(mmsi);
        IObservable<IVesselNavigation> live = ais.GetNavigationMessagesForVessel(mmsi);

        return live.Prepend(lastKnown);
    }
}
```

`StartWith` might remind you of [`BehaviorSubject<T>`](03_CreatingObservableSequences.md#behaviorsubject), because that also ensures that consumers receive a value as soon as they subscribe. It's not quite the same—`BehaviorSubject<T>` caches the last value its own source emits. You might thank that would make it a better way to implement this vessel navigation example. However, since this example is able to return a source for any vessel (the `mmsi` argument is a [Maritime Mobile Service Identity](https://en.wikipedia.org/wiki/Maritime_Mobile_Service_Identity) uniquely identifying a vessel) it would need to keep a `BehaviorSubject<T>` running for every single vessel you were interested in, which might be impractical.

`BehaviorSubject<T>` can hold onto only one value, which is fine for this AIS scenario, and `Prepend` shares this limitation. But what if you need a source to begin with some particular sequence?

### StartWith

`StartWith` is a generalization of `Prepend` that enables us to provide any number of values to emit immediately upon subscription. As with `Prepend`, it will then go on to forward any further notifications that emerge from the source.

As you can see from its signature, this method takes a `params` array of values so you can pass in as many or as few values as you need:

```cs
// prefixes a sequence of values to an observable sequence.
public static IObservable<TSource> StartWith<TSource>(
    this IObservable<TSource> source, 
    params TSource[] values)
```

There's also an overload that accepts an `IEnumerable<T>`. Note that Rx will _not_ defer its enumeration of this. `StartWith` immediately converts the `IEnumerable<T>` into an array before returning.

`StartsWith` is not a common LINQ operator, and its existence is peculiar to Rx. If you imagine what `StartsWith` would look like in LINQ to Objects, it would not be meaningfully different from [`Concat`](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.concat). There's a difference in Rx because `StartsWith` effectively bridges between _pull_ and _push_ worlds. It effectively converts the items we supply into an observable, and it then concatenates the `source` argument onto that.

### Append

The existence of `Prepend` might lead you to wonder whether there is an `Append` for adding a single item onto the end of any `IObservable<T>`. After all, this is a common LINQ operator; [LINQ to Objects has an `Append` implementation](https://learn.microsoft.com/en-us/dotnet/api/system.linq.enumerable.append), for example. And Rx does indeed supply such a thing:

```cs
IObservable<string> oneMore = arguments.Append("And another thing...");
```

There is no corresponding `EndWith`. Apparently there's not much demand—the [Rx repository](https://github.com/dotnet/reactive) has not yet had a feature request. So although the symmetry of `Prepend` and `Append` does suggest that there could be a similar symmetry between `StartWith` and an as-yet-hypothetical `EndWith`, the absence of this counterpart doesn't seem to have caused any problems. There's an obvious value to being able to create observable sources that always immediately produce a useful output; it's not clear what `EndWith` would be useful for beside satisfying a craving for symmetry.

### DefaultIfEmpty

The next operator we'll examine doesn't strictly performs sequential combination. However, it's a very close relative of `Append` and `Prepend`. Like those operators, this will emit everything their source does. And like those operators, `DefaultIfEmpty` takes one additional item. The difference is that it won't always emit that additional item.

Whereas `Prepend` emits its additional item at the start, and `Append` emits its additional item at the end, `DefaultIfEmpty` emits the additional item only if the source completes without producing anything. So this provides a way of guaranteeing that an observable will not be empty.

You don't have to supply `DefaultIfEmpty` with a value. If you use the overload in which you supply no such value, it will just use `default(T)`. This will be a zero-like value for _struct_ types and `null` for reference types.

### Repeat

The final operator that combines sequences sequentially is `Repeat`. It allows you to simply repeat a sequence. It offers overloads where you can specify the number of times to repeat the input, and one that repeats infinitely:

```cs
//Repeats the observable sequence a specified number of times.
public static IObservable<TSource> Repeat<TSource>(
    this IObservable<TSource> source, 
    int repeatCount)

// Repeats the observable sequence indefinitely and sequentially.
public static IObservable<TSource> Repeat<TSource>(
    this IObservable<TSource> source)
```

`Repeat` resubscribes to the source for each repetition.

If you use the overload that repeats indefinitely, then the only way the sequence will stop is if there is an error or the subscription is disposed of. The overload that specifies a repeat count will stop on error, un-subscription, or when it reaches that count. This example shows the sequence [0,1,2] being repeated three times.

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

We'll now move on to operators for combining observable sequences that are producing values concurrently.

### Amb

`Amb` is a strangely named operator. It's short for _ambiguous_, but that doesn't tell us much more than `Amb`. If you're curious about the name you can read about the [origins of `Amb` in Appendix C](C_AlgebraicUnderpinnings#amb), but for now, let's look at what it actually does. 
Rx's `Amb` takes any number of `IObservable<T>` sources as inputs, and waits to see which, if any, first produces some sort of output. As soon as this happens, it immediately unsubscribes from all of the other sources, and forwards all notifications from the source that reacted first.

Why is that useful?

A common use case for `Amb` is when you want to produce some sort of result as quickly as possible, and you have multiple options for obtaining that result but you don't know in advance which will be fastest. Perhaps there are multiple servers that could all potentially give you the answer you want, and it's impossible to predict which will have the lowest response time. You could send requests to all of them, and then just use the first to respond. If you model each individual request as its own `IObservable<T>`, `Amb` can handle this for you. Note that this isn't very efficient: you're asking several servers all to do the same work, and you're going to discard the results from most of them. (Since `Amb` unsubscribes from all the sources it's not going to use as soon as the first reacts, it's possible that you might be able to send a message to all the other servers to cancel the request. But this is still somewhat wasteful.) But there may be scenarios in which timeliness is crucial, and for those cases it might be worth tolerating a bit of wasted effort to produce faster results.

To illustrate `Amb`'s behaviour, here's a marble diagram showing three sequences, `s1`, `s2`, and `s3`, each able to produce a sequence values. The line labelled `r` shows the result of passing all three sequences into `Amb`. As you can see, `r` provides exactly the same notifications as `s1`. This is because in this example, `s1` was the first sequence to produce a value.

TODO: draw properly.

```
s1 -1---2----3--4|
s2 --99--88-|
s3 ----8---7--6---|
r  -1---2----3--4|
```

This code creates exactly the situation described in that marble diagram, to verify that this is indeed how `Amb` behaves:

```cs
var s1 = new Subject<int>();
var s2 = new Subject<int>();
var s3 = new Subject<int>();

var result = Observable.Amb(s1, s2, s3);

result.Subscribe(
    Console.WriteLine,
    () => Console.WriteLine("Completed"));

s1.OnNext(1);
s2.OnNext(99);
s3.OnNext(8);
s1.OnNext(2);
s2.OnNext(88);
s3.OnNext(7);
s2.OnCompleted();
s1.OnNext(3);
s3.OnNext(6);
s1.OnNext(4);
s1.OnCompleted();
s3.OnCompleted();
```

Output:

```
1
2
3
4
Completed
```

If we changed the order so that `s2.OnNext(99)` came before the call to `s1.OnNext(1);` then s2 would produce values first and the marble diagram would look like this.

```
s1 --1--2----3--4|
s2 99----88--|
s3 ---8-----7--6--|
r  99----88-|
```

There are a few overloads of `Amb`. The preceding example used the overload that takes a `params` array of sequences. There's also an overload that takes exactly two sources, avoiding the array allocation that occurs with `params`. Finally, you could pass in an `IEnumerable<IObservable<T>>`. (Note that there are no overloads that take an `IObservable<IObservable<T>>`. `Amb` requires all of the source observables it monitors to be supplied up front.)

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

Here is the marble diagram illustrating how this code behaves:

```
s1-----1|
s2---2|
s3-3|
rs-3|
```

Take note that the inner observable sequences are not subscribed to until the outer sequence has yielded them all. This means that the third sequence is able to return values the fastest even though there are two sequences yielded one second before it (due to the `Thread.Sleep`).

### Merge

The `Merge` extension method takes multiple sequences as its input. Any time any of those input sequences produces a value, the observable returned by `Merge` produces that same value. If the input sequences produce values at the same time on different threads, `Merge` handles this safely, ensuring that it delivers items one at a time.

Since `Merge` returns a single observable sequence that includes all of the values from all of its input sequences, there's a sense in which it is similar to `Concat`. But whereas `Concat` waits until each input sequence completes before moving onto the next, `Merge` supports concurrently active sequences. As soon as you subscribe to the observable returned by `Merge`, it immediately subscribes to all of its inputs, forwarding everything any of them produces. This marble diagram shows two sequences, `s1` and `s2`, running concurrently and `r` shows the effect of combining these with `Merge`—the values from both source sequences emerge from the merged sequence.

TODO: draw.

```
s1 --1--1--1--|
s2 ---2---2---2|
r  --12-1-21--2|
```

The result of a `Merge` will complete only once all input sequences complete. However, the `Merge` operator will error if any of the input sequences terminates erroneously (at which point it will unsubscribe from all its other inputs).

If you read the [Creating Observables chapter](03_CreatingObservableSequences.md), you've already seen one example of `Merge`. I used it to combine the individual sequences representing the various events provided by a `FileSystemWatcher` into a single stream at the end of the ['Representing Filesystem Events in Rx'](03_CreatingObservableSequences.md#representing-filesystem-events-in-rx) section. As another example, let's look at AIS once again. There is no publicly available single global source that can provide all AIS messages across the entire globe as an `IObservable<IAisMessage>`. Any single source is likely to cover just one area, or maybe even just a single AIS receiver. With `Merge`, it's straightforward to combine these into a single source:

```cs
IObservable<IAisMessage> station1 = aisStations.GetMessagesFromStation("AdurStation");
IObservable<IAisMessage> station2 = aisStations.GetMessagesFromStation("EastbourneStation");

IObservable<IAisMessage> allMessages = station1.Merge(station2);
```

If you want to combine more than two sources, you have a few options::

- Chain `Merge` operators together e.g. `s1.Merge(s2).Merge(s3)`
- Pass a `params` array of sequences to the `Observable.Merge` static method. e.g. `Observable.Merge(s1,s2,s3)`
- Apply the `Merge` operator to an `IEnumerable<IObservable<T>>`.
- Apply the `Merge` operator to an `IObservable<IObservable<T>>`.

The overloads look like this:

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

When you know at compile time exactly how many sequences you will be merging, choosing between the first two operators really is a matter of your preferred style: either provide them as a `params` array or chain the operators together. The third and fourth overloads allow to you merge sequences that can be evaluated lazily at run time. That last `Merge` overload that takes a sequence of sequences is particularly interesting, because it makes it possible for the set of sources being merged to grow over time. With that last overload, `Merge` will remain subscribed to `sources` for as long as your code remains subscribed to the `IObservable<T>` that `Merge` returns. So if `sources` emits more and more `IObservable<T>`s over time, these will all be included by `Merge`.

That might sound familiar. In the [Transformation chapter](06_Transformation.md), we looked at the [`SelectMany` operator](06_Transformation.md#selectmany), which is able to flatten multiple observable sources back out into a single observable source. This is just another illustration of why I've described `SelectMany` as a fundamental operator in Rx: strictly speaking we don't need a lot of the operators that Rx gives us because we could build them using `SelectMany`. Here's a simple re-implementation of that last `Merge` overload using `SelectMany`:

```cs
public static IObservable<T> MyMerge<T>(this IObservable<IObservable<T>> sources) =>
    sources.SelectMany(source => source);
```

As well as illustrating that we don't technically need Rx to provide that last `Merge` for us, it's also a good illustration of why it's helpful that it does. It's not immediately obvious what this does—why are we passing a lambda that just returns its argument? Unless you've seen this before, it can take some thought to work out that `SelectMany` expects us to pass a callback that it invokes for each incoming item, but that our input items are already nested sequences, so we can just return each item directly, and `SelectMany` will then take that and merge everything it produces into its output stream. And even if you have internalized `SelectMany` so completely that you know right away that this will just flatten `sources`, you'd still probably find `Observable.Merge(sources)` a more direct expression of intent.

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

For each of the `Merge` overloads that accept variable numbers of sources (either via an array, an `IEnumerable<IObservable<T>>`, or an `IObservable<IObservable<T>>`) there's an additional overload adding a `maxconcurrent` parameter. For example:

```cs
public static IObservable<TSource> Merge<TSource>(this IEnumerable<IObservable<TSource>> sources, int maxConcurrent)
```

This enables you to limit the number of sources that `Merge` accepts inputs from at any single time. If the number of sources available exceeds `maxConcurrent` (either because you passed in a collection with more sources, or because you used the `IObservable<IObservable<T>`-based overload and the source emitted more nested sources than `maxConcurrent`) `Merge` will wait for existing sources to complete before moving onto new ones. A `maxConcurrent` of 1 makes `Merge` behave in the same way as `Concat`.

### Switch

Rx's `Switch` operator takes an `IObservable<IObservable<T>>`, and produces notifications from the most recent nested observable. Each time its source produces a new nested `IObservable<T>`, `Switch` unsubscribes from the previous nested source (unless this is the first source, in which case there won't be a previous one) and subscribes to the latest one.

`Switch` can be used in a 'time to leave' type application. In fact you can see the source code for a modified version of [how Bing provides (or at least provided; the implementation may have changed) notifications telling you that it's time to leave for an appointment](https://github.com/reaqtive/reaqtor/blob/c3ae17f93ae57f3fb75a53f76e60ae69299a509e/Reaqtor/Samples/Remoting/Reaqtor.Remoting.Samples/DomainFeeds.cs#L33-L76). Since that's derived from a real example, it's a little complex, so I'll describe just the essence here.

The basic idea with a 'time to leave' notification is that we using map and route finding services to work out the expected journey time to get to wherever the appointment is, and to use the [`Timer` operator](03_CreatingObservableSequences.md#observabletimer) to create an `IObservable<T>` that will produce a notification when it's time to leave. (Specifically this code produces an `IObservable<TrafficInfo>` which reports the proposed route for the journey, and expected travel time.) However, there are two things that can change, rendering the initial predicted journey time useless. First, traffic conditions can change. When the user created their appointment, we have to guess the expected journey time based on how traffic normally flows at the time of day in question. However, if there turns out to be really bad traffic on the day, the estimate will need to be revised upwards, and we'll need to notify the end user earlier.

The other thing that can change is the user's location. This will also obviously affect the predicted journey time.

To handle this, the system will need observable sources that can report changes in the user's location, and changes in traffic conditions affecting the proposed journey. Every time either of these reports a change, we will need to produce a new estimated journey time, and a new `IObservable<TrafficInfo>` that will produce a notification when it's time to leave.

Every time we revise our estimate, we want to abandon the previously created `IObservable<TrafficInfo>`. (Otherwise, the user will receive a bewildering number of notifications telling them to leave, one for every time we recalculated the journey time.) We just want to use the latest one. And that's exactly what `Switch` does.

You can see the [example for that scenario in the Reaqtor repo](https://github.com/reaqtive/reaqtor/blob/c3ae17f93ae57f3fb75a53f76e60ae69299a509e/Reaqtor/Samples/Remoting/Reaqtor.Remoting.Samples/DomainFeeds.cs#L33-L76). Here, I'm going to present a different, simpler scenario: live searches. As you type, the text is sent to a search service and the results are returned to you as an observable sequence. Most implementations have a slight delay before sending the request so that unnecessary work does not happen. Imagine I want to search for "Intro to Rx". I quickly type in "Into to" and realize I have missed the letter 'r'. I stop briefly and change the text to "Intro ". By now, two searches have been sent to the server. The first search will return results that I do not want. Furthermore, if I were to receive data for the first search merged together with results for the second search, it would be a very odd experience for the user. I really only want results corresponding to the latest search text. This scenario fits perfectly with the `Switch` method.

In this example, there is a source that represents a sequence of search text. Values the user types are represented as the source sequence. Using `Select`, we pass the value of the search to a function that takes a `string` and returns an `IObservable<string>`. This creates our resulting nested sequence, `IObservable<IObservable<string>>`.

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

The previous methods allowed us to flatten multiple sequences sharing a common type into a result sequence of the same type (with various strategies for deciding what to include and what to discard). The operators in this section still take multiple sequences as an input, but attempt to pair values from each sequence to produce a single value for the output sequence. In some cases, they also allow you to provide sequences of different types.

### Zip

`Zip` combines pairs of items from two sequences. So its first output is created by combining the first item from one input with the first item from the other. The second output combines the second item from each input. And so on. The name is meant to evoke a zipper on clothing or a bag, which brings the teeth on each half of the zipper together one pair at a time.

Since `Zip` combines pairs of item in strict order, it will complete when the first of the sequences complete. If one of the sequence has reached its end, then even if the other continues to emit values, there will be nothing to pair any of these values with, so `Zip` just unsubscribes at this point and reports completion.

If either of the sequences produces an error, the sequence returned by `Zip` will report that same error.

If one of the source sequences publishes values faster than the other sequence, the rate of publishing will be dictated by the slower of the two sequences, because it can only emit an item when it has one from each source.

Here's an example:

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

### SequenceEqual

There's another operator that processes pairs of items from two source: `SequenceEqual`. But instead of producing an output for each pair of inputs, this compares each pair, and ultimately produces a single value indicating whether every pair of inputs was equal or not.

In the case where the sources produce different values, `SequenceEqual` produces a single `false` value as soon as it detects this. But if the sources are equal, it can only report this when both have completed because until that happens, it doesn't yet know if there might a difference coming later.

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

### CombineLatest

The `CombineLatest` operator is similar to `Zip` in that it combines pairs of items from its sources. However, instead of pairing the first items, then the second, and so on, `CombineLatest` produces an output any time _either_ of its inputs produces a new value. For each new value to emerge from an input, `CombineLatest` uses that along with the most recently seen value from the other input. The signature is as follows.

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

Some readers may have noticed that this method could produce a lot of duplicate values. For example, if the web server goes down the result sequence will yield '`false`'. If the database then goes down, another (unnecessary) '`false`' value will be yielded. This would be an appropriate time to use the `DistinctUntilChanged` extension method. The corrected code would look like the example below.

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

TODO: these next two sequences were relocated from the now-dropped Sequences of Coincidence chapter. They need editing

## Join								

The `Join` operator allows you to logically join two sequences. Whereas the `Zip` operator would pair values from the two sequences together by index, the `Join` operator allows you join sequences by intersecting windows. Like the `Window` overload we just looked at, you can specify when a window should close via an observable sequence; this sequence is returned from a function that takes an opening value. The `Join` operator has two such functions, one for the first source sequence and one for the second source sequence. Like the `Zip` operator, we also need to provide a selector function to produce the result item from the pair of values.

```csharp
public static IObservable<TResult> Join<TLeft, TRight, TLeftDuration, TRightDuration, TResult>
(
    this IObservable<TLeft> left,
    IObservable<TRight> right,
    Func<TLeft, IObservable<TLeftDuration>> leftDurationSelector,
    Func<TRight, IObservable<TRightDuration>> rightDurationSelector,
    Func<TLeft, TRight, TResult> resultSelector
)
```

This is a complex signature to try and understand in one go, so let's take it one parameter at a time.

`IObservable<TLeft> left` is the source sequence that defines when a window starts. This is just like the `Buffer` and `Window` operators, except that every value published from this source opens a new window. In `Buffer` and `Window`, by contrast, some values just fell into an existing window.

I like to think of `IObservable<TRight> right` as the window value sequence. While the left sequence controls opening the windows, the right sequence will try to pair up with a value from the left sequence.

Let us imagine that our left sequence produces a value, which creates a new window. If the right sequence produces a value while the window is open, then the `resultSelector` function is called with the two values. This is the crux of join, pairing two values from a sequence that occur within the same window. This then leads us to our next question; when does the window close? The answer illustrates both the power and the complexity of the `Join` operator.

When `left` produces a value, a window is opened. That value is also passed, at that time, to the `leftDurationSelector` function, which returns an `IObservable<TLeftDuration>`. When that sequence produces a value or completes, the window for that value is closed. Note that it is irrelevant what the type of `TLeftDuration` is. This initially left me with the feeling that `IObservable<TLeftDuration>` was a bit excessive as you effectively just need some sort of event to say 'Closed'. However, by being allowed to use `IObservable<T>`, you can do some clever manipulation as we will see later.

Let us now imagine a scenario where the left sequence produces values twice as fast as the right sequence. Imagine that in addition we never close the windows; we could do this by always returning `Observable.Never<Unit>()` from the `leftDurationSelector` function. This would result in the following pairs being produced.

Left Sequence

<div class="marble">
<pre class="line">L 0-1-2-3-4-5-</pre>
</div>

Right Sequence

<div class="marble">
<pre class="line">R --A---B---C-</pre>
</div>
<div class="output">
<div class="line">0, A</div>
<div class="line">1, A</div>
<div class="line">0, B</div>
<div class="line">1, B</div>
<div class="line">2, B</div>
<div class="line">3, B</div>
<div class="line">0, C</div>
<div class="line">1, C</div>
<div class="line">2, C</div>
<div class="line">3, C</div>
<div class="line">4, C</div>
<div class="line">5, C</div>
</div>

As you can see, the left values are cached and replayed each time the right produces a value.

Now it seems fairly obvious that, if I immediately closed the window by returning `Observable.Empty<Unit>`, or perhaps `Observable.Return(0)`, windows would never be opened thus no pairs would ever get produced. However, what could I do to make sure that these windows did not overlap- so that, once a second value was produced I would no longer see the first value? Well, if we returned the `left` sequence from the `leftDurationSelector`, that could do the trick. But wait, when we return the sequence `left` from the `leftDurationSelector`, it would try to create another subscription and that may introduce side effects. The quick answer to that is to `Publish` and `RefCount` the `left` sequence. If we do that, the results look more like this.

<div class="marble">
<pre class="line">left  |-0-1-2-3-4-5|</pre>
<pre class="line">right |---A---B---C|</pre>
<pre class="line">result|---1---3---5</pre>
<pre class="line">          A   B   C</pre>
</div>

The last example is very similar to `CombineLatest`, except that it is only producing a pair when the right sequence changes. We could use `Join` to produce our own version of [`CombineLatest`](12_CombiningSequences.html#CombineLatest). If the values from the left sequence expire when the next value from left was notified, then I would be well on my way to implementing my version of `CombineLatest`. However I need the same thing to happen for the right. Luckily the `Join` operator provides a `rightDurationSelector` that works just like the `leftDurationSelector`. This is simple to implement; all I need to do is return a reference to the same left sequence when a left value is produced, and do the same for the right. The code looks like this.

```csharp
public static IObservable<TResult> MyCombineLatest<TLeft, TRight, TResult>
(
    IObservable<TLeft> left,
    IObservable<TRight> right,
    Func<TLeft, TRight, TResult> resultSelector
)
{
    var refcountedLeft = left.Publish().RefCount();
    var refcountedRight = right.Publish().RefCount();

    return Observable.Join(
        refcountedLeft,
        refcountedRight,
        value => refcountedLeft,
        value => refcountedRight,
        resultSelector);
}
```

While the code above is not production quality (it would need to have some gates in place to mitigate race conditions), it shows how powerful `Join` is; we can actually use it to create other operators!

## GroupJoin							

When the `Join` operator pairs up values that coincide within a window, it will pass the scalar values left and right to the `resultSelector`. The `GroupJoin` operator takes this one step further by passing the left (scalar) value immediately to the `resultSelector` with the right (sequence) value. The right parameter represents all of the values from the right sequences that occur within the window. Its signature is very similar to `Join`, but note the difference in the `resultSelector` parameter.

```csharp
public static IObservable<TResult> GroupJoin<TLeft, TRight, TLeftDuration, TRightDuration, TResult>
(
    this IObservable<TLeft> left,
    IObservable<TRight> right,
    Func<TLeft, IObservable<TLeftDuration>> leftDurationSelector,
    Func<TRight, IObservable<TRightDuration>> rightDurationSelector,
    Func<TLeft, IObservable<TRight>, TResult> resultSelector
)
```

If we went back to our first `Join` example where we had

* the `left` producing values twice as fast as the right,
* the left never expiring
* the right immediately expiring

this is what the result may look like

<div class="marble">
<pre class="line">left              |-0-1-2-3-4-5|</pre>
<pre class="line">right             |---A---B---C|</pre>
<pre class="line">0th window values   --A---B---C|</pre>
<pre class="line">1st window values     A---B---C|</pre>
<pre class="line">2nd window values       --B---C|</pre>
<pre class="line">3rd window values         B---C|</pre>
<pre class="line">4th window values           --C|</pre>
<pre class="line">5th window values             C|</pre>
</div>

We could switch it around and have the left expired immediately and the right never expire. The result would then look like this:

<div class="marble">
<pre class="line">left              |-0-1-2-3-4-5|</pre>
<pre class="line">right             |---A---B---C|</pre>
<pre class="line">0th window values   |</pre>
<pre class="line">1st window values     A|</pre>
<pre class="line">2nd window values       A|</pre>
<pre class="line">3rd window values         AB|</pre>
<pre class="line">4th window values           AB|</pre>
<pre class="line">5th window values             ABC|</pre>
</div>

This starts to make things interesting. Perceptive readers may have noticed that with `GroupJoin` you could effectively re-create your own `Join` method by doing something like this:

```csharp
public IObservable<TResult> MyJoin<TLeft, TRight, TLeftDuration, TRightDuration, TResult>(
    IObservable<TLeft> left,
    IObservable<TRight> right,
    Func<TLeft, IObservable<TLeftDuration>> leftDurationSelector,
    Func<TRight, IObservable<TRightDuration>> rightDurationSelector,
    Func<TLeft, TRight, TResult> resultSelector)
{
    return Observable.GroupJoin
    (
        left,
        right,
        leftDurationSelector,
        rightDurationSelector,
        (leftValue, rightValues)=> rightValues.Select(rightValue=>resultSelector(leftValue, rightValue))
    )
    .Merge();
}
```

You could even create a crude version of `Window` with this code:

```csharp
public IObservable<IObservable<T>> MyWindow<T>(IObservable<T> source, TimeSpan windowPeriod)
{
    return Observable.Create<IObservable<T>>(o =>;
    {
        var sharedSource = source
            .Publish()
            .RefCount();

        var intervals = Observable.Return(0L)
            .Concat(Observable.Interval(windowPeriod))
            .TakeUntil(sharedSource.TakeLast(1))
            .Publish()
            .RefCount();

        return intervals.GroupJoin(
                sharedSource, 
                _ => intervals, 
                _ => Observable.Empty<Unit>(), 
                (left, sourceValues) => sourceValues)
            .Subscribe(o);
    });
}
```

For an alternative summary of reducing operators to a primitive set see Bart DeSmet's [excellent MINLINQ post](http://blogs.bartdesmet.net/blogs/bart/archive/2010/01/01/the-essence-of-linq-minlinq.aspx "The essence of LINQ - MinLINQ") (and [follow-up video](http://channel9.msdn.com/Shows/Going+Deep/Bart-De-Smet-MinLINQ-The-Essence-of-LINQ "The essence of LINQ - MINLINQ - Channel9") ). Bart is one of the key members of the team that built Rx, so it is great to get some insight on how the creators of Rx think.

Showcasing `GroupJoin` and the use of other operators turned out to be a fun academic exercise. While watching videos and reading books on Rx will increase your familiarity with it, nothing replaces the experience of actually picking it apart and using it in earnest.

`GroupJoin` and other window operators reduce the need for low-level plumbing of state and concurrency. By exposing a high-level API, code that would be otherwise difficult to write, becomes a cinch to put together. For example, those in the finance industry could use `GroupJoin` to easily produce real-time Volume or Time Weighted Average Prices (VWAP/TWAP).

Rx delivers yet another way to query data in motion by allowing you to interrogate sequences of coincidence. This enables you to solve the intrinsically complex problem of managing state and concurrency while performing matching from multiple sources. By encapsulating these low level operations, you are able to leverage Rx to design your software in an expressive and testable fashion. Using the Rx operators as building blocks, your code effectively becomes a composition of many simple operators. This allows the complexity of the domain code to be the focus, not the otherwise incidental supporting code.


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

## Summary

This chapter covered a set of methods that allow us to combine  observable sequences. This brings us to a close on Part 2. We've looked at the operators that are mostly concerned with defining the computations we want to perform on the data. In Part 3 we will move onto practical concerns such as managing side effects, error handling, and scheduling. The