# What's Wrong with Classic IO Streams

The abstraction that `System.IO.Stream` represents was designed as a way for an operating system to enable application code to communicate with devices that could receive and/or produce streams of bytes. This makes them a good model for the [reel to reel tape storage devices](https://en.wikipedia.org/wiki/IBM_7-track) that were commonplace back when this kind of stream was designed, but unnecessarily cumbersome if you just want to represent a sequence of values. Over the years, streams have been co-opted to represent an increasingly diverse range of things, including files, keyboards, network connections, and OS status information, meaning that by the time .NET came along in 2002, its `Stream` type needed a mixture of features to accommodate some quite diverse scenarios. And since not all streams are alike, it's quite common for some of these features to not to work on some streams.

IO streams were designed to support efficient delivery of fairly high volumes of byte data, often with devices that inherently work with data in big chunks. In the main scenarios for which they were designed, read and write operations would involve calls into operating system APIs, which are typically relatively expensive, so the basic read and write operations expect to work with arrays of bytes. (If you make one system call to deliver thousands of bytes, the overhead of that single call is far lower than if you were working one byte at a time.) While that's good for efficiency, it can be inconvenient for developers (and irksome if you were hoping to use streams purely to represent in-process event streams that don't actually need to make system calls, and therefore don't get to enjoy the upside of this performance/convenience trade off).

There is a standard band-aid kind of a fix for this: libraries that present streams to application code often don't represent the underlying OS stream directly. Instead, they are often _buffered_, meaning that the library will perform reads fairly large chunks, and hold recently-fetched bytes in memory until the application code asks for them. This can enable methods like .NET's single-byte [`Stream.ReadByte`](https://learn.microsoft.com/en-us/dotnet/api/system.io.stream.readbyte) method to work reasonably efficiently: several thousand calls to that might correspond to only one call to the operating system API that provides access to whatever physical device the stream represents. Likewise, if you're sending data into an IO stream, a buffered stream will wait until you've supplied some minimum quantity of data (4096 bytes is a common default with certain .NET `Stream`s) before it actually sends any data to its destination.

But this could be a serious problem for the kinds of event sources we represent in Rx. If an IO stream deliberately insulates you from the real movement of data, that could introduce delays that might be disastrous in a financial application where delays in delivery and receipt of information can have enormous financial consequences. And even if there aren't direct financial implications, this kind of buffering would be unhelpful in representing events in a user interface—nobody wants to have to click a button several thousand times before the application starts to act on that input.

There's also the problem that you don't always know which kind of stream you've been given. If you know for a fact that you've got an unbuffered stream representing a file on disk (because you created that stream yourself) you'd typically right quite different code than you would if you knew you had a buffered stream. But if you've written a method that takes a `Stream` argument, it's not clear what you've got, so you don't necessarily know which coding strategy is best.

Another problem is that because they are byte-oriented, there's no such thing as a `System.IO.Stream` that produces more complex values. If you want a stream of `int` values (which isn't a _much_ more complex idea than a stream of _byte_ values) `System.IO.Stream` does nothing to help you, and might even hinder you. You can try reading four bytes at a time but a `System.IO.Stream` is at liberty to decide that it's only going to return three. (The reason streams are allowed to be petty in this way is that the original design presumes that a stream represents some underlying device that might inherently work with fixed size units of data. Disk drives and SSDs are incapable of reading or writing individual bytes; instead, each operation works with some whole number of 'sectors' each of which are hundreds or thousands of bytes long. So a read operation might simply be unable to give you exactly as many bytes as you asked for.) It's now the consuming code's problem to work out how to deal with that.

You can write wrappers to deal with these issues caused by `Stream`'s origins as an abstraction for a magnetic tape storage device, but if you want the type system to help you to distinguish between a stream of `int` values and a stream of `float` values, `Stream` won't help you. You'll end up needing some different abstraction that has a type parameter. Something like `IObservable<T>` in fact.





Going back to the 'stream of `int` values' idea, 

 Some `Stream` instances don't have this 'buffering' layer, and operate more immediately (at the price of being unable to handle one-value-at-a-time operations as efficient, or maybe even at all) but the problem is you don't necessarily know which kind you've got. If you've written a method with an argument of type `Stream`, callers could be passing you anything.


And although they do get used for one-byte-at-a-time devices (notably the keyboard input in a command line tool that presents an interactive prompt) they're not especially well suited to that.








if you want to implement your own type that derives from `Stream` to represents some source of events, you'll need to implement all ten of the abstract members it defines: 5 properties and 5 methods


The essence of a `System.IO.Stream` is

The `System.IO.Stream` implementations are commonly used to stream data (generally bytes) to or from an I/O device like a file, network or block of memory. 
> `System.IO.Stream` implementations can have both the ability to read and write, and sometimes the ability to seek (i.e. fast forward through a stream or move backwards). 
> When I refer to an instance of `IObservable<T>` as a stream, it does not exhibit the seek or write functionality that streams do. 
This is a fundamental difference preventing Rx being built on top of the `System.IO.Stream` paradigm. 
Rx does however have the concept of forward streaming (push), disposing (closing) and completing (eof). 
Rx also extends the metaphor by introducing concurrency constructs, and query operations like transformation, merging, aggregating and expanding. 
> These features are also not an appropriate fit for the existing `System.IO.Stream` types. Some others refer to instances of `IObservable<T>` as Observable Collections, which I find hard to understand. While the observable part makes sense to me, I do not find them like collections at all. You generally cannot sort, insert or remove	items from an `IObservable<T>` instance like I would expect you can	with a collection. Collections generally have some sort of backing store like an internal array. The values from an `IObservable<T>` source are not usually pre-materialized as you would expect from a normal collection. There is also a type in WPF/Silverlight called an `ObservableCollection<T>`	that does exhibit collection-like behavior, and is very well suited to this description.
> In fact `IObservable<T>` integrates very well with `ObservableCollection<T>` instances. 
So to save on any confusion we will refer to instances of `IObservable<T>` as *sequences*. 
While instances of `IEnumerable<T>` are also sequences,	we will adopt the convention that they are sequences of _data at rest_, and	`IObservable<T>` instances are sequences of _data in motion_.
