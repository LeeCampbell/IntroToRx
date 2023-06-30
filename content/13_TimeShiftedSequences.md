---
title: Time-shifted sequences
---

# Time-shifted sequences				

When working with observable sequences, the time axis is an unknown quantity: when will the next notification arrive? When consuming an `IEnumerable` sequence, asynchrony is not a concern; when we call `MoveNext()`, we are blocked until the sequence yields. We've already seen a couple of operators that offer optional time-based operation: [`Buffer`](./08_Partitioning.md#buffer) and [`Window`])(08_Partitioning#window). This chapter looks at the various operators that are all about timing.


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


TODO: this is about timing but not timeshifting. Decide whether to rename this chapter, or split it, so that we can accommodate these (which were in the Transformation chapter):

## Timestamp and TimeInterval		

As observable sequences are asynchronous it can be convenient to know timings for when elements are received. The `Timestamp` extension method is a handy convenience method that wraps elements of a sequence in a light weight `Timestamped<T>` structure. The `Timestamped<T>` type is a struct that exposes the value of the element it wraps, and the timestamp it was created with as a `DateTimeOffset`.

In this example we create a sequence of three values, one second apart, and then transform it to a time stamped sequence. The handy implementation of `ToString()` on `Timestamped<T>` gives us a readable output.

```csharp
Observable.Interval(TimeSpan.FromSeconds(1))
          .Take(3)
          .Timestamp()
          .Dump("TimeStamp");
```

Output

```
TimeStamp --> 0@01/01/2012 12:00:01 a.m. +00:00
TimeStamp --> 1@01/01/2012 12:00:02 a.m. +00:00
TimeStamp --> 2@01/01/2012 12:00:03 a.m. +00:00
TimeStamp completed
```

We can see that the values 0, 1 &amp; 2 were each produced one second apart. An alternative to getting an absolute timestamp is to just get the interval since the last element. The `TimeInterval` extension method provides this. As per the `Timestamp` method, elements are wrapped in a light weight structure. This time the structure is the `TimeInterval<T>` type.

```csharp
Observable.Interval(TimeSpan.FromSeconds(1))
          .Take(3)
          .TimeInterval()
          .Dump("TimeInterval");
```

Output:

```
TimeInterval --> 0@00:00:01.0180000
TimeInterval --> 1@00:00:01.0010000
TimeInterval --> 2@00:00:00.9980000
TimeInterval completed
```

As you can see from the output, the timings are not exactly one second but are pretty close.
