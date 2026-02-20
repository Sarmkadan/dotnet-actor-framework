// ... (rest of the file remains unchanged)

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
