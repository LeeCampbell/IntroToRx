---
title : Appendix B Disposables
---

# Appendix B: Disposables
    
Rx leverages the existing `IDisposable` interface for subscription management. This is an incredibly useful design decision, as users can work with a familiar type and reuse existing language features. Rx further extends its usage of the `IDisposable` type by providing several public implementations of the interface. These can be found in the `System.Reactive.Disposables` namespace. Here, we will briefly enumerate each of them.


<dl>
    <dt>Disposable.Empty</dt>
    <dd>
        This static property exposes an implementation of <em>IDisposable</em> that performs no action when the <em>Dispose</em> method is invoked. This can be useful whenever you need to fulfil an interface requirement, like <em>Observable.Create</em>, but do not have any resource management that needs to take place.</dd>
    <dt>Disposable.Create(Action)</dt>
    <dd>
        This static method exposes an implementation of <em>IDisposable</em> that performs the action provided when the <em>Dispose</em> method is invoked. As the implementation follows the guidance to be idempotent, the action will only be called on the first time the <em>Dispose</em> method is invoked.</dd>
    <dt>BooleanDisposable</dt>
    <dd>
        This class simply has the <em>Dispose</em> method and a read-only property <em>IsDisposed</em>. <em>IsDisposed</em> is <code>false</code> when the class is constructed, and is set to <code>true</code> when the <em>Dispose</em> method is invoked.
    </dd>
    <dt>CancellationDisposable</dt>
    <dd>
        The <em>CancellationDisposable</em> class offers an integration point between the .NET <a href="http://msdn.microsoft.com/en-us/library/dd997364.aspx">cancellation paradigm</a> (<em>CancellationTokenSource</em>) and the resource management paradigm (<em>IDisposable</em>). You can create an instance of the <em>CancellationDisposable</em> class by providing a <em>CancellationTokenSource</em> to the constructor, or by having the parameterless constructor create one for you. Calling <em>Dispose</em> will invoke the <em>Cancel</em> method on the <em>CancellationTokenSource</em>. There are two properties (<em>Token</em> and <em>IsDisposed</em>) that <em>CancellationDisposable</em> exposes; they are wrappers for the <em>CancellationTokenSource</em> properties, respectively <em>Token</em> and <em>IsCancellationRequested</em>.
    </dd>
    <dt>CompositeDisposable</dt>
    <dd>
        The <em>CompositeDisposable</em> type allows you to treat many disposable resources as one. Common usage is to create an instance of <em>CompositeDisposable</em> by passing in a <code>params</code> array of disposable resources. Calling <em>Dispose</em> on the <em>CompositeDisposable</em> will call dispose on each of these resources in the order they were provided. Additionally, the <em>CompositeDisposable</em> class implements <em>ICollection&lt;IDisposable&gt;</em>; this allows you to add and remove resources from the collection. After the <em>CompositeDisposable</em> has been disposed of, any further resources that are added to this collection will be disposed of instantly. Any item that is removed from the collection is also disposed of, regardless of whether the collection itself has been disposed of. This includes usage of both the <em>Remove</em> and <em>Clear</em> methods.
    </dd>
    <dt>ContextDisposable</dt>
    <dd>
        <em>ContextDisposable</em> allows you to enforce that disposal of a resource is performed on a given <em>SynchronizationContext</em>. The constructor requires both a <em>SynchronizationContext</em> and an <em>IDisposable</em> resource. When the <em>Dispose</em> method is invoked on the <em>ContextDisposable</em>, the provided resource will be disposed of on the specified context.
    </dd>
    <dt>MultipleAssignmentDisposable</dt>
    <dd>
        The <em>MultipleAssignmentDisposable</em> exposes a read-only <em>IsDisposed</em> property and a read/write property <em>Disposable</em>. Invoking the <em>Dispose</em> method on the <em>MultipleAssignmentDisposable</em> will dispose of the current value held by the <em>Disposable</em> property. It will then set that value to null. As long as the <em>MultipleAssignmentDisposable</em> has not been disposed of, you are able to set the <em>Disposable</em> property to <em>IDisposable</em> values as you would expect. Once the <em>MultipleAssignmentDisposable</em> has been disposed, attempting to set the <em>Disposable</em> property will cause the value to be instantly disposed of; meanwhile, <em>Disposable</em> will remain null.
    </dd>
    <dt>RefCountDisposable</dt>
    <dd>
        The <em>RefCountDisposable</em> offers the ability to prevent the disposal of an underlying resource until all dependent resources have been disposed. You need an underlying <em>IDisposable</em> value to construct a <em>RefCountDisposable</em>. You can then call the <em>GetDisposable</em> method on the <em>RefCountDisposable</em> instance to retrieve a dependent resource. Each time a call to <em>GetDisposable</em> is made, an internal counter is incremented. Each time one of the dependent disposables from <em>GetDisposable</em> is disposed, the counter is decremented. Only if the counter reaches zero will the underlying be disposed of. This allows you to call <em>Dispose</em> on the <em>RefCountDisposable</em> itself before or after the count is zero.
    </dd>
    <dt>ScheduledDisposable</dt>
    <dd>
        In a similar fashion to <em>ContextDisposable</em>, the <em>ScheduledDisposable</em> type allows you to specify a scheduler, onto which the underlying resource will be disposed. You need to pass both the instance of <em>IScheduler</em> and instance of <em>IDisposable</em> to the constructor. When the <em>ScheduledDisposable</em> instance is disposed of, the disposal of the underlying resource will be scheduled onto the provided scheduler.
    </dd>
    <dt>SerialDisposable</dt>
    <dd>
        <em>SerialDisposable</em> is very similar to <em>MultipleAssignmentDisposable</em>, as they both expose a read/write <em>Disposable</em> property. The contrast between them is that whenever the <em>Disposable</em> property is set on a <em>SerialDisposable</em>, the previous value is disposed of. Like the <em>MultipleAssignmentDisposable</em>, once the <em>SerialDisposable</em> has been disposed of, the <em>Disposable</em> property will be set to null and any further attempts to set it will have the value disposed of. The value will remain as null.
    </dd>
    <dt>SingleAssignmentDisposable</dt>
    <dd>
        The <em>SingleAssignmentDisposable</em> class also exposes <em>IsDisposed</em> and <em>Disposable</em> properties. Like <em>MultipleAssignmentDisposable</em> and <em>SerialDisposable</em>, the <em>Disposable</em> value will be set to null when the <em>SingleAssignmentDisposable</em> is disposed of. The difference in implementation here is that the <em>SingleAssignmentDisposable</em> will throw an <em>InvalidOperationException</em> if there is an attempt to set the <em>Disposable</em> property while the value is not null and the <em>SingleAssignmentDisposable</em> has not been disposed of.
    </dd>
</dl>

<!-- 
TODO: we recently made SingleAssignmentDisposableValue public after a request to do so.


TODO: ICancelable?

TODO: StableCompositeDisposable?


TODO: fit this in?

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

Note that "Being disposed." is only printed once. -->
