<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Reducing a sequence</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="Reduction"></a>
    <h1>Reducing a sequence</h1>
    <p>
        We live in the information age. Data is being created, stored and distributed at
        a phenomenal rate. Consuming this data can be overwhelming, like trying to drink
        directly from the fire hose. We need the ability to pick out the data we need, choose
        what is and is not relevant, and roll up groups of data to make it relevant. Users,
        customers and managers need you do this with more data than ever before, while still
        delivering higher performance and tighter deadlines.
    </p>
    <p>
        Given that we know how to create an observable sequence, we will now look at the
        various methods that can reduce an observable sequence. We can categorize operators
        that reduce a sequence to the following:
    </p>
    <dl>
        <dt>Filter and partition operators</dt>
        <dd>
            Reduce the source sequence to a sequence with at most the same number of elements</dd>
        <dt>Aggregation operators</dt>
        <dd>
            Reduce the source sequence to a sequence with a single element</dd>
        <dt>Fold operators</dt>
        <dd>
            Reduce the source sequence to a single element as a scalar value</dd>
    </dl>
    <p>
        We discovered that the creation of an observable sequence from a scalar value is
        defined as <i>anamorphism</i> or described as an '<i>unfold</i>'. We can think of
        the anamorphism from <code>T</code> to <em>IObservable&lt;T&gt;</em> as an 'unfold'.
        This could also be referred to as "entering the monad" where in this case (and for
        most cases in this book) the monad is <em>IObservable&lt;T&gt;</em>. What we will
        now start looking at are methods that eventually get us to the inverse which is
        defined as <i>catamorphism</i> or a <em>fold</em>. Other popular names for fold
        are 'reduce', 'accumulate' and 'inject'.
    </p>
    <a name="Where"></a>
    <h2>Where</h2>
    <p>
        Applying a filter to a sequence is an extremely common exercise and the most common
        filter is the <em>Where</em> clause. In Rx you can apply a where clause with the
        <em>Where</em> extension method. For those that are unfamiliar, the signature of
        the <em>Where</em> method is as follows:
    </p>
    <pre class="csharpcode">
        IObservable&lt;T&gt; Where(this IObservable&lt;T&gt; source, Fun&lt;T, bool&gt; predicate)
    </pre>
    <p>
        Note that both the source parameter and the return type are the same. This allows
        for a fluent interface, which is used heavily throughout Rx and other LINQ code.
        In this example we will use the <em>Where</em> to filter out all even values produced
        from a <em>Range</em> sequence.
    </p>
    <pre class="csharpcode">
        var oddNumbers = Observable.Range(0, 10)
            .Where(i =&gt; i % 2 == 0)
            .Subscribe(
                Console.WriteLine, 
                () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">2</div>
        <div class="line">4</div>
        <div class="line">6</div>
        <div class="line">8</div>
        <div class="line">Completed</div>
    </div>
    <p>
        The <em>Where</em> operator is one of the many standard LINQ operators. This and
        other LINQ operators are common use in the various implementations of query operators,
        most notably the <em>IEnumerable&lt;T&gt;</em> implementation. In most cases the
        operators behave just as they do in the <em>IEnumerable&lt;T&gt;</em> implementations,
        but there are some exceptions. We will discuss each implementation and explain any
        variation as we go. By implementing these common operators Rx also gets language
        support for free via C# query comprehension syntax. For the examples in this book
        however, we will keep with using extension methods for consistency.
    </p>
    <a name="Distinct"></a>
    <h2>Distinct and DistinctUntilChanged</h2>
    <p>
        As I am sure most readers are familiar with the <em>Where</em> extension method
        for <em>IEnumerable&lt;T&gt;</em>, some will also know the <em>Distinct</em> method.
        In Rx, the <em>Distinct</em> method has been made available for observable sequences
        too. For those that are unfamiliar with <em>Distinct</em>, and as a recap for those
        that are, <em>Distinct</em> will only pass on values from the source that it has
        not seen before.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        var distinct = subject.Distinct();
            
        subject.Subscribe(
            i =&gt; Console.WriteLine("{0}", i),
            () =&gt; Console.WriteLine("subject.OnCompleted()"));
        distinct.Subscribe(
            i =&gt; Console.WriteLine("distinct.OnNext({0})", i),
            () =&gt; Console.WriteLine("distinct.OnCompleted()"));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        subject.OnNext(1);
        subject.OnNext(1);
        subject.OnNext(4);
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">distinct.OnNext(1)</div>
        <div class="line">2</div>
        <div class="line">distinct.OnNext(2)</div>
        <div class="line">3</div>
        <div class="line">distinct.OnNext(3)</div>
        <div class="line">1</div>
        <div class="line">1</div>
        <div class="line">4</div>
        <div class="line">distinct.OnNext(4)</div>
        <div class="line">subject.OnCompleted()</div>
        <div class="line">distinct.OnCompleted()</div>
    </div>
    <p>
        Take special note that the value 1 is pushed 3 times but only passed through the
        first time. There are overloads to <em>Distinct</em> that allow you to specialize
        the way an item is determined to be distinct or not. One way is to provide a function
        that returns a different value to use for comparison. Here we look at an example
        that uses a property from a custom class to define if a value is distinct.
    </p>
    <pre class="csharpcode">
        public class Account
        {
            public int AccountId { get; set; }
            //... etc
        }
        public void Distinct_with_KeySelector()
        {
            var subject = new Subject&lt;Account&gt;();
            var distinct = subject.Distinct(acc =&gt; acc.AccountId);
        }
    </pre>
    <p>
        In addition to the <code>keySelector</code> function that can be provided, there
        is an overload that takes an <em>IEqualityComparer&lt;T&gt;</em> instance. This
        is useful if you have a custom implementation that you can reuse to compare instances
        of your type <code>T</code>. Lastly there is an overload that takes a <code>keySelector</code>
        and an instance of <em>IEqualityComparer&lt;TKey&gt;</em>. Note that the equality
        comparer in this case is aimed at the selected key type (<code>TKey</code>), not
        the type <code>T</code>.
    </p>
    <p>
        A variation of <em>Distinct</em>, that is peculiar to Rx, is <em>DistinctUntilChanged</em>.
        This method will surface values only if they are different from the previous value.
        Reusing our first <em>Distinct</em> example, note the change in output.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        var distinct = subject.DistinctUntilChanged();
            
        subject.Subscribe(
            i =&gt; Console.WriteLine("{0}", i),
            () =&gt; Console.WriteLine("subject.OnCompleted()"));
        distinct.Subscribe(
            i =&gt; Console.WriteLine("distinct.OnNext({0})", i),
            () =&gt; Console.WriteLine("distinct.OnCompleted()"));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        subject.OnNext(1);
        subject.OnNext(1);
        subject.OnNext(4);
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">distinct.OnNext(1)</div>
        <div class="line">2</div>
        <div class="line">distinct.OnNext(2)</div>
        <div class="line">3</div>
        <div class="line">distinct.OnNext(3)</div>
        <div class="line">1</div>
        <div class="line">distinct.OnNext(1)</div>
        <div class="line">1</div>
        <div class="line">4</div>
        <div class="line">distinct.OnNext(4)</div>
        <div class="line">subject.OnCompleted()</div>
        <div class="line">distinct.OnCompleted()</div>
    </div>
    <p>
        The difference between the two examples is that the value 1 is pushed twice. However
        the third time that the source pushes the value 1, it is immediately after the second
        time value 1 is pushed. In this case it is ignored. Teams I have worked with have
        found this method to be extremely useful in reducing any noise that a sequence may
        provide.
    </p>
    <a name="IgnoreElements"></a>
    <h2>IgnoreElements</h2>
    <p>
        The <em>IgnoreElements</em> extension method is a quirky little tool that allows
        you to receive the <em>OnCompleted</em> or <em>OnError</em> notifications. We could
        effectively recreate it by using a <em>Where</em> method with a predicate that always
        returns false.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        
        //Could use subject.Where(_=>false);
        var noElements = subject.IgnoreElements();
        subject.Subscribe(
            i=&gt;Console.WriteLine("subject.OnNext({0})", i),
            () =&gt; Console.WriteLine("subject.OnCompleted()"));
        noElements.Subscribe(
            i=&gt;Console.WriteLine("noElements.OnNext({0})", i),
            () =&gt; Console.WriteLine("noElements.OnCompleted()"));

        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">subject.OnNext(1)</div>
        <div class="line">subject.OnNext(2)</div>
        <div class="line">subject.OnNext(3)</div>
        <div class="line">subject.OnCompleted()</div>
        <div class="line">noElements.OnCompleted()</div>
    </div>
    <p>
        As suggested earlier we could use a <em>Where</em> to produce the same result
    </p>
    <pre class="csharpcode">
        subject.IgnoreElements();
        //Equivalent to 
        subject.Where(value=>false);
        //Or functional style that implies that the value is ignored.
        subject.Where(_=>false);
    </pre>
    <p>
        Just before we leave <em>Where</em> and <em>IgnoreElements</em>, I wanted to just
        quickly look at the last line of code. Until recently, I personally was not aware
        that '<code>_</code>' was a valid variable name; however it is commonly used by
        functional programmers to indicate an ignored parameter. This is perfect for the
        above example; for each value we receive, we ignore it and always return false.
        The intention is to improve the readability of the code via convention.
    </p>
    <a name="SkipAndTake"></a>
    <h2>Skip and Take</h2>
    <p>
        The other key methods to filtering are so similar I think we can look at them as
        one big group. First we will look at <em>Skip</em> and <em>Take</em>. These act
        just like they do for the <em>IEnumerable&lt;T&gt;</em> implementations. These are
        the most simple and probably the most used of the Skip/Take methods. Both methods
        just have the one parameter; the number of values to skip or to take.
    </p>
    <p>
        If we first look at <em>Skip</em>, in this example we have a range sequence of 10
        items and we apply a <code>Skip(3)</code> to it.
    </p>
    <pre class="csharpcode">
        Observable.Range(0, 10)
            .Skip(3)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">5</div>
        <div class="line">6</div>
        <div class="line">7</div>
        <div class="line">8</div>
        <div class="line">9</div>
        <div class="line">Completed</div>
    </div>
    <p>
        Note the first three values (0, 1 &amp; 2) were all ignored from the output. Alternatively,
        if we used <code>Take(3)</code> we would get the opposite result; i.e. we would
        only get the first 3 values and then the Take operator would complete the sequence.
    </p>
    <pre class="csharpcode">
        Observable.Range(0, 10)
            .Take(3)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">Completed</div>
    </div>
    <p>
        Just in case that slipped past any readers, it is the <em>Take</em> operator that
        completes once it has received its count. We can prove this by applying it to an
        infinite sequence.
    </p>
    <pre class="csharpcode">
        Observable.Interval(TimeSpan.FromMilliseconds(100))
            .Take(3)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">Completed</div>
    </div>
    <a name="SkipWhileTakeWhile"></a>
    <h3>SkipWhile and TakeWhile</h3>
    <p>
        The next set of methods allows you to skip or take values from a sequence while
        a predicate evaluates to true. For a <em>SkipWhile</em> operation this will filter
        out all values until a value fails the predicate, then the remaining sequence can
        be returned.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject
            .SkipWhile(i =&gt; i &lt; 4)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        subject.OnNext(4);
        subject.OnNext(3);
        subject.OnNext(2);
        subject.OnNext(1);
        subject.OnNext(0);

        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">4</div>
        <div class="line">3</div>
        <div class="line">2</div>
        <div class="line">1</div>
        <div class="line">0</div>
        <div class="line">Completed</div>
    </div>
    <p>
        <em>TakeWhile</em> will return all values while the predicate passes, and when the
        first value fails the sequence will complete.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject
            .TakeWhile(i =&gt; i &lt; 4)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        subject.OnNext(4);
        subject.OnNext(3);
        subject.OnNext(2);
        subject.OnNext(1);
        subject.OnNext(0);

        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">Completed</div>
    </div>
    <a name="SkipLastTakeLast"></a>
    <h3>SkipLast and TakeLast</h3>
    <p>
        These methods become quite self explanatory now that we understand Skip/Take and
        SkipWhile/TakeWhile. Both methods require a number of elements at the end of a sequence
        to either skip or take. The implementation of the <em>SkipLast</em> could cache
        all values, wait for the source sequence to complete, and then replay all the values
        except for the last number of elements. The Rx team however, has been a bit smarter
        than that. The real implementation will queue the specified number of notifications
        and once the queue size exceeds the value, it can be sure that it may drain a value
        from the queue.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject
            .SkipLast(2)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
        Console.WriteLine("Pushing 1");
        subject.OnNext(1);
        Console.WriteLine("Pushing 2");
        subject.OnNext(2);
        Console.WriteLine("Pushing 3");
        subject.OnNext(3);
        Console.WriteLine("Pushing 4");
        subject.OnNext(4);
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Pushing 1</div>
        <div class="line">Pushing 2</div>
        <div class="line">Pushing 3</div>
        <div class="line">1</div>
        <div class="line">Pushing 4</div>
        <div class="line">2</div>
        <div class="line">Completed</div>
    </div>
    <p>
        Unlike <em>SkipLast</em>, <em>TakeLast</em> does have to wait for the source sequence
        to complete to be able to push its results. As per the example above, there are
        <code>Console.WriteLine</code> calls to indicate what the program is doing at each
        stage.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject
            .TakeLast(2)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
        Console.WriteLine("Pushing 1");
        subject.OnNext(1);
        Console.WriteLine("Pushing 2");
        subject.OnNext(2);
        Console.WriteLine("Pushing 3");
        subject.OnNext(3);
        Console.WriteLine("Pushing 4");
        subject.OnNext(4);
        Console.WriteLine("Completing");
        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Pushing 1</div>
        <div class="line">Pushing 2</div>
        <div class="line">Pushing 3</div>
        <div class="line">Pushing 4</div>
        <div class="line">Completing</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">Completed</div>
    </div>
    <a name="SkipUntilTakeUntil"></a>
    <h3>SkipUntil and TakeUntil</h3>
    <p>
        Our last two methods make an exciting change to the methods we have previously looked.
        These will be the first two methods that we have discovered together that require
        two observable sequences.
    </p>
    <p>
        <em>SkipUntil</em> will skip all values until any value is produced by a secondary
        observable sequence.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        var otherSubject = new Subject&lt;Unit&gt;();
        subject
            .SkipUntil(otherSubject)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        otherSubject.OnNext(Unit.Default);
        subject.OnNext(4);
        subject.OnNext(5);
        subject.OnNext(6);
        subject.OnNext(7);
        subject.OnNext(8);

        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">4</div>
        <div class="line">5</div>
        <div class="line">6</div>
        <div class="line">7</div>
        <div class="line">Completed</div>
    </div>
    <p>
        Obviously, the converse is true for <em>TakeWhile</em>. When the secondary sequence
        produces a value, then the <em>TakeWhile</em> operator will complete the output
        sequence.
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        var otherSubject = new Subject&lt;Unit&gt;();
        subject
            .TakeUntil(otherSubject)
            .Subscribe(Console.WriteLine, () =&gt; Console.WriteLine("Completed"));
        subject.OnNext(1);
        subject.OnNext(2);
        subject.OnNext(3);
        otherSubject.OnNext(Unit.Default);
        subject.OnNext(4);
        subject.OnNext(5);
        subject.OnNext(6);
        subject.OnNext(7);
        subject.OnNext(8);

        subject.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">Completed</div>
    </div>
    <p>
        That was our quick run through of the filtering methods available in Rx. While they
        are pretty simple, as we will see, the power in Rx is down to the composability
        of its operators.
    </p>
    <p>
        These operators provide a good introduction to the filtering in Rx. The filter operators
        are your first stop for managing the potential deluge of data we can face in the
        information age. We now know how to remove unmatched data, duplicate data or excess
        data. Next, we will move on to the other two sub classifications of the reduction operators,
        inspection and aggregation.
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
