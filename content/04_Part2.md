---
title: PART 2
---

# PART 2 - From Events to Insights 

We live in the information age. Data is being created, stored and distributed at a phenomenal rate. Consuming this data can be overwhelming, like trying to drink directly from a fire hose. We need the ability to identify the important data, meaning we need ways to determine what is and is not relevant. We need to take groups of data and process them collectively to discover patterns or other information that might not be apparent from any individual raw input. Users, customers and managers need you do this with more data than ever before, while still delivering higher performance and tighter deadlines.

Rx provides some powerful mechanisms for extracting meaningful insights from raw data streams. This is one of the main reasons for representing information as `IObservable<T>` streams in the first place. The preceding chapter showed how to create an observable sequence, so now we will look at how to exploit the power this has unlocked using the the various Rx methods that can process and transform an observable sequence. 

Rx supports most of the standard LINQ operators. It also defines numerous additional operators. These fall broadly into categories, and each of the following chapters tackles one category:

* [Filtering](./05_Filtering.md)
* [Transformation]()
* [Aggregation]
* [Partitioning]
* [Combination]
* [Error Handling]
* [Timing]

Notes:
* testing/discrimination/scrutinisation/picking
    * where, ignoreelements (segue by pointing out that Where might filter out everything but will still forward end/error)
    * OfType
    * Skip/Take[While/Until]
    * distinct
    * First(OrDefault), Last(OrDefault), Single(OrDefault)
    * ElementAt

* transformation
    * Select
    * SelectMany
    * Cast
    * Materialize/Dematerialize
* aggregation
    * to Boolean
        * Any
        * All
        * Contains
        * SequenceEqual
    * To numeric
        * Count
        * Sum, Average
        * Min, Max
    * Custom
        * Aggregate
        * Scan
* partitioning
    * GroupBy
    * Buffer
    * Window
* combination
    * Concat
    * DefaultIfEmpty
    * Repeat
    * Zip
    * CombineLatest
    * StartWith (prepend)
    * Append?
    * merge
    * join
    * Amb
    * Switch
    * TakeUntil (the flavour that accepts an IObservable<T> as its cut-off)
    * And/Then/When

Does Part 3 start here? The existing book has the rather nondescriptive "Taming the sequence" title, and
I'd prefer to come up with something a bit more meaningful. Maybe "Getting Pragmatic"

* Schedulers

* Leaving IObservable
    * Do
    * For
* Timing
    * TimeStamp TimeInterval
    * TakeUntil (time-based)
    * Delay
    * Sample
    * Throttle
    * Timeout
* Error Handling
    * Catch
    * Finally
    * Using
    * Retry
    * OnErrorResumeNext


TODO: where does Defer fit in? I use it in ch03. I refer to it in Combining Sequences. But it's not clear where its home is.

TODO: where does integration with Task fit in? Chapter 3's From Task section talks about the ToObservable extension method. The chapter currently called "Leaving the Monad" talks about ToTask. What about ToAsync and FromAsync? Chapter 5 mentions in passing that you can `await` any sequence in the ElementAt section, but I don't think I mention that any point earlier than that.

Check these to see if there's anything in them not now covered earlier:
* HotAndColdObservables
* SequencesOfCoincidence