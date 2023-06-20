---
title: Aggregation
---

# Aggregation

Data is not always tractable is its raw form. Sometimes we need to consolidate, collate, combine or condense the mountains of data we receive. This might just be a case of reducing the volume of data to a manageable level. For example, consider fast moving data from domains like instrumentation, finance, signal processing and operational intelligence. This kind of data can change at a rate of over ten values per second for individual sources, and much higher rates if we're observing multiple sources. Can a person actually consume this? For human consumption, aggregate values like averages, minimums and maximums can be of more use.

We can often achieve more than this. The way in which we combine and correlate may enable us to reveal patterns, providing insights that would not be available from any individual message, or from simple reduction to a single statistical measure. Rx's composability enables us to express complex and subtle computations over streams of data enabling us not just to reduce the volume of messages that users have to deal with, but to increase the amount of value in each message a human receives.

We will start with the simplest aggregation functions, which reduce an observable sequence to a sequence with a single value in some specific way. We then move on to more general-purpose operators that enable you to define your own aggregation mechanisms.

## Simple Numeric Aggregation

Rx supports various standard LINQ operators that reduce all of the values in a sequence down to a single numeric result.


### Count

`Count` tells you how many elements a sequence contains. Although this is a standard LINQ operator, Rx's version deviates from the `IEnumerable<T>` version as Rx will return an observable sequence, not a scalar value. As usual, this is because of the push-related nature of Rx. Rx's `Count` can't demand that its source supply all elements immediately, so it just has to wait until the source says that it has finished.

The sequence that `Count` returns will always be of type `IObservable<int>`, regardless of the source's element type. This will produce nothing until the source completes, at which point it will produce a single value reporting how many elements the source produced, and then it will in turn immediately complete. This example uses `Count` with `Range`, because `Range` produces all of its values as quickly as possible and then completes, meaning we get a result from `Count` immediately:

```csharp
IObservable<int> numbers = Observable.Range(0,3);
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

If you are expecting your sequence to have more values than a 32-bit signed integer can count, you can use the `LongCount` operator instead. This is just the same as `Count` except it returns an `IObservable<long>`.

### Sum

The `Sum` operator adds together all the values in its source, producing the total as its only output. As with `Count`, Rx's `Sum` differs from most other LINQ providers in that it does not produce a scalar as its output—it produces an observable sequence that does nothing until its source completes. When the source completes, the observable returned by `Sum` produces a single value and then immediately completes. This example shows it in use:

```cs
IObservable<int> numbers = Observable.Range(1,5);
numbers.Dump("numbers");
numbers.Sum().Dump("sum");
```

The output shows the numbers produced by the source, and also the single result produced by `Sum`:

```
numbers-->1
numbers-->2
numbers-->3
numbers-->4
numbers-->5
numbers completed
sum-->15
sum completed
```

`Sum` is only able to work with values of type `int`, `long`, `float`, `double` `decimal`, or the nullable versions of these. This means that there are types you might expect to be able to `Sum` that you can't. For example the `BigInteger` type in the `System.Numerics` namespace represents integer values whose size is limited only by available memory, and how long you're prepared to wait for it to perform calculations. (Even basic arithmetic gets very slow on numbers with millions of digits.) You can use `+` to add these together because the type defines an overload for that operator. But `Sum` has historically had no way to find that. The introduction of [generic math in C# 11.0](https://learn.microsoft.com/en-us/dotnet/standard/generics/math#operator-interfaces) means that it would technically be possible to introduce a version of `Sum` that would work for any type `T` that implemented [`IAdditionOperators<T, T, T>`](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.iadditionoperators-3). However, that would mean a dependency on .NET 7.0 (because generic math is not available in order versions), and at the time of writing this, Rx supports .NET 7.0 through its `net6.0` target. It could need to introduce a separate `net7.0` target to enable this, but has not yet done so. (To be fair, [`Sum` in LINQ to Objects also doesn't support this yet](https://github.com/dotnet/runtime/issues/64031).)

If you supply `Sum` with the nullable versions of these types (e.g., your source is an `IObservable<int?>`) then `Sum` will also return a sequence with a nullable item type, and it will produce `null` if any of the input values is `null`.

Although `Sum` can work only with a small, fixed list of numeric types, your source doesn't necessarily have to produce values of those types. `Sum` offers overloads that accept a lambda that extracts a suitable numeric value from each input element. For example, suppose you wanted to answer the following unlikely question: if the next 10 ships that happen to broadcast descriptions of themselves over AIS were put side by side, would they all fit in a channel of some particular width? We could do this by filtering the AIS messages down to those that provide ship size information, using `Take` to collect the next 10 such messages, and then using `Sum`. The Ais.NET library's `IVesselDimensions` interface does not implement addition (and even if it did, we already just saw that Rx wouldn't be able to exploit that), but that's fine: all we need to do is supply a lambda that can take an `IVesselDimensions` and return a value of some numeric type that `Sum` can process:

```cs
IObservable<IVesselDimensions> vesselDimensions = receiverHost.Messages
    .OfType<IVesselDimensions>();

