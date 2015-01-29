<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Testing Rx</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="TestingRx"></a>
    <h1>Testing Rx</h1>
    <p>
        Testing software has its roots in debugging and demonstrating code. Having largely
        matured past manual tests that try to "break the application", modern quality assurance
        standards demand a level of automation that can help evaluate and prevent bugs.
        While teams of testing specialists are common, more and more coders are expected
        to provide quality guarantees via automated test suites.
    </p>
    <p>
        Up to this point, we have covered a broad scope of Rx, and we have almost enough
        knowledge to start using Rx in anger! Still, many developers would not dream of
        coding without first being able to write tests. Tests can be used to prove that
        code is in fact satisfying requirements, provide a safety net against regression
        and can even help document the code. This chapter makes the assumption that you
        are familiar with the concepts of dependency injection and unit testing with test-doubles,
        such as mocks or stubs.
    </p>
    <p>
        Rx poses some interesting problems to our Test-Driven community:
    </p>
    <ul>
        <li>Scheduling, and therefore threading, is generally avoided in test scenarios as it
            can introduce race conditions which may lead to non-deterministic tests</li>
        <li>Tests should run as fast as possible</li>
        <li>For many, Rx is a new technology/library. Naturally, as we progress on our journey
            to mastering Rx, we may want to refactor some of our previous Rx code. We want to
            use tests to ensure that our refactoring has not altered the internal behavior of
            our code base </li>
        <li>Likewise, tests will ensure nothing breaks when we upgrade versions of Rx. </li>
    </ul>
    <p>
        While we do want to test our code, we don't want to introduce slow or non-deterministic
        tests; indeed, the later would introduce false-negatives or false-positives. If
        we look at the Rx library, there are plenty of methods that involve scheduling (implicitly
        or explicitly), so using Rx effectively makes it hard to avoid scheduling. This
        LINQ query shows us that there are at least 26 extension methods that accept an
        <em>IScheduler</em> as a parameter.
    </p>
    <pre class="csharpcode">
        var query = from method in typeof(Observable).GetMethods()
                    from parameter in method.GetParameters()
                    where typeof (IScheduler).IsAssignableFrom(parameter.ParameterType)
                    group method by method.Name into m
                    orderby m.Key
                    select m.Key;
        foreach (var methodName in query)
        {
            Console.WriteLine(methodName);
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Buffer</div>
        <div class="line">Delay</div>
        <div class="line">Empty</div>
        <div class="line">Generate</div>
        <div class="line">Interval</div>
        <div class="line">Merge</div>
        <div class="line">ObserveOn</div>
        <div class="line">Range</div>
        <div class="line">Repeat</div>
        <div class="line">Replay</div>
        <div class="line">Return</div>
        <div class="line">Sample</div>
        <div class="line">Start</div>
        <div class="line">StartWith</div>
        <div class="line">Subscribe</div>
        <div class="line">SubscribeOn</div>
        <div class="line">Take</div>
        <div class="line">Throttle</div>
        <div class="line">Throw</div>
        <div class="line">TimeInterval</div>
        <div class="line">Timeout</div>
        <div class="line">Timer</div>
        <div class="line">Timestamp</div>
        <div class="line">ToAsync</div>
        <div class="line">ToObservable</div>
        <div class="line">Window</div>
    </div>
    <p>
        Many of these methods also have an overload that does not take an <em>IScheduler</em>
        and instead uses a default instance. TDD/Test First coders will want to opt for
        the overload that accepts the <em>IScheduler</em>, so that they can have some control
        over scheduling in our tests. I will explain why soon.
    </p>
    <p>
        Consider this example, where we create a sequence that publishes values every second
        for five seconds.
    </p>
    <pre class="csharpcode">
        var interval = Observable
            .Interval(TimeSpan.FromSeconds(1))
            .Take(5);
    </pre>
    <p>
        If we were to write a test that ensured that we received five values and they were
        each one second apart, it would take five seconds to run. That would be no good;
        I want hundreds if not thousands of tests to run in five seconds. Another very common
        requirement is to test a timeout. Here, we try to test a timeout of one minute.
    </p>
    <pre class="csharpcode">
        var never = Observable.Never&lt;int&gt;();
        var exceptionThrown = false;
        never.Timeout(TimeSpan.FromMinutes(1))
            .Subscribe(
                i =&gt; Console.WriteLine("This will never run."),
                ex =&gt; exceptionThrown = true);
        Assert.IsTrue(exceptionThrown);
    </pre>
    <p>
        We have two problems here:
    </p>
    <ol>
        <li>either the <em>Assert</em> runs too soon, and the test is pointless as it always
            fails, or</li>
        <li>we have to add a delay of one minute to perform an accurate test</li>
    </ol>
    <p>
        For this test to be useful, it would therefore take one minute to run. Unit tests
        that take one minute to run are not acceptable.
    </p>
    <a name="TestScheduler"></a>
    <h2>TestScheduler</h2>
    <p>
        To our rescue comes the <em>TestScheduler</em>; it introduces the concept of a virtual
        scheduler to allow us to emulate and control time.
    </p>
    <p>
        A virtual scheduler can be conceptualized as a queue of actions to be executed.
        Each are assigned a point in time when they should be executed. We use the <em>TestScheduler</em>
        as a substitute, or <a href="http://xunitpatterns.com/Test%20Double.html">test double</a>,
        for the production <em>IScheduler</em> types. Using this virtual scheduler, we can
        either execute all queued actions, or only those up to a specified point in time.
    </p>
    <p>
        In this example, we schedule a task onto the queue to be run immediately by using
        the simple overload (<em>Schedule(Action)</em>). We then advance the virtual clock
        forward by one tick. By doing so, we execute everything scheduled up to that point
        in time. Note that even though we schedule an action to be executed immediately,
        it will not actually be executed until the clock is manually advanced.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        var wasExecuted = false;
        scheduler.Schedule(() =&gt; wasExecuted = true);
        Assert.IsFalse(wasExecuted);
        scheduler.AdvanceBy(1); //execute 1 tick of queued actions
        Assert.IsTrue(wasExecuted);
    </pre>
    <p class="comment">
        Running and debugging this example may help you to better understand the basics
        of the <em>TestScheduler</em>.
    </p>
    <p>
        The <em>TestScheduler</em> implements the <em>IScheduler</em> interface (naturally)
        and also extends it to allow us to control and monitor virtual time. We are already
        familiar with the <em>IScheduler.Schedule</em> methods, however the <em>AdvanceBy(long)</em>,
        <em>AdvanceTo(long)</em> and <em>Start()</em> methods unique to the <em>TestScheduler</em>
        are of most interest. Likewise, the <em>Clock</em> property will also be of interest,
        as it can help us understand what is happening internally.
    </p>
    <pre class="csharpcode">
        public class TestScheduler : ...
        {
            //Implementation of IScheduler
            public DateTimeOffset Now { get; }
            public IDisposable Schedule&lt;TState&gt;(
                TState state, 
                Func&lt;IScheduler, TState, IDisposable&gt; action)
            public IDisposable Schedule&lt;TState&gt;(
                TState state, 
                TimeSpan dueTime, 
                Func&lt;IScheduler, TState, IDisposable&gt; action)
            public IDisposable Schedule&lt;TState&gt;(
                TState state, 
                DateTimeOffset dueTime, 
                Func&lt;IScheduler, TState, IDisposable&gt; action)

            //Useful extensions for testing
            public bool IsEnabled { get; private set; }
            public TAbsolute Clock { get; protected set; }
            public void Start()
            public void Stop()
            public void AdvanceTo(long time)
            public void AdvanceBy(long time)
            
            //Other methods
            ...
        }
    </pre>
    <a name="AdvanceTo"></a>
    <h3>AdvanceTo</h3>
    <p>
        The <em>AdvanceTo(long)</em> method will execute all the actions that have been
        scheduled up to the absolute time specified. The <em>TestScheduler</em> uses ticks
        as its measurement of time. In this example, we schedule actions to be invoked now,
        in 10 ticks, and in 20 ticks.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        scheduler.Schedule(() =&gt; Console.WriteLine("A")); //Schedule immediately
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("B"));
        scheduler.Schedule(TimeSpan.FromTicks(20), () =&gt; Console.WriteLine("C"));

        Console.WriteLine("scheduler.AdvanceTo(1);");
        scheduler.AdvanceTo(1);
        Console.WriteLine("scheduler.AdvanceTo(10);");
        scheduler.AdvanceTo(10);
        Console.WriteLine("scheduler.AdvanceTo(15);");
        scheduler.AdvanceTo(15);
        Console.WriteLine("scheduler.AdvanceTo(20);");
        scheduler.AdvanceTo(20);
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">scheduler.AdvanceTo(1);</div>
        <div class="line">A</div>
        <div class="line">scheduler.AdvanceTo(10);</div>
        <div class="line">B</div>
        <div class="line">scheduler.AdvanceTo(15);</div>
        <div class="line">scheduler.AdvanceTo(20);</div>
        <div class="line">C</div>
    </div>
    <p>
        Note that nothing happened when we advanced to 15 ticks. All work scheduled before
        15 ticks had been performed and we had not advanced far enough yet to get to the
        next scheduled action.
    </p>
    <a name="AdvanceBy"></a>
    <h3>AdvanceBy</h3>
    <p>
        The <em>AdvanceBy(long)</em> method allows us to move the clock forward a relative
        amount of time. Again, the measurements are in ticks. We can take the last example
        and modify it to use <em>AdvanceBy(long)</em>.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        scheduler.Schedule(() =&gt; Console.WriteLine("A")); //Schedule immediately
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("B"));
        scheduler.Schedule(TimeSpan.FromTicks(20), () =&gt; Console.WriteLine("C"));

        Console.WriteLine("scheduler.AdvanceBy(1);");
        scheduler.AdvanceBy(1);
        Console.WriteLine("scheduler.AdvanceBy(9);");
        scheduler.AdvanceBy(9);
        Console.WriteLine("scheduler.AdvanceBy(5);");
        scheduler.AdvanceBy(5);
        Console.WriteLine("scheduler.AdvanceBy(5);");
        scheduler.AdvanceBy(5);
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">scheduler.AdvanceBy(1);</div>
        <div class="line">A</div>
        <div class="line">scheduler.AdvanceBy(9);</div>
        <div class="line">B</div>
        <div class="line">scheduler.AdvanceBy(5);</div>
        <div class="line">scheduler.AdvanceBy(5);</div>
        <div class="line">C</div>
    </div>
    <a name="Start"></a>
    <h3>Start</h3>
    <p>
        The <em>TestScheduler</em>'s <em>Start()</em> method is an effective way to execute
        everything that has been scheduled. We take the same example again and swap out
        the <em>AdvanceBy(long)</em> calls for a single <em>Start()</em> call.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        scheduler.Schedule(() =&gt; Console.WriteLine("A")); //Schedule immediately
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("B"));
        scheduler.Schedule(TimeSpan.FromTicks(20), () =&gt; Console.WriteLine("C"));

        Console.WriteLine("scheduler.Start();");
        scheduler.Start();
        Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">scheduler.Start();</div>
        <div class="line">A</div>
        <div class="line">B</div>
        <div class="line">C</div>
        <div class="line">scheduler.Clock:20</div>
    </div>
    <p>
        Note that once all of the scheduled actions have been executed, the virtual clock
        matches our last scheduled item (20 ticks).
    </p>
    <p>
        We further extend our example by scheduling a new action to happen after <em>Start()</em>
        has already been called.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        scheduler.Schedule(() =&gt; Console.WriteLine("A"));
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("B"));
        scheduler.Schedule(TimeSpan.FromTicks(20), () =&gt; Console.WriteLine("C"));

        Console.WriteLine("scheduler.Start();");
        scheduler.Start();
        Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
        
        scheduler.Schedule(() =&gt; Console.WriteLine("D"));
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">scheduler.Start();</div>
        <div class="line">A</div>
        <div class="line">B</div>
        <div class="line">C</div>
        <div class="line">scheduler.Clock:20</div>
    </div>
    <p>
        Note that the output is exactly the same; If we want our fourth action to be executed,
        we will have to call <em>Start()</em> again.
    </p>
    <a name="Stop"></a>
    <h3>Stop</h3>
    <p>
        In previous releases of Rx, the <em>Start()</em> method was called <em>Run()</em>.
        Now there is a <em>Stop()</em> method whose name seems to imply some symmetry with
        <em>Start()</em>. All it does however, is set the <em>IsEnabled</em> property to
        false. This property is used as an internal flag to check whether the internal queue
        of actions should continue being executed. The processing of the queue may indeed
        be instigated by <em>Start()</em>, however <em>AdvanceTo</em> or <em>AdvanceBy</em>
        can be used too.
    </p>
    <p>
        In this example, we show how you could use <em>Stop()</em> to pause processing of
        scheduled actions.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        scheduler.Schedule(() =&gt; Console.WriteLine("A"));
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("B"));
        scheduler.Schedule(TimeSpan.FromTicks(15), scheduler.Stop);
        scheduler.Schedule(TimeSpan.FromTicks(20), () =&gt; Console.WriteLine("C"));

        Console.WriteLine("scheduler.Start();");
        scheduler.Start();
        Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">scheduler.Start();</div>
        <div class="line">A</div>
        <div class="line">B</div>
        <div class="line">scheduler.Clock:15</div>
    </div>
    <p>
        Note that "C" never gets printed as we stop the clock at 15 ticks. I have been testing
        Rx successfully for nearly two years now, yet I have not found the need to use the
        <em>Stop()</em> method. I imagine that there are cases that warrant its use; however
        I just wanted to make the point that you do not have to be concerned about the lack
        of use of it in your tests.
    </p>
    <a name="ScheduleCollisions"></a>
    <h3>Schedule collisions</h3>
    <p>
        When scheduling actions, it is possible and even likely that many actions will be
        scheduled for the same point in time. This most commonly would occur when scheduling
        multiple actions for <i>now</i>. It could also happen that there are multiple actions
        scheduled for the same point in the future. The <em>TestScheduler</em> has a simple
        way to deal with this. When actions are scheduled, they are marked with the clock
        time they are scheduled for. If multiple items are scheduled for the same point
        in time, they are queued in order that they were scheduled; when the clock advances,
        all items for that point in time are executed in the order that they were scheduled.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("A"));
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("B"));
        scheduler.Schedule(TimeSpan.FromTicks(10), () =&gt; Console.WriteLine("C"));

        Console.WriteLine("scheduler.Start();");
        scheduler.Start();
        Console.WriteLine("scheduler.Clock:{0}", scheduler.Clock);
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">scheduler.AdvanceTo(10);</div>
        <div class="line">A</div>
        <div class="line">B</div>
        <div class="line">C</div>
        <div class="line">scheduler.Clock:10</div>
    </div>
    <p>
        Note that the virtual clock is at 10 ticks, the time we advanced to.
    </p>
    <a name="TestingRxCode"></a>
    <h2>Testing Rx code</h2>
    <p>
        Now that we have learnt a little bit about the <em>TestScheduler</em>, let's look
        at how we could use it to test our two initial code snippets that use <em>Interval</em>
        and <em>Timeout</em>. We want to execute tests as fast as possible but still maintain
        the semantics of time. In this example we generate our five values one second apart
        but pass in our <em>TestScheduler</em> to the <em>Interval</em> method to use instead
        of the default scheduler.
    </p>
    <pre class="csharpcode">
        [TestMethod]
        public void Testing_with_test_scheduler()
        {
            var expectedValues = new long[] {0, 1, 2, 3, 4};
            var actualValues = new List&lt;long&gt;();
            var scheduler = new TestScheduler();

            var interval = Observable
                .Interval(TimeSpan.FromSeconds(1), scheduler)
                .Take(5);
            interval.Subscribe(actualValues.Add);

            scheduler.Start();
            CollectionAssert.AreEqual(expectedValues, actualValues);
            //Executes in less than 0.01s "on my machine"
        }
    </pre>
    <p>
        While this is mildly interesting, what I think is more important is how we would
        test a real piece of code. Imagine, if you will, a ViewModel that subscribes to
        a stream of prices. As prices are published, it adds them to a collection. Assuming
        this is a WPF or Silverlight implementation, we take the liberty of enforcing that
        the subscription be done on the <em>ThreadPool</em> and the observing is executed
        on the <em>Dispatcher</em>.
    </p>
    <pre class="csharpcode">
        public class MyViewModel : IMyViewModel
        {
            private readonly IMyModel _myModel;
            private readonly ObservableCollection&lt;decimal&gt; _prices;

            public MyViewModel(IMyModel myModel)
            {
                _myModel = myModel;
                _prices = new ObservableCollection&lt;decimal&gt;();
            }

            public void Show(string symbol)
            {
                //TODO: resource mgt, exception handling etc...
                _myModel.PriceStream(symbol)
                        .SubscribeOn(Scheduler.ThreadPool)
                        .ObserveOn(Scheduler.Dispatcher)
                        .Timeout(TimeSpan.FromSeconds(10), Scheduler.ThreadPool)
                        .Subscribe(
                            Prices.Add,
                            ex=&gt;
                                {
                                    if(ex is TimeoutException)
                                        IsConnected = false;
                                });
                IsConnected = true;
            }

            public ObservableCollection&lt;decimal&gt; Prices
            {
                get { return _prices; }
            }

            public bool IsConnected { get; private set; }
        }
    </pre>
    <a name="SchedulerDI"></a>
    <h3>Injecting scheduler dependencies</h3>
    <p>
        While the snippet of code above may do what we want it to, it will be hard to test
        as it is accessing the schedulers via static properties. To help my testing, I have
        created my own interface that exposes the same <em>IScheduler</em> implementations
        that the <em>Scheduler</em> type does, i suggest you adopt this interface too.
    </p>
    <pre class="csharpcode">
        public interface ISchedulerProvider
        {
            IScheduler CurrentThread { get; }
            IScheduler Dispatcher { get; }
            IScheduler Immediate { get; }
            IScheduler NewThread { get; }
            IScheduler ThreadPool { get; }
            //IScheduler TaskPool { get; } 
        }
     </pre>
    <p>
        Whether the <code>TaskPool</code> property should be included or not depends on
        your target platform. If you adopt this concept, feel free to name this type in
        accordance with your naming conventions e.g. <code>SchedulerService</code>, <code>Schedulers</code>.
        The default implementation that we would run in production is implemented as follows:
    </p>
    <pre class="csharpcode">
        public sealed class SchedulerProvider : ISchedulerProvider
        {
            public IScheduler CurrentThread 
            { 
                get { return Scheduler.CurrentThread; } 
            }
            public IScheduler Dispatcher 
            { 
                get { return DispatcherScheduler.Instance; }
            }
            public IScheduler Immediate 
            { 
                get { return Scheduler.Immediate; } 
            }
            public IScheduler NewThread 
            { 
                get { return Scheduler.NewThread; } 
            }
            public IScheduler ThreadPool 
            { 
                get { return Scheduler.ThreadPool; } 
            }
            //public IScheduler TaskPool { get { return Scheduler.TaskPool; } } 
        }
    </pre>
    <p>
        This now allows me to substitute implementations of <em>ISchedulerProvider</em>
        to help with testing. I could mock the <em>ISchedulerProvider</em>, but I find it
        easier to provide a test implementation. My implementation for testing is as follows.
    </p>
    <pre class="csharpcode">
        public sealed class TestSchedulers : ISchedulerProvider
        {
            private readonly TestScheduler _currentThread = new TestScheduler();
            private readonly TestScheduler _dispatcher = new TestScheduler();
            private readonly TestScheduler _immediate = new TestScheduler();
            private readonly TestScheduler _newThread = new TestScheduler();
            private readonly TestScheduler _threadPool = new TestScheduler();
            #region Explicit implementation of ISchedulerService
            IScheduler ISchedulerProvider.CurrentThread { get { return _currentThread; } }
            IScheduler ISchedulerProvider.Dispatcher { get { return _dispatcher; } }
            IScheduler ISchedulerProvider.Immediate { get { return _immediate; } }
            IScheduler ISchedulerProvider.NewThread { get { return _newThread; } }
            IScheduler ISchedulerProvider.ThreadPool { get { return _threadPool; } }
            #endregion
            public TestScheduler CurrentThread { get { return _currentThread; } }
            public TestScheduler Dispatcher { get { return _dispatcher; } }
            public TestScheduler Immediate { get { return _immediate; } }
            public TestScheduler NewThread { get { return _newThread; } }
            public TestScheduler ThreadPool { get { return _threadPool; } }
        }
    </pre>
    <p>
        Note that <em>ISchedulerProvider</em> is implemented explicitly. This means that,
        in our tests, we can access the <em>TestScheduler</em> instances directly, but our
        system under test (SUT) still just sees the interface implementation. I can now
        write some tests for my ViewModel. Below, we test a modified version of the <em>MyViewModel</em>
        class that takes an <em>ISchedulerProvider</em> and uses that instead of the static
        schedulers from the <em>Scheduler</em> class. We also use the popular <a href="http://code.google.com/p/moq/">
            Moq</a> framework in order to mock out our model.
    </p>
    <pre class="csharpcode">
        [TestInitialize]
        public void SetUp()
        {
            _myModelMock = new Mock&lt;IMyModel&gt;();
            _schedulerProvider = new TestSchedulers();
            _viewModel = new MyViewModel(_myModelMock.Object, _schedulerProvider);
        }

        [TestMethod]
        public void Should_add_to_Prices_when_Model_publishes_price()
        {
            decimal expected = 1.23m;
            var priceStream = new Subject&lt;decimal&gt;();
            _myModelMock.Setup(svc =&gt; svc.PriceStream(It.IsAny&lt;string&gt;())).Returns(priceStream);

            _viewModel.Show("SomeSymbol");
            //Schedule the OnNext
            _schedulerProvider.ThreadPool.Schedule(() =&gt; priceStream.OnNext(expected));  

            Assert.AreEqual(0, _viewModel.Prices.Count);
            //Execute the OnNext action
            _schedulerProvider.ThreadPool.AdvanceBy(1);  
            Assert.AreEqual(0, _viewModel.Prices.Count);
            //Execute the OnNext handler
            _schedulerProvider.Dispatcher.AdvanceBy(1);  
            Assert.AreEqual(1, _viewModel.Prices.Count);
            Assert.AreEqual(expected, _viewModel.Prices.First());
        }

        [TestMethod]
        public void Should_disconnect_if_no_prices_for_10_seconds()
        {
            var timeoutPeriod = TimeSpan.FromSeconds(10);
            var priceStream = Observable.Never&lt;decimal&gt;();
            _myModelMock.Setup(svc =&gt; svc.PriceStream(It.IsAny&lt;string&gt;())).Returns(priceStream);

            _viewModel.Show("SomeSymbol");

            _schedulerProvider.ThreadPool.AdvanceBy(timeoutPeriod.Ticks - 1);
            Assert.IsTrue(_viewModel.IsConnected);
            _schedulerProvider.ThreadPool.AdvanceBy(timeoutPeriod.Ticks);
            Assert.IsFalse(_viewModel.IsConnected);
        }
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">2 passed, 0 failed, 0 skipped, took 0.41 seconds (MSTest 10.0).</div>
    </div>
    <p>
        These two tests ensure five things:
    </p>
    <ol>
        <li>That the <em>Price</em> property has prices added to it as the model produces them</li>
        <li>That the sequence is subscribed to on the ThreadPool</li>
        <li>That the <em>Price</em> property is updated on the Dispatcher i.e. the sequence
            is observed on the Dispatcher</li>
        <li>That a timeout of 10 seconds between prices will set the ViewModel to disconnected.</li>
        <li>The tests run fast. While the time to run the tests is not that impressive, most
            of that time seems to be spent warming up my test harness. Moreover, increasing
            the test count to 10 only adds 0.03seconds. In general, on a modern CPU, I expect
            to see unit tests run at a rate of +1000 tests per second</li>
    </ol>
    <p>
        Usually, I would not have more than one assert/verify per test, but here it does
        help illustrate a point. In the first test, we can see that only once both the <code>
            ThreadPool</code> and the <code>Dispatcher</code> schedulers have been run will
        we get a result. In the second test, it helps to verify that the timeout is not
        less than 10 seconds.
    </p>
    <p>
        In some scenarios, you are not interested in the scheduler and you want to
        be focusing your tests on other functionality. If this is the case, then you may
        want to create another test implementation of the <em>ISchedulerProvider</em> that
        returns the <em>ImmediateScheduler</em> for all of its members. That can help reduce
        the noise in your tests.
    </p>
    <pre class="csharpcode">  
        public sealed class ImmediateSchedulers : ISchedulerService
        {
            public IScheduler CurrentThread { get { return Scheduler.Immediate; } }
            public IScheduler Dispatcher { get { return Scheduler.Immediate; } }
            public IScheduler Immediate { get { return Scheduler.Immediate; } }
            public IScheduler NewThread { get { return Scheduler.Immediate; } }
            public IScheduler ThreadPool { get { return Scheduler.Immediate; } }
        }
    </pre>
    <a name="AdvancedFeatures"></a>
    <h2>Advanced features - ITestableObserver</h2>
    <p>
        The <em>TestScheduler</em> provides further advanced features. I find that I am
        able to get by quite well without these methods, but others may find them useful.
        Perhaps this is because I have found myself accustomed to testing without them from
        using earlier versions of Rx.
    </p>
    <a name="StartIOb"></a>
    <h3>Start(Func&lt;IObservable&lt;T&gt;&gt;)</h3>
    <p>
        There are three overloads to <em>Start</em>, which are used to start an observable
        sequence at a given time, record the notifications it makes and dispose of the subscription
        at a given time. This can be confusing at first, as the parameterless overload of
        <em>Start</em> is quite unrelated. These three overloads return an <em>ITestableObserver&lt;T&gt;</em>
        which allows you to record the notifications from an observable sequence, much like
        the <em>Materialize</em> method we saw in the <a href="08_Transformation.html#MaterializeAndDematerialize">
            Transformation chapter</a>.
    </p>
    <pre class="csharpcode">
        public interface ITestableObserver&lt;T&gt; : IObserver&lt;T&gt;
        {
            // Gets recorded notifications received by the observer.
            IList&lt;Recorded&lt;Notification&lt;T&gt;&gt;&gt; Messages { get; }
        }
    </pre>
    <p>
        While there are three overloads, we will look at the most specific one first. This
        overload takes four parameters:
    </p>
    <ol>
        <li>an observable sequence factory delegate</li>
        <li>the point in time to invoke the factory</li>
        <li>the point in time to subscribe to the observable sequence returned from the factory</li>
        <li>the point in time to dispose of the subscription</li>
    </ol>
    <p>
        The <i>time</i> for the last three parameters is measured in ticks, as per the rest
        of the <em>TestScheduler</em> members.
    </p>
    <pre class="csharpcode">
        public ITestableObserver&lt;T&gt; Start&lt;T&gt;(
            Func&lt;IObservable&lt;T&gt;&gt; create, 
            long created, 
            long subscribed, 
            long disposed)
        {...}
    </pre>
    <p>
        We could use this method to test the <em>Observable.Interval</em> factory method.
        Here, we create an observable sequence that spawns a value every second for 4 seconds.
        We use the <em>TestScheduler.Start</em> method to create and subscribe to it immediately
        (by passing 0 for the second and third parameters). We dispose of our subscription
        after 5 seconds. Once the <em>Start</em> method has run, we output what we have
        recorded.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        var source = Observable.Interval(TimeSpan.FromSeconds(1), scheduler)
            .Take(4);

        var testObserver = scheduler.Start(
            () =&gt; source, 
            0, 
            0, 
            TimeSpan.FromSeconds(5).Ticks);

        Console.WriteLine("Time is {0} ticks", scheduler.Clock);
        Console.WriteLine("Received {0} notifications", testObserver.Messages.Count);
        foreach (Recorded&lt;Notification&lt;long&gt;&gt; message in testObserver.Messages)
        {
            Console.WriteLine("{0} @ {1}", message.Value, message.Time);
        }
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">Time is 50000000 ticks</div>
        <div class="line">Received 5 notifications</div>
        <div class="line">OnNext(0) @ 10000001</div>
        <div class="line">OnNext(1) @ 20000001</div>
        <div class="line">OnNext(2) @ 30000001</div>
        <div class="line">OnNext(3) @ 40000001</div>
        <div class="line">OnCompleted() @ 40000001</div>
    </div>
    <p>
        Note that the <em>ITestObserver&lt;T&gt;</em> records <code>OnNext</code> and <code>
            OnCompleted</code> notifications. If the sequence was to terminate in error,
        the <em>ITestObserver&lt;T&gt;</em> would record the <code>OnError</code> notification
        instead.
    </p>
    <p>
        We can play with the input variables to see the impact it makes. We know that the
        <em>Observable.Interval</em> method is a Cold Observable, so the virtual time of
        the creation is not relevant. Changing the virtual time of the subscription can
        change our results. If we change it to 2 seconds, we will notice that if
        we leave the disposal time at 5 seconds, we will miss some messages.
    </p>
    <pre class="csharpcode">
        var testObserver = scheduler.Start(
            () =&gt; Observable.Interval(TimeSpan.FromSeconds(1), scheduler).Take(4), 
            0,
            TimeSpan.FromSeconds(2).Ticks,
            TimeSpan.FromSeconds(5).Ticks);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Time is 50000000 ticks</div>
        <div class="line">Received 2 notifications</div>
        <div class="line">OnNext(0) @ 30000000</div>
        <div class="line">OnNext(1) @ 40000000</div>
    </div>
    <p>
        We start the subscription at 2 seconds; the <em>Interval</em> produces values after
        each second (i.e. second 3 and 4), and we dispose on second 5. So we miss the other
        two <code>OnNext</code> messages as well as the <code>OnCompleted</code> message.
    </p>
    <p>
        There are two other overloads to this <em>TestScheduler.Start</em> method.
    </p>
    <pre class="csharpcode">
        public ITestableObserver&lt;T&gt; Start&lt;T&gt;(Func&lt;IObservable&lt;T&gt;&gt; create, long disposed)
        {
          if (create == null)
            throw new ArgumentNullException("create");
          else
            return this.Start&lt;T&gt;(create, 100L, 200L, disposed);
        }

        public ITestableObserver&lt;T&gt; Start&lt;T&gt;(Func&lt;IObservable&lt;T&gt;&gt; create)
        {
          if (create == null)
            throw new ArgumentNullException("create");
          else
            return this.Start&lt;T&gt;(create, 100L, 200L, 1000L);
        }
    </pre>
    <p>
        As you can see, these overloads just call through to the variant we have been looking
        at, but passing some default values. I am not sure why these default values are
        special; I can not imagine why you would want to use these two methods, unless your
        specific use case matched that specific configuration exactly.
    </p>
    <a name="CreateColdObservable"></a>
    <h3>CreateColdObservable</h3>
    <p>
        Just as we can record an observable sequence, we can also use <em>CreateColdObservable</em>
        to playback a set of <em>Recorded&lt;Notification&lt;int&gt;&gt;</em>. The signature
        for <em>CreateColdObservable</em> simply takes a <code>params</code> array of recorded
        notifications.
    </p>
    <pre class="csharpcode">
        // Creates a cold observable from an array of notifications.
        // Returns a cold observable exhibiting the specified message behavior.
        public ITestableObservable&lt;T&gt; CreateColdObservable&lt;T&gt;(
            params Recorded&lt;Notification&lt;T&gt;&gt;[] messages)
        {...}
    </pre>
    <p>
        The <em>CreateColdObservable</em> returns an <em>ITestableObservable&lt;T&gt;</em>.
        This interface extends <em>IObservable&lt;T&gt;</em> by exposing the list of "subscriptions"
        and the list of messages it will produce.
    </p>
    <pre class="csharpcode">
        public interface ITestableObservable&lt;T&gt; : IObservable&lt;T&gt;
        {
            // Gets the subscriptions to the observable.
            IList&lt;Subscription&gt; Subscriptions { get; }

            // Gets the recorded notifications sent by the observable.
            IList&lt;Recorded&lt;Notification&lt;T&gt;&gt;&gt; Messages { get; }
        }
    </pre>
    <p>
        Using <em>CreateColdObservable</em>, we can emulate the <em>Observable.Interval</em>
        test we had earlier.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        var source = scheduler.CreateColdObservable(
            new Recorded&lt;Notification&lt;long&gt;&gt;(10000000, Notification.CreateOnNext(0L)),
            new Recorded&lt;Notification&lt;long&gt;&gt;(20000000, Notification.CreateOnNext(1L)),
            new Recorded&lt;Notification&lt;long&gt;&gt;(30000000, Notification.CreateOnNext(2L)),
            new Recorded&lt;Notification&lt;long&gt;&gt;(40000000, Notification.CreateOnNext(3L)),
            new Recorded&lt;Notification&lt;long&gt;&gt;(40000000, Notification.CreateOnCompleted&lt;long&gt;())
            );

        var testObserver = scheduler.Start(
            () =&gt; source,
            0,
            0,
            TimeSpan.FromSeconds(5).Ticks);

        Console.WriteLine("Time is {0} ticks", scheduler.Clock);
        Console.WriteLine("Received {0} notifications", testObserver.Messages.Count);
        foreach (Recorded&lt;Notification&lt;long&gt;&gt; message in testObserver.Messages)
        {
            Console.WriteLine("  {0} @ {1}", message.Value, message.Time);
        }
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">Time is 50000000 ticks</div>
        <div class="line">Received 5 notifications</div>
        <div class="line">OnNext(0) @ 10000001</div>
        <div class="line">OnNext(1) @ 20000001</div>
        <div class="line">OnNext(2) @ 30000001</div>
        <div class="line">OnNext(3) @ 40000001</div>
        <div class="line">OnCompleted() @ 40000001</div>
    </div>
    <p>
        Note that our output is exactly the same as the previous example with <em>Observable.Interval</em>.
    </p>
    <a name="CreateHotObservable"></a>
    <h3>CreateHotObservable</h3>
    <p>
        We can also create hot test observable sequences using the <em>CreateHotObservable</em>
        method. It has the same parameters and return value as <em>CreateColdObservable</em>;
        the difference is that the virtual time specified for each message is now relative
        to when the observable was created, not when it is subscribed to as per the <em>CreateColdObservable</em>
        method.
    </p>
    <p>
        This example is just that last "cold" sample, but creating a Hot observable instead.
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        var source = scheduler.CreateHotObservable(
            new Recorded&lt;Notification&lt;long&gt;&gt;(10000000, Notification.CreateOnNext(0L)),
        ...    
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">Time is 50000000 ticks</div>
        <div class="line">Received 5 notifications</div>
        <div class="line">OnNext(0) @ 10000000</div>
        <div class="line">OnNext(1) @ 20000000</div>
        <div class="line">OnNext(2) @ 30000000</div>
        <div class="line">OnNext(3) @ 40000000</div>
        <div class="line">OnCompleted() @ 40000000</div>
    </div>
    <p>
        Note that the output is almost the same. Scheduling of the creation and subscription
        do not affect the Hot Observable, therefore the notifications happen 1 tick earlier
        than their Cold counterparts.
    </p>
    <p>
        We can see the major difference a Hot Observable bears by changing the virtual create
        time and virtual subscribe time to be different values. With a Cold Observable,
        the virtual create time has no real impact, as subscription is what initiates any
        action. This means we can not miss any early message on a Cold Observable. For Hot
        Observables, we can miss messages if we subscribe too late. Here, we create the
        Hot Observable immediately, but only subscribe to it after 1 second (thus missing
        the first message).
    </p>
    <pre class="csharpcode">
        var scheduler = new TestScheduler();
        var source = scheduler.CreateHotObservable(
            new Recorded&gt;Notification&gt;long&lt;&lt;(10000000, Notification.CreateOnNext(0L)),
            new Recorded&gt;Notification&gt;long&lt;&lt;(20000000, Notification.CreateOnNext(1L)),
            new Recorded&gt;Notification&gt;long&lt;&lt;(30000000, Notification.CreateOnNext(2L)),
            new Recorded&gt;Notification&gt;long&lt;&lt;(40000000, Notification.CreateOnNext(3L)),
            new Recorded&gt;Notification&gt;long&lt;&lt;(40000000, Notification.CreateOnCompleted&gt;long&lt;())
            );

        var testObserver = scheduler.Start(
            () =&lt; source,
            0,
            TimeSpan.FromSeconds(1).Ticks,
            TimeSpan.FromSeconds(5).Ticks);

        Console.WriteLine("Time is {0} ticks", scheduler.Clock);
        Console.WriteLine("Received {0} notifications", testObserver.Messages.Count);
        foreach (Recorded&gt;Notification&gt;long&lt;&lt; message in testObserver.Messages)
        {
            Console.WriteLine("  {0} @ {1}", message.Value, message.Time);
        }
    </pre>
    <p>
        Output:</p>
    <div class="output">
        <div class="line">Time is 50000000 ticks</div>
        <div class="line">Received 4 notifications</div>
        <div class="line">OnNext(1) @ 20000000</div>
        <div class="line">OnNext(2) @ 30000000</div>
        <div class="line">OnNext(3) @ 40000000</div>
        <div class="line">OnCompleted() @ 40000000</div>
    </div>
    <a name="CreateObserver"></a>
    <h3>CreateObserver</h3>
    <p>
        Finally, if you do not want to use the <em>TestScheduler.Start</em> methods, and
        you need more fine-grained control over your observer, you can use <em>TestScheduler.CreateObserver()</em>.
        This will return an <em>ITestObserver</em> that you can use to manage the subscriptions
        to your observable sequences with. Furthermore, you will still be exposed to the
        recorded messages and any subscribers.
    </p>
    <p>
        Current industry standards demand broad coverage of automated unit tests to meet
        quality assurance standards. Concurrent programming, however, is often a difficult
        area to test well. Rx delivers a well-designed implementation of testing features,
        allowing deterministic and high-throughput testing. The <em>TestScheduler</em> provides
        methods to control virtual time and produce observable sequences for testing. This
        ability to easily and reliably test concurrent systems sets Rx apart from many other
        libraries.
    </p>
    <hr />
    <div class="webonly">
        <h1 class="ignoreToc">Additional recommended reading</h1>
        <div align="center">

            <!--Test Driven development (By example) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=0321146530&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>
            
            <!--Art of Unit testing Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=1933988274&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>
            
            <!--Refactoring (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B007WTFWJ6&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>

            <!--Domain Driven Design (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
				<iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B00794TAUG&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
						style="width:120px;height:240px;" 
						scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
			</div>
        </div>    </div>
</body>
</html>
