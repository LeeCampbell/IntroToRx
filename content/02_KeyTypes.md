<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Key Types</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<!--TODO: Link on this page to the C# in a nutshell amazon via affiliates -->
<body>
    <a name="KeyTypes"></a>
    <h1>Key types</h1>
    <!--p>
        To use a framework you need to have a familiarty with the key features and their
        benefits. Without this you find yourself just pasting samples from forums and hacking
        code until it works, kind of. Then the next poor developer to maintain the code
        base ha to try to figure out what the intention of your code base was. Fate is only
        too kind when that maintenence developer is the same as the original developer.
        Rx is powerful, but also allows for a simplification of your code. To write good
        Reactive code you have to know the basics.
    </p-->
    <p>
        There are two key types to understand when working with Rx, and a subset of auxiliary
        types that will help you to learn Rx more effectively. The <em>IObserver&lt;T&gt;</em>
        and <em>IObservable&lt;T&gt;</em> form the fundamental building blocks for Rx, while
        implementations of <em>ISubject&lt;TSource, TResult&gt;</em> reduce the learning
        curve for developers new to Rx.
    </p>
    <p>
        Many are familiar with LINQ and its many popular forms like LINQ to Objects, LINQ
        to SQL &amp; LINQ to XML. Each of these common implementations allows you query
        <i>data at rest</i>; Rx offers the ability to query <i>data in motion</i>. Essentially
        Rx is built upon the foundations of the <a href="http://en.wikipedia.org/wiki/Observer_pattern">
            Observer</a> pattern. .NET already exposes some other ways to implement the
        Observer pattern such as multicast delegates or events (which are usually multicast
        delegates). Multicast delegates are not ideal however as they exhibit the following
        less desirable features;
    </p>
    <ul>
        <li>In C#, events have a curious interface. Some find the <code>+=</code> and <code>
            -=</code> operators an unnatural way to register a callback</li>
        <li>Events are difficult to compose</li>
        <li>Events don't offer the ability to be easily queried over time</li>
        <li>Events are a common cause of accidental memory leaks</li>
        <li>Events do not have a standard pattern for signaling completion</li>
        <li>Events provide almost no help for concurrency or multithreaded applications. e.g.
            To raise an event on a separate thread requires you to do all of the plumbing</li>
    </ul>
    <p>
        Rx looks to solve these problems. Here I will introduce you to the building blocks
        and some basic types that make up Rx.
    </p>
    <a name="IObservable"></a>
    <h2>IObservable&lt;T&gt;</h2>
    <p>
        <a title="IObservable(Of T) interface - MSDN" href="http://msdn.microsoft.com/en-us/library/dd990377.aspx">
            <em>IObservable&lt;T&gt;</em></a> is one of the two new core interfaces for
        working with Rx. It is a simple interface with just a <a href="http://msdn.microsoft.com/en-us/library/dd782981(v=VS.100).aspx">
            Subscribe</a> method. Microsoft is so confident that this interface will be
        of use to you it has been included in the BCL as of version 4.0 of .NET. You should
        be able to think of anything that implements <em>IObservable&lt;T&gt;</em> as a
        streaming sequence of <em>T</em> objects. So if a method returned an <em>IObservable&lt;Price&gt;</em>
        I could think of it as a stream of Prices.
    </p>
    <pre class="csharpcode">
        //Defines a provider for push-based notification.
        public interface IObservable&lt;out T&gt;
        {
            //Notifies the provider that an observer is to receive notifications.
            IDisposable Subscribe(IObserver&lt;T&gt; observer);
        }
    </pre>
    <p class="comment">
        .NET already has the concept of Streams with the type and sub types of <em>System.IO.Stream</em>.
        The <em>System.IO.Stream</em> implementations are commonly used to stream data (generally
        bytes) to or from an I/O device like a file, network or block of memory. <em>System.IO.Stream</em>
        implementations can have both the ability to read and write, and sometimes the ability
        to seek (i.e. fast forward through a stream or move backwards). When I refer to
        an instance of <em>IObservable&lt;T&gt;</em> as a stream, it does not exhibit the
        seek or write functionality that streams do. This is a fundamental difference preventing
        Rx being built on top of the <em>System.IO.Stream</em> paradigm. Rx does however
        have the concept of forward streaming (push), disposing (closing) and completing
        (eof). Rx also extends the metaphor by introducing concurrency constructs, and query
        operations like transformation, merging, aggregating and expanding. These features
        are also not an appropriate fit for the existing <em>System.IO.Stream</em> types.
        Some others refer to instances of <em>IObservable&lt;T&gt;</em> as Observable Collections,
        which I find hard to understand. While the observable part makes sense to me, I
        do not find them like collections at all. You generally cannot sort, insert or remove
        items from an <em>IObservable&lt;T&gt;</em> instance like I would expect you can
        with a collection. Collections generally have some sort of backing store like an
        internal array. The values from an <em>IObservable&lt;T&gt;</em> source are not
        usually pre-materialized as you would expect from a normal collection. There is
        also a type in WPF/Silverlight called an <em>ObservableCollection&lt;T&gt;</em>
        that does exhibit collection-like behavior, and is very well suited to this description.
        In fact <em>IObservable&lt;T&gt;</em> integrates very well with <em>ObservableCollection&lt;T&gt;</em>
        instances. So to save on any confusion we will refer to instances of <em>IObservable&lt;T&gt;</em>
        as <b>sequences</b>. While instances of <em>IEnumerable&lt;T&gt;</em> are also sequences,
        we will adopt the convention that they are sequences of <i>data at rest</i>, and
        <em>IObservable&lt;T&gt;</em> instances are sequences of <i>data in motion</i>.
    </p>
    <a name="IObserver"></a>
    <h2>IObserver&lt;T&gt;</h2>
    <p>
        <a title="IObserver(Of T) interface - MSDN" href="http://msdn.microsoft.com/en-us/library/dd783449.aspx">
            <em>IObserver&lt;T&gt;</em></a> is the other one of the two core interfaces
        for working with Rx. It too has made it into the BCL as of .NET 4.0. Don't worry
        if you are not on .NET 4.0 yet as the Rx team have included these two interfaces
        in a separate assembly for .NET 3.5 and Silverlight users. <em>IObservable&lt;T&gt;</em>
        is meant to be the &quot;functional dual of <em>IEnumerable&lt;T&gt;</em>&quot;.
        If you want to know what that last statement means, then enjoy the hours of videos
        on <a href="http://channel9.msdn.com/tags/Rx/">Channel9</a> where they discuss the
        mathematical purity of the types. For everyone else it means that where an <em>IEnumerable&lt;T&gt;</em>
        can effectively yield three things (the next value, an exception or the end of the
        sequence), so too can <em>IObservable&lt;T&gt;</em> via <em>IObserver&lt;T&gt;</em>'s
        three methods <em>OnNext(T)</em>, <em>OnError(Exception)</em> and <em>OnCompleted()</em>.
    </p>
    <pre class="csharpcode">
        //Provides a mechanism for receiving push-based notifications.
        public interface IObserver&lt;in T&gt;
        {
            //Provides the observer with new data.
            void OnNext(T value);
            //Notifies the observer that the provider has experienced an error condition.
            void OnError(Exception error);
            //Notifies the observer that the provider has finished sending push-based notifications.
            void OnCompleted();
        }
    </pre>
    <p>
        Rx has an implicit contract that must be followed. An implementation of <em>IObserver&lt;T&gt;</em>
        may have zero or more calls to <em>OnNext(T)</em> followed optionally by a call
        to either <em>OnError(Exception)</em> or <em>OnCompleted()</em>. This protocol ensures
        that if a sequence terminates, it is always terminated by an <em>OnError(Exception)</em>,
        <b>or</b> an <em>OnCompleted()</em>. This protocol does not however demand that
        an <em>OnNext(T)</em>, <em>OnError(Exception)</em> or <em>OnCompleted()</em> ever
        be called. This enables to concept of empty and infinite sequences. We will look
        into this more later.
    </p>
    <p>
        Interestingly, while you will be exposed to the <em>IObservable&lt;T&gt;</em> interface
        frequently if you work with Rx, in general you will not need to be concerned with
        <em>IObserver&lt;T&gt;</em>. This is due to Rx providing anonymous implementations
        via methods like <em>Subscribe</em>.
    </p>
    <a name="ImplementingIObserverAndIObservable"></a>
    <h3>Implementing IObserver&lt;T&gt; and IObservable&lt;T&gt;</h3>
    <p>
        It is quite easy to implement each interface. If we wanted to create an observer
        that printed values to the console it would be as easy as this.
    </p>
    <pre class="csharpcode">
        public class MyConsoleObserver&lt;T&gt; : IObserver&lt;T&gt;
        {
            public void OnNext(T value)
            {
                Console.WriteLine("Received value {0}", value);
            }

            public void OnError(Exception error)
            {
                Console.WriteLine("Sequence faulted with {0}", error);
            }

            public void OnCompleted()
            {
                Console.WriteLine("Sequence terminated");
            }
        }
    </pre>
    <p>
        Implementing an observable sequence is a little bit harder. An overly simplified
        implementation that returned a sequence of numbers could look like this.
    </p>
    <pre class="csharpcode">
        public class MySequenceOfNumbers : IObservable&lt;int&gt;
        {
            public IDisposable Subscribe(IObserver&lt;int&gt; observer)
            {
                observer.OnNext(1);
                observer.OnNext(2);
                observer.OnNext(3);
                observer.OnCompleted();
                return Disposable.Empty;
            }
        }
    </pre>
    <p>
        We can tie these two implementations together to get the following output
    </p>
    <pre class="csharpcode">
        var numbers = new MySequenceOfNumbers();
        var observer = new MyConsoleObserver&lt;int&gt;();
        numbers.Subscribe(observer);
    </pre>
    <p>
        Output:
    </p>
    <div class="output">
        <div class="line">Received value 1</div>
        <div class="line">Received value 2</div>
        <div class="line">Received value 3</div>
        <div class="line">Sequence terminated</div>
    </div>
    <p>
        The problem we have here is that this is not really reactive at all. This implementation
        is blocking, so we may as well use an <em>IEnumerable&lt;T&gt;</em> implementation
        like a <em>List&lt;T&gt;</em> or an array.
    </p>
    <p>
        This problem of implementing the interfaces should not concern us too much. You
        will find that when you use Rx, you do not have the need to actually implement these
        interfaces, Rx provides all of the implementations you need out of the box. Let's
        have a look at the simple ones.
    </p>
    <a name="Subject"></a>
    <h2>Subject&lt;T&gt;</h2>
    <p>
        I like to think of the <em>IObserver&lt;T&gt;</em> and the <em>IObservable&lt;T&gt;</em>
        as the 'reader' and 'writer' or, 'consumer' and 'publisher' interfaces. If you were
        to create your own implementation of <em>IObservable&lt;T&gt;</em> you may find
        that while you want to publicly expose the IObservable characteristics you still
        need to be able to publish items to the subscribers, throw errors and notify when
        the sequence is complete. Why that sounds just like the methods defined in <em>IObserver&lt;T&gt;</em>!
        While it may seem odd to have one type implementing both interfaces, it does make
        life easy. This is what <a title="Using Rx Subjects - MSDN" href="http://msdn.microsoft.com/en-us/library/hh242969(v=VS.103).aspx">
            subjects</a> can do for you. <a title="Subject(Of T) - MSDN" href="http://msdn.microsoft.com/en-us/library/hh229173(v=VS.103).aspx">
                <em>Subject&lt;T&gt;</em></a> is the most basic of the subjects. Effectively
        you can expose your <em>Subject&lt;T&gt;</em> behind a method that returns <em>IObservable&lt;T&gt;</em>
        but internally you can use the <em>OnNext</em>, <em>OnError</em> and <em>OnCompleted</em>
        methods to control the sequence.
    </p>
    <p>
        In this very basic example, I create a subject, subscribe to that subject and then
        publish values to the sequence (by calling <code>subject.OnNext(T)</code>).
    </p>
    <pre class="csharpcode">
      static void Main(string[] args)
      {
        var subject = new Subject&lt;string&gt;();
        WriteSequenceToConsole(subject);

        subject.OnNext(&quot;a&quot;);
        subject.OnNext(&quot;b&quot;);
        subject.OnNext(&quot;c&quot;);
        Console.ReadKey();
      }
      
      //Takes an IObservable&lt;string&gt; as its parameter. 
      //Subject&lt;string&gt; implements this interface.
      static void WriteSequenceToConsole(IObservable&lt;string&gt; sequence)
      {
        //The next two lines are equivalent.
        //sequence.Subscribe(value=>Console.WriteLine(value));
        sequence.Subscribe(Console.WriteLine);
      }
    </pre>
    <p>
        Note that the <code>WriteSequenceToConsole</code> method takes an <em>IObservable&lt;string&gt;</em>
        as it only wants access to the subscribe method. Hang on, doesn't the <em>Subscribe</em>
        method need an <em>IObserver&lt;string&gt;</em> as an argument? Surely <em>Console.WriteLine</em>
        does not match that interface. Well it doesn't, but the Rx team supply me with an
        Extension Method to <em>IObservable&lt;T&gt;</em> that just takes an <a title="Action(Of T) Delegate - MSDN"
            href="http://msdn.microsoft.com/en-us/library/018hxwa8.aspx"><em>Action&lt;T&gt;</em></a>.
        The action will be executed every time an item is published. There are <a title="ObservableExtensions class - MSDN"
            href="http://msdn.microsoft.com/en-us/library/system.observableextensions(v=VS.103).aspx">
            other overloads to the Subscribe extension method</a> that allows you to pass
        combinations of delegates to be invoked for <em>OnNext</em>, <em>OnCompleted</em>
        and <em>OnError</em>. This effectively means I don't need to implement <em>IObserver&lt;T&gt;</em>.
        Cool.
    </p>
    <p>
        As you can see, <em>Subject&lt;T&gt;</em> could be quite useful for getting started
        in Rx programming. <em>Subject&lt;T&gt;</em> however, is a basic implementation.
        There are three siblings to <em>Subject&lt;T&gt;</em> that offer subtly different
        implementations which can drastically change the way your program runs.
    </p>
    <!--
        G.A:
 
        ReplaySubject<T>
                     Rewrite second sentence.
    -->
    <a name="ReplaySubject"></a>
    <h2>ReplaySubject&lt;T&gt;</h2>
    <p>
        <a title="ReplaySubject(Of T) - MSDN" href="http://msdn.microsoft.com/en-us/library/hh211810(v=VS.103).aspx">
            <em>ReplaySubject&lt;T&gt;</em></a> provides the feature of caching values and
        then replaying them for any late subscriptions. Consider this example where we have
        moved our first publication to occur before our subscription
    </p>
    <pre class="csharpcode">
        static void Main(string[] args)
        {
          var subject = new Subject&lt;string&gt;();

          subject.OnNext(&quot;a&quot;);
          WriteSequenceToConsole(subject);
  
          subject.OnNext(&quot;b&quot;);
          subject.OnNext(&quot;c&quot;);
          Console.ReadKey();
        }
    </pre>
    <p>
        The result of this would be that 'b' and 'c' would be written to the console, but
        'a' ignored. If we were to make the minor change to make subject a <em>ReplaySubject&lt;T&gt;</em>
        we would see all publications again.
    </p>
    <pre class="csharpcode">
        var subject = new ReplaySubject&lt;string&gt;();

        subject.OnNext("a");
        WriteSequenceToConsole(subject);

        subject.OnNext("b");
        subject.OnNext("c");
    </pre>
    <p>
        This can be very handy for eliminating race conditions. Be warned though, the default
        constructor of the <em>ReplaySubject&lt;T&gt;</em> will create an instance that
        caches every value published to it. In many scenarios this could create unnecessary
        memory pressure on the application. <em>ReplaySubject&lt;T&gt;</em> allows you to
        specify simple cache expiry settings that can alleviate this memory issue. One option
        is that you can specify the size of the buffer in the cache. In this example we
        create the <em>ReplaySubject&lt;T&gt;</em> with a buffer size of 2, and so only
        get the last two values published prior to our subscription:
    </p>
    <pre class="csharpcode">
        public void ReplaySubjectBufferExample()
        {
            var bufferSize = 2;
            var subject = new ReplaySubject&lt;string&gt;(bufferSize);

            subject.OnNext("a");
            subject.OnNext("b");
            subject.OnNext("c");
            subject.Subscribe(Console.WriteLine);
            subject.OnNext("d");
        }
    </pre>
    <p>
        Here the output would show that the value 'a' had been dropped from the cache, but
        values 'b' and 'c' were still valid. The value 'd' was published after we subscribed
        so it is also written to the console.
    </p>
    <div class="output">
        <div class="line">Output:</div>
        <div class="line">b</div>
        <div class="line">c</div>
        <div class="line">d</div>
    </div>
    <p>
        Another option for preventing the endless caching of values by the <em>ReplaySubject&lt;T&gt;</em>,
        is to provide a window for the cache. In this example, instead of creating a <em>ReplaySubject&lt;T&gt;</em>
        with a buffer size, we specify a window of time that the cached values are valid
        for.
    </p>
    <pre class="csharpcode">
        public void ReplaySubjectWindowExample()
        {
            var window = TimeSpan.FromMilliseconds(150);
            var subject = new ReplaySubject&lt;string&gt;(window);

            subject.OnNext("w");
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            subject.OnNext("x");
            Thread.Sleep(TimeSpan.FromMilliseconds(100));
            subject.OnNext("y");
            subject.Subscribe(Console.WriteLine);
            subject.OnNext("z");
        }
    </pre>
    <p>
        In the above example the window was specified as 150 milliseconds. Values are published
        100 milliseconds apart. Once we have subscribed to the subject, the first value
        is 200ms old and as such has expired and been removed from the cache.
    </p>
    <div class="output">
        <div class="line">Output:</div>
        <div class="line">x</div>
        <div class="line">y</div>
        <div class="line">z</div>
    </div>
    <a name="BehaviorSubject"></a>
    <h2>BehaviorSubject&lt;T&gt;</h2>
    <p>
        <a title="BehaviorSubject(Of T) - MSDN" href="http://msdn.microsoft.com/en-us/library/hh211949(v=VS.103).aspx">
            <em>BehaviorSubject&lt;T&gt;</em></a> is similar to <em>ReplaySubject&lt;T&gt;</em>
        except it only remembers the last publication. <em>BehaviorSubject&lt;T&gt;</em>
        also requires you to provide it a default value of <em>T</em>. This means that all
        subscribers will receive a value immediately (unless it is already completed).
    </p>
    <p>
        In this example the value 'a' is written to the console:
    </p>
    <pre class="csharpcode">
        public void BehaviorSubjectExample()
        {
            //Need to provide a default value.
            var subject = new BehaviorSubject&lt;string&gt;(&quot;a&quot;);
            subject.Subscribe(Console.WriteLine);
        }
    </pre>
    <p>
        In this example the value 'b' is written to the console, but not 'a'.
    </p>
    <pre class="csharpcode">
        public void BehaviorSubjectExample2()
        {
          var subject = new BehaviorSubject&lt;string&gt;(&quot;a&quot;);
          subject.OnNext(&quot;b&quot;);
          subject.Subscribe(Console.WriteLine);
        }
    </pre>
    <p>
        In this example the values 'b', 'c' &amp; 'd' are all written to the console, but
        again not 'a'
    </p>
    <pre class="csharpcode">
        public void BehaviorSubjectExample3()
        {
          var subject = new BehaviorSubject&lt;string&gt;(&quot;a&quot;);

          subject.OnNext(&quot;b&quot;);
          subject.Subscribe(Console.WriteLine);
          subject.OnNext(&quot;c&quot;);
          subject.OnNext(&quot;d&quot;);
        }
    </pre>
    <p>
        Finally in this example, no values will be published as the sequence has completed.
        Nothing is written to the console.
    </p>
    <pre class="csharpcode">
        public void BehaviorSubjectCompletedExample()
        {
          var subject = new BehaviorSubject&lt;string&gt;(&quot;a&quot;);
          subject.OnNext(&quot;b&quot;);
          subject.OnNext(&quot;c&quot;);
          subject.OnCompleted();
          subject.Subscribe(Console.WriteLine);
        }
    </pre>
    <p>
        That note that there is a difference between a <em>ReplaySubject&lt;T&gt;</em> with a buffer size
        of one (commonly called a 'replay one subject') and a <em>BehaviorSubject&lt;T&gt;</em>.
        A <em>BehaviorSubject&lt;T&gt;</em> requires an initial value. With the assumption
        that neither subjects have completed, then you can be sure that the <em>BehaviorSubject&lt;T&gt;</em>
        will have a value. You cannot be certain with the <em>ReplaySubject&lt;T&gt;</em> however. With this
        in mind, it is unusual to ever complete a <em>BehaviorSubject&lt;T&gt;</em>. Another difference is
        that a replay-one-subject will still cache its value once it has been completed.
        So subscribing to a completed <em>BehaviorSubject&lt;T&gt;</em> we can be sure to not receive any
        values, but with a <em>ReplaySubject&lt;T&gt;</em> it is possible.
    </p>
    <p>
        <em>BehaviorSubject&lt;T&gt;</em>s are often associated with class <a href="http://msdn.microsoft.com/en-us/library/65zdfbdt(v=vs.71).aspx">
            properties</a>. As they always have a value and can provide change notifications,
        they could be candidates for backing fields to properties.
    </p>
    <a name="AsyncSubject"></a>
    <h2>AsyncSubject&lt;T&gt;</h2>
    <p>
        <a title="AsyncSubject(Of T) - MSDN" href="http://msdn.microsoft.com/en-us/library/hh229363(v=VS.103).aspx">
            <em>AsyncSubject&lt;T&gt;</em></a> is similar to the Replay and Behavior subjects
        in the way that it caches values, however it will only store the last value, and
        only publish it when the sequence is completed. The general usage of the <em>AsyncSubject&lt;T&gt;</em>
        is to only ever publish one value then immediately complete. This means that is
        becomes quite comparable to <em>Task&lt;T&gt;</em>.
    </p>
    <p>
        In this example no values will be published as the sequence never completes. No
        values will be written to the console.
    </p>
    <pre class="csharpcode">
        static void Main(string[] args)
        {
          var subject = new AsyncSubject&lt;string&gt;();
          subject.OnNext(&quot;a&quot;);
          WriteSequenceToConsole(subject);
          subject.OnNext(&quot;b&quot;);
          subject.OnNext(&quot;c&quot;);
          Console.ReadKey();
        }
    </pre>
    <p>
        In this example we invoke the <em>OnCompleted</em> method so the last value 'c'
        is written to the console:
    </p>
    <pre class="csharpcode">
        static void Main(string[] args)
        {
          var subject = new AsyncSubject&lt;string&gt;();

          subject.OnNext(&quot;a&quot;);
          WriteSequenceToConsole(subject);
          subject.OnNext(&quot;b&quot;);
          subject.OnNext(&quot;c&quot;);
          subject.OnCompleted();
          Console.ReadKey();
        }
    </pre>
    <a name="ImplicitContracts"></a>
    <h2>Implicit contracts</h2>
    <p>
        There are implicit contacts that need to be upheld when working with Rx as mentioned
        above. The key one is that once a sequence is completed, no more activity can
        happen on that sequence. A sequence can be completed in one of two ways, either
        by <em>OnCompleted()</em> or by <em>OnError(Exception)</em>.
    </p>
    <p>
        The four subjects described in this chapter all cater for this implicit contract by
        ignoring any attempts to publish values, errors or completions once the sequence
        has already terminated.
    </p>
    <p>
        Here we see an attempt to publish the value 'c' on a completed sequence. Only values
        'a' and 'b' are written to the console.
    </p>
    <pre class="csharpcode">
        public void SubjectInvalidUsageExample()
        {
            var subject = new Subject&lt;string&gt;();

            subject.Subscribe(Console.WriteLine);

            subject.OnNext("a");
            subject.OnNext("b");
            subject.OnCompleted();
            subject.OnNext("c");
        }
    </pre>
    <a name="ISubject"></a>
    <h2>ISubject interfaces</h2>
    <p>
        While each of the four subjects described in this chapter implement the <em>IObservable&lt;T&gt;</em>
        and <em>IObserver&lt;T&gt;</em> interfaces, they do so via another set of interfaces:
    </p>
    <pre class="csharpcode">
        //Represents an object that is both an observable sequence as well as an observer.
        public interface ISubject&lt;in TSource, out TResult&gt; 
            : IObserver&lt;TSource&gt;, IObservable&lt;TResult&gt;
        {
        }
    </pre>
    <p>
        As all the subjects mentioned here have the same type for both <em>TSource</em>
        and <em>TResult</em>, they implement this interface which is the superset of all
        the previous interfaces:
    </p>
    <pre class="csharpcode">
        //Represents an object that is both an observable sequence as well as an observer.
        public interface ISubject&lt;T&gt; : ISubject&lt;T, T&gt;, IObserver&lt;T&gt;, IObservable&lt;T&gt;
        {
        }
    </pre>
    <p>
        These interfaces are not widely used, but prove useful as the subjects do not share
        a common base class. We will see the subject interfaces used later when we discover
        <a href="14_HotAndColdObservables.html">hot and cold observables</a>.
    </p>
    <a name="SubjectFactory"></a>
    <h2>Subject factory</h2>
    <p>
        Finally it is worth making you aware that you can also create a subject via a factory
        method. Considering that a subject combines the <em>IObservable&lt;T&gt;</em> and
        <em>IObserver&lt;T&gt;</em> interfaces, it seems sensible that there should be a
        factory that allows you to combine them yourself. The <em>Subject.Create(IObserver&lt;TSource&gt;,
            IObservable&lt;TResult&gt;)</em> factory method provides just this.
    </p>
    <pre class="csharpcode">
        //Creates a subject from the specified observer used to publish messages to the subject
        //  and observable used to subscribe to messages sent from the subject
        public static ISubject&lt;TSource, TResult&gt; Create&lt;TSource, TResult&gt;(
            IObserver&lt;TSource&gt; observer, 
            IObservable&lt;TResult&gt; observable)
        {...}
    </pre>
    <p>
        Subjects provide a convenient way to poke around Rx, however they are not recommended
        for day to day use. An explanation is in the <a href="18_UsageGuidelines.html">Usage
            Guidelines</a> in the appendix. Instead of using subjects, favor the factory
        methods we will look at in <a href="04_CreatingObservableSequences.html">Part 2</a>.
    </p>
    <p>
        The fundamental types <em>IObserver&lt;T&gt;</em> and <em>IObservable&lt;T&gt;</em>
        and the auxiliary subject types create a base from which to build your Rx knowledge.
        It is important to understand these simple types and their implicit contracts. In
        production code you may find that you rarely use the <em>IObserver&lt;T&gt;</em>
        interface and subject types, but understanding them and how they fit into the Rx
        eco-system is still important. The <em>IObservable&lt;T&gt;</em> interface is the
        dominant type that you will be exposed to for representing a sequence of data in
        motion, and therefore will comprise the core concern for most of your work with Rx and most
        of this book.
    </p>
    <hr />
    <div class="webonly">
        <h1 class="ignoreToc">Additional recommended reading</h1>
        <div align="center">
            <!--Head First Design Patterns (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
				<iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B00AA36RZY&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
						style="width:120px;height:240px;" 
						scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
			</div>

			<!--Design Patterns (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
				<iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B000SEIBB8&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
						style="width:120px;height:240px;" 
						scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
			</div>

			<!--Clean Code (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
				<iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=0132350882&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
						style="width:120px;height:240px;" 
						scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
			</div>
			
			<!--C# 3.0 Design Patterns (Kindle) Amazon.co.uk-->
            <div style="display:inline-block; vertical-align: top; margin: 10px; width: 140px; font-size: 11px; text-align: center">
				<iframe src="http://rcm-uk.amazon.co.uk/e/cm?t=int0b-21&amp;o=2&amp;p=8&amp;l=as1&amp;asins=B0043EWUAC&amp;ref=qf_sp_asin_til&amp;fc1=000000&amp;IS2=1&amp;lt1=_blank&amp;m=amazon&amp;lc1=0000FF&amp;bc1=000000&amp;bg1=FFFFFF&amp;f=ifr" 
						style="width:120px;height:240px;" 
						scrolling="no" marginwidth="0" marginheight="0" frameborder="0"></iframe>
			</div>
        </div>    </div>
</body>
</html>
