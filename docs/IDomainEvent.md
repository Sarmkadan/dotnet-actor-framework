# IDomainEvent

The `IDomainEvent` interface defines the core contract for domain events within the `dotnet-actor-framework`, providing both the structural definition for event data and the necessary mechanisms for event-driven communication via subscription and publication within the actor system.

## API

### Properties

*   **`Guid EventId`**: The unique identifier for a specific instance of the domain event.
*   **`DateTime OccurredAt`**: The timestamp indicating when the event was generated.
*   **`string EventType`**: An abstract identifier representing the type of the domain event.
*   **`string SystemName`**: The name of the domain system where the event originated.
*   **`Guid SystemId`**: The unique identifier for the domain system.
*   **`string ActorPath`**: The hierarchical path of the actor associated with the event.
*   **`Guid ActorId`**: The unique identifier of the actor associated with the event.
*   **`string ErrorMessage`**: If the event represents a failure, this property provides the associated error description.
*   **`string StackTrace`**: If the event represents a failure, this property provides the associated stack trace information.

### Delegates

*   **`delegate Task EventHandler<in TEvent>(TEvent @event)`**: Defines the signature for methods that handle published domain events.

### Methods

*   **`void Subscribe<TEvent>(EventHandler<TEvent> handler)`**: Subscribes the specified handler to events of type `TEvent`.
*   **`void Unsubscribe<TEvent>(EventHandler<TEvent> handler)`**: Removes a previously subscribed handler for events of type `TEvent`.
*   **`async Task PublishAsync<TEvent>(TEvent @event)`**: Publishes an event of type `TEvent` to all currently subscribed handlers. This operation is asynchronous. Throws an exception if any handler execution fails.
*   **`int GetSubscriberCount<TEvent>()`**: Returns the current number of subscribers registered for the specified event type.
*   **`void Clear()`**: Removes all active event subscriptions from the system.

## Usage

### Example 1: Basic Event Subscription and Publication

```csharp
public record UserCreatedEvent(string Username) : IDomainEvent;

// Subscription
domainEventSystem.Subscribe<UserCreatedEvent>(async (evt) => {
    Console.WriteLine($"User created: {evt.Username}");
    await Task.CompletedTask;
});

// Publication
var newUserEvent = new UserCreatedEvent("john_doe");
await domainEventSystem.PublishAsync(newUserEvent);
```

### Example 2: Handling Failure Events

```csharp
public record ProcessFailedEvent(string ErrorMessage, string StackTrace) : IDomainEvent;

// Handler for failures
domainEventSystem.Subscribe<ProcessFailedEvent>(async (evt) => {
    logger.LogError($"Processing failed: {evt.ErrorMessage}. Trace: {evt.StackTrace}");
    await Task.CompletedTask;
});
```

## Notes

*   **Thread Safety**: While the `dotnet-actor-framework` ensures that the `Subscribe`, `Unsubscribe`, and subscription management methods are thread-safe, the handlers themselves may be executed concurrently. Developers should ensure that handlers are thread-safe if they access shared state.
*   **Execution Order**: Handlers subscribed via `Subscribe<TEvent>` do not have guaranteed execution order.
*   **Exception Handling**: `PublishAsync` may throw exceptions if an individual event handler fails. It is recommended that event handlers implement internal try-catch blocks to prevent a single failing handler from disrupting the entire publication chain.
*   **Clear Operation**: The `Clear()` method is destructive and removes all subscriptions across all event types. Use with caution in systems where persistence of subscriptions is required.
