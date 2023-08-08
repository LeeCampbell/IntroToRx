---
title: Timing
---

# Time-shifted sequences				

With event sources, timing is often important. In some cases, the only information of interest about some event might the time at which it occurred. The only way in which the core `IObservable<T>` and `IObserver<T>` interfaces address time is that a source can decide when it calls an observer's `OnNext` method. A subscriber knows when an event occurred because it is occurring right now. This isn't always the most convenient way in which to work with timing, so the Rx library provides some timing-related operators. We've already seen a couple of operators that offer optional time-based operation: [`Buffer`](./08_Partitioning.md#buffer) and [`Window`])(08_Partitioning#window). This chapter looks at the various operators that are all about timing.

## Timestamp and TimeInterval		

As observable sequences are asynchronous it can be convenient to know timings for when elements are received. Obviously, a subscriber can always just use `DateTimeOffset.Now`, but if you want to refer to the arrival time as part of a larger query, the `Timestamp` extension method is a handy convenience method that attaches a timestamp to each element. It wraps elements from its source sequence in a light weight `Timestamped<T>` structure. The `Timestamped<T>` type is a struct that exposes the value of the element it wraps, and also a `DateTimeOffset` indicating when `Timestamp` operator received it.

In this example we create a sequence of three values, one second apart, and then transform it to a time stamped sequence.

```csharp
Observable.Interval(TimeSpan.FromSeconds(1))
          .Take(3)
          .Timestamp()
          .Dump("TimeStamp");
```

As you can see, `Timestamped<T>`'s implementation of `ToString()` gives us a readable output.

```
TimeStamp-->0@07/08/2023 10:03:58 +00:00
TimeStamp-->1@07/08/2023 10:03:59 +00:00
TimeStamp-->2@07/08/2023 10:04:00 +00:00
TimeStamp completed
```

We can see that the values 0, 1 & 2 were each produced one second apart.

Rx also offers `TimeInterval`. Instead of reporting the time at which items arrived, it reports the interval between items (or, in the case of the first element, how long it took for that to emerge after subscription). Similarly to the `Timestamp` method, elements are wrapped in a light weight structure. But whereas `Timestamped<T>` adorned each item with the arrival time, `TimeInterval` wraps each element with the `TimeInterval<T>` type which adds a `TimeSpan`. We can modify the previous example to use `TimeInterval`:

```csharp
Observable.Interval(TimeSpan.FromSeconds(1))
          .Take(3)
          .TimeInterval()
          .Dump("TimeInterval");
```

As you can see, the output now reports the time between elements instead of the time of day at which they were received:

```
TimeStamp-->0@00:00:01.0183771
TimeStamp-->1@00:00:00.9965679
TimeStamp-->2@00:00:00.9908958
TimeStamp completed
```

As you can see from the output, the timings are not exactly one second but are pretty close. Some of this will be measurement noise in the `TimeInterval` operator, but most of this variability is likely to arise from the `Observable.Interval` class. There will always be a limit to the precision with which a scheduler can honour the timing request of it. Some scheduler introduce more variation than others—the schedulers that deliver work via a UI thread are ultimately limited by how quickly that thread's message loop responds. But even in the most favourable condition, schedulers are limited by the fact that .NET is not built for use in real-time systems (and nor are most of the operating systems Rx can be used on). So with all of the operators in this section, you should be aware that timing is always a _best effort_ affair in Rx.

## Delay								

The `Delay` extension method time-shifts an entire sequence. `Delay` attempts to preserve the relative time intervals between the values. There is inevitably a limit to the precision with which it can do this—it won't recreate timing down to the nearest nanosecond. The exact precision is determined by the scheduler you use, and will typically get worse under heavy load, but it will typically reproduce timings to within a few milliseconds.

There are overloads of `Delay` accepting a `TimeSpan`, and an optional scheduler, which will delay the sequence by the specified amount. And there are also delays that accept a `DateTimeOffset` (and also, optionally, a scheduler) which will wait until the specified time occurs, and then start replaying the input. (This second, absolute time based approach is essentially equivalent to the `TimeSpan` overloads—you would get more or less the same effect by subtracting the current time from the target time to get a `TimeSpan`, except the `DateTimeOffset` version attempts to deal with changes in the system clock that occur between `Delay` being called, and the specified time arriving.)

To show the `Delay` method in action, this example creates a sequence of values one second apart and timestamps them. This will show that it is not the subscription that is being delayed, but the actual forwarding of the notifications to our final subscriber.

