---
title: Partitioning
---

# Partitioning


## Partitioning

Rx also gives you the ability to partition your sequence with features like the standard LINQ operator `GroupBy`. This can be useful for taking a single sequence and fanning out to many subscribers or perhaps taking aggregates on partitions.

### MinBy and MaxBy

The `MinBy` and `MaxBy` operators allow you to partition your sequence based on a key selector function. Key selector functions are common in other LINQ operators like the `IEnumerable<T>` `ToDictionary` or `GroupBy` and the [`Distinct`}(05_Filtering.html#Distinct) method. Each method will return you the values from the key that was the minimum or maximum respectively.

```csharp
// Returns an observable sequence containing a list of zero or more elements that have a 
//  minimum key value.
public static IObservable<IList<TSource>> MinBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector)
{...}

public static IObservable<IList<TSource>> MinBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    IComparer<TKey> comparer)
{...}

// Returns an observable sequence containing a list of zero or more elements that have a
//  maximum key value.
public static IObservable<IList<TSource>> MaxBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector)
{...}

public static IObservable<IList<TSource>> MaxBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    IComparer<TKey> comparer)
{...}
```

Take note that each `Min` and `Max` operator has an overload that takes a comparer. This allows for comparing custom types or custom sorting of standard types.

Consider a sequence from 0 to 10. If we apply a key selector that partitions the values in to groups based on their modulus of 3, we will have 3 groups of values. The values and their keys will be as follows:

```csharp
Func<int, int> keySelector = i => i % 3;
```

- 0, key: 0
- 1, key: 1
- 2, key: 2
- 3, key: 0
- 4, key: 1
- 5, key: 2
- 6, key: 0
- 7, key: 1
- 8, key: 2
- 9, key: 0

We can see here that the minimum key is 0 and the maximum key is 2. If therefore, we applied the `MinBy` operator our single value from the sequence would be the list of [0,3,6,9]. Applying the `MaxBy` operator would produce the list [2,5,8]. The `MinBy` and `MaxBy` operators will only yield a single value (like an `AsyncSubject`) and that value will be an `IList<T>` with zero or more values.

If instead of the values for the minimum/maximum key, you wanted to get the minimum value for each key, then you would need to look at `GroupBy`.

TODO: I wrote this but it was totally wrong! Rework to explain how Rx's Min/MaxBy are just not the same thing as LINQ to Objects' ones.

### MinBy and MaxBy

Rx offers two subtle variations on `Min` and `Max`: `MinBy` and `MaxBy`. (These are not universally supported as LINQ operators. .NET only added these for LINQ to Objects in .NET 6.0. Rx has had them for much longer, so these were once Rx-specific operators that have now effectively been retconned as standard LINQ operators.) This example is similar to the preceding one, but replaces `Max` with `MaxBy`:

```cs
IObservable<IVesselDimensions> widthOfWidestVessel = vesselDimensions
    .Take(10)
    .MaxBy(dimensions => checked((int)(dimensions.DimensionToPort + dimensions.DimensionToStarboard)));
```

Notice that the type of sequence we get is different. `Max` returned an `IObservable<int>`, because it invokes the callback for every item in the source, and then produces the highest of the values that our callback returned. But with `MaxBy`, we get back an `IObservable<IVesselDimensions>`.

`MinBy` and `MaxBy` return a sequence of the same type as their inputs. Instead of returning the minimum or maximum value, they remember which particular source value caused the callback to emit the minimum or maximum value, and once the source completes, they return that source value (instead of the value the callback returned for that source value, which is what the callback-based overloads of `Min` and `Max` do).

`MinBy` and `MaxBy` are only available in the form where you supply a callback. The point of these operators is that they separate out the criteria for comparison from the result. (You could achieve the same effect by calling `Min` or `Max` with a custom comparer, but these operators are typically more convenient.)




### GroupBy

The `GroupBy` operator allows you to partition your sequence just as `IEnumerable<T>`'s `GroupBy` operator does. In a similar fashion to how the `IEnumerable<T>` operator returns an `IEnumerable<IGrouping<TKey, T>>`, the `IObservable<T>` `GroupBy` operator returns an `IObservable<IGroupedObservable<TKey, T>>`.

```csharp
// Transforms a sequence into a sequence of observable groups, 
//  each of which corresponds to a unique key value, 
//  containing all elements that share that same key value.
public static IObservable<IGroupedObservable<TKey, TSource>> GroupBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector)
{...}

public static IObservable<IGroupedObservable<TKey, TSource>> GroupBy<TSource, TKey>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    IEqualityComparer<TKey> comparer)
{...}

public static IObservable<IGroupedObservable<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector)
{...}

public static IObservable<IGroupedObservable<TKey, TElement>> GroupBy<TSource, TKey, TElement>(
    this IObservable<TSource> source, 
    Func<TSource, TKey> keySelector, 
    Func<TSource, TElement> elementSelector, 
    IEqualityComparer<TKey> comparer)
{...}
```

I find the last two overloads a little redundant as we could easily just compose a `Select` operator to the query to get the same functionality.

In a similar fashion that the `IGrouping<TKey, T>` type extends the `IEnumerable<T>`, the `IGroupedObservable<T>` just extends `IObservable<T>` by adding a `Key` property. The use of the `GroupBy` effectively gives us a nested observable sequence.

To use the `GroupBy` operator to get the minimum/maximum value for each key, we can first partition the sequence and then `Min`/`Max` each partition.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(0.1)).Take(10);
var group = source.GroupBy(i => i % 3);
group.Subscribe(
    grp => 
        grp.Min().Subscribe(
            minValue => 
            Console.WriteLine("{0} min value = {1}", grp.Key, minValue)),
    () => Console.WriteLine("Completed"));
