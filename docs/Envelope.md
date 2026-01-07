# Envelope
The `Envelope` type represents a transport wrapper for a message in the actor framework, carrying metadata such as sender, recipient, timestamps, delivery state, and retry information. It is used by the actor system to route messages and manage delivery semantics.

## API
### Envelope()
**Purpose:** Initializes a new instance of the `Envelope` class with default values.  
**Parameters:** None.  
**Returns:** A new `Envelope` object.  
**Throws:** None.

### Message
**Purpose:** Gets or sets the message payload contained in the envelope.  
**Parameters:** None.  
**Returns:** The `Message` object.  
**Throws:** None.

### Sender
**Purpose:** Gets or sets the optional sender of the envelope. May be `null` when the sender is unknown or irrelevant.  
**Parameters:** None.  
**Returns:** An `ActorRef?` representing the sender, or `null`.  
**Throws:** None.

### Recipient
**Purpose:** Gets or sets the intended recipient of the envelope.  
**Parameters:** None.  
**Returns:** An `ActorRef` representing the recipient.  
**Throws:** None.

### SentAt
**Purpose:** Gets or sets the date and time when the envelope was sent.  
**Parameters:** None.  
**Returns:** A `DateTime` value indicating the send time.  
**Throws:** None.

### EnvelopeId
**Purpose:** Gets or sets the unique identifier for the envelope.  
**Parameters:** None.  
**Returns:** A `Guid` that uniquely identifies this envelope instance.  
**Throws:** None.

### RetryCount
**Purpose:** Gets or sets the number of delivery attempts that have been made for this envelope.  
**Parameters:** None.  
**Returns:** An `int` indicating the current retry count.  
**Throws:** None.

### IsDelivered
**Purpose:** Gets or sets a flag indicating whether the envelope has been successfully delivered to its recipient.  
**Parameters:** None.  
**Returns:** `true` if the envelope has been marked as delivered; otherwise `false`.  
**Throws:** None.

### MarkAsDelivered()
**Purpose:** Marks the envelope as delivered by setting `IsDelivered` to `true`.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### IncrementRetryCount()
**Purpose:** Increments the `RetryCount` property by one.  
**Parameters:** None.  
**Returns:** `void`.  
**Throws:** None.

### GetElapsedTime()
**Purpose:** Calculates the time that has elapsed since the envelope was sent.  
**Parameters:** None.  
**Returns:** A `TimeSpan` representing `DateTime.UtcNow - SentAt`.  
**Throws:** None.

### HasExceededRetryLimit()
**Purpose:** Determines whether the retry count has surpassed the configured maximum allowed retries.  
**Parameters:** None.  
**Returns:** `true` if `RetryCount` exceeds the limit; otherwise `false`.  
**Throws:** None.

### GetDeliveryPriority()
**Purpose:** Computes a priority value used to order envelope delivery attempts; lower values indicate higher priority.  
**Parameters:** None.  
**Returns:** An `int` representing the delivery priority.  
**Throws:** None.

### ToString()
**Purpose:** Returns a string representation of the envelope, useful for debugging and logging.  
**Parameters:** None.  
**Returns:** A `string` containing the envelope’s key properties.  
**Throws:** None.

## Usage
### Example 1: Creating and sending an envelope
```csharp
var envelope = new Envelope
{
    Message = new GreetingMessage("Hello, Actor!"),
    Recipient = recipientRef,
    SentAt = DateTime.UtcNow
};

// Optional sender (may be null if unknown)
envelope.Sender = Context.Self;

// Dispatch the envelope through the actor system
actorSystem.Tell(envelope.Recipient, envelope);
```

### Example 2: Handling retries and delivery confirmation
```csharp
if (!envelope.IsDelivered && envelope.HasExceededRetryLimit())
{
    // Move to dead‑letter queue after too many attempts
    DeadLetterQueue.Enqueue(envelope);
}
else if (!envelope.IsDelivered)
{
    // Increment retry count and reschedule delivery
    envelope.IncrementRetryCount();
    var delay = TimeSpan.FromSeconds(Math.Pow(2, envelope.RetryCount)); // exponential backoff
    Task.Delay(delay).ContinueWith(_ =>
        actorSystem.Tell(envelope.Recipient, envelope));
}
else
{
    // Message successfully delivered
    envelope.MarkAsDelivered();
}
```

## Notes
- The `Sender` property may be `null`; code that depends on a sender should handle this case.
- `SentAt` should be set to a meaningful time (typically `DateTime.UtcNow`) before the envelope is processed; otherwise `GetElapsedTime` will return misleading values.
- `EnvelopeId` is intended to be unique; duplicating IDs may cause incorrect deduplication logic in higher‑level components.
- `RetryCount` is an `int`; excessive retries could lead to overflow, though in practice limits are far below `int.MaxValue`.
- `MarkAsDelivered` should be called only after a successful delivery; calling it prematurely may mask failures.
- `IncrementRetryCount` is not atomic; if the envelope is accessed concurrently from multiple threads, external synchronization (e.g., `lock` or `Interlocked`) is required to avoid lost updates.
- `GetElapsedTime` relies on the system clock; adjustments to the system time may affect the returned interval.
- `HasExceededRetryLimit` and `GetDeliveryPriority` depend on policies defined elsewhere; their behavior is consistent as long as those policies do not change during the envelope’s lifetime.
- The type does not provide any built‑in thread‑safety; instances shared across threads should be protected by the caller.
- `ToString` is primarily for diagnostics and may allocate memory; avoid invoking it in performance‑critical paths.
