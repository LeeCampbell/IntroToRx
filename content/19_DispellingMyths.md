<?xml version="1.0" encoding="utf-8" ?>
<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Appendix B Dispelling myths</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="DispellingEventMyths"></a>
    <h1>Dispelling event myths</h1>
    <p>
        The previous parts in this book should have given you a solid and broad foundation
        in the fundamentals of Rx. We will use this based to learn the really fun and sometimes
        complex parts of Rx. Just before we do, I want to first make sure we are all on
        the same page and dispel some common myths and misunderstandings. Carrying these
        misconceptions into a world of concurrency will make things seem magic and mysterious.
        This normally leads to problematic code.
    </p>
    <a name="EventMyths"></a>
    <h2>Event myths</h2>
    <p>
        Often in my career, I have found myself involved in the process of interviewing
        new candidates for developer roles. I have often been surprised about the lack of
        understanding developers had surrounding .NET events. Carrying these misconceptions
        into a world of concurrency will make things seem magic and mysterious. This normally
        leads to problematic code. Here is a short list of verifiable facts about events.
    </p>
    <dl>
        <dt>Events are just a syntactic implementation of the observer pattern</dt>
        <dd>
            The += and -= syntax in c# may lead you to think that there is something clever
            going on here, but it is just the observer pattern; you are providing a delegate
            to get called back on. Most events pass you data in the form of the sender and the
            <em>EventArgs</em>.
        </dd>
        <dt>Events are multicast</dt>
        <dd>
            Many consumers can register for the same event. Each delegate (handler) will be
            internally added to a callback list.
        </dd>
        <dt>Events are single threaded.</dt>
        <dd>
            There is nothing multithreaded about an event. Each of the callback handlers are
            each just called in the order that they registered, and they are called sequentially.
        </dd>
        <dt>Event handlers that throw exceptions stop other handlers being called</dt>
        <dd>
            Since handlers are called sequentially, they are also called on the same thread
            that raised the event. This means that, if one handler throws an exception, there
            cannot be a chance for any user code to intercept the exception. This means that
            the remaining handlers will not be called.
        </dd>
    </dl>
    <p>
        Common myths about events that I have heard (or even believed at some point) include:
    </p>
    <ul>
        <li>Handlers are called all at the same time on the thread pool, in parallel</li>
        <li>All handlers will get called. Throwing an exception from a handler will not affect
            other handlers</li>
        <li>You don't need to unregister an event handler, .NET is managed so it will garbage
            collect everything for you.</li>
    </ul>
    <p>
        The silly thing about these myths is that it only takes fifteen minutes to prove
        them all wrong; you just open up Visual Studio or <a href="http://www.linqpad.net/">
            LINQPad</a> and test them out. In my opinion, there is something satisfying
        about proving something you only believed in good faith to be true.
    </p>
    <!--<a name="MemoryManagementMyths"></a>
    <h2>Memory management myths</h2>
    <p>
        Event handles 
        IDispose pattern 
        Finalise is just as good as Dispose 
        GC is free 
        Setting to NULL is the same as Dispose or finalise
    </p>
    <a name="ConcurrencyMyths"></a>
    <h2>Concurrency myths</h2>
    <p>
    </p>-->
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