```cs
IObservable<Timestamped<long>> source = Observable
    .Interval(TimeSpan.FromSeconds(1))
    .Take(5)
    .Timestamp();

IObservable<Timestamped<long>> delay = source.Delay(TimeSpan.FromSeconds(2));

source.Subscribe(
    value => Console.WriteLine("source : {0}", value),
    () => Console.WriteLine("source Completed"));
delay.Subscribe(
    value => Console.WriteLine("delay : {0}", value),
    () => Console.WriteLine("delay Completed"));
```

If you look at the timestamps in the output, you can see that both the immediate and the delayed subscriptions subscribed to the source at the same time: both report the first timestamp as `10:15:46`. But you can also see that the `delay` subscription did not start receiving those events until 2 seconds later.

```
source : 0@07/08/2023 10:15:46 +00:00
source : 1@07/08/2023 10:15:47 +00:00
source : 2@07/08/2023 10:15:48 +00:00
delay : 0@07/08/2023 10:15:46 +00:00
source : 3@07/08/2023 10:15:49 +00:00
delay : 1@07/08/2023 10:15:47 +00:00
delay : 2@07/08/2023 10:15:48 +00:00
source : 4@07/08/2023 10:15:50 +00:00
source Completed
delay : 3@07/08/2023 10:15:49 +00:00
delay : 4@07/08/2023 10:15:50 +00:00
delay Completed
```

Note that `Delay` will not time-shift `OnError` notifications. These will be propagated immediately.

## Sample				

The `Sample` method produces items whatever interval you ask. Each time it produces a value, it reports the last value that emerged from your source. If you have a source that produces data at a higher rate than you need (e.g. suppose you have an accelerometer that reports 100 measurements per second, but you only need to take a reading 10 times a second), `Sample` provides an easy way to reduce the data rate. This example shows `Sample` in action.

```csharp
IObservable<long> interval = Observable.Interval(TimeSpan.FromMilliseconds(150));
interval.Sample(TimeSpan.FromSeconds(1)).Subscribe(Console.WriteLine);
```

Output:

```
5
12
18
```

If you looked at these numbers closely, you might have noticed that the interval between the values is not the same each time. I chose a source interval of 150ms and a sample interval of 1 second to highlight an aspect of sampling that can require careful handling: if the rate at which a source produces items doesn't line up neatly with the sampling rate, this can mean that `Sample` introduces irregularities that weren't present in the source. If we list the times at which the underlying sequence produces values, and the times at which `Sample` takes each value, we can see that with these particular timings, the sample intervals only line up with the source timings every 3 seconds.

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

Since the first sample is taken after the source emits five, and two thirds of the way into the gap after which it will produce six, there's a sense in which the "right" current value is something like 5.67, but `Sample` doesn't attempt any such interpolation. It just reports the last value to emerge from the source. A related consequence is that if the sampling interval is short enough that you're asking `Sample` to report values faster than they are emerging from the source, it will just repeat values.

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

We could apply `Throttle` to use a live search feature that makes suggestions as you type. We would typically want to wait until the user has stopped typing for a bit before searching for suggestions, because otherwise, we might end up kicking off several searches in a row, cancelling the last one each time the user presses another key. Only once there is a pause should we can execute a search with what they have typed so far. `Throttle` fits well with this scenario, because it won't allow any events through at all if the source is producing values faster than the specified rate.

Note that the RxJS library decided to make their version of throttle work differently, so if you ever find yourself using both Rx.NET and RxJS, be aware that they don't work the same way. In RxJS, throttle doesn't shut off completely when the source exceeds the specified rate: it just drops enough items that the output never exceeds the specified rate. So RxJS's throttle implementation is a kind of rate limiter, whereas Rx.NET's `Throttle` is more like a self-resetting circuit breaker that shuts off completely during an overload.

## Timeout					

The `Timeout` operator method allows us terminate a sequence with an error if the source does not produce any notifications for a given period. We can either specify the period as a sliding window with a `TimeSpan`, or as an absolute time that the sequence must complete by providing a `DateTimeOffset`.

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

Alternatively, we can pass `Timeout` an absolute time; if the sequence does not completed by that time, it produces an error.

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

There are other `Timeout` overloads enabling us to substitute an alternative sequence when a timeout occurs.

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

As we've now seen, Rx provides features to manage timing in a reactive paradigm. Data can be timed, throttled, or sampled to meet your needs. Entire sequences can be shifted in time with the delay feature, and timeliness of data can be asserted with the `Timeout` operator. These simple yet powerful features further extend the developer's tool belt for querying data in motion.