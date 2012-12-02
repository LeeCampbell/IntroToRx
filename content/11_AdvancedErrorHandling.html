<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Advanced error handling</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="AdvancedErrorHandling"></a>
    <h1>Advanced error handling</h1>
    <p>
        Exceptions happen. Exceptions themselves are not bad or good, however the way we raise or
        catch them can. Some exceptions are predictable and are due
        to sloppy code, for example a <em>DivideByZeroException</em>. Other exceptions cannot
        be prevented with defensive coding, for example an I/O exception like <em>FileNotFoundException</em>
        or <em>TimeoutException</em>. In these cases, we need to cater for the exception
        gracefully. Providing some sort of error message to the user, logging the error
        or perhaps retrying are all potential ways to handle these exceptions.
    </p>
    <p>
        The <em>IObserver&lt;T&gt;</em> interface and <em>Subscribe</em> extension methods
        provide the ability to cater for sequences that terminate in error, however they
        leave the sequence terminated. They also do not offer a composable way to cater
        for different <em>Exception</em> types. A functional approach that enables composition
        of error handlers, allowing us to remain in the monad, would be more useful. Again,
        Rx delivers.
    </p>
    <a name="ControlFlowConstructs"></a>
    <h2>Control flow constructs</h2>
    <p>
        Using marble diagrams, we will examine various ways to handle different control
        flows. Just as with normal .NET code, we have flow control constructs such as <code>
            try</code>/<code>catch</code>/<code>finally</code>. In this chapter we see how
        they can be applied to observable sequences.
    </p>
    <a name="Catch"></a>
    <h3>Catch</h3>
    <p>
        Just like a catch in SEH (Structured Exception Handling), with Rx you have the option
        of swallowing an exception, wrapping it in another exception or performing some
        other logic.
    </p>
    <p>
        We already know that observable sequences can handle erroneous situations with the
        <em>OnError</em> construct. A useful method in Rx for handling an <em>OnError</em>
        notification is the <em>Catch</em> extension method. Catch allows you to intercept
        a specific <em>Exception</em> type and then continue with another sequence.
    </p>
    <p>
        Below is the signature for the simple overload of catch:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TSource&gt; Catch&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; first, 
            IObservable&lt;TSource&gt; second)
        {
            ...
        }
    </pre>
    <a name="CatchSwallowingException"></a>
    <h4>Swallowing exceptions</h4>
    <p>
        With Rx, you can catch and swallow exceptions in a similar way to SEH. It is quite
        simple; we use the <em>Catch</em> extension method and provide an empty sequence
        as the second value.
    </p>
    <p>
        We can represent an exception being swallowed like this with a marble diagram.
    </p>
    <div class="marble">
        <pre class="line">S1--1--2--3--X</pre>
        <pre class="line">S2            -|</pre>
        <pre class="line">R --1--2--3----|</pre>
    </div>
    <p>
        Here S1 represents the first sequence that ends with an error (X). S2 is the continuation
        sequence, an empty sequence. R is the result sequence which starts as S1, then continues
        with S2 when S1 terminates.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        var result = source.Catch(Observable.Empty&lt;int&gt;());

        result.Dump("Catch"););

        source.OnNext(1);
        source.OnNext(2);
        source.OnError(new Exception("Fail!"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Catch-->1</div>
        <div class="line">Catch-->2</div>
        <div class="line">Catch completed</div>
    </div>
    <p>
        The example above will catch and swallow all types of exceptions. This is somewhat
        equivalent to the following with SEH:
    </p>
    <pre class="csharpcode">
        try
        {
            DoSomeWork();
        }
        catch
        {
        }
    </pre>
    <p>
        Just as it is generally avoided in SEH, you probably also want to limit your use
        of swallowing errors in Rx. You may, however, have a specific exception you want
        to handle. Catch has an overload that enables you specify the type of exception.
        Just as the following code would allow you to catch a <em>TimeoutException</em>:
    </p>
    <pre class="csharpcode">
        try
        {
            //
        }
        catch (TimeoutException tx)
        {
            //
        }
    </pre>
    <p>
        Rx also offers an overload of <em>Catch</em> to cater for this.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TSource&gt; Catch&lt;TSource, TException&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TException, IObservable&lt;TSource&gt;&gt; handler) 
            where TException : Exception
        {
            ...
        }
    </pre>
    <p>
        The following Rx code allows you to catch a <em>TimeoutException</em>. Instead of
        providing a second sequence, we provide a function that takes the exception and
        returns a sequence. This allows you to use a factory to create your continuation.
        In this example, we add the value -1 to the error sequence and then complete it.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        var result = source.Catch&lt;int, TimeoutException&gt;(tx=&gt;Observable.Return(-1));

        result.Dump("Catch");

        source.OnNext(1);
        source.OnNext(2);
        source.OnError(new TimeoutException());
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Catch-->1</div>
        <div class="line">Catch-->2</div>
        <div class="line">Catch-->-1</div>
        <div class="line">Catch completed</div>
    </div>
    <p>
        If the sequence was to terminate with an <em>Exception</em> that could not be cast
        to a <em>TimeoutException</em>, then the error would not be caught and would flow
        through to the subscriber.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        var result = source.Catch&lt;int, TimeoutException&gt;(tx=&gt;Observable.Return(-1));

        result.Dump("Catch");

        source.OnNext(1);
        source.OnNext(2);
        source.OnError(new ArgumentException("Fail!"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Catch-->1</div>
        <div class="line">Catch-->2</div>
        <div class="line">Catch failed-->Fail!</div>
    </div>
    <a name="Finally"></a>
    <h3>Finally</h3>
    <p>
        Similar to the <code>finally</code> statement with SEH, Rx exposes the ability to
        execute code on completion of a sequence, regardless of how it terminates. The <em>Finally</em>
        extension method accepts an <em>Action</em> as a parameter. This <em>Action</em>
        will be invoked if the sequence terminates normally or erroneously, or if the subscription
        is disposed of.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TSource&gt; Finally&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, 
            Action finallyAction)
        {
            ...
        }
    </pre>
    <p>
        In this example, we have a sequence that completes. We provide an action and see
        that it is called after our <code>OnCompleted</code> handler.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        var result = source.Finally(() =&gt; Console.WriteLine("Finally action ran"));
        result.Dump("Finally");
        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);
        source.OnCompleted();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Finally-->1</div>
        <div class="line">Finally-->2</div>
        <div class="line">Finally-->3</div>
        <div class="line">Finally completed</div>
        <div class="line">Finally action ran</div>
    </div>
    <p>
        In contrast, the source sequence could have terminated with an exception. In that
        case, the exception would have been sent to the console, and then the delegate we provided
        would have been executed.
    </p>
    <p>
        Alternatively, we could have disposed of our subscription. In the next example,
        we see that the <em>Finally</em> action is invoked even though the sequence does
        not complete.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        var result = source.Finally(() =&gt; Console.WriteLine("Finally"));
        var subscription = result.Subscribe(
            Console.WriteLine,
            Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);
        subscription.Dispose();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">Finally</div>
    </div>
    <p>
        Note that there is an anomaly in the current implementation of <em>Finally</em>.
        If there is no <em>OnError</em> handler provided, the error will be promoted to
        an exception and thrown. This will be done before the <em>Finally</em> action is
        invoked. We can reproduce this behavior easily by removing the <em>OnError</em>
        handler from our examples above.
    </p>
    <pre class="csharpcode">
        var source = new Subject&lt;int&gt;();
        var result = source.Finally(() =&gt; Console.WriteLine("Finally"));
        result.Subscribe(
            Console.WriteLine,
            //Console.WriteLine,
            () =&gt; Console.WriteLine("Completed"));
        source.OnNext(1);
        source.OnNext(2);
        source.OnNext(3);
        //Brings the app down. Finally action is not called.
        source.OnError(new Exception("Fail"));
    </pre>
    <p>
        Hopefully this will be identified as a bug and fixed by the time you read this in
        the next release of Rx. Out of academic interest, here is a sample of a <em>Finally</em>
        extension method that would work as expected.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;T&gt; MyFinally&lt;T&gt;(
            this IObservable&lt;T&gt; source, 
            Action finallyAction)
        {
            return Observable.Create&lt;T&gt;(o =&gt;
            {
                var finallyOnce = Disposable.Create(finallyAction);
                var subscription = source.Subscribe(
                    o.OnNext,
                    ex =&gt;
                    {
                        try { o.OnError(ex); }
                        finally { finallyOnce.Dispose(); }
                    },
                    () =&gt;
                    {
                        try { o.OnCompleted(); }
                        finally { finallyOnce.Dispose(); }
                    });

                return new CompositeDisposable(subscription, finallyOnce);
            });
        }
    </pre>
    <a name="Using"></a>
    <h3>Using</h3>
    <p>
        The <em>Using</em> factory method allows you to bind the lifetime of a resource
        to the lifetime of an observable sequence. The signature itself takes two factory
        methods; one to provide the resource and one to provide the sequence. This allows
        everything to be lazily evaluated.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TSource&gt; Using&lt;TSource, TResource&gt;(
            Func&lt;TResource&gt; resourceFactory, 
            Func&lt;TResource, IObservable&lt;TSource&gt;&gt; observableFactory) 
            where TResource : IDisposable
        {
            ...
        }
    </pre>
    <p>
        The <em>Using</em> method will invoke both the factories when you subscribe to the
        sequence. The resource will be disposed of when the sequence is terminated gracefully,
        terminated erroneously or when the subscription is disposed.
    </p>
    <p>
        To provide an example, we will reintroduce the <em>TimeIt</em> class from <a href="03_LifetimeManagement.html#IDisposable">
            Chapter 3</a>. I could use this handy little class to time the duration of a
        subscription. In the next example we create an observable sequence with the <em>Using</em>
        factory method. We provide a factory for a <em>TimeIt</em> resource and a function
        that returns a sequence.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1));
        var result = Observable.Using(
            () =&gt; new TimeIt("Subscription Timer"),
            timeIt =&gt; source);
        result.Take(5).Dump("Using");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Using-->0</div>
        <div class="line">Using-->1</div>
        <div class="line">Using-->2</div>
        <div class="line">Using-->3</div>
        <div class="line">Using-->4</div>
        <div class="line">Using completed</div>
        <div class="line">Subscription Timer took 00:00:05.0138199</div>
    </div>
    <p>
        Due to the <code>Take(5)</code> decorator, the sequence completes after five elements
        and thus the subscription is disposed of. Along with the subscription, the <em>TimeIt</em>
        resource is also disposed of, which invokes the logging of the elapsed time.
    </p>
    <p>
        This mechanism can find varied practical applications in the hands of an imaginative
        developer. The resource being an <em>IDisposable</em> is convenient; indeed, it
        makes it so that many types of resources can be bound to, such as other subscriptions,
        stream reader/writers, database connections, user controls and, with <em>Disposable.Create(Action)</em>,
        virtually anything else.
    </p>
    <a name="OnErrorResumeNext"></a>
    <h3>OnErrorResumeNext</h3>
    <p>
        Just the title of this section will send a shudder down the spines of old VB
        developers! In Rx, there is an extension method called <em>OnErrorResumeNext</em>
        that has similar semantics to the VB keywords/statement that share the same name.
        This extension method allows the continuation of a sequence with another sequence
        regardless of whether the first sequence completes gracefully or due to an error.
        Under normal use, the two sequences would merge as below:
    </p>
    <div class="marble">
        <pre class="line">S1--0--0--|</pre>
        <pre class="line">S2        --0--|</pre>
        <pre class="line">R --0--0----0--|</pre>
    </div>
    <p>
        In the event of a failure in the first sequence, then the sequences would still
        merge:
    </p>
    <div class="marble">
        <pre class="line">S1--0--0--X</pre>
        <pre class="line">S2        --0--|</pre>
        <pre class="line">R --0--0----0--|</pre>
    </div>
    <p>
        The overloads to <em>OnErrorResumeNext</em> are as follows:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TSource&gt; OnErrorResumeNext&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; first, 
            IObservable&lt;TSource&gt; second)
        {
            ..
        }
        
        public static IObservable&lt;TSource&gt; OnErrorResumeNext&lt;TSource&gt;(
            params IObservable&lt;TSource&gt;[] sources)
        {
            ...
        }
        
        public static IObservable&lt;TSource&gt; OnErrorResumeNext&lt;TSource&gt;(
            this IEnumerable&lt;IObservable&lt;TSource&gt;&gt; sources)
        {
            ...
        }
    </pre>
    <p>
        It is simple to use; you can pass in as many continuations sequences as you like
        using the various overloads. Usage should be limited however. Just as the <em>OnErrorResumeNext</em>
        keyword warranted mindful use in VB, so should it be used with caution in Rx. It
        will swallow exceptions quietly and can leave your program in an unknown state.
        Generally, this will make your code harder to maintain and debug.
    </p>
    <a name="Retry"></a>
    <h3>Retry</h3>
    <p>
        If you are expecting your sequence to encounter predictable issues, you might simply
        want to retry. One such example when you want to retry is when performing I/O (such
        as web request or disk access). I/O is notorious for intermittent failures. The
        <em>Retry</em> extension method offers the ability to retry on failure a specified
        number of times or until it succeeds.
    </p>
    <pre class="csharpcode">
        //Repeats the source observable sequence until it successfully terminates.
        public static IObservable&lt;TSource&gt; Retry&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source)
        {
            ...
        }
        
        // Repeats the source observable sequence the specified number of times or until it 
        //  successfully terminates.
        public static IObservable&lt;TSource&gt; Retry&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source, int retryCount)
        {
            ...
        }
    </pre>
    <p>
        In the diagram below, the sequence (S) produces values then fails. It is re-subscribed,
        after which it produces values and fails again; this happens a total of two times.
        The result sequence (R) is the concatenation of all the successive subscriptions
        to (S).
    </p>
    <div class="marble">
        <pre class="line">S --1--2--X</pre>
        <pre class="line">            --1--2--3--X</pre>
        <pre class="line">                         --1</pre>
        <pre class="line">R --1--2------1--2--3------1</pre>
    </div>
    <p>
        In the next example, we just use the simple overload that will always retry on any
        exception.
    </p>
    <pre class="csharpcode">
        public static void RetrySample&lt;T&gt;(IObservable&lt;T&gt; source)
        {
            source.Retry().Subscribe(t=&gt;Console.WriteLine(t)); //Will always retry
            Console.ReadKey();
        }
    </pre>
    <p>
        Given the source [0,1,2,X], the output would be:
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
    </div>
    <p>
        This output would continue forever, as we throw away the token from the subscribe
        method. As a marble diagram it would look like this:
    </p>
    <div class="marble">
        <pre class="line">S--0--1--2--x</pre>
        <pre class="line">             --0--1--2--x</pre>
        <pre class="line">                         --0--</pre>
        <pre class="line">R--0--1--2-----0--1--2-----0--</pre>
    </div>
    <p>
        Alternatively, we can specify the maximum number of retries. In this example, we
        only retry once, therefore the error that gets published on the second subscription
        will be passed up to the final subscription. Note that to retry once you pass a
        value of 2. Maybe the method should have been called <em>Try</em>?
    </p>
    <pre class="csharpcode">
        source.Retry(2).Dump("Retry(2)"); 
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Retry(2)-->0</div>
        <div class="line">Retry(2)-->1</div>
        <div class="line">Retry(2)-->2</div>
        <div class="line">Retry(2)-->0</div>
        <div class="line">Retry(2)-->1</div>
        <div class="line">Retry(2)-->2</div>
        <div class="line">Retry(2) failed-->Test Exception</div>
    </div>
    <p>
        As a marble diagram, this would look like:</p>
    <div class="marble">
        <pre class="line">S--0--1--2--x</pre>
        <pre class="line">             --0--1--2--x</pre>
        <pre class="line">R--0--1--2-----0--1--2--x</pre>
    </div>
    <p>
        Proper care should be taken when using the infinite repeat overload. Obviously
        if there is a persistent problem with your underlying sequence, you may find yourself
        stuck in an infinite loop. Also, take note that there is no overload that allows
        you to specify the type of exception to retry on.
    </p>
    <p>
        A useful extension method to add to your own library might be a "Back Off and Retry"
        method. The teams I have worked with have found such a feature useful when performing
        I/O, especially network requests. The concept is to try, and on failure wait for
        a given period of time and then try again. Your version of this method may take
        into account the type of <em>Exception</em> you want to retry on, as well as the
        maximum number of times to retry. You may even want to lengthen the to wait period
        to be less aggressive on each subsequent retry.
    </p>
    <!--TODO: Build BackOffRetry with the reader-->
    <p>
        Requirements for exception management that go beyond simple <em>OnError</em> handlers
        are commonplace. Rx delivers the basic exception handling operators which you can
        use to compose complex and robust queries. In this chapter we have covered advanced
        error handling and some more resource management features from Rx. We looked at
        the <em>Catch</em>, <em>Finally</em> and <em>Using</em> methods as well as the other
        methods like <em>OnErrorResumeNext</em> and <em>Retry</em>, that allow you to play
        a little 'fast and loose'. We have also revisited the use of marble diagrams to help
        us visualize the combination of multiple sequences. This will help us in our next
        chapter where we will look at other ways of composing and aggregating observable
        sequences.
    </p>
    <hr />
    <div class="webonly">
        <h1 class="ignoreToc">Additional recommended reading</h1>
        <div align="center">
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--Domain Driven Design (Kindle) Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B00794TAUG&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--Purely functional data structures Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=0521663504&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
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
                <!--Real-world functional programming Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=1933988924&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;margin: 10px" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>                  
        </div>    </div>
</body>
</html>
