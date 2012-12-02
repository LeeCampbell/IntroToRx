<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Sequences of coincidence</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <!-- TODO: The advanced overload of buffer belongs in this chapter-->
    <a name="SequencesOfCoincidence"></a>
    <h1>Sequences of coincidence</h1>
    <p>
        <!-- need reason why we care. Create a proposition and support it. -->
        <!-- * *
            Number of requests while database is down
            When person loggedin to a chat, get their tweets
            Notify when a stock price moves by more than 2% in 4 hrs
            When FX rates move by more than X in Y time, notify of trades booked in that currency
            Album sales by artist, by location, the day after concert.
            Presidential Election
            Google searches for Marathon during Marathon races
                Tweets tagged #Euro given a price change of x over a period of time y.
                Hmmm look to Rx forums for some good examples...
            -->
        <!--Complex query may require more than just composition of sequences and operators.-->
        <!--Interrogate one sequence based on the activity in another sequence   -->
    </p>
    <p>
        We can conceptualize events that have <i>duration</i> as <em>windows</em>. For example;
    </p>
    <ul>
        <li>a server is up</li>
        <li>a person is in a room</li>
        <li>a button is pressed (and not yet released).</li>
    </ul>
    <p>
        The first example could be re-worded as "for this window of time, the server was
        up". An event from one source may have a greater value if it coincides with an event
        from another source. For example, while at a music festival, you may only be interested
        in tweets (event) about an artist while they are playing (window). In finance, you
        may only be interested in trades (event) for a certain instrument while the New
        York market is open (window). In operations, you may be interested in the user sessions
        (window) that remained active during an upgrade of a system (window). In that example,
        we would be querying for coinciding windows.
    </p>
    <p>
    </p>
    <p>
        Rx provides the power to query sequences of coincidence, sometimes called 'sliding
        windows'. We already recognize the benefit that Rx delivers when querying data in
        motion. By additionally providing the power to query sequences of coincidence, Rx
        exposes yet another dimension of possibilities.
    </p>
    <a name="BufferRevisted"></a>
    <h2>Buffer revisited</h2>
    <p>
        <a href="13_TimeShiftedSequences.html#Buffer"><em>Buffer</em></a> is not a new operator
        to us; however, it can now be conceptually grouped with the window operators. Each
        of these windowing operators act on a sequence and a window of time. Each operator
        will open a window when the source sequence produces a value. The way the window
        is closed, and which values are exposed, are the main differences between each of
        the operators. Let us just quickly recap the internal working of the <em>Buffer</em>
        operator and see how this maps to the concept of "windows of time".
    </p>
    <p>
        <em>Buffer</em> will create a window when the first value is produced. It will then
        put that value into an internal cache. The window will stay open until the count
        of values has been reached; each of these values will have been cached. When the
        count has been reached, the window will close and the cache will be published to
        the result sequence as an <em>IList&lt;T&gt;</em>. When the next value is produced
        from the source, the cache is cleared and we start again. This means that <em>Buffer</em>
        will take an <em>IObservable&lt;T&gt;</em> and return an <em>IObservable&lt;IList&lt;T&gt;&gt;</em>.
    </p>
    <p>
        <em>Example Buffer with count of 3</em>
    </p>
    <div class="marble">
        <pre class="line">source|-0-1-2-3-4-5-6-7-8-9|</pre>
        <pre class="line">result|-----0-----3-----6-9|</pre>
        <pre class="line">            1     4     7</pre>
        <pre class="line">            2     5     8</pre>
    </div>
    <p class="comment">
        In this marble diagram, I have represented the list of values being returned at
        a point in time as a column of data, i.e. the values 0, 1 &amp; 2 are all returned
        in the first buffer.
    </p>
    <p>
        Understanding buffer with time is only a small step away from understanding buffer
        with count; instead of passing a count, we pass a <em>TimeSpan</em>. The closing
        of the window (and therefore the buffer's cache) is now dictated by time instead
        of the number of values. This is now more complicated as we have introduced some
        sort of scheduling. To produce the <em>IList&lt;T&gt;</em> at the correct point
        in time, we need a scheduler assigned to perform the timing. Incidentally, this
        makes testing a lot easier.
    </p>
    <p>
        <em>Example Buffer with time of 5 units</em>
    </p>
    <div class="marble">
        <pre class="line">source|-0-1-2-3-4-5-6-7-8-9-|</pre>
        <pre class="line">result|----0----2----5----7-|</pre>
        <pre class="line">           1    3    6    8</pre>
        <pre class="line">                4         9</pre>
    </div>
    <a name="Window"></a>
    <h2>Window</h2>
    <p>
        The <em>Window</em> operators are very similar to the <em>Buffer</em> operators;
        they only really differ by their return type. Where <em>Buffer</em> would take an
        <em>IObservable&lt;T&gt;</em> and return an <em>IObservable&lt;IList&lt;T&gt;&gt;</em>,
        the Window operators return an <em>IObservable&lt;IObservable&lt;T&gt;&gt;</em>.
        It is also worth noting that the <em>Buffer</em> operators will not yield their
        buffers until the window closes.
    </p>
    <p>
        Here we can see the simple overloads to <em>Window</em>. There is a surprising symmetry
        with the <em>Window</em> and <em>Buffer</em> overloads.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            int count)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            int count, 
            int skip)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            int count)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            TimeSpan timeShift)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            IScheduler scheduler)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            TimeSpan timeShift, 
            IScheduler scheduler)
        {...}
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            TimeSpan timeSpan, 
            int count, 
            IScheduler scheduler)
        {...}
    </pre>
    <p>
        This is an example of <em>Window</em> with a count of 3 as a marble diagram:
    </p>
    <div class="marble">
        <pre class="line">source |-0-1-2-3-4-5-6-7-8-9|</pre>
        <pre class="line">window0|-0-1-2|</pre>
        <pre class="line">window1        3-4-5|</pre>
        <pre class="line">window2              6-7-8|</pre>
        <pre class="line">window3                    9|</pre>
    </div>
    <p>
        For demonstration purposes, we could reconstruct that with this code.
    </p>
    <pre class="csharpcode">
        var windowIdx = 0;
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
        source.Window(3)
                .Subscribe(window =&gt;
                {
                    var id = windowIdx++;
                    Console.WriteLine("--Starting new window");
                    var windowName = "Window" + thisWindowIdx;
                    window.Subscribe(
                        value =&gt; Console.WriteLine("{0} : {1}", windowName, value),
                        ex =&gt; Console.WriteLine("{0} : {1}", windowName, ex),
                        () =&gt; Console.WriteLine("{0} Completed", windowName));
                },
                () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">--Starting new window</div>
        <div class="line">window0 : 0</div>
        <div class="line">window0 : 1</div>
        <div class="line">window0 : 2</div>
        <div class="line">window0 Completed</div>
        <div class="line">--Starting new window</div>
        <div class="line">window1 : 3</div>
        <div class="line">window1 : 4</div>
        <div class="line">window1 : 5</div>
        <div class="line">window1 Completed</div>
        <div class="line">--Starting new window</div>
        <div class="line">window2 : 6</div>
        <div class="line">window2 : 7</div>
        <div class="line">window2 : 8</div>
        <div class="line">window2 Completed</div>
        <div class="line">--Starting new window</div>
        <div class="line">window3 : 9</div>
        <div class="line">window3 Completed</div>
        <div class="line">Completed</div>
    </div>
    <p>
        <em>Example of Window with time of 5 units</em>
    </p>
    <div class="marble">
        <pre class="line">source |-0-1-2-3-4-5-6-7-8-9|</pre>
        <pre class="line">window0|-0-1-|</pre>
        <pre class="line">window1      2-3-4|</pre>
        <pre class="line">window2           -5-6-|</pre>
        <pre class="line">window3                7-8-9|</pre>
    </div>
    <p>
        A major difference we see here is that the <em>Window</em> operators can notify
        you of values from the source as soon as they are produced. The <em>Buffer</em>
        operators, on the other hand, must wait until the window closes before the values
        can be notified as an entire list.
    </p>
    <a name="FlatteningAWindowOperation"></a>
    <h3>Flattening a Window operation</h3>
    <p>
        I think it is worth noting, at least from an academic standpoint, that the <em>Window</em>
        operators produce <em>IObservable&lt;IObservable&lt;T&gt;&gt;</em>. We have explored
        the concept of <a href="07_Aggregation.html#NestedObservables">nested observables</a>
        in the earlier chapter on <a href="07_Aggregation.html">Aggregation</a>. <em>Concat</em>,
        <em>Merge</em> and <em>Switch</em> each have an overload that takes an <em>IObservable&lt;IObservable&lt;T&gt;&gt;</em>
        and returns an <em>IObservable&lt;T&gt;</em>. As the <em>Window</em> operators ensure
        that the windows (child sequences) do not overlap, we can use either of the <em>Concat</em>,
        <em>Switch</em> or <em>Merge</em> operators to turn a windowed sequence back into
        its original form.
    </p>
    <pre class="csharpcode">
        //is the same as Observable.Interval(TimeSpan.FromMilliseconds(200)).Take(10) 
         var switchedWindow = Observable.Interval(TimeSpan.FromMilliseconds(200)).Take(10)
            .Window(TimeSpan.FromMilliseconds(500))
            .Switch();
    </pre>
    <a name="CustomizingWindows"></a>
    <h3>Customizing windows</h3>
    <p>
        The overloads above provide simple ways to break a sequence into smaller nested
        windows using a count and/or a time span. Now we will look at the other overloads,
        that provide more flexibility over how windows are managed.
    </p>
    <pre class="csharpcode">
        //Projects each element of an observable sequence into consecutive non-overlapping windows.
        //windowClosingSelector : A function invoked to define the boundaries of the produced 
        //  windows. A new window is started when the previous one is closed.
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window&lt;TSource, TWindowClosing&gt;
        (
            this IObservable&lt;TSource&gt; source, 
            Func&lt;IObservable&lt;TWindowClosing&gt;&gt; windowClosingSelector
        )
        {...}
    </pre>
    <p>
        The first of these complex overloads allows us to control when windows should close.
        The <em>windowClosingSelector</em> function is called each time a window is created.
        Windows are created on subscription and immediately after a window closes; windows
        close when the sequence from the <em>windowClosingSelector</em> produces a value.
        The value is disregarded so it doesn't matter what type the sequence values are;
        in fact you can just complete the sequence from <em>windowClosingSelector</em> to
        close the window instead.
    </p>
    <p>
        In this example, we create a window with a closing selector. We return the same
        subject from that selector every time, then notify from the subject whenever a user
        presses enter from the console.
    </p>
    <pre class="csharpcode">
        var windowIdx = 0;
        var source = Observable.Interval(TimeSpan.FromSeconds(1)).Take(10);
        var closer = new Subject&lt;Unit&gt;();
        source.Window(() =&gt; closer)
                .Subscribe(window =&gt;
                {
                    var thisWindowIdx = windowIdx++;
                    Console.WriteLine("--Starting new window");
                    var windowName = "Window" + thisWindowIdx;
                    window.Subscribe(
                        value =&gt; Console.WriteLine("{0} : {1}", windowName, value),
                        ex =&gt; Console.WriteLine("{0} : {1}", windowName, ex),
                        () =&gt; Console.WriteLine("{0} Completed", windowName));
                },
                () =&gt; Console.WriteLine("Completed"));

        var input = "";
        while (input!="exit")
        {
            input = Console.ReadLine();
            closer.OnNext(Unit.Default);
        }
    </pre>
    <p>
        Output (when I hit enter after '1' and '5' are displayed):
    </p>
    <div class="output">
        <div class="line">--Starting new window</div>
        <div class="line">window0 : 0</div>
        <div class="line">window0 : 1</div>
        <div class="line"></div>
        <div class="line">window0 Completed</div>
        <div class="line">--Starting new window</div>
        <div class="line">window1 : 2</div>
        <div class="line">window1 : 3</div>
        <div class="line">window1 : 4</div>
        <div class="line">window1 : 5</div>
        <div class="line"></div>
        <div class="line">window1 Completed</div>
        <div class="line">--Starting new window</div>
        <div class="line">window2 : 6</div>
        <div class="line">window2 : 7</div>
        <div class="line">window2 : 8</div>
        <div class="line">window2 : 9</div>
        <div class="line">window2 Completed</div>
        <div class="line">Completed</div>
    </div>
    <p>
        The most complex overload of <em>Window</em> allows us to create potentially overlapping
        windows.
    </p>
    <pre class="csharpcode">
        //Projects each element of an observable sequence into zero or more windows.
        // windowOpenings : Observable sequence whose elements denote the creation of new windows.
        // windowClosingSelector : A function invoked to define the closing of each produced window.
        public static IObservable&lt;IObservable&lt;TSource&gt;&gt; Window
            &lt;TSource, TWindowOpening, TWindowClosing&gt;
        (
            this IObservable&lt;TSource&gt; source, 
            IObservable&lt;TWindowOpening&gt; windowOpenings, 
            Func&lt;TWindowOpening, IObservable&lt;TWindowClosing&gt;&gt; windowClosingSelector
        )
        {...}
    </pre>
    <p>
        This overload takes three arguments
    </p>
    <ol>
        <li>The source sequence</li>
        <li>A sequence that indicates when a new window should be opened</li>
        <li>A function that takes a window opening value, and returns a window closing sequence</li>
    </ol>
    <p>
        This overload offers great flexibility in the way windows are opened and closed.
        Windows can be largely independent from each other; they can overlap, vary in size
        and even skip values from the source.
    </p>
    <p>
        To ease our way into this more complex overload, let's first try to use it to recreate
        a simpler version of <em>Window</em> (the overload that takes a count). To do so,
        we need to open a window once on the initial subscription, and once each time the
        source has produced then specified count. The window needs to close each time that
        count is reached. To achieve this we only need the source sequence. We will share
        it by using the <em>Publish</em> method, then supply 'views' of the source as each
        of the arguments.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IObservable&lt;T&gt;&gt; MyWindow&lt;T&gt;(
            this IObservable&lt;T&gt; source, 
            int count)
        {
            var shared = source.Publish().RefCount();
            var windowEdge = shared
                .Select((i, idx) =&gt; idx % count)
                .Where(mod =&gt; mod == 0)
                .Publish()
                .RefCount();
            return shared.Window(windowEdge, _ =&gt; windowEdge);
        }
    </pre>
    <p>
        If we now want to extend this method to offer skip functionality, we need to have
        two different sequences: one for opening and one for closing. We open a window on
        subscription and again after the <em>skip</em> items have passed. We close those
        windows after '<code>count</code>' items have passed since the window opened.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IObservable&lt;T&gt;&gt; MyWindow&lt;T&gt;(
            this IObservable&lt;T&gt; source, 
            int count, 
            int skip)
        {
            if (count &lt;= 0) throw new ArgumentOutOfRangeException();
            if (skip &lt;= 0) throw new ArgumentOutOfRangeException();

            var shared = source.Publish().RefCount();
            var index = shared
                .Select((i, idx) =&gt; idx)
                .Publish()
                .RefCount();
            var windowOpen = index.Where(idx =&gt; idx % skip == 0);
            var windowClose = index.Skip(count-1);
            return shared.Window(windowOpen, _ =&gt; windowClose);
        }
    </pre>
    <p>
        We can see here that the <code>windowClose</code> sequence is re-subscribed to each
        time a window is opened, due to it being returned from a function. This allows us
        to reapply the skip (<code>Skip(count-1)</code>) for each window. Currently, we
        ignore the value that the <code>windowOpen</code> pushes to the <code>windowClose</code>
        selector, but if you require it for some logic, it is available to you.
    </p>
    <p>
        As you can see, the <em>Window</em> operator can be quite powerful. We can even
        use <em>Window</em> to replicate other operators; for instance we can create our
        own implementation of <em>Buffer</em> that way. We can have the <em>SelectMany</em>
        operator take a single value (the window) to produce zero or more values of another
        type (in our case, a single <em>IList&lt;T&gt;</em>). To create the <em>IList&lt;T&gt;</em>
        without blocking, we can apply the <em>Aggregate</em> method and use a new <em>List&lt;T&gt;</em>
        as the seed.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;IList&lt;T&gt;&gt; MyBuffer&lt;T&gt;(
            this IObservable&lt;T&gt; source, 
            int count)
        {
            return source.Window(count)
                .SelectMany(window =&gt; 
                    window.Aggregate(
                        new List&lt;T&gt;(), 
                        (list, item) =&gt;
                        {
                            list.Add(item);
                            return list;
                        }));
        }
    </pre>
    <p>
        It may be an interesting exercise to try implementing other time shifting methods,
        like <em>Sample</em> or <em>Throttle</em>, with <em>Window</em>.
    </p>
    <a name="Join"></a>
    <h2>Join</h2>
    <p>
        The <em>Join</em> operator allows you to logically join two sequences. Whereas the
        <em>Zip</em> operator would pair values from the two sequences together by index,
        the <em>Join</em> operator allows you join sequences by intersecting windows. Like
        the <em>Window</em> overload we just looked at, you can specify when a window should
        close via an observable sequence; this sequence is returned from a function that
        takes an opening value. The <em>Join</em> operator has two such functions, one for
        the first source sequence and one for the second source sequence. Like the <em>Zip</em>
        operator, we also need to provide a selector function to produce the result item
        from the pair of values.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TResult&gt; Join
            &lt;TLeft, TRight, TLeftDuration, TRightDuration, TResult&gt;
        (
            this IObservable&lt;TLeft&gt; left,
            IObservable&lt;TRight&gt; right,
            Func&lt;TLeft, IObservable&lt;TLeftDuration&gt;&gt; leftDurationSelector,
            Func&lt;TRight, IObservable&lt;TRightDuration&gt;&gt; rightDurationSelector,
            Func&lt;TLeft, TRight, TResult&gt; resultSelector
        )
    </pre>
    <p>
        This is a complex signature to try and understand in one go, so let's take it one
        parameter at a time.
    </p>
    <p>
        <em>IObservable&lt;TLeft&gt; left</em> is the source sequence that defines when
        a window starts. This is just like the <em>Buffer</em> and <em>Window</em> operators,
        except that every value published from this source opens a new window. In <em>Buffer</em>
        and <em>Window</em>, by contrast, some values just fell into an existing window.
    </p>
    <p>
        I like to think of <em>IObservable&lt;TRight&gt; right</em> as the window value
        sequence. While the left sequence controls opening the windows, the right sequence
        will try to pair up with a value from the left sequence.
    </p>
    <p>
        Let us imagine that our left sequence produces a value, which creates a new window.
        If the right sequence produces a value while the window is open, then the <code>resultSelector</code>
        function is called with the two values. This is the crux of join, pairing two values
        from a sequence that occur within the same window. This then leads us to our next
        question; when does the window close? The answer illustrates both the power and
        the complexity of the <em>Join</em> operator.
    </p>
    <p>
        When <em>left</em> produces a value, a window is opened. That value is also passed,
        at that time, to the <em>leftDurationSelector</em> function, which returns an <em>IObservable&lt;TLeftDuration&gt;</em>.
        When that sequence produces a value or completes, the window for that value is closed.
        Note that it is irrelevant what the type of <em>TLeftDuration</em> is. This initially
        left me with the feeling that <em>IObservable&lt;TLeftDuration&gt;</em> was a bit
        excessive as you effectively just need some sort of event to say 'Closed'. However,
        by being allowed to use <em>IObservable&lt;T&gt;</em>, you can do some clever manipulation
        as we will see later.
    </p>
    <p>
        Let us now imagine a scenario where the left sequence produces values twice as fast
        as the right sequence. Imagine that in addition we never close the windows; we could
        do this by always returning <em>Observable.Never&lt;Unit&gt;()</em> from the <em>leftDurationSelector</em>
        function. This would result in the following pairs being produced.
    </p>
    <p>
        Left Sequence</p>
    <div class="marble">
        <pre class="line">L 0-1-2-3-4-5-</pre>
    </div>
    <p>
        Right Sequence</p>
    <div class="marble">
        <pre class="line">R --A---B---C-</pre>
    </div>
    <div class="output">
        <div class="line">0, A</div>
        <div class="line">1, A</div>
        <div class="line">0, B</div>
        <div class="line">1, B</div>
        <div class="line">2, B</div>
        <div class="line">3, B</div>
        <div class="line">0, C</div>
        <div class="line">1, C</div>
        <div class="line">2, C</div>
        <div class="line">3, C</div>
        <div class="line">4, C</div>
        <div class="line">5, C</div>
    </div>
    <p>
        As you can see, the left values are cached and replayed each time the right produces
        a value.
    </p>
    <p>
        Now it seems fairly obvious that, if I immediately closed the window by returning
        <em>Observable.Empty&lt;Unit&gt;</em>, or perhaps <em>Observable.Return(0)</em>,
        windows would never be opened thus no pairs would ever get produced. However, what
        could I do to make sure that these windows did not overlap- so that, once a second
        value was produced I would no longer see the first value? Well, if we returned the
        <code>left</code> sequence from the <code>leftDurationSelector</code>, that could
        do the trick. But wait, when we return the sequence <code>left</code> from the <code>
            leftDurationSelector</code>, it would try to create another subscription and
        that may introduce side effects. The quick answer to that is to <em>Publish</em>
        and <em>RefCount</em> the <code>left</code> sequence. If we do that, the results
        look more like this.
    </p>
    <div class="marble">
        <pre class="line">left  |-0-1-2-3-4-5|</pre>
        <pre class="line">right |---A---B---C|</pre>
        <pre class="line">result|---1---3---5</pre>
        <pre class="line">          A   B   C</pre>
    </div>
    <p>
        The last example is very similar to <em>CombineLatest</em>, except that it is only
        producing a pair when the right sequence changes. We could use <em>Join</em> to
        produce our own version of <a href="12_CombiningSequences.html#CombineLatest"><em>CombineLatest</em></a>.
        If the values from the left sequence expire when the next value from left was notified,
        then I would be well on my way to implementing my version of <em>CombineLatest</em>.
        However I need the same thing to happen for the right. Luckily the <em>Join</em>
        operator provides a <code>rightDurationSelector</code> that works just like the
        <code>leftDurationSelector</code>. This is simple to implement; all I need to do
        is return a reference to the same left sequence when a left value is produced, and
        do the same for the right. The code looks like this.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TResult&gt; MyCombineLatest&lt;TLeft, TRight, TResult&gt;
        (
            IObservable&lt;TLeft&gt; left,
            IObservable&lt;TRight&gt; right,
            Func&lt;TLeft, TRight, TResult&gt; resultSelector
        )
        {
            var refcountedLeft = left.Publish().RefCount();
            var refcountedRight = right.Publish().RefCount();
            return Observable.Join(
                refcountedLeft,
                refcountedRight,
                value =&gt; refcountedLeft,
                value =&gt; refcountedRight,
                resultSelector);
        }
    </pre>
    <p>
        While the code above is not production quality (it would need to have some gates
        in place to mitigate race conditions), it shows how powerful <em>Join</em> is; we
        can actually use it to create other operators!
    </p>
    <a name="GroupJoin"></a>
    <h2>GroupJoin</h2>
    <p>
        When the <em>Join</em> operator pairs up values that coincide within a window, it
        will pass the scalar values left and right to the <em>resultSelector</em>. The <em>GroupJoin</em>
        operator takes this one step further by passing the left (scalar) value immediately
        to the <code>resultSelector</code> with the right (sequence) value. The right parameter
        represents all of the values from the right sequences that occur within the window.
        Its signature is very similar to <em>Join</em>, but note the difference in the <code>
            resultSelector</code> parameter.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TResult&gt; GroupJoin
            &lt;TLeft, TRight, TLeftDuration, TRightDuration, TResult&gt;
        (
            this IObservable&lt;TLeft&gt; left,
            IObservable&lt;TRight&gt; right,
            Func&lt;TLeft, IObservable&lt;TLeftDuration&gt;&gt; leftDurationSelector,
            Func&lt;TRight, IObservable&lt;TRightDuration&gt;&gt; rightDurationSelector,
            Func&lt;TLeft, IObservable&lt;TRight&gt;, TResult&gt; resultSelector
        )
    </pre>
    <p>
        If we went back to our first <em>Join</em> example where we had
    </p>
    <ul>
        <li>the <code>left</code> producing values twice as fast as the right, </li>
        <li>the left never expiring </li>
        <li>the right immediately expiring </li>
    </ul>
    <p>
        this is what the result may look like
    </p>
    <div class="marble">
        <pre class="line">left              |-0-1-2-3-4-5|</pre>
        <pre class="line">right             |---A---B---C|</pre>
        <pre class="line">0th window values   --A---B---C|</pre>
        <pre class="line">1st window values     A---B---C|</pre>
        <pre class="line">2nd window values       --B---C|</pre>
        <pre class="line">3rd window values         B---C|</pre>
        <pre class="line">4th window values           --C|</pre>
        <pre class="line">5th window values             C|</pre>
    </div>
    <p>
        We could switch it around and have the left expired immediately and the right never
        expire. The result would then look like this:
    </p>
    <div class="marble">
        <pre class="line">left              |-0-1-2-3-4-5|</pre>
        <pre class="line">right             |---A---B---C|</pre>
        <pre class="line">0th window values   |</pre>
        <pre class="line">1st window values     A|</pre>
        <pre class="line">2nd window values       A|</pre>
        <pre class="line">3rd window values         AB|</pre>
        <pre class="line">4th window values           AB|</pre>
        <pre class="line">5th window values             ABC|</pre>
    </div>
    <p>
        This starts to make things interesting. Perceptive readers may have noticed that
        with <em>GroupJoin</em> you could effectively re-create your own <em>Join</em> method
        by doing something like this:
    </p>
    <pre class="csharpcode">
        public IObservable&lt;TResult&gt; MyJoin&lt;TLeft, TRight, TLeftDuration, TRightDuration, TResult&gt;(
            IObservable&lt;TLeft&gt; left,
            IObservable&lt;TRight&gt; right,
            Func&lt;TLeft, IObservable&lt;TLeftDuration&gt;&gt; leftDurationSelector,
            Func&lt;TRight, IObservable&lt;TRightDuration&gt;&gt; rightDurationSelector,
            Func&lt;TLeft, TRight, TResult&gt; resultSelector)
        {
            return Observable.GroupJoin
            (
                left,
                right,
                leftDurationSelector,
                rightDurationSelector,
                (leftValue, rightValues)=&gt;
                    rightValues.Select(rightValue=&gt;resultSelector(leftValue, rightValue))
            )
            .Merge();
        }
    </pre>
    <p>
        You could even create a crude version of <em>Window</em> with this code:
    </p>
    <pre class="csharpcode">
        public IObservable&lt;IObservable&lt;T&gt;&gt; MyWindow&lt;T&gt;(
            IObservable&lt;T&gt; source, 
            TimeSpan windowPeriod)
        {
            return Observable.Create&lt;IObservable&lt;T&gt;&gt;(o 
                =&gt;
                {
                var sharedSource = source
                    .Publish()
                    .RefCount();

                var intervals = Observable.Return(0L)
                    .Concat(Observable.Interval(windowPeriod))
                    .TakeUntil(sharedSource.TakeLast(1))
                    .Publish()
                    .RefCount();

                return intervals.GroupJoin(
                        sharedSource, 
                        _ =&gt; intervals, 
                        _ =&gt; Observable.Empty&lt;Unit&gt;(), 
                        (left, sourceValues) =&gt; sourceValues)
                    .Subscribe(o);
            });
        }
    </pre>
    <p>
        For an alternative summary of reducing operators to a primitive set see Bart DeSmet's
        <a title="The essence of LINQ - MinLINQ" href="http://blogs.bartdesmet.net/blogs/bart/archive/2010/01/01/the-essence-of-linq-minlinq.aspx"
            target="_blank">excellent MINLINQ post</a> (and <a title="The essence of LINQ - MINLINQ - Channel9"
                href="http://channel9.msdn.com/Shows/Going+Deep/Bart-De-Smet-MinLINQ-The-Essence-of-LINQ"
                target="_blank">follow-up video</a>). Bart is one of the key members of
        the team that built Rx, so it is great to get some insight on how the creators of
        Rx think.
    </p>
    <p>
        Showcasing <em>GroupJoin</em> and the use of other operators turned out to be a
        fun academic exercise. While watching videos and reading books on Rx will increase
        your familiarity with it, nothing replaces the experience of actually picking it
        apart and using it in earnest.
    </p>
    <p>
        <em>GroupJoin</em> and other window operators reduce the need for low-level plumbing
        of state and concurrency. By exposing a high-level API, code that would be otherwise
        difficult to write, becomes a cinch to put together. For example, those in the finance
        industry could use <em>GroupJoin</em> to easily produce real-time Volume or Time
        Weighted Average Prices (VWAP/TWAP).
    </p>
    <p>
        Rx delivers yet another way to query data in motion by allowing you to interrogate
        sequences of coincidence. This enables you to solve the intrinsically complex problem
        of managing state and concurrency while performing matching from multiple sources.
        By encapsulating these low level operations, you are able to leverage Rx to design
        your software in an expressive and testable fashion. Using the Rx operators as building
        blocks, your code effectively becomes a composition of many simple operators. This
        allows the complexity of the domain code to be the focus, not the otherwise incidental
        supporting code.
    </p>
    <hr />
    <a name="Summary"></a>
    <h1>Summary</h1>
    <p>
        When LINQ was first released, it brought the ability to query static data sources
        directly into the language. With the volume of data produced in modern times, only
        being able to query data-at-rest, limits your competitive advantage. Being able to
        make sense of information as it flows, opens an entirely new spectrum of software.
        We need more than just the ability to react to events, we have been able to do this
        for years. We need the ability to construct complex queries across multiple sources
        of flowing data.
    </p>
    <p>
        Rx brings event processing to the masses by allowing you to query data-in-motion
        directly from your favorite .NET language. Composition is king: you compose operators
        to create queries and you compose sequences to enrich the data. Rx leverages common
        types, patterns and language features to deliver an incredibly powerful library
        that can change the way you write modern software.
    </p>
    <p>
        Throughout the book you will have learnt the basic types and principle of Rx. You
        have discovered functional programming concepts and how they apply to observable
        sequences. You can identify potential pitfalls of certain patterns and how to avoid
        them. You understand the internal working of the operators and are even able to
        build your own implementations of many of them. Finally you are able to construct
        complex queries that manage concurrency in a safe and declarative way while still
        being testable.
    </p>
    <p>
        You have everything you need to confidently build applications using the Reactive
        Extensions for .NET. If you do find yourself at any time stuck, and not sure how
        to solve a problem or need help, you can probably solve it without outside stimulus.
        Remember to first draw a marble diagram of what you think the problem space is.
        This should allow you to see the patterns in the flow which will help you choose
        the correct operators. Secondly, remember to follow the <a href="18_UsageGuidelines.html">
                                                                    Guidelines</a>. Third, write a spike. Use <a href="http://www.linqpad.net/">LINQPad</a> or a blank Visual Studio project
        to flesh out a small sample. Finally, if you are still stuck, your best place to
        look for help is the MSDN <a href="http://social.msdn.microsoft.com/Forums/en-US/rx/">
                                      Rx forum</a>. <a href="http://stackoverflow.com/">StackOverflow.com</a> is another
        useful resource too, but with regards to Rx questions, the MSDN forum is dedicated
        to Rx and seems to have a higher quality of answers.
    </p>
    <hr/>
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
