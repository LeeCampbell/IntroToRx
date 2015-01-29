<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Scheduling and threading</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="PART4"></a>
    <h1 class="SectionHeader">PART 4 - Concurrency</h1>
    <p>
        Rx is primarily a system for querying <i>data in motion</i> asynchronously. To effectively
        provide the level of asynchrony that developers require, some level of concurrency
        control is required. We need the ability to generate sequence data concurrently
        to the consumption of the sequence data.
    </p>
    <p>
        In this fourth and final part of the book, we will look at the various concurrency
        considerations one must undertake when querying data in motion. We will look how
        to avoid concurrency when possible and use it correctly when justifiable. We will
        look at the excellent abstractions Rx provides, that enable concurrency to become
        declarative and also unit testable. In my opinion, theses two features are enough
        reason alone to adopt Rx into your code base. We will also look at the complex issue
        of querying concurrent sequences and analyzing data in sliding windows of time.
    </p>
    <a name="SchedulingAndThreading"></a>
    <h1>Scheduling and threading</h1>
    <p>
        So far, we have managed to avoid any explicit usage of threading or concurrency.
        There are some methods that we have covered that implicitly introduce some level
        of concurrency to perform their jobs (e.g. <em>Buffer</em>, <em>Delay</em>, <em>Sample</em>
        each require a separate thread/scheduler/timer to work their magic). Most of this
        however, has been kindly abstracted away from us. This chapter will look at the
        elegant beauty of the Rx API and its ability to effectively remove the need for
        <em>WaitHandle</em> types, and any explicit calls to <em>Thread</em>s, the <em>ThreadPool</em>
        or <em>Task</em>s.
    </p>
    <a name="RxIsSingleThreadedByDefault"></a>
    <h2>Rx is single-threaded by default</h2>
    <p>
        A popular misconception is that Rx is multithreaded by default. It is perhaps more
        an idle assumption than a strong belief, much in the same way some assume that standard
        .NET events are multithreaded until they challenge that notion. We debunk this myth
        and assert that events are most certainly single threaded and synchronous in the
        <a href="19_DispellingMyths.html#DispellingEventMyths">Appendix</a>.
    </p>
    <p>
        Like events, Rx is just a way of chaining callbacks together for a given notification.
        While Rx is a free-threaded model, this does not mean that subscribing or calling
        <code>OnNext</code> will introduce multi-threading to your sequence. Being free-threaded
        means that you are not restricted to which thread you choose to do your work. For
        example, you can choose to do your work such as invoking a subscription, observing
        or producing notifications, on any thread you like. The alternative to a free-threaded
        model is a <i>Single Threaded Apartment</i> (STA) model where you must interact
        with the system on a given thread. It is common to use the STA model when working
        with User Interfaces and some COM interop. So, just as a recap: if you do not introduce
        any scheduling, your callbacks will be invoked on the same thread that the <em>OnNext</em>/<em>OnError</em>/<em>OnCompleted</em>
        methods are invoked from.
    </p>
    <p>
        In this example, we create a subject then call <em>OnNext</em> on various threads
        and record the threadId in our handler.
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
        var subject = new Subject&lt;object&gt;();

        subject.Subscribe(
            o =&gt; Console.WriteLine("Received {1} on threadId:{0}", 
                Thread.CurrentThread.ManagedThreadId, 
                o));

        ParameterizedThreadStart notify = obj =&gt;
        {
            Console.WriteLine("OnNext({1}) on threadId:{0}",
                                Thread.CurrentThread.ManagedThreadId, 
                                obj);
            subject.OnNext(obj);
        };

        notify(1);
        new Thread(notify).Start(2);
        new Thread(notify).Start(3);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Starting on threadId:9</div>
        <div class="line">OnNext(1) on threadId:9</div>
        <div class="line">Received 1 on threadId:9</div>
        <div class="line">OnNext(2) on threadId:10</div>
        <div class="line">Received 2 on threadId:10</div>
        <div class="line">OnNext(3) on threadId:11</div>
        <div class="line">Received 3 on threadId:11</div>
    </div>
    <p>
        Note that each <em>OnNext</em> was called back on the same thread that it was notified
        on. This is not always what we are looking for. Rx introduces a very handy mechanism
        for introducing concurrency and multithreading to your code: Scheduling.
    </p>
    <a name="SubscribeOnObserveOn"></a>
    <h2>SubscribeOn and ObserveOn</h2>
    <p>
        In the Rx world, there are generally two things you want to control the concurrency
        model for:
    </p>
    <ol>
        <li>The invocation of the subscription </li>
        <li>The observing of notifications </li>
    </ol>
    <p>
        As you could probably guess, these are exposed via two extension methods to <em>IObservable&lt;T&gt;</em>
        called <em>SubscribeOn</em> and <em>ObserveOn</em>. Both methods have an overload
        that take an <em>IScheduler</em> (or <em>SynchronizationContext</em>) and return
        an <em>IObservable&lt;T&gt;</em> so you can chain methods together.
    </p>
    <pre class="csharpcode">
        public static class Observable 
        {
            public static IObservable&lt;TSource&gt; ObserveOn&lt;TSource&gt;(
                this IObservable&lt;TSource&gt; source, 
                IScheduler scheduler)
            {...}
            public static IObservable&lt;TSource&gt; ObserveOn&lt;TSource&gt;(
                this IObservable&lt;TSource&gt; source, 
                SynchronizationContext context)
            {...}
            public static IObservable&lt;TSource&gt; SubscribeOn&lt;TSource&gt;(
                this IObservable&lt;TSource&gt; source, 
                IScheduler scheduler)
            {...}
            public static IObservable&lt;TSource&gt; SubscribeOn&lt;TSource&gt;(
                this IObservable&lt;TSource&gt; source, 
                SynchronizationContext context)
            {...}
        }
    </pre>
    <p>
        One pitfall I want to point out here is, the first few times I used these overloads,
        I was confused as to what they actually do. You should use the <em>SubscribeOn</em>
        method to describe how you want any warm-up and background processing code to be
        scheduled. For example, if you were to use <em>SubscribeOn</em> with <em>Observable.Create</em>,
        the delegate passed to the <em>Create</em> method would be run on the specified
        scheduler.
    </p>
    <p>
        In this example, we have a sequence produced by <em>Observable.Create</em> with
        a standard subscription.
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
        var source = Observable.Create&lt;int&gt;(
            o =&gt;
            {
                Console.WriteLine("Invoked on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
                o.OnNext(1);
                o.OnNext(2);
                o.OnNext(3);
                o.OnCompleted();
                Console.WriteLine("Finished on threadId:{0}",
                Thread.CurrentThread.ManagedThreadId);
                return Disposable.Empty;
            });

        source
            //.SubscribeOn(Scheduler.ThreadPool)
            .Subscribe(
                o =&gt; Console.WriteLine("Received {1} on threadId:{0}",
                    Thread.CurrentThread.ManagedThreadId,
                    o),
                () =&gt; Console.WriteLine("OnCompleted on threadId:{0}",
                    Thread.CurrentThread.ManagedThreadId));
        Console.WriteLine("Subscribed on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Starting on threadId:9</div>
        <div class="line">Invoked on threadId:9</div>
        <div class="line">Received 1 on threadId:9</div>
        <div class="line">Received 2 on threadId:9</div>
        <div class="line">Received 3 on threadId:9</div>
        <div class="line">OnCompleted on threadId:9</div>
        <div class="line">Finished on threadId:9</div>
        <div class="line">Subscribed on threadId:9</div>
    </div>
    <p>
        You will notice that all actions were performed on the same thread. Also, note that
        everything is sequential. When the subscription is made, the <em>Create</em> delegate
        is called. When <code>OnNext(1)</code> is called, the <em>OnNext</em> handler is
        called, and so on. This all stays synchronous until the <em>Create</em> delegate
        is finished, and the <em>Subscribe</em> line can move on to the final line that
        declares we are subscribed on thread 9.
    </p>
    <p>
        If we apply <em>SubscribeOn</em> to the chain (i.e. un-comment it), the order of
        execution is quite different.
    </p>
    <div class="output">
        <div class="line">Starting on threadId:9</div>
        <div class="line">Subscribed on threadId:9</div>
        <div class="line">Invoked on threadId:10</div>
        <div class="line">Received 1 on threadId:10</div>
        <div class="line">Received 2 on threadId:10</div>
        <div class="line">Received 3 on threadId:10</div>
        <div class="line">OnCompleted on threadId:10</div>
        <div class="line">Finished on threadId:10</div>
    </div>
    <p>
        Observe that the subscribe call is now non-blocking. The <em>Create</em> delegate
        is executed on the thread pool and so are all our handlers.
    </p>
    <p>
        The <em>ObserveOn</em> method is used to declare where you want your notifications
        to be scheduled to. I would suggest the <em>ObserveOn</em> method is most useful
        when working with STA systems, most commonly UI applications. When writing UI applications,
        the <em>SubscribeOn</em>/<em>ObserveOn</em> pair is very useful for two reasons:
    </p>
    <ol>
        <li>you do not want to block the UI thread</li>
        <li>but you do need to update UI objects on the UI thread.</li>
    </ol>
    <p>
        It is critical to avoid blocking the UI thread, as doing so leads to a poor user
        experience. General guidance for Silverlight and WPF is that any work that blocks
        for longer than 150-250ms should not be performed on the UI thread (Dispatcher).
        This is approximately the period of time over which a user can notice a lag in the
        UI (mouse becomes sticky, animations sluggish). In the upcoming Metro style apps
        for Windows 8, the maximum allowed blocking time is only 50ms. This more stringent
        rule is to ensure a consistent <q>fast and fluid</q> experience across applications.
        With the processing power offered by current desktop processors, you can achieve
        a lot of processing 50ms. However, as processor become more varied (single/multi/many
        core, plus high power desktop vs. lower power ARM tablet/phones), how much you can
        do in 50ms fluctuates widely. In general terms: any I/O, computational intensive
        work or any processing unrelated to the UI should be marshaled off the UI thread.
        The general pattern for creating responsive UI applications is:
    </p>
    <ul>
        <li>respond to some sort of user action</li>
        <li>do work on a background thread</li>
        <li>pass the result back to the UI thread</li>
        <li>update the UI</li>
    </ul>
    <p>
        This is a great fit for Rx: responding to events, potentially composing multiple
        events, passing data to chained method calls. With the inclusion of scheduling,
        we even have the power to get off and back onto the UI thread for that responsive
        application feel that users demand.
    </p>
    <p>
        Consider a WPF application that used Rx to populate an <em>ObservableCollection&lt;T&gt;</em>.
        You would almost certainly want to use <em>SubscribeOn</em> to leave the <em>Dispatcher</em>,
        followed by <em>ObserveOn</em> to ensure you were notified back on the Dispatcher.
        If you failed to use the <em>ObserveOn</em> method, then your <em>OnNext</em> handlers
        would be invoked on the same thread that raised the notification. In Silverlight/WPF,
        this would cause some sort of not-supported/cross-threading exception. In this example,
        we subscribe to a sequence of <code>Customers</code>. We perform the subscription
        on a new thread and ensure that as we receive <code>Customer</code> notifications,
        we add them to the <code>Customers</code> collection on the <em>Dispatcher</em>.
    </p>
    <pre class="csharpcode">
        _customerService.GetCustomers()
            .SubscribeOn(Scheduler.NewThread)
            .ObserveOn(DispatcherScheduler.Instance) 
            //or .ObserveOnDispatcher() 
            .Subscribe(Customers.Add);
    </pre>
    <a name="Schedulers"></a>
    <h2>Schedulers</h2>
    <p>
        The <em>SubscribeOn</em> and <em>ObserveOn</em> methods required us to pass in an
        <em>IScheduler</em>. Here we will dig a little deeper and see what schedulers are,
        and what implementations are available to us.
    </p>
    <p>
        There are two main types we use when working with schedulers:
    </p>
    <dl>
        <dt>The <em>IScheduler</em> interface</dt>
        <dd>
            A common interface for all schedulers</dd>
        <dt>The static <em>Scheduler</em> class</dt>
        <dd>
            Exposes both implementations of <em>IScheduler</em> and helpful extension methods
            to the <em>IScheduler</em> interface</dd>
    </dl>
    <p>
        The <em>IScheduler</em> interface is of less importance right now than the types
        that implement the interface. The key concept to understand is that an <em>IScheduler</em>
        in Rx is used to schedule some action to be performed, either as soon as possible
        or at a given point in the future. The implementation of the <em>IScheduler</em>
        defines how that action will be invoked i.e. asynchronously via a thread pool, a
        new thread or a message pump, or synchronously on the current thread. Depending
        on your platform (Silverlight 4, Silverlight 5, .NET 3.5, .NET 4.0), you will be
        exposed most of the implementations you will need via a static class <em>Scheduler</em>.
    </p>
    <p>
        Before we look at the <em>IScheduler</em> interface in detail, let's look at the
        extension method we will use the most often and then introduce the common implementations.
    </p>
    <p>
        This is the most commonly used (extension) method for <em>IScheduler</em>. It simply
        sets an action to be performed as soon as possible.
    </p>
    <pre class="csharpcode">
        public static IDisposable Schedule(this IScheduler scheduler, Action action)
        {...}
    </pre>
    <p>
        You could use the method like this:
    </p>
    <pre class="csharpcode">
        IScheduler scheduler = ...;
        scheduler.Schedule(()=>{ Console.WriteLine("Work to be scheduled"); });
    </pre>
    <p>
        These are the static properties that you can find on the <em>Scheduler</em> type.
    </p>
    <p>
        <em>Scheduler.Immediate</em> will ensure the action is not scheduled, but rather
        executed immediately.
    </p>
    <p>
        <em>Scheduler.CurrentThread</em> ensures that the actions are performed on the thread
        that made the original call. This is different from <em>Immediate</em>, as <em>CurrentThread</em>
        will queue the action to be performed. We will compare these two schedulers using
        a code example soon.
    </p>
    <p>
        <em>Scheduler.NewThread</em> will schedule work to be done on a new thread.</p>
    <p>
        <em>Scheduler.ThreadPool</em> will schedule all actions to take place on the Thread
        Pool.</p>
    <p>
        <em>Scheduler.TaskPool</em> will schedule actions onto the TaskPool. This is not
        available in Silverlight 4 or .NET 3.5 builds.
    </p>
    <p>
        If you are using WPF or Silverlight, then you will also have access to <em>DispatcherScheduler.Instance</em>.
        This allows you to schedule tasks onto the <em>Dispatcher</em> with the common interface,
        either now or in the future. There is the <em>SubscribeOnDispatcher()</em> and <em>ObserveOnDispatcher()</em>
        extension methods to <em>IObservable&lt;T&gt;</em>, that also help you access the
        Dispatcher. While they appear useful, you will want to avoid these two methods for
        production code, and we explain why in the <a href="16_TestingRx.html">Testing Rx</a>
        chapter.
    </p>
    <p>
        Most of the schedulers listed above are quite self explanatory for basic usage.
        We will take an in-depth look at all of the implementations of <em>IScheduler</em>
        later in the chapter.
    </p>
    <a name="ConcurrencyPitfalls"></a>
    <h2>Concurrency pitfalls</h2>
    <p>
        Introducing concurrency to your application will increase its complexity. If your
        application is not noticeably improved by adding a layer of concurrency, then you
        should avoid doing so. Concurrent applications can exhibit maintenance problems
        with symptoms surfacing in the areas of debugging, testing and refactoring.
    </p>
    <p>
        The common problem that concurrency introduces is unpredictable timing. Unpredictable
        timing can be caused by variable load on a system, as well as variations in system
        configurations (e.g. varying core clock speed and availability of processors). These
        can ultimately can result in race conditions. Symptoms of race conditions include
        out-of-order execution, <a href="http://en.wikipedia.org/wiki/Deadlock">deadlocks</a>,
        <a href="http://en.wikipedia.org/wiki/Deadlock#Livelock">livelocks</a> and corrupted
        state.
    </p>
    <p>
        In my opinion, the biggest danger when introducing concurrency haphazardly to an
        application, is that you can silently introduce bugs. These defects may slip past
        Development, QA and UAT and only manifest themselves in Production environments.
    </p>
    <p>
        Rx, however, does such a good job of simplifying the concurrent processing of observable
        sequences that many of these concerns can be mitigated. You can still create problems,
        but if you follow the guidelines then you can feel a lot safer in the knowledge
        that you have heavily reduced the capacity for unwanted race conditions.
    </p>
    <p>
        In a later chapter, <a href="16_TestingRx.html">Testing Rx</a>, we will look at
        how Rx improves your ability to test concurrent workflows.
    </p>
    <a name="LockUps"></a>
    <h3>Lock-ups</h3>
    <p>
        When working on my first commercial application that used Rx, the team found out
        the hard way that Rx code can most certainly deadlock. When you consider that some
        calls (like <em>First</em>, <em>Last</em>, <em>Single</em> and <em>ForEach</em>)
        are blocking, and that we can schedule work to be done in the future, it becomes
        obvious that a race condition can occur. This example is the simplest block I could
        think of. Admittedly, it is fairly elementary but it will get the ball rolling.
    </p>
    <pre class="csharpcode">
        var sequence = new Subject&lt;int&gt;();
        Console.WriteLine("Next line should lock the system.");
        var value = sequence.First();
        sequence.OnNext(1);
        Console.WriteLine("I can never execute....");
    </pre>
    <p>
        Hopefully, we won't ever write such code though, and if we did, our tests would
        give us quick feedback that things went wrong. More realistically, race conditions
        often slip into the system at integration points. The next example may be a little
        harder to detect, but is only small step away from our first, unrealistic example.
        Here, we block in the constructor of a UI element which will always be created on
        the dispatcher. The blocking call is waiting for an event that can only be raised
        from the dispatcher, thus creating a deadlock.
    </p>
    <pre class="csharpcode">
        public Window1()
        {
            InitializeComponent();
            DataContext = this;
            Value = "Default value";
            //Deadlock! 
            //We need the dispatcher to continue to allow me to click the button to produce a value
            Value = _subject.First();
            //This will give same result but will not be blocking (deadlocking). 
            _subject.Take(1).Subscribe(value =&gt; Value = value);
        }
        private void MyButton_Click(object sender, RoutedEventArgs e)
        {
            _subject.OnNext("New Value");
        }
        public string Value
        {
            get { return _value; }
            set
            {
                _value = value;
                var handler = PropertyChanged;
                if (handler != null) handler(this, new PropertyChangedEventArgs("Value"));
            }
        }
    </pre>
    <p>
        Next, we start seeing things that can become more sinister. The button's click handler
        will try to get the first value from an observable sequence exposed via an interface.
    </p>
    <pre class="csharpcode">
        public partial class Window1 : INotifyPropertyChanged
        {
            //Imagine DI here.
            private readonly IMyService _service = new MyService(); 
            private int _value2;

            public Window1()
            {
                InitializeComponent();
                DataContext = this;
            }
            public int Value2
            {
                get { return _value2; }
                set
                {
                    _value2 = value;
                    var handler = PropertyChanged;
                    if (handler != null) handler(this, new PropertyChangedEventArgs("Value2"));
                }
            }
            #region INotifyPropertyChanged Members
            public event PropertyChangedEventHandler PropertyChanged;
            #endregion
            private void MyButton2_Click(object sender, RoutedEventArgs e)
            {
                Value2 = _service.GetTemperature().First();
            }
        }
    </pre>
    <p>
        There is only one small problem here in that we block on the <em>Dispatcher</em>
        thread (<code>First</code> is a blocking call), however this manifests itself into
        a deadlock if the service code is written incorrectly.
    </p>
    <pre class="csharpcode">
        class MyService : IMyService
        {
            public IObservable&lt;int&gt; GetTemperature()
            {
                return Observable.Create&lt;int&gt;(
                    o =&gt;
                    {
                        o.OnNext(27);
                        o.OnNext(26);
                        o.OnNext(24);
                        return () =&gt; { };
                    })
                   .SubscribeOnDispatcher();
            }
        }
    </pre>
    <p>
        This odd implementation, with explicit scheduling, will cause the three <code>OnNext</code>
        calls to be scheduled once the <code>First()</code> call has finished; however,
        <em>that</em> is waiting for an <em>OnNext</em> to be called: we are deadlocked.
    </p>
    <p>
        So far, this chapter may seem to say that concurrency is all doom and gloom by focusing
        on the problems you could face; this is not the intent though. We do not magically
        avoid classic concurrency problems simply by adopting Rx. Rx will however make it
        easier to get it right, provided you follow these two simple rules.
    </p>
    <ol>
        <li>Only the final subscriber should be setting the scheduling</li>
        <li>Avoid using blocking calls: e.g. <em>First</em>, <em>Last</em> and <em>Single</em></li>
    </ol>
    <p>
        The last example came unstuck with one simple problem; the service was dictating
        the scheduling paradigm when, really, it had no business doing so. Before we had
        a clear idea of where we should be doing the scheduling in my first Rx project,
        we had all sorts of layers adding 'helpful' scheduling code. What it ended up creating
        was a threading nightmare. When we removed all the scheduling code and then confined
        it it in a single layer (at least in the Silverlight client), most of our concurrency
        problems went away. I recommend you do the same. At least in WPF/Silverlight applications,
        the pattern should be simple: "Subscribe on a Background thread; Observe on the
        Dispatcher".
    </p>
    <a name="AdvancedFeaturesOfSchedulers"></a>
    <h2>Advanced features of schedulers</h2>
    <p>
        We have only looked at the most simple usage of schedulers so far:
    </p>
    <ul>
        <li>Scheduling an action to be executed as soon as possible</li>
        <li>Scheduling the subscription of an observable sequence</li>
        <li>Scheduling the observation of notifications coming from an observable sequence</li>
    </ul>
    <p>
        Schedulers also provide more advanced features that can help you with various problems.
    </p>
    <a name="PassingState"></a>
    <h3>Passing state</h3>
    <p>
        In the extension method to <em>IScheduler</em> we have looked at, you could only
        provide an <em>Action</em> to execute. This <em>Action</em> did not accept any parameters.
        If you want to pass state to the <em>Action</em>, you could use a closure to share
        the data like this:
    </p>
    <pre class="csharpcode">
        var myName = "Lee";
        Scheduler.NewThread.Schedule(
            () =&gt; Console.WriteLine("myName = {0}", myName));
    </pre>
    <p>
        This could create a problem, as you are sharing state across two different scopes.
        I could modify the variable <em>myName</em> and get unexpected results.
    </p>
    <p>
        In this example, we use a closure as above to pass state. I immediately modify the
        closure and this creates a race condition: will my modification happen before or
        after the state is used by the scheduler?
    </p>
    <pre class="csharpcode">
        var myName = "Lee";
        scheduler.Schedule(
            () =&gt; Console.WriteLine("myName = {0}", myName));
        myName = "John";//What will get written to the console?
    </pre>
    <p>
        In my tests, "John" is generally written to the console when <em>scheduler</em>
        is an instance of <em>NewThreadScheduler</em>. If I use the <em>ImmediateScheduler</em>
        then "Lee" would be written. The problem with this is the non-deterministic nature
        of the code.
    </p>
    <p>
        A preferable way to pass state is to use the <em>Schedule</em> overloads that accept
        state. This example takes advantage of this overload, giving us certainty about
        our state.
    </p>
    <pre class="csharpcode">
        var myName = "Lee";
        scheduler.Schedule(myName, 
            (_, state) =&gt;
            {
                Console.WriteLine(state);
                return Disposable.Empty;
            });
        myName = "John";
    </pre>
    <p>
        Here, we pass <em>myName</em> as the state. We also pass a delegate that will take
        the state and return a disposable. The disposable is used for cancellation; we will
        look into that later. The delegate also takes an <em>IScheduler</em> parameter,
        which we name "_" (underscore). This is the convention to indicate we are ignoring
        the argument. When we pass <em>myName</em> as the state, a reference to the state
        is kept internally. So when we update the <em>myName</em> variable to "John", the
        reference to "Lee" is still maintained by the scheduler's internal workings.
    </p>
    <p>
        Note that in our previous example, we modify the <em>myName</em> variable to point
        to a new instance of a string. If we were to instead have an instance that we actually
        modified, we could still get unpredictable behavior. In the next example, we now
        use a list for our state. After scheduling an action to print out the element count
        of the list, we modify that list.
    </p>
    <pre class="csharpcode">
        var list = new List&lt;int&gt;();
        scheduler.Schedule(list,
            (innerScheduler, state) =&gt;
            {
                Console.WriteLine(state.Count);
                return Disposable.Empty;
            });
        list.Add(1);
    </pre>
    <p>
        Now that we are modifying shared state, we can get unpredictable results. In this
        example, we don't even know what type the scheduler is, so we cannot predict the
        race conditions we are creating. As with any concurrent software, you should avoid
        modifying shared state.
    </p>
    <a name="FutureScheduling"></a>
    <h3>Future scheduling</h3>
    <p>
        As you would expect with a type called "IScheduler", you are able to schedule an
        action to be executed in the future. You can do so by specifying the exact point
        in time an action should be invoked, or you can specify the period of time to wait
        until the action is invoked. This is clearly useful for features such as buffering, timers etc.
    </p>
    <p>
        Scheduling in the future is thus made possible by two styles of overloads, one that
        takes a <em>TimeSpan</em> and one that takes a <em>DateTimeOffset</em>. These are
        the two most simple overloads that execute an action in the future.
    </p>
    <pre class="csharpcode">
        public static IDisposable Schedule(
            this IScheduler scheduler, 
            TimeSpan dueTime, 
            Action action)
        {...}
        public static IDisposable Schedule(
            this IScheduler scheduler, 
            DateTimeOffset dueTime, 
            Action action)
        {...}
    </pre>
    <p>
        You can use the <em>TimeSpan</em> overload like this:
    </p>
    <pre class="csharpcode">
        var delay = TimeSpan.FromSeconds(1);
        Console.WriteLine("Before schedule at {0:o}", DateTime.Now);
        scheduler.Schedule(delay, 
            () =&gt; Console.WriteLine("Inside schedule at {0:o}", DateTime.Now));
        Console.WriteLine("After schedule at  {0:o}", DateTime.Now);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Before schedule at 2012-01-01T12:00:00.000000+00:00</div>
        <div class="line">After schedule at 2012-01-01T12:00:00.058000+00:00</div>
        <div class="line">Inside schedule at 2012-01-01T12:00:01.044000+00:00</div>
    </div>
    <p>
        We can see therefore that scheduling is non-blocking as the 'before' and 'after' calls
        are very close together in time. You can also see that approximately one second
        after the action was scheduled, it was invoked.
    </p>
    <p>
        You can specify a specific point in time to schedule the task with the <em>DateTimeOffset</em>
        overload. If, for some reason, the point in time you specify is in the past, then
        the action is scheduled as soon as possible.
    </p>
    <a name="Cancelation"></a>
    <h3>Cancelation</h3>
    <p>
        Each of the overloads to <em>Schedule</em> returns an <em>IDisposable</em>; this
        way, a consumer can cancel the scheduled work. In the previous example, we scheduled
        work to be invoked in one second. We could cancel that work by disposing of the
        cancellation token (i.e. the return value).
    </p>
    <pre class="csharpcode">
        var delay = TimeSpan.FromSeconds(1);
        Console.WriteLine("Before schedule at {0:o}", DateTime.Now);
        var token = scheduler.Schedule(delay, 
            () =&gt; Console.WriteLine("Inside schedule at {0:o}", DateTime.Now));
        Console.WriteLine("After schedule at  {0:o}", DateTime.Now);
        token.Dispose();
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Before schedule at 2012-01-01T12:00:00.000000+00:00</div>
        <div class="line">After schedule at 2012-01-01T12:00:00.058000+00:00</div>
    </div>
    <p>
        Note that the scheduled action never occurs, as we have cancelled it almost immediately.
    </p>
    <p>
        When the user cancels the scheduled action method before the scheduler is able to
        invoke it, that action is just removed from the queue of work. This is what we see
        in example above. If you want to cancel scheduled work that is already running,
        then you can use one of the overloads to the <em>Schedule</em> method that takes
        a <em>Func&lt;IDisposable&gt;</em>. This gives a way for users to cancel out of
        a job that may already be running. This job could be some sort of I/O, heavy computations
        or perhaps usage of <em>Task</em> to perform some work.
    </p>
    <p>
        Now this may create a problem; if you want to cancel work that has already been
        started, you need to dispose of an instance of <em>IDisposable</em>, but how do
        you return the disposable if you are still doing the work? You could fire up another
        thread so the work happens concurrently, but creating threads is something we are
        trying to steer away from.
    </p>
    <p>
        In this example, we have a method that we will use as the delegate to be scheduled.
        It just fakes some work by performing a spin wait and adding values to the <em>list</em>
        argument. The key here is that we allow the user to cancel with the <em>CancellationToken</em>
        via the disposable we return.
    </p>
    <pre class="csharpcode">
        public IDisposable Work(IScheduler scheduler, List&lt;int&gt; list)
        {
            var tokenSource = new CancellationTokenSource();
            var cancelToken = tokenSource.Token;
            var task = new Task(() =&gt;
            {
                Console.WriteLine();
                for (int i = 0; i &lt; 1000; i++)
                {
                    var sw = new SpinWait();
                    for (int j = 0; j &lt; 3000; j++) sw.SpinOnce();
                    Console.Write(".");
                    list.Add(i);
                    if (cancelToken.IsCancellationRequested)
                    {
                        Console.WriteLine("Cancelation requested");
                        //cancelToken.ThrowIfCancellationRequested();
                        return;
                    }
                }
            }, cancelToken);
            task.Start();
            return Disposable.Create(tokenSource.Cancel);
        }
    </pre>
    <p>
        This code schedules the above code and allows the user to cancel the processing
        work by pressing Enter
    </p>
    <pre class="csharpcode">
        var list = new List&lt;int&gt;();
        Console.WriteLine("Enter to quit:");
        var token = scheduler.Schedule(list, Work);
        Console.ReadLine();
        Console.WriteLine("Cancelling...");
        token.Dispose();
        Console.WriteLine("Cancelled");
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">Enter to quit:</div>
        <div class="line">........</div>
        <div class="line">Cancelling...</div>
        <div class="line">Cancelled</div>
        <div class="line">Cancelation requested</div>
    </div>
    <p>
        The problem here is that we have introduced explicit use of <em>Task</em>. We can
        avoid explicit usage of a concurrency model if we use the Rx recursive scheduler
        features instead.
    </p>
    <a name="Recursion"></a>
    <h3>Recursion</h3>
    <p>
        The more advanced overloads of <em>Schedule</em> extension methods take some strange
        looking delegates as parameters. Take special note of the final parameter in each
        of these overloads of the <em>Schedule</em> extension method.
    </p>
    <pre class="csharpcode">
        public static IDisposable Schedule(
            this IScheduler scheduler, 
            Action&lt;Action&gt; action)
        {...}
        public static IDisposable Schedule&lt;TState&gt;(
            this IScheduler scheduler, 
            TState state, 
            Action&lt;TState, Action&lt;TState&gt;&gt; action)
        {...}
        public static IDisposable Schedule(
            this IScheduler scheduler, 
            TimeSpan dueTime, 
            Action&lt;Action&lt;TimeSpan&gt;&gt; action)
        {...}
        public static IDisposable Schedule&lt;TState&gt;(
            this IScheduler scheduler, 
            TState state, 
            TimeSpan dueTime, 
            Action&lt;TState, Action&lt;TState, TimeSpan&gt;&gt; action)
        {...}
        public static IDisposable Schedule(
            this IScheduler scheduler, 
            DateTimeOffset dueTime, 
            Action&lt;Action&lt;DateTimeOffset&gt;&gt; action)
        {...}
        public static IDisposable Schedule&lt;TState&gt;(
            this IScheduler scheduler, 
            TState state, DateTimeOffset dueTime, 
            Action&lt;TState, Action&lt;TState, DateTimeOffset&gt;&gt; action)
        {...}   
    </pre>
    <p>
        Each of these overloads take a delegate "action" that allows you to call "action"
        recursively. This may seem a very odd signature, but it makes for a great API. This
        effectively allows you to create a recursive delegate call. This may be best shown
        with an example.
    </p>
    <p>
        This example uses the most simple recursive overload. We have an Action that can
        be called recursively.
    </p>
    <pre class="csharpcode">
        Action&lt;Action&gt; work = (Action self) 
            =&gt;
            {
                Console.WriteLine("Running");
                self();
            };
        var token = s.Schedule(work);
            
        Console.ReadLine();
        Console.WriteLine("Cancelling");
        token.Dispose();
        Console.WriteLine("Cancelled");
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Enter to quit:</div>
        <div class="line">Running</div>
        <div class="line">Running</div>
        <div class="line">Running</div>
        <div class="line">Running</div>
        <div class="line">Cancelling</div>
        <div class="line">Cancelled</div>
        <div class="line">Running</div>
    </div>
    <p>
        Note that we didn't have to write any cancellation code in our delegate. Rx handled
        the looping and checked for cancellation on our behalf. Brilliant! Unlike simple
        recursive methods in C#, we are also protected from stack overflows, as Rx provides
        an extra level of abstraction. Indeed, Rx takes our recursive method and transforms
        it to a loop structure instead.
    </p>
    <a name="CreatingYourOwnIterator"></a>
    <h4>Creating your own iterator</h4>
    <p>
        Earlier in the book, we looked at how we can use <a href="04_CreatingObservableSequences.html#FromAPM">
            Rx with APM</a>. In our example, we just read the entire file into memory. We
        also referenced Jeffrey van Gogh's <a href="http://blogs.msdn.com/b/jeffva/archive/2010/07/23/rx-on-the-server-part-1-of-n-asynchronous-system-io-stream-reading.aspx">
            blog post</a>, which sadly is now out of date; however, his concepts are still
        sound. Instead of the Iterator method from Jeffrey's post, we can use schedulers
        to achieve the same result.
    </p>
    <p>
        The goal of the following sample is to open a file and stream it in chunks. This
        enables us to work with files that are larger than the memory available to us, as
        we would only ever read and cache a portion of the file at a time. In addition to
        this, we can leverage the compositional nature of Rx to apply multiple transformations
        to the file such as encryption and compression. By reading chunks at a time, we
        are able to start the other transformations before we have finished reading the
        file.
    </p>
    <p>
        First, let us refresh our memory with how to get from the <em>FileStream</em>'s
        APM methods into Rx.
    </p>
    <pre class="csharpcode">
        var source = new FileStream(@"C:\Somefile.txt", FileMode.Open, FileAccess.Read);
        var factory = Observable.FromAsyncPattern&lt;byte[], int, int, int&gt;(
            source.BeginRead, 
            source.EndRead);
        var buffer = new byte[source.Length];
        IObservable&lt;int&gt; reader = factory(buffer, 0, (int)source.Length);
        reader.Subscribe(
            bytesRead =&gt; 
                Console.WriteLine("Read {0} bytes from file into buffer", bytesRead));
    </pre>
    <p>
        The example above uses <em>FromAsyncPattern</em> to create a factory. The factory
        will take a byte array (buffer), an offset (0) and a length (source.Length); it
        effectively returns the count of the bytes read as a single-value sequence. When
        the sequence (reader) is subscribed to, <em>BeginRead</em> will read values, starting
        from the offset, into the buffer. In this case, we will read the whole file. Once
        the file has been read into the buffer, the sequence (reader) will push the single
        value (bytesRead) in to the sequence.
    </p>
    <p>
        This is all fine, but if we want to read chunks of data at a time then this is not
        good enough. We need to specify the buffer size we want to use. Let's start with
        4KB (4096 bytes).
    </p>
    <pre class="csharpcode">
        var bufferSize = 4096;
        var buffer = new byte[bufferSize];
        IObservable&lt;int&gt; reader = factory(buffer, 0, bufferSize);
        reader.Subscribe(
            bytesRead =&gt; 
                Console.WriteLine("Read {0} bytes from file", bytesRead));
    </pre>
    <p>
        This works but will only read a max of 4KB from the file. If the file is larger,
        we want to keep reading all of it. As the <em>Position</em> of the <em>FileStream</em>
        will have advanced to the point it stopped reading, we can reuse the <em>factory</em>
        to reload the buffer. Next, we want to start pushing these bytes into an observable
        sequence. Let's start by creating the signature of an extension method.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;byte&gt; ToObservable(
            this FileStream source, 
            int buffersize, 
            IScheduler scheduler)
        {...}
    </pre>
    <p>
        We can ensure that our extension method is lazily evaluated by using <em>Observable.Create</em>.
        We can also ensure that the <em>FileStream</em> is closed when the consumer disposes
        of the subscription by taking advantage of the <em>Observable.Using</em> operator.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;byte&gt; ToObservable(
            this FileStream source, 
            int buffersize, 
            IScheduler scheduler)
        {
            var bytes = Observable.Create&lt;byte&gt;(o =&gt;
            {
                ...
            });

            return Observable.Using(() =&gt; source, _ =&gt; bytes);
        }
    </pre>
    <p>
        Next, we want to leverage the scheduler's recursive functionality to continuously
        read chunks of data while still providing the user with the ability to dispose/cancel
        when they choose. This creates a bit of a pickle; we can only pass in one state
        parameter but need to manage multiple moving parts (buffer, factory, filestream).
        To do this, we create our own private helper class:
    </p>
    <pre class="csharpcode">
        private sealed class StreamReaderState
        {
            private readonly int _bufferSize;
            private readonly Func&lt;byte[], int, int, IObservable&lt;int&gt;&gt; _factory;

            public StreamReaderState(FileStream source, int bufferSize)
            {
                _bufferSize = bufferSize;
                _factory = Observable.FromAsyncPattern&lt;byte[], int, int, int&gt;(
                    source.BeginRead, 
                    source.EndRead);
                Buffer = new byte[bufferSize];
            }

            public IObservable&lt;int&gt; ReadNext()
            {
                return _factory(Buffer, 0, _bufferSize);
            }

            public byte[] Buffer { get; set; }
        }
    </pre>
    <p>
        This class will allow us to read data into a buffer, then read the next chunk by
        calling <code>ReadNext()</code>. In our <em>Observable.Create</em> delegate, we
        instantiate our helper class and use it to push the buffer into our observable sequence.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;byte&gt; ToObservable(
            this FileStream source, 
            int buffersize, 
            IScheduler scheduler)
        {
            var bytes = Observable.Create&lt;byte&gt;(o =&gt;
            {
                var initialState = new StreamReaderState(source, buffersize);

                initialState
                    .ReadNext()
                    .Subscribe(bytesRead =&gt;
                    {
                        for (int i = 0; i &lt; bytesRead; i++)
                        {
                            o.OnNext(initialState.Buffer[i]);
                        }
                    });
                ...
            });

            return Observable.Using(() =&gt; source, _ =&gt; bytes);
        }
    </pre>
    <p>
        So this gets us off the ground, but we are still do not support reading files larger
        than the buffer. Now, we need to add recursive scheduling. To do this, we need a
        delegate to fit the required signature. We will need one that accepts a <em>StreamReaderState</em>
        and can recursively call an <em>Action&lt;StreamReaderState&gt;</em>.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;byte&gt; ToObservable(
            this FileStream source, 
            int buffersize, 
            IScheduler scheduler)
        {
            var bytes = Observable.Create&lt;byte&gt;(o =&gt;
            {
                var initialState = new StreamReaderState(source, buffersize);

                Action&lt;StreamReaderState, Action&lt;StreamReaderState&gt;&gt; iterator;
                iterator = (state, self) =&gt;
                {
                    state.ReadNext()
                         .Subscribe(bytesRead =&gt;
                                {
                                    for (int i = 0; i &lt; bytesRead; i++)
                                    {
                                        o.OnNext(state.Buffer[i]);
                                    }
                                    self(state);
                                });
                };
                return scheduler.Schedule(initialState, iterator);
            });

            return Observable.Using(() =&gt; source, _ =&gt; bytes);
        }
    </pre>
    <p>
        We now have an <em>iterator</em> action that will:
    </p>
    <ol>
        <li>call <em>ReadNext()</em></li>
        <li>subscribe to the result</li>
        <li>push the buffer into the observable sequence</li>
        <li>and recursively call itself.</li>
    </ol>
    <p>
        We also schedule this recursive action to be called on the provided scheduler. Next,
        we want to complete the sequence when we get to the end of the file. This is easy,
        we maintain the recursion until the <em>bytesRead</em> is 0.
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;byte&gt; ToObservable(
            this FileStream source, 
            int buffersize, 
            IScheduler scheduler)
        {
            var bytes = Observable.Create&lt;byte&gt;(o =&gt;
            {
                var initialState = new StreamReaderState(source, buffersize);

                Action&lt;StreamReaderState, Action&lt;StreamReaderState&gt;&gt; iterator;
                iterator = (state, self) =&gt;
                {
                    state.ReadNext()
                         .Subscribe(bytesRead =&gt;
                                {
                                    for (int i = 0; i &lt; bytesRead; i++)
                                    {
                                        o.OnNext(state.Buffer[i]);
                                    }
                                    if (bytesRead &gt; 0)
                                        self(state);
                                    else
                                        o.OnCompleted();
                                });
                };
                return scheduler.Schedule(initialState, iterator);
            });

            return Observable.Using(() =&gt; source, _ =&gt; bytes);
        }
    </pre>
    <p>
        At this point, we have an extension method that iterates on the bytes from a file
        stream. Finally, let us apply some clean up so that we correctly manage our resources
        and exceptions, and the finished method looks something like this:
    </p>
    <pre class="csharpcode">
        public static IObservable&lt;byte&gt; ToObservable(
            this FileStream source, 
            int buffersize, 
            IScheduler scheduler)
        {
            var bytes = Observable.Create&lt;byte&gt;(o =&gt;
            {
                var initialState = new StreamReaderState(source, buffersize);
                var currentStateSubscription = new SerialDisposable();
                Action&lt;StreamReaderState, Action&lt;StreamReaderState&gt;&gt; iterator =
                (state, self) =&gt;
                    currentStateSubscription.Disposable = state.ReadNext()
                         .Subscribe(
                            bytesRead =&gt;
                            {
                                for (int i = 0; i &lt; bytesRead; i++)
                                {
                                    o.OnNext(state.Buffer[i]);
                                }

                                if (bytesRead &gt; 0)
                                    self(state);
                                else
                                    o.OnCompleted();
                            },
                            o.OnError);

                var scheduledWork = scheduler.Schedule(initialState, iterator);
                return new CompositeDisposable(currentStateSubscription, scheduledWork);
            });

            return Observable.Using(() =&gt; source, _ =&gt; bytes);
        }
    </pre>
    <p>
        This is example code and your mileage may vary. I find that increasing the buffer
        size and returning <em>IObservable&lt;IList&lt;byte&gt;&gt;</em> suits me better,
        but the example above works fine too. The goal here was to provide an example of
        an iterator that provides concurrent I/O access with cancellation and resource-efficient
        buffering.
    </p>
    <!--<a name="ScheduledExceptions"></a>
    <h4>Exceptions from scheduled code</h4>
    <p>
        TODO:
    </p>-->
    <a name="CombinationsOfSchedulerFeatures"></a>
    <h4>Combinations of scheduler features</h4>
    <p>
        We have discussed many features that you can use with the <em>IScheduler</em> interface.
        Most of these examples, however, are actually using extension methods to invoke
        the functionality that we are looking for. The interface itself exposes the richest
        overloads. The extension methods are effectively just making a trade-off; improving
        usability/discoverability by reducing the richness of the overload. If you want
        access to passing state, cancellation, future scheduling and recursion, it is all
        available directly from the interface methods.
    </p>
    <pre class="csharpcode">
        namespace System.Reactive.Concurrency
        {
          public interface IScheduler
          {
            //Gets the scheduler's notion of current time.
            DateTimeOffset Now { get; }

            // Schedules an action to be executed with given state. 
            //  Returns a disposable object used to cancel the scheduled action (best effort).
            IDisposable Schedule&lt;TState&gt;(
                TState state, 
                Func&lt;IScheduler, TState, IDisposable&gt; action);

            // Schedules an action to be executed after dueTime with given state. 
            //  Returns a disposable object used to cancel the scheduled action (best effort).
            IDisposable Schedule&lt;TState&gt;(
                TState state, 
                TimeSpan dueTime, 
                Func&lt;IScheduler, TState, IDisposable&gt; action);

            //Schedules an action to be executed at dueTime with given state. 
            //  Returns a disposable object used to cancel the scheduled action (best effort).
            IDisposable Schedule&lt;TState&gt;(
                TState state, 
                DateTimeOffset dueTime, 
                Func&lt;IScheduler, TState, IDisposable&gt; action);
          }
        }
    </pre>
    <a name="SchedulersIndepth"></a>
    <h2>Schedulers in-depth</h2>
    <p>
        We have largely been concerned with the abstract concept of a scheduler and the
        <em>IScheduler</em> interface. This abstraction allows low-level plumbing to remain
        agnostic towards the implementation of the concurrency model. As in the file reader
        example above, there was no need for the code to know which implementation of <em>IScheduler</em>
        was passed, as this is a concern of the consuming code.
    </p>
    <p>
        Now we take an in-depth look at each implementation of <em>IScheduler</em>, consider
        the benefits and tradeoffs they each make, and when each is appropriate to use.
    </p>
    <a name="ImmediateScheduler"></a>
    <h3>ImmediateScheduler</h3>
    <p>
        The <em>ImmediateScheduler</em> is exposed via the <em>Scheduler.Immediate</em>
        static property. This is the most simple of schedulers as it does not actually schedule
        anything. If you call <em>Schedule(Action)</em> then it will just invoke the action.
        If you schedule the action to be invoked in the future, the <em>ImmediateScheduler</em>
        will invoke a <em>Thread.Sleep</em> for the given period of time and then execute
        the action. In summary, the <em>ImmediateScheduler</em> is synchronous.
    </p>
    <a name="Current"></a>
    <h3>CurrentThreadScheduler</h3>
    <p>
        Like the <em>ImmediateScheduler</em>, the <em>CurrentThreadScheduler</em> is single-threaded.
        It is exposed via the <em>Scheduler.Current</em> static property. The key difference
        is that the <em>CurrentThreadScheduler</em> acts like a message queue or a <i>Trampoline</i>.
        If you schedule an action that itself schedules an action, the <em>CurrentThreadScheduler</em>
        will queue the inner action to be performed later; in contrast, the <em>ImmediateScheduler</em>
        would start working on the inner action straight away. This is probably best explained
        with an example.
    </p>
    <p>
        In this example, we analyze how <em>ImmediateScheduler</em> and <em>CurrentThreadScheduler</em>
        perform nested scheduling differently.
    </p>
    <pre class="csharpcode">
        private static void ScheduleTasks(IScheduler scheduler)
        {
            Action leafAction = () =&gt; Console.WriteLine("----leafAction.");
            Action innerAction = () =&gt;
            {
                Console.WriteLine("--innerAction start.");
                scheduler.Schedule(leafAction);
                Console.WriteLine("--innerAction end.");
            };
            Action outerAction = () =&gt;
            {
                Console.WriteLine("outer start.");
                scheduler.Schedule(innerAction);
                Console.WriteLine("outer end.");
            };
            scheduler.Schedule(outerAction);
        }
        public void CurrentThreadExample()
        {
            ScheduleTasks(Scheduler.CurrentThread);
            /*Output: 
            outer start. 
            outer end. 
            --innerAction start. 
            --innerAction end. 
            ----leafAction. 
            */ 
        }
        public void ImmediateExample()
        {
            ScheduleTasks(Scheduler.Immediate);
            /*Output: 
            outer start. 
            --innerAction start. 
            ----leafAction. 
            --innerAction end. 
            outer end. 
            */ 
        }
    </pre>
    <p>
        Note how the <em>ImmediateScheduler</em> does not really "schedule" anything at
        all, all work is performed immediately (synchronously). As soon as <em>Schedule</em>
        is called with a delegate, that delegate is invoked. The <em>CurrentThreadScheduler</em>,
        however, invokes the first delegate, and, when nested delegates are scheduled, queues
        them to be invoked later. Once the initial delegate is complete, the queue is checked
        for any remaining delegates (i.e. nested calls to <em>Schedule</em>) and they are
        invoked. The difference here is quite important as you can potentially get out-of-order
        execution, unexpected blocking, or even deadlocks by using the wrong one.
    </p>
    <a name="Dispatcher"></a>
    <h3>DispatcherScheduler</h3>
    <p>
        The <em>DispatcherScheduler</em> is found in <em>System.Reactive.Window.Threading.dll</em>
        (for WPF, Silverlight 4 and Silverlight 5). When actions are scheduled using the
        <em>DispatcherScheduler</em>, they are effectively marshaled to the <em>Dispatcher</em>'s
        <em>BeginInvoke</em> method. This will add the action to the end of the dispatcher's
        <i>Normal</i> priority queue of work. This provides similar queuing semantics to
        the <em>CurrentThreadScheduler</em> for nested calls to <em>Schedule</em>.
    </p>
    <p>
        When an action is scheduled for future work, then a <em>DispatcherTimer</em> is
        created with a matching interval. The callback for the timer's tick will stop the
        timer and re-schedule the work onto the <em>DispatcherScheduler</em>. If the <em>DispatcherScheduler</em>
        determines that the <em>dueTime</em> is actually not in the future then no timer
        is created, and the action will just be scheduled normally.
    </p>
    <p>
        I would like to highlight a hazard of using the <em>DispatcherScheduler</em>. You
        can construct your own instance of a <em>DispatcherScheduler</em> by passing in
        a reference to a <em>Dispatcher</em>. The alternative way is to use the static property
        <em>DispatcherScheduler.Instance</em>. This can introduce hard to understand problems
        if it is not used properly. The static property does not return a reference to a
        static field, but creates a new instance each time, with the static property <em>Dispatcher.CurrentDispatcher</em>
        as the constructor argument. If you access <em>Dispatcher.CurrentDispatcher</em>
        from a thread that is not the UI thread, it will thus give you a new instance of
        a <em>Dispatcher</em>, but it will not be the instance you were hoping for.
    </p>
    <p>
        For example, imagine that we have a WPF application with an <em>Observable.Create</em>
        method. In the delegate that we pass to <em>Observable.Create</em>, we want to schedule
        the notifications on the dispatcher. We think this is a good idea because any consumers
        of the sequence would get the notifications on the dispatcher for free.
    </p>
    <pre class="csharpcode">
        var fileLines = Observable.Create&lt;string&gt;(
            o =&gt;
            {
                var dScheduler = DispatcherScheduler.Instance;
                var lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    var localLine = line;
                    dScheduler.Schedule(
                        () =&gt; o.OnNext(localLine));
                }
                return Disposable.Empty;
            });
    </pre>
    <p>
        This code may intuitively seem correct, but actually takes away power from consumers
        of the sequence. When we subscribe to the sequence, we decide that reading a file
        on the UI thread is a bad idea. So we add in a <code>SubscribeOn(Scheduler.NewThread)</code>
        to the chain as below:
    </p>
    <pre class="csharpcode">
        fileLines
            .SubscribeOn(Scheduler.ThreadPool)
            .Subscribe(line =&gt; Lines.Add(line));
    </pre>
    <p>
        This causes the create delegate to be executed on a new thread. The delegate will
        read the file then get an instance of a <em>DispatcherScheduler</em>. The <em>DispatcherScheduler</em>
        tries to get the <em>Dispatcher</em> for the current thread, but we are no longer
        on the UI thread, so there isn't one. As such, it creates a new dispatcher that
        is used for the <em>DispatcherScheduler</em> instance. We schedule some work (the
        notifications), but, as the underlying <em>Dispatcher</em> has not been run, nothing
        happens; we do not even get an exception. I have seen this on a commercial project
        and it left quite a few people scratching their heads.
    </p>
    <p>
        This takes us to one of our guidelines regarding scheduling: <q>the use of <em>SubscribeOn</em>
            and <em>ObserveOn</em> should only be invoked by the final subscriber</q>. If
        you introduce scheduling in your own extension methods or service methods, you should
        allow the consumer to specify their own scheduler. We will see more reasons for
        this guidance in the next chapter.
    </p>
    <a name="EventLoopScheduler"></a>
    <h3>EventLoopScheduler</h3>
    <p>
        The <em>EventLoopScheduler</em> allows you to designate a specific thread to a scheduler.
        Like the <em>CurrentThreadScheduler</em> that acts like a trampoline for nested
        scheduled actions, the <em>EventLoopScheduler</em> provides the same trampoline
        mechanism. The difference is that you provide an <em>EventLoopScheduler</em> with
        the thread you want it to use for scheduling instead, of just picking up the current
        thread.
    </p>
    <p>
        The <em>EventLoopScheduler</em> can be created with an empty constructor, or you
        can pass it a thread factory delegate.
    </p>
    <pre class="csharpcode">
        // Creates an object that schedules units of work on a designated thread.
        public EventLoopScheduler()
        {...}

        // Creates an object that schedules units of work on a designated thread created by the 
        //  provided factory function.
        public EventLoopScheduler(Func&lt;ThreadStart, Thread&gt; threadFactory)
        {...}
    </pre>
    <p>
        The overload that allows you to pass a factory enables you to customize the thread
        before it is assigned to the <em>EventLoopScheduler</em>. For example, you can set
        the thread name, priority, culture and most importantly whether the thread is a
        background thread or not. Remember that if you do not set the thread's property
        <em>IsBackground</em> to false, then your application will not terminate until it
        the thread is terminated. The <em>EventLoopScheduler</em> implements <em>IDisposable</em>,
        and calling Dispose will allow the thread to terminate. As with any implementation
        of <em>IDisposable</em>, it is appropriate that you explicitly manage the lifetime
        of the resources you create.
    </p>
    <p>
        This can work nicely with the <em>Observable.Using</em> method, if you are so inclined.
        This allows you to bind the lifetime of your <em>EventLoopScheduler</em> to that
        of an observable sequence - for example, this <code>GetPrices</code> method that
        takes an <em>IScheduler</em> for an argument and returns an observable sequence.
    </p>
    <pre class="csharpcode">
        private IObservable&lt;Price&gt; GetPrices(IScheduler scheduler)
        {...}
    </pre>
    <p>
        Here we bind the lifetime of the <em>EventLoopScheduler</em> to that of the result
        from the <code>GetPrices</code> method.
    </p>
    <pre class="csharpcode">
        Observable.Using(()=&gt;new EventLoopScheduler(), els=&gt; GetPrices(els))
                .Subscribe(...)
    </pre>
    <a name="NewThread"></a>
    <h3>New Thread</h3>
    <p>
        If you do not wish to manage the resources of a thread or an <em>EventLoopScheduler</em>,
        then you can use <em>NewThreadScheduler</em>. You can create your own instance of
        <em>NewThreadScheduler</em> or get access to the static instance via the property
        <em>Scheduler.NewThread</em>. Like <em>EventLoopScheduler</em>, you can use the
        parameterless constructor or provide your own thread factory function. If you do
        provide your own factory, be careful to set the <em>IsBackground</em> property appropriately.
    </p>
    <p>
        When you call <em>Schedule</em> on the <em>NewThreadScheduler</em>, you are actually
        creating an <em>EventLoopScheduler</em> under the covers. This way, any nested scheduling
        will happen on the same thread. Subsequent (non-nested) calls to <em>Schedule</em>
        will create a new <em>EventLoopScheduler</em> and call the thread factory function
        for a new thread too.
    </p>
    <p>
        In this example we run a piece of code reminiscent of our comparison between <em>Immediate</em>
        and <em>Current</em> schedulers. The difference here, however, is that we track
        the <code>ThreadId</code> that the action is performed on. We use the <em>Schedule</em>
        overload that allows us to pass the Scheduler instance into our nested delegates.
        This allows us to correctly nest calls.
    </p>
    <pre class="csharpcode">
        private static IDisposable OuterAction(IScheduler scheduler, string state)
        {
            Console.WriteLine("{0} start. ThreadId:{1}", 
                state, 
                Thread.CurrentThread.ManagedThreadId);
            scheduler.Schedule(state + ".inner", InnerAction);
            Console.WriteLine("{0} end. ThreadId:{1}", 
                state, 
                Thread.CurrentThread.ManagedThreadId);
            return Disposable.Empty;
        }
        private static IDisposable InnerAction(IScheduler scheduler, string state)
        {
            Console.WriteLine("{0} start. ThreadId:{1}", 
                state, 
                Thread.CurrentThread.ManagedThreadId);
            scheduler.Schedule(state + ".Leaf", LeafAction);
            Console.WriteLine("{0} end. ThreadId:{1}", 
                state, 
                Thread.CurrentThread.ManagedThreadId);
            return Disposable.Empty;
        }
        private static IDisposable LeafAction(IScheduler scheduler, string state)
        {
            Console.WriteLine("{0}. ThreadId:{1}", 
                state, 
                Thread.CurrentThread.ManagedThreadId);
            return Disposable.Empty;
        }
    </pre>
    <p>
        When executed with the <em>NewThreadScheduler</em> like this:
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Starting on thread :{0}", 
            Thread.CurrentThread.ManagedThreadId);
        Scheduler.NewThread.Schedule("A", OuterAction);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Starting on thread :9</div>
        <div class="line">A start. ThreadId:10</div>
        <div class="line">A end. ThreadId:10</div>
        <div class="line">A.inner start . ThreadId:10</div>
        <div class="line">A.inner end. ThreadId:10</div>
        <div class="line">A.inner.Leaf. ThreadId:10</div>
    </div>
    <p>
        As you can see, the results are very similar to the <em>CurrentThreadScheduler</em>,
        except that the trampoline happens on a separate thread. This is in fact exactly
        the output we would get if we used an <em>EventLoopScheduler</em>. The differences
        between usages of the <em>EventLoopScheduler</em> and the <em>NewThreadScheduler</em>
        start to appear when we introduce a second (non-nested) scheduled task.
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Starting on thread :{0}", 
            Thread.CurrentThread.ManagedThreadId);
        Scheduler.NewThread.Schedule("A", OuterAction);
        Scheduler.NewThread.Schedule("B", OuterAction);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Starting on thread :9</div>
        <div class="line">A start. ThreadId:10</div>
        <div class="line">A end. ThreadId:10</div>
        <div class="line">A.inner start . ThreadId:10</div>
        <div class="line">A.inner end. ThreadId:10</div>
        <div class="line">A.inner.Leaf. ThreadId:10</div>
        <div class="line">B start. ThreadId:11</div>
        <div class="line">B end. ThreadId:11</div>
        <div class="line">B.inner start . ThreadId:11</div>
        <div class="line">B.inner end. ThreadId:11</div>
        <div class="line">B.inner.Leaf. ThreadId:11</div>
    </div>
    <p>
        Note that there are now three threads at play here. Thread 9 is the thread we started
        on and threads 10 and 11 are performing the work for our two calls to Schedule.
    </p>
    <a name="ThreadPool"></a>
    <h3>Thread Pool</h3>
    <p>
        The <em>ThreadPoolScheduler</em> will simply just tunnel requests to the <em>ThreadPool</em>.
        For requests that are scheduled as soon as possible, the action is just sent to
        <em>ThreadPool.QueueUserWorkItem</em>. For requests that are scheduled in the future,
        a <em>System.Threading.Timer</em> is used.
    </p>
    <p>
        As all actions are sent to the <em>ThreadPool</em>, actions can potentially run
        out of order. Unlike the previous schedulers we have looked at, nested calls are
        not guaranteed to be processed serially. We can see this by running the same test
        as above but with the <em>ThreadPoolScheduler</em>.
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Starting on thread :{0}", 
            Thread.CurrentThread.ManagedThreadId);
        Scheduler.ThreadPool.Schedule("A", OuterAction);
        Scheduler.ThreadPool.Schedule("B", OuterAction);
    </pre>
    <p>
        The output
    </p>
    <div class="output">
        <div class="line">Starting on thread :9</div>
        <div class="line">A start. ThreadId:10</div>
        <div class="line">A end. ThreadId:10</div>
        <div class="line">A.inner start . ThreadId:10</div>
        <div class="line">A.inner end. ThreadId:10</div>
        <div class="line">A.inner.Leaf. ThreadId:10</div>
        <div class="line">B start. ThreadId:11</div>
        <div class="line">B end. ThreadId:11</div>
        <div class="line">B.inner start . ThreadId:10</div>
        <div class="line">B.inner end. ThreadId:10</div>
        <div class="line">B.inner.Leaf. ThreadId:11</div>
    </div>
    <p>
        Note, that as per the <em>NewThreadScheduler</em> test, we initially start on one
        thread but all the scheduling happens on two other threads. The difference is that
        we can see that part of the second run "B" runs on thread 11 while another part
        of it runs on 10.
    </p>
    <a name="TaskPool"></a>
    <h3>TaskPool</h3>
    <p>
        The <em>TaskPoolScheduler</em> is very similar to the <em>ThreadPoolScheduler</em>
        and, when available (depending on your target framework), you should favor it over
        the later. Like the <em>ThreadPoolScheduler</em>, nested scheduled actions are not
        guaranteed to be run on the same thread. Running the same test with the <em>TaskPoolScheduler</em>
        shows us similar results.
    </p>
    <pre class="csharpcode">
        Console.WriteLine("Starting on thread :{0}", 
            Thread.CurrentThread.ManagedThreadId);
        Scheduler.TaskPool.Schedule("A", OuterAction);
        Scheduler.TaskPool.Schedule("B", OuterAction);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Starting on thread :9</div>
        <div class="line">A start. ThreadId:10</div>
        <div class="line">A end. ThreadId:10</div>
        <div class="line">B start. ThreadId:11</div>
        <div class="line">B end. ThreadId:11</div>
        <div class="line">A.inner start . ThreadId:10</div>
        <div class="line">A.inner end. ThreadId:10</div>
        <div class="line">A.inner.Leaf. ThreadId:10</div>
        <div class="line">B.inner start . ThreadId:11</div>
        <div class="line">B.inner end. ThreadId:11</div>
        <div class="line">B.inner.Leaf. ThreadId:10</div>
    </div>
    <a name="TestScheduler"></a>
    <h3>TestScheduler</h3>
    <p>
        It is worth noting that there is also a <em>TestScheduler</em> accompanied by its
        base classes <em>VirtualTimeScheduler</em> and <em>VirtualTimeSchedulerBase</em>.
        The latter two are not really in the scope of an introduction to Rx, but the former
        is. We will cover all things testing including the <em>TestScheduler</em> in the
        next chapter, <a href="16_TestingRx.html">Testing Rx</a>.
    </p>
    <a name="SelectingAScheduler"></a>
    <h2>Selecting an appropriate scheduler</h2>
    <p>
        With all of these options to choose from, it can be hard to know which scheduler
        to use and when. Here is a simple check list to help you in this daunting task:
    </p>
    <a name="UIApplications"></a>
    <h3>UI Applications</h3>
    <ul>
        <li>The final subscriber is normally the presentation layer and should control the scheduling.</li>
        <li>Observe on the <em>DispatcherScheduler</em> to allow updating of ViewModels</li>
        <li>Subscribe on a background thread to prevent the UI from becoming unresponsive
            <ul>
                <li>If the subscription will not block for more than 50ms then
                    <ul>
                        <li>Use the <em>TaskPoolScheduler</em> if available, or</li>
                        <li>Use the <em>ThreadPoolScheduler</em></li>
                    </ul>
                </li>
                <li>If any part of the subscription could block for longer than 50ms, then you should
                    use the <em>NewThreadScheduler</em>. </li>
            </ul>
        </li>
    </ul>
    <a name="ServiceLayer"></a>
    <h3>Service layer</h3>
    <ul>
        <li>If your service is reading data from a queue of some sort, consider using a dedicated
            <em>EventLoopScheduler</em>. This way, you can preserve order of events</li>
        <li>If processing an item is expensive (>50ms or requires I/O), then consider using
            a <em>NewThreadScheduler</em></li>
        <li>If you just need the scheduler for a timer, e.g. for <em>Observable.Interval</em>
            or <em>Observable.Timer</em>, then favor the <em>TaskPool</em>. Use the <em>ThreadPool</em>
            if the <em>TaskPool</em> is not available for your platform.</li>
    </ul>
    <p class="comment">
        The <em>ThreadPool</em> (and the <em>TaskPool</em> by proxy) have a time delay before
        they will increase the number of threads that they use. This delay is 500ms. Let
        us consider a PC with two cores that we will schedule four actions onto. By default,
        the thread pool size will be the number of cores (2). If each action takes 1000ms,
        then two actions will be sitting in the queue for 500ms before the thread pool size
        is increased. Instead of running all four actions in parallel, which would take
        one second in total, the work is not completed for 1.5 seconds as two of the actions
        sat in the queue for 500ms. For this reason, you should only schedule work that
        is very fast to execute (guideline 50ms) onto the ThreadPool or TaskPool. Conversely,
        creating a new thread is not free, but with the power of processors today the creation
        of a thread for work over 50ms is a small cost.
    </p>
    <p>
        Concurrency is hard. We can choose to make our life easier by taking advantage of
        Rx and its scheduling features. We can improve it even further by only using Rx
        where appropriate. While Rx has concurrency features, these should not be mistaken
        for a concurrency framework. Rx is designed for querying data, and as discussed
        in <a href="01_WhyRx.html#Could">the first chapter</a>, parallel computations or
        composition of asynchronous methods is more appropriate for other frameworks.
    </p>
    <p>
        Rx solves the issues for concurrently generating and consuming data via the <em>ObserveOn</em>/<em>SubscribeOn</em>
        methods. By using these appropriately, we can simplify our code base, increase responsiveness
        and reduce the surface area of our concurrency concerns. Schedulers provide a rich
        platform for processing work concurrently without the need to be exposed directly
        to threading primitives. They also help with common troublesome areas of concurrency
        such as cancellation, passing state and recursion. By reducing the concurrency surface
        area, Rx provides a (relatively) simple yet powerful set of concurrency features
        paving the way to the <a href="http://blogs.msdn.com/b/brada/archive/2003/10/02/50420.aspx">
            pit of success</a>.
    </p>
    <hr />
    <div class="webonly">
        <h1 class="ignoreToc">Additional recommended reading</h1>
        <div align="center">
            <!--Concurrent Programming on Windows: Architecture, Principles, and Patterns (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B0015DYKI4&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>

            <div style="display:inline-block; vertical-align: top;  margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--C# in a nutshell Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B008E6I1K8&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
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
                <!--Parallel Programming with Microsoft® .NET: Design Patterns for Decomposition and Coordination on Multicore Architectures (Patterns & Practices) (Kindle) Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B0043EWUG6&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
						style="width:120px;height:240px;" 
						scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
			</div>
        </div>    </div>
</body>
</html>
