<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Inspection</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <!--
        Review code samples. I think one says 6 in the code but 2 in the output.
        Begin middle end.
        EM vs Strong
        Word spell check.
        -->
    <!--
        Last chapter talked about the deluge of data and removing the crap.
        This chapter shoould continue the theme and instead discuss "in contrast to removing data 
        we dont want, we can also picking out data we do want, or validate that the sequence meets
         an expectation."

-->
    <a name="Inspection"></a>
    <h1>Inspection</h1>
    <p>
        Making sense of all the data we consume is not always about just filtering out the
        redundant and superfluous. Sometimes we need to pluck out data that is relevant
        or validate that a sequence even meets our expectations. Does this data have any
        values that meet this specification? Is this specific value in the sequence? Get
        me that specific value from the sequence!
    </p>
    <p>
        In the last chapter we looked at a series of ways to reduce your observable sequence
        via a variety of filters. Next we will look at operators that provide inspection
        functionality. Most of these operators will reduce your observable sequence down
        to a sequence with a single value in it. As the return value of these methods is
        not a scalar (it is still IObservable&lt;T&gt;) these methods do not actually satisfy
        our definition of catamorphism, but suit our examination of reducing a sequence
        to a single value.
    </p>
    <p>
        The series of methods we will look at next are useful for inspecting a given sequence.
        Each of them returns an observable sequence with the single value containing the
        result. This proves useful, as by their nature they are asynchronous. They are all
        quite simple so we will be brief with each of them.
    </p>
    <a name="Any"></a>
    <h2>Any</h2>
    <p>
        First we can look at the parameterless overload for the extension method <em>Any</em>.
        This will simply return an observable sequence that has the single value of <code>false</code>
        if the source completes without any values. If the source does produce a value however,
        then when the first value is produced, the result sequence will immediately push
        <code>true</code> and then complete. If the first notification it gets is an error,
        then it will pass that error on.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Subject completed"));
        var any = subject.Any();

        any.Subscribe(b =&gt; Console.WriteLine("The subject has any values? {0}", b));

        subject.OnNext(1);
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">The subject has any values? True</div>
        <div class="line">subject completed</div>
    </div>
    <p>
        If we now remove the OnNext(1), the output will change to the following
    </p>
    <div class="output">
        <div class="line">subject completed</div>
        <div class="line">The subject has any values? False</div>
    </div>
    <p>
        If the source errors it would only be interesting if it was the first notification,
        otherwise the <em>Any</em> method will have already pushed true. If the first notification
        is an error then <em>Any</em> will just pass it along as an <em>OnError</em> notification.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(Console.WriteLine,
            ex =&gt; Console.WriteLine("subject OnError : {0}", ex),
            () =&gt; Console.WriteLine("Subject completed"));
        var any = subject.Any();

        any.Subscribe(b =&gt; Console.WriteLine("The subject has any values? {0}", b),
            ex =&gt; Console.WriteLine(".Any() OnError : {0}", ex),
            () =&gt; Console.WriteLine(".Any() completed"));

        subject.OnError(new Exception());
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">subject OnError : System.Exception: Fail</div>
        <div class="line">.Any() OnError : System.Exception: Fail</div>
    </div>
    <p>
        The <em>Any</em> method also has an overload that takes a predicate. This effectively
        makes it a <em>Where</em> with an <em>Any</em> appended to it.
    </p>
    <pre class="csharpcode">
        subject.Any(i =&gt; i &gt; 2);
        //Functionally equivalent to 
        subject.Where(i =&gt; i &gt; 2).Any();
    </pre>
    <p>
        As an exercise, write your own version of the two <em>Any</em> extension method overloads.
        While the answer may not be immediately obvious, we have covered enough material
        for you to create this using the methods you know...
    </p>
    <hr style="page-break-after: always" />
    <p>
        Example of the <em>Any</em> extension methods written with Observable.Create:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;bool&gt; MyAny&lt;T&gt;(
            this IObservable&lt;T&gt; source)
        {
            return Observable.Create&lt;bool&gt;(
                o =&gt;
                {
                    var hasValues = false;
                    return source
                        .Take(1)
                        .Subscribe(
                            _ =&gt; hasValues = true,
                            o.OnError,
                            () =&gt;
                            {
                                o.OnNext(hasValues);
                                o.OnCompleted();
                            });
                });
        }

        public static IObservable&lt;bool&gt; MyAny&lt;T&gt;(
            this IObservable&lt;T&gt; source, 
            Func&lt;T, bool&gt; predicate)
        {
            return source
                .Where(predicate)
                .MyAny();
        }
    </pre>
    <a name="All"></a>
    <h2>All</h2>
    <p>
        The <em>All</em>() extension method works just like the <em>Any</em> method, except
        that all values must meet the predicate. As soon as a value does not meet the predicate
        a <code>false</code> value is returned then the output sequence completed. If the
        source is empty, then <em>All</em> will push <code>true</code> as its value. As
        per the <em>Any</em> method, and errors will be passed along to the subscriber of
        the <em>All</em> method.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Subject completed"));
        var all = subject.All(i =&gt; i &lt; 5);
        all.Subscribe(b =&gt; Console.WriteLine("All values less than 5? {0}", b));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(6);
        subject.OnNext(2);
        subject.OnNext(1);
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">6</div>
        <div class="line">All values less than 5? False</div>
        <div class="line">all completed</div>
        <div class="line">2</div>
        <div class="line">1</div>
        <div class="line">subject completed</div>
    </div>
    <p>
        Early adopters of Rx may notice that the <em>IsEmpty</em> extension method is missing.
        You can easily replicate the missing method using the <em>All</em> extension method.
    </p>
    <pre class="csharpcode">
        //IsEmpty() is deprecated now.
        //var isEmpty = subject.IsEmpty();
        var isEmpty = subject.All(_ => false);
    </pre>
    <a name="Contains"></a>
    <h2>Contains</h2>
    <p>
        The <em>Contains</em> extension method overloads could sensibly be overloads to
        the <em>Any</em> extension method. The <em>Contains</em> extension method has the
        same behavior as <em>Any</em>, however it specifically targets the use of <em>IComparable</em>
        instead of the usage of predicates and is designed to seek a specific value
        instead of a value that fits the predicate. I believe that these are not overloads
        of <em>Any</em> for consistency with <em>IEnumerable</em>.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(
            Console.WriteLine, 
            () =&gt; Console.WriteLine("Subject completed"));
        var contains = subject.Contains(2);
        contains.Subscribe(
            b =&gt; Console.WriteLine("Contains the value 2? {0}", b),
            () =&gt; Console.WriteLine("contains completed"));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
            
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">Contains the value 2? True</div>
        <div class="line">contains completed</div>
        <div class="line">3</div>
        <div class="line">Subject completed</div>
    </div>
    <p>
        There is also an overload to <em>Contains</em> that allows you to specify an implementation
        of <em>IEqualityComparer&lt;T&gt;</em> other than the default for the type. This
        can be useful if you have a sequence of custom types that may have some special
        rules for equality depending on the use case.
    </p>
    <a name="DefaultIfEmpty"></a>
    <h2>DefaultIfEmpty</h2>
    <p>
        The <em>DefaultIfEmpty</em> extension method will return a single value if the source
        sequence is empty. Depending on the overload used, it will either be the value provided
        as the default, or <code>Default(T)</code>. <code>Default(T)</code> will
        be the zero value for struct types and will be null for classes. If the source is
        not empty then all values will be passed straight on through.
    </p>
    <p>
        In this example the source produces values, so the result of <em>DefaultIfEmpty</em>
        is just the source.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Subject completed"));
        var defaultIfEmpty = subject.DefaultIfEmpty();
        defaultIfEmpty.Subscribe(
            b =&gt; Console.WriteLine("defaultIfEmpty value: {0}", b),
            () =&gt; Console.WriteLine("defaultIfEmpty completed"));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);

        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">defaultIfEmpty value: 1</div>
        <div class="line">2</div>
        <div class="line">defaultIfEmpty value: 2</div>
        <div class="line">3</div>
        <div class="line">defaultIfEmpty value: 3</div>
        <div class="line">Subject completed</div>
        <div class="line">defaultIfEmpty completed</div>
    </div>
    <p>
        If the source is empty, we can use either the default value for the type (i.e. 0
        for int) or provide our own value in this case 42.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Subject completed"));
        var defaultIfEmpty = subject.DefaultIfEmpty();
        defaultIfEmpty.Subscribe(
            b =&gt; Console.WriteLine("defaultIfEmpty value: {0}", b),
            () =&gt; Console.WriteLine("defaultIfEmpty completed"));

        var default42IfEmpty = subject.DefaultIfEmpty(42);
        default42IfEmpty.Subscribe(
            b =&gt; Console.WriteLine("default42IfEmpty value: {0}", b),
            () =&gt; Console.WriteLine("default42IfEmpty completed"));

        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Subject completed</div>
        <div class="line">defaultIfEmpty value: 0</div>
        <div class="line">defaultIfEmpty completed</div>
        <div class="line">default42IfEmpty value: 42</div>
        <div class="line">default42IfEmpty completed</div>
    </div>
    <a name="ElementAt"></a>
    <h2>ElementAt</h2>
    <p>
        The <strong>ElementAt</strong> extension method allows us to "cherry pick" out a value
        at a given index. Like the <em>IEnumerable&lt;T&gt;</em> version it is uses a 0
        based index.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("Subject completed"));
        var elementAt1 = subject.ElementAt(1);
        elementAt1.Subscribe(
            b =&gt; Console.WriteLine("elementAt1 value: {0}", b),
            () =&gt; Console.WriteLine("elementAt1 completed"));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);

        subject.OnCompleted();
    </pre>
    <p>
        Output
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">elementAt1 value: 2</div>
        <div class="line">elementAt1 completed</div>
        <div class="line">3</div>
        <div class="line">subject completed</div>
    </div>
    <p>
        As we can't check the length of an observable sequence it is fair to assume that
        sometimes this method could cause problems. If your source sequence only produces
        five values and we ask for <code>ElementAt(5)</code>, the result sequence will error
        with an <em>ArgumentOutOfRangeException</em> inside when the source completes. There
        are three options we have:
    </p>
    <ul>
        <li>Handle the OnError gracefully</li>
        <li>Use .Skip(5).Take(1); This will ignore the first 5 values and the only take the
            6th value. If the sequence has less than 6 elements we just get an empty sequence,
            but no errors.</li>
        <li>Use ElementAtOrDefault</li>
    </ul>
    <p>
        <em>ElementAtOrDefault</em> extension method will protect us in case the index is
        out of range, by pushing the <code>Default(T)</code> value. Currently there is not
        an option to provide your own default value.
    </p>
    <a name="SequenceEqual"></a>
    <h2>SequenceEqual</h2>
    <p>
        Finally <em>SequenceEqual</em> extension method is perhaps a stretch to put in a
        chapter that starts off talking about catamorphism and fold, but it does serve well
        for the theme of inspection. This method allows us to compare two observable sequences.
        As each source sequence produces values, they are compared to a cache of the other
        sequence to ensure that each sequence has the same values in the same order and
        that the sequences are the same length. This means that the result sequence can
        return <code>false</code> as soon as the source sequences produce diverging values,
        or <code>true</code> when both sources complete with the same values.
    </p>
    <pre class="csharpcode">
        var subject1 = new Subject&lt;int&gt;();
        subject1.Subscribe(
            i=&gt;Console.WriteLine("subject1.OnNext({0})", i),
            () =&gt; Console.WriteLine("subject1 completed"));
        var subject2 = new Subject&lt;int&gt;();
        subject2.Subscribe(
            i=&gt;Console.WriteLine("subject2.OnNext({0})", i),
            () =&gt; Console.WriteLine("subject2 completed"));


        var areEqual = subject1.SequenceEqual(subject2);
        areEqual.Subscribe(
            i =&gt; Console.WriteLine("areEqual.OnNext({0})", i),
            () =&gt; Console.WriteLine("areEqual completed"));

        subject1.OnNext(1);
        subject1.OnNext(2);

        subject2.OnNext(1);
        subject2.OnNext(2);
        subject2.OnNext(3);

        subject1.OnNext(3);

        subject1.OnCompleted();
        subject2.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="csharpcode">
        <div class="line">subject1.OnNext(1)</div>
        <div class="line">subject1.OnNext(2)</div>
        <div class="line">subject2.OnNext(1)</div>
        <div class="line">subject2.OnNext(2)</div>
        <div class="line">subject2.OnNext(3)</div>
        <div class="line">subject1.OnNext(3)</div>
        <div class="line">subject1 completed</div>
        <div class="line">subject2 completed</div>
        <div class="line">areEqual.OnNext(True)</div>
        <div class="line">areEqual completed</div>
    </div>
    <p>
        This chapter covered a set of methods that allow us to inspect observable sequences.
        The result of each, generally, returns a sequence with a single value. We will continue
        to look at methods to reduce our sequence until we discover the elusive functional
        fold feature.
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
