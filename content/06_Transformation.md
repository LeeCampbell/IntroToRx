---
title: Transformation of sequences
---

# Transformation of sequences			

The values from the sequences we consume are not always in the format we need. Sometimes there is too much noise in the data so we strip the values down. Sometimes each value needs to be expanded either into a richer object or into more values. By composing operators, Rx allows you to control the quality as well as the quantity of values in the observable sequences you consume.

Up until now, we have looked at creation of sequences, transition into sequences, and, the reduction of sequences by filtering. In this chapter we will look at _transforming_ sequences.

TODO: should we move this one chapter earlier? Would it be handy to have this in Filtering?

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



## Select							

The classic transformation method is `Select`. It allows you provide a function that takes a value of `TSource` and return a value of `TResult`. The signature for `Select` is nice and simple and suggests that its most common usage is to transform from one type to another type, i.e. `IObservable<TSource>` to `IObservable<TResult>`.

```csharp
IObservable<TResult> Select<TSource, TResult>(
    this IObservable<TSource> source, 
    Func<TSource, TResult> selector)
```

Note that there is no restriction that prevents `TSource` and `TResult` being the same thing. So for our first example, we will take a sequence of integers and transform each value by adding 3, resulting in another sequence of integers.

```csharp
var source = Observable.Range(0, 5);
source.Select(i=>i+3)
      .Dump("+3")
```

Output:

```
+3 --> 3
+3 --> 4
+3 --> 5
+3 --> 6
+3 --> 7
+3 completed
```

While this can be useful, more common use is to transform values from one type to another. In this example we transform integer values to characters.

```csharp
Observable.Range(1, 5);
          .Select(i =>(char)(i + 64))
          .Dump("char");
```

Output:

```
char --> A
char --> B
char --> C
char --> D
char --> E
char completed
```

If we really want to take advantage of LINQ we could transform our sequence of integers	to a sequence of anonymous types.

```csharp
Observable.Range(1, 5)
          .Select(i => new { Number = i, Character = (char)(i + 64) })
          .Dump("anon");
```

Output:

```
anon --> { Number = 1, Character = A }
anon --> { Number = 2, Character = B }
anon --> { Number = 3, Character = C }
anon --> { Number = 4, Character = D }
anon --> { Number = 5, Character = E }
anon completed
```

