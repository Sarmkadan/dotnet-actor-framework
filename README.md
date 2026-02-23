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

## ExternalServiceClient

The `ExternalServiceClient` is a generic HTTP client designed for integrating with external RESTful services. It provides convenient methods for making GET, POST, PUT, and DELETE requests with built-in error handling, automatic retries, and JSON serialization support.

### Usage Example

```csharp
// Create a client for a payment service API
var paymentServiceClient = new ExternalServiceClient(
    baseUrl: "https://api.paymentservice.com/v1",
    maxRetries: 3,
    retryDelay: TimeSpan.FromSeconds(1)
);

try
{
    // GET request - retrieve payment status
    var paymentStatus = await paymentServiceClient.GetAsync<PaymentStatus>(
        $"payments/{paymentId}"
    );
    Console.WriteLine($"Payment status: {paymentStatus?.Status}");

    // POST request - create a new payment
    var newPayment = await paymentServiceClient.PostAsync<PaymentResult>(
        "payments",
        new { amount = 99.99m, currency = "USD", customerId = customerId }
    );
    Console.WriteLine($"Created payment: {newPayment?.Id}");

    // PUT request - update payment details
    var updatedPayment = await paymentServiceClient.PutAsync<PaymentResult>(
        $"payments/{paymentId}",
        new { amount = 149.99m, status = "completed" }
    );

    // DELETE request - cancel a payment
    var isDeleted = await paymentServiceClient.DeleteAsync(
        $"payments/{paymentId}"
    );
    Console.WriteLine($"Payment deleted: {isDeleted}");
}
finally
{
    paymentServiceClient.Dispose();
}
```

// ... (rest of the file remains unchanged)
