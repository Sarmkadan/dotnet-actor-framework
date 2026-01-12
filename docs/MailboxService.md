# MailboxService

The `MailboxService` is a concurrency-safe mailbox manager for actor-based systems in the dotnet-actor-framework. It maintains per-actor message queues, enforces capacity limits, and provides asynchronous enqueue/dequeue semantics for message passing between actors. The service tracks mailbox statistics and allows inspection of queue state without blocking.

## API

### `public MailboxService(int capacity)`
Initializes a new `MailboxService` with the specified maximum number of messages it can hold.

- **capacity**: The maximum number of messages the mailbox can store. Must be a positive integer.

Throws `ArgumentOutOfRangeException` if `capacity` is zero or negative.

---

### `public IMailbox CreateMailbox()`
Creates and registers a new mailbox instance associated with this service.

- **Return value**: A new `IMailbox` instance tied to this service.

The returned mailbox is automatically tracked by the service and will appear in statistics.

---

### `public IMailbox? GetMailbox()`
Retrieves the mailbox instance associated with the current actor context.

- **Return value**: The `IMailbox` instance for the current actor, or `null` if none exists.

This method is typically used within actor logic to access the mailbox tied to the executing actor.

---

### `public async Task EnqueueAsync(Envelope envelope)`
Asynchronously enqueues a message into the current actor’s mailbox.

- **envelope**: The message envelope to enqueue.
- **Return value**: A `Task` that completes when the message has been accepted.

If the mailbox is full, this method blocks until space becomes available or the operation is canceled.

---

### `public async Task<Envelope?> DequeueAsync()`
Asynchronously dequeues the next message from the current actor’s mailbox.

- **Return value**: A `Task<Envelope?>` resolving to the next message, or `null` if the mailbox is empty and closed.

Returns `null` if the mailbox is empty and no further messages are expected.

---

### `public int GetMailboxSize()`
Gets the current number of messages in the mailbox.

- **Return value**: The count of messages currently in the mailbox.

This is a snapshot value and may change immediately after return.

---

### `public bool IsMailboxFull()`
Determines whether the mailbox has reached its capacity.

- **Return value**: `true` if the mailbox is at or above capacity; otherwise, `false`.

Useful for backpressure signaling in actor logic.

---

### `public void RemoveMailbox()`
Removes the mailbox associated with the current actor from the service.

The mailbox is deregistered and will no longer be accessible via `GetMailbox()`. Any pending messages are discarded.

---

### `public MailboxStatistics GetStatistics()`
Retrieves a snapshot of current mailbox statistics.

- **Return value**: A `MailboxStatistics` struct containing counts and load metrics.

The returned statistics are a point-in-time snapshot and may not reflect concurrent changes.

---

### `public void Clear()`
Removes all messages from the current actor’s mailbox.

This operation is synchronous and does not block other operations.

---

### `public Guid ActorId`
Gets the unique identifier of the actor associated with the current mailbox context.

- **Return value**: The `Guid` representing the actor’s identity.

This value is constant for the lifetime of the actor.

---

### `public int Capacity`
Gets the maximum number of messages the mailbox can hold.

- **Return value**: The configured capacity as a positive integer.

This value is set at construction and does not change.

---

### `public IMailbox Mailbox`
Gets the mailbox instance associated with the current actor context.

- **Return value**: The `IMailbox` instance for the current actor.

This property is equivalent to calling `GetMailbox()` and will return `null` if no mailbox exists.

---

### `public async Task<bool> EnqueueAsync(Envelope envelope, CancellationToken cancellationToken)`
Enqueues a message with explicit cancellation support.

- **envelope**: The message envelope to enqueue.
- **cancellationToken**: A token to observe for cancellation.
- **Return value**: A `Task<bool>` that returns `true` if the message was enqueued, `false` if canceled.

If the token is canceled before the message is accepted, the operation aborts and returns `false`.

---

### `public async Task<Envelope?> DequeueAsync(CancellationToken cancellationToken)`
Dequeues a message with explicit cancellation support.

- **cancellationToken**: A token to observe for cancellation.
- **Return value**: A `Task<Envelope?>` resolving to the next message, `null` if empty and closed, or `null` if canceled.

If the token is canceled before a message is available, the operation aborts and returns `null`.

---

### `public int GetSize()`
Gets the current number of messages in the mailbox.

- **Return value**: The count of messages currently in the mailbox.

This method is identical in behavior to `GetMailboxSize()`.

---

### `public double GetLoadFactor()`
Computes the current load as a ratio of messages to capacity.

- **Return value**: A `double` between `0.0` and `1.0`, or greater than `1.0` if over capacity.

Useful for monitoring backpressure and scaling decisions.

---
### `public void Dispose()`
Releases all resources held by the `MailboxService`.

After disposal, the service is no longer usable. Any pending operations may throw `ObjectDisposedException`.

---

## Usage

### Example 1: Basic Actor Mailbox Usage
