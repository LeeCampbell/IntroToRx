---
title: 'Intro to Rx - Why Rx?'
...

PART 1 - Getting started {.SectionHeader}
========================

Why Rx?
=======

Users expect real time data. They want their tweets now. Their order
confirmed now. They need prices accurate as of now. Their online games
need to be responsive. As a developer, you demand fire-and-forget
messaging. You don't want to be blocked waiting for a result. You want
to have the result pushed to you when it is ready. Even better, when
working with result sets, you want to receive individual results as they
are ready. You do not want to wait for the entire set to be processed
before you see the first row. The world has moved to push; users are
waiting for us to catch up. Developers have tools to push data, this is
easy. Developers need tools to react to push data.

Welcome to [Reactive Extensions for
.NET](http://msdn.microsoft.com/en-us/devlabs/gg577609 "Reactive Extensions for .NET")
(Rx). This book is aimed at any .NET developer curious about the
*IObservable\<T\>* and *IObserver\<T\>* interfaces that have popped up
in .NET 4. The Reactive Extensions libraries from Microsoft are the
implementations of these interfaces that are quickly picking up traction
with Server, Client and Web developers alike. Rx is a powerfully
productive development tool. Rx enables developers to solve problems in
an elegant, familiar and declarative style; often crucially with less
code than was possible without Rx. By leveraging LINQ, Rx gets to boast
the standard benefits of a LINQ implementation^[1](#reference1)^.

Integrated
:   LINQ is integrated into the C\# language.
Unitive
:   Using LINQ allows you to leverage your existing skills for querying
    data at rest (LINQ to SQL, LINQ to XML or LINQ to objects) to query
    data in motion. You could think of Rx as LINQ to events. LINQ allows
    you to transition from other paradigms into a common paradigm. For
    example you can transition a standard .NET event, an asynchronous
    method call, a *Task* or perhaps a 3rd party middleware API into a
    single common Rx paradigm. By leveraging our existing language of
    choice and using familiar operators like *Select*, *Where*,
    *GroupBy* etc, developers can rationalize and communicate designs or
    code in a common form.
Extensible
:   You can extend Rx with your own custom query operators (extension
    methods).
Declarative
:   LINQ allows your code to read as a declaration of *what* your code
    does and leaves the *how* to the implementation of the operators.
Composable
:   LINQ features, such as extension methods, lambda syntax and query
    comprehension syntax, provide a fluent API for developers to
    consume. Queries can be constructed with numerous operators. Queries
    can then be composed together to further produce composite queries.
Transformative
:   Queries can transform their data from one type to another. A query
    might translate a single value to another value, aggregated from a
    sequence of values to a single average value or expand a single data
    value into a sequence of values.

When is Rx appropriate?
-----------------------

Rx offers a natural paradigm for dealing with sequences of events. A
sequence can contain zero or more events. Rx proves to be most valuable
when composing sequences of events.

### Should use Rx

Managing events like these is what Rx was built for:

-   UI events like mouse move, button click
-   Domain events like property changed, collection updated, "Order
    Filled", "Registration accepted" etc.
-   Infrastructure events like from file watcher, system and WMI events
-   Integration events like a broadcast from a message bus or a push
    event from WebSockets API or other low latency middleware like
    [Nirvana](http://www.my-channels.com)
-   Integration with a CEP engine like
    [StreamInsight](http://www.microsoft.com/sqlserver/en/us/solutions-technologies/business-intelligence/complex-event-processing.aspx)
    or [StreamBase](http://www.streambase.com).

Interestingly Microsoft's CEP product
[StreamInsight](http://www.microsoft.com/sqlserver/en/us/solutions-technologies/business-intelligence/complex-event-processing.aspx),
which is part of the SQL Server family, also uses LINQ to build queries
over streaming events of data.

Rx is also very well suited for introducing and managing concurrency for
the purpose of *offloading*. That is, performing a given set of work
concurrently to free up the current thread. A very popular use of this
is maintaining a responsive UI.

You should consider using Rx if you have an existing *IEnumerable\<T\>*
that is attempting to model data in motion. While *IEnumerable\<T\>*
*can* model data in motion (by using lazy evaluation like
`yield return`), it probably won't scale. Iterating over an
*IEnumerable\<T\>* will consume/block a thread. You should either favor
the non-blocking nature of Rx via either IObservable\<T\> or consider
the `async` features in .NET 4.5.

### Could use Rx

Rx can also be used for asynchronous calls. These are effectively
sequences of one event.

-   Result of a Task or Task\<T\>
-   Result of an APM method call like *FileStream* BeginRead/EndRead

You may find the using TPL, Dataflow or `async` keyword (.NET 4.5)
proves to be a more natural way of composing asynchronous methods. While
Rx can definitely help with these scenarios, if there are other more
appropriate frameworks at your disposal you should consider them first.

Rx can be used, but is less suited for, introducing and managing
concurrency for the purposes of *scaling* or performing *parallel*
computations. Other dedicated frameworks like TPL (Task Parallel
Library) or C++ AMP are more appropriate for performing parallel compute
intensive work.

See more on TPL, Dataflow, `async` and C++ AMP at [Microsoft's
Concurrency homepage](http://msdn.microsoft.com/en-us/concurrency).

### Won't use Rx

Rx and specifically *IObservable\<T\>* is not a replacement for
*IEnumerable\<T\>*. I would not recommend trying to take something that
is naturally pull based and force it to be push based.

-   Translating existing *IEnumerable\<T\>* values to *IObservable\<T\>*
    just so that the code base can be "more Rx"
-   Message queues. Queues like in MSMQ or a JMS implementation
    generally have transactionality and are by definition sequential. I
    feel *IEnumerable\<T\>* is a natural fit for here.

By choosing the best tool for the job your code should be easier to
maintain, provide better performance and you will probably get better
support.

Rx in action
------------

Adopting and learning Rx can be an iterative approach where you can
slowly apply it to your infrastructure and domain. In a short time you
should be able to have the skills to produce code, or reduce existing
code, to queries composed of simple operators. For example this simple
ViewModel is all I needed to code to integrate a search that is to be
executed as a user types.

``` {.csharpcode}
        public class MemberSearchViewModel : INotifyPropertyChanged
        {
            //Fields removed...
            public MemberSearchViewModel(IMemberSearchModel memberSearchModel,
                ISchedulerProvider schedulerProvider)
            {
                _memberSearchModel = memberSearchModel;
                
                //Run search when SearchText property changes
                this.PropertyChanges(vm => vm.SearchText)
                    .Subscribe(Search);
            }
            
            //Assume INotifyPropertyChanged implementations of properties...
            public string SearchText { get; set; }
            public bool IsSearching { get; set; }
            public string Error { get; set; }
            public ObservableCollection<string> Results { get; }

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
                        ex =>
                        {
                            IsSearching = false;
                            Error = ex.Message;
                        },
                        () => { IsSearching = false; });
            }

            ...
        }
    
```

While this code snippet is fairly small it supports the following
requirements:

-   Maintains a responsive UI
-   Supports timeouts
-   Knows when the search is complete
-   Allows results to come back one at a time
-   Handles errors
-   Is unit testable, even with the concurrency concerns
-   If a user changes the search, cancel current search and execute new
    search with new text.

To produce this sample is almost a case of composing the operators that
match the requirements into a single query. The query is small,
maintainable, declarative and far less code than "rolling your own".
There is the added benefit of reusing a well tested API. The less code
*you* have to write, the less code *you* have to test, debug and
maintain. Creating other queries like the following is simple:

-   calculating a moving average of a series of values e.g. **service
    level agreements** for average latencies or downtime
-   combining event data from multiple sources e.g.: **search results**
    from Bing, Google and Yahoo, or **sensor data** from Accelerometer,
    Gyro, Magnetometer or temperatures
-   grouping data e.g. **tweets** by topic or user, or **stock prices**
    by delta or liquidity
-   filtering data e.g. **online game servers** within a region, for a
    specific game or with a minimum number of participants.

Push is here. Arming yourself with Rx is a powerful way to meet users'
expectations of a push world. By understanding and composing the
constituent parts of Rx you will be able to make short work of
complexities of processing incoming events. Rx is set to become a
day-to-day part of your coding experience.

* * * * *

^1^ [Essential
LINQ](http://www.amazon.co.uk/gp/product/B001XT616O/ref=as_li_qf_sp_asin_tl?ie=UTF8&camp=1634&creative=6738&creativeASIN=B001XT616O&linkCode=as2&tag=int0b-21)![](http://www.assoc-amazon.co.uk/e/ir?t=int0b-21&l=as2&o=2&a=B001XT616O)
- Calvert, Kulkarni

Additional recommended reading {.ignoreToc}
==============================
