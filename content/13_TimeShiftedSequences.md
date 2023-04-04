---
title: Time-shifted sequences
---

# Time-shifted sequences				

When working with observable sequences, the time axis is an unknown quantity: when will the next notification arrive? When consuming an `IEnumerable` sequence, asynchrony is not a concern; when we call `MoveNext()`, we are blocked until the sequence yields. This chapter looks at the various methods we can apply to an observable sequence when its relationship with time is a concern.

## Buffer							

Our first subject will be the `Buffer` method. In some situations, you may not want a deluge of individual notifications to process. Instead, you might prefer to work with batches of data. It may be the case that processing one item at a time is just too expensive, and the trade-off is to deal with messages in batches, at the cost of accepting a delay.

The `Buffer` operator allows you to store away a range of values and then re-publish them as a list once the buffer is full. You can temporarily withhold a specified number of elements, stash away all the values for a given time span, or use a combination of both count and time. `Buffer` also offers more advanced overloads that we will look at in a future chapter.

```csharp
public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    int count)
{...}

public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    TimeSpan timeSpan)
{...}

public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    TimeSpan timeSpan, 
    int count)
{...}
```

The two overloads of `Buffer` are straight forward and should make it simple for other developers to understand the intent of the code.

```csharp
IObservable<IList<T>> bufferedSequence;
bufferedSequence = mySequence.Buffer(4);

// or
bufferedSequence = mySequence.Buffer(TimeSpan.FromSeconds(1))
```

For some use cases, it may not be enough to specify only a buffer size and a maximum delay period. Some systems may have a sweet spot for the size of a batch they can process, but also have a time constraint to ensure that data is not stale. In this case buffering by both time and count would be suitable.

In this example below, we create a sequence that produces the first ten values one second apart, then a further hundred values within another second. We buffer by a maximum period of three seconds and a maximum batch size of fifteen values.

```csharp
var idealBatchSize = 15;
var maxTimeDelay = TimeSpan.FromSeconds(3);
var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10)
    .Concat(Observable.Interval(TimeSpan.FromSeconds(0.01)).Take(100));

source.Buffer(maxTimeDelay, idealBatchSize)
    .Subscribe(
        buffer => Console.WriteLine("Buffer of {1} @ {0}", DateTime.Now, buffer.Count), 
        () => Console.WriteLine("Completed"));
```

Output:

```
Buffer of 3 @ 01/01/2012 12:00:03
Buffer of 3 @ 01/01/2012 12:00:06
Buffer of 3 @ 01/01/2012 12:00:09
Buffer of 15 @ 01/01/2012 12:00:10
Buffer of 15 @ 01/01/2012 12:00:10
Buffer of 15 @ 01/01/2012 12:00:10
Buffer of 15 @ 01/01/2012 12:00:11
Buffer of 15 @ 01/01/2012 12:00:11
Buffer of 15 @ 01/01/2012 12:00:11
Buffer of 11 @ 01/01/2012 12:00:11
```

Note the variations in time and buffer size. We never get a buffer containing more than fifteen elements, and we never wait more than three seconds. A practical application of this is when you are loading data from an external source into an `ObservableCollection<T>` in a WPF application. It may be the case that adding one item at a time is just an unnecessary load on the dispatcher (especially if you are expecting over a hundred items). You may have also measured, for example that processing a batch of fifty items takes 100ms. You decide that this is the maximum amount of time you want to block the dispatcher, to keep the application responsive. This could give us two reasonable values to use: `source.Buffer(TimeSpan.FromMilliseconds(100), 50)`. This means the longest we will block the UI is about 100ms to process a batch of 50 values, and we will never have values waiting for longer than 100ms before they are processed.

### Overlapping buffers				

`Buffer` also offers overloads to manipulate the overlapping of the buffers. The variants we have looked at so far do not overlap and have no gaps between buffers, i.e. all values from the source are propagated through.

```csharp
public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    int count, 
    int skip)
{...}

public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    TimeSpan timeSpan, 
    TimeSpan timeShift)
{...}
```