To further leverage LINQ we could write the above query using [query comprehension syntax](http://www.albahari.com/nutshell/linqsyntax.aspx).

```csharp
var query = from i in Observable.Range(1, 5)
            select new {Number = i, Character = (char) (i + 64)};

query.Dump("anon");
```

In Rx, `Select` has another overload. The second overload provides two values to the `selector` function. The additional argument is the element's index in the sequence. Use this method if the index of the element in the sequence is important to your selector function.

## SelectMany						

Whereas `Select` produces one output for each input, `SelectMany` enables each input element to be transformed into any number of outputs. To see how this can work, let's first look at an example that uses just `Select`:

```cs
Observable
    .Range(1, 5)
    .Select(i => new string((char)(i+64), i))
    .Dump("strings");
```

which produces this output:

```
strings-->A
strings-->BB
strings-->CCC
strings-->DDDD
strings-->EEEEE
strings completed
```

As you can see, for each of the numbers produced by `Range`, our output contains a string whose length is that many characters. What if, instead of transforming each number into a string, we transformed it into an `IObservable<char>`. We can do that just by adding `.ToObservable()` after constructing the string:

```cs
Observable
    .Range(1, 5)
    .Select(i => new string((char)(i+64), i).ToObservable())
    .Dump("sequences");
```

(Alternatively, we could have replaced the selection expression with `i => Observable.Repeat((char)(i+64), i)`. Either has exactly the same effect.) The output isn't terribly useful:

```
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings-->System.Reactive.Linq.ObservableImpl.ToObservableRecursive`1[System.Char]
strings completed
```

We have an observable sequence of observable sequences. But look at what happens if we now replace that `Select` with a `SelectMany`:

```cs
Observable
    .Range(1, 5)
    .SelectMany(i => new string((char)(i+64), i).ToObservable())
    .Dump("chars");
```

This gives us an `IObservable<char>`, with this output:

```
chars-->A
chars-->B
chars-->B
chars-->C
chars-->C
chars-->D
chars-->C
chars-->D
chars-->E
chars-->D
chars-->E
chars-->D
chars-->E
chars-->E
chars-->E
chars completed
```

The order has become a little scrambled, but if you look carefully you'll see that the number of occurrences of each letter is the same as when we were emitting strings. There is just one `A`, for example, but `C` appears three times, and `E` five times.

`SelectMany` expects the transformation function to return an `IObservable<T>` for each input, and it then combines the result of those back into a single result. The LINQ to Objects equivalent is a little less chaotic. If you were to run this:

```cs
Enumerable
    .Range(1, 5)
    .SelectMany(i => new string((char)(i+64), i))
    .ToList()
```

it would produce a list with these elements:

```
[ A, B, B, C, C, C, D, D, D, D, E, E, E, E, E ]
```

The order is less odd. It's worth exploring the reasons for this in a little more detail.


### IEnumerable<T> vs. IObservable<T> SelectMany	

`IEnumerable<T>` is pull based—sequences produce elements only when asked. `Enumerable.SelectMany` pulls items from its sources in a very particular order. It begins by asking its source `IEnumerable<int>` (the one returned by `Range` in the preceding example), and then retrieves the first value. `SelectMany` then invokes our callback, passing this first item, and then enumerates everything in the `IEnumerable<char>` our callback returns. Only when it has exhausted this does it ask the source (`Range`) for a second item. Again, it passes that second item to our callback and then fully enumerates the `IEnumerable<char>`, we return, and so on. So we get everything from the first nested sequence first, then everything from the second, etc.

`Enumerable.SelectMany` is able to proceed in this way for two reasons. First, the pull-based nature of `IEnumerable<T>` enables it to decide on the order in which it processes things. Second, with `IEnumerable<T>` it is normal for operations to block, i.e., not to return until they have something for us. When the preceding example calls `ToList`, it won't return until it has fully populated a `List<T>` with all of the results.

Rx is not like that. First, consumers don't get to tell sources when to produce each item—sources emit items when they are ready to. Second, Rx typically models ongoing processes, so we don't expect method calls to block until they are done. There are some cases where Rx sequences will naturally produce all of their items very quickly and complete as soon as they can, but kinds of information sources that we tend to want model with Rx typically don't behave that way. So most operations in Rx do not block—they immediately return something (such as an `IObservable<T>`, or an `IDisposable` representing a subscription) and will then produce values later.

The Rx version of the example we're currently examining is in fact one of these unusual cases where each of the sequences emits items as soon as it can. Logically speaking, all of the nested `IObservable<char>` sequences are in progress concurrently. The result is a mess because each of the observable sources here attempts to produce every element as quickly as the source can consume them. The fact that they end up being interleaved has to do with the way these kinds of observable sources use Rx's _scheduler_ system, which we will describe in chapter XXX. Schedulers ensure that even when we are modelling logically concurrent processes, the rules of Rx are maintained, and observers of the output of `SelectMany` will only be given one item at a time. The following marble diagram shows the events that lead to the scrambled output we see:

![An Rx marble diagram illustrating two observables. The first is labelled 'source', and it shows six events, labelled numerically. These fall into three groups: events 1 and 2 occur close together, and are followed by a gap. Then events 3, 4, and 5 are close together. And then after another gap event 6 occurs, not close to any. The second observable is labelled 'source.Quiescent(TimeSpan.FromSeconds(2), Scheduler.Default). It shows three events. The first is labelled '[1, 2], and its horizontal position shows that it occurs a little bit after the '2' event on the 'source' observable. The second event on the second observable is labelled '[3,4,5]' and occurs a bit after the '5' event on the 'source' observable. The third event from on the second observable is labelled '[6]', and occurs a bit after the '6' event on the 'source' observable. The image conveys the idea that each time the source produces some events and then stops, the second observable will produce an event shortly after the source stops, which will contain a list with all of the events from the source's most recent burst of activity.](GraphicsIntro/Ch06-Transformation-MarblesSelect-Many-Marbles.svg)


We can make a small tweak to prevent the child sequences all from trying to run at the same time:

```cs
Observable
    .Range(1, 5)
    .SelectMany(i => Observable.Repeat((char)(i+64), i).Delay(TimeSpan.FromMilliseconds(i * 100)))
    .Dump("chars");
```

Now we get output consistent with the `IEnumerable<T>` version:

```
chars-->A
chars-->B
chars-->B
chars-->C
chars-->C
chars-->C
chars-->D
chars-->D
chars-->D
chars-->D
chars-->E
chars-->E
chars-->E
chars-->E
chars-->E
chars completed
```

This clarifies that `SelectMany` lets you produce a sequence for each item that the source produces, and to have all of the items from all of those new sequences flattened back out into one sequence that contains everything.

However, we probably won't want production code to introduce delays just to make it easier to see what's going. Rx's ability to model concurrent process is one of the big reasons for using it. So we need to be able to think about this style of concurrency, so it can be helpful to visualize this kind of asynchronous operation.

### Visualizing sequences			

Let's divert quickly and talk about a technique we will use to help communicate the concepts relating to sequences. Marble diagrams are a way of visualizing sequences. Marble diagrams are great for sharing Rx concepts and describing composition of sequences. When using marble diagrams there are only a few things you need to know

1. a sequence is represented by a horizontal line 
2. time moves to the right (i.e. things on the left happened before things on the right)
3. notifications are represented by symbols:
 *. '0' for OnNext 
 *. 'X' for an OnError 
 *. '|' for OnCompleted 
1. many concurrent sequences can be visualized by creating rows of sequences

This is a sample of a sequence of three values that completes:

<div class="marble">
    <pre class="line">--0--0--0-|</pre>
</div>

This is a sample of a sequence of four values then an error:

<div class="marble">
    <pre class="line">--0--0--0--0--X</pre>
</div>

Now going back to our `SelectMany` example, we can visualize our input sequence by using values in instead of the 0 marker. This is the marble diagram representation of the sequence [1,2,3] spaced three seconds apart (note each character represents one second).

<div class="marble">
    <pre class="line">--1--2--3|</pre>
</div>

Now we can leverage the power of marble diagrams by introducing the concept of time and space. Here we see the visualization of the sequence produced by the first value 1 which gives us the sequence [10,11,12]. These values were spaced four seconds apart, but the initial value is produce immediately.

<div class="marble">
    <pre class="line">1---1---1|</pre>
    <pre class="line">0   1   2|</pre>
</div>

As the values are double digit they cover two rows, so the value of 10 is not confused with the value 1 immediately followed by the value 0. We add a row for each sequence produced by the `selector` function.

<div class="marble">
    <pre class="line">--1--2--3|</pre>
    <pre class="line"> </pre>
    <pre class="line" style="color: blue">  1---1---1|</pre>
    <pre class="line" style="color: blue">  0   1   2|</pre>
    <pre class="line"> </pre>
    <pre class="line" style="color: red">     2---2---2|</pre>
    <pre class="line" style="color: red">     0   1   2|</pre>
    <pre class="line"></pre>
    <pre class="line" style="color: green">        3---3---3|</pre>
    <pre class="line" style="color: green">        0   1   2|</pre>
</div>

Now that we can visualize the source sequence and its child sequences, we should be able to deduce the expected output of the `SelectMany` operator. To create a result row for our marble diagram, we simple allow the values from each child sequence to 'fall' into the new result row.

<div class="marble">
    <pre class="line">--1--2--3|</pre>
    <pre class="line"> </pre>
    <pre class="line" style="color: blue">  1---1---1|</pre>
    <pre class="line" style="color: blue">  0   1   2|</pre>
    <pre class="line"> </pre>
    <pre class="line" style="color: red">     2---2---2|</pre>
    <pre class="line" style="color: red">     0   1   2|</pre>
    <pre class="line"></pre>
    <pre class="line" style="color: green">        3---3---3|</pre>
    <pre class="line" style="color: green">        0   1   2|</pre>
    <pre class="line"></pre>
    <pre class="line">--<span style="color: blue">1</span>--<span style="color: red">2</span><span
        style="color: blue">1</span>-<span style="color: green">3</span><span style="color: red">2</span><span
            style="color: blue">1</span>-<span style="color: green">3</span><span style="color: red">2</span>--<span
                style="color: green">3</span>|</pre>
    <pre class="line">&nbsp;&nbsp;<span style="color: blue">0</span>&nbsp;&nbsp;<span
        style="color: red">0</span><span style="color: blue">1</span>&nbsp;<span style="color: green">0</span><span
            style="color: red">1</span><span style="color: blue">2</span>&nbsp;<span style="color: green">1</span><span
                style="color: red">2</span>&nbsp;&nbsp;<span style="color: green">2</span>|</pre>
    <pre class="line"></pre>
</div>

If we take this exercise and now apply it to code, we can validate our marble diagram. First our method that will produce our child sequences:

```csharp
private IObservable<long> GetSubValues(long offset)
{
    //Produce values [x*10, (x*10)+1, (x*10)+2] 4 seconds apart, but starting immediately.
    return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(4))
                     .Select(x => (offset*10) + x)
                     .Take(3);
}
```

This is the code that takes the source sequence to produce our final output:

```csharp
// Values [1,2,3] 3 seconds apart.
Observable.Interval(TimeSpan.FromSeconds(3))
          .Select(i => i + 1) //Values start at 0, so add 1.
          .Take(3)            //We only want 3 values
          .SelectMany(GetSubValues) //project into child sequences
          .Dump("SelectMany");
