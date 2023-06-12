---
title : Transforming Simple Sequences into Valuable Insights
---

TODO: this feels more like a "PART" heading and introduction, because it's not just this chapter that covers this ground. The next four chapters are really all connected with this.

# Transforming Simple Sequences into Valuable Insights


TODO: this categorization feels a bit arbitrary. For one thing, aggregation and folding are the same thing. `Aggregate` is just LINQ's name for `fold`. The distinction being made here is a minor technical distinction: is the fold result emitted from an `IObservable<T>` or returned directly as a `T`. That's really an operational distinction—it just provides two slightly different technical modes of consuming the same basic capability.

We can categorize operators that reduce a sequence to the following:

<dl>
    <dt>Filter and partition operators</dt>
    <dd>
        Reduce the source sequence to a sequence with at most the same number of elements</dd>
    <dt>Aggregation operators</dt>
    <dd>
        Reduce the source sequence to a sequence with a single element</dd>
    <dt>Fold operators</dt>
    <dd>
        Reduce the source sequence to a single element as a scalar value</dd>
</dl>

TODO contd...: so how do I want to categorise it? Maybe:

* testing/discrimination/scrutinisation/picking
    * where, ignoreelements (segue by pointing out that Where might filter out everything but will still forward end/error)
    * OfType
    * Skip/Take[While/Until]
    * distinct
    * First(OrDefault), Last(OrDefault), Single(OrDefault)
    * DefaultIfEmpty
    * ElementAt

* transformation
    * Select
    * Cast
    * SelectMany
    * Materialize/Dematerialize
* aggregation
    * to Boolean
        * Any
        * All
        * Contains
        * SequenceEqual
    * To numeric
        * Count
        * Sum, Average
        * Min(By), Max(By)
    * Custom
        * Aggregate
        * Scan
* partitioning
    * GroupBy
    * Buffer
    * Window
* combination
    * Concat
    * Repeat
    * Zip
    * CombineLatest
    * StartWith (prepend)
    * Append?
    * merge
    * join
    * Amb
    * Switch
    * And/Then/When
* Error Handling
    * Catch
    * Finally
    * Using
    * Retry
    * OnErrorResumeNext
* Timing
    * TimeStamp TimeInterval
    * Delay
    * Sample
    * Throttle
    * Timeout
    * (Buffer/Window again?)


We discovered that the creation of an observable sequence from a scalar value is defined as _anamorphism_ or described as an _'unfold'_. We can think of the anamorphism from `T` to `IObservable<T>` as an 'unfold'. This could also be referred to as "entering the monad" where in this case (and for most cases in this book) the monad is `IObservable<T>`. What we will now start looking at are methods that eventually get us to the inverse which is defined as _catamorphism_ or a `fold`. Other popular names for fold are 'reduce', 'accumulate' and 'inject'.

## Where								
Applying a filter to a sequence is an extremely common exercise and the most common filter is the `Where` clause. In Rx you can apply a where clause with the `Where` extension method. For those that are unfamiliar, the signature of the `Where` method is as follows:

    IObservable<T> Where(this IObservable<T> source, Fun<T, bool> predicate)

Note that both the source parameter and the return type are the same. This allows for a fluent interface, which is used heavily throughout Rx and other LINQ code. In this example we will use the `Where` to filter out all even values produced from a `Range` sequence.

```csharp
var oddNumbers = Observable.Range(0, 10)
    .Where(i => i % 2 == 0)
    .Subscribe(
        Console.WriteLine, 
        () => Console.WriteLine("Completed"));
```

Output:

```
0
2
4
6
8
Completed
```

The `Where` operator is one of the many standard LINQ operators. This and other LINQ operators are common use in the various implementations of query operators, most notably the `IEnumerable<T>` implementation. In most cases the operators behave just as they do in the `IEnumerable<T>` implementations, but there are some exceptions. We will discuss each implementation and explain any variation as we go. By implementing these common operators Rx also gets language support for free via C# query comprehension syntax. For the examples in this book however, we will keep with using extension methods for consistency.

## Distinct and DistinctUntilChanged	
As I am sure most readers are familiar with the `Where` extension method for `IEnumerable<T>`, some will also know the `Distinct` method. In Rx, the `Distinct` method has been made available for observable sequences too. For those that are unfamiliar with `Distinct`, and as a recap for those that are, `Distinct` will only pass on values from the source that it has not seen before.

```csharp
var subject = new Subject<int>();
var distinct = subject.Distinct();
    
subject.Subscribe(
    i => Console.WriteLine("{0}", i),
    () => Console.WriteLine("subject.OnCompleted()"));

distinct.Subscribe(
    i => Console.WriteLine("distinct.OnNext({0})", i),
    () => Console.WriteLine("distinct.OnCompleted()"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnNext(1);
subject.OnNext(1);
subject.OnNext(4);
subject.OnCompleted();
```

