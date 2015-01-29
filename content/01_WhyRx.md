<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Why Rx?</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<!--TODO: Link on this page to the Essential LINQ via affiliates -->
<body>
    <a name="PART1"></a>
    <h1 class="SectionHeader">PART 1 - Getting started</h1>
    <a name="WhyRx"></a>
    <h1>Why Rx?</h1>
    <p>
        Users expect real time data. They want their tweets now. Their order confirmed now.
        They need prices accurate as of now. Their online games need to be responsive. As
        a developer, you demand fire-and-forget messaging. You don't want to be blocked
        waiting for a result. You want to have the result pushed to you when it is ready.
        Even better, when working with result sets, you want to receive individual results
        as they are ready. You do not want to wait for the entire set to be processed before
        you see the first row. The world has moved to push; users are waiting for us to
        catch up. Developers have tools to push data, this is easy. Developers need tools
        to react to push data.
    </p>
    <p>
        Welcome to <a title="Reactive Extensions for .NET" href="http://msdn.microsoft.com/en-us/devlabs/gg577609">
            Reactive Extensions for .NET</a> (Rx). This book is aimed at any .NET developer
        curious about the <em>IObservable&lt;T&gt;</em> and <em>IObserver&lt;T&gt;</em>
        interfaces that have popped up in .NET 4. The Reactive Extensions libraries from
        Microsoft are the implementations of these interfaces that are quickly picking up
        traction with Server, Client and Web developers alike. Rx is a powerfully productive
        development tool. Rx enables developers to solve problems in an elegant, familiar
        and declarative style; often crucially with less code than was possible without
        Rx. By leveraging LINQ, Rx gets to boast the standard benefits of a LINQ implementation<sup><a
            href="#reference1">1</a></sup>.
    </p>
    <dl>
        <dt>Integrated</dt>
        <dd>
            LINQ is integrated into the C# language.
        </dd>
        <dt>Unitive</dt>
        <dd>
            Using LINQ allows you to leverage your existing skills for querying data at rest
            (LINQ to SQL, LINQ to XML or LINQ to objects) to query data in motion. You could
            think of Rx as LINQ to events. LINQ allows you to transition from other paradigms
            into a common paradigm. For example you can transition a standard .NET event, an
            asynchronous method call, a <em>Task</em> or perhaps a 3rd party middleware API
            into a single common Rx paradigm. By leveraging our existing language of choice
            and using familiar operators like <em>Select</em>, <em>Where</em>, <em>GroupBy</em>
            etc, developers can rationalize and communicate designs or code in a common form.
        </dd>
        <dt>Extensible</dt>
        <dd>
            You can extend Rx with your own custom query operators (extension methods).
        </dd>
        <dt>Declarative</dt>
        <dd>
            LINQ allows your code to read as a declaration of <i>what</i> your code does and
            leaves the <i>how</i> to the implementation of the operators.</dd>
        <dt>Composable</dt>
        <dd>
            LINQ features, such as extension methods, lambda syntax and query comprehension
            syntax, provide a fluent API for developers to consume. Queries can be constructed
            with numerous operators. Queries can then be composed together to further produce
            composite queries.</dd>
        <dt>Transformative</dt>
        <dd>
            Queries can transform their data from one type to another. A query might translate
            a single value to another value, aggregated from a sequence of values to a single
            average value or expand a single data value into a sequence of values.
        </dd>
    </dl>
    <a name="WhenRx"></a>
    <h2>When is Rx appropriate?</h2>
    <p>
        Rx offers a natural paradigm for dealing with sequences of events. A sequence can
        contain zero or more events. Rx proves to be most valuable when composing sequences
        of events.
    </p>
    <a name="Should"></a>
    <h3>Should use Rx</h3>
    <p>
        Managing events like these is what Rx was built for:
    </p>
    <ul>
        <li>UI events like mouse move, button click</li>
        <li>Domain events like property changed, collection updated, "Order Filled", "Registration
            accepted" etc.</li>
        <li>Infrastructure events like from file watcher, system and WMI events</li>
        <li>Integration events like a broadcast from a message bus or a push event from WebSockets
            API or other low latency middleware like <a href="http://www.my-channels.com">Nirvana</a></li>
        <li>Integration with a CEP engine like <a href="http://www.microsoft.com/sqlserver/en/us/solutions-technologies/business-intelligence/complex-event-processing.aspx">
            StreamInsight</a> or <a href="http://www.streambase.com">StreamBase</a>.</li>
    </ul>
    <p>
        Interestingly Microsoft's CEP product <a href="http://www.microsoft.com/sqlserver/en/us/solutions-technologies/business-intelligence/complex-event-processing.aspx">
            StreamInsight</a>, which is part of the SQL Server family, also uses LINQ to
        build queries over streaming events of data.
    </p>
    <p>
        Rx is also very well suited for introducing and managing concurrency for the purpose
        of <i>offloading</i>. That is, performing a given set of work concurrently to free
        up the current thread. A very popular use of this is maintaining a responsive UI.
    </p>
    <p>
        You should consider using Rx if you have an existing <em>IEnumerable&lt;T&gt;</em>
        that is attempting to model data in motion. While <em>IEnumerable&lt;T&gt;</em>
        <i>can</i> model data in motion (by using lazy evaluation like <code>yield return</code>),
        it probably won't scale. Iterating over an <em>IEnumerable&lt;T&gt;</em> will consume/block
        a thread. You should either favor the non-blocking nature of Rx via either IObservable&lt;T&gt;
        or consider the <code>async</code> features in .NET 4.5.
    </p>
    <a name="Could"></a>
    <h3>Could use Rx</h3>
    <p>
        Rx can also be used for asynchronous calls. These are effectively sequences of one
        event.
    </p>
    <ul>
        <li>Result of a Task or Task&lt;T&gt;</li>
        <li>Result of an APM method call like <em>FileStream</em> BeginRead/EndRead</li>
    </ul>
    <p>
        You may find the using TPL, Dataflow or <code>async</code> keyword (.NET 4.5) proves
        to be a more natural way of composing asynchronous methods. While Rx can definitely
        help with these scenarios, if there are other more appropriate frameworks at your
        disposal you should consider them first.
    </p>
    <p>
        Rx can be used, but is less suited for, introducing and managing concurrency for
        the purposes of <i>scaling</i> or performing <i>parallel</i> computations. Other
        dedicated frameworks like TPL (Task Parallel Library) or C++ AMP are more appropriate
        for performing parallel compute intensive work.
    </p>
    <p>
        See more on TPL, Dataflow, <code>async</code> and C++ AMP at <a href="http://msdn.microsoft.com/en-us/concurrency">
            Microsoft's Concurrency homepage</a>.
    </p>
    <a name="Wont"></a>
    <h3>Won't use Rx</h3>
    <p>
        Rx and specifically <em>IObservable&lt;T&gt;</em> is not a replacement for <em>IEnumerable&lt;T&gt;</em>.
        I would not recommend trying to take something that is naturally pull based and
        force it to be push based.
    </p>
    <ul>
        <li>Translating existing <em>IEnumerable&lt;T&gt;</em> values to <em>IObservable&lt;T&gt;</em>
            just so that the code base can be "more Rx"</li>
        <li>Message queues. Queues like in MSMQ or a JMS implementation generally have transactionality
            and are by definition sequential. I feel <em>IEnumerable&lt;T&gt;</em> is a natural
            fit for here.</li>
    </ul>
    <p>
        By choosing the best tool for the job your code should be easier to maintain, provide
        better performance and you will probably get better support.
    </p>
    <a name="RxInAction"></a>
    <h2>Rx in action</h2>
    <p>
    </p>
    <p>
        Adopting and learning Rx can be an iterative approach where you can slowly apply
        it to your infrastructure and domain. In a short time you should be able to have
        the skills to produce code, or reduce existing code, to queries composed of simple
        operators. For example this simple ViewModel is all I needed to code to integrate
        a search that is to be executed as a user types.
    </p>
    <!--TODO: Drop an Rx sample bomb here-->
    <pre class="csharpcode">
        public class MemberSearchViewModel : INotifyPropertyChanged
        {
            //Fields removed...
            public MemberSearchViewModel(IMemberSearchModel memberSearchModel,
                ISchedulerProvider schedulerProvider)
            {
                _memberSearchModel = memberSearchModel;
                
                //Run search when SearchText property changes
                this.PropertyChanges(vm =&gt; vm.SearchText)
                    .Subscribe(Search);
            }
            
            //Assume INotifyPropertyChanged implementations of properties...
            public string SearchText { get; set; }
            public bool IsSearching { get; set; }
            public string Error { get; set; }
            public ObservableCollection&lt;string&gt; Results { get; }

            //Search on background thread and return result on dispatcher.
            private void Search(string searchText)
            {
                using (_currentSearch) { }
                IsSearching = true;
                Results.Clear();
                Error = null;

                _currentSearch = _memberSearchModel.SearchMembers(searchText)
                    .Timeout(TimeSpan.FromSeconds(2))
                    .SubscribeOn(_schedulerProvider.TaskPool)
                    .ObserveOn(_schedulerProvider.Dispatcher)
                    .Subscribe(
                        Results.Add,
                        ex =&gt;
                        {
                            IsSearching = false;
                            Error = ex.Message;
                        },
                        () =&gt; { IsSearching = false; });
            }

            ...
        }
    </pre>
    <p>
        While this code snippet is fairly small it supports the following requirements:
    </p>
    <ul>
        <li>Maintains a responsive UI</li>
        <li>Supports timeouts</li>
        <li>Knows when the search is complete</li>
        <li>Allows results to come back one at a time</li>
        <li>Handles errors</li>
        <li>Is unit testable, even with the concurrency concerns</li>
        <li>If a user changes the search, cancel current search and execute new search with
            new text.</li>
    </ul>
    <p>
        To produce this sample is almost a case of composing the operators that match the
        requirements into a single query. The query is small, maintainable, declarative
        and far less code than "rolling your own". There is the added benefit of reusing
        a well tested API. The less code <i>you</i> have to write, the less code <i>you</i>
        have to test, debug and maintain. Creating other queries like the following is simple:
    </p>
    <ul>
        <li>calculating a moving average of a series of values e.g. <b>service level agreements</b>
            for average latencies or downtime</li>
        <li>combining event data from multiple sources e.g.: <b>search results</b> from Bing,
            Google and Yahoo, or <b>sensor data</b> from Accelerometer, Gyro, Magnetometer or
            temperatures</li>
        <li>grouping data e.g. <b>tweets</b> by topic or user, or <b>stock prices</b> by delta
            or liquidity</li>
        <li>filtering data e.g. <b>online game servers</b> within a region, for a specific game
            or with a minimum number of participants.</li>
    </ul>
    <p>
        Push is here. Arming yourself with Rx is a powerful way to meet users' expectations
        of a push world. By understanding and composing the constituent parts of Rx you
        will be able to make short work of complexities of processing incoming events. Rx
        is set to become a day-to-day part of your coding experience.
    </p>
    <hr />
    <p class="comment">
        <a name="reference1"></a><sup>1</sup>
        <a href="http://www.amazon.co.uk/gp/product/B001XT616O/ref=as_li_qf_sp_asin_tl?ie=UTF8&amp;camp=1634&amp;creative=6738&amp;creativeASIN=B001XT616O&amp;linkCode=as2&amp;tag=int0b-21">Essential LINQ</a><img src="http://www.assoc-amazon.co.uk/e/ir?t=int0b-21&amp;l=as2&amp;o=2&amp;a=B001XT616O" width="1" height="1" border="0" alt="" style="border:none !important; margin:0px !important;" class="webonly" />
         - Calvert, Kulkarni
    </p>
    <div class="webonly">
        <h1 class="ignoreToc">Additional recommended reading</h1>
        <div align="center">
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
                <!--Essential Linq Amazon.co.uk-->
                <iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B001XT616O&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
                        style="width:120px;height:240px;" 
                        scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
            </div>
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
        </div>    </div>
</body>
</html>