```

The output produced matches our expectations from the marble diagram.

```
SelectMany --> 10
SelectMany --> 20
SelectMany --> 11
SelectMany --> 30
SelectMany --> 21
SelectMany --> 12
SelectMany --> 31
SelectMany --> 22
SelectMany --> 32
SelectMany completed
```

We have previously looked at the `Select` operator when it is used in Query Comprehension Syntax, so it is worth noting how you use the `SelectMany` operator. The `Select` extension method maps quite obviously to query comprehension syntax, `SelectMany` is not so obvious. As we saw in the earlier example, the simple implementation of just suing select is as follows:

```csharp
var query = from i in Observable.Range(1, 5)
            select i;
```

If we wanted to add a simple `where` clause we can do so like this:

```csharp
var query = from i in Observable.Range(1, 5)
            where i%2==0
            select i;
```

To add a `SelectMany` to the query, we actually add an extra `from` clause.

```csharp
var query = from i in Observable.Range(1, 5)
            where i%2==0
            from j in GetSubValues(i)
            select j;

// Equivalent to 
var query = Observable.Range(1, 5)
                      .Where(i=>i%2==0)
                      .SelectMany(GetSubValues);
```

An advantage of using the query comprehension syntax is that you can easily access other variables in the scope of the query. In this example we select into an anon type both the value from the source and the child value.

```csharp
var query = from i in Observable.Range(1, 5)
            where i%2==0
            from j in GetSubValues(i)
            select new {i, j};