IObservable<int> totalVesselWidths = vesselDimensions
    .Take(10)
    .Sum(dimensions => checked((int)(dimensions.DimensionToPort + dimensions.DimensionToStarboard)));
```

(If you're wondering what's with cast and the `checked` keyword here, AIS defines these values as unsigned integers, so the Ais.NET library reports them as `uint`, which is not a type Rx's `Sum` supports. In practice, it's very unlikely that a vessel will be wide enough to overflow a 32-bit signed integer, so we just cast it to `int`, and the `checked` keyword will throw an exception in the unlikely event that we encounter ship more than 2.1 billion metres wide.)

### Average

The standard LINQ operator `Average` effectively calculates the value that `Sum` would calculate, and then divides it by the value that `Count` would calculate. And once again, whereas most LINQ implementations would return a scalar, Rx's `Average` produces an observable.

Although `Average` can process values of the same numeric types as `Sum`, the output type will be different in some cases. If the source is `IObservable<int>`, or if you use one of the overloads that takes a lambda that extracts the value from the source, and that lambda returns an `int`, the result will be a `double.` This is because the average of a set of whole numbers is not necessarily a whole number. Likewise, averaging `long` values produces a `double`. However, inputs of type `decimal` produce outputs of type `decimal`, and likewise `float` inputs produce a `float output.

As with `Sum`, if the inputs to `Average` are nullable, the output will be too.


### Min and Max

Rx implements the standard LINQ `Min` and `Max` operators which find the element with the highest or lowest value. As with all the other operators in this section, these do not return scalars, and instead return an `IObservable<T>` that produces a single value.

Rx defines specialized implementations for the same numeric types that `Sum` and `Average` support. However, unlike those operators it also defines an overload that will accept source items of any type. When you use `Min` or `Max` on a source type where Rx does not define a specialized implementation, it uses [`Comparer<T>.Default`](https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.comparer-1.default) to compare items. There is also an overload enabling you to pass a comparer.

As with `Sum` and `Average` there are overloads that accept a callback. If you use these overloads, `Min` and `Max` will invoke this callback for each source item, and will look for the lowest or highest value that your callback returns. Note that the single output they eventually produce will be a value returned by the callback, and not the original source item from which that value was derived. To see what that means, look at this example:

```cs
IObservable<int> widthOfWidestVessel = vesselDimensions
    .Take(10)
    .Max(dimensions => checked((int)(dimensions.DimensionToPort + dimensions.DimensionToStarboard)));
```

`Max` returns an `IObservable<int>` here, which will be the width of the widest vessel out of the next 10 messages that report vessel dimensions. But what if you didn't just want to see the width? What if you wanted the whole message?


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


## Simple Boolean Aggregation

LINQ defines several standard operators that reduce entire sequences to a single boolean value.

### Any

There are two ways to use `Any`. First we can look at the parameterless overload for the extension method `Any`. This effectively asks the question "are there any elements in this sequence?" It returns an observable sequence that will produce a single value of `false` if the source completes without emitting any values. If the source does produce a value however, then when the first value is produced, the result sequence will immediately produce `true` and then complete. If the first notification it gets is an error, then it will pass that error on.

```csharp
var subject = new Subject<int>();
subject.Subscribe(Console.WriteLine, () => Console.WriteLine("Subject completed"));
var any = subject.Any();

any.Subscribe(b => Console.WriteLine("The subject has any values? {0}", b));

subject.OnNext(1);
subject.OnCompleted();
```

Output:

```
1
The subject has any values? True
subject completed
```

If we now remove the OnNext(1), the output will change to the following

```
subject completed
The subject has any values? False
```

