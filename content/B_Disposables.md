
TODO: these were in the now-defunct lifetime management chapter


The Rx library itself adopts this liberal usage of the `IDisposable` interface and introduces several of its own custom implementations:

- Disposable
- BooleanDisposable
- CancellationDisposable
- CompositeDisposable
- ContextDisposable
- MultipleAssignmentDisposable
- RefCountDisposable
- ScheduledDisposable
- SerialDisposable
- SingleAssignmentDisposable

For a full rundown of each of the implementations see the [Disposables](20_Disposables.html) reference in the Appendix. For now we will look at the extremely simple and useful `Disposable` static class:

```csharp
namespace System.Reactive.Disposables
{
    public static class Disposable
    {
    // Gets the disposable that does nothing when disposed.
    public static IDisposable Empty { get {...} }

    // Creates the disposable that invokes the specified action when disposed.
    public static IDisposable Create(Action dispose)
    {...}
    }
}
```

As you can see it exposes two members: `Empty` and `Create`. The `Empty` method allows you get a stub instance of an `IDisposable` that does nothing when `Dispose()` is called. This is useful for when you need to fulfil an interface requirement that returns an `IDisposable` but you have no specific implementation that is relevant.

The other overload is the `Create` factory method which allows you to pass an `Action` to be invoked when the instance is disposed. The `Create` method will ensure the standard Dispose semantics, so calling `Dispose()` multiple times will only invoke the delegate you provide once:

```csharp
var disposable = Disposable.Create(() => Console.WriteLine("Being disposed."));
Console.WriteLine("Calling dispose...");
disposable.Dispose();
Console.WriteLine("Calling again...");
disposable.Dispose();
```

Output:

```
Calling dispose...
Being disposed.
Calling again...
```

Note that "Being disposed." is only printed once. In a later chapter we cover another	useful method for binding the lifetime of a resource to that of a subscription in the [Observable.Using](11_AdvancedErrorHandling.html#Using) method.
