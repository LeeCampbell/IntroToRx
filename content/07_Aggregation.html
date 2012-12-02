<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Aggregation</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="Aggregation"></a>
    <h1>Aggregation</h1>
    <p>
        Data is not always valuable is its raw form. Sometimes we need to consolidate, collate,
        combine or condense the mountains of data we receive into more consumable bite sized
        chunks. Consider fast moving data from domains like instrumentation, finance, signal
        processing and operational intelligence. This kind of data can change at a rate
        of over ten values per second. Can a person actually consume this? Perhaps for human
        consumption, aggregate values like averages, minimums and maximums can be of more
        use.
    </p>
    <p>
        Continuing with the theme of reducing an observable sequence, we will look at the
        aggregation functions that are available to us in Rx. Our first set of methods continues
        on from our last chapter, as they take an observable sequence and reduce it to a
        sequence with a single value. We then move on to find operators that can transition
        a sequence back to a scalar value, a functional fold.
    </p>
    <p>
        Just before we move on to introducing the new operators, we will quickly create
        our own extension method. We will use this 'Dump' extension method to help build
        our samples.
    </p>
    <pre class="csharpcode">
        public static class SampleExtentions
        {
	        public static void Dump&lt;T&gt;(this IObservable&lt;T&gt; source, string name)
	        {
		        source.Subscribe(
			        i=&gt;Console.WriteLine("{0}--&gt;{1}", name, i), 
			        ex=&gt;Console.WriteLine("{0} failed--&gt;{1}", name, ex.Message),
			        ()=&gt;Console.WriteLine("{0} completed", name));
	        }
        }
    </pre>
    <p>
        Those who use <a href="http://www.linqpad.net/">LINQPad</a> will recognize that this is the source of inspiration. For those
        who have not used LINQPad, I highly recommend it. It is perfect for whipping up
        quick samples to validate a snippet of code. LINQPad also fully supports the <em>IObservable&lt;T&gt;</em>
        type.
    </p>
    <a name="Count"></a>
    <h2>Count</h2>
    <p>
        <em>Count</em> is a very familiar extension method for those that use LINQ on <em>IEnumerable&lt;T&gt;</em>.
        Like all good method names, it "does what it says on the tin". The Rx version deviates
        from the <em>IEnumerable&lt;T&gt;</em> version as Rx will return an observable sequence,
        not a scalar value. The return sequence will have a single value being the count
        of the values in the source sequence. Obviously we cannot provide the count until
        the source sequence completes.
    </p>
    <pre class="csharpcode">
        var numbers = Observable.Range(0,3);
        numbers.Dump("numbers");
        numbers.Count().Dump("count");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">numbers--&gt;1</div>
        <div class="line">numbers--&gt;2</div>
        <div class="line">numbers--&gt;3</div>
        <div class="line">numbers Completed</div>
        <div class="line">count--&gt;3</div>
        <div class="line">count Completed</div>
    </div>
    <p>
        If you are expecting your sequence to have more values than a 32 bit integer can
        hold, there is the option to use the <em>LongCount</em> extension method. This
        is just the same as <em>Count</em> except it returns an <em>IObservable&lt;long&gt;</em>.
    </p>
    <a name="MaxAndMin"></a>
    <h2>Min, Max, Sum and Average</h2>
    <p>
        Other common aggregations are <em>Min</em>, <em>Max</em>, <em>Sum</em> and <em>Average</em>.
        Just like <em>Count</em>, these all return a sequence with a single value. Once
        the source completes the result sequence will produce its value and then complete.
    </p>
    <pre class="csharpcode">
        var numbers = new Subject&lt;int&gt;();
        
        numbers.Dump("numbers");
        numbers.Min().Dump("Min");
        numbers.Average().Dump("Average");

        numbers.OnNext(1);
        numbers.OnNext(2);
        numbers.OnNext(3);
        numbers.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">numbers--&gt;1</div>
        <div class="line">numbers--&gt;2</div>
        <div class="line">numbers--&gt;3</div>
        <div class="line">numbers Completed</div>
        <div class="line">min--&gt;1</div>
        <div class="line">min Completed</div>
        <div class="line">avg--&gt;2</div>
        <div class="line">avg Completed</div>
    </div>
    <p>
        The <em>Min</em> and <em>Max</em> methods have overloads that allow you to provide
        a custom implementation of an <em>IComparer&lt;T&gt;</em> to sort your values in
        a custom way. The <em>Average</em> extension method specifically calculates the
        mean (as opposed to median or mode) of the sequence. For sequences of integers (int
        or long) the output of <em>Average</em> will be an <em>IObservable&lt;double&gt;</em>.
        If the source is of nullable integers then the output will be <em>IObservable&lt;double?&gt;</em>.
        All other numeric types (<em>float</em>, <em>double</em>, <em>decimal</em> and their
        nullable equivalents) will result in the output sequence being of the same type
        as the input sequence.
    </p>
    <a name="Fold"></a>
    <h2>Functional folds</h2>
    <p>
        Finally we arrive at the set of methods in Rx that meet the functional description
        of catamorphism/fold. These methods will take an <em>IObservable&lt;T&gt;</em> and
        produce a <code>T</code>.
    </p>
    <p>
        Caution should be prescribed whenever using any of these fold methods on an observable
        sequence, as they are all blocking. The reason you need to be careful with blocking
        methods is that you are moving from an asynchronous paradigm to a synchronous one,
        and without care you can introduce concurrency problems such as locking UIs and
        deadlocks. We will take a deeper look into these problems in a later chapter when
        we look at concurrency.
    </p>
    <p class="comment">
        It is worth noting that in the soon to be released .NET 4.5 and Rx 2.0 will provide
        support for avoiding these concurrency problems. The new <code>async</code>/<code>await</code>
        keywords and related features in Rx 2.0 can help exit the monad in a safer way.
    </p>
    <a name="First"></a>
    <h3>First</h3>
    <p>
        The <em>First()</em> extension method simply returns the first value from a sequence.
    </p>
    <pre class="csharpcode">
        var interval = Observable.Interval(TimeSpan.FromSeconds(3));
        //Will block for 3s before returning
        Console.WriteLine(interval.First());
    </pre>
    <p>
        If the source sequence does not have any values (i.e. is an empty sequence) then
        the <em>First</em> method will throw an exception. You can cater for this in three
        ways:
    </p>
    <ul>
        <li>Use a try/catch blocks around the <em>First()</em> call</li>
        <li>Use <em>Take(1)</em> instead. However, this will be asynchronous, not blocking.</li>
        <li>Use <em>FirstOrDefault</em> extension method instead</li>
    </ul>
    <p>
        The <em>FirstOrDefault</em> will still block until the source produces any notification.
        If the notification is an <em>OnError</em> then it will be thrown. If the notification
        is an <em>OnNext</em> then that value will be returned, otherwise if it is an <em>OnCompleted</em>
        the default will be returned. As we have seen in earlier methods, we can either
        choose to use the parameterless method in which the default value will be <code>default(T)</code>
        (i.e. null for reference types or the zero value for value types), alternatively
        we can provide our own default value to use.
    </p>
    <p>
        A special mention should be made for the unique relationship that <em>BehaviorSubject</em>
        and the <em>First()</em> extension method has. The reason behind this is that the
        <em>BehaviorSubject</em> is guaranteed to have a notification, be it a value, an
        error or a completion. This effectively removes the blocking nature of the <em>First</em>
        extension method when used with a <em>BehaviorSubject</em>. This can be used to
        make behavior subjects act like properties.
    </p>
    <a name="Last"></a>
    <h3>Last</h3>
    <p>
        The <em>Last</em> and <em>LastOrDefault</em> will block until the source completes
        and then return the last value. Just like the <em>First()</em> method any <em>OnError</em>
        notifications will be thrown. If the sequence is empty then <em>Last()</em> will
        throw an <em>InvalidOperationException</em>, but you can use <em>LastOrDefault</em>
        to avoid this.
    </p>
    <a name="Single"></a>
    <h3>Single</h3>
    <p>
        The <em>Single</em> extension method is for getting the single value from a sequence.
        The difference between this and <em>First()</em> or <em>Last()</em> is that it helps
        to assert your assumption that the sequence will only contain a single value. The
        method will block until the source produces a value and then completes. If the sequence
        produces any other combination of notifications then the method will throw. This
        method works especially well with <em>AsyncSubject</em> instances as they only produce
        a single value sequences.
    </p>
    <a name="BuildYourOwn"></a>
    <h2>Build your own aggregations</h2>
    <p>
        If the provided aggregations do not meet your needs, you can build your own. Rx
        provides two different ways to do this.
    </p>
    <a name="Aggregate"></a>
    <h3>Aggregate</h3>
    <p>
        The <em>Aggregate</em> method allows you to apply an accumulator function to the
        sequence. For the basic overload, you need to provide a function that takes the
        current state of the accumulated value and the value that the sequence is pushing.
        The result of the function is the new accumulated value. This overload signature
        is as follows:
    </p>
    <pre class="csharpcode">
        IObservable&lt;TSource&gt; Aggregate&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TSource, TSource&gt; accumulator)
    </pre>
    <p>
        If you wanted to produce your own version of <em>Sum</em> for <code>int</code> values, you could do so by
        providing a function that just adds to the current state of the accumulator.
    </p>
    <pre class="csharpcode">
        var sum = source.Aggregate((acc, currentValue) => acc + currentValue);
    </pre>
    <p>
        This overload of <em>Aggregate</em> has several problems. First is that it requires
        the aggregated value must be the same type as the sequence values. We have already
        seen in other aggregates like <em>Average</em> this is not always the case. Secondly,
        this overload needs at least one value to be produced from the source or the output
        will error with an <em>InvalidOperationException</em>. It should be completely valid
        for us to use <em>Aggregate</em> to create our own <em>Count</em> or <em>Sum</em>
        on an empty sequence. To do this you need to use the other overload. This overload
        takes an extra parameter which is the seed. The seed value provides an initial accumulated
        value. It also allows the aggregate type to be different to the value type.
    </p>
    <pre class="csharpcode">
        IObservable&lt;TAccumulate&gt; Aggregate&lt;TSource, TAccumulate&gt;(
            this IObservable&lt;TSource&gt; source, 
            TAccumulate seed, 
            Func&lt;TAccumulate, TSource, TAccumulate&gt; accumulator)
    </pre>
    <p>
        To update our <em>Sum</em> implementation to use this overload is easy. Just
        add the seed which will be 0. This will now return 0 as the sum when the sequence
        is empty which is just what we want. You also now can also create your own version
        of <em>Count</em>.
    </p>
    <pre class="csharpcode">
        var sum = source.Aggregate(0, (acc, currentValue) => acc + currentValue);
        var count = source.Aggregate(0, (acc, currentValue) => acc + 1);
        //or using '_' to signify that the value is not used.
        var count = source.Aggregate(0, (acc, _) => acc + 1);
    </pre>
    <p>
        As an exercise write your own <em>Min</em> and <em>Max</em> methods using <em>Aggregate</em>.
        You will probably find the <em>IComparer&lt;T&gt;</em> interface useful, and in
        particular the static <code>Comparer&lt;T&gt;.Default</code> property. When you
        have done the exercise, continue to the example implementations...
    </p>
    <hr style="page-break-after: always" />
    <p>
        Examples of creating <em>Min</em> and <em>Max</em> from <em>Aggregate</em>:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;T&gt; MyMin&lt;T&gt;(this IObservable&lt;T&gt; source)
        {
            return source.Aggregate(
                (min, current) =&gt; Comparer&lt;T&gt;
                    .Default
                    .Compare(min, current) &gt; 0 
                        ? current 
                        : min);
        }

        public static IObservable&lt;T&gt; MyMax&lt;T&gt;(this IObservable&lt;T&gt; source)
        {
            var comparer = Comparer&lt;T&gt;.Default;
            Func&lt;T, T, T&gt; max = 
                (x, y) =&gt;
                {
                    if(comparer.Compare(x, y) &lt; 0)
                    {
                        return y;
                    }
                    return x;
                };
            return source.Aggregate(max);
        }
    </pre>
    <a name="Scan"></a>
    <h3>Scan</h3>
    <p>
        While <em>Aggregate</em> allows us to get a final value for sequences that will
        complete, sometimes this is not what we need. If we consider a use case that requires
        that we get a running total as we receive values, then <em>Aggregate</em> is not
        a good fit. <em>Aggregate</em> is also not a good fit for infinite sequences. The
        <em>Scan</em> extension method however meets this requirement perfectly. The signatures
        for both <em>Scan</em> and <em>Aggregate</em> are the same; the difference is that
        <em>Scan</em> will push the <i>result</i> from every call to the accumulator function.
        So instead of being an aggregator that reduces a sequence to a single value sequence,
        it is an accumulator that we return an accumulated value for each value of the source
        sequence. In this example we produce a running total.
    </p>
    <pre class="csharpcode">
        var numbers = new Subject&lt;int&gt;();
        var scan = numbers.Scan(0, (acc, current) => acc + current);

        numbers.Dump("numbers");
        scan.Dump("scan");

        numbers.OnNext(1);
        numbers.OnNext(2);
        numbers.OnNext(3);
        numbers.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">numbers--&gt;1</div>
        <div class="line">sum--&gt;1</div>
        <div class="line">numbers--&gt;2</div>
        <div class="line">sum--&gt;3</div>
        <div class="line">numbers--&gt;3</div>
        <div class="line">sum--&gt;6</div>
        <div class="line">numbers completed</div>
        <div class="line">sum completed</div>
    </div>
    <p>
        It is probably worth pointing out that you use <em>Scan</em> with <em>TakeLast()</em>
        to produce <em>Aggregate</em>.
    </p>
    <pre class="csharpcode">
        source.Aggregate(0, (acc, current) =&gt; acc + current);
        //is equivalent to 
        source.Scan(0, (acc, current) =&gt; acc + current).TakeLast();
    </pre>
    <p>
        As another exercise, use the methods we have covered so far in the book to produce
        a sequence of running minimum and running maximums. The key here is that each time
        we receive a value that is less than (or more than for a Max operator) our current accumulator we should
        push that value and update the accumulator value. We don't however want to push
        duplicate values. For example, given a sequence of [2, 1, 3, 5, 0] we should see
        output like [2, 1, 0] for the running minimum, and [2, 3, 5] for the running maximum.
        We don't want to see [2, 1, 2, 2, 0] or [2, 2, 3, 5, 5]. Continue to see an example
        implementation.
    </p>
    <hr style="page-break-after: always" />
    <p>
        Example of a running minimum:
    </p>
    <pre class="csharpcode">
        var comparer = Comparer&lt;T&gt;.Default;
        Func&lt;T,T,T&gt; minOf = (x, y) =&gt; comparer.Compare(x, y) &lt; 0 ? x: y;
        var min = source.Scan(minOf)
            .DistinctUntilChanged();
    </pre>
    <p>
        Example of a running maximum:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;T&gt; RunningMax&lt;T&gt;(this IObservable&lt;T&gt; source)
        {
            return source.Scan(MaxOf)
                .Distinct();
        }
        private static T MaxOf&lt;T&gt;(T x, T y)
        {
            var comparer = Comparer&lt;T&gt;.Default;
            if (comparer.Compare(x, y) &lt; 0)
            {
                return y;
            }
            return x;
        }
    </pre>
    <p>
        While the only functional differences between the two examples is checking greater
        instead of less than, the examples show two different styles. Some people prefer
        the terseness of the first example, others like their curly braces and the verbosity
        of the second example. The key here was to compose the <em>Scan</em> method with
        the <em>Distinct</em> or <em>DistinctUntilChanged</em> methods. It is probably preferable
        to use the <em>DistinctUntilChanged</em> so that we internally are not keeping a
        cache of all values.
    </p>
    <a name="Partitioning"></a>
    <h2>Partitioning</h2>
    <p>
        Rx also gives you the ability to partition your sequence with features like the
        standard LINQ operator <em>GroupBy</em>. This can be useful for taking a single
        sequence and fanning out to many subscribers or perhaps taking aggregates on partitions.
    </p>
    <a name="MinByMaxBy"></a>
    <h3>MinBy and MaxBy</h3>
    <p>
        The <em>MinBy</em> and <em>MaxBy</em> operators allow you to partition your sequence
        based on a key selector function. Key selector functions are common in other LINQ
        operators like the <em>IEnumerable&lt;T&gt;</em> <em>ToDictionary</em> or <em>GroupBy</em>
        and the <a href="05_Filtering.html#Distinct"><em>Distinct</em></a> method. Each
        method will return you the values from the key that was the minimum or maximum respectively.
    </p>
    <pre class="csharpcode">
        // Returns an observable sequence containing a list of zero or more elements that have a 
        //  minimum key value.
        public static IObservable&lt;IList&lt;TSource&gt;&gt; MinBy&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector)
        {...}
        public static IObservable&lt;IList&lt;TSource&gt;&gt; MinBy&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            IComparer&lt;TKey&gt; comparer)
        {...}
    
        // Returns an observable sequence containing a list of zero or more elements that have a
        //  maximum key value.
        public static IObservable&lt;IList&lt;TSource&gt;&gt; MaxBy&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector)
        {...}
        public static IObservable&lt;IList&lt;TSource&gt;&gt; MaxBy&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            IComparer&lt;TKey&gt; comparer)
        {...}
    </pre>
    <p>
        Take note that each <em>Min</em> and <em>Max</em> operator has an overload that
        takes a comparer. This allows for comparing custom types or custom sorting of standard
        types.
    </p>
    <p>
        Consider a sequence from 0 to 10. If we apply a key selector that partitions the
        values in to groups based on their modulus of 3, we will have 3 groups of values.
        The values and their keys will be as follows:
    </p>
    <pre class="csharpcode">
        Func&lt;int, int&gt; keySelector = i =&gt; i % 3;
    </pre>
    <ul>
        <li>0, key: 0</li>
        <li>1, key: 1</li>
        <li>2, key: 2</li>
        <li>3, key: 0</li>
        <li>4, key: 1</li>
        <li>5, key: 2</li>
        <li>6, key: 0</li>
        <li>7, key: 1</li>
        <li>8, key: 2</li>
        <li>9, key: 0</li>
    </ul>
    <p>
        We can see here that the minimum key is 0 and the maximum key is 2. If therefore,
        we applied the <em>MinBy</em> operator our single value from the sequence would
        be the list of [0,3,6,9]. Applying the <em>MaxBy</em> operator would produce the
        list [2,5,8]. The <em>MinBy</em> and <em>MaxBy</em> operators will only yield a
        single value (like an <em>AsyncSubject</em>) and that value will be an <em>IList&lt;T&gt;</em>
        with zero or more values.
    </p>
    <p>
        If instead of the values for the minimum/maximum key, you wanted to get the minimum
        value for each key, then you would need to look at <em>GroupBy</em>.
    </p>
    <a name="GroupBy"></a>
    <h3>GroupBy</h3>
    <p>
        The <em>GroupBy</em> operator allows you to partition your sequence just as <em>IEnumerable&lt;T&gt;</em>'s
        <em>GroupBy</em> operator does. In a similar fashion to how the <em>IEnumerable&lt;T&gt;</em>
        operator returns an <em>IEnumerable&lt;IGrouping&lt;TKey, T&gt;&gt;</em>, the <em>IObservable&lt;T&gt;</em>
        <em>GroupBy</em> operator returns an <em>IObservable&lt;IGroupedObservable&lt;TKey,
            T&gt;&gt;</em>.
    </p>
    <pre class="csharpcode">
        // Transforms a sequence into a sequence of observable groups, 
        //  each of which corresponds to a unique key value, 
        //  containing all elements that share that same key value.
        public static IObservable&lt;IGroupedObservable&lt;TKey, TSource&gt;&gt; GroupBy&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector)
        {...}
        public static IObservable&lt;IGroupedObservable&lt;TKey, TSource&gt;&gt; GroupBy&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            IEqualityComparer&lt;TKey&gt; comparer)
        {...}
        public static IObservable&lt;IGroupedObservable&lt;TKey, TElement&gt;&gt; GroupBy&lt;TSource, TKey, TElement&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            Func&lt;TSource, TElement&gt; elementSelector)
        {...}
        public static IObservable&lt;IGroupedObservable&lt;TKey, TElement&gt;&gt; GroupBy&lt;TSource, TKey, TElement&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            Func&lt;TSource, TElement&gt; elementSelector, 
            IEqualityComparer&lt;TKey&gt; comparer)
        {...}
    </pre>
    <p>
        I find the last two overloads a little redundant as we could easily just compose
        a <em>Select</em> operator to the query to get the same functionality.
    </p>
    <p>
        In a similar fashion that the <em>IGrouping&lt;TKey, T&gt;</em> type extends the
        <em>IEnumerable&lt;T&gt;</em>, the <em>IGroupedObservable&lt;T&gt;</em> just extends
        <em>IObservable&lt;T&gt;</em> by adding a <em>Key</em> property. The use of the
        <em>GroupBy</em> effectively gives us a nested observable sequence.
    </p>
    <p>
        To use the <em>GroupBy</em> operator to get the minimum/maximum value for each key,
        we can first partition the sequence and then <em>Min</em>/<em>Max</em> each partition.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(0.1)).Take(10);
        var group = source.GroupBy(i =&gt; i % 3);
        group.Subscribe(
            grp =&gt; 
                grp.Min().Subscribe(
                    minValue =&gt; 
                    Console.WriteLine("{0} min value = {1}", grp.Key, minValue)),
            () =&gt; Console.WriteLine("Completed"));
        
    </pre>
    <p>
        The code above would work, but it is not good practice to have these nested subscribe
        calls. We have lost control of the nested subscription, and it is hard to read.
        When you find yourself creating nested subscriptions, you should consider how to
        apply a better pattern. In this case we can use <em>SelectMany</em> which we will
        look at in the next chapter.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(0.1)).Take(10);
        var group = source.GroupBy(i =&gt; i % 3);
        group.SelectMany(
                grp =&gt;
                    grp.Max()
                    .Select(value =&gt; new { grp.Key, value }))
            .Dump("group");
    </pre>
    <a name="NestedObservables"></a>
    <h3>Nested observables</h3>
    <p>
        The concept of a sequence of sequences can be somewhat overwhelming at first, especially
        if both sequence types are <em>IObservable</em>. While it is an advanced topic,
        we will touch on it here as it is a common occurrence with Rx. I find it easier
        if I can conceptualize a scenario or example to understand concepts better.
    </p>
    <p>
        Examples of Observables of Observables:
    </p>
    <dl>
        <dt>Partitions of Data</dt>
        <dd>
            You may partition data from a single source so that it can easily be filtered and
            shared to many sources. Partitioning data may also be useful for aggregates as we
            have seen. This is commonly done with the <em>GroupBy</em> operator.
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
    <p>
        Considering these examples, you could see how useful it could be to have the
        concept of nested observables. There are a suite of operators that work very well
        with nested observables such as <em>SelectMany</em>, <em>Merge</em> and <em>Switch</em>
        that we look at in future chapters.
    </p>
    <p>
        When working with nested observables, it can be handy to adopt the convention that
        a new sequence represents a creation (e.g. A new partition is created, new game
        host comes online, a market opens, users joins a chat, creating a file in a watched
        directory). You can then adopt the convention for what a completed inner sequence
        represents (e.g. Game host goes offline, Market Closes, User leave chat, File being
        watched is deleted). The great thing with nested observables is that a completed
        inner sequence can effectively be restarted by creating a new inner sequence.
    </p>
    <p>
        In this chapter we are starting to uncover the power of LINQ and how it applies
        to Rx. We chained methods together to recreate the effect that other methods already
        provide. While this is academically nice, it also allows us to starting thinking
        in terms of functional composition. We have also seen that some methods work nicely
        with certain types: <code>First()</code> + <em>BehaviorSubject&lt;T&gt;</em>, <code>
            Single()</code> + <em>AsyncSubject&lt;T&gt;</em>, <code>Single()</code> + <code>Aggregate()</code>
        etc. We have covered the second of our three classifications of operators, <i>catamorphism</i>.
        Next we will discover more methods to add to our functional composition tool belt
        and also find how Rx deals with our third functional concept, <i>bind</i>.
    </p>
    <p>
        Consolidating data into groups and aggregates enables sensible consumption of mass
        data. Fast moving data can be too overwhelming for batch processing systems and
        human consumption. Rx provides the ability to aggregate and partition on the fly,
        enabling real-time reporting without the need for expensive CEP or OLAP products.
    </p>
    <hr />
    <div class="webonly">
        <h1 class="ignoreToc">Additional recommended reading</h1>
        <div align="center">
            <div style="display:inline-block; vertical-align: top;  margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--C# in a nutshell Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B008E6I1K8&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;margin: 10px" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>

            </div>
            <div style="display:inline-block; vertical-align: top;  margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--C# Linq pocket reference Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=0596519249&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;margin: 10px" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>

            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--CLR via C# v4 Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B00AA36R4U&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;margin: 10px" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>

            </div>
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--Real-world functional programming Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=1933988924&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;margin: 10px" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>

            </div>           
        </div>    </div>
</body>
</html>
