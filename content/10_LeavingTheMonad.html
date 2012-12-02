<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Leaving the monad</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <!--TODO: Enumerators -->
    <a name="LeavingTheMonad"></a>
    <h1>Leaving the monad</h1>
    <!--
            
            TODO: Create a compelling reason that you would want to leave the monad and support 
            it. else Create the argument that you can but don't want to and then prove it.

    -->
    <p>
        An observable sequence is a useful construct, especially when we have the power
        of LINQ to compose complex queries over it. Even though we recognize the benefits
        of the observable sequence, sometimes it is required to leave the <em>IObservable&lt;T&gt;</em>
        paradigm for another paradigm, maybe to enable you to integrate with an existing API
        (i.e. use events or <em>Task&lt;T&gt;</em>). You might leave the observable paradigm
        if you find it easier for testing, or it may simply be easier for you to learn Rx
        by moving between an observable paradigm and a more familiar one.
    </p>
    <a name="WhatIsAMonad"></a>
    <h2>What is a monad</h2>
    <p>
        We have casually referred to the term <i>monad</i> earlier in the book, but to most
        it will be a very foreign term. I am going to try to avoid overcomplicating what
        a monad is, but give enough of an explanation to help us out with our next category
        of methods. The full definition of a monad is quite abstract. <a href="http://www.haskell.org/haskellwiki/Monad_tutorials_timeline">
            Many others</a> have tried to provide their definition of a monad using all
        sorts of metaphors from astronauts to Alice in Wonderland. Many of the tutorials
        for monadic programming use Haskell for the code examples which can add to the confusion.
        For us, a monad is effectively a programming structure that represents computations.
        Compare this to other programming structures:
    </p>
    <dl>
        <dt>Data structure</dt>
        <dd>
            Purely state e.g. a List, a Tree or a Tuple
        </dd>
        <dt>Contract</dt>
        <dd>
            Contract definition or abstract functionality e.g. an interface or abstract class
        </dd>
        <dt>Object-Orientated structure</dt>
        <dd>
            State and behavior together
        </dd>
    </dl>
    <p>
        Generally a monadic structure allows you to chain together operators to produce
        a pipeline, just as we do with our extension methods.
    </p>
    <cite>Monads are a kind of abstract data type constructor that encapsulate program logic
        instead of data in the domain model. </cite>
    <p>
        This neat definition of a monad lifted from Wikipedia allows us to start viewing
        sequences as monads; the abstract data type in this case is the <em>IObservable&lt;T&gt;</em>
        type. When we use an observable sequence, we compose functions onto the abstract
        data type (the <em>IObservable&lt;T&gt;</em>) to create a query. This query becomes
        our encapsulated programming logic.
    </p>
    <p>
        The use of monads to define control flows is particularly useful when dealing with
        typically troublesome areas of programming such as IO, concurrency and exceptions.
        This just happens to be some of Rx's strong points!
    </p>
    <a name="WhyLeaveTheMonad"></a>
    <h2>Why leave the monad?</h2>
    <p>
        There is a variety of reasons you may want to consume an observable sequence in
        a different paradigm. Libraries that need to expose functionality externally may
        be required to present it as events or as <em>Task</em> instances. In demonstration and sample
        code you may prefer to use blocking methods to limit the number of asynchronous
        moving parts. This may help make the learning curve to Rx a little less steep!
    </p>
    <p>
        In production code, it is rarely advised to 'break the monad', especially moving
        from an observable sequence to blocking methods. Switching between asynchronous
        and synchronous paradigms should be done with caution, as this is a common root
        cause for concurrency problems such as deadlock and scalability issues.
    </p>
    <p>
        In this chapter, we will look at the methods in Rx which allow you to leave the
        <em>IObservable&lt;T&gt;</em> monad.
    </p>
    <a name="ForEach"></a>
    <h2>ForEach</h2>
    <p>
        The <em>ForEach</em> method provides a way to process elements as they are received.
        The key difference between <em>ForEach</em> and <em>Subscribe</em> is that <em>ForEach</em>
        will block the current thread until the sequence completes.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(5);
        source.ForEach(i => Console.WriteLine("received {0} @ {1}", i, DateTime.Now));
        Console.WriteLine("completed @ {0}", DateTime.Now);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">received 0 @ 01/01/2012 12:00:01 a.m.</div>
        <div class="line">received 1 @ 01/01/2012 12:00:02 a.m.</div>
        <div class="line">received 2 @ 01/01/2012 12:00:03 a.m.</div>
        <div class="line">received 3 @ 01/01/2012 12:00:04 a.m.</div>
        <div class="line">received 4 @ 01/01/2012 12:00:05 a.m.</div>
        <div class="line">completed @ 01/01/2012 12:00:05 a.m.</div>
    </div>
    <p>
        Note that the completed line is last, as you would expect. To be clear, you can
        get similar functionality from the <em>Subscribe</em> extension method, but the
        <em>Subscribe</em> method will not block. So if we substitute the call to <em>ForEach</em>
        with a call to <em>Subscribe</em>, we will see the completed line happen first.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(5);
        source.Subscribe(i => Console.WriteLine("received {0} @ {1}", i, DateTime.Now));
        Console.WriteLine("completed @ {0}", DateTime.Now);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">completed @ 01/01/2012 12:00:00 a.m.</div>
        <div class="line">received 0 @ 01/01/2012 12:00:01 a.m.</div>
        <div class="line">received 1 @ 01/01/2012 12:00:02 a.m.</div>
        <div class="line">received 2 @ 01/01/2012 12:00:03 a.m.</div>
        <div class="line">received 3 @ 01/01/2012 12:00:04 a.m.</div>
        <div class="line">received 4 @ 01/01/2012 12:00:05 a.m.</div>
    </div>
    <p>
        Unlike the <em>Subscribe</em> extension method, <em>ForEach</em> has only the one
        overload; the one that take an <em>Action&lt;T&gt;</em> as its single argument.
        In contrast, previous (pre-release) versions of Rx, the <em>ForEach</em> method
        had most of the same overloads as <em>Subscribe</em>. Those overloads of <em>ForEach</em>
        have been deprecated, and I think rightly so. There is no need to have an <code>OnCompleted</code>
        handler in a synchronous call, it is unnecessary. You can just place the call immediately
        after the <em>ForEach</em> call as we have done above. Also, the <code>OnError</code>
        handler can now be replaced with standard Structured Exception Handling like you
        would use for any other synchronous code, with a <code>try</code>/<code>catch</code>
        block. This also gives symmetry to the <em>ForEach</em> instance method on the <em>List&lt;T&gt;</em>
        type.
    </p>
    <pre class="csharpcode">
        var source = Observable.Throw&lt;int&gt;(new Exception("Fail"));
        try
        {
            source.ForEach(Console.WriteLine);
        }
        catch (Exception ex)
        {
            Console.WriteLine("error @ {0} with {1}", DateTime.Now, ex.Message);
        }
        finally
        {
            Console.WriteLine("completed @ {0}", DateTime.Now);    
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">error @ 01/01/2012 12:00:00 a.m. with Fail</div>
        <div class="line">completed @ 01/01/2012 12:00:00 a.m.</div>
    </div>
    <p>
        The <em>ForEach</em> method, like its other blocking friends (<em>First</em> and
        <em>Last</em> etc.), should be used with care. I would leave the <em>ForEach</em>
        method for spikes, tests and demo code only. We will discuss the problems with introducing
        blocking calls when we look at concurrency.
    </p>
    <!--TODO: The  GetEnumerator, Latest, MostRecent and Next operators are not covered. These could be really useful.-->
    <!--<a name="ObservableSequencesToEnumerators"></a>
        <h2>Observable sequences to enumerators</h2>
        <p></p>
        <a name="GetEnumerator"></a>
        <h3>GetEnumerator</h3>
        <p></p>
        <a name="Latest"></a>
        <h3>Latest</h3>
        <p></p>
        <a name="MostRecent"></a>
        <h3>MostRecent</h3>
        <p></p>
        <a name="Next"></a>
        <h3>Next</h3>
        <p></p>
    -->
    <a name="ToEnumerable"></a>
    <h2>ToEnumerable</h2>
    <p>
        An alternative way to switch out of the <em>IObservable&lt;T&gt;</em> is to call
        the <em>ToEnumerable</em> extension method. As a simple example:
    </p>
    <pre class="csharpcode">
        var period = TimeSpan.FromMilliseconds(200);
        var source = Observable.Timer(TimeSpan.Zero, period) 
            .Take(5); 
        var result = source.ToEnumerable();
        foreach (var value in result) 
        { 
            Console.WriteLine(value); 
        } 
        Console.WriteLine("done");
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
        <div class="line">done</div>
    </div>
    <p>
        The source observable sequence will be subscribed to when you start to enumerate
        the sequence (i.e. lazily). In contrast to the <em>ForEach</em> extension method,
        using the <em>ToEnumerable</em> method means you are only blocked when you try to
        move to the next element and it is not available. Also, if the sequence produces
        values faster than you consume them, they will be cached for you.
    </p>
    <p>
        To cater for errors, you can wrap your <code>foreach</code> loop in a <code>try</code>/<code>catch</code>
        as you do with any other enumerable sequence:
    </p>
    <pre class="csharpcode">
        try 
        { 
            foreach (var value in result)
            { 
                Console.WriteLine(value); 
            } 
        } 
        catch (Exception e) 
        { 
            Console.WriteLine(e.Message);
        } 
    </pre>
    <p>
        As you are moving from a push to a pull model (non-blocking to blocking), the standard
        warning applies.
    </p>
    <a name="ToBatch"></a>
    <h2>To a single collection</h2>
    <p>
        To avoid having to oscillate between push and pull, you can use one of the
        next four methods to get the entire list back in a single notification. They all
        have the same semantics, but just produce the data in a different format. They are
        similar to their corresponding <em>IEnumerable&lt;T&gt;</em> operators, but the
        return values differ in order to retain asynchronous behavior.
    </p>
    <a name="ToArrayAndToList"></a>
    <h3>ToArray and ToList</h3>
    <p>
        Both <em>ToArray</em> and <em>ToList</em> take an observable sequence and package
        it into an array or an instance of <em>List&lt;T&gt;</em> respectively. Once the
        observable sequence completes, the array or list will be pushed as the single value
        of the result sequence.
    </p>
    <pre class="csharpcode">
        var period = TimeSpan.FromMilliseconds(200); 
        var source = Observable.Timer(TimeSpan.Zero, period).Take(5); 
        var result = source.ToArray(); 
        result.Subscribe( 
            arr =&gt; { 
                Console.WriteLine("Received array"); 
                foreach (var value in arr) 
                { 
                    Console.WriteLine(value); 
                } 
            }, 
            () =&gt; Console.WriteLine("Completed")
        ); 
        Console.WriteLine("Subscribed"); 
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Subscribed</div>
        <div class="line">Received array</div>
        <div class="line">0</div>
        <div class="line">1</div>
        <div class="line">2</div>
        <div class="line">3</div>
        <div class="line">4</div>
        <div class="line">Completed</div>
    </div>
    <p>
        As these methods still return observable sequences we can use our <code>OnError</code>
        handler for errors. Note that the source sequence is packaged to a single notification;
        you either get the whole sequence <b>or</b> the error. If the source produces values
        and then errors, you will not receive any of those values. All four operators (<em>ToArray</em>,
        <em>ToList</em>, <em>ToDictionary</em> and <em>ToLookup</em>) handle errors like
        this.
    </p>
    <a name="ToDictionaryAndToLookup"></a>
    <h3>ToDictionary and ToLookup</h3>
    <p>
        As an alternative to arrays and lists, Rx can package an observable sequence into
        a dictionary or lookup with the <em>ToDictionary</em> and <em>ToLookup</em> methods.
        Both methods have the same semantics as the <em>ToArray</em> and <em>ToList</em>
        methods, as they return a sequence with a single value and have the same error handling
        features.
    </p>
    <p>
        The <em>ToDictionary</em> extension method overloads:
    </p>
    <pre class="csharpcode">
        // Creates a dictionary from an observable sequence according to a specified key selector 
        // function, a comparer, and an element selector function.
        public static IObservable&lt;IDictionary&lt;TKey, TElement&gt;&gt; ToDictionary&lt;TSource, TKey, TElement&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            Func&lt;TSource, TElement&gt; elementSelector, 
            IEqualityComparer&lt;TKey&gt; comparer) 
        {...} 
        // Creates a dictionary from an observable sequence according to a specified key selector 
        // function, and an element selector function. 
        public static IObservable&lt;IDictionary&lt;TKey, TElement&gt;&gt; ToDictionary&lt;TSource, TKey, TElement&gt;( 
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            Func&lt;TSource, TElement&gt; elementSelector) 
        {...} 
        // Creates a dictionary from an observable sequence according to a specified key selector 
        // function, and a comparer. 
        public static IObservable&lt;IDictionary&lt;TKey, TSource&gt;&gt; ToDictionary&lt;TSource, TKey&gt;( 
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector,
            IEqualityComparer&lt;TKey&gt; comparer) 
        {...} 
        // Creates a dictionary from an observable sequence according to a specified key selector 
        // function. 
        public static IObservable&lt;IDictionary&lt;TKey, TSource&gt;&gt; ToDictionary&lt;TSource, TKey&gt;( 
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector) 
        {...} 
    </pre>
    <p>
        The <em>ToLookup</em> extension method overloads:
    </p>
    <pre class="csharpcode">
        // Creates a lookup from an observable sequence according to a specified key selector 
        // function, a comparer, and an element selector function. 
        public static IObservable&lt;ILookup&lt;TKey, TElement&gt;&gt; ToLookup&lt;TSource, TKey, TElement&gt;( 
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            Func&lt;TSource, TElement&gt; elementSelector,
            IEqualityComparer&lt;TKey&gt; comparer) 
        {...} 
        // Creates a lookup from an observable sequence according to a specified key selector 
        // function, and a comparer. 
        public static IObservable&lt;ILookup&lt;TKey, TSource&gt;&gt; ToLookup&lt;TSource, TKey&gt;(
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            IEqualityComparer&lt;TKey&gt; comparer) 
        {...} 
        // Creates a lookup from an observable sequence according to a specified key selector 
        // function, and an element selector function. 
        public static IObservable&lt;ILookup&lt;TKey, TElement&gt;&gt; ToLookup&lt;TSource, TKey, TElement&gt;( 
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource, TKey&gt; keySelector, 
            Func&lt;TSource, TElement&gt; elementSelector)
        {...} 
        // Creates a lookup from an observable sequence according to a specified key selector 
        // function. 
        public static IObservable&lt;ILookup&lt;TKey, TSource&gt;&gt; ToLookup&lt;TSource, TKey&gt;( 
            this IObservable&lt;TSource&gt; source, 
            Func&lt;TSource,
            TKey&gt; keySelector) 
        {...} 
    </pre>
    <p>
        Both <em>ToDictionary</em> and <em>ToLookup</em> require a function that can be
        applied each value to get its key. In addition, the <em>ToDictionary</em> method
        overloads mandate that all keys should be unique. If a duplicate key is found, it
        terminate the sequence with a <em>DuplicateKeyException</em>. On the other hand,
        the <em>ILookup&lt;TKey, TElement&gt;</em> is designed to have multiple values grouped
        by the key. If you have many values per key, then <em>ToLookup</em> is probably
        the better option.
    </p>
    <a name="ToTask"></a>
    <h2>ToTask</h2>
    <p>
        We have compared <em>AsyncSubject&lt;T&gt;</em> to <em>Task&lt;T&gt;</em> and even
        showed how to <a href="04_CreatingObservableSequences.html#FromTask">transition from
            a task</a> to an observable sequence. The <em>ToTask</em> extension method will
        allow you to convert an observable sequence into a <em>Task&lt;T&gt;</em>. Like
        an <em>AsyncSubject&lt;T&gt;</em>, this method will ignore multiple values, only
        returning the last value.
    </p>
    <pre class="csharpcode">
        // Returns a task that contains the last value of the observable sequence. 
        public static Task&lt;TResult&gt; ToTask&lt;TResult&gt;(
            this IObservable&lt;TResult&gt; observable) 
        {...} 
        // Returns a task that contains the last value of the observable sequence, with state to 
        //  use as the underlying task's AsyncState. 
        public static Task&lt;TResult&gt; ToTask&lt;TResult&gt;(
            this IObservable&lt;TResult&gt; observable,
            object state) 
        {...} 
        // Returns a task that contains the last value of the observable sequence. Requires a 
        //  cancellation token that can be used to cancel the task, causing unsubscription from 
        //  the observable sequence. 
        public static Task&lt;TResult&gt; ToTask&lt;TResult&gt;(
            this IObservable&lt;TResult&gt; observable, 
            CancellationToken cancellationToken) 
        {...} 
        // Returns a task that contains the last value of the observable sequence, with state to 
        //  use as the underlying task's AsyncState. Requires a cancellation token that can be used
        //  to cancel the task, causing unsubscription from the observable sequence. 
        public static Task&lt;TResult&gt; ToTask&lt;TResult&gt;(
            this IObservable&lt;TResult&gt; observable, 
            CancellationToken cancellationToken, 
            object state) 
        {...} 
    </pre>
    <p>
        This is a simple example of how the <em>ToTask</em> operator can be used. Note,
        the <em>ToTask</em> method is in the <code>System.Reactive.Threading.Tasks</code>
        namespace.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1)) 
            .Take(5);
        var result = source.ToTask(); //Will arrive in 5 seconds. 
        Console.WriteLine(result.Result);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">4</div>
    </div>
    <p>
        If the source sequence was to manifest error then the task would follow the error-handling
        semantics of tasks.
    </p>
    <pre class="csharpcode">
        var source = Observable.Throw&lt;long&gt;(new Exception("Fail!")); 
        var result = source.ToTask(); 
        try 
        { 
            Console.WriteLine(result.Result);
        } 
        catch (AggregateException e) 
        { 
            Console.WriteLine(e.InnerException.Message); 
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Fail!</div>
    </div>
    <p>
        Once you have your task, you can of course engage in all the features of the TPL
        such as continuations.
    </p>
    <a name="ToEventT"></a>
    <h2>ToEvent&lt;T&gt;</h2>
    <p>
        Just as you can use an event as the source for an observable sequence with <a href="04_CreatingObservableSequences.html#FromEvent">
            <em>FromEventPattern</em></a>, you can also make your observable sequence look
        like a standard .NET event with the <em>ToEvent</em> extension methods.
    </p>
    <pre class="csharpcode">
        // Exposes an observable sequence as an object with a .NET event. 
        public static IEventSource&lt;unit&gt; ToEvent(this IObservable&lt;Unit&gt; source)
        {...} 
        // Exposes an observable sequence as an object with a .NET event. 
        public static IEventSource&lt;TSource&gt; ToEvent&lt;TSource&gt;(
            this IObservable&lt;TSource&gt; source) 
        {...} 
        // Exposes an observable sequence as an object with a .NET event. 
        public static IEventPatternSource&lt;TEventArgs&gt; ToEventPattern&lt;TEventArgs&gt;(
            this IObservable&lt;EventPattern&lt;TEventArgs&gt;&gt; source) 
            where TEventArgs : EventArgs 
        {...} 
</pre>
    <p>
        The <em>ToEvent</em> method returns an <em>IEventSource&lt;T&gt;</em>, which will
        have a single event member on it: <code>OnNext</code>.
    </p>
    <pre class="csharpcode">
        public interface IEventSource&lt;T&gt; 
        { 
            event Action&lt;T&gt; OnNext; 
        } 
    </pre>
    <p>
        When we convert the observable sequence with the <em>ToEvent</em> method, we can
        just subscribe by providing an <em>Action&lt;T&gt;</em>, which we do here with a
        lambda.
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1))
            .Take(5); 
        var result = source.ToEvent(); 
        result.OnNext += val => Console.WriteLine(val);
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
    </div>
    <a name="ToEventPattern"></a>
    <h3>ToEventPattern</h3>
    <p>
        Note that this does not follow the standard pattern of events. Normally, when you
        subscribe to an event, you need to handle the <code>sender</code> and <em>EventArgs</em>
        parameters. In the example above, we just get the value. If you want to expose your
        sequence as an event that follows the standard pattern, you will need to use
        <em>ToEventPattern</em>.
    </p>
    <p>
        The <em>ToEventPattern</em> will take an <em>IObservable&lt;EventPattern&lt;TEventArgs&gt;&gt;</em>
        and convert that into an <em>IEventPatternSource&lt;TEventArgs&gt;</em>. The public
        interface for these types is quite simple.
    </p>
    <pre class="csharpcode">
        public class EventPattern&lt;TEventArgs&gt; : IEquatable&lt;EventPattern&lt;TEventArgs&gt;&gt;
            where TEventArgs : EventArgs 
        { 
            public EventPattern(object sender, TEventArgs e)
            { 
                this.Sender = sender; 
                this.EventArgs = e; 
            } 
            public object Sender { get; private set; } 
            public TEventArgs EventArgs { get; private set; } 
            //...equality overloads
        } 
        public interface IEventPatternSource&lt;TEventArgs&gt; where TEventArgs : EventArgs
        { 
            event EventHandler&lt;TEventArgs&gt; OnNext; 
        } 
    </pre>
    <p>
        These look quite easy to work with. So if we create an <em>EventArgs</em> type
        and then apply a simple transform using <em>Select</em>, we can make a standard
        sequence fit the pattern.
    </p>
    <p>
        The <em>EventArgs</em> type:
    </p>
    <pre class="csharpcode">
        public class MyEventArgs : EventArgs 
        { 
            private readonly long _value; 
            public MyEventArgs(long value) 
            { 
                _value = value; 
            } 
            public long Value 
            { 
                get { return _value; } 
            } 
        } 
    </pre>
    <p>
        The transform:
    </p>
    <pre class="csharpcode">
        var source = Observable.Interval(TimeSpan.FromSeconds(1))
            .Select(i =&gt; new EventPattern&lt;MyEventArgs&gt;(this, new MyEventArgs(i)));
    </pre>
    <p>
        Now that we have a sequence that is compatible, we can use the <em>ToEventPattern</em>,
        and in turn, a standard event handler.
    </p>
    <pre class="csharpcode">
        var result = source.ToEventPattern(); 
        result.OnNext += (sender, eventArgs) => Console.WriteLine(eventArgs.Value);
    </pre>
    <p>
        Now that we know how to get back into .NET events, let's take a break and remember
        why Rx is a better model.
    </p>
    <ul>
        <li>In C#, events have a curious interface. Some find the <code>+=</code> and <code>
            -=</code> operators an unnatural way to register a callback</li>
        <li>Events are difficult to compose</li>
        <li>Events do not offer the ability to be easily queried over time</li>
        <li>Events are a common cause of accidental memory leaks</li>
        <li>Events do not have a standard pattern for signaling completion</li>
        <li>Events provide almost no help for concurrency or multithreaded applications. For
            instance, raising an event on a separate thread requires you to do all of the plumbing</li>
    </ul>
    <hr />
    <p>
        The set of methods we have looked at in this chapter complete the circle started
        in the <a href="04_CreatingObservableSequences.html#TransitioningIntoIObservable">Creating
            a Sequence</a> chapter. We now have the means to enter and leave the observable
        sequence monad. Take care when opting in and out of the <em>IObservable&lt;T&gt;</em>
        monad. Doing so excessively can quickly make a mess of your code base, and may indicate
        a design flaw.
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
