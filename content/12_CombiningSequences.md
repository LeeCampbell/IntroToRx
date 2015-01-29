<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Combining sequences</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="CombiningMultipleSequences"></a>
    <h1>Combining sequences</h1>
    <p>
        Data sources are everywhere, and sometimes we need to consume data from more than
        just a single source. Common examples that have many inputs include: multi touch
        surfaces, news feeds, price feeds, social media aggregators, file watchers, heart-beating/polling
        servers, etc. The way we deal with these multiple stimuli is varied too. We may
        want to consume it all as a deluge of integrated data, or one sequence at a time
        as sequential data. We could also get it in an orderly fashion, pairing data values
        from two sources to be processed together, or perhaps just consume the data from
        the first source that responds to the request.
    </p>
    <p>
        We have uncovered the benefits of operator composition; now we turn our focus to
        sequence composition. Earlier on, we briefly looked at operators that work with
        multiple sequences such as <em>SelectMany</em>, <em>TakeUntil</em>/<em>SkipUntil</em>,
        <em>Catch</em> and <em>OnErrorResumeNext</em>. These give us a hint at the potential
        that sequence composition can deliver. By uncovering the features of sequence composition
        with Rx, we find yet another layer of game changing functionality. Sequence composition
        enables you to create complex queries across multiple data sources. This unlocks
        the possibility to write some very powerful and succinct code.
    </p>
    <p>
        Now we will build upon the concepts covered in the <a href="11_AdvancedErrorHandling.html">
            Advanced Error Handling</a> chapter. There we were able to provide continuations
        for sequences that failed. We will now examine operators aimed at composing sequences
        that are still operational instead of sequences that have terminated due to an error.
    </p>
    <a name="SimpleConcatenation"></a>
    <h2>Sequential concatenation</h2>
    <p>
        The first methods we will look at are those that concatenate sequences sequentially.
        They are very similar to the methods we have seen before for dealing with faulted
        sequences.
    </p>
    <a name="Concat"></a>
    <h3>Concat</h3>
    <p>
        The <em>Concat</em> extension method is probably the most simple composition method.
        It simply concatenates two sequences. Once the first sequence completes, the second
        sequence is subscribed to and its values are passed on through to the result sequence.
        It behaves just like the <em>Catch</em> extension method, but will concatenate operational
        sequences when they complete, instead of faulted sequences when they <em>OnError</em>.
        The simple signature for <em>Concat</em> is as follows.
    </p>
    <pre class="csharpcode">
        // Concatenates two observable sequences. Returns an observable sequence that contains the
        //  elements of the first sequence, followed by those of the second the sequence.
        public static IObservable&lt;TSource&gt; Concat&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; first, 
            IObservable&lt;TSource&gt; second)
        {
            ...
        }
    </pre>
    <p>
        Usage of <em>Concat</em> is familiar. Just like <em>Catch</em> or <em>OnErrorResumeNext</em>,
        we pass the continuation sequence to the extension method.
    </p>
    <pre class="csharpcode">
        //Generate values 0,1,2 
        var s1 = Observable.Range(0, 3);
        //Generate values 5,6,7,8,9 
        var s2 = Observable.Range(5, 5);
        s1.Concat(s2)
            .Subscribe(Console.WriteLine);
    </pre>
    Returns:
    <div class="marble">
        <pre class="line">s1 --0--1--2-|</pre>
        <pre class="line">s2           -5--6--7--8--|</pre>
        <pre class="line">r  --0--1--2--5--6--7--8--|</pre>
    </div>
    <p>
        If either sequence was to fault so too would the result sequence. In particular,
        if <em>s1</em> produced an <code>OnError</code> notification, then <em>s2</em> would
        never be used. If you wanted <em>s2</em> to be used regardless of how s1 terminates,
        then <em>OnErrorResumeNext</em> would be your best option.
    </p>
    <p>
        <em>Concat</em> also has two useful overloads. These overloads allow you to pass
        multiple observable sequences as either a <code>params</code> array or an <em>IEnumerable&lt;IObservable&lt;T&gt;&gt;</em>.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TSource&gt; Concat&lt;TSource&gt;(
            params IObservable&lt;TSource&gt;[] sources)
        {...}
        
        public static IObservable&lt;TSource&gt; Concat&lt;TSource&gt;(
            this IEnumerable&lt;IObservable&lt;TSource&gt;&gt; sources)
        {...}
    </pre>
    <p>
        The ability to pass an <em>IEnumerable&lt;IObservable&lt;T&gt;&gt;</em> means that
        the multiple sequences can be lazily evaluated. The overload that takes a <code>params</code>
        array is well-suited to times when we know how many sequences we want to merge at
        compile time, whereas the <em>IEnumerable&lt;IObservable&lt;T&gt;&gt;</em> overload
        is a better fit when we do not know this ahead of time.
    </p>
    <p>
        In the case of the lazily evaluated <em>IEnumerable&lt;IObservable&lt;T&gt;&gt;</em>,
        the <em>Concat</em> method will take one sequence, subscribe until it is completed
        and then switch to the next sequence. To help illustrate this, we create a method
        that returns a sequence of sequences and is sprinkled with logging. It returns three
        observable sequences each with a single value [1], [2] and [3]. Each sequence returns
        its value on a timer delay.
    </p>
    <pre class="csharpcode">
        public IEnumerable&lt;IObservable&lt;long&gt;&gt; GetSequences()
        {
            Console.WriteLine("GetSequences() called");
            Console.WriteLine("Yield 1st sequence");
            yield return Observable.Create&lt;long&gt;(o =&gt;
                {
                    Console.WriteLine("1st subscribed to");
                    return Observable.Timer(TimeSpan.FromMilliseconds(500))
                        .Select(i=&gt;1L)
                        .Subscribe(o);
                });
            Console.WriteLine("Yield 2nd sequence");
            yield return Observable.Create&lt;long&gt;(o =&gt;
                {
                    Console.WriteLine("2nd subscribed to");
                    return Observable.Timer(TimeSpan.FromMilliseconds(300))
                        .Select(i=&gt;2L)
                        .Subscribe(o);
                });
            Thread.Sleep(1000);     //Force a delay
            Console.WriteLine("Yield 3rd sequence");
            yield return Observable.Create&lt;long&gt;(o =&gt;
                {
                    Console.WriteLine("3rd subscribed to");
                    return Observable.Timer(TimeSpan.FromMilliseconds(100))
                        .Select(i=&gt;3L)
                        .Subscribe(o);
                });
            Console.WriteLine("GetSequences() complete");
        }
    </pre>
    <p>
        When we call our <code>GetSequences</code> method and concatenate the results, we
        see the following output using our <code>Dump</code> extension method.
    </p>
    <pre class="csharpcode">
        GetSequences().Concat().Dump("Concat");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">GetSequences() called</div>
        <div class="line">Yield 1st sequence</div>
        <div class="line">1st subscribed to</div>
        <div class="line">Concat-->1</div>
        <div class="line">Yield 2nd sequence</div>
        <div class="line">2nd subscribed to</div>
        <div class="line">Concat-->2</div>
        <div class="line">Yield 3rd sequence</div>
        <div class="line">3rd subscribed to</div>
        <div class="line">Concat-->3</div>
        <div class="line">GetSequences() complete</div>
        <div class="line">Concat completed</div>
    </div>
    <p>
        Below is a marble diagram of the <em>Concat</em> operator applied to the <code>GetSequences</code>
        method. 's1', 's2' and 's3' represent sequence 1, 2
        and 3. Respectively, 'rs' represents the result sequence.
    </p>
    <div class="marble">
        <pre class="line">s1-----1|</pre>
        <pre class="line">s2      ---2|</pre>
        <pre class="line">s3          -3|</pre>
        <pre class="line">rs-----1---2-3|</pre>
    </div>
    <p>
        You should note that the second sequence is only yielded once the first sequence
        has completed. To prove this, we explicitly put in a 500ms delay on producing a
        value and completing. Once that happens, the second sequence is then subscribed
        to. When that sequence completes, then the third sequence is processed in the same
        fashion.
    </p>
    <a name="Repeat"></a>
    <h3>Repeat</h3>
    <p>
        Another simple extension method is <em>Repeat</em>. It allows you to simply repeat
        a sequence, either a specified or an infinite number of times.
    </p>
    <pre class="csharpcode">
        // Repeats the observable sequence indefinitely and sequentially.
        public static IObservable&lt;TSource&gt; Repeat&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source)
        {...}

        //Repeats the observable sequence a specified number of times.
        public static IObservable&lt;TSource&gt; Repeat&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            int repeatCount)
        {...}
    </pre>
    <p>
        If you use the overload that loops indefinitely, then the only way the sequence
        will stop is if there is an error or the subscription is disposed of. The overload
        that specifies a repeat count will stop on error, un-subscription, or when it reaches
        that count. This example shows the sequence [0,1,2] being repeated three times.
    </p>
    <pre class="csharpcode">
        var source = Observable.Range(0, 3);
        var result = source.Repeat(3);

        result.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">Completed</div>
    </div>
    <a name="StartWith"></a>
    <h3>StartWith</h3>
    <p>
        Another simple concatenation method is the <em>StartWith</em> extension method.
        It allows you to prefix values to a sequence. The method signature takes a <code>params</code>
        array of values so it is easy to pass in as many or as few values as you need.
    </p>
    <pre class="csharpcode">
        // prefixes a sequence of values to an observable sequence.
        public static IObservable&lt;TSource&gt; StartWith&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            params TSource[] values)
        {
            ...
        }
    </pre>
    <p>
        Using <em>StartWith</em> can give a similar effect to a <em>BehaviorSubject&lt;T&gt;</em>
        by ensuring a value is provided as soon as a consumer subscribes. It is not the
        same as a <em>BehaviorSubject</em> however, as it will not cache the last value.
    </p>
    <p>
        In this example, we prefix the values -3, -2 and -1 to the sequence [0,1,2].
    </p>
    <pre class="csharpcode">
        //Generate values 0,1,2 
        var source = Observable.Range(0, 3);
        var result = source.StartWith(-3, -2, -1);
        result.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">-3</div>
        <div class="line">-2</div>
        <div class="line">-1</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">Completed</div>
    </div>
    <a name="ConcurrentSequences"></a>
    <h2>Concurrent sequences</h2>
    <p>
        The next set of methods aims to combine observable sequences that are producing
        values concurrently. This is an important step in our journey to understanding Rx.
        For the sake of simplicity, we have avoided introducing concepts related to concurrency
        until we had a broad understanding of the simple concepts.
    </p>
    <a name="Amb"></a>
    <h3>Amb</h3>
    <p>
        The <em>Amb</em> method was a new concept to me when I started using Rx. It is a
        non-deterministic function, first introduced by John McCarthy and is an abbreviation
        of the word <i>Ambiguous</i>. The Rx implementation will return values from the sequence
        that is first to produce values, and will completely ignore the other sequences.
        In the examples below I have three sequences that all produce values. The sequences
        can be represented as the marble diagram below.
    </p>
    <div class="marble">
        <pre class="line">s1 -1--1--|</pre>
        <pre class="line">s2 --2--2--|</pre>
        <pre class="line">s3 ---3--3--|</pre>
        <pre class="line">r  -1--1--|</pre>
    </div>
    <p>
        The code to produce the above is as follows.
    </p>
    <pre class="csharpcode">
        var s1 = new Subject&lt;int&gt;();
        var s2 = new Subject&lt;int&gt;();
        var s3 = new Subject&lt;int&gt;();

        var result = Observable.Amb(s1, s2, s3);

        result.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));

        s1.OnNext(1);
        s2.OnNext(2);
        s3.OnNext(3);
        s1.OnNext(1);
        s2.OnNext(2);
        s3.OnNext(3);
        s1.OnCompleted();
        s2.OnCompleted();
        s3.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">1</div>
        <div class="line">Completed</div>
    </div>
    <p>
        If we comment out the first <code>s1.OnNext(1);</code> then s2 would produce values
        first and the marble diagram would look like this.
    </p>
    <div class="marble">
        <pre class="line">s1 ---1--|</pre>
        <pre class="line">s2 -2--2--|</pre>
        <pre class="line">s3 --3--3--|</pre>
        <pre class="line">r  -2--2--|</pre>
    </div>
    <p>
        The <em>Amb</em> feature can be useful if you have multiple cheap resources that
        can provide values, but latency is widely variable. For an example, you may have
        servers replicated around the world. Issuing a query is cheap for both the client
        to send and for the server to respond, however due to network conditions the latency
        is not predictable and varies considerably. Using the <em>Amb</em> operator, you can send
        the same request out to many servers and consume the result of the first that
        responds.
    </p>
    <p>
        There are other useful variants of the <em>Amb</em> method. We have used the overload
        that takes a <code>params</code> array of sequences. You could alternatively use
        it as an extension method and chain calls until you have included all the target
        sequences (e.g. s1.Amb(s2).Amb(s3)). Finally, you could pass in an <em>IEnumerable&lt;IObservable&lt;T&gt;&gt;</em>.
    </p>
    <pre class="csharpcode">
        // Propagates the observable sequence that reacts first.
        public static IObservable&lt;TSource&gt; Amb&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; first, 
            IObservable&lt;TSource&gt; second)
        {...}
        public static IObservable&lt;TSource&gt; Amb&lt;TSource&gt;(
            params IObservable&lt;TSource&gt;[] sources)
        {...}
        public static IObservable&lt;TSource&gt; Amb&lt;TSource&gt;(
            this IEnumerable&lt;IObservable&lt;TSource&gt;&gt; sources)
        {...}
    </pre>
    <p>
        Reusing the <code>GetSequences</code> method from the <em>Concat</em> section, we
        see that the evaluation of the outer (IEnumerable) sequence is eager.
    </p>
    <pre class="csharpcode">
        GetSequences().Amb().Dump("Amb");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">GetSequences() called</div>
        <div class="line">Yield 1st sequence</div>
        <div class="line">Yield 2nd sequence</div>
        <div class="line">Yield 3rd sequence</div>
        <div class="line">GetSequences() complete</div>
        <div class="line">1st subscribed to</div>
        <div class="line">2nd subscribed to</div>
        <div class="line">3rd subscribed to</div>
        <div class="line">Amb-->3</div>
        <div class="line">Amb completed</div>
    </div>
    <p>
        Marble:
    </p>
    <div class="marble">
        <pre class="line">s1-----1|</pre>
        <pre class="line">s2---2|</pre>
        <pre class="line">s3-3|</pre>
        <pre class="line">rs-3|</pre>
    </div>
    <p>
        Take note that the inner observable sequences are not subscribed to until the outer
        sequence has yielded them all. This means that the third sequence is able to return
        values the fastest even though there are two sequences yielded one second before it (due to the <code>Thread.Sleep</code>).
    </p>
    <a name="Merge"></a>
    <h3>Merge</h3>
    <p>
        The <em>Merge</em> extension method does a primitive combination of multiple concurrent
        sequences. As values from any sequence are produced, those values become part of
        the result sequence. All sequences need to be of the same type, as per the previous
        methods. In this diagram, we can see <em>s1</em> and <em>s2</em> producing values
        concurrently and the values falling through to the result sequence as they occur.
    </p>
    <div class="marble">
        <pre class="line">s1 --1--1--1--|</pre>
        <pre class="line">s2 ---2---2---2|</pre>
        <pre class="line">r  --12-1-21--2|</pre>
    </div>
    <p>
        The result of a <em>Merge</em> will complete only once all input sequences complete.
        By contrast, the <em>Merge</em> operator will error if any of the input sequences
        terminates erroneously.
    </p>
    <pre class="csharpcode">
        //Generate values 0,1,2 
        var s1 = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .Take(3);
        //Generate values 100,101,102,103,104 
        var s2 = Observable.Interval(TimeSpan.FromMilliseconds(150))
            .Take(5)
            .Select(i =&gt; i + 100);
        s1.Merge(s2)
            .Subscribe(
                Console.WriteLine,
                ()=&gt;Console.WriteLine("Completed"));
    </pre>
    <p>
        The code above could be represented by the marble diagram below. In this case, each
        unit of time is 50ms. As both sequences produce a value at 750ms, there is a race
        condition and we cannot be sure which value will be notified first in the result
        sequence (sR).
    </p>
    <div class="marble">
        <pre class="line">s1 ----0----0----0| </pre>
        <pre class="line">s2 --0--0--0--0--0|</pre>
        <pre class="line">sR --0-00--00-0--00|</pre>
    </div>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">100</div>
        <div class="line">0</div>
        <div class="line">101</div>
        <div class="line">102</div>
        <div class="line">1</div>
        <div class="line">103</div>
        <div class="line">104 //Note this is a race condition. 2 could be </div>
        <div class="line">2 // published before 104. </div>
    </div>
    <p>
        You can chain this overload of the <em>Merge</em> operator to merge multiple sequences.
        <em>Merge</em> also provides numerous other overloads that allow you to pass more
        than two source sequences. You can use the static method <em>Observable.Merge</em>
        which takes a <code>params</code> array of sequences that is known at compile time.
        You could pass in an <em>IEnumerable</em> of sequences like the <em>Concat</em>
        method. <em>Merge</em> also has the overload that takes an <em>IObservable&lt;IObservable&lt;T&gt;&gt;</em>,
        a nested observable. To summarize:
    </p>
    <ul>
        <li>Chain <em>Merge</em> operators together e.g. <code>s1.Merge(s2).Merge(s3)</code></li>
        <li>Pass a <code>params</code> array of sequences to the <em>Observable.Merge</em> static
            method. e.g. <code>Observable.Merge(s1,s2,s3)</code></li>
        <li>Apply the <em>Merge</em> operator to an <em>IEnumerable&lt;IObservable&lt;T&gt;&gt;</em>.</li>
        <li>Apply the <em>Merge</em> operator to an <em>IObservable&lt;IObservable&lt;T&gt;&gt;</em>.</li>
    </ul>
    <pre class="csharpcode">
        /// Merges two observable sequences into a single observable sequence.
        /// Returns a sequence that merges the elements of the given sequences.
        public static IObservable&lt;TSource&gt; Merge&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; first, 
            IObservable&lt;TSource&gt; second)
        {...}
        
        // Merges all the observable sequences into a single observable sequence.
        // The observable sequence that merges the elements of the observable sequences.
        public static IObservable&lt;TSource&gt; Merge&lt;TSource&gt;(
            params IObservable&lt;TSource&gt;[] sources)
        {...}
        
        // Merges an enumerable sequence of observable sequences into a single observable sequence.
        public static IObservable&lt;TSource&gt; Merge&lt;TSource&gt;(
            this IEnumerable&lt;IObservable&lt;TSource&gt;&gt; sources)
        {...}
        
        // Merges an observable sequence of observable sequences into an observable sequence.
        // Merges all the elements of the inner sequences in to the output sequence.
        public static IObservable&lt;TSource&gt; Merge&lt;TSource&gt;(
            this IObservable&lt;IObservable&lt;TSource&gt;&gt; sources)
        {...}
    </pre>
    <p>
        For merging a known number of sequences, the first two operators are effectively
        the same thing and which style you use is a matter of taste: either provide them
        as a <code>params</code> array or chain the operators together. The third and fourth
        overloads allow to you merge sequences that can be evaluated lazily at run time.
        The <em>Merge</em> operators that take a sequence of sequences make for an interesting
        concept. You can either pull or be pushed observable sequences, which will be subscribed
        to immediately.
    </p>
    <p>
        If we again reuse the <code>GetSequences</code> method, we can see how the <em>Merge</em>
        operator works with a sequence of sequences.
    </p>
    <pre class="csharpcode">
        GetSequences().Merge().Dump("Merge");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">GetSequences() called</div>
        <div class="line">Yield 1st sequence</div>
        <div class="line">1st subscribed to</div>
        <div class="line">Yield 2nd sequence</div>
        <div class="line">2nd subscribed to</div>
        <div class="line">Merge-->2</div>
        <div class="line">Merge-->1</div>
        <div class="line">Yield 3rd sequence</div>
        <div class="line">3rd subscribed to</div>
        <div class="line">GetSequences() complete</div>
        <div class="line">Merge-->3</div>
        <div class="line">Merge completed</div>
    </div>
    <p>
        As we can see from the marble diagram, s1 and s2 are yielded and subscribed to immediately.
        s3 is not yielded for one second and then is subscribed to. Once all input sequences
        have completed, the result sequence completes.
    </p>
    <div class="marble">
        <pre class="line">s1-----1|</pre>
        <pre class="line">s2---2|</pre>
        <pre class="line">s3          -3|</pre>
        <pre class="line">rs---2-1-----3|</pre>
    </div>
    <a name="Switch"></a>
    <h3>Switch</h3>
    <p>
        Receiving all values from a nested observable sequence is not always what you need.
        In some scenarios, instead of receiving everything, you may only want the values
        from the most recent inner sequence. A great example of this is live searches. As
        you type, the text is sent to a search service and the results are returned to you
        as an observable sequence. Most implementations have a slight delay before sending
        the request so that unnecessary work does not happen. Imagine I want to search for
        "Intro to Rx". I quickly type in "Into to" and realize I have missed the letter
        'r'. I stop briefly and change the text to "Intro ". By now, two searches have been
        sent to the server. The first search will return results that I do not want. Furthermore,
        if I were to receive data for the first search merged together with results for
        the second search, it would be a very odd experience for the user. This scenario
        fits perfectly with the <em>Switch</em> method.
    </p>
    <p>
        In this example, there is a source that represents a sequence of search text.
        <!--When
        the user types in a new value, the source sequence OnNext's the value-->
        Values the user types are represented as the source sequence. Using <em>Select</em>,
        we pass the value of the search to a function that takes a <code>string</code> and
        returns an <em>IObservable&lt;string&gt;</em>. This creates our resulting nested
        sequence, <em>IObservable&lt;IObservable&lt;string&gt;&gt;</em>.
    </p>
    <p>
        Search function signature:
    </p>
    <pre class="csharpcode">    
        private IObservable&lt;string&gt; SearchResults(string query)
        {
            ...
        }
    </pre>
    <p>
        Using <em>Merge</em> with overlapping search:
    </p>
    <pre class="csharpcode">
        IObservable&lt;string&gt; searchValues = ....;
        IObservable&lt;IObservable&lt;string&gt;&gt; search = searchValues
            .Select(searchText=&gt;SearchResults(searchText));
                         
        var subscription = search
            .Merge()
            .Subscribe(
                Console.WriteLine);
    </pre>
    <!--TODO: Show output here-->
    <p>
        If we were lucky and each search completed before the next element from <code>searchValues</code>
        was produced, the output would look sensible. It is much more likely, however that
        multiple searches will result in overlapped search results. This marble diagram
        shows what the <em>Merge</em> function could do in such a situation.
    </p>
    <ul>
        <li><em>SV</em> is the searchValues sequence</li>
        <li><em>S1</em> is the search result sequence for the first value in searchValues/SV</li>
        <li><em>S2</em> is the search result sequence for the second value in searchValues/SV</li>
        <li><em>S3</em> is the search result sequence for the third value in searchValues/SV</li>
        <li><em>RM</em> is the result sequence for the merged (<em>R</em>esult <em>M</em>erge)
            sequences</li>
    </ul>
    <div class="marble">
        <pre class="line">SV--1---2---3---|</pre>
        <pre class="line">S1  -1--1--1--1|</pre>
        <pre class="line">S2      --2-2--2--2|</pre>
        <pre class="line">S3          -3--3|</pre>
        <pre class="line">RM---1--1-2123123-2|</pre>
    </div>
    <p>
        Note how the values from the search results are all mixed together. This is not
        what we want. If we use the <em>Switch</em> extension method we will get much better
        results. <em>Switch</em> will subscribe to the outer sequence and as each inner
        sequence is yielded it will subscribe to the new inner sequence and dispose of the
        subscription to the previous inner sequence. This will result in the following marble
        diagram where <em>RS</em> is the result sequence for the Switch (<em>R</em>esult
        <em>S</em>witch) sequences
    </p>
    <div class="marble">
        <pre class="line">SV--1---2---3---|</pre>
        <pre class="line">S1  -1--1--1--1|</pre>
        <pre class="line">S2      --2-2--2--2|</pre>
        <pre class="line">S3          -3--3|</pre>
        <pre class="line">RS --1--1-2-23--3|</pre>
    </div>
    <p>
        Also note that, even though the results from S1 and S2 are still being pushed, they
        are ignored as their subscription has been disposed of. This eliminates the issue
        of overlapping values from the nested sequences.
    </p>
    <a name="ParingSequences"></a>
    <h2>Pairing sequences</h2>
    <p>
        The previous methods allowed us to flatten multiple sequences sharing a common type
        into a result sequence of the same type. These next sets of methods still take multiple
        sequences as an input, but attempt to pair values from each sequence to produce
        a single value for the output sequence. In some cases, they also allow you to provide
        sequences of different types.
    </p>
    <a name="CombineLatest"></a>
    <h3>CombineLatest</h3>
    <p>
        The <em>CombineLatest</em> extension method allows you to take the most recent value
        from two sequences, and with a given function transform those into a value for the
        result sequence. Each input sequence has the last value cached like <code>Replay(1)</code>.
        Once both sequences have produced at least one value, the latest output from each
        sequence is passed to the <code>resultSelector</code> function every time either
        sequence produces a value. The signature is as follows.
    </p>
    <pre class="csharpcode">
    // Composes two observable sequences into one observable sequence by using the selector 
    //  function whenever one of the observable sequences produces an element.
    public static IObservable&lt;TResult&gt; CombineLatest&lt;TFirst, TSecond, TResult&gt;(
        this IObservable&lt;TFirst&gt; first, 
        IObservable&lt;TSecond&gt; second, 
        Func&lt;TFirst, TSecond, TResult&gt; resultSelector)
    {...}
    </pre>
    <p>
        The marble diagram below shows off usage of <em>CombineLatest</em> with one sequence
        that produces numbers (N), and the other letters (L). If the <code>resultSelector</code>
        function just joins the number and letter together as a pair, this would be the result
        (R):
    </p>
    <div class="marble">
        <pre class="line">N---1---2---3---</pre>
        <pre class="line">L--a------bc----</pre>
        <pre class="line">                </pre>
        <pre class="line">R---1---2-223---</pre>
        <pre class="line">    a   a bcc   </pre>
    </div>
    <p>
        If we slowly walk through the above marble diagram, we first see that <code>L</code>
        produces the letter 'a'. <code>N</code> has not produced any value yet so there
        is nothing to pair, no value is produced for the result (R). Next, <code>N</code>
        produces the number '1' so we now have a pair '1a' that is yielded in the result
        sequence. We then receive the number '2' from <code>N</code>. The last letter is
        still 'a' so the next pair is '2a'. The letter 'b' is then produced creating the
        pair '2b', followed by 'c' giving '2c'. Finally the number 3 is produced and we
        get the pair '3c'.
    </p>
    <p>
        This is great in case you need to evaluate some combination of state which needs
        to be kept up-to-date when the state changes. A simple example would be a monitoring
        system. Each service is represented by a sequence that returns a Boolean indicating
        the availability of said service. The monitoring status is green if all services
        are available; we can achieve this by having the result selector perform a logical
        AND. Here is an example.
    </p>
    <pre class="csharpcode">
        IObservable&lt;bool&gt; webServerStatus = GetWebStatus();
        IObservable&lt;bool&gt; databaseStatus = GetDBStatus();
        //Yields true when both systems are up.
        var systemStatus = webServerStatus
            .CombineLatest(
                databaseStatus,
                (webStatus, dbStatus) =&gt; webStatus &amp;&amp; dbStatus);
    </pre>
    <p>
        Some readers may have noticed that this method could produce a lot of duplicate
        values. For example, if the web server goes down the result sequence will yield
        '<code>false</code>'. If the database then goes down, another (unnecessary) '<code>false</code>'
        value will be yielded. This would be an appropriate time to use the <em>DistictUntilChanged</em>
        extension method. The corrected code would look like the example below.
    </p>
    <pre class="csharpcode">
        //Yields true when both systems are up, and only on change of status
        var systemStatus = webServerStatus
            .CombineLatest(
                databaseStatus,
                (webStatus, dbStatus) =&gt; webStatus &amp;&amp; dbStatus)
            .DistinctUntilChanged();
    </pre>
    <p>
        To provide an even better service, we could provide a default value by prefixing
        <code>false</code> to the sequence.
    </p>
    <pre class="csharpcode">
        //Yields true when both systems are up, and only on change of status
        var systemStatus = webServerStatus
            .CombineLatest(
                databaseStatus,
                (webStatus, dbStatus) =&gt; webStatus &amp;&amp; dbStatus)
            .DistinctUntilChanged()
            .StartWith(false);

    </pre>
    <a name="Zip"></a>
    <h3>Zip</h3>
    <p>
        The <em>Zip</em> extension method is another interesting merge feature. Just like
        a zipper on clothing or a bag, the <em>Zip</em> method brings together two sequences
        of values as pairs; two by two. Things to note about the <em>Zip</em> function is
        that the result sequence will complete when the first of the sequences complete,
        it will error if either of the sequences error and it will only publish once it
        has a pair of fresh values from each source sequence. So if one of the source sequences
        publishes values faster than the other sequence, the rate of publishing will be
        dictated by the slower of the two sequences.
    </p>
    <pre class="csharpcode">
        //Generate values 0,1,2 
        var nums = Observable.Interval(TimeSpan.FromMilliseconds(250))
            .Take(3);
        //Generate values a,b,c,d,e,f 
        var chars = Observable.Interval(TimeSpan.FromMilliseconds(150))
            .Take(6)
            .Select(i =&gt; Char.ConvertFromUtf32((int)i + 97));
        //Zip values together
        nums.Zip(chars, (lhs, rhs) =&gt; new { Left = lhs, Right = rhs })
            .Dump("Zip");
    </pre>
    <p>
        This can be seen in the marble diagram below. Note that the result uses
        two lines so that we can represent a complex type, i.e. the anonymous type with
        the properties Left and Right.
    </p>
    <div class="marble">
        <pre class="line">nums  ----0----1----2| </pre>
        <pre class="line">chars --a--b--c--d--e--f| </pre>
        <pre class="line">                        </pre>
        <pre class="line">result----0----1----2|</pre>
        <pre class="line">          a    b    c|</pre>
    </div>
    <p>
        The actual output of the code:
    </p>
    <div class="output">
        <div class="line">{ Left = 0, Right = a }</div>
        <div class="line">{ Left = 1, Right = b }</div>
        <div class="line">{ Left = 2, Right = c }</div>
    </div>
    <p>
        Note that the <code>nums</code> sequence only produced three values before completing,
        while the <code>chars</code> sequence produced six values. The result sequence thus
        has three values, as this was the most pairs that could be made.
    </p>
    <p>
        The first use I saw of <em>Zip</em> was to showcase drag and drop. <a href="http://channel9.msdn.com/Blogs/J.Van.Gogh/Writing-your-first-Rx-Application">
            The example</a> tracked mouse movements from a <code>MouseMove</code> event
        that would produce event arguments with its current X,Y coordinates. First, the
        example turns the event into an observable sequence. Then they cleverly zipped the
        sequence with a <code>Skip(1)</code> version of the same sequence. This allows the
        code to get a delta of the mouse position, i.e. where it is now (sequence.Skip(1))
        minus where it was (sequence). It then applied the delta to the control it was dragging.
    </p>
    <p>
        To visualize the concept, let us look at another marble diagram. Here we have the
        mouse movement (MM) and the Skip 1 (S1). The numbers represent the index of the mouse
        movement.
    </p>
    <div class="marble">
        <pre class="line">MM --1--2--3--4--5</pre>
        <pre class="line">S1    --2--3--4--5</pre>
        <pre class="line">                  </pre>
        <pre class="line">Zip   --1--2--3--4</pre>
        <pre class="line">        2  3  4  5</pre>
    </div>
    <p>
        Here is a code sample where we fake out some mouse movements with our own subject.
    </p>
    <pre class="csharpcode">
        var mm = new Subject&lt;Coord&gt;();
        var s1 = mm.Skip(1);

        var delta = mm.Zip(s1,
                            (prev, curr) =&gt; new Coord
                                {
                                    X = curr.X - prev.X,
                                    Y = curr.Y - prev.Y
                                });

        delta.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));

        mm.OnNext(new Coord { X = 0, Y = 0 });
        mm.OnNext(new Coord { X = 1, Y = 0 }); //Move across 1
        mm.OnNext(new Coord { X = 3, Y = 2 }); //Diagonally up 2
        mm.OnNext(new Coord { X = 0, Y = 0 }); //Back to 0,0
        mm.OnCompleted();
    </pre>
    <p>
        This is the simple Coord(inate) class we use.
    </p>
    <pre class="csharpcode">
        public class Coord
        {
            public int X { get; set; }
            public int Y { get; set; }
            public override string ToString()
            {
                return string.Format("{0},{1}", X, Y);
            }
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0,1</div>
        <div class="line">2,2</div>
        <div class="line">-3,-2</div>
        <div class="line">Completed</div>
    </div>
    <p>
        It is also worth noting that <em>Zip</em> has a second overload that takes an <em>IEnumerable&lt;T&gt;</em>
        as the second input sequence.
    </p>
    <pre class="csharpcode">
        // Merges an observable sequence and an enumerable sequence into one observable sequence 
        //  containing the result of pair-wise combining the elements by using the selector function.
        public static IObservable&lt;TResult&gt; Zip&lt;TFirst, TSecond, TResult&gt;(
            this IObservable&lt;TFirst&gt; first, 
            IEnumerable&lt;TSecond&gt; second, 
            Func&lt;TFirst, TSecond, TResult&gt; resultSelector)
        {...}
    </pre>
    <p>
        This allows us to zip sequences from both <em>IEnumerable&lt;T&gt;</em> and <em>IObservable&lt;T&gt;</em>
        paradigms!
    </p>
    <a name="AndThenWhen"></a>
    <h3>And-Then-When</h3>
    <p>
        If <em>Zip</em> only taking two sequences as an input is a problem, then you can
        use a combination of the three <em>And</em>/<em>Then</em>/<em>When</em> methods.
        These methods are used slightly differently from most of the other Rx methods. Out
        of these three, <em>And</em> is the only extension method to <em>IObservable&lt;T&gt;</em>.
        Unlike most Rx operators, it does not return a sequence; instead, it returns the
        mysterious type <em>Pattern&lt;T1, T2&gt;</em>. The <em>Pattern&lt;T1, T2&gt;</em>
        type is public (obviously), but all of its properties are internal. The only two
        (useful) things you can do with a <em>Pattern&lt;T1, T2&gt;</em> are invoking its
        <em>And</em> or <em>Then</em> methods. The <em>And</em> method called on the <em>Pattern&lt;T1,
            T2&gt;</em> returns a <em>Pattern&lt;T1, T2, T3&gt;</em>. On that type, you
        will also find the <em>And</em> and <em>Then</em> methods. The generic <em>Pattern</em>
        types are there to allow you to chain multiple <em>And</em> methods together, each
        one extending the generic type parameter list by one. You then bring them all together
        with the <em>Then</em> method overloads. The <em>Then</em> methods return you a
        <em>Plan</em> type. Finally, you pass this <em>Plan</em> to the <em>Observable.When</em>
        method in order to create your sequence.
    </p>
    <p>
        It may sound very complex, but comparing some code samples should make it easier to
        understand. It will also allow you to see which style you prefer to use.
    </p>
    <p>
        To <em>Zip</em> three sequences together, you can either use <em>Zip</em> methods
        chained together like this:
    </p>
    <pre class="csharpcode">
        var one = Observable.Interval(TimeSpan.FromSeconds(1)).Take(5);
        var two = Observable.Interval(TimeSpan.FromMilliseconds(250)).Take(10);
        var three = Observable.Interval(TimeSpan.FromMilliseconds(150)).Take(14);
        
        //lhs represents 'Left Hand Side'
        //rhs represents 'Right Hand Side'
        var zippedSequence = one
            .Zip(two, (lhs, rhs) =&gt; new {One = lhs, Two = rhs})
            .Zip(three, (lhs, rhs) =&gt; new {One = lhs.One, Two = lhs.Two, Three = rhs});

        zippedSequence.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Or perhaps use the nicer syntax of the <em>And</em>/<em>Then</em>/<em>When</em>:
    </p>
    <pre class="csharpcode">
        var pattern = one.And(two).And(three);
        var plan = pattern.Then((first, second, third)=&gt;new{One=first, Two=second, Three=third});
        var zippedSequence = Observable.When(plan);

        zippedSequence.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        This can be further reduced, if you prefer, to:
    </p>
    <pre class="csharpcode">
        var zippedSequence = Observable.When(
                one.And(two)
                   .And(three)
                   .Then((first, second, third) =&gt; 
                        new { 
                            One = first, 
                            Two = second, 
                            Three = third 
                        })
                );

        zippedSequence.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        The <em>And</em>/<em>Then</em>/<em>When</em> trio has more overloads that enable
        you to group an even greater number of sequences. They also allow you to provide
        more than one 'plan' (the output of the <em>Then</em> method). This gives you the
        <em>Merge</em> feature but on the collection of 'plans'. I would suggest playing
        around with them if this functionality is of interest to you. The verbosity of 
        enumerating all of the combinations of these methods would be of low value.
        You will get far more value out of using them and discovering for yourself.
    </p>
    <p>
        As we delve deeper into the depths of what the Rx libraries provide us, we can see
        more practical usages for it. Composing sequences with Rx allows us to easily make
        sense of the multiple data sources a problem domain is exposed to. We can concatenate
        values or sequences together sequentially with <em>StartWith</em>, <em>Concat</em>
        and <em>Repeat</em>. We can process multiple sequences concurrently with <em>Merge</em>,
        or process a single sequence at a time with <em>Amb</em> and <em>Switch</em>. Pairing
        values with <em>CombineLatest</em>, <em>Zip</em> and the <em>And</em>/<em>Then</em>/<em>When</em>
        operators can simplify otherwise fiddly operations like our drag-and-drop examples
        and monitoring system status.
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
                <!--Essential Linq Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B001XT616O&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
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
