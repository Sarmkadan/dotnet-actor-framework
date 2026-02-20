// ... (rest of the file remains unchanged)

## PersistenceService

The `PersistenceService` provides a unified facade for managing actor persistence in the DotNetActorFramework. It combines snapshot storage and event journaling operations into a single service, allowing actors to persist their state and recover from failures. The service coordinates between an `ISnapshotStore` for state snapshots and an `IEventJournal` for event sourcing, providing methods for both snapshot management and event journal operations.


### Usage Example

```csharp
// Create required dependencies (typically injected via DI)
var snapshotStore = new InMemorySnapshotStore();
var eventJournal = new InMemoryEventJournal();
var loggerFactory = LoggerFactory.Create(builder => {});

// Initialize the persistence service
var persistenceService = new PersistenceService(
    snapshotStore,
    eventJournal,
    loggerFactory.CreateLogger<PersistenceService>()
);

// Example actor ID and path
guid actorId = Guid.NewGuid();
var actorPath = new ActorPath("order-processing", "order-123");

// Save actor state as a snapshot
var orderState = new OrderState
{
    OrderId = "order-123",
    Status = "Processing",
    Items = new List<OrderItem> { new OrderItem("item-1", 2) }
};

await persistenceService.SaveSnapshotAsync(actorId, actorPath, orderState, sequenceNr: 42);

// Load the latest snapshot
var latestSnapshot = await persistenceService.LoadLatestSnapshotAsync(actorId, actorPath);

// Append events to the journal
var events = new List<ActorEvent>
{
    new ActorEvent(
        eventId: Guid.NewGuid(),
        occurredAt: DateTime.UtcNow,
        eventType: "OrderCreated",
        data: new { OrderId = "order-123", CustomerId = "cust-456" }
    )
};

await persistenceService.AppendEventsAsync(actorId, actorPath, events);

// Read events from the journal
var recentEvents = await persistenceService.ReadEventsAsync(
    actorId, 
    actorPath, 
    fromSequenceNr: 1,
    toSequenceNr: 100
);

// Delete old snapshots and events when they're no longer needed
await persistenceService.DeleteSnapshotsAsync(actorId, actorPath, maxSequenceNr: 30);
await persistenceService.DeleteEventsAsync(actorId, actorPath, maxSequenceNr: 30);
```

## IDomainEvent

The `IDomainEvent` interface represents a domain event in the actor system, providing a way to publish and subscribe to events. It defines properties such as `EventId`, `OccurredAt`, and `EventType`, which are used to identify and characterize the event.

### Usage Example

```csharp
public class OrderPlacedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
    public string EventType => nameof(OrderPlacedEvent);

    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
}

public class OrderService
{
    private readonly EventBus _eventBus;

    public OrderService(EventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public async Task PlaceOrder(Order order)
    {
        // Process the order
        var @event = new OrderPlacedEvent
        {
            OrderId = order.Id,
            Amount = order.Amount
        };

        await _eventBus.PublishAsync(@event);
    }
}
```

// ... (rest of the file remains unchanged)
