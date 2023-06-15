---
title: Aggregation
---

# Aggregation						

Data is not always valuable is its raw form. Sometimes we need to consolidate, collate, combine or condense the mountains of data we receive into more consumable bite sized chunks. Consider fast moving data from domains like instrumentation, finance, signal processing and operational intelligence. This kind of data can change at a rate of over ten values per second. Can a person actually consume this? Perhaps for human consumption, aggregate values like averages, minimums and maximums can be of more use.

Continuing with the theme of reducing an observable sequence, we will look at the aggregation functions that are available to us in Rx. Our first set of methods continues on from our last chapter, as they take an observable sequence and reduce it to a sequence with a single value. We then move on to find operators that can transition a sequence back to a scalar value, a functional fold.

Just before we move on to introducing the new operators, we will quickly create our own extension method. We will use this 'Dump' extension method to help build our samples.

```csharp
public static class SampleExtentions
{
    public static void Dump<T>(this IObservable<T> source, string name)
    {
        source.Subscribe(
            i=>Console.WriteLine("{0}-->{1}", name, i), 
            ex=>Console.WriteLine("{0} failed-->{1}", name, ex.Message),
            ()=>Console.WriteLine("{0} completed", name));
    }
}
```

Those who use [LINQPad](http://www.linqpad.net/) will recognize that this is the source of inspiration. For those who have not used LINQPad, I highly recommend it. It is perfect for whipping up quick samples to validate a snippet of code. LINQPad also fully supports the `IObservable<T>` type.

## Count					

`Count` is a very familiar extension method for those that use LINQ on `IEnumerable<T>`. Like all good method names, it "does what it says on the tin". The Rx version deviates from the `IEnumerable<T>` version as Rx will return an observable sequence, not a scalar value. The return sequence will have a single value being the count of the values in the source sequence. Obviously we cannot provide the count until the source sequence completes.

```csharp
var numbers = Observable.Range(0,3);
numbers.Dump("numbers");
numbers.Count().Dump("count");
```

Output:

```
numbers-->1
numbers-->2
numbers-->3
numbers Completed
count-->3
count Completed
```

If you are expecting your sequence to have more values than a 32 bit integer can hold, there is the option to use the `LongCount` extension method. This is just the same as `Count` except it returns an `IObservable<long>`.

## Min, Max, Sum and Average			

Other common aggregations are `Min`, `Max`, `Sum` and `Average`. Just like `Count`, these all return a sequence with a single value. Once the source completes the result sequence will produce its value and then complete.

```csharp
var numbers = new Subject<int>();

numbers.Dump("numbers");
numbers.Min().Dump("Min");
numbers.Average().Dump("Average");

numbers.OnNext(1);
numbers.OnNext(2);
numbers.OnNext(3);
numbers.OnCompleted();
```

Output:

```
numbers-->1
numbers-->2
numbers-->3
numbers Completed
min-->1
min Completed
avg-->2
avg Completed
```

The `Min` and `Max` methods have overloads that allow you to provide a custom implementation of an `IComparer<T>` to sort your values in a custom way. The `Average` extension method specifically calculates the mean (as opposed to median or mode) of the sequence. For sequences of integers (int or long) the output of `Average` will be an `IObservable<double>`. If the source is of nullable integers then the output will be `IObservable<double?>`. All other numeric types (`float`, `double`, `decimal` and their nullable equivalents) will result in the output sequence being of the same type as the input sequence.

## Functional folds					

Finally we arrive at the set of methods in Rx that meet the functional description of catamorphism/fold. These methods will take an `IObservable<T>` and produce a `T`.

Caution should be prescribed whenever using any of these fold methods on an observable sequence, as they are all blocking. The reason you need to be careful with blocking methods is that you are moving from an asynchronous paradigm to a synchronous one, and without care you can introduce concurrency problems such as locking UIs and deadlocks. We will take a deeper look into these problems in a later chapter when we look at concurrency.

> It is worth noting that in the soon to be released .NET 4.5 and Rx 2.0 will provide support for avoiding these concurrency problems. The new `async`/`await` keywords and related features in Rx 2.0 can help exit the monad in a safer way.

TODO: this was where First, Last, and Single were. (That was an odd choice. They are technically catamorphisms, but they are degenerate cases. `Aggregate` is the canonical example but that wasn't in this section! `Sum`, `Average`, `Min`, and `Max` are also all good examples (more specialized than Aggregate, but at least they look at every input), but they're also not in here. To be fair, the Rx source code puts FirstAsync and SingleAsync in Observable.Aggregates.cs. But it's the fact that this section _only_ contained these, and none of the other more obviously aggregating catamorphisms.) I've moved those out into filtering, because they feel more akin to Take and Last. From a structural algebra perspective these are different kinds of things, but in terms of what they actually do, they are positional filters, so they seem to fit better in the filtering chapter than Aggregation because they don't aggregate.


## Build your own aggregations		

If the provided aggregations do not meet your needs, you can build your own. Rx provides two different ways to do this.

### Aggregate						

The `Aggregate` method allows you to apply an accumulator function to the sequence. For the basic overload, you need to provide a function that takes the current state of the accumulated value and the value that the sequence is pushing. The result of the function is the new accumulated value. 

This overload signature is as follows:

```csharp
IObservable<TSource> Aggregate<TSource>(
    this IObservable<TSource> source, 
    Func<TSource, TSource, TSource> accumulator)
```

If you wanted to produce your own version of `Sum` for `int` values, you could do so by providing a function that just adds to the current state of the accumulator.

```csharp
var sum = source.Aggregate((acc, currentValue) => acc + currentValue);
```

This overload of `Aggregate` has several problems. First is that it requires the aggregated value must be the same type as the sequence values. We have already seen in other aggregates like `Average` this is not always the case. Secondly, this overload needs at least one value to be produced from the source or the output will error with an `InvalidOperationException`. 

It should be completely valid for us to use `Aggregate` to create our own `Count` or `Sum` on an empty sequence. To do this you need to use the other overload. This overload takes an extra parameter which is the seed. The seed value provides an initial accumulated value. It also allows the aggregate type to be different to the value type.

```csharp
IObservable<TAccumulate> Aggregate<TSource, TAccumulate>(
    this IObservable<TSource> source, 
    TAccumulate seed, 
    Func<TAccumulate, TSource, TAccumulate> accumulator)
```

To update our `Sum` implementation to use this overload is easy. Just add the seed which will be 0. This will now return 0 as the sum when the sequence is empty which is just what we want. You also now can also create your own version of `Count`.

```csharp
var sum = source.Aggregate(0, (acc, currentValue) => acc + currentValue);
var count = source.Aggregate(0, (acc, currentValue) => acc + 1);
// or using '_' to signify that the value is not used.
var count = source.Aggregate(0, (acc, _) => acc + 1);
```

As an exercise write your own `Min` and `Max` methods using `Aggregate`. You will probably find the `IComparer<T>` interface useful, and in particular the static `Comparer<T>.Default` property. When you have done the exercise, continue to the example implementations...

Examples of creating `Min` and `Max` from `Aggregate`:

```csharp
public static IObservable<T> MyMin<T>(this IObservable<T> source)
{
    return source.Aggregate(
        (min, current) => Comparer<T>
            .Default
            .Compare(min, current) > 0 
                ? current 
                : min);
}

public static IObservable<T> MyMax<T>(this IObservable<T> source)
{
    var comparer = Comparer<T>.Default;
    Func<T, T, T> max = 
        (x, y) =>
        {
            if(comparer.Compare(x, y) < 0)
            {
                return y;
            }
            return x;
        };
    return source.Aggregate(max);
}
```

### Scan								

While `Aggregate` allows us to get a final value for sequences that will complete, sometimes this is not what we need. If we consider a use case that requires that we get a running total as we receive values, then `Aggregate` is not a good fit. `Aggregate` is also not a good fit for infinite sequences. The `Scan` extension method however meets this requirement perfectly. The signatures for both `Scan` and `Aggregate` are the same; the difference is that `Scan` will push the _result_ from every call to the accumulator function.

So instead of being an aggregator that reduces a sequence to a single value sequence, it is an accumulator that we return an accumulated value for each value of the source sequence. In this example we produce a running total.

```csharp
var numbers = new Subject<int>();
var scan = numbers.Scan(0, (acc, current) => acc + current);

numbers.Dump("numbers");
scan.Dump("scan");

numbers.OnNext(1);
numbers.OnNext(2);
numbers.OnNext(3);
numbers.OnCompleted();
```

Output:

```
numbers-->1
sum-->1
numbers-->2
sum-->3
numbers-->3
sum-->6
numbers completed
sum completed
```

It is probably worth pointing out that you use `Scan` with `TakeLast()` to produce `Aggregate`.

```csharp
source.Aggregate(0, (acc, current) => acc + current);
// is equivalent to 
source.Scan(0, (acc, current) => acc + current).TakeLast();
```

As another exercise, use the methods we have covered so far in the book to produce a sequence of running minimum and running maximums. The key here is that each time we receive a value that is less than (or more than for a Max operator) our current accumulator we should push that value and update the accumulator value. We don't however want to push duplicate values. For example, given a sequence of [2, 1, 3, 5, 0] we should see output like [2, 1, 0] for the running minimum, and [2, 3, 5] for the running maximum. We don't want to see [2, 1, 2, 2, 0] or [2, 2, 3, 5, 5]. Continue to see an example implementation.

Example of a running minimum:

```csharp
var comparer = Comparer<T>.Default;
Func<T,T,T> minOf = (x, y) => comparer.Compare(x, y) < 0 ? x: y;
var min = source.Scan(minOf).DistinctUntilChanged();
```

Example of a running maximum:

```csharp
public static IObservable<T> RunningMax<T>(this IObservable<T> source)
{
    return source.Scan(MaxOf)
        .Distinct();
}

private static T MaxOf<T>(T x, T y)
{
    var comparer = Comparer<T>.Default;
    if (comparer.Compare(x, y) < 0)
    {
        return y;
    }
    return x;
}
```

While the only functional differences between the two examples is checking greater instead of less than, the examples show two different styles. Some people prefer the terseness of the first example, others like their curly braces and the verbosity of the second example. The key here was to compose the `Scan` method with the `Distinct` or `DistinctUntilChanged` methods. It is probably preferable to use the `DistinctUntilChanged` so that we internally are not keeping a cache of all values.

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