<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Transformation of sequences</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="TransformationOfSequences"></a>
    <h1>Transformation of sequences</h1>
    <p>
        The values from the sequences we consume are not always in the format we need. Sometimes
        there is too much noise in the data so we strip the values down. Sometimes each
        value needs to be expanded either into a richer object or into more values. By composing
        operators, Rx allows you to control the quality as well as the quantity of values
        in the observable sequences you consume.
    </p>
    <p>
        Up until now, we have looked at creation of sequences, transition into sequences,
        and, the reduction of sequences by filtering, aggregating or folding. In this chapter
        we will look at <i>transforming</i> sequences. This allows us to introduce our third
        category of functional methods, <i>bind</i>. A bind function in Rx will take a sequence
        and apply some set of transformations on each element to produce a new sequence.
        To review:
    </p>
    <p class="centered">
        <strong>Ana(morphism) T --> IObservable&lt;T&gt;</strong>
    </p>
    <p class="centered">
        <strong>Cata(morphism) IObservable&lt;T&gt; --> T</strong>
    </p>
    <p class="centered">
        <strong>Bind IObservable&lt;T1&gt; --> IObservable&lt;T2&gt;</strong>
    </p>
    <p>
        Now that we have been introduced to all three of our higher order functions, you
        may find that you already know them. Bind and Cata(morphism) were made famous by
        <a href="http://en.wikipedia.org/wiki/MapReduce">MapReduce</a> framework from Google.
        Here Google refer to Bind and Cata by their perhaps more common aliases; Map and
        Reduce.
    </p>
    <p>
        It may help to remember our terms as the <em>ABCs</em> of higher order functions.
    </p>
    <p>
        <strong>A</strong>na enters the sequence. T --> IObservable&lt;T&gt;
    </p>
    <p>
        <strong>B</strong>ind modifies the sequence. IObservable&lt;T1&gt; --> IObservable&lt;T2&gt;
    </p>
    <p>
        <strong>C</strong>ata leaves the sequence. IObservable&lt;T&gt; --> T
    </p>
    <a name="Select"></a>
    <h2>Select</h2>
    <p>
        The classic transformation method is <em>Select</em>. It allows you provide a function
        that takes a value of <code>TSource</code> and return a value of <code>TResult</code>.
        The signature for <em>Select</em> is nice and simple and suggests that its most
        common usage is to transform from one type to another type, i.e. <em>IObservable&lt;TSource&gt;</em>
        to <em>IObservable&lt;TResult&gt;</em>.
    </p>
    <pre class="csharpcode">
        IObservable&lt;TResult&gt; Select&lt;TSource, TResult&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TResult&gt; selector)
    </pre>
    <p>
        Note that there is no restriction that prevents <code>TSource</code> and <code>TResult</code>
        being the same thing. So for our first example, we will take a sequence of integers
        and transform each value by adding 3, resulting in another sequence of integers.
    </p>
    <pre class="csharpcode">
        var source = Observable.Range(0, 5);
        source.Select(i=&gt;i+3)
            .Dump("+3")
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">+3--&gt;3</div>
        <div class="line">+3--&gt;4</div>
        <div class="line">+3--&gt;5</div>
        <div class="line">+3--&gt;6</div>
        <div class="line">+3--&gt;7</div>
        <div class="line">+3 completed</div>
    </div>
    <p>
        While this can be useful, more common use is to transform values from one type to
        another. In this example we transform integer values to characters.
    </p>
    <pre class="csharpcode">
        Observable.Range(1, 5);
            .Select(i =&gt;(char)(i + 64))
            .Dump("char");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">char--&gt;A</div>
        <div class="line">char--&gt;B</div>
        <div class="line">char--&gt;C</div>
        <div class="line">char--&gt;D</div>
        <div class="line">char--&gt;E</div>
        <div class="line">char completed</div>
    </div>
    <p>
        If we really want to take advantage of LINQ we could transform our sequence of integers
        to a sequence of anonymous types.
    </p>
    <pre class="csharpcode">
        Observable.Range(1, 5)
            .Select(
                i =&gt; new { Number = i, Character = (char)(i + 64) })
            .Dump("anon");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">anon--&gt;{ Number = 1, Character = A }</div>
        <div class="line">anon--&gt;{ Number = 2, Character = B }</div>
        <div class="line">anon--&gt;{ Number = 3, Character = C }</div>
        <div class="line">anon--&gt;{ Number = 4, Character = D }</div>
        <div class="line">anon--&gt;{ Number = 5, Character = E }</div>
        <div class="line">anon completed</div>
    </div>
    <p>
        To further leverage LINQ we could write the above query using <a href="http://www.albahari.com/nutshell/linqsyntax.aspx">
            query comprehension syntax</a>.
    </p>
    <pre class="csharpcode">
        var query = from i in Observable.Range(1, 5)
                    select new {Number = i, Character = (char) (i + 64)};
        query.Dump("anon");
    </pre>
    <p>
        In Rx, <em>Select</em> has another overload. The second overload provides two values
        to the <code>selector</code> function. The additional argument is the element's
        index in the sequence. Use this method if the index of the element in the sequence
        is important to your selector function.
    </p>
    <a name="CastAndOfType"></a>
    <h2>Cast and OfType</h2>
    <p>
        If you were to get a sequence of objects i.e. <em>IObservable&lt;object&gt;</em>,
        you may find it less than useful. There is a method specifically for <em>IObservable&lt;object&gt;</em>
        that will cast each element to a given type, and logically it is called <em>Cast&lt;T&gt;()</em>.
    </p>
    <pre class="csharpcode">
        var objects = new Subject&lt;object&gt;();
        objects.Cast&lt;int&gt;().Dump("cast");
        objects.OnNext(1);
        objects.OnNext(2);
        objects.OnNext(3);
        objects.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">cast--&gt;1</div>
        <div class="line">cast--&gt;2</div>
        <div class="line">cast--&gt;3</div>
        <div class="line">cast completed</div>
    </div>
    <p>
        If however we were to add a value that could not be cast into the sequence then
        we get errors.
    </p>
    <pre class="csharpcode">
        var objects = new Subject&lt;object&gt;();
        objects.Cast&lt;int&gt;().Dump("cast");
        objects.OnNext(1);
        objects.OnNext(2);
        objects.OnNext("3");//Fail
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">cast--&gt;1</div>
        <div class="line">cast--&gt;2</div>
        <div class="line">cast failed --&gt;Specified cast is not valid.</div>
    </div>
    <p>
        Thankfully, if this is not what we want, we could use the alternative extension
        method <em>OfType&lt;T&gt;()</em>.
    </p>
    <pre class="csharpcode">
        var objects = new Subject&lt;object&gt;();
        objects.OfType&lt;int&gt;().Dump("OfType");
        objects.OnNext(1);
        objects.OnNext(2);
        objects.OnNext("3");//Ignored
        objects.OnNext(4);
        objects.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">OfType--&gt;1</div>
        <div class="line">OfType--&gt;2</div>
        <div class="line">OfType--&gt;4</div>
        <div class="line">OfType completed</div>
    </div>
    <p>
        It is fair to say that while these are convenient methods to have, we could have
        created them with the operators we already know about.
    </p>
    <pre class="csharpcode">
        //source.Cast&lt;int&gt;(); is equivalent to
        source.Select(i=>(int)i);
        
        //source.OfType&lt;int&gt;();
        source.Where(i=&gt;i is int).Select(i=&gt;(int)i);
    </pre>
    <a name="TimeStampAndTimeInterval"></a>
    <h2>Timestamp and TimeInterval</h2>
    <p>
        As observable sequences are asynchronous it can be convenient to know timings for
        when elements are received. The <em>Timestamp</em> extension method is a handy convenience
        method that wraps elements of a sequence in a light weight <em>Timestamped&lt;T&gt;</em>
        structure. The <em>Timestamped&lt;T&gt;</em> type is a struct that exposes the value
        of the element it wraps, and the timestamp it was created with as a <em>DateTimeOffset</em>.
    </p>
    <p>
        In this example we create a sequence of three values, one second apart, and then
        transform it to a time stamped sequence. The handy implementation of <code>ToString()</code>
        on <em>Timestamped&lt;T&gt;</em> gives us a readable output.
    </p>
    <pre class="csharpcode">
        Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(3)
            .Timestamp()
            .Dump("TimeStamp");
    </pre>
    <p>
        Output
    </p>
    <div class="output">
        <div class="line">TimeStamp--&gt;0@01/01/2012 12:00:01 a.m. +00:00</div>
        <div class="line">TimeStamp--&gt;1@01/01/2012 12:00:02 a.m. +00:00</div>
        <div class="line">TimeStamp--&gt;2@01/01/2012 12:00:03 a.m. +00:00</div>
        <div class="line">TimeStamp completed</div>
    </div>
    <p>
        We can see that the values 0, 1 &amp; 2 were each produced one second apart. An
        alternative to getting an absolute timestamp is to just get the interval since the
        last element. The <em>TimeInterval</em> extension method provides this. As per the
        <em>Timestamp</em> method, elements are wrapped in a light weight structure. This
        time the structure is the <em>TimeInterval&lt;T&gt;</em> type.
    </p>
    <pre class="csharpcode">
        Observable.Interval(TimeSpan.FromSeconds(1))
                .Take(3)
                .TimeInterval()
                .Dump("TimeInterval");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">TimeInterval--&gt;0@00:00:01.0180000</div>
        <div class="line">TimeInterval--&gt;1@00:00:01.0010000</div>
        <div class="line">TimeInterval--&gt;2@00:00:00.9980000</div>
        <div class="line">TimeInterval completed</div>
    </div>
    <p>
        As you can see from the output, the timings are not exactly one second but are pretty
        close.
    </p>
    <a name="MaterializeAndDematerialize"></a>
    <h2>Materialize and Dematerialize</h2>
    <p>
        The <em>Timestamp</em> and <em>TimeInterval</em> transform operators can prove useful
        for logging and debugging sequences, so too can the <em>Materialize</em> operator.
        <em>Materialize</em> transitions a sequence into a metadata representation of the
        sequence, taking an <em>IObservable&lt;T&gt;</em> to an <em>IObservable&lt;Notification&lt;T&gt;&gt;</em>.
        The <em>Notification</em> type provides meta data for the events of the sequence.
    </p>
    <p>
        If we materialize a sequence, we can see the wrapped values being returned.
    </p>
    <pre class="csharpcode">
        Observable.Range(1, 3)
            .Materialize()
            .Dump("Materialize");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Materialize--&gt;OnNext(1)</div>
        <div class="line">Materialize--&gt;OnNext(2)</div>
        <div class="line">Materialize--&gt;OnNext(3)</div>
        <div class="line">Materialize--&gt;OnCompleted()</div>
        <div class="line">Materialize completed</div>
    </div>
    <p>
        Note that when the source sequence completes, the materialized sequence produces
        an 'OnCompleted' notification value and then completes. <em>Notification&lt;T&gt;</em>
        is an abstract class with three implementations:
    </p>
    <ul>
        <li>OnNextNotification</li>
        <li>OnErrorNotification</li>
        <li>OnCompletedNotification</li>
    </ul>
    <p>
        <em>Notification&lt;T&gt;</em> exposes four public properties to help you discover
        it: <code>Kind</code>, <code>HasValue</code>, <code>Value</code> and <code>Exception</code>.
        Obviously only <em>OnNextNotification</em> will return true for <code>HasValue</code>
        and have a useful implementation of <code>Value</code>. It should also be obvious
        that <code>OnErrorNotification</code> is the only implementation that will have
        a value for <code>Exception</code>. The <code>Kind</code> property returns an <code>
            enum</code> which should allow you to know which methods are appropriate to
        use.
    </p>
    <pre class="csharpcode">
        public enum NotificationKind
        {
            OnNext,
            OnError,
            OnCompleted,
        }
    </pre>
    <p>
        In this next example we produce a faulted sequence. Note that the final value of
        the materialized sequence is an <em>OnErrorNotification</em>. Also that the materialized
        sequence does not error, it completes successfully.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        source.Materialize()
            .Dump("Materialize");

        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);
        source.OnError(new Exception("Fail?"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Materialize--&gt;OnNext(1)</div>
        <div class="line">Materialize--&gt;OnNext(2)</div>
        <div class="line">Materialize--&gt;OnNext(3)</div>
        <div class="line">Materialize--&gt;OnError(System.Exception)</div>
        <div class="line">Materialize completed</div>
    </div>
    <p>
        Materializing a sequence can be very handy for performing analysis or logging of
        a sequence. You can unwrap a materialized sequence by applying the <em>Dematerialize</em>
        extension method. The <em>Dematerialize</em> will only work on <em>IObservable&lt;Notification&lt;TSource&gt;&gt;</em>.
    </p>
    <a name="SelectMany"></a>
    <h2>SelectMany</h2>
    <p>
        Of the transformation operators above, we can see that <em>Select</em> is the most
        useful. It allows very broad flexibility in its transformation output and can even
        be used to reproduce some of the other transformation operators. The <em>SelectMany</em>
        operator however is even more powerful. In LINQ and therefore Rx, the <i>bind</i>
        method is <em>SelectMany</em>. Most other transformation operators can be built
        with <em>SelectMany</em>. Considering this, it is a shame to think that <em>SelectMany</em>
        may be one of the most misunderstood methods in LINQ.
    </p>
    <p>
        In my personal discovery of Rx, I struggled to grasp the <em>SelectMany</em> extension
        method. One of my colleagues helped me understand <em>SelectMany</em> better by
        suggesting I think of it as <q>from one, select many</q>. An even better definition
        is <q>From one, select zero or more</q>. If we look at the signature for <em>SelectMany</em>
        we see that it takes a source sequence and a function as its parameters.
    </p>
    <pre class="csharpcode">
        IObservable&lt;TResult&gt; SelectMany&lt;TSource, TResult&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, IObservable&lt;TResult&gt;&gt; selector)
    </pre>
    <p>
        The <code>selector</code> parameter is a function that takes a single value of <code>
            T</code> and returns a sequence. Note that the sequence the <code>selector</code>
        returns does not have to be of the same type as the <code>source</code>. Finally,
        the <em>SelectMany</em> return type is the same as the <code>selector</code> return
        type.
    </p>
    <p>
        This method is very important to understand if you wish to work with Rx effectively,
        so let's step through this slowly. It is also important to note its subtle differences
        to <em>IEnumerable&lt;T&gt;</em>'s <em>SelectMany</em> operator, which we will look
        at soon.
    </p>
    <p>
        Our first example will take a sequence with the single value '3' in it. The selector
        function we provide will produce a further sequence of numbers. This result sequence
        will be a range of numbers from 1 to the value provided i.e. 3. So we take the sequence
        [3] and return the sequence [1,2,3] from our <code>selector</code> function.
    </p>
    <pre class="csharpcode">
        Observable.Return(3)
            .SelectMany(i => Observable.Range(1, i))
            .Dump("SelectMany");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">SelectMany--&gt;1</div>
        <div class="line">SelectMany--&gt;2</div>
        <div class="line">SelectMany--&gt;3</div>
        <div class="line">SelectMany completed</div>
    </div>
    <p>
        If we modify our source to be a sequence of [1,2,3] like this...
    </p>
    <pre class="csharpcode">
        Observable.Range(1,3)
            .SelectMany(i => Observable.Range(1, i))
            .Dump("SelectMany");
    </pre>
    <p>
        ...we will now get an output with the result of each sequence ([1], [1,2] and [1,2,3])
        flattened to produce [1,1,2,1,2,3].
    </p>
    <div class="output">
        <div class="line">SelectMany--&gt;1</div>
        <div class="line">SelectMany--&gt;1</div>
        <div class="line">SelectMany--&gt;2</div>
        <div class="line">SelectMany--&gt;1</div>
        <div class="line">SelectMany--&gt;2</div>
        <div class="line">SelectMany--&gt;3</div>
        <div class="line">SelectMany completed</div>
    </div>
    <p>
        This last example better illustrates how <em>SelectMany</em> can take a <em>single</em>
        value and expand it to many values. When we then apply this to a <em>sequence</em>
        of values, the result is each of the child sequences combined to produce the final
        sequence. In both examples, we have returned a sequence that is the same type as
        the source. This is not a restriction however, so in this next example we return
        a different type. We will reuse the <em>Select</em> example of transforming an integer
        to an ASCII character. To do this, the <code>selector</code> function just returns
        a char sequence with a single value.
    </p>
    <pre class="csharpcode">
        Func&lt;int, char&gt; letter = i =&gt; (char)(i + 64);
        Observable.Return(1)
            .SelectMany(i =&gt; Observable.Return(letter(i)));
            .Dump("SelectMany");
    </pre>
    <p>
        So with the input of [1] we return a sequence of [A].
    </p>
    <div class="output">
        <div class="line">SelectMany--&gt;A</div>
        <div class="line">SelectMany completed</div>
    </div>
    <p>
        Extending the source sequence to have many values, will give us a result with many
        values.
    </p>
    <pre class="csharpcode">
        Func&lt;int, char&gt; letter = i =&gt; (char)(i + 64);
        Observable.Range(1,3)
            .SelectMany(i =&gt; Observable.Return(letter(i)))
            .Dump("SelectMany");
    </pre>
    <p>
        Now the input of [1,2,3] produces [[A], [B], [C]] which is flattened to just [A,B,C].
    </p>
    <div class="output">
        <div class="line">SelectMany--&gt;A</div>
        <div class="line">SelectMany--&gt;B</div>
        <div class="line">SelectMany--&gt;C</div>
    </div>
    <p>
        Note that we have effectively recreated the <em>Select</em> operator.
    </p>
    <p>
        The last example maps a number to a letter. As there are only 26 letters, it would
        be nice to ignore values greater than 26. This is easy to do. While we must return
        a sequence for each element of the source, there aren't any rules that prevent it
        from being an empty sequence. In this case if the element value is a number outside
        of the range 1-26 we return an empty sequence.
    </p>
    <pre class="csharpcode">
        Func&lt;int, char&gt; letter = i =&gt; (char)(i + 64);
        Observable.Range(1, 30)
            .SelectMany(
                i =&gt;
                {
                    if (0 &lt; i &amp;&amp; i &lt; 27)
                    {
                        return Observable.Return(letter(i));
                    }
                    else
                    {
                        return Observable.Empty&lt;char&gt;();
                    }
                })
            .Dump("SelectMany");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">A</div>
        <div class="line">B</div>
        <div class="line">C</div>
        <div class="line">...</div>
        <div class="line">X</div>
        <div class="line">Y</div>
        <div class="line">Z</div>
        <div class="line">Completed</div>
    </div>
    <p>
        To be clear, for the source sequence [1..30], the value 1 produced a sequence [A],
        the value 2 produced a sequence [B] and so on until value 26 produced a sequence
        [Z]. When the source produced value 27, the <code>selector</code> function returned
        the empty sequence []. Values 28, 29 and 30 also produced empty sequences. Once
        all the sequences from the calls to the selector had been fattened to produce the
        final result, we end up with the sequence [A..Z].
    </p>
    <p>
        Now that we have covered the third of our three higher order functions, let us take
        time to reflect on some of the methods we have already learnt. First we can consider
        the <em>Where</em> extension method. We first looked at this method in the chapter
        on <a href="05_Filtering.html#Where">Reducing a sequence</a>. While this method
        does reduce a sequence, it is not a fit for a functional <i>fold</i> as the result
        is still a sequence. Taking this into account, we find that <em>Where</em> is actually
        a fit for <i>bind</i>. As an exercise, try to write your own extension method version
        of <em>Where</em> using the <em>SelectMany</em> operator. Review the last example
        for some help...
    </p>
    <hr style="page-break-after: always" />
    <p>
        An example of a <em>Where</em> extension method written using <em>SelectMany</em>:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;T&gt; Where&lt;T&gt;(this IObservable&lt;T&gt; source, Func&lt;T, bool&gt; predicate)
        {
            return source.SelectMany(
                item =&gt;
                {
                    if (predicate(item))
                    {
                        return Observable.Return(item);
                    }
                    else
                    {
                        return Observable.Empty&lt;T&gt;();
                    }
                });
        }
    </pre>
    <p>
        Now that we know we can use <em>SelectMany</em> to produce <em>Where</em>, it should
        be a natural progression for you the reader to be able to extend this to reproduce
        other filters like <em>Skip</em> and <em>Take</em>.
    </p>
    <p>
        As another exercise, try to write your own version of the <em>Select</em> extension
        method using <em>SelectMany</em>. Refer to our example where we use <em>SelectMany</em>
        to convert <code>int</code> values into <code>char</code> values if you need some
        help...
    </p>
    <hr style="page-break-after: always" />
    <p>
        An example of a <em>Select</em> extension method written using <em>SelectMany</em>:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TResult&gt; MySelect&lt;TSource, TResult&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TResult&gt; selector)
        {
            return source.SelectMany(value =&gt; Observable.Return(selector(value)));
        }
    </pre>
    <a name="IEnumerableVsIObservableSelectMany"></a>
    <h3>IEnumerable&lt;T&gt; vs. IObservable&lt;T&gt; SelectMany</h3>
    <p>
        It is worth noting the difference between the implementations of <em>IEnumerable&lt;T&gt;</em>
        <em>SelectMany</em> and <em>IObservable&lt;T&gt;</em> <em>SelectMany</em>. Consider
        that <em>IEnumerable&lt;T&gt;</em> sequences are pull based and blocking. This means
        that when an <em>IEnumerable&lt;T&gt;</em> is processed with a <em>SelectMany</em>
        it will pass one item at a time to the <code>selector</code> function and wait until
        it has processed all of the values from the <code>selector</code> before requesting
        (pulling) the next value from the source.
    </p>
    <p>
        Consider an <em>IEnumerable&lt;T&gt;</em> source sequence of [1,2,3]. If we process
        that with a <em>SelectMany</em> operator that returns a sequence of [x*10, (x*10)+1,
        (x*10)+2], we would get the [[10,11,12], [20,21,22], [30,31,32]].
    </p>
    <pre class="csharpcode">
        private IEnumerable&lt;int&gt; GetSubValues(int offset)
        {
            yield return offset * 10;
            yield return (offset * 10) + 1;
            yield return (offset * 10) + 2;
        }
    </pre>
    <p>
        We then apply the <code>GetSubValues</code> method with the following code:
    </p>
    <pre class="csharpcode">
        var enumerableSource = new [] {1, 2, 3};
        var enumerableResult = enumerableSource.SelectMany(GetSubValues);
        foreach (var value in enumerableResult)
        {
            Console.WriteLine(value);
        }
    </pre>
    <p>
        The resulting child sequences are flattened into [10,11,12,20,21,22,30,31,32].
    </p>
    <div class="output">
        <div class="line">10</div>
        <div class="line">11</div>
        <div class="line">12</div>
        <div class="line">20</div>
        <div class="line">21</div>
        <div class="line">22</div>
        <div class="line">30</div>
        <div class="line">31</div>
        <div class="line">32</div>
    </div>
    <p>
        The difference with <em>IObservable&lt;T&gt;</em> sequences is that the call to
        the <em>SelectMany</em>'s <code>selector</code> function is not blocking and the
        result sequence can produce values over time. This means that subsequent 'child'
        sequences can overlap. Let us consider again a sequence of [1,2,3], but this time
        values are produced three second apart. The <code>selector</code> function will
        also produce sequence of [x*10, (x*10)+1, (x*10)+2] as per the example above, however
        these values will be four seconds apart.
    </p>
    <p>
        To visualize this kind of asynchronous data we need to represent space and time.
    </p>
    <a name="VisualizingSequences"></a>
    <h3>Visualizing sequences</h3>
    <p>
        Let's divert quickly and talk about a technique we will use to help communicate
        the concepts relating to sequences. Marble diagrams are a way of visualizing sequences.
        Marble diagrams are great for sharing Rx concepts and describing composition of
        sequences. When using marble diagrams there are only a few things you need to know
    </p>
    <ol>
        <li>a sequence is represented by a horizontal line </li>
        <li>time moves to the right (i.e. things on the left happened before things on the right)</li>
        <li>notifications are represented by symbols:
            <ol>
                <li>'0' for OnNext </li>
                <li>'X' for an OnError </li>
                <li>'|' for OnCompleted </li>
            </ol>
        </li>
        <li>many concurrent sequences can be visualized by creating rows of sequences</li>
    </ol>
    <p>
        This is a sample of a sequence of three values that completes:
    </p>
    <div class="marble">
        <pre class="line">--0--0--0-|</pre>
    </div>
    <p>
        This is a sample of a sequence of four values then an error:
    </p>
    <div class="marble">
        <pre class="line">--0--0--0--0--X</pre>
    </div>
    <p>
        Now going back to our <em>SelectMany</em> example, we can visualize our input sequence
        by using values in instead of the 0 marker. This is the marble diagram representation
        of the sequence [1,2,3] spaced three seconds apart (note each character represents
        one second).
    </p>
    <div class="marble">
        <pre class="line">--1--2--3|</pre>
    </div>
    <p>
        Now we can leverage the power of marble diagrams by introducing the concept of time
        and space. Here we see the visualization of the sequence produced by the first value
        1 which gives us the sequence [10,11,12]. These values were spaced four seconds
        apart, but the initial value is produce immediately.
    </p>
    <div class="marble">
        <pre class="line">1---1---1|</pre>
        <pre class="line">0   1   2|</pre>
    </div>
    <p>
        As the values are double digit they cover two rows, so the value of 10 is not confused
        with the value 1 immediately followed by the value 0. We add a row for each sequence
        produced by the <code>selector</code> function.
    </p>
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
    <p>
        Now that we can visualize the source sequence and its child sequences, we should
        be able to deduce the expected output of the <em>SelectMany</em> operator. To create
        a result row for our marble diagram, we simple allow the values from each child
        sequence to 'fall' into the new result row.
    </p>
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
    <p>
        If we take this exercise and now apply it to code, we can validate our marble diagram.
        First our method that will produce our child sequences:
    </p>
    <pre class="csharpcode">
        private IObservable&lt;long&gt; GetSubValues(long offset)
        {
            //Produce values [x*10, (x*10)+1, (x*10)+2] 4 seconds apart, but starting immediately.
            return Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(4))
                .Select(x =&gt; (offset*10) + x)
                .Take(3);
        }
    </pre>
    <p>
        This is the code that takes the source sequence to produce our final output:
    </p>
    <pre class="csharpcode">
        // Values [1,2,3] 3 seconds apart.
        Observable.Interval(TimeSpan.FromSeconds(3))
            .Select(i => i + 1) //Values start at 0, so add 1.
            .Take(3)            //We only want 3 values
            .SelectMany(GetSubValues) //project into child sequences
            .Dump("SelectMany");
    </pre>
    <p>
        The output produced matches our expectations from the marble diagram.
    </p>
    <div class="output">
        <div class="line">SelectMany--&gt;10</div>
        <div class="line">SelectMany--&gt;20</div>
        <div class="line">SelectMany--&gt;11</div>
        <div class="line">SelectMany--&gt;30</div>
        <div class="line">SelectMany--&gt;21</div>
        <div class="line">SelectMany--&gt;12</div>
        <div class="line">SelectMany--&gt;31</div>
        <div class="line">SelectMany--&gt;22</div>
        <div class="line">SelectMany--&gt;32</div>
        <div class="line">SelectMany completed</div>
    </div>
    <p>
        We have previously looked at the <em>Select</em> operator when it is used in Query
        Comprehension Syntax, so it is worth noting how you use the <em>SelectMany</em>
        operator. The <em>Select</em> extension method maps quite obviously to query comprehension
        syntax, <em>SelectMany</em> is not so obvious. As we saw in the earlier example,
        the simple implementation of just suing select is as follows:
    </p>
    <pre class="csharpcode">
        var query = from i in Observable.Range(1, 5)
                    select i;
    </pre>
    <p>
        If we wanted to add a simple <code>where</code> clause we can do so like this:
    </p>
    <pre class="csharpcode">
        var query = from i in Observable.Range(1, 5)
                    where i%2==0
                    select i;
    </pre>
    <p>
        To add a <em>SelectMany</em> to the query, we actually add an extra <code>from</code>
        clause.
    </p>
    <pre class="csharpcode">
        var query = from i in Observable.Range(1, 5)
                    where i%2==0
                    from j in GetSubValues(i)
                    select j;
        //Equivalent to 
        var query = Observable.Range(1, 5)
                           .Where(i=>i%2==0)
                           .SelectMany(GetSubValues);
    </pre>
    <p>
        An advantage of using the query comprehension syntax is that you can easily access
        other variables in the scope of the query. In this example we select into an anon
        type both the value from the source and the child value.
    </p>
    <pre class="csharpcode">
        var query = from i in Observable.Range(1, 5)
                    where i%2==0
                    from j in GetSubValues(i)
                    select new {i, j};
        query.Dump("SelectMany");
    </pre>
    <p>
        Output
    </p>
    <div class="output">
        <div class="line">SelectMany--&gt;{ i = 2, j = 20 }</div>
        <div class="line">SelectMany--&gt;{ i = 4, j = 40 }</div>
        <div class="line">SelectMany--&gt;{ i = 2, j = 21 }</div>
        <div class="line">SelectMany--&gt;{ i = 4, j = 41 }</div>
        <div class="line">SelectMany--&gt;{ i = 2, j = 22 }</div>
        <div class="line">SelectMany--&gt;{ i = 4, j = 42 }</div>
        <div class="line">SelectMany completed</div>
    </div>
    <hr />
    <a name="Part2Summary"></a>
    <p>
        This brings us to a close on Part 2. The key takeaways from this were to allow you
        the reader to understand a key principal to Rx: functional composition. As we move
        through Part 2, examples became progressively more complex. We were leveraging the
        power of LINQ to chain extension methods together to compose complex queries.
    </p>
    <p>
        We didn't try to tackle all of the operators at once, we approached them in groups.
    </p>
    <ul>
        <li>Creation</li>
        <li>Reduction</li>
        <li>Inspection</li>
        <li>Aggregation</li>
        <li>Transformation</li>
    </ul>
    <p>
        On deeper analysis of the operators we find that most of the operators are actually
        specialization of the higher order functional concepts. We named them the ABC's
        of functional programming:
    </p>
    <ul>
        <li>Anamorphism, aka:
            <ul>
                <li>Ana</li>
                <li>Unfold</li>
                <li>Generate</li>
            </ul>
        </li>
        <li>Bind, aka:
            <ul>
                <li>Map</li>
                <li>SelectMany</li>
                <li>Projection</li>
                <li>Transform</li>
            </ul>
        </li>
        <li>Catamorphism, aka:
            <ul>
                <li>Cata</li>
                <li>Fold</li>
                <li>Reduce</li>
                <li>Accumulate</li>
                <li>Inject</li>
            </ul>
        </li>
    </ul>
    <p>
        Now you should feel that you have a strong understanding of how a sequence can be
        manipulated. What we have learnt up to this point however can all largely be applied
        to <em>IEnumerable</em> sequences too. Rx can be much more complex than what many
        people will have dealt with in <em>IEnumerable</em> world, as we have seen with
        the <em>SelectMany</em> operator. In the next part of the book we will uncover features
        specific to the asynchronous nature of Rx. With the foundation we have built so
        far we should be able to tackle the far more challenging and interesting features
        of Rx.
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