query.Dump("SelectMany");
```

Output

```
SelectMany --> { i = 2, j = 20 }
SelectMany --> { i = 4, j = 40 }
SelectMany --> { i = 2, j = 21 }
SelectMany --> { i = 4, j = 41 }
SelectMany --> { i = 2, j = 22 }
SelectMany --> { i = 4, j = 42 }
SelectMany completed
```

This brings us to a close on Part 2. The key takeaways from this were to allow you the reader to understand a key principal to Rx: functional composition. As we move through Part 2, examples became progressively more complex. We were leveraging the power of LINQ to chain extension methods together to compose complex queries.

We didn't try to tackle all of the operators at once, we approached them in groups.

- Creation
- Reduction
- Inspection
- Aggregation
- Transformation

On deeper analysis of the operators we find that most of the operators are actually	specialization of the higher order functional concepts. We named them the ABC's of functional programming:

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


### The Significant of SelectMany

If you've been reading this book's chapters in order, you had already seen two examples of `SelectMany` in earlier chapters. The first example in the [**LINQ Operators and Composition** section of Chapter 2](./02_KeyTypes.md#linq-operators-and-composition) used it. Here's the relevant code:

```cs
IObservable<int> onoffs =
    from _ in src
    from delta in Observable.Return(1, scheduler).Concat(Observable.Return(-1, scheduler).Delay(minimumInactivityPeriod, scheduler))
    select delta;