Output:

```
1
distinct.OnNext(1)
2
distinct.OnNext(2)
3
distinct.OnNext(3)
1
1
4
distinct.OnNext(4)
subject.OnCompleted()
distinct.OnCompleted()
```

Take special note that the value 1 is pushed 3 times but only passed through the first time. There are overloads to `Distinct` that allow you to specialize the way an item is determined to be distinct or not. One way is to provide a function that returns a different value to use for comparison. Here we look at an example that uses a property from a custom class to define if a value is distinct.

```csharp
public class Account
{
    public int AccountId { get; set; }
    //... etc
}

public void Distinct_with_KeySelector()
{
    var subject = new Subject<Account>();
    var distinct = subject.Distinct(acc => acc.AccountId);
}
```

In addition to the `keySelector` function that can be provided, there is an overload that takes an `IEqualityComparer<T>` instance. This is useful if you have a custom implementation that you can reuse to compare instances of your type `T`. Lastly there is an overload that takes a `keySelector` and an instance of `IEqualityComparer<TKey>`. Note that the equality comparer in this case is aimed at the selected key type (`TKey`), not the type `T`.

A variation of `Distinct`, that is peculiar to Rx, is `DistinctUntilChanged`. This method will surface values only if they are different from the previous value. Reusing our first `Distinct` example, note the change in output.

```csharp
var subject = new Subject<int>();
var distinct = subject.DistinctUntilChanged();
    
subject.Subscribe(
    i => Console.WriteLine("{0}", i),
    () => Console.WriteLine("subject.OnCompleted()"));

distinct.Subscribe(
    i => Console.WriteLine("distinct.OnNext({0})", i),
    () => Console.WriteLine("distinct.OnCompleted()"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnNext(1);
subject.OnNext(1);
subject.OnNext(4);
subject.OnCompleted();
```

Output:

```
1
distinct.OnNext(1)
2
distinct.OnNext(2)
3
distinct.OnNext(3)
1
distinct.OnNext(1)
1
4
distinct.OnNext(4)
subject.OnCompleted()
distinct.OnCompleted()

```

The difference between the two examples is that the value 1 is pushed twice. However the third time that the source pushes the value 1, it is immediately after the second time value 1 is pushed. In this case it is ignored. Teams I have worked with have found this method to be extremely useful in reducing any noise that a sequence may provide.

## IgnoreElements					

The `IgnoreElements` extension method is a quirky little tool that allows you to receive the `OnCompleted` or `OnError` notifications. We could effectively recreate it by using a `Where` method with a predicate that always returns false.

```csharp
var subject = new Subject<int>();

// Could use subject.Where(_=>false);
var noElements = subject.IgnoreElements();

subject.Subscribe(
    i=>Console.WriteLine("subject.OnNext({0})", i),
    () => Console.WriteLine("subject.OnCompleted()"));

noElements.Subscribe(
    i=>Console.WriteLine("noElements.OnNext({0})", i),
    () => Console.WriteLine("noElements.OnCompleted()"));

subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnCompleted();
```

Output:

```
subject.OnNext(1)
subject.OnNext(2)
subject.OnNext(3)
subject.OnCompleted()
noElements.OnCompleted()

```

As suggested earlier we could use a `Where` to produce the same result

```csharp
subject.IgnoreElements();
// Equivalent to 
subject.Where(value=>false);
// Or functional style that implies that the value is ignored.
subject.Where(_=>false);
```

Just before we leave `Where` and `IgnoreElements`, I wanted to just quickly look at the last line of code. Until recently, I personally was not aware that '`_`' was a valid variable name; however it is commonly used by functional programmers to indicate an ignored parameter. This is perfect for the above example; for each value we receive, we ignore it and always return false. The intention is to improve the readability of the code via convention.

## Skip and Take						

The other key methods to filtering are so similar I think we can look at them as one big group. First we will look at `Skip` and `Take`. These act just like they do for the `IEnumerable<T>` implementations. These are the most simple and probably the most used of the Skip/Take methods. Both methods just have the one parameter; the number of values to skip or to take.

If we first look at `Skip`, in this example we have a range sequence of 10 items and we apply a `Skip(3)` to it.

```csharp
Observable.Range(0, 10)
    .Skip(3)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
```

Output:

```
3
4
5
6
7
8
9
Completed

```

Note the first three values (0, 1 &amp; 2) were all ignored from the output. Alternatively, if we used `Take(3)` we would get the opposite result; i.e. we would only get the first 3 values and then the Take operator would complete the sequence.

```csharp
Observable.Range(0, 10)
    .Take(3)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
```

Output:

```
0
1
2
Completed

```

Just in case that slipped past any readers, it is the `Take` operator that completes once it has received its count. We can prove this by applying it to an infinite sequence.

```csharp
Observable.Interval(TimeSpan.FromMilliseconds(100))
    .Take(3)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
```

Output:

```
0
1
2
Completed

```

### SkipWhile and TakeWhile			

The next set of methods allows you to skip or take values from a sequence while a predicate evaluates to true. For a `SkipWhile` operation this will filter out all values until a value fails the predicate, then the remaining sequence can be returned.

```csharp
var subject = new Subject<int>();
subject
    .SkipWhile(i => i < 4)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnNext(4);
subject.OnNext(3);
subject.OnNext(2);
subject.OnNext(1);
subject.OnNext(0);

subject.OnCompleted();
```

Output:

```
4
3
2
1
0
Completed

```

`TakeWhile` will return all values while the predicate passes, and when the first value fails the sequence will complete.

```csharp
var subject = new Subject<int>();
subject
    .TakeWhile(i => i < 4)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
subject.OnNext(4);
subject.OnNext(3);
subject.OnNext(2);
subject.OnNext(1);
subject.OnNext(0);

subject.OnCompleted();
```

Output:

```
1
2
3
Completed

```

### SkipLast and TakeLast			

These methods become quite self explanatory now that we understand Skip/Take and SkipWhile/TakeWhile. Both methods require a number of elements at the end of a sequence to either skip or take. The implementation of the `SkipLast` could cache all values, wait for the source sequence to complete, and then replay all the values except for the last number of elements. The Rx team however, has been a bit smarter than that. The real implementation will queue the specified number of notifications and once the queue size exceeds the value, it can be sure that it may drain a value from the queue.

```csharp
var subject = new Subject<int>();
subject
    .SkipLast(2)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
Console.WriteLine("Pushing 1");
subject.OnNext(1);
Console.WriteLine("Pushing 2");
subject.OnNext(2);
Console.WriteLine("Pushing 3");
subject.OnNext(3);
Console.WriteLine("Pushing 4");
subject.OnNext(4);
subject.OnCompleted();
```

Output:

```
Pushing 1
Pushing 2
Pushing 3
1
Pushing 4
2
Completed

```

Unlike `SkipLast`, `TakeLast` does have to wait for the source sequence to complete to be able to push its results. As per the example above, there are `Console.WriteLine` calls to indicate what the program is doing at each stage.

```csharp
var subject = new Subject<int>();
subject
    .TakeLast(2)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
Console.WriteLine("Pushing 1");
subject.OnNext(1);
Console.WriteLine("Pushing 2");
subject.OnNext(2);
Console.WriteLine("Pushing 3");
subject.OnNext(3);
Console.WriteLine("Pushing 4");
subject.OnNext(4);
Console.WriteLine("Completing");
subject.OnCompleted();
```

Output:

```
Pushing 1
Pushing 2
Pushing 3
Pushing 4
Completing
3
4
Completed

```

### SkipUntil and TakeUntil			

Our last two methods make an exciting change to the methods we have previously looked. These will be the first two methods that we have discovered together that require two observable sequences.

`SkipUntil` will skip all values until any value is produced by a secondary observable sequence.

```csharp
var subject = new Subject<int>();
var otherSubject = new Subject<Unit>();
subject
    .SkipUntil(otherSubject)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
otherSubject.OnNext(Unit.Default);
subject.OnNext(4);
subject.OnNext(5);
subject.OnNext(6);
subject.OnNext(7);
subject.OnNext(8);

subject.OnCompleted();
```

Output:

```
4
5
6
7
Completed

```

Obviously, the converse is true for `TakeWhile`. When the secondary sequence produces a value, then the `TakeWhile` operator will complete the output sequence.

```csharp
var subject = new Subject<int>();
var otherSubject = new Subject<Unit>();
subject
    .TakeUntil(otherSubject)
    .Subscribe(Console.WriteLine, () => Console.WriteLine("Completed"));
subject.OnNext(1);
subject.OnNext(2);
subject.OnNext(3);
otherSubject.OnNext(Unit.Default);
subject.OnNext(4);
subject.OnNext(5);
subject.OnNext(6);
subject.OnNext(7);
subject.OnNext(8);

subject.OnCompleted();
```

Output:

```
1
2
3
Completed
```

That was our quick run through of the filtering methods available in Rx. While they are pretty simple, as we will see, the power in Rx is down to the composability of its operators.

These operators provide a good introduction to the filtering in Rx. The filter operators are your first stop for managing the potential deluge of data we can face in the information age. We now know how to remove unmatched data, duplicate data or excess data. Next, we will move on to the other two sub classifications of the reduction operators, inspection and aggregation.