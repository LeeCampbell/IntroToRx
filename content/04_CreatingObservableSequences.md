<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Creating a sequence</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<!--TODO: Link on this page to the CLR via C# & Concurrent Programming on Windows via amazon affiliates -->
<!--
        LC
        Introduce the concept of Lazy. Contrast this to the recently mentioned earger disposal. Lazy eval + eager disposal = miniumn resource lifetime.
        Check for And also, and then continuations through out. Ensure that we are creating sensible an obvious breaks in passage.

        G.A.
I've been up for a while so it's probably me fucking up something obvious, but the code as is gives me this error:
Cannot convert lambda expression to delegate type 'System.Func<System.IObserver<string>,System.Action>' because some of the return types in the block are not implicitly convertible to the delegate return type
 
And if I use the Action variant instead I get:
Cannot convert lambda expression to delegate type 'System.Func<System.IObserver<string>,System.IDisposable>' because some of the return types in the block are not implicitly convertible to the delegate return type
 
-->
<body>
    <a name="PART2"></a>
    <h1 class="SectionHeader">PART 2 - Sequence basics</h1>
    <p>
        So you want to get involved and write some Rx code, but how do you get started?
        We have looked at the key types, but know that we should not be creating our own
        implementations of <em>IObserver&lt;T&gt;</em> or <em>IObservable&lt;T&gt;</em>
        and should favor factory methods over using subjects. Even if we have an observable
        sequence, how do we pick out the data we want from it? We need to understand the
        basics of creating an observable sequence, getting values into it and picking out
        the values we want from them.
    </p>
    <p>
        In Part 2 we discover the basics for constructing and querying observable sequences.
        We assert that LINQ is fundamental to using and understanding Rx. On deeper inspection,
        we find that <i>functional programming</i> concepts are core to having a deep understanding
        of LINQ and therefore enabling you to master Rx. To support this understanding,
        we classify the query operators into three main groups. Each of these groups proves
        to have a root operator that the other operators can be constructed from. Not only
        will this deconstruction exercise provide a deeper insight to Rx, functional programming
        and query composition; it should arm you with the ability to create custom operators
        where the general Rx operators do not meet your needs.
    </p>
    <a name="CreationOfObservables"></a>
    <h1>Creating a sequence</h1>
    <p>
        In the previous chapters we used our first Rx extension method, the <em>Subscribe</em>
        method and its overloads. We also have seen our first factory method in <code>Subject.Create()</code>.
        We will start looking at the vast array of other methods that enrich <em>IObservable&lt;T&gt;</em>
        to make Rx what it is. It may be surprising to see that there are relatively few
        public instance methods in the Rx library. There are however a large number of public
        static methods, and more specifically, a large number of extension methods. Due
        to the large number of methods and their overloads, we will break them down into
        categories.
    </p>
    <p class="comment">
        Some readers may feel that they can skip over parts of the next few chapters. I
        would only suggest doing so if you are very confident with LINQ and functional composition.
        The intention of this book is to provide a step-by-step introduction to Rx, with
        the goal of you, the reader, being able to apply Rx to your software. The appropriate
        application of Rx will come through a sound understanding of the fundamentals of
        Rx. The most common mistakes people will make with Rx are due to a misunderstanding
        of the principles upon which Rx was built. With this in mind, I encourage you to
        read on.
    </p>
    <p>
        It seems sensible to follow on from our examination of our key types where we simply
        constructed new instances of subjects. Our first category of methods will be <i>creational</i>
        methods: simple ways we can create instances of <em>IObservable&lt;T&gt;</em> sequences.
        These methods generally take a seed to produce a sequence: either a single value
        of a type, or just the type itself. In functional programming this can be described
        as <i>anamorphism</i> or referred to as an '<i>unfold</i>'.
    </p>
    <a name="SimpleFactoryMethods"></a>
    <h2>Simple factory methods</h2>
    <a name="ObservableReturn"></a>
    <h3>Observable.Return</h3>
    <p>
        In our first and most basic example we introduce <em>Observable.Return&lt;T&gt;(T value)</em>.
        This method takes a value of T and returns an IObservable&lt;T&gt; with the single
        value and then completes. It has <i>unfolded</i> a value of <em>T</em> into an observable
        sequence.
    </p>
    <pre class="csharpcode">
        var singleValue = Observable.Return&lt;string&gt;("Value");
        //which could have also been simulated with a replay subject
        var subject = new ReplaySubject&lt;string&gt;();
        subject.OnNext("Value");
        subject.OnCompleted();
    </pre>
    <p>
        Note that in the example above that we could use the factory method or get the same
        effect by using the replay subject. The obvious difference is that the factory method
        is only one line and it allows for declarative over imperative programming style.
        In the example above we specified the type parameter as <em>string</em>, this is
        not necessary as it can be inferred from the argument provided.
    </p>
    <pre class="csharpcode">
        singleValue = Observable.Return&lt;string&gt;("Value");
        //Can be reduced to the following
        singleValue = Observable.Return("Value");
    </pre>
    <a name="ObservableEmpty"></a>
    <h3>Observable.Empty</h3>
    <p>
        The next two examples only need the type parameter to unfold into an observable
        sequence. The first is <em>Observable.Empty&lt;T&gt;()</em>. This returns an empty
        <em>IObservable&lt;T&gt;</em> i.e. it just publishes an <em>OnCompleted</em> notification.
    </p>
    <pre class="csharpcode">
        var empty = Observable.Empty&lt;string&gt;();
        //Behaviorally equivalent to
        var subject = new ReplaySubject&lt;string&gt;();
        subject.OnCompleted();
    </pre>
    <a name="ObservableNever"></a>
    <h3>Observable.Never</h3>
    <p>
        The <em>Observable.Never&lt;T&gt;()</em> method will return infinite sequence without
        any notifications.
    </p>
    <pre class="csharpcode">
        var never = Observable.Never&lt;string&gt;();
        //similar to a subject without notifications
        var subject = new Subject&lt;string&gt;();
    </pre>
    <a name="ObservableThrow"></a>
    <h3>Observable.Throw</h3>
    <p>
        <em>Observable.Throw&lt;T&gt;(Exception)</em> method needs the type parameter information,
        it also need the <em>Exception</em> that it will <em>OnError</em> with. This method
        creates a sequence with just a single <em>OnError</em> notification containing the
        exception passed to the factory.
    </p>
    <pre class="csharpcode">
        var throws = Observable.Throw&lt;string&gt;(new Exception()); 
        //Behaviorally equivalent to
        var subject = new ReplaySubject&lt;string&gt;(); 
        subject.OnError(new Exception());
    </pre>
    <a name="ObservableCreate"></a>
    <h3>Observable.Create</h3>
    <p>
        The <em>Create</em> factory method is a little different to the above creation methods.
        The method signature itself may be a bit overwhelming at first, but becomes quite
        natural once you have used it.
    </p>
    <pre class="csharpcode">
        //Creates an observable sequence from a specified Subscribe method implementation.
        public static IObservable&lt;TSource&gt; Create&lt;TSource&gt;(
            Func&lt;IObserver&lt;TSource&gt;, IDisposable&gt; subscribe)
        {...}
        public static IObservable&lt;TSource&gt; Create&lt;TSource&gt;(
            Func&lt;IObserver&lt;TSource&gt;, Action&gt; subscribe)
        {...}
    </pre>
    <p>
        Essentially this method allows you to specify a delegate that will be executed anytime
        a subscription is made. The <em>IObserver&lt;T&gt;</em> that made the subscription
        will be passed to your delegate so that you can call the <em>OnNext</em>/<em>OnError</em>/<em>OnCompleted</em>
        methods as you need. This is one of the few scenarios where you will need to concern
        yourself with the <em>IObserver&lt;T&gt;</em> interface. Your delegate is a <em>Func</em>
        that returns an <em>IDisposable</em>. This <em>IDisposable</em> will have its <em>Dispose()</em>
        method called when the subscriber disposes from their subscription.
    </p>
    <p>
        The <em>Create</em> factory method is the preferred way to implement custom observable
        sequences. The usage of subjects should largely remain in the realms of samples
        and testing. Subjects are a great way to get started with Rx. They reduce the learning
        curve for new developers, however they pose several concerns that the <em>Create</em>
        method eliminates. Rx is effectively a functional programming paradigm. Using subjects
        means we are now managing state, which is potentially mutating. Mutating state and
        asynchronous programming are very hard to get right. Furthermore many of the operators
        (extension methods) have been carefully written to ensure correct and consistent
        lifetime of subscriptions and sequences are maintained. When you introduce subjects
        you can break this. Future releases may also see significant performance degradation
        if you explicitly use subjects.
    </p>
    <p>
        The <em>Create</em> method is also preferred over creating custom types that implement
        the <em>IObservable</em> interface. There really is no need to implement the observer/observable
        interfaces yourself. Rx tackles the intricacies that you may not think of such as
        thread safety of notifications and subscriptions.
    </p>
    <p>
        A significant benefit that the <em>Create</em> method has over subjects is that
        the sequence will be lazily evaluated. Lazy evaluation is a very important part
        of Rx. It opens doors to other powerful features such as scheduling and combination
        of sequences that we will see later. The delegate will only be invoked when a subscription
        is made.
    </p>
    <p>
        In this example we show how we might first return a sequence via standard blocking
        eagerly evaluated call, and then we show the correct way to return an observable
        sequence without blocking by lazy evaluation.
    </p>
    <pre class="csharpcode">
        private IObservable&lt;string&gt; BlockingMethod()
        {
            var subject = new ReplaySubject&lt;string&gt;();
            subject.OnNext("a");
            subject.OnNext("b");
            subject.OnCompleted();
            Thread.Sleep(1000);
            return subject;
        }
        private IObservable&lt;string&gt; NonBlocking()
        {
            return Observable.Create&lt;string&gt;(
                (IObserver&lt;string&gt; observer) =&gt;
                {
                    observer.OnNext("a");
                    observer.OnNext("b");
                    observer.OnCompleted();
                    Thread.Sleep(1000);
                    return Disposable.Create(() =&gt; Console.WriteLine("Observer has unsubscribed"));
                    //or can return an Action like 
                    //return () =&gt; Console.WriteLine("Observer has unsubscribed"); 
                });
        }
    </pre>
    <p>
        While the examples are somewhat contrived, the intention is to show that when a
        consumer calls the eagerly evaluated, blocking method, they will be blocked for
        at least 1 second before they even receive the <em>IObservable&lt;string&gt;</em>,
        regardless of if they do actually subscribe to it or not. The non blocking method
        is lazily evaluated so the consumer immediately receives the <em>IObservable&lt;string&gt;</em>
        and will only incur the cost of the thread sleep if they subscribe.
    </p>
    <p>
        As an exercise, try to build the <em>Empty</em>, <em>Return</em>, <em>Never</em>
        &amp; <em>Throw</em> extension methods yourself using the <em>Create</em> method.
        If you have Visual Studio or <a href="http://www.linqpad.net/">LINQPad</a> available to you right now, code it up as quickly
        as you can. If you don't (perhaps you are on the train on the way to work), try
        to conceptualize how you would solve this problem. When you are done move forward
        to see some examples of how it could be done...
    </p>
    <hr style="page-break-after: always" />
    <p>
        Examples of <em>Empty</em>, <em>Return</em>, <em>Never</em> and <em>Throw</em> recreated
        with <em>Observable.Create</em>:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;T&gt; Empty&lt;T&gt;()
        {
            return Observable.Create&lt;T&gt;(o =&gt;
            {
                o.OnCompleted();
                return Disposable.Empty;
            });
        }
        public static IObservable&lt;T&gt; Return&lt;T&gt;(T value)
        {
            return Observable.Create&lt;T&gt;(o =&gt;
            {
                o.OnNext(value);
                o.OnCompleted();
                return Disposable.Empty;
            });
        }
        public static IObservable&lt;T&gt; Never&lt;T&gt;()
        {
            return Observable.Create&lt;T&gt;(o =&gt;
            {
                return Disposable.Empty;
            });
        }
        public static IObservable&lt;T&gt; Throws&lt;T&gt;(Exception exception)
        {
            return Observable.Create&lt;T&gt;(o =&gt;
            {
                o.OnError(exception);
                return Disposable.Empty;
            });
        }
    </pre>
    <p>
        You can see that <em>Observable.Create</em> provides the power to build our own
        factory methods if we wish. You may have noticed that in each of the examples we
        only are able to return our subscription token (the implementation of <em>IDisposable</em>)
        once we have produced all of our <em>OnNext</em> notifications. This is because
        inside of the delegate we provide, we are completely sequential. It also makes the
        token rather pointless. Now we look at how we can use the return value in a more
        useful way. First is an example where inside our delegate we create a Timer that
        will call the observer's <em>OnNext</em> each time the timer ticks.
    </p>
    <pre class="csharpcode">
        //Example code only
        public void NonBlocking_event_driven()
        {
            var ob = Observable.Create&lt;string&gt;(
                observer =>
                {
                    var timer = new System.Timers.Timer();
                    timer.Interval = 1000;
                    timer.Elapsed += (s, e) => observer.OnNext("tick");
                    timer.Elapsed += OnTimerElapsed;
                    timer.Start();
                    
                    return Disposable.Empty;
                });
            var subscription = ob.Subscribe(Console.WriteLine);
            Console.ReadLine();
            subscription.Dispose();
        }
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine(e.SignalTime);
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">tick</div>
        <div class="line">01/01/2012 12:00:00</div>
        <div class="line">tick</div>
        <div class="line">01/01/2012 12:00:01</div>
        <div class="line">tick</div>
        <div class="line">01/01/2012 12:00:02</div>
        <div class="line"></div>
        <div class="line">01/01/2012 12:00:03</div>
        <div class="line">01/01/2012 12:00:04</div>
        <div class="line">01/01/2012 12:00:05</div>
    </div>
    <p>
        The example above is broken. When we dispose of our subscription, we will stop seeing
        "tick" being written to the screen; however we have not released our second event
        handler "<code>OnTimerElasped</code>" and have not disposed of the instance of the
        timer, so it will still be writing the <code>ElapsedEventArgs.SignalTime</code>
        to the console after our disposal. The extremely simple fix is to return <code>timer</code>
        as the <em>IDisposable</em> token.
    </p>
    <pre class="csharpcode">
        //Example code only
        var ob = Observable.Create&lt;string&gt;(
            observer =>
            {
                var timer = new System.Timers.Timer();
                timer.Interval = 1000;
                timer.Elapsed += (s, e) => observer.OnNext("tick");
                timer.Elapsed += OnTimerElapsed;
                timer.Start();
                    
                return timer;
            });
    </pre>
    <p>
        Now when a consumer disposes of their subscription, the underlying <em>Timer</em>
        will be disposed of too.
    </p>
    <p>
        <em>Observable.Create</em> also has an overload that requires your <em>Func</em>
        to return an <em>Action</em> instead of an <em>IDisposable</em>. In a similar example
        to above, this one shows how you could use an action to un-register the event handler,
        preventing a memory leak by retaining the reference to the timer.
    </p>
    <pre class="csharpcode">
        //Example code only
        var ob = Observable.Create&lt;string&gt;(
            observer =>
            {
                var timer = new System.Timers.Timer();
                timer.Enabled = true;
                timer.Interval = 100;
                timer.Elapsed += OnTimerElapsed;
                timer.Start();
                    
                return ()=>{
                    timer.Elapsed -= OnTimerElapsed;
                    timer.Dispose();
                };
            });
    </pre>
    <p>
        These last few examples showed you how to use the <em>Observable.Create</em> method.
        These were just examples; there are actually better ways to produce values from
        a timer that we will look at soon. The intention is to show that <em>Observable.Create</em>
        provides you a lazily evaluated way to create observable sequences. We will dig
        much deeper into lazy evaluation and application of the <em>Create</em> factory
        method throughout the book especially when we cover concurrency and scheduling.
    </p>
    <a name="Unfold"></a>
    <h2>Functional unfolds</h2>
    <p>
        As a functional programmer you would come to expect the ability to unfold a potentially
        infinite sequence. An issue we may face with <em>Observable.Create</em> is that
        is that it can be a clumsy way to produce an infinite sequence. Our timer example
        above is an example of an infinite sequence, and while this is a simple implementation
        it is an annoying amount of code for something that effectively is delegating all
        the work to the <em>System.Timers.Timer</em> class. The <em>Observable.Create</em>
        method also has poor support for unfolding sequences using corecursion.
    </p>
    <a name="Corecursion"></a>
    <h3>Corecursion</h3>
    <p>
        Corecursion is a function to apply to the current state to produce the next state.
        Using corecursion by taking a value, applying a function to it that extends that
        value and repeating we can create a sequence. A simple example might be to take
        the value 1 as the seed and a function that increments the given value by one. This
        could be used to create sequence of [1,2,3,4,5...].
    </p>
    <p>
        Using corecursion to create an <em>IEnumerable&lt;int&gt;</em> sequence is made
        simple with the <code>yield return</code> syntax.
    </p>
    <pre class="csharpcode">
        private static IEnumerable&lt;T&gt; Unfold&lt;T&gt;(T seed, Func&lt;T, T&gt; accumulator)
        {
            var nextValue = seed;
            while (true)
            {
                yield return nextValue;
                nextValue = accumulator(nextValue);
            }
        }
    </pre>
    <p>
        The code above could be used to produce the sequence of natural numbers like this.
    </p>
    <pre class="csharpcode">
        var naturalNumbers = Unfold(1, i => i + 1);
        Console.WriteLine("1st 10 Natural numbers");
        foreach (var naturalNumber in naturalNumbers.Take(10))
        {
            Console.WriteLine(naturalNumber);
        }
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">1st 10 Natural numbers</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">5</div>
        <div class="line">6</div>
        <div class="line">7</div>
        <div class="line">8</div>
        <div class="line">9</div>
        <div class="line">10</div>
    </div>
    <p>
        Note the <code>Take(10)</code> is used to terminate the infinite sequence.
    </p>
    <p>
        Infinite and arbitrary length sequences can be very useful. First we will look at
        some that come with Rx and then consider how we can generalize the creation of infinite
        observable sequences.
    </p>
    <a name="ObservableRange"></a>
    <h3>Observable.Range</h3>
    <p>
        <em>Observable.Range(int, int)</em> simply returns a range of integers. The first
        integer is the initial value and the second is the number of values to yield. This
        example will write the values '10' through to '24' and then complete.
    </p>
    <pre class="csharpcode">
        var range = Observable.Range(10, 15);
        range.Subscribe(Console.WriteLine, ()=&gt;Console.WriteLine("Completed"));
    </pre>
    <a name="ObservableGenerate"></a>
    <h3>Observable.Generate</h3>
    <p>
        It is difficult to emulate the <em>Range</em> factory method using <em>Observable.Create</em>.
        It would be cumbersome to try and respect the principles that the code should be
        lazily evaluated and the consumer should be able to dispose of the subscription
        resources when they so choose. This is where we can use corecursion to provide a
        richer unfold. In Rx the unfold method is called <em>Observable.Generate</em>.
    </p>
    <p>
        The simple version of <em>Observable.Generate</em> takes the following parameters:
    </p>
    <ul>
        <li>an initial state</li>
        <li>a predicate that defines when the sequence should terminate</li>
        <li>a function to apply to the current state to produce the next state</li>
        <li>a function to transform the state to the desired output</li>
    </ul>
    <pre class="csharpcode">
        public static IObservable&lt;TResult&gt; Generate&lt;TState, TResult&gt;(
            TState initialState, 
            Func&lt;TState, bool&gt; condition, 
            Func&lt;TState, TState&gt; iterate, 
            Func&lt;TState, TResult&gt; resultSelector)
    </pre>
    <p>
        As an exercise, write your own <em>Range</em> factory method using <em>Observable.Generate</em>.
    </p>
    <p>
        Consider the <em>Range</em> signature <code>Range(int start, int count)</code>,
        which provides the seed and a value for the conditional predicate. You know how
        each new value is derived from the previous one; this becomes your iterate function.
        Finally, you probably don't need to transform the state so this makes the result
        selector function very simple.
    </p>
    <p>
        Continue when you have built your own version...
    </p>
    <hr style="page-break-after: always" />
    <p>
        Example of how you could use <em>Observable.Generate</em> to construct a similar
        <em>Range</em> factory method.
    </p>
    <pre class="csharpcode">
        //Example code only
        public static IObservable&lt;int&gt; Range(int start, int count)
        {
            var max = start + count;
            return Observable.Generate(
                start, 
                value =&gt; value &lt; max, 
                value =&gt; value + 1, 
                value =&gt; value);
        }
    </pre>
    <a name="ObservableInterval"></a>
    <h3>Observable.Interval</h3>
    <p>
        Earlier in the chapter we used a <em>System.Timers.Timer</em> in our observable
        to generate a continuous sequence of notifications. As mentioned in the example
        at the time, this is not the preferred way of working with timers in Rx. As Rx provides
        operators that give us this functionality it could be argued that to not use them
        is to re-invent the wheel. More importantly the Rx operators are the preferred way
        of working with timers due to their ability to substitute in schedulers which is
        desirable for easy substitution of the underlying timer. There are at least three
        various timers you could choose from for the example above:
    </p>
    <ul>
        <li><em>System.Timers.Timer</em></li>
        <li><em>System.Threading.Timer</em></li>
        <li><em>System.Windows.Threading.DispatcherTimer</em></li>
    </ul>
    <p>
        By abstracting the timer away via a scheduler we are able to reuse the same code
        for multiple platforms. More importantly than being able to write platform independent
        code is the ability to substitute in a test-double scheduler/timer to enable testing.
        Schedulers are a complex subject that is out of scope for this chapter, but they
        are covered in detail in the later chapter on <a href="15_SchedulingAndThreading.html">
            Scheduling and threading</a>.
    </p>
    <p>
        There are three better ways of working with constant time events, each being a further
        generalization of the former. The first is <strong>Observable.Interval(TimeSpan)</strong>
        which will publish incremental values starting from zero, based on a frequency of
        your choosing. This example publishes values every 250 milliseconds.
    </p>
    <pre class="csharpcode">
        var interval = Observable.Interval(TimeSpan.FromMilliseconds(250));
        interval.Subscribe(
            Console.WriteLine, 
            () => Console.WriteLine("completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">5</div>
    </div>
    <p>
        Once subscribed, you must dispose of your subscription to stop the sequence. It
        is an example of an infinite sequence.
    </p>
    <a name="ObservableTimer"></a>
    <h3>Observable.Timer</h3>
    <p>
        The second factory method for producing constant time based sequences is <strong>Observable.Timer</strong>.
        It has several overloads; the first of which we will look at being very simple.
        The most basic overload of <em>Observable.Timer</em> takes just a <em>TimeSpan</em>
        as <em>Observable.Interval</em> does. The <em>Observable.Timer</em> will however
        only publish one value (0) after the period of time has elapsed, and then it will
        complete.
    </p>
    <pre class="csharpcode">
        var timer = Observable.Timer(TimeSpan.FromSeconds(1));
        timer.Subscribe(
            Console.WriteLine, 
            () => Console.WriteLine("completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">0</div>
        <div class="line">completed</div>
    </div>
    <p>
        Alternatively, you can provide a <em>DateTimeOffset</em> for the <code>dueTime</code>
        parameter. This will produce the value 0 and complete at the due time.
    </p>
    <p>
        A further set of overloads adds a <em>TimeSpan</em> that indicates the period to
        produce subsequent values. This now allows us to produce infinite sequences and
        also construct <em>Observable.Interval</em> from <em>Observable.Timer</em>.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;long&gt; Interval(TimeSpan period)
        {
            return Observable.Timer(period, period);
        }
    </pre>
    <p>
        Note that this now returns an <em>IObservable</em> of <code>long</code> not <code>int</code>.
        While <em>Observable.Interval</em> would always wait the given period before producing
        the first value, this <em>Observable.Timer</em> overload gives the ability to start
        the sequence when you choose. With <em>Observable.Timer</em> you can write the following
        to have an interval sequence that started immediately.
    </p>
    <pre class="csharpcode">
        Observable.Timer(TimeSpan.Zero, period);
    </pre>
    <p>
        This takes us to our third way and most general way for producing timer related
        sequences, back to <em>Observable.Generate</em>. This time however, we are looking
        at a more complex overload that allows you to provide a function that specifies
        the due time for the next value.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;TResult&gt; Generate&lt;TState, TResult&gt;(
            TState initialState, 
            Func&lt;TState, bool&gt; condition, 
            Func&lt;TState, TState&gt; iterate, 
            Func&lt;TState, TResult&gt; resultSelector, 
            Func&lt;TState, TimeSpan&gt; timeSelector)
    </pre>
    <p>
        Using this overload, and specifically the extra <code>timeSelector</code> argument,
        we can produce our own implementation of <em>Observable.Timer</em> and in turn,
        <em>Observable.Interval</em>.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;long&gt; Timer(TimeSpan dueTime)
        {
            return Observable.Generate(
                0l,
                i =&gt; i &lt; 1,
                i =&gt; i + 1,
                i =&gt; i,
                i =&gt; dueTime);
        }
        public static IObservable&lt;long&gt; Timer(TimeSpan dueTime, TimeSpan period)
        {
            return Observable.Generate(
                0l,
                i =&gt; true,
                i =&gt; i + 1,
                i =&gt; i,
                i =&gt; i == 0 ? dueTime : period);
        }
        public static IObservable&lt;long&gt; Interval(TimeSpan period)
        {
            return Observable.Generate(
                0l,
                i =&gt; true,
                i =&gt; i + 1,
                i =&gt; i,
                i =&gt; period);
        }
    </pre>
    <p>
        This shows how you can use <em>Observable.Generate</em> to produce infinite sequences.
        I will leave it up to you the reader, as an exercise using <em>Observable.Generate</em>,
        to produce values at variable rates. I find using these methods invaluable not only
        in day to day work but especially for producing dummy data.
    </p>
    <a name="TransitioningIntoIObservable"></a>
    <h2>Transitioning into IObservable&lt;T&gt;</h2>
    <p>
        Generation of an observable sequence covers the complicated aspects of functional
        programming i.e. corecursion and unfold. You can also start a sequence by simply
        making a transition from an existing synchronous or asynchronous paradigm into the
        Rx paradigm.
    </p>
    <a name="ObservableStart"></a>
    <h3>From delegates</h3>
    <p>
        The <em>Observable.Start</em> method allows you to turn a long running <em>Func&lt;T&gt;</em>
        or <em>Action</em> into a single value observable sequence. By default, the processing
        will be done asynchronously on a ThreadPool thread. If the overload you use is a
        <em>Func&lt;T&gt;</em> then the return type will be <em>IObservable&lt;T&gt;</em>.
        When the function returns its value, that value will be published and then the sequence
        completed. If you use the overload that takes an <em>Action</em>, then the returned
        sequence will be of type <em>IObservable&lt;Unit&gt;</em>. The <em>Unit</em> type
        is a functional programming construct and is analogous to <code>void</code>. In
        this case <em>Unit</em> is used to publish an acknowledgement that the <em>Action</em>
        is complete, however this is rather inconsequential as the sequence is immediately
        completed straight after <em>Unit</em> anyway. The <em>Unit</em> type itself has
        no value; it just serves as an empty payload for the <em>OnNext</em> notification.
        Below is an example of using both overloads.
    </p>
    <pre class="csharpcode">
        static void StartAction()
        {
            var start = Observable.Start(() =&gt;
                {
                    Console.Write("Working away");
                    for (int i = 0; i &lt; 10; i++)
                    {
                        Thread.Sleep(100);
                        Console.Write(".");
                    }
                });
            start.Subscribe(
                unit =&gt; Console.WriteLine("Unit published"), 
                () =&gt; Console.WriteLine("Action completed"));
        }
        static void StartFunc()
        {
            var start = Observable.Start(() =&gt;
            {
                Console.Write("Working away");
                for (int i = 0; i &lt; 10; i++)
                {
                    Thread.Sleep(100);
                    Console.Write(".");
                }
                return "Published value";
            });
            start.Subscribe(
                Console.WriteLine, 
                () =&gt; Console.WriteLine("Action completed"));
        }
    </pre>
    <p>
        Note the difference between <em>Observable.Start</em> and <em>Observable.Return</em>;
        <em>Start</em> lazily evaluates the value from a function, <em>Return</em> provided
        the value eagerly. This makes <em>Start</em> very much like a <em>Task</em>. This
        can also lead to some confusion on when to use each of the features. Both are valid
        tools and the choice come down to the context of the problem space. Tasks are well
        suited to parallelizing computational work and providing workflows via continuations
        for computationally heavy work. Tasks also have the benefit of documenting and enforcing
        single value semantics. Using <em>Start</em> is a good way to integrate computationally
        heavy work into an existing code base that is largely made up of observable sequences.
        We look at <a href="12_CombiningSequences.html">composition of sequences</a> in
        more depth later in the book.
    </p>
    <a name="FromEvent"></a>
    <h3>From events</h3>
    <p>
        As we discussed early in the book, .NET already has the event model for providing
        a reactive, event driven programming model. While Rx is a more powerful and useful
        framework, it is late to the party and so needs to integrate with the existing event
        model. Rx provides methods to take an event and turn it into an observable sequence.
        There are several different varieties you can use. Here is a selection of common
        event patterns.
    </p>
    <pre class="csharpcode">
        //Activated delegate is EventHandler
        var appActivated = Observable.FromEventPattern(
                h =&gt; Application.Current.Activated += h,
                h =&gt; Application.Current.Activated -= h);
        //PropertyChanged is PropertyChangedEventHandler
        var propChanged = Observable.FromEventPattern
            &lt;PropertyChangedEventHandler, PropertyChangedEventArgs&gt;(
                handler =&gt; handler.Invoke,
                h =&gt; this.PropertyChanged += h,
                h =&gt; this.PropertyChanged -= h);
                
        //FirstChanceException is EventHandler&lt;FirstChanceExceptionEventArgs&gt;
        var firstChanceException = Observable.FromEventPattern&lt;FirstChanceExceptionEventArgs&gt;(
                h =&gt; AppDomain.CurrentDomain.FirstChanceException += h,
                h =&gt; AppDomain.CurrentDomain.FirstChanceException -= h);      

    </pre>
    <p>
        So while the overloads can be confusing, they key is to find out what the event's
        signature is. If the signature is just the base <em>EventHandler</em> delegate then
        you can use the first example. If the delegate is a sub-class of the <em>EventHandler</em>,
        then you need to use the second example and provide the <em>EventHandler</em> sub-class
        and also its specific type of <em>EventArgs</em>. Alternatively, if the delegate
        is the newer generic <em>EventHandler&lt;TEventArgs&gt;</em>, then you need to use
        the third example and just specify what the generic type of the event argument is.
    </p>
    <p>
        It is very common to want to expose property changed events as observable sequences.
        These events can be exposed via <em>INotifyPropertyChanged</em> interface, a <em>DependencyProperty</em>
        or perhaps by events named appropriately to the Property they are representing.
        If you are looking at writing your own wrappers to do this sort of thing, I would
        strongly suggest looking at the Rxx library on <a href="http://Rxx.codeplex.com">http://Rxx.codeplex.com</a>
        first. Many of these have been catered for in a very elegant fashion.
    </p>
    <a name="FromTask"></a>
    <h3>From Task</h3>
    <p>
        Rx provides a useful, and well named set of overloads for transforming from other
        existing paradigms to the Observable paradigm. The <em>ToObservable()</em> method
        overloads provide a simple route to make the transition.
    </p>
    <p>
        As we mentioned earlier, the <em>AsyncSubject&lt;T&gt;</em> is similar to a <em>Task&lt;T&gt;</em>.
        They both return you a single value from an asynchronous source. They also both
        cache the result for any repeated or late requests for the value. The first <code>ToObservable()</code>
        extension method overload we look at is an extension to <em>Task&lt;T&gt;</em>.
        The implementation is simple;
    </p>
    <ul>
        <li>if the task is already in a status of <code>RanToCompletion</code> then the value
            is added to the sequence and then the sequence completed</li>
        <li>if the task is Cancelled then the sequence will error with a <em>TaskCanceledException</em></li>
        <li>if the task is Faulted then the sequence will error with the task's inner exception</li>
        <li>if the task has not yet completed, then a continuation is added to the task to perform
            the above actions appropriately</li>
    </ul>
    <p>
        There are two reasons to use the extension method:
    </p>
    <ol>
        <li>From Framework 4.5, almost all I/O-bound functions return <em>Task&lt;T&gt;</em></li>
        <li>If <em>Task&lt;T&gt;</em> is a good fit, it's preferable to use it over <em>IObservable&lt;T&gt;</em>
            - because it communicates single-value result in the type system. In other words,
            a function that returns a single value in the future should return a <em>Task&lt;T&gt;</em>,
            not an <em>IObservable&lt;T&gt;</em>. Then if you need to combine it with other
            observables, use <code>ToObservable()</code>.</li>
    </ol>
    <p>
        Usage of the extension method is also simple.
    </p>
    <pre class="csharpcode">
        var t = Task.Factory.StartNew(()=&gt;"Test");
        var source = t.ToObservable();
        source.Subscribe(
            Console.WriteLine,
            () =&gt; Console.WriteLine("completed"));
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Test</div>
        <div class="line">completed</div>
    </div>
    <p>
        There is also an overload that converts a <em>Task</em> (non generic) to an <em>IObservable&lt;Unit&gt;</em>.
    </p>
    <a name="FromIEnumerable"></a>
    <h3>From IEnumerable&lt;T&gt;</h3>
    <p>
        The final overload of <em>ToObservable</em> takes an <em>IEnumerable&lt;T&gt;</em>.
        This is semantically like a helper method for an <em>Observable.Create</em> with
        a <code>foreach</code> loop in it.
    </p>
    <pre class="csharpcode">
        //Example code only
        public static IObservable&lt;T&gt; ToObservable&lt;T&gt;(this IEnumerable&lt;T&gt; source)
        {
            return Observable.Create&lt;T&gt;(o =&gt;
            {
                foreach (var item in source)
                {
                    o.OnNext(item);
                }
                //Incorrect disposal pattern
                return Disposable.Empty;
            });
        }
    </pre>
    <p>
        This crude implementation however is naive. It does not allow for correct disposal,
        it does not handle exceptions correctly and as we will see later in the book, it
        does not have a very nice concurrency model. The version in Rx of course caters
        for all of these tricky details so you don't need to worry.
    </p>
    <p>
        When transitioning from <em>IEnumerable&lt;T&gt;</em> to <em>IObservable&lt;T&gt;</em>,
        you should carefully consider what you are really trying to achieve. You should
        also carefully test and measure the performance impacts of your decisions. Consider
        that the blocking synchronous (pull) nature of <em>IEnumerable&lt;T&gt;</em> sometimes
        just does not mix well with the asynchronous (push) nature of <em>IObservable&lt;T&gt;</em>.
        Remember that it is completely valid to pass <em>IEnumerable</em>, <em>IEnumerable&lt;T&gt;</em>,
        arrays or collections as the data type for an observable sequence. If the sequence
        can be materialized all at once, then you may want to avoid exposing it as an <em>IEnumerable</em>.
        If this seems like a fit for you then also consider passing immutable types like
        an array or a <em>ReadOnlyCollection&lt;T&gt;</em>. We will see the use of <em>IObservable&lt;IList&lt;T&gt;&gt;</em>
        later for operators that provide batching of data.
    </p>
    <a name="FromAPM"></a>
    <h3>From APM</h3>
    <p>
        Finally we look at a set of overloads that take you from the <a href="http://msdn.microsoft.com/en-us/magazine/cc163467.aspx">
            Asynchronous Programming Model</a> (APM) to an observable sequence. This is
        the style of programming found in .NET that can be identified with the use of two
        methods prefixed with <em>Begin...</em> and <em>End...</em> and the iconic <em>IAsyncResult</em>
        parameter type. This is commonly seen in the I/O APIs.
    </p>
    <pre class="csharpcode">
        class WebRequest
        {    
            public WebResponse GetResponse() 
            {...}
            public IAsyncResult BeginGetResponse(
                AsyncCallback callback, 
                object state) 
            {...}
            public WebResponse EndGetResponse(IAsyncResult asyncResult) 
            {...}
            ...
        }
        class Stream
        {
            public int Read(
                byte[] buffer, 
                int offset, 
                int count) 
            {...}
            public IAsyncResult BeginRead(
                byte[] buffer, 
                int offset, 
                int count, 
                AsyncCallback callback, 
                object state) 
            {...}
            public int EndRead(IAsyncResult asyncResult) 
            {...}
            ...
        }
    </pre>
    <p class="comment">
        At time of writing .NET 4.5 was still in preview release. Moving forward with .NET
        4.5 the APM model will be replaced with <em>Task</em> and new <code>async</code>
        and <code>await</code> keywords. Rx 2.0 which is also in a beta release will integrate
        with these features. .NET 4.5 and Rx 2.0 are not in the scope of this book.
    </p>
    <p>
        APM, or the Async Pattern, has enabled a very powerful, yet clumsy way of for .NET
        programs to perform long running I/O bound work. If we were to use the synchronous
        access to IO, e.g. <code>WebRequest.GetResponse()</code> or <code>Stream.Read(...)</code>,
        we would be blocking a thread but not performing any work while we waited for the
        IO. This can be quite wasteful on busy servers performing a lot of concurrent work
        to hold a thread idle while waiting for I/O to complete. Depending on the implementation,
        APM can work at the hardware device driver layer and not require any threads while
        blocking. Information on how to follow the APM model is scarce. Of the documentation
        you can find it is pretty shaky, however, for more information on APM, see Jeffrey
        Richter's brilliant book <cite>CLR via C#</cite> or Joe Duffy's comprehensive <cite>
            Concurrent Programming on Windows</cite>. Most stuff on the internet is blatant
        plagiary of Richter's examples from his book. An in-depth examination of APM is
        outside of the scope of this book.
    </p>
    <p>
        To utilize the Asynchronous Programming Model but avoid its awkward API, we can
        use the <em>Observable.FromAsyncPattern</em> method. Jeffrey van Gogh gives a brilliant
        walk through of the <em>Observable.FromAsyncPattern</em> in <a href="http://blogs.msdn.com/b/jeffva/archive/2010/07/23/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading.aspx">
            Part 1</a> of his <cite>Rx on the Server</cite> blog series. While the theory
        backing the Rx on the Server series is sound, it was written in mid 2010 and targets
        an old version of Rx.
    </p>
    <p>
        With 30 overloads of <em>Observable.FromAsyncPattern</em> we will look at the general
        concept so that you can pick the appropriate overload for yourself. First if we
        look at the normal pattern of APM we will see that the BeginXXX method will take
        zero or more data arguments followed by an <code>AsyncCallback</code> and an <em>Object</em>.
        The BeginXXX method will also return an <em>IAsyncResult</em> token.
    </p>
    <pre class="csharpcode">
        //Standard Begin signature
        IAsyncResult BeginXXX(AsyncCallback callback, Object state);
        //Standard Begin signature with data
        IAsyncResult BeginYYY(string someParam1, AsyncCallback callback, object state);
    </pre>
    <p>
        The EndXXX method will accept an <em>IAsyncResult</em> which should be the token
        returned from the BeginXXX method. The EndXXX can also return a value.
    </p>
    <pre class="csharpcode">
        //Standard EndXXX Signature
        void EndXXX(IAsyncResult asyncResult);
        //Standard EndXXX Signature with data
        int EndYYY(IAsyncResult asyncResult);
    </pre>
    <p>
        The generic arguments for the <em>FromAsyncPattern</em> method are just the BeginXXX
        data arguments if any, followed by the EndXXX return type if any. If we apply that
        to our <code>Stream.Read(byte[], int, int, AsyncResult, object)</code> example above
        we see that we have a <code>byte[]</code>, an <code>int</code> and another <code>int</code>
        as our data parameters for <em>BeginRead</em> method.
    </p>
    <pre class="csharpcode">
        //IAsyncResult BeginRead(
        //  byte[] buffer, 
        //  int offset, 
        //  int count, 
        //  AsyncCallback callback, object state) {...}
        Observable.FromAsyncPattern&lt;byte[], int, int ...
    </pre>
    <p>
        Now we look at the EndXXX method and see it returns an <code>int</code>, which completes
        the generic signature of our <em>FromAsyncPattern</em> call.
    </p>
    <pre class="csharpcode">
        //int EndRead(
        //  IAsyncResult asyncResult) {...}
        Observable.FromAsyncPattern&lt;byte[], int, int, int&gt;
    </pre>
    <p>
        The result of the call to <em>Observable.FromAsyncPattern</em> does <i>not</i> return
        an observable sequence. It returns a delegate that returns an observable sequence.
        The signature for this delegate will match the generic arguments of the call to
        <em>FromAsyncPattern</em>, except that the return type will be wrapped in an observable
        sequence.
    </p>
    <pre class="csharpcode">
        var fileLength = (int) stream.Length;
        //read is a Func&lt;byte[], int, int, IObservable&lt;int&gt;&gt;
        var read = Observable.FromAsyncPattern&lt;byte[], int, int, int&gt;(
            stream.BeginRead, 
            stream.EndRead);
        var buffer = new byte[fileLength];
        var bytesReadStream = read(buffer, 0, fileLength);
        bytesReadStream.Subscribe(
            byteCount =&gt;
            {
                Console.WriteLine("Number of bytes read={0}, buffer should be populated with data now.",
                byteCount);
            });
    </pre>
    <p>
        Note that this implementation is just an example. For a very well designed implementation
        that is built against the latest version of Rx you should look at the Rxx project
        on <a href="http://rxx.codeplex.com">http://rxx.codeplex.com</a>.
    </p>
    <p>
        This covers the first classification of query operators: creating observable sequences.
        We have looked at the various eager and lazy ways to create a sequence. We have
        introduced the concept of corecursion and show how we can use it with the <em>Generate</em>
        method to unfold potentially infinite sequences. We can now produce timer based
        sequences using the various factory methods. We should also be familiar with ways
        to transition from other synchronous and asynchronous paradigms and be able to decide
        when it is or is not appropriate to do so. As a quick recap:
    </p>
    <ul>
        <li>Factory Methods
            <ul>
                <li>Observable.Return</li>
                <li>Observable.Empty</li>
                <li>Observable.Never</li>
                <li>Observable.Throw</li>
                <li>Observable.Create</li>
            </ul>
        </li>
        <li>Unfold methods
            <ul>
                <li>Observable.Range</li>
                <li>Observable.Interval</li>
                <li>Observable.Timer</li>
                <li>Observable.Generate</li>
            </ul>
        </li>
        <li>Paradigm Transition
            <ul>
                <li>Observable.Start</li>
                <li>Observable.FromEventPattern</li>
                <li>Task.ToObservable</li>
                <li>Task&lt;T&gt;.ToObservable</li>
                <li>IEnumerable&lt;T&gt;.ToObservable</li>
                <li>Observable.FromAsyncPattern</li>
            </ul>
        </li>
    </ul>
    <p>
        Creating an observable sequence is our first step to practical application of Rx:
        create the sequence and then expose it for consumption. Now that we have a firm
        grasp on how to create an observable sequence, we can discover the operators that
        allow us to query an observable sequence.
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