```

(If you're wondering where the call to `SelectMany` is in that, remember that if a Query Expression contains two `from` clauses, the C# compiler turns those into a call to `SelectMany`.) This illustrates a common pattern in Rx, which might be described as fanning out, and then back in again.

As you may recall, this example worked by creating a new, short-lived `IObservable<int>` for each item produced by `src`. (These child sequences, represented by the `delta` range variable in the example, produce the value `1`, and then after the specified `minimumActivityPeriod`, they produce `-1`. This enabled us to keep count of the number of recent events emitted.) This is the _fanning out_ part, where items in a source sequence produce new observable sequences. `SelectMany` is crucial in these scenarios because it enables all of those new sequences to be flattened back out into a single output sequence.

The second place I used `SelectMany` was slightly different: it was the final example of the [**Representing Filesystem Events in Rx** section in Chapter 3](./03_CreatingObservableSequences.md#representing-filesystem-events-in-rx). Although that example also combined multiple observable sources into a single observable, that list of observables was fixed: there was one for each of the different events from `FileSystemWatcher`. It used a different operator `Merge` (which we'll get to in XXX), which was simpler to use in that scenario because you just pass it the list of all the observables you'd like to combine. However, because of a few other things this code wanted to do (including deferred startup, automated disposal, and sharing a single source when multiple subscribers were active), the particular combination of operators used to achieve this meant our merging code that returned an `IObservable<FileSystemEventArgs>`, needed to be invoked as a transforming step. If we'd just used `Select`, the result would have been an `IObservable<IObservable<FileSystemEventArgs>>`. The structure of the code meant that it would only ever produce a single `IObservable<FileSystemEventArgs>`, so the double-wrapped type would be rather inconvenient. `SelectMany` is very useful in these scenarios—if composition of operators has introduced an extra layer of observables-in-observables that you don't want, `SelectMany` can unwrap one layer for you.

These two cases—fanning out then back in, and removing or avoiding a layer of observables of observables—come up quite often, which makes `SelectMany` an important method. (It's not surprising that I was unable to avoid using it in earlier examples.)

As it happens, `SelectMany` is also a particularly important operator in the mathematical theory that Rx is based on. It is a fundamental operator, in the sense that it is possible to build many other Rx operators with it. [Section XXX in Appendix C](./C_AlgebraicUnderpinnings) shows how you can implement `Select` and `Where` using `SelectMany`.




## Cast

If you were to get a sequence of objects i.e. `IObservable<object>`, you may find it less than useful. There is a method specifically for `IObservable<object>` that will cast each element to a given type, and logically it is called `Cast<T>()`.

```csharp
var objects = new Subject<object>();
objects.Cast<int>().Dump("cast");
objects.OnNext(1);
objects.OnNext(2);
objects.OnNext(3);
objects.OnCompleted();
```

Output:

```
cast --> 1
cast --> 2
cast --> 3
cast completed
```

If however we were to add a value that could not be cast into the sequence then we get errors.

```csharp
var objects = new Subject<object>();
objects.Cast<int>().Dump("cast");
objects.OnNext(1);
objects.OnNext(2);
objects.OnNext("3");//Fail
```

Output:

```
cast --> 1
cast --> 2
cast failed --> Specified cast is not valid.
```

That is the difference between `Cast` and the [`OfType` operator shown in Chapter 5](./05_Filtering.md#oftype). `OfType` is a filtering operator, and it removes any items that are not of the specified type. `Cast` doesn't remove anything—it is more like `Select` in that it applies a transformation (specifically a cast) to every input. If the cast fails, we get an error.

This distinction might be easier to see if we recreate the functionality of `Cast` and `OfType` using other more fundamental operators.

```csharp
// source.Cast<int>(); is equivalent to
source.Select(i=>(int)i);

// source.OfType<int>();
source.Where(i=>i is int).Select(i=>(int)i);
```



## Materialize and Dematerialize			

The `Timestamp` and `TimeInterval` transform operators can prove useful for logging and debugging sequences, so too can the `Materialize` operator. `Materialize` transitions a sequence into a metadata representation of the sequence, taking an `IObservable<T>` to an `IObservable<Notification<T>>`. The `Notification` type provides meta data for the events of the sequence.

If we materialize a sequence, we can see the wrapped values being returned.

```csharp
Observable.Range(1, 3)
          .Materialize()
          .Dump("Materialize");
```

Output:

```
Materialize --> OnNext(1)
Materialize --> OnNext(2)
Materialize --> OnNext(3)
Materialize --> OnCompleted()
Materialize completed
```

Note that when the source sequence completes, the materialized sequence produces an 'OnCompleted' notification value and then completes. `Notification<T>` is an abstract class with three implementations:

 * OnNextNotification
 * OnErrorNotification
 * OnCompletedNotification

`Notification<T>` exposes four public properties to help you discover it: `Kind`, `HasValue`, `Value` and `Exception`. Obviously only `OnNextNotification` will return true for `HasValue` and have a useful implementation of `Value`. It should also be obvious that `OnErrorNotification` is the only implementation that will have a value for `Exception`. The `Kind` property returns an `enum` which should allow you to know which methods are appropriate to use.

```csharp
public enum NotificationKind
{
    OnNext,
    OnError,
    OnCompleted,
}
```

In this next example we produce a faulted sequence. Note that the final value of the materialized sequence is an `OnErrorNotification`. Also that the materialized sequence does not error, it completes successfully.

```csharp
var source = new Subject<int>();
source.Materialize()
      .Dump("Materialize");

source.OnNext(1);
source.OnNext(2);
source.OnNext(3);

source.OnError(new Exception("Fail?"));
```

Output:

```
Materialize --> OnNext(1)
Materialize --> OnNext(2)
Materialize --> OnNext(3)
Materialize --> OnError(System.Exception)
Materialize completed
```

Materializing a sequence can be very handy for performing analysis or logging of a sequence. You can unwrap a materialized sequence by applying the `Dematerialize` extension method. The `Dematerialize` will only work on `IObservable<Notification<TSource>>`.