There are three interesting things you can do with overlapping buffers:

- **Overlapping behavior**: Ensure that current buffer includes some or all values from previous buffer
- **Standard behavior**: Ensure that each new buffer only has new data
- **Skip behavior**: Ensure that each new buffer not only contains new data exclusively, but also ignores one or more values since the previous buffer
</dl>

#### Overlapping buffers by count	

If you are specifying a buffer size as a count, then you need to use this overload.

```csharp
public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    int count, 
    int skip)
{...}
```

You can apply the above scenarios as follows:

- **Overlapping behavior**: `skip` < `count` *
- **Standard behavior**: `skip` = `count`
- **Skip behavior**: `skip` > `count`

> *The `skip` parameter cannot be less than or equal to zero. If you want to use a value of zero (i.e. each buffer contains all values), then consider using the <a href="07_Aggregation.html#Scan">`Scan`</a> method instead with an `IList<T>` as the accumulator.

Let's see each of these in action. In this example, we have a source that produces values every second. We apply each of the variations of the buffer overload.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
source.Buffer(3, 1)
    .Subscribe(
        buffer =>
        {
            Console.WriteLine("--Buffered values");
            foreach (var value in buffer)
            {
                Console.WriteLine(value);
            }
        }, () => Console.WriteLine("Completed"));
```

Output

```
--Buffered values
0
1
2
--Buffered values
1
2
3
--Buffered values
2
3
4
--Buffered values
3
4
5
etc....
```

Note that in each buffer, one value is skipped from the previous batch. If we change the `skip` parameter from 1 to 3 (same as the buffer size), we see standard buffer behavior.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
source.Buffer(3, 3)
    ...
```

Output

```
--Buffered values
0
1
2
--Buffered values
3
4
5
--Buffered values
6
7
8
--Buffered values
9
Completed
```

Finally, if we change the `skip` parameter to 5 (a value greater than the count of 3), we can see that two values are lost between each buffer.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
source.Buffer(3, 5)
    ...
```

Output

```
--Buffered values
0
1
2
--Buffered values
5
6
7
Completed
```

#### Overlapping buffers by time			

You can, of course, apply the same three behaviors with buffers defined by time instead of count.

```csharp
public static IObservable<IList<TSource>> Buffer<TSource>(
    this IObservable<TSource> source, 
    TimeSpan timeSpan, 
    TimeSpan timeShift)
{...}
```

To exactly replicate the output from our [Overlapping Buffers By Count[(#OverlappingBuffersByCount) examples, we only need to provide the following arguments:

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
var overlapped = source.Buffer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
var standard = source.Buffer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
var skipped = source.Buffer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5));
```

As our source produces values consistently every second, we can use the same values from our count example but as seconds.

## Delay								

The `Delay` extension method is a purely a way to time-shift an entire sequence. You can provide either a relative time the sequence should be delayed by using a `TimeSpan`, or an absolute point in time that the sequence should wait for using a `DateTimeOffset`. The relative time intervals between the values are preserved.

```csharp
// Time-shifts the observable sequence by a relative time.
public static IObservable<TSource> Delay<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime)
{...}

// Time-shifts the observable sequence by a relative time.
public static IObservable<TSource> Delay<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IScheduler scheduler)
{...}

// Time-shifts the observable sequence by an absolute time.
public static IObservable<TSource> Delay<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime)
{...}

// Time-shifts the observable sequence by an absolute time.
public static IObservable<TSource> Delay<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IScheduler scheduler)
{...}
```

To show the `Delay` method in action, we create a sequence of values one second apart and timestamp them. This will show that it is not the subscription that is being delayed, but the actual forwarding of the notifications to our final subscriber.

```csharp
var source = Observable.Interval(TimeSpan.FromSeconds(1))
    .Take(5)
    .Timestamp();

var delay = source.Delay(TimeSpan.FromSeconds(2));

source.Subscribe(
    value => Console.WriteLine("source : {0}", value),
    () => Console.WriteLine("source Completed"));
delay.Subscribe(
    value => Console.WriteLine("delay : {0}", value),
    () => Console.WriteLine("delay Completed"));
```

Output:

```
source : 0@01/01/2012 12:00:00 pm +00:00
source : 1@01/01/2012 12:00:01 pm +00:00
source : 2@01/01/2012 12:00:02 pm +00:00
delay : 0@01/01/2012 12:00:00 pm +00:00
source : 3@01/01/2012 12:00:03 pm +00:00
delay : 1@01/01/2012 12:00:01 pm +00:00
source : 4@01/01/2012 12:00:04 pm +00:00
source Completed
delay : 2@01/01/2012 12:00:02 pm +00:00
delay : 3@01/01/2012 12:00:03 pm +00:00
delay : 4@01/01/2012 12:00:04 pm +00:00
delay Completed
```

It is worth noting that `Delay` will not time-shift `OnError` notifications. These will be propagated immediately.

## Sample				

The `Sample` method simply takes the last value for every specified `TimeSpan`. This is great for getting timely data from a sequence that produces too much information for your requirements. This example shows sample in action.

```csharp
var interval = Observable.Interval(TimeSpan.FromMilliseconds(150));
interval.Sample(TimeSpan.FromSeconds(1)).Subscribe(Console.WriteLine);
```

Output:

```
5
12
18
```

This output is interesting and this is the reason why I choose the value of 150ms. If we plot the underlying sequence of values against the time they are produced, we can see that `Sample` is taking the last value it received for each period of one second.

| Relative time (ms) | Source value | Sampled value |
| :----------------- | :----------- | :------------ |
| 0                  |              |               |
| 50                 |              |               |
| 100                |              |               |
| 150                | 0            |               |
| 200                |              |               |
| 250                |              |               |
| 300                | 1            |               |
| 350                |              |               |
| 400                |              |               |
| 450                | 2            |               |
| 500                |              |               |
| 550                |              |               |
| 600                | 3            |               |
| 650                |              |               |
| 700                |              |               |
| 750                | 4            |               |
| 800                |              |               |
| 850                |              |               |
| 900                | 5            |               |
| 950                |              |               |
| 1000               |              | 5             |
| 1050               | 6            |               |
| 1100               |              |               |
| 1150               |              |               |
| 1200               | 7            |               |
| 1250               |              |               |
| 1300               |              |               |
| 1350               | 8            |               |
| 1400               |              |               |
| 1450               |              |               |
| 1500               | 9            |               |
| 1550               |              |               |
| 1600               |              |               |
| 1650               | 10           |               |
| 1700               |              |               |
| 1750               |              |               |
| 1800               | 11           |               |
| 1850               |              |               |
| 1900               |              |               |
| 1950               | 12           |               |
| 2000               |              | 12            |
| 2050               |              |               |
| 2100               | 13           |               |
| 2150               |              |               |
| 2200               |              |               |
| 2250               | 14           |               |
| 2300               |              |               |
| 2350               |              |               |
| 2400               | 15           |               |
| 2450               |              |               |
| 2500               |              |               |
| 2550               | 16           |               |
| 2600               |              |               |
| 2650               |              |               |
| 2700               | 17           |               |
| 2750               |              |               |
| 2800               |              |               |
| 2850               | 18           |               |
| 2900               |              |               |
| 2950               |              |               |
| 3000               | 19           | 19            |

## Throttle							

The `Throttle` extension method provides a sort of protection against sequences that produce values at variable rates and sometimes too quickly. Like the `Sample` method, `Throttle` will return the last sampled value for a period of time. Unlike `Sample` though, `Throttle`'s period is a sliding window. Each time `Throttle` receives a value, the window is reset. Only once the period of time has elapsed will the last value be propagated. This means that the `Throttle` method is only useful for sequences that produce values at a variable rate. Sequences that produce values at a constant rate (like `Interval` or `Timer`) either would have all of their values suppressed if they produced values faster than the throttle period, or all of their values would be propagated if they produced values slower than the throttle period.

```csharp
// Ignores values from an observable sequence which are followed by another value before
//  dueTime.
public static IObservable<TSource> Throttle<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime)
{...}
public static IObservable<TSource> Throttle<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IScheduler scheduler)
{...}
```

A great application of the `Throttle` method would be to use it with a live search like "Google Suggest". While the user is still typing we can hold off on the search. Once there is a pause for a given period, we can execute the search with what they have typed. The Rx team has a great example of this scenario in the [Rx Hands On Lab](http://download.microsoft.com/download/C/5/D/C5D669F9-01DF-4FAF-BBA9-29C096C462DB/Rx%20HOL%20.NET.pdf "Rx Hands On Lab as PDF - Mcrosoft.com").

## Timeout					

We have considered handling timeout exceptions previously in the chapter on [Flow control](11_AdvancedErrorHandling.html#CatchSwallowingException). The `Timeout` extension method allows us terminate the sequence with an error if we do not receive any notifications for a given period. We can either specify the period as a sliding window with a `TimeSpan`, or as an absolute time that the sequence must complete by providing a `DateTimeOffset`.

```csharp
// Returns either the observable sequence or a TimeoutException if the maximum duration
//  between values elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime)
{...}
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IScheduler scheduler)
{...}

// Returns either the observable sequence or a TimeoutException if dueTime elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime)
{...}
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IScheduler scheduler)
{...}
```

If we provide a `TimeSpan` and no values are produced within that time span, then the sequence fails with a `TimeoutException`.

```csharp
var source = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(10)
    .Concat(Observable.Interval(TimeSpan.FromSeconds(2)));

var timeout = source.Timeout(TimeSpan.FromSeconds(1));
timeout.Subscribe(
    Console.WriteLine, 
    Console.WriteLine, 
    () => Console.WriteLine("Completed"));
```

Output:

```
0
1
2
3
4
System.TimeoutException: The operation has timed out.
```

Like the `Throttle` method, this overload is only useful for sequences that produce values at a variable rate.

The alternative use of `Timeout` is to set an absolute time; the sequence must be completed by then.

```csharp
var dueDate = DateTimeOffset.UtcNow.AddSeconds(4);
var source = Observable.Interval(TimeSpan.FromSeconds(1));
var timeout = source.Timeout(dueDate);
timeout.Subscribe(
    Console.WriteLine, 
    Console.WriteLine, 
    () => Console.WriteLine("Completed"));
```

Output:

```
0
1
2
System.TimeoutException: The operation has timed out.
```

Perhaps an even more interesting usage of the `Timeout` method is to substitute in an alternative sequence when a timeout occurs. 
The `Timeout` method has overloads the provide the option of specifying a continuation sequence to use if a timeout occurs. 
This functionality behaves much like the [Catch](11_AdvancedErrorHandling.html#Catch) operator. 
It is easy to imagine that the simple overloads actually just call through to these over loads and specify an `Observable.Throw<TimeoutException>` as the continuation sequence.

```csharp
// Returns the source observable sequence or the other observable sequence if the maximum 
// duration between values elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IObservable<TSource> other)
{...}

public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    TimeSpan dueTime, 
    IObservable<TSource> other, 
    IScheduler scheduler)
{...}

// Returns the source observable sequence or the other observable sequence if dueTime 
// elapses.
public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IObservable<TSource> other)
{...}  

public static IObservable<TSource> Timeout<TSource>(
    this IObservable<TSource> source, 
    DateTimeOffset dueTime, 
    IObservable<TSource> other, 
    IScheduler scheduler)
{...}
```

<!--
    TODO: Observable.GroupByUntil
    TODO: Observable.Merge reprise(has options to take schedulers too)
-->

Rx provides features to tame the unpredictable element of time in a reactive paradigm. Data can be buffered, throttled, sampled or delayed to meet your needs. Entire sequences can be shifted in time with the delay feature, and timeliness of data can be asserted with the `Timeout` operator. These simple yet powerful features further extend the developer's tool belt for querying data in motion.