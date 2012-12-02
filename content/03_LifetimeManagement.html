<?xml version="1.0" encoding="utf-8" ?>
<html>
    <head>
        <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
        <title>Intro to Rx - Lifetime management</title>
        <link rel="stylesheet" href="Kindle.css" type="text/css" />
    </head>
    <!--TODO: Link on this page to the C# in a nutshell(?) Amazon via affiliates -->
<!--
    LC.
        re read apply laws of that kindle book.
        Create a Proposition and support it or create and argument and prove it.
        Beginning middle and end. Beginning should give and idea to the destination, should not be a surprise.
        Code and keyword highlights seem inconsistent.
        
        Proposition is that you can explicitly manage the lifetime of your subscriptions to queries.
        

    G.A.
        (overall pretty focused and flows from one point to the next competently; would be even better with some rewriting)
 
    Subscription finalizers
        A caveat is a negative notice, a warning  that doesn't work here.
-->
<body>
    <a name="LifetimeManagement"></a>
    <h1>Lifetime management</h1>
    <p>
        The very nature of Rx code is that you as a consumer do not know when a sequence
        will provide values or terminate. This uncertainty does not prevent your code from
        providing a level of certainty. You can control when you will start accepting values
        and when you choose to stop accepting values. You still need to be the master of
        your domain. Understanding the basics of managing Rx resources allow your applications
        to be as efficient, bug free and predictable as possible.
    </p>
    <p>
        Rx provides fine grained control to the lifetime of subscriptions to queries. While using
        familiar interfaces, you can deterministically release resources associated to queries.
        This allows you to make the decisions on how to most effectively manage your resources,
        ideally keeping the scope as tight as possible.
    </p>
    <p>
        In the previous chapter we introduced you to the key types and got off the ground
        with some examples. For the sake of keeping the initial samples simple we ignored
        a very important part of the <em>IObservable&lt;T&gt;</em> interface. The <em>Subscribe</em>
        method takes an <em>IObserver&lt;T&gt;</em> parameter, but we did not need to provide
        that as we used the extension method that took an <em>Action&lt;T&gt;</em> instead.
        The important part we overlooked is that both <em>Subscribe</em> methods have a
        return value. The return type is <em>IDisposable</em>. In this chapter we will further
        explore how this return value can be used to management lifetime of our subscriptions.
    </p>
    <a name="Subscribe"></a>
    <h2>Subscribing</h2>
    <p>
        Just before we move on, it is worth briefly looking at all of the overloads of the
        <em>Subscribe</em> extension method. The overload we used in the previous chapter
        was the simple <a title="Subscribe Extension method overloads on MSDN" href="http://msdn.microsoft.com/en-us/library/ff626574(v=VS.92).aspx">
            Overload to Subscribe</a> which allowed us to pass just an <em>Action&lt;T&gt;</em>
        to be performed when <em>OnNext</em> was invoked. Each of these further overloads
        allows you to avoid having to create and then pass in an instance of <em>IObserver&lt;T&gt;</em>.
    </p>
    <pre class="csharpcode">
        //Just subscribes to the Observable for its side effects. 
        // All OnNext and OnCompleted notifications are ignored.
        // OnError notifications are re-thrown as Exceptions.
        IDisposable Subscribe&lt;TSource&gt;(this IObservable&lt;TSource&gt; source);
            
        //The onNext Action provided is invoked for each value.
        //OnError notifications are re-thrown as Exceptions.
        IDisposable Subscribe&lt;TSource&gt;(this IObservable&lt;TSource&gt; source, 
            Action&lt;TSource&gt; onNext);
            
        //The onNext Action is invoked for each value.
        //The onError Action is invoked for errors
        IDisposable Subscribe&lt;TSource&gt;(this IObservable&lt;TSource&gt; source, 
            Action&lt;TSource&gt; onNext, 
            Action&lt;Exception&gt; onError);
            
        //The onNext Action is invoked for each value.
        //The onCompleted Action is invoked when the source completes.
        //OnError notifications are re-thrown as Exceptions.
        IDisposable Subscribe&lt;TSource&gt;(this IObservable&lt;TSource&gt; source, 
            Action&lt;TSource&gt; onNext, 
            Action onCompleted);
            
        //The complete implementation
        IDisposable Subscribe&lt;TSource&gt;(this IObservable&lt;TSource&gt; source, 
            Action&lt;TSource&gt; onNext, 
            Action&lt;Exception&gt; onError, 
            Action onCompleted);
    </pre>
    <p>
        Each of these overloads allows you to pass various combinations of delegates that
        you want executed for each of the notifications an <em>IObservable&lt;T&gt;</em>
        instance could produce. A key point to note is that if you use an overload that
        does not specify a delegate for the <em>OnError</em> notification, any <em>OnError</em>
        notifications will be re-thrown as an exception. Considering that the error could
        be raised at any time, this can make debugging quite difficult. It is normally best
        to use an overload that specifies a delegate to cater for <em>OnError</em> notifications.
    </p>
    <p>
        In this example we attempt to catch error using standard .NET Structured Exception
        Handling:
    </p>
    <pre class="csharpcode">
        var values = new Subject&lt;int&gt;();
        try
        {
            values.Subscribe(value => Console.WriteLine("1st subscription received {0}", value));
        }
        catch (Exception ex)
        {
            Console.WriteLine("Won't catch anything here!");
        }
            
        values.OnNext(0);
        //Exception will be thrown here causing the app to fail.
        values.OnError(new Exception("Dummy exception"));
    </pre>
    <p>
        The correct way to way to handle exceptions is to provide a delegate for <em>OnError</em>
        notifications as in this example.
    </p>
    <pre class="csharpcode">
        var values = new Subject&lt;int&gt;();
            
        values.Subscribe(
            value => Console.WriteLine("1st subscription received {0}", value),
            ex => Console.WriteLine("Caught an exception : {0}", ex));

        values.OnNext(0);
        values.OnError(new Exception("Dummy exception"));
    </pre>
    <p>
        We will look at other interesting ways to deal with errors on a sequence in later
        chapters in the book.
    </p>
    <a name="Unsubscribing"></a>
    <h2>Unsubscribing</h2>
    <p>
        We have yet to look at how we could unsubscribe from a subscription. If you were
        to look for an <i>Unsubscribe</i> method in the Rx public API you would not find
        any. Instead of supplying an Unsubscribe method, Rx will return an <em>IDisposable</em>
        whenever a subscription is made. This disposable can be thought of as the subscription
        itself, or perhaps a token representing the subscription. Disposing it will dispose
        the subscription and effectively <em>unsubscribe</em>. Note that calling <em>Dispose</em>
        on the result of a Subscribe call will not cause any side effects for other subscribers;
        it just removes the subscription from the observable's internal list of subscriptions.
        This then allows us to call <em>Subscribe</em> many times on a single <em>IObservable&lt;T&gt;</em>,
        allowing subscriptions to come and go without affecting each other. In this example
        we initially have two subscriptions, we then dispose of one subscription early which
        still allows the other to continue to receive publications from the underlying sequence:
    </p>
    <pre class="csharpcode">
        var values = new Subject&lt;int&gt;();
        var firstSubscription = values.Subscribe(value =&gt; 
            Console.WriteLine("1st subscription received {0}", value));
        var secondSubscription = values.Subscribe(value =&gt; 
            Console.WriteLine("2nd subscription received {0}", value));
        values.OnNext(0);
        values.OnNext(1);
        values.OnNext(2);
        values.OnNext(3);
        firstSubscription.Dispose();
        Console.WriteLine("Disposed of 1st subscription");
        values.OnNext(4);
        values.OnNext(5);
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">1st subscription received 0</div>
        <div class="line">2nd subscription received 0</div>
        <div class="line">1st subscription received 1</div>
        <div class="line">2nd subscription received 1</div>
        <div class="line">1st subscription received 2</div>
        <div class="line">2nd subscription received 2</div>
        <div class="line">1st subscription received 3</div>
        <div class="line">2nd subscription received 3</div>
        <div class="line">Disposed of 1st subscription</div>
        <div class="line">2nd subscription received 4</div>
        <div class="line">2nd subscription received 5</div>
    </div>
    <p>
        The team building Rx could have created a new interface like <i>ISubscription</i>
        or <i>IUnsubscribe</i> to facilitate unsubscribing. They could have added an <i>Unsubscribe</i>
        method to the existing <em>IObservable&lt;T&gt;</em> interface. By using the <em>IDisposable</em>
        type instead we get the following benefits for free:
    </p>
    <ul>
        <li>The type already exists </li>
        <li>People understand the type </li>
        <li><em>IDisposable</em> has standard usages and patterns </li>
        <li>Language support via the <em>using</em> keyword </li>
        <li>Static analysis tools like FxCop can help you with its usage </li>
        <li>The <em>IObservable&lt;T&gt;</em> interface remains very simple.</li>
    </ul>
    <p>
        As per the <em>IDisposable</em> guidelines, you can call <em>Dispose</em> as many
        times as you like. The first call will unsubscribe and any further calls will do
        nothing as the subscription will have already been disposed.
    </p>
    <a name="OnErrorAndOnCompleted"></a>
    <h2>OnError and OnCompleted</h2>
    <p>
        Both the <em>OnError</em> and <em>OnCompleted</em> signify the completion of a sequence.
        If your sequence publishes an <em>OnError</em> or <em>OnCompleted</em> it will be
        the last publication and no further calls to <em>OnNext</em> can be performed. In
        this example we try to publish an <em>OnNext</em> call after an <em>OnCompleted</em>
        and the <em>OnNext</em> is ignored:
    </p>
    <pre class="csharpcode">
        var subject = new Subject&lt;int&gt;();
        subject.Subscribe(
            Console.WriteLine, 
            () =&gt; Console.WriteLine("Completed"));
        subject.OnCompleted();
        subject.OnNext(2);
    </pre>
    <p>
        Of course, you could implement your own <em>IObservable&lt;T&gt;</em> that allows
        publishing after an <em>OnCompleted</em> or an <em>OnError</em>, however it would
        not follow the precedence of the current Subject types and would be a non-standard
        implementation. I think it would be safe to say that the inconsistent behavior would
        cause unpredictable behavior in the applications that consumed your code.
    </p>
    <p>
        An interesting thing to consider is that when a sequence completes or errors, you
        should still dispose of your subscription.
    </p>
    <a name="IDisposable"></a>
    <h2>IDisposable</h2>
    <p>
        The <em>IDisposable</em> interface is a handy type to have around and it is also
        integral to Rx. I like to think of types that implement <em>IDisposable</em> as
        having explicit lifetime management. I should be able to say "I am done with that"
        by calling the <em>Dispose()</em> method.
    </p>
    <p>
        By applying this kind of thinking, and then leveraging the C# <code>using</code>
        statement, you can create handy ways to create scope. As a reminder, the <code>using</code>
        statement is effectively a <code>try</code>/<code>finally</code> block that will
        always call <em>Dispose</em> on your instance when leaving the scope.
    </p>
    <p>
        If we consider that we can use the <em>IDisposable</em> interface to effectively create a
        scope, you can create some fun little classes to leverage this. For example here
        is a simple class to log timing events:
    </p>
    <pre class="csharpcode">
        public class TimeIt : IDisposable
        {
            private readonly string _name;
            private readonly Stopwatch _watch;

            public TimeIt(string name)
            {
                _name = name;
                _watch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _watch.Stop();
                Console.WriteLine("{0} took {1}", _name, _watch.Elapsed);
            }
        }
    </pre>
    <p>
        This handy little class allows you to create scope and measure the time certain
        sections of your code base take to run. You could use it like this:
    </p>
    <pre class="csharpcode">
        using (new TimeIt("Outer scope"))
        {
            using (new TimeIt("Inner scope A"))
            {
                DoSomeWork("A");
            }
            using (new TimeIt("Inner scope B"))
            {
                DoSomeWork("B");
            }
            Cleanup();
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Inner scope A took 00:00:01.0000000</div>
        <div class="line">Inner scope B took 00:00:01.5000000</div>
        <div class="line">Outer scope took 00:00:02.8000000</div>
    </div>
    <p>
        You could also use the concept to set the color of text in a console application:
    </p>
    <pre class="csharpcode">
        //Creates a scope for a console foreground color. When disposed, will return to 
        //  the previous Console.ForegroundColor
        public class ConsoleColor : IDisposable
        {
            private readonly System.ConsoleColor _previousColor;

            public ConsoleColor(System.ConsoleColor color)
            {
                _previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color;
            }

            public void Dispose()
            {
                Console.ForegroundColor = _previousColor;
            }
        }
    </pre>
    <p>
        I find this handy for easily switching between colors in little <i>spike</i> console
        applications:
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Normal color");
        using (new ConsoleColor(System.ConsoleColor.Red))
        {
            Console.WriteLine("Now I am Red");
            using (new ConsoleColor(System.ConsoleColor.Green))
            {
                Console.WriteLine("Now I am Green");
            }
            Console.WriteLine("and back to Red");
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line" style="color: #C0C0C0;">Normal color</div>
        <div class="line" style="color: #FF0000;">Now I am Red</div>
        <div class="line" style="color: #00FF00;">Now I am Green</div>
        <div class="line" style="color: #FF0000;">and back to Red</div>
    </div>
    <p>
        So we can see that you can use the <em>IDisposable</em> interface for more than
        just common use of deterministically releasing unmanaged resources. It is a useful
        tool for managing lifetime or scope of anything; from a stopwatch timer, to the current
        color of the console text, to the subscription to a sequence of notifications.
    </p>
    <p>
        The Rx library itself adopts this liberal usage of the <em>IDisposable</em> interface
        and introduces several of its own custom implementations:
    </p>
    <ul>
        <li>Disposable</li>
        <li>BooleanDisposable</li>
        <li>CancellationDisposable</li>
        <li>CompositeDisposable</li>
        <li>ContextDisposable</li>
        <li>MultipleAssignmentDisposable</li>
        <li>RefCountDisposable</li>
        <li>ScheduledDisposable</li>
        <li>SerialDisposable</li>
        <li>SingleAssignmentDisposable</li>
    </ul>
    <p>
        For a full rundown of each of the implementations see the <a href="20_Disposables.html">
            Disposables</a> reference in the Appendix. For now we will look at the extremely
        simple and useful <em>Disposable</em> static class:
    </p>
    <pre class="csharpcode">
        namespace System.Reactive.Disposables
        {
          public static class Disposable
          {
            // Gets the disposable that does nothing when disposed.
            public static IDisposable Empty { get {...} }

            // Creates the disposable that invokes the specified action when disposed.
            public static IDisposable Create(Action dispose)
            {...}
          }
        }
    </pre>
    <p>
        As you can see it exposes two members: <em>Empty</em> and <em>Create</em>. The <em>Empty</em>
        method allows you get a stub instance of an <em>IDisposable</em> that does nothing
        when <code>Dispose()</code> is called. This is useful for when you need to fulfil
        an interface requirement that returns an <em>IDisposable</em> but you have no specific
        implementation that is relevant.
    </p>
    <p>
        The other overload is the <em>Create</em> factory method which allows you to pass
        an <em>Action</em> to be invoked when the instance is disposed. The <em>Create</em>
        method will ensure the standard Dispose semantics, so calling <code>Dispose()</code>
        multiple times will only invoke the delegate you provide once:
    </p>
    <pre class="csharpcode">
        var disposable = Disposable.Create(() => Console.WriteLine("Being disposed."));
        Console.WriteLine("Calling dispose...");
        disposable.Dispose();
        Console.WriteLine("Calling again...");
        disposable.Dispose();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Calling dispose...</div>
        <div class="line">Being disposed.</div>
        <div class="line">Calling again...</div>
    </div>
    <p>
        Note that "Being disposed." is only printed once. In a later chapter we cover another
        useful method for binding the lifetime of a resource to that of a subscription in
        the <a href="11_AdvancedErrorHandling.html#Using">Observable.Using</a> method.
    </p>
    <!--
G.A.
    Subscription finalizers
     A caveat is a negative notice, a warning  that doesn't work here.
-->
    <a name="Finalizers"></a>
    <h2>Resource management vs. memory management</h2>
    <p>
        It seems many .NET developers only have a vague understanding of the .NET runtime's
        Garbage Collector and specifically how it interacts with Finalizers and <em>IDisposable</em>.
        As the author of the <a href="http://msdn.microsoft.com/en-us/library/ms229042.aspx">
            Framework Design Guidelines</a> points out, this may be due to the confusion
        between 'resource management' and 'memory management':
    </p>
    <p class="comment">
        Many people who hear about the Dispose pattern for the first time complain that
        the GC isn't doing its job. They think it should collect resources, and that this
        is just like having to manage resources as you did in the unmanaged world. The truth
        is that the GC was never meant to manage resources. It was designed to manage memory
        and it is excellent in doing just that. - <a href="http://blogs.msdn.com/b/kcwalina/">
            Krzysztof Cwalina</a> from <a href="http://www.bluebytesoftware.com/blog/2005/04/08/DGUpdateDisposeFinalizationAndResourceManagement.aspx">
                Joe Duffy's blog</a>
    </p>
    <p>
        This is both a testament to Microsoft for making .NET so easy to work with and also
        a problem as it is a key part of the runtime to misunderstand. Considering this,
        I thought it was prudent to note that <i>subscriptions will not be automatically disposed
            of</i>. You can safely assume that the instance of <em>IDisposable</em> that
        is returned to you does not have a finalizer and will not be collected when it goes
        out of scope. If you call a <em>Subscribe</em> method and ignore the return value,
        you have lost your only handle to unsubscribe. The subscription will still exist,
        and you have effectively lost access to this resource, which could result in leaking
        memory and running unwanted processes.
    </p>
    <p>
        The exception to this cautionary note is when using the <em>Subscribe</em> extension
        methods. These methods will internally construct behavior that will <i>automatically
            detach</i> subscriptions when the sequence completes or errors. Even with the
        automatic detach behavior; you still need to consider sequences that never terminate
        (by <em>OnCompleted</em> or <em>OnError</em>). You will need the instance of <em>IDisposable</em>
        to terminate the subscription to these infinite sequences explicitly.
    </p>
    <p class="comment">
        You will find many of the examples in this book will not allocate the <em>IDisposable</em>
        return value. This is only for brevity and clarity of the sample. <a href="18_UsageGuidelines.html">
            Usage guidelines</a> and best practice information can be found in the appendix.
    </p>
    <p>
        By leveraging the common <em>IDisposable</em> interface, Rx offers the ability to
        have deterministic control over the lifetime of your subscriptions. Subscriptions
        are independent, so the disposable of one will not affect another. While some <em>Subscribe</em>
        extension methods utilize an automatically detaching observer, it is still considered
        best practice to explicitly manage your subscriptions, as you would with any other
        resource implementing <em>IDisposable</em>. As we will see in later chapters, a
        subscription may actually incur the cost of other resources such as event handles,
        caches and threads. It is also best practice to always provide an <em>OnError</em>
        handler to prevent an exception being thrown in an otherwise difficult to handle
        manner.
    </p>
    <p>
        With the knowledge of subscription lifetime management, you are able to keep a tight
        leash on subscriptions and their underlying resources. With judicious application
        of standard disposal patterns to your Rx code, you can keep your applications predictable,
        easier to maintain, easier to extend and hopefully bug free.
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