In the case where the source does produce a value, `Any` immediately unsubscribes from it. So if the source wants to report an error, `Any` will only see this if that is the first notification it produces.

```csharp
var subject = new Subject<int>();
subject.Subscribe(Console.WriteLine,
    ex => Console.WriteLine("subject OnError : {0}", ex),
    () => Console.WriteLine("Subject completed"));
IObservable<bool> any = subject.Any();

any.Subscribe(b => Console.WriteLine("The subject has any values? {0}", b),
    ex => Console.WriteLine(".Any() OnError : {0}", ex),
    () => Console.WriteLine(".Any() completed"));

subject.OnError(new Exception());
```

Output:

```
subject OnError : System.Exception: Fail
.Any() OnError : System.Exception: Fail
```

The `Any` method also has an overload that takes a predicate. This effectively asks a slightly different question: "are there any elements in this sequence that meet these criteria?" The effect is similar to using `Where` followed by the no-arguments form of `Any`.

```csharp
IObservable<bool> any = subject.Any(i => i > 2);
// Functionally equivalent to 
IObservable<bool> longWindedAny = subject.Where(i => i > 2).Any();
```

As an exercise, write your own version of the two `Any` extension method overloads. While the answer may not be immediately obvious, we have covered enough material for you to create this using the methods you know...

Example of the `Any` extension methods written with `Observable.Create`:

```csharp
public static IObservable<bool> MyAny<T>(this IObservable<T> source)
{
    return Observable.Create<bool>(
        o =>
        {
            var hasValues = false;
            return source
                .Take(1)
                .Subscribe(
                    _ => hasValues = true,
                    o.OnError,
                    () =>
                    {
                        o.OnNext(hasValues);
                        o.OnCompleted();
                    });
        });
}

public static IObservable<bool> MyAny<T>(
    this IObservable<T> source, 
    Func<T, bool> predicate)
{
    return source
        .Where(predicate)
        .MyAny();
}
```

## All

The `All` operator is similar to the `Any` method that takes a predicate, except that all values must meet the predicate. As soon as a value does not meet the predicate a `false` value is returned then the output sequence completed. If the source reaches its end without producing any elements that do not satisfy the predicate, then `All` will push `true` as its value. (A consequence of this is that if you use `All` on an empty sequence, the result will be a sequence that produces `true`. This is consistent with how `All` works in other LINQ providers, but it might be surprising for anyone not familiar with the formal logic convention known as [vacuous truth](https://en.wikipedia.org/wiki/Vacuous_truth).)

Once `All` decides to produce a `false` value, it immediately unsubscribes from the source (just like `Any` does as soon as it determines that it can produce `true`.) If the source produces an error before this happens, the error will be passed along to the subscriber of the `All` method. 

```csharp
var subject = new Subject<int>();
subject.Subscribe(Console.WriteLine, () => Console.WriteLine("Subject completed"));
var all = subject.All(i => i < 5);
all.Subscribe(b => Console.WriteLine("All values less than 5? {0}", b));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(6);
subject.OnNext(2);
subject.OnNext(1);
subject.OnCompleted();
```

Output:

```
1
2
6
All values less than 5? False
all completed
2
1
subject completed
```


## IsEmpty

The LINQ `IsEmpty` operator is logically the opposite of the no-arguments `Any` method. It returns `true` if and only if the source completes without producing any elements, and if the source produces an item, `IsEmpty` produces `false` and immediately unsubscribes.


## Contains

The `Contains` operator determines whether a particular element is present in a sequence. You could implement it using `Any`, just supplying a callback that compares each item with the value you're looking for. However, it will typically be slightly more succinct, and may be a more direct expression of intent to write `Contains`.

```csharp
var subject = new Subject<int>();
subject.Subscribe(
    Console.WriteLine, 
    () => Console.WriteLine("Subject completed"));

var contains = subject.Contains(2);

contains.Subscribe(
    b => Console.WriteLine("Contains the value 2? {0}", b),
    () => Console.WriteLine("contains completed"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
    
subject.OnCompleted();
```

Output:

```
1
2
Contains the value 2? True
contains completed
3
Subject completed
```

There is also an overload to `Contains` that allows you to specify an implementation of `IEqualityComparer<T>` other than the default for the type. This can be useful if you have a sequence of custom types that may have some special rules for equality depending on the use case.


Next time: continue from here.

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



TODO: integrate these, which were previously in the vaguely titled Inspection chapter





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
