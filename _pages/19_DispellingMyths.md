---
layout: page
permalink: /19_DispellingMyths.html
title: 'Intro to Rx - Appendix B Dispelling myths'
...

Dispelling event myths
======================

The previous parts in this book should have given you a solid and broad foundation in the fundamentals of Rx. 
We will use this based to learn the really fun and sometimes complex parts of Rx. 
Just before we do, I want to first make sure we are all on the same page and dispel some common myths and misunderstandings. 
Carrying these misconceptions into a world of concurrency will make things seem magic and mysterious. 
This normally leads to problematic code.

Event myths
-----------

Often in my career, I have found myself involved in the process of interviewing new candidates for developer roles. \
I have often been surprised about the lack of understanding developers had surrounding .NET events. 
Carrying these misconceptions into a world of concurrency will make things seem magic and mysterious. 
This normally leads to problematic code. 
Here is a short list of verifiable facts about events.

Events are just a syntactic implementation of the observer pattern
:   The += and -= syntax in c\# may lead you to think that there is something clever going on here, but it is just the observer pattern; you are providing a delegate to get called back on. 
	Most events pass you data in the form of the sender and the `EventArgs`.
Events are multicast
:   Many consumers can register for the same event. 
	Each delegate (handler) will be internally added to a callback list.
Events are single threaded.
:   There is nothing multithreaded about an event. 
	Each of the callback handlers are each just called in the order that they registered, and they are called sequentially.
Event handlers that throw exceptions stop other handlers being called
:   Since handlers are called sequentially, they are also called on the same thread that raised the event. 
	This means that, if one handler throws an exception, there cannot be a chance for any user code to intercept the exception. 
	This means that the remaining handlers will not be called.

Common myths about events that I have heard (or even believed at some point) include:

-   Handlers are called all at the same time on the thread pool, in parallel
-   All handlers will get called. Throwing an exception from a handler will not affect other handlers
-   You don't need to unregister an event handler, .NET is managed so it  will garbage collect everything for you.

The silly thing about these myths is that it only takes fifteen minutes to prove them all wrong; you just open up Visual Studio or [LINQPad](http://www.linqpad.net/) and test them out. 
In my opinion, there is something satisfying about proving something you only believed in good faith to be true.