---
layout: page
permalink: /06_Inspection.html
title: 'Intro to Rx - Inspection'
...

Inspection
==========

Making sense of all the data we consume is not always about just filtering out the redundant and superfluous. 
Sometimes we need to pluck out data that is relevant or validate that a sequence even meets our expectations. 
Does this data have any values that meet this specification? 
Is this specific value in the sequence? 
Get me that specific value from the sequence!

In the last chapter we looked at a series of ways to reduce your observable sequence via a variety of filters. 
Next we will look at operators that provide inspection functionality. 
Most of these operators will reduce your observable sequence down to a sequence with a single value in it. 
As the return value of these methods is not a scalar (it is still `IObservable<T>`) these methods do not actually satisfy our definition of catamorphism, but suit our examination of reducing a sequence to a single value.

The series of methods we will look at next are useful for inspecting a given sequence. 
Each of them returns an observable sequence with the single value containing the result. 
This proves useful, as by their nature they are asynchronous. 
They are all quite simple so we will be brief with each of them.

Any
---

First we can look at the parameterless overload for the extension method `Any`. 
This will simply return an observable sequence that has the single value of `false` if the source completes without any values. 
If the source does produce a value however, then when the first value is produced, the result sequence will immediately push `true` and then complete. 
If the first notification it gets is an error, then it will pass that error on.


	var subject = new Subject<int>();
	subject.Subscribe(Console.WriteLine, () => Console.WriteLine("Subject completed"));
	var any = subject.Any();

	any.Subscribe(b => Console.WriteLine("The subject has any values? {0}", b));

	subject.OnNext(1);
	subject.OnCompleted();



Output:

	1

	The subject has any values? True

	subject completed

If we now remove the OnNext(1), the output will change to the following

	subject completed

	The subject has any values? False

If the source errors it would only be interesting if it was the first notification, otherwise the `Any` method will have already pushed true.
If the first notification is an error then `Any` will just pass it along as an *OnError* notification.


	var subject = new Subject<int>();
	subject.Subscribe(Console.WriteLine,
		ex => Console.WriteLine("subject OnError : {0}", ex),
		() => Console.WriteLine("Subject completed"));
	var any = subject.Any();

	any.Subscribe(b => Console.WriteLine("The subject has any values? {0}", b),
		ex => Console.WriteLine(".Any() OnError : {0}", ex),
		() => Console.WriteLine(".Any() completed"));

	subject.OnError(new Exception());



Output:

	subject OnError : System.Exception: Fail

	.Any() OnError : System.Exception: Fail

The `Any` method also has an overload that takes a predicate. 
This effectively makes it a *Where* with an `Any` appended to it.


	subject.Any(i => i > 2);
	//Functionally equivalent to 
	subject.Where(i => i > 2).Any();



As an exercise, write your own version of the two `Any` extension method overloads. 
While the answer may not be immediately obvious, we have covered enough material for you to create this using the methods you know...

* * * * *

Example of the `Any` extension methods written with Observable.Create:


	public static IObservable<bool> MyAny<T>(
		this IObservable<T> source)
	{
		return Observable.Create<bool>(
			o =>
			{
				var hasValues = false;
				return source
					.Take(1)
					.Subscribe(
						_ => hasValues = true,
						o.OnError,
						() =>
						{
							o.OnNext(hasValues);
							o.OnCompleted();
						});
			});
	}

	public static IObservable<bool> MyAny<T>(
		this IObservable<T> source, 
		Func<T, bool> predicate)
	{
		return source
			.Where(predicate)
			.MyAny();
	}



All
---

The `All()` extension method works just like the `Any` method, except that all values must meet the predicate.
As soon as a value does not meet the predicate a `false` value is returned then the output sequence completed. 
If the source is empty, then `All` will push `true` as its value. 
As per the `Any` method, and errors will be passed along to the subscriber of the `All` method.


	var subject = new Subject<int>();
	subject.Subscribe(Console.WriteLine, () => Console.WriteLine("Subject completed"));
	var all = subject.All(i => i < 5);
	all.Subscribe(b => Console.WriteLine("All values less than 5? {0}", b));

	subject.OnNext(1);
	subject.OnNext(2);
	subject.OnNext(6);
	subject.OnNext(2);
	subject.OnNext(1);
	subject.OnCompleted();
    


Output:

	1

	2

	6

	All values less than 5? False

	all completed

	2

	1

	subject completed

Early adopters of Rx may notice that the *IsEmpty* extension method is missing. 
You can easily replicate the missing method using the `All` extension method.

	//IsEmpty() is deprecated now.
	//var isEmpty = subject.IsEmpty();
	var isEmpty = subject.All(_ => false);
   


Contains
--------

The `Contains` extension method overloads could sensibly be overloads to the `Any` extension method. 
The `Contains` extension method has the same behavior as `Any`, however it specifically targets the use of `IComparable` instead of the usage of predicates and is designed to seek a specific value instead of a value that fits the predicate. 
I believe that these are not overloads of `Any` for consistency with `IEnumerable<T>`.

	var subject = new Subject<int>();
	subject.Subscribe(
		Console.WriteLine, 
		() => Console.WriteLine("Subject completed"));
	var contains = subject.Contains(2);
	contains.Subscribe(
		b => Console.WriteLine("Contains the value 2? {0}", b),
		() => Console.WriteLine("contains completed"));

	subject.OnNext(1);
	subject.OnNext(2);
	subject.OnNext(3);
		
	subject.OnCompleted();
    


Output:

	1

	2

	Contains the value 2? True

	contains completed

	3

	Subject completed

There is also an overload to `Contains` that allows you to specify an implementation of `IEqualityComparer<T>` other than the default for the type. 
This can be useful if you have a sequence of custom types that may have some special rules for equality depending on the use case.

DefaultIfEmpty
--------------

The `DefaultIfEmpty` extension method will return a single value if the source sequence is empty. 
Depending on the overload used, it will either be the value provided as the default, or `Default(T)`. 
`Default(T)` will be the zero value for struct types and will be `null` for classes. 
If the source is not empty then all values will be passed straight on through.

In this example the source produces values, so the result of `DefaultIfEmpty` is just the source.


	var subject = new Subject<int>();
	subject.Subscribe(
		Console.WriteLine,
		() => Console.WriteLine("Subject completed"));
	var defaultIfEmpty = subject.DefaultIfEmpty();
	defaultIfEmpty.Subscribe(
		b => Console.WriteLine("defaultIfEmpty value: {0}", b),
		() => Console.WriteLine("defaultIfEmpty completed"));

	subject.OnNext(1);
	subject.OnNext(2);
	subject.OnNext(3);

	subject.OnCompleted();



Output:

	1

	defaultIfEmpty value: 1

	2

	defaultIfEmpty value: 2

	3

	defaultIfEmpty value: 3

	Subject completed

	defaultIfEmpty completed

If the source is empty, we can use either the default value for the type (i.e. 0 for int) or provide our own value in this case 42.


	var subject = new Subject<int>();
	subject.Subscribe(
		Console.WriteLine,
		() => Console.WriteLine("Subject completed"));
	var defaultIfEmpty = subject.DefaultIfEmpty();
	defaultIfEmpty.Subscribe(
		b => Console.WriteLine("defaultIfEmpty value: {0}", b),
		() => Console.WriteLine("defaultIfEmpty completed"));

	var default42IfEmpty = subject.DefaultIfEmpty(42);
	default42IfEmpty.Subscribe(
		b => Console.WriteLine("default42IfEmpty value: {0}", b),
		() => Console.WriteLine("default42IfEmpty completed"));

	subject.OnCompleted();
    


Output:

	Subject completed

	defaultIfEmpty value: 0

	defaultIfEmpty completed

	default42IfEmpty value: 42

	default42IfEmpty completed

ElementAt
---------

The `ElementAt` extension method allows us to "cherry pick" out a value at a given index. 
Like the `IEnumerable<T>` version it is uses a 0 based index.


	var subject = new Subject<int>();
	subject.Subscribe(
		Console.WriteLine,
		() => Console.WriteLine("Subject completed"));
	var elementAt1 = subject.ElementAt(1);
	elementAt1.Subscribe(
		b => Console.WriteLine("elementAt1 value: {0}", b),
		() => Console.WriteLine("elementAt1 completed"));

	subject.OnNext(1);
	subject.OnNext(2);
	subject.OnNext(3);

	subject.OnCompleted();



Output

	1

	2

	elementAt1 value: 2

	elementAt1 completed

	3

	subject completed

As we can't check the length of an observable sequence it is fair to assume that sometimes this method could cause problems. 
If your source sequence only produces five values and we ask for `ElementAt(5)`, the result sequence will error with an `ArgumentOutOfRangeException` inside when the source completes. 
There are three options we have:

-   Handle the `OnError` gracefully
-   Use `.Skip(5).Take(1)`; This will ignore the first 5 values and the only take the 6th value. 
    If the sequence has less than 6 elements we just get an empty sequence, but no errors.
-   Use ElementAtOrDefault

`ElementAtOrDefault` extension method will protect us in case the index is out of range, by pushing the `Default(T)` value. 
Currently there is not an option to provide your own default value.

SequenceEqual
-------------

Finally `SequenceEqual` extension method is perhaps a stretch to put in a chapter that starts off talking about catamorphism and fold, but it does serve well for the theme of inspection. 
This method allows us to compare two observable sequences. 
As each source sequence produces values, they are compared to a cache of the other sequence to ensure that each sequence has the same values in the same order and that the sequences are the same length. 
This means that the result sequence can return `false` as soon as the source sequences produce diverging values, or `true` when both sources complete with the same values.


	var subject1 = new Subject<int>();
	subject1.Subscribe(
		i=>Console.WriteLine("subject1.OnNext({0})", i),
		() => Console.WriteLine("subject1 completed"));
	var subject2 = new Subject<int>();
	subject2.Subscribe(
		i=>Console.WriteLine("subject2.OnNext({0})", i),
		() => Console.WriteLine("subject2 completed"));


	var areEqual = subject1.SequenceEqual(subject2);
	areEqual.Subscribe(
		i => Console.WriteLine("areEqual.OnNext({0})", i),
		() => Console.WriteLine("areEqual completed"));

	subject1.OnNext(1);
	subject1.OnNext(2);

	subject2.OnNext(1);
	subject2.OnNext(2);
	subject2.OnNext(3);

	subject1.OnNext(3);

	subject1.OnCompleted();
	subject2.OnCompleted();



Output:

	subject1.OnNext(1)

	subject1.OnNext(2)

	subject2.OnNext(1)

	subject2.OnNext(2)

	subject2.OnNext(3)

	subject1.OnNext(3)

	subject1 completed

	subject2 completed

	areEqual.OnNext(True)

	areEqual completed

This chapter covered a set of methods that allow us to inspect observable sequences. 
The result of each, generally, returns a sequence with a single value. 
We will continue to look at methods to reduce our sequence until we discover the elusive functional fold feature.
