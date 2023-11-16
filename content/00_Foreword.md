---
title : Foreword
---

# Introduction to Rx
By Ian Griffiths and Lee Campbell
   
---

Reactive programming is not a new concept. Any kind of user interface development
necessary involves code that responds to events. Languages like Smalltalk, Delphi
and the .NET languages have popularized reactive or event-driven programming paradigms.
Architectural patterns such as CEP (Complex Event Processing), and
CQRS (Command Query Responsibility Segregation) have events as a fundamental part
of their makeup. Reactive programming is a useful concept in any program that has
to deal with things happening.

> Reactive programming is a useful concept in any program that has
to deal with things happening.

The event driven paradigm allows for code to be invoked without the need for breaking
encapsulation or applying expensive polling techniques. There are many common ways to implement this, including
the Observer pattern, events exposed directly in the language (e.g. C#) or
other forms of callback via delegate registration. The Reactive Extensions extend
the callback metaphor with LINQ to enable querying sequences of events and managing
concurrency.

The .NET runtime libraries have included the `IObservable<T>` and
`IObserver<T>` interfaces that representing the core concept of reactive programming
for well over a decade now. The Reactive Extensions for .NET are effectively a library of implementations of these
interfaces. Rx.NET implementation first appeared
back in 2010 but since then, Rx libraries have become available for other languages, and this way of programming has become
especially popular in JavaScript.

This book will introduce Rx via C#. The concepts are universal, so users of other .NET languages
such as VB.NET and F#, will be able to extract the concepts and translate them to their particular
language.

Rx.NET is just a library, originally created by Microsoft, but now an open source project
supported entirely through community effort. (Rx's current lead maintainer, [Ian Griffiths](https://endjin.com/who-we-are/our-people/ian-griffiths/),
is also the author of the latest revision of this book, and indeed the author of this very
sentence.)

If you have never used Rx before, it _will_ change the way you design and build software.
It provides a well thought out abstraction for a fundamentally important idea in computing—sequences
of events. These are as important as lists or arrays, but before Rx there was little
direct support in libraries or languages, and what support there was tended to be rather
ad hoc, and built on weak theoretical underpinnings. Rx changes that. The extent to
which this Microsoft invention has been wholehearted adopted by some developer communities
traditionally not especially Microsoft-friendly is a testament to the quality of its
fundamental design.

This book aims to teach you:

  * about the types that Rx defines
  * about the extension methods Rx provides, and how to use them
  * how to manage subscriptions to event sources
  * how to visualize "sequences" of data and sketch your solution before coding it
  * how to deal with concurrency to your advantage and avoid common pitfalls
  * how to compose, aggregate and transform streams
  * how to test your Rx code
  * some common best practices when using Rx
    
The best way to learn Rx is to use it. Reading the theory from this book will only
help you be familiar with Rx, but to fully understand it you should build things
with it. So we warmly encourage you to build based on the examples in this book.


#Acknowledgements    {#Acknowledgements}

Firstly, I (Ian Griffiths) should make it clear that this revised edition builds
on the excellent work of the original author Lee Campbell. I am grateful that he
generously allowed the Rx.NET project to make use of his content, enabling this
new edition to come into existence.

I would also like to recognize the people that made this book
possible. Crucial to the first edition of the book, in addition to the author, [Lee Campbell](https://leecampbell.com/), were: 
[James Miles](http://enumeratethis.com/), 
[Matt Barrett](http://weareadaptive.com/blog/), 
[John Marks](http://johnhmarks.wordpress.com/), 
Duncan Mole, 
Cathal Golden, 
[Keith Woods](http://keith-woods.com), 
[Ray Booysen](http://nondestructiveme.com) &amp; [Olivier DeHeurles](http://odeheurles.com/),
[Matt Davey](http://mdavey.wordpress.com), [Joe Albahari](http://www.albahari.com/) 
and Gregory Andrien.
Extra special thanks to the team at Microsoft that did the hard work and brought us Rx; 
[Jeffrey Van Gogh](http://blogs.msdn.com/b/jeffva/), 
[Wes Dyer](http://blogs.msdn.com/b/wesdyer/), 
[Erik Meijer](http://www.applied-duality.com/) &amp; 
[Bart De Smet](http://blogs.bartdesmet.net/bart/). 
For this, the second edition of the book, thanks again to Lee Campbell for allowing us to update his content.
Thanks to everyone at [endjin](endjin.com) and especially [Howard van Rooijen](https://endjin.com/who-we-are/our-people/howard-van-rooijen/) and [Matthew Adams](https://endjin.com/who-we-are/our-people/matthew-adams/)
for funding not only the updates to this book, but also the ongoing development of Rx.NET itself.
(And thanks for employing me too!)

The content of the first edition of this book was originally posted as a series of blog posts at 
[http://LeeCampbell.blogspot.com](http://leecampbell.blogspot.co.uk/2010/08/reactive-extensions-for-net.html)
and proved popular enough that Lee reproduced as an e-book. 

The version that this book has been written against is `System.Reactive` version 6.0.

So, fire up Visual Studio and let's get started.

---
