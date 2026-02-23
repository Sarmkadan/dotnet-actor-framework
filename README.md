// ... (rest of the file remains unchanged)

## InMemoryEventJournal

The `InMemoryEventJournal` is an in-memory implementation of the event journal for testing and development purposes. It stores events in a concurrent dictionary, allowing for fast and efficient storage and retrieval of events.

### Usage Example

```csharp
var eventJournal = new InMemoryEventJournal();
var actorId = Guid.NewGuid();
var actorPath = new ActorPath("order-processing", "order-123");

var events = new List<ActorEvent>
{
    new ActorEvent(
        eventId: Guid.NewGuid(),
        occurredAt: DateTime.UtcNow,
        eventType: "OrderCreated",
        data: new { OrderId = "order-123", CustomerId = "cust-456" }
    ),
    new ActorEvent(
        eventId: Guid.NewGuid(),
        occurredAt: DateTime.UtcNow,
        eventType: "OrderUpdated",
        data: new { OrderId = "order-123", Status = "Processing" }
    )
};

await eventJournal.AppendEventsAsync(actorId, actorPath, events);

// Read events
var recentEvents = await eventJournal.ReadEventsAsync(actorId, actorPath, 1, 100);
foreach (var e in recentEvents)
{
    Console.WriteLine($"Event {e.EventId} - {e.EventType}");
}

// Read events in reverse order
var pastEvents = await eventJournal.ReadEventsBackwardAsync(actorId, actorPath, 1, 100);
foreach (var e in pastEvents)
{
    Console.WriteLine($"Event {e.EventId} - {e.EventType}");
}

// Delete old events
await eventJournal.DeleteEventsAsync(actorId, actorPath, 1);

// Delete all events
await eventJournal.DeleteAllEventsAsync(actorId, actorPath);
```

// ... (rest of the file remains unchanged)
