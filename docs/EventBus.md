# EventBus

`EventBus` is an in-process, typed publish/subscribe channel for domain events. Components subscribe a delegate to a concrete event type, and any caller that publishes an event of that type invokes every subscribed delegate. It lives in the `DotNetActorFramework.Events` namespace and is defined in `src/DotNetActorFramework/Events/EventBus.cs`.

The bus is for domain notifications (for example, "an actor was created"). It is not an actor mailbox: nothing is delivered to a specific `ActorRef`, and there is no ordering, retry, or persistence. Use `ActorRef` messaging for work that must be delivered to an actor.

An `EventBus` can be created directly with `new EventBus()`, or obtained from an `ActorSystemBuilder` that was configured with `WithEventBus()`; `GetEventBus()` returns `null` if the feature was not enabled.

## Publish/Subscribe Model

- **Routing is by the static event type.** `Subscribe<TEvent>` and `PublishAsync<TEvent>` both key on `typeof(TEvent)`. A handler registered for `DomainEvent` or `IDomainEvent` does not receive events of derived types, and publishing through an `IDomainEvent`-typed variable routes to subscribers of `IDomainEvent`, not to the runtime type's subscribers.
- **Subscriptions are per delegate.** The same delegate instance may be subscribed more than once and will then be invoked once per subscription.
- **Publishing is a snapshot.** `PublishAsync` and `TryPublishAsync` copy the current subscriber list under a lock and then invoke the copy outside the lock. Handlers may subscribe or unsubscribe while a publish is in progress; such changes apply to later publishes.
- **Handlers run concurrently.** All handlers for an event are started before any of them is awaited. Handlers therefore must be safe to run alongside each other and must not assume an execution order.
- **No automatic cleanup.** The bus holds strong references to handlers until they are unsubscribed, `SubscribeOnce` fires, or `Clear()` is called. Subscribe with the lifetime of the subscriber in mind.

## Types

### `IDomainEvent`

Contract for all events published on the bus.

| Member | Description |
|---|---|
| `Guid EventId { get; }` | Unique identifier of this event instance. |
| `DateTime OccurredAt { get; }` | UTC timestamp at which the event occurred. |
| `string EventType { get; }` | Logical type name, such as `actor.created`. |

### `DomainEvent`

Abstract base class that implements `IDomainEvent`. Its protected constructor assigns a new `EventId` and sets `OccurredAt` to `DateTime.UtcNow`. Derived classes override `EventType`.

### `EventHandler<in TEvent>`

```csharp
public delegate Task EventHandler<in TEvent>(TEvent @event) where TEvent : IDomainEvent;
```

The handler signature. It is declared in `DotNetActorFramework.Events`; see the note on name clashes under [Notes](#notes).

### Built-in event types

The library defines the following event types. They all derive from `ActorSystemEvent`, which derives from `DomainEvent` and adds `string SystemName` and `Guid SystemId`.

| Type | `EventType` | Additional properties |
|---|---|---|
| `ActorCreatedEvent` | `actor.created` | `string ActorPath`, `Guid ActorId` |
| `ActorTerminatedEvent` | `actor.terminated` | `string ActorPath`, `Guid ActorId` |
| `ActorErrorEvent` | `actor.error` | `string ActorPath`, `Guid ActorId`, `string ErrorMessage`, `string StackTrace` |
| `MessageProcessedEvent` | `message.processed` | `string ActorPath`, `Guid MessageId`, `string MessageType`, `long ElapsedMilliseconds` |

These types are only definitions. Nothing in the library currently publishes them; the caller that owns the actor lifecycle is responsible for constructing and publishing them.

## API

### `void Subscribe<TEvent>(EventHandler<TEvent> handler)`

Adds `handler` to the subscribers for `TEvent`.

- **Parameters**
  - `handler`: The delegate to invoke when an event of type `TEvent` is published. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `handler` is `null`.
- **Notes**
  - Duplicates are not filtered. Subscribing the same delegate twice causes two invocations per publish.

### `void SubscribeOnce<TEvent>(EventHandler<TEvent> handler)`

Adds a wrapper that invokes `handler` for the first `TEvent` published and then removes itself.

- **Parameters**
  - `handler`: The delegate to invoke at most once. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `handler` is `null`.
- **Notes**
  - The wrapper is removed before `handler` runs, and an interlocked flag guarantees a single invocation even when several publishes overlap.
  - The wrapper is internal, so the caller has no reference to pass to `Unsubscribe`. To cancel a one-time subscription before it fires, use `Subscribe` and `Unsubscribe` instead.
  - If `handler` throws, the subscription is still consumed.

### `void Unsubscribe<TEvent>(EventHandler<TEvent> handler)`

Removes one subscription of `handler` for `TEvent`.

- **Parameters**
  - `handler`: The delegate to remove. A `null` value is ignored.
- **Throws**
  - None.
- **Notes**
  - Only the first matching subscription is removed. If the delegate was subscribed twice, call this method twice.
  - Delegates compare by target and method. Two lambdas are equal only if they are the same delegate instance, so keep a reference to the delegate you subscribed.
  - Removing an unknown handler is a no-op. When the last subscriber for a type is removed, the type's entry is dropped.

### `Task PublishAsync<TEvent>(TEvent @event)`

Invokes every handler subscribed to `TEvent` with `@event` and completes when all of them have completed.

- **Parameters**
  - `@event`: The event to publish. Must not be `null`.
- **Returns**
  - A task that completes when all handlers complete. It completes immediately if there are no subscribers.
- **Throws** (through the returned task)
  - `ArgumentNullException`: If `@event` is `null`.
  - The exception from a failing handler. If one or more handlers fault, the task waits for all handlers to finish and then faults with one of their exceptions. The others are not observed.
- **Notes**
  - A handler that throws synchronously, before it returns a `Task`, stops the publish at that point. Handlers after it are not invoked, and handlers already started keep running. Declare handlers as `async` to avoid this.
  - Use `TryPublishAsync` when you need every handler to run regardless of failures.

### `Task<IReadOnlyList<Exception>> TryPublishAsync<TEvent>(TEvent @event)`

Publishes `@event` to every subscribed handler and collects handler failures instead of throwing them.

- **Parameters**
  - `@event`: The event to publish. Must not be `null`.
- **Returns**
  - The exceptions thrown by handlers, in no guaranteed order. The list is empty when every handler succeeds or when there are no subscribers.
- **Throws** (through the returned task)
  - `ArgumentNullException`: If `@event` is `null`.
- **Notes**
  - Every handler is invoked, including those that throw synchronously.
  - The method does not report handler failures through its own exception, so check the returned list.

### `int GetSubscriberCount<TEvent>()`

Returns the number of subscriptions currently registered for `TEvent`.

- **Returns**
  - The count, or `0` if there are none. Duplicate subscriptions are counted separately, and `SubscribeOnce` wrappers are counted until they fire.
- **Throws**
  - None.

### `void Clear()`

Removes all subscriptions for all event types.

- **Throws**
  - None.
- **Notes**
  - This affects every component sharing the bus. Calling it while publishes are in progress is safe; those publishes continue with their snapshot.

## Thread Safety

Subscription changes and snapshots are guarded by a single internal lock, so every public method may be called from multiple threads. The lock is not held while handlers run, so a handler may safely call `Subscribe`, `Unsubscribe`, or `PublishAsync` on the same bus.

## Usage

### Example 1: Define, subscribe, and publish

```csharp
using DotNetActorFramework.Events;

var bus = new EventBus();

bus.Subscribe<OrderPlacedEvent>(async evt =>
{
    Console.WriteLine($"Order {evt.OrderId} placed for {evt.Total:C}");
    await Task.CompletedTask;
});

await bus.PublishAsync(new OrderPlacedEvent { OrderId = "A-1001", Total = 42.50m });

// Type declarations must follow top-level statements in a Program.cs file.
public sealed class OrderPlacedEvent : DomainEvent
{
    public override string EventType => "order.placed";

    public string OrderId { get; init; } = string.Empty;
    public decimal Total { get; init; }
}
```

### Example 2: One-time handler and collecting failures

```csharp
using DotNetActorFramework.Events;

var bus = new EventBus();

// Fires for the first event only; removed automatically before it runs.
bus.SubscribeOnce<OrderPlacedEvent>(async evt =>
{
    Console.WriteLine($"First order seen: {evt.OrderId}");
    await Task.CompletedTask;
});

// Reuses OrderPlacedEvent from Example 1. A failing subscriber does not stop the others when using TryPublishAsync.
bus.Subscribe<OrderPlacedEvent>(async evt =>
{
    if (evt.Total < 0)
        throw new InvalidOperationException("Negative totals are not allowed.");

    await Task.CompletedTask;
});

var failures = await bus.TryPublishAsync(new OrderPlacedEvent { OrderId = "A-1002", Total = -1m });

foreach (var ex in failures)
    Console.WriteLine($"Handler failed: {ex.Message}");
```

### Example 3: Unsubscribing and inspecting subscribers

```csharp
using DotNetActorFramework.Events;

var bus = new EventBus();

// Keep a reference to the delegate; Unsubscribe needs the same instance.
DotNetActorFramework.Events.EventHandler<ActorCreatedEvent> onCreated = async evt =>
{
    Console.WriteLine($"Actor created: {evt.ActorPath}");
    await Task.CompletedTask;
};

bus.Subscribe(onCreated);
Console.WriteLine(bus.GetSubscriberCount<ActorCreatedEvent>()); // 1

bus.Unsubscribe(onCreated);
Console.WriteLine(bus.GetSubscriberCount<ActorCreatedEvent>()); // 0
```

## Notes

- **Name clashes.** `EventHandler<TEvent>` in this namespace shares its name with `System.EventHandler<TEventArgs>`. If a file imports both `System` and `DotNetActorFramework.Events`, write the type name fully qualified (`DotNetActorFramework.Events.EventHandler<T>`) or use a lambda with `Subscribe`, as the examples do.
- **Exception handling.** Prefer `TryPublishAsync` when a failing subscriber must not prevent others from running. Use `PublishAsync` when the publisher should observe failures.
- **Scope.** The bus lives in process and is not shared across nodes. For delivery to other systems, see the `Integration` namespace, where `IntegrationEventPublisher` uses the bus internally.
