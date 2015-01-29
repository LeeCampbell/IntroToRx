<html>
<head>
    <meta http-equiv="Content-Type" content="text/html;charset=iso-8859-1" />
    <title>Intro to Rx - Foreword</title>
    <link rel="stylesheet" href="Kindle.css" type="text/css" />
</head>
<body>
    <a name="IntroductiontoRx"></a>
    <h1 class="ignoreToc kindleOnly">Introduction to Rx</h1>
    <h2 class="ignoreToc kindleOnly" style="text-align: center">Lee Campbell</h2>
    <hr class="kindleOnly" />
    <a name="Preface"></a>
    <h1 class="SectionHeader">Preface</h1>
    <p>
        Reactive programming is not a new concept. I remember studying my first Event Driven
        module for Visual Basic 5 in 2000. Even then the technology (Visual Basic 5) was
        already considered somewhat dated. Long before VB5 and the turn of the millennium,
        we have seen languages supporting events. Over time languages like Smalltalk, Delphi
        and the .NET languages have popularized reactive or event-driven programming paradigms.
        This not to say that events are pass&eacute;: current trends such as CEP (Complex
        Event Processing), CQRS (Command Query Responsibility Segregation) and rich immersive
        GUIs, all have events as a fundamental part of their makeup.
    </p>
    <p>
        The event driven paradigm allows for code to be invoked without the need for breaking
        encapsulation or applying expensive polling techniques. This is commonly implemented
        with the Observer pattern, events exposed directly in the language (e.g. C#) or
        other forms of callback via delegate registration. The Reactive Extensions extend
        the callback metaphor with LINQ to enable querying sequences of events and managing
        concurrency.
    </p>
    <p>
        The Reactive Extensions are effectively a library of implementations of the <em>IObservable&lt;T&gt;</em>
        and <em>IObserver&lt;T&gt;</em> interfaces for .NET, Silverlight and Windows Phone
        7. The libraries are also available in JavaScript. As a dynamic language, JavaScript
        had no need for the two interfaces so the JavaScript implementation could have been
        written long before .NET 4 was released. This book will introduce Rx via C#. Users
        of VB.NET, F# and other .NET languages hopefully will be able to extract the concepts
        and translate them to their particular language. JavaScript users should be able
        to gather the concepts from this book and apply them to their language. JavaScript
        users may however find some features are not supported, and some concepts, such
        as scheduling do not transcend platforms.
    </p>
    <p>
        As Rx is just a library, the team at Microsoft delivering Rx was able to isolate
        themselves from the release schedule of the .NET Framework. This proved important
        as the libraries saw fairly constant evolution since late 2009 through to their
        official release in mid 2011. This evolution has been largely enabled by the openness
        of the team and their ability to take onboard criticisms, suggestions and feature
        requests from the brave community of pre-release users.
    </p>
    <p>
        While Rx is <i>just a library</i>, it is a significant and bold move forward for
        the team at Microsoft and for any consumers of the library. Rx <i>will</i> change
        the way you design and build software for the following reasons:
    </p>
    <ul>
        <li>The way that it tackles the Observer pattern is a divorce from .NET events toward
            a Java-style interface pattern but far more refined.</li>
        <li>The way it tackles concurrency is quite a shift how many .NET developers would have
            done it before. </li>
        <li>The abundance of (extension) methods in the library. </li>
        <li>The way in which it integrates with LINQ to leverage LINQ's composability &amp;
            declarative style, makes Rx very usable and discoverable to those already familiar
            with LINQ and <em>IEnumerable&lt;T&gt;</em>. </li>
        <li>The way it can help any .NET developer that works with event driven and/or asynchronous
            programs. Developers of Rich Clients, Web Clients and Services alike can all benefit
            from Rx. </li>
        <li>The future plans seem even grander, but that is a different book for some time in
            the future :-) </li>
    </ul>
    <p>
        This book aims to teach you:
    </p>
    <ul>
        <li>about the new types that Rx will provide </li>
        <li>about the extension methods and how to use them </li>
        <li>how to manage subscriptions to "sequences" of data </li>
        <li>how to visualize "sequences" of data and sketch your solution before coding it</li>
        <li>how to deal with concurrency to your advantage and avoid common pitfalls </li>
        <li>how to compose, aggregate and transform streams </li>
        <li>how to test your Rx code </li>
        <li>some guidance on best practices when using Rx.</li>
    </ul>
    <p>
        The best way to learn Rx is to use it. Reading the theory from this book will only
        help you be familiar with Rx, but will not really enable you to fully understand
        Rx. You can download the latest version of Rx from the Microsoft Data Developer
        site (<a href="http://msdn.microsoft.com/en-us/data/gg577609">http://msdn.microsoft.com/en-us/data/gg577609</a>)
        or if you use NuGet you can just download Rx via that.
    </p>
    <p>
        My experience with Rx is straight from the trenches. I worked on a team of exceptional
        developers on a project that was an early adopter of Rx (late 2009). The project
        was a financial services application that started off life as a Silverlight project
        then expanded into an integration project. We used Rx everywhere; client side in
        Silverlight 3/4, and server side in .NET 3.5/4.0. We used Rx eagerly and sometimes
        too eagerly. We were past leading edge, we were <i>bleeding</i> edge. We were finding
        bugs in the early releases and posting proposed fixes to the guys at Microsoft.
        We were constantly updating to the latest version. It cost the project to be early
        adopters, but in time the payment was worth it. Rx allowed us to massively simplify
        an application that was inherently asynchronous, highly concurrent and targeted
        low latencies. Similar workflows that I had written in previous projects were pages
        of code long; now with Rx were several lines of LINQ. Trying to test asynchronous
        code on clients (WPF/Win Forms/Silverlight) was a constant challenge, but Rx solved
        that too. Today if you ask a question on the Rx Forums, you will most likely be
        answered by someone from that team (or Dave Sexton).
    </p>
    <a name="Acknowledgements"></a>
    <h1>Acknowledgements</h1>
    <p>
        I would like to take this quick pause to recognize the people that made this book
        possible. First is my poor wife for losing a husband to a dark room for several
        months. Her understanding and tolerance is much appreciated. To my old team "Alpha
        Alumni"; every developer on that team has helped me in some way to better myself
        as a developer. Specific mention goes to <a href="http://enumeratethis.com/">James Miles</a>,
        Matt Barrett, <a href="http://johnhmarks.wordpress.com/">John Marks</a>, Duncan
        Mole, Cathal Golden, <a href="http://keith-woods.com">Keith Woods</a>, <a href="http://nondestructiveme.com/">
            Ray Booysen</a> &amp; <a href="http://odeheurles.com/">Olivier DeHeurles</a>
        for all the deep dive sessions, emails, forum banter, BBM exchanges, lunch breaks
        and pub sessions spent trying to get our heads around Rx. To <a href="http://mdavey.wordpress.com">
            Matt Davey</a> for being brave enough to support us in using Rx back in 2009.
        To the team at Microsoft that did the hard work and brought us Rx; <a href="http://blogs.msdn.com/b/jeffva/">
            Jeffrey Van Gogh</a>, <a href="http://blogs.msdn.com/b/wesdyer/">Wes Dyer</a>,
        <a href="http://en.wikipedia.org/wiki/Erik_Meijer_(computer_scientist)">Erik Meijer</a>
        &amp; <a href="http://blogs.bartdesmet.net/bart/">Bart De Smet</a>. Extra special
        mention to Bart, there is just something about the <a href="http://channel9.msdn.com/Tags/bart+de+smet">
            content</a> that Bart <a href="http://www.infoq.com/author/Bart-De-Smet">produces</a>
        that clicks with me. Finally to the guys that helped edit the book; <a href="http://www.albahari.com/">
            Joe Albahari</a> and Gregory Andrien. Joe is a veteran author of books such
        as the C# in a nutshell, C# pocket reference and LINQ pocket reference, and managed
        to find time to help out on this project while also releasing the latest versions
        of these books. For Gregory and I, this was a first for both of us, as editor and
        author respectively. Gregory committed many late nights to helping complete this
        project. There is also some sweet irony in having a French person as the editor.
        Even though English is not his native tongue, he clearly has a better grasp of it
        than I.
    </p>
    <p>
        It is my intention that from the experiences both good and bad, I can help speed
        up your understanding of Rx and lower that barrier to entry to using Rx. This will
        be a progressive step-by-step approach. It may seem slow in places, but the fundamentals
        are so important to have a firm grasp on the powerful features. I hope you will
        have the patience to join me all the way to the end.
    </p>
    <p>
        The content of this book was originally posted as a series of blog posts at <a href="http://leecampbell.blogspot.co.uk/2010/08/reactive-extensions-for-net.html">
            http://LeeCampbell.blogspot.com</a> and has proved popular enough that I thought
        it warranted being reproduced as an e-book. In the spirit of other books such as
        Joe Albahari's <a href="http://www.albahari.com/threading/">Threading in C#</a>
        and Scott Chacon's <a href="http://git-scm.com/book">Pro Git</a> books, and considering
        the blog was free, I have made the first version of this book free.
    </p>
    <p>
        The version that this book has been written against is the .Net 4.0 targeted Rx
        assemblies version 1.0.10621.0 (NuGet: Rx-Main v1.0.11226).
    </p>
    <p class="kindleOnly">
        The <a href="toc.html">Table of Contents</a> for the <i>Introduction to Rx</i> shows
        you all of the topics covered in this book. Kindle users can get to the table of
        contents by pressing the Menu button from any page. Move the 5-way down until you
        underline "Table of Contents" and press the 5-way to go to it.
    </p>
    <p>
        So, fire up Visual Studio and let's get started.
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
