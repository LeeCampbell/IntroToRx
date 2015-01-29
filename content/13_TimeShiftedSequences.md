<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Time-shifted sequences</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="TimeShiftedSequences"></a>
    <h1>Time-shifted sequences</h1>
    <p>
        When working with observable sequences, the time axis is an unknown quantity: when
        will the next notification arrive? When consuming an <em>IEnumerable</em> sequence,
        asynchrony is not a concern; when we call <code>MoveNext()</code>, we are blocked
        until the sequence yields. This chapter looks at the various methods we can apply
        to an observable sequence when its relationship with time is a concern.
    </p>
    <a name="Buffer"></a>
    <h2>Buffer</h2>
    <p>
        Our first subject will be the <em>Buffer</em> method. In some situations, you may
        not want a deluge of individual notifications to process. Instead, you might prefer
        to work with batches of data. It may be the case that processing one item at a time
        is just too expensive, and the trade-off is to deal with messages in batches, at
        the cost of accepting a delay.
    </p>
    <p>
        The <em>Buffer</em> operator allows you to store away a range of values and then
        re-publish them as a list once the buffer is full. You can temporarily withhold
        a specified number of elements, stash away all the values for a given time span,
        or use a combination of both count and time. <em>Buffer</em> also offers more advanced
        overloads that we will look at in a future chapter.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            int count)
        {...}
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan)
        {...}
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            int count)
        {...}
    </pre>
    <p>
        The two overloads of <em>Buffer</em> are straight forward and should make it simple
        for other developers to understand the intent of the code.
    </p>
    <pre class="csharpcode">
        IObservable&lt;IList&lt;T&gt;&gt; bufferedSequence;
        bufferedSequence = mySequence.Buffer(4);
        //or
        bufferedSequence = mySequence.Buffer(TimeSpan.FromSeconds(1))
    </pre>
    <p>
        For some use cases, it may not be enough to specify only a buffer size and a maximum
        delay period. Some systems may have a sweet spot for the size of a batch they can
        process, but also have a time constraint to ensure that data is not stale. In this
        case buffering by both time and count would be suitable.
    </p>
    <p>
        In this example below, we create a sequence that produces the first ten values one
        second apart, then a further hundred values within another second. We buffer by
        a maximum period of three seconds and a maximum batch size of fifteen values.
    </p>
    <pre class="csharpcode">
        var idealBatchSize = 15;
        var maxTimeDelay = TimeSpan.FromSeconds(3);
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10)
            .Concat(Observable.Interval(TimeSpan.FromSeconds(0.01)).Take(100));

        source.Buffer(maxTimeDelay, idealBatchSize)
            .Subscribe(
                buffer =&gt; Console.WriteLine("Buffer of {1} @ {0}", DateTime.Now, buffer.Count), 
                () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">Buffer of 3 @ 01/01/2012 12:00:03</div>
        <div class="line">Buffer of 3 @ 01/01/2012 12:00:06</div>
        <div class="line">Buffer of 3 @ 01/01/2012 12:00:09</div>
        <div class="line">Buffer of 15 @ 01/01/2012 12:00:10</div>
        <div class="line">Buffer of 15 @ 01/01/2012 12:00:10</div>
        <div class="line">Buffer of 15 @ 01/01/2012 12:00:10</div>
        <div class="line">Buffer of 15 @ 01/01/2012 12:00:11</div>
        <div class="line">Buffer of 15 @ 01/01/2012 12:00:11</div>
        <div class="line">Buffer of 15 @ 01/01/2012 12:00:11</div>
        <div class="line">Buffer of 11 @ 01/01/2012 12:00:11</div>
    </div>
    <p>
        Note the variations in time and buffer size. We never get a buffer containing more
        than fifteen elements, and we never wait more than three seconds. A practical application
        of this is when you are loading data from an external source into an <em>ObservableCollection&lt;T&gt;</em>
        in a WPF application. It may be the case that adding one item at a time is just
        an unnecessary load on the dispatcher (especially if you are expecting over a hundred
        items). You may have also measured, for example that processing a batch of fifty
        items takes 100ms. You decide that this is the maximum amount of time you want to
        block the dispatcher, to keep the application responsive. This could give us two
        reasonable values to use: <code>source.Buffer(TimeSpan.FromMilliseconds(100), 50)</code>.
        This means the longest we will block the UI is about 100ms to process a batch of
        50 values, and we will never have values waiting for longer than 100ms before they
        are processed.
    </p>
    <a name="OverlappingBuffers"></a>
    <h3>Overlapping buffers</h3>
    <p>
        <em>Buffer</em> also offers overloads to manipulate the overlapping of the buffers.
        The variants we have looked at so far do not overlap and have no gaps between buffers,
        i.e. all values from the source are propagated through.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            int count, 
            int skip)
        {...}
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            TimeSpan timeShift)
        {...}
    </pre>
    <p>
        There are three interesting things you can do with overlapping buffers:
    </p>
    <dl>
        <dt>Overlapping behavior</dt>
        <dd>
            Ensure that current buffer includes some or all values from previous buffer</dd>
        <dt>Standard behavior</dt>
        <dd>
            Ensure that each new buffer only has new data</dd>
        <dt>Skip behavior</dt>
        <dd>
            Ensure that each new buffer not only contains new data exclusively, but also ignores
            one or more values since the previous buffer</dd>
    </dl>
    <a name="OverlappingBuffersByCount"></a>
    <h4>Overlapping buffers by count</h4>
    <p>
        If you are specifying a buffer size as a count, then you need to use this overload.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            int count, 
            int skip)
        {...}
    </pre>
    <p>
        You can apply the above scenarios as follows:
    </p>
    <dl>
        <dt>Overlapping behavior</dt>
        <dd>
            <em>skip</em> &lt; <em>count</em>*</dd>
        <dt>Standard behavior</dt>
        <dd>
            <em>skip</em> = <em>count</em></dd>
        <dt>Skip behavior</dt>
        <dd>
            <em>skip</em> &gt; <em>count</em></dd>
    </dl>
    <p class="comment">
        *The <em>skip</em> parameter cannot be less than or equal to zero. If you want to
        use a value of zero (i.e. each buffer contains all values), then consider using
        the <a href="07_Aggregation.html#Scan"><em>Scan</em></a> method instead with an
        <em>IList&lt;T&gt;</em> as the accumulator.
    </p>
    <p>
        Let's see each of these in action. In this example, we have a source that produces
        values every second. We apply each of the variations of the buffer overload.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
        source.Buffer(3, 1)
            .Subscribe(
                buffer =&gt;
                {
                    Console.WriteLine("--Buffered values");
                    foreach (var value in buffer)
                    {
                        Console.WriteLine(value);
                    }
                }, () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output
    </p>
    <div class="output">
        <div class="line">--Buffered values</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">--Buffered values</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">--Buffered values</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">--Buffered values</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">5</div>
        <div class="line">etc....</div>
    </div>
    <p>
        Note that in each buffer, one value is skipped from the previous batch. If we change
        the <em>skip</em> parameter from 1 to 3 (same as the buffer size), we see standard
        buffer behavior.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
        source.Buffer(3, 3)
            ...
    </pre>
    <p>
        Output
    </p>
    <div class="output">
        <div class="line">--Buffered values</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">--Buffered values</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">5</div>
        <div class="line">--Buffered values</div>
        <div class="line">6</div>
        <div class="line">7</div>
        <div class="line">8</div>
        <div class="line">--Buffered values</div>
        <div class="line">9</div>
        <div class="line">Completed</div>
    </div>
    <p>
        Finally, if we change the <em>skip</em> parameter to 5 (a value greater than the
        count of 3), we can see that two values are lost between each buffer.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
        source.Buffer(3, 5)
            ...
    </pre>
    <p>
        Output
    </p>
    <div class="output">
        <div class="line">--Buffered values</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">--Buffered values</div>
        <div class="line">5</div>
        <div class="line">6</div>
        <div class="line">7</div>
        <div class="line">Completed</div>
    </div>
    <a name="OverlappingBuffersByTime"></a>
    <h4>Overlapping buffers by time</h4>
    <p>
        You can, of course, apply the same three behaviors with buffers defined by time
        instead of count.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IList&lt;TSource&gt;&gt; Buffer&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            TimeSpan timeShift)
        {...}
    </pre>
    <p>
        To exactly replicate the output from our <a href="#OverlappingBuffersByCount">Overlapping
            Buffers By Count</a> examples, we only need to provide the following arguments:
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
        var overlapped = source.Buffer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1));
        var standard = source.Buffer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3));
        var skipped = source.Buffer(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(5));
    </pre>
    <p>
        As our source produces values consistently every second, we can use the same
        values from our count example but as seconds.
    </p>
    <a name="Delay"></a>
    <h2>Delay</h2>
    <p>
        The <em>Delay</em> extension method is a purely a way to time-shift an entire sequence.
        You can provide either a relative time the sequence should be delayed by using a
        <em>TimeSpan</em>, or an absolute point in time that the sequence should wait for
        using a <em>DateTimeOffset</em>. The relative time intervals between the values
        are preserved.
    </p>
    <pre class="csharpcode">
        // Time-shifts the observable sequence by a relative time.
        public static IObservable&lt;TSource&gt; Delay&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime)
        {...}

        // Time-shifts the observable sequence by a relative time.
        public static IObservable&lt;TSource&gt; Delay&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime, 
            IScheduler scheduler)
        {...}

        // Time-shifts the observable sequence by an absolute time.
        public static IObservable&lt;TSource&gt; Delay&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            DateTimeOffset dueTime)
        {...}

        // Time-shifts the observable sequence by an absolute time.
        public static IObservable&lt;TSource&gt; Delay&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            DateTimeOffset dueTime, 
            IScheduler scheduler)
        {...}
    </pre>
    <p>
        To show the <em>Delay</em> method in action, we create a sequence of values one
        second apart and timestamp them. This will show that it is not the subscription
        that is being delayed, but the actual forwarding of the notifications to our final
        subscriber.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(5)
            .Timestamp();

        var delay = source.Delay(TimeSpan.FromSeconds(2));

        source.Subscribe(
            value =&gt; Console.WriteLine("source : {0}", value),
            () =&gt; Console.WriteLine("source Completed"));
        delay.Subscribe(
            value =&gt; Console.WriteLine("delay : {0}", value),
            () =&gt; Console.WriteLine("delay Completed"));
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">source : 0@01/01/2012 12:00:00 pm +00:00</div>
        <div class="line">source : 1@01/01/2012 12:00:01 pm +00:00</div>
        <div class="line">source : 2@01/01/2012 12:00:02 pm +00:00</div>
        <div class="line">delay : 0@01/01/2012 12:00:00 pm +00:00</div>
        <div class="line">source : 3@01/01/2012 12:00:03 pm +00:00</div>
        <div class="line">delay : 1@01/01/2012 12:00:01 pm +00:00</div>
        <div class="line">source : 4@01/01/2012 12:00:04 pm +00:00</div>
        <div class="line">source Completed</div>
        <div class="line">delay : 2@01/01/2012 12:00:02 pm +00:00</div>
        <div class="line">delay : 3@01/01/2012 12:00:03 pm +00:00</div>
        <div class="line">delay : 4@01/01/2012 12:00:04 pm +00:00</div>
        <div class="line">delay Completed</div>
    </div>
    <p>
        It is worth noting that <em>Delay</em> will not time-shift <em>OnError</em> notifications.
        These will be propagated immediately.
    </p>
    <a name="Sample"></a>
    <h2>Sample</h2>
    <p>
        The <em>Sample</em> method simply takes the last value for every specified <em>TimeSpan</em>.
        This is great for getting timely data from a sequence that produces too much information
        for your requirements. This example shows sample in action.
    </p>
    <pre class="csharpcode">
        var interval = Observable.Interval(TimeSpan.FromMilliseconds(150));
        interval.Sample(TimeSpan.FromSeconds(1))
                .Subscribe(Console.WriteLine);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">5</div>
        <div class="line">12</div>
        <div class="line">18</div>
    </div>
    <p>
        This output is interesting and this is the reason why I choose the value of 150ms.
        If we plot the underlying sequence of values against the time they are produced,
        we can see that <em>Sample</em> is taking the last value it received for each period
        of one second.
    </p>
    <table style="font-size: 11px; font-family: Segoe UI; text-align: right">
        <tr>
            <th>
                Relative time (ms)
            </th>
            <th>
                Source value
            </th>
            <th>
                Sampled value
            </th>
        </tr>
        <tr>
            <td>
                0
            </td>
            <td width="74" style='width: 80px'>
            </td>
            <td width="74" style='width: 80px'>
            </td>
        </tr>
        <tr>
            <td>
                50
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                100
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                150
            </td>
            <td>
                0
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                200
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                250
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                300
            </td>
            <td>
                1
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                350
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                400
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                450
            </td>
            <td>
                2
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                500
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                550
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                600
            </td>
            <td>
                3
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                650
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                700
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                750
            </td>
            <td>
                4
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                800
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                850
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                900
            </td>
            <td>
                5
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                950
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1000
            </td>
            <td>
            </td>
            <td>
                5
            </td>
        </tr>
        <tr>
            <td>
                1050
            </td>
            <td>
                6
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1100
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1150
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1200
            </td>
            <td>
                7
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1250
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1300
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1350
            </td>
            <td>
                8
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1400
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1450
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1500
            </td>
            <td>
                9
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1550
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1600
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1650
            </td>
            <td>
                10
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1700
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1750
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1800
            </td>
            <td>
                11
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1850
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1900
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                1950
            </td>
            <td>
                12
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2000
            </td>
            <td>
            </td>
            <td>
                12
            </td>
        </tr>
        <tr>
            <td>
                2050
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2100
            </td>
            <td>
                13
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2150
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2200
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2250
            </td>
            <td>
                14
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2300
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2350
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2400
            </td>
            <td>
                15
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2450
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2500
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2550
            </td>
            <td>
                16
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2600
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2650
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2700
            </td>
            <td>
                17
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2750
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2800
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2850
            </td>
            <td>
                18
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2900
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                2950
            </td>
            <td>
            </td>
            <td>
            </td>
        </tr>
        <tr>
            <td>
                3000
            </td>
            <td>
                19
            </td>
            <td>
                19
            </td>
        </tr>
    </table>
    <a name="Throttle"></a>
    <h2>Throttle</h2>
    <p>
        The <em>Throttle</em> extension method provides a sort of protection against sequences
        that produce values at variable rates and sometimes too quickly. Like the <em>Sample</em>
        method, <em>Throttle</em> will return the last sampled value for a period of time.
        Unlike <em>Sample</em> though, <em>Throttle</em>'s period is a sliding window. Each
        time <em>Throttle</em> receives a value, the window is reset. Only once the period
        of time has elapsed will the last value be propagated. This means that the <em>Throttle</em>
        method is only useful for sequences that produce values at a variable rate. Sequences
        that produce values at a constant rate (like <em>Interval</em> or <em>Timer</em>)
        either would have all of their values suppressed if they produced values faster
        than the throttle period, or all of their values would be propagated if they produced
        values slower than the throttle period.
    </p>
    <pre class="csharpcode">
        // Ignores values from an observable sequence which are followed by another value before
        //  dueTime.
        public static IObservable&lt;TSource&gt; Throttle&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime)
        {...}
        public static IObservable&lt;TSource&gt; Throttle&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime, 
            IScheduler scheduler)
        {...}
    </pre>
    <p>
        A great application of the <em>Throttle</em> method would be to use it with a live
        search like "Google Suggest". While the user is still typing we can hold off on
        the search. Once there is a pause for a given period, we can execute the search
        with what they have typed. The Rx team has a great example of this scenario in the
        <a href="http://download.microsoft.com/download/C/5/D/C5D669F9-01DF-4FAF-BBA9-29C096C462DB/Rx%20HOL%20.NET.pdf"
            title="Rx Hands On Lab as PDF - Mcrosoft.com">Rx Hands On Lab</a>
    </p>
    <a name="Timeout"></a>
    <h2>Timeout</h2>
    <p>
        We have considered handling timeout exceptions previously in the chapter on <a href="11_AdvancedErrorHandling.html#CatchSwallowingException">
            Flow control</a>. The <em>Timeout</em> extension method allows us terminate
        the sequence with an error if we do not receive any notifications for a given period.
        We can either specify the period as a sliding window with a <em>TimeSpan</em>, or
        as an absolute time that the sequence must complete by providing a <em>DateTimeOffset</em>.
    </p>
    <pre class="csharpcode">
        // Returns either the observable sequence or a TimeoutException if the maximum duration
        //  between values elapses.
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime)
        {...}
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime, 
            IScheduler scheduler)
        {...}

        // Returns either the observable sequence or a TimeoutException if dueTime elapses.
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            DateTimeOffset dueTime)
        {...}
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            DateTimeOffset dueTime, 
            IScheduler scheduler)
        {...}
    </pre>
    <p>
        If we provide a <em>TimeSpan</em> and no values are produced within that time span,
        then the sequence fails with a <em>TimeoutException</em>.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromMilliseconds(100)).Take(10)
            .Concat(Observable.Interval(TimeSpan.FromSeconds(2)));

        var timeout = source.Timeout(TimeSpan.FromSeconds(1));
        timeout.Subscribe(
            Console.WriteLine, 
            Console.WriteLine, 
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">System.TimeoutException: The operation has timed out.</div>
    </div>
    <p>
        Like the <em>Throttle</em> method, this overload is only useful for sequences that
        produce values at a variable rate.
    </p>
    <p>
        The alternative use of <em>Timeout</em> is to set an absolute time; the sequence
        must be completed by then.
    </p>
    <pre class="csharpcode">
        var dueDate = DateTimeOffset.UtcNow.AddSeconds(4);
        var source = Observable.Interval(TimeSpan.FromSeconds(1));
        var timeout = source.Timeout(dueDate);
        timeout.Subscribe(
            Console.WriteLine, 
            Console.WriteLine, 
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">System.TimeoutException: The operation has timed out.</div>
    </div>
    <p>
        Perhaps an even more interesting usage of the <em>Timeout</em> method is to substitute
        in an alternative sequence when a timeout occurs. The <em>Timeout</em> method has
        overloads the provide the option of specifying a continuation sequence to use if
        a timeout occurs. This functionality behaves much like the <a href="11_AdvancedErrorHandling.html#Catch">
            Catch</a> operator. It is easy to imagine that the simple overloads actually
        just call through to these over loads and specify an <code>Observable.Throw&lt;TimeoutException&gt;</code>
        as the continuation sequence.
    </p>
    <pre class="csharpcode">
        // Returns the source observable sequence or the other observable sequence if the maximum 
        //  duration between values elapses.
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime, 
            IObservable&lt;TSource&gt; other)
        {...}
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan dueTime, 
            IObservable&lt;TSource&gt; other, 
            IScheduler scheduler)
        {...}

        // Returns the source observable sequence or the other observable sequence if dueTime 
        //  elapses.
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            DateTimeOffset dueTime, 
            IObservable&lt;TSource&gt; other)
        {...}  
        public static IObservable&lt;TSource&gt; Timeout&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            DateTimeOffset dueTime, 
            IObservable&lt;TSource&gt; other, 
            IScheduler scheduler)
        {...}
    </pre>
    <!--
        Observable.GroupByUntil
        Observable.Merge reprise(has options to take schedulers too)
    -->
    <p>
        Rx provides features to tame the unpredictable element of time in a reactive paradigm.
        Data can be buffered, throttled, sampled or delayed to meet your needs. Entire sequences
        can be shifted in time with the delay feature, and timeliness of data can be asserted
        with the <em>Timeout</em> operator. These simple yet powerful features further extend
        the developer's tool belt for querying data in motion.
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