```

The code above would work, but it is not good practice to have these nested subscribe calls. We have lost control of the nested subscription, and it is hard to read. When you find yourself creating nested subscriptions, you should consider how to apply a better pattern. In this case we can use `SelectMany` which we will look at in the next chapter.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(0.1)).Take(10);
var group = source.GroupBy(i => i % 3);
group.SelectMany(
        grp =>
            grp.Max()
            .Select(value => new { grp.Key, value }))
    .Dump("group");
```

### Nested observables

The concept of a sequence of sequences can be somewhat overwhelming at first, especially if both sequence types are `IObservable`. While it is an advanced topic, we will touch on it here as it is a common occurrence with Rx. I find it easier if I can conceptualize a scenario or example to understand concepts better.

Examples of Observables of Observables:

<dl>
    <dt>Partitions of Data</dt>
    <dd>
        You may partition data from a single source so that it can easily be filtered and
        shared to many sources. Partitioning data may also be useful for aggregates as we
        have seen. This is commonly done with the `GroupBy` operator.
    </dd>
    <dt>Online Game servers</dt>
    <dd>
        Consider a sequence of servers. New values represent a server coming online. The
        value itself is a sequence of latency values allowing the consumer to see real time
        information of quantity and quality of servers available. If a server went down
        then the inner sequence can signify that by completing.
    </dd>
    <dt>Financial data streams</dt>
    <dd>
        New markets or instruments may open and close during the day. These would then stream
        price information and could complete when the market closes.
    </dd>
    <dt>Chat Room</dt>
    <dd>
        Users can join a chat (outer sequence), leave messages (inner sequence) and leave
        a chat (completing the inner sequence).
    </dd>
    <dt>File watcher</dt>
    <dd>
        As files are added to a directory they could be watched for modifications (outer
        sequence). The inner sequence could represent changes to the file, and completing
        an inner sequence could represent deleting the file.
    </dd>
</dl>

Considering these examples, you could see how useful it could be to have the concept of nested observables. There are a suite of operators that work very well with nested observables such as `SelectMany`, `Merge` and `Switch` that we look at in future chapters.

When working with nested observables, it can be handy to adopt the convention that a new sequence represents a creation (e.g. A new partition is created, new game host comes online, a market opens, users joins a chat, creating a file in a watched directory). You can then adopt the convention for what a completed inner sequence represents (e.g. Game host goes offline, Market Closes, User leave chat, File being watched is deleted). The great thing with nested observables is that a completed inner sequence can effectively be restarted by creating a new inner sequence.

In this chapter we are starting to uncover the power of LINQ and how it applies to Rx. We chained methods together to recreate the effect that other methods already provide. While this is academically nice, it also allows us to starting thinking in terms of functional composition. We have also seen that some methods work nicely with certain types: `First()` + `BehaviorSubject<T>`, `Single()` + `AsyncSubject<T>`, `Single()` + `Aggregate()` etc. We have covered the second of our three classifications of operators, _catamorphism_. Next we will discover more methods to add to our functional composition tool belt and also find how Rx deals with our third functional concept, _bind_.

Consolidating data into groups and aggregates enables sensible consumption of mass data. Fast moving data can be too overwhelming for batch processing systems and human consumption. Rx provides the ability to aggregate and partition on the fly, enabling real-time reporting without the need for expensive CEP or OLAP products.
