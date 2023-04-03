---
title : Appendix A Usage guidelines
---

# Appendix		
 
# Usage guidelines

This is a list of quick guidelines intended to help you when writing Rx queries.

- Members that return a sequence should never return null. This applies to `IEnumerable<T>` and `IObservable<T>` sequences. Return an empty sequence instead.
- Dispose of subscriptions.
- Subscriptions should match their scope.
- Always provide an `OnError` handler.
- Avoid breaking the monad with blocking operators such as `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Single`, `SingleOrDefault` and `ForEach`.
- Avoid switching between monads, i.e. going from `IObservable<T>` to `IEnumerable<T>` and back to `IObservable<T>`.
- Favor lazy evaluation over eager evaluation.
- Break large queries up into parts. Key indicators of a large query:		
	1. nesting
	2. over 10 lines of query comprehension syntax
	3. using the into statement
- Name your queries well, i.e. avoid using the names like `query`, `q`, `xs`, `ys`, `subject` etc.
- Avoid creating side effects. If you must create side effects, be explicit by using the `Do` operator.
- Avoid the use of the subject types. Rx is effectively a functional programming paradigm. Using subjects means we are now managing state, which is potentially mutating. Dealing with both mutating state and asynchronous programming at the same time is very hard to get right.Furthermore, many of the operators (extension methods) have been carefully written to ensure that correct and consistent lifetime of subscriptions and sequences is maintained;when you introduce subjects, you can break this. Future releases may also see significant performance degradation if you explicitly use subjects.
- Avoid creating your own implementations of the `IObservable<T>` interface. Favor using the `Observable.Create` factory method overloads instead.
- Avoid creating your own implementations of the `IObserver<T>` interface. Favor using the `Subscribe` extension method overloads instead.
- The subscriber should define the concurrency model. The `SubscribeOn` and `ObserveOn` operators should only ever precede a `Subscribe` method.