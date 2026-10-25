# BoundedMailbox

`BoundedMailbox` (`src/DotNetActorFramework/Services/BoundedMailbox.cs`) is the FIFO `IMailbox` implementation with a fixed capacity. It wraps a `System.Threading.Channels.Channel<Envelope>` and applies one of four `MailboxOverflowPolicy` values when the channel is full. `MailboxService` creates it when the mailbox type is `FIFO` (the default). The same file also defines `BoundedPriorityMailbox`, which has the same overflow and watermark behavior but orders messages by `Envelope.Message.Priority`.

Messages are dequeued in arrival order. The mailbox never blocks on dequeue: `DequeueAsync` returns `null` when the queue is empty.

## Overflow policy

The policy is chosen in the constructor and applies to every enqueue. The default is `DropNewest`.

| Policy | Channel mode | Behavior when the mailbox is full | `EnqueueAsync` result |
|---|---|---|---|
| `DropNewest` | `BoundedChannelFullMode.DropNewest` | The channel removes the most recently queued message and stores the incoming one. The incoming message is always accepted. | `true` |
| `DropOldest` | `BoundedChannelFullMode.DropOldest` | The channel removes the oldest queued message (the head) and stores the incoming one. | `true` |
| `Fail` | `BoundedChannelFullMode.Wait` (see note) | Nothing is queued. The method throws `MailboxFullException`. | throws |
| `Wait` | `BoundedChannelFullMode.Wait` | `WriteAsync` waits asynchronously until a slot is free. There is no timeout and no cancellation token. | `true` once written |

Notes:

- **`DropNewest` drops the newest queued message, not the incoming one.** Example with capacity 2, queue `[1, 2]`: enqueueing `3` leaves `[1, 3]`. The message `2` is the one lost.
- **Drops are silent.** Messages removed by `DropNewest` or `DropOldest` are not counted in `DroppedMessageCount`, and no log entry is written. The caller only sees `true`.
- **`Fail` uses `TryWrite`.** The capacity check and the write are one atomic channel operation. In `Wait` mode `TryWrite` returns `false` when the channel is full instead of blocking, which is what the `Fail` branch relies on.
- **Unknown enum values** fall back to `DropNewest` when the channel options are built. `MailboxService` validates the policy first, so this fallback is only reachable when the constructor is called directly.

## Public API

### Constructor

#### `public BoundedMailbox(Guid actorId, int capacity, MailboxOverflowPolicy overflowPolicy = MailboxOverflowPolicy.DropNewest, int highWatermarkThreshold = 80, ILogger<BoundedMailbox>? logger = null)`

- **actorId**: The actor that owns the mailbox. Used in exception messages and log entries.
- **capacity**: Maximum number of queued messages. Must be at least `1`.
- **overflowPolicy**: The policy applied when the mailbox is full.
- **highWatermarkThreshold**: Load percentage (0-100, inclusive) at which warnings start. Default `80`.
- **logger**: Optional logger. `null` disables all logging from this mailbox.

**Throws:**
- `ArgumentException` (parameter `actorId`) if `actorId` is `Guid.Empty`.
- `MailboxConfigurationException` if `capacity` is less than `1`, or if `highWatermarkThreshold` is outside 0-100.

Note: the constructor's XML comment lists `ArgumentException` for the capacity and policy checks. The code throws `MailboxConfigurationException` for capacity and threshold, and it does not validate `overflowPolicy`. See [Known discrepancies](#known-discrepancies).

### Properties

| Member | Type | Description |
|---|---|---|
| `ActorId` | `Guid` | The owning actor. |
| `Capacity` | `int` | Maximum number of queued messages. |
| `OverflowPolicy` | `MailboxOverflowPolicy` | The policy set in the constructor. |
| `HighWatermarkWarningThreshold` | `int` | Warning threshold, as a percentage (0-100). |
| `IsFull` | `bool` | `true` when the queued count is at least `Capacity`. A snapshot; it can change right after it is read. |
| `DroppedMessageCount` | `int` | Number of enqueues that failed with an unexpected exception and returned `false`. Does not include messages dropped by the channel. |

### Methods

#### `public Task<bool> EnqueueAsync(Envelope envelope)`

Adds `envelope` to the mailbox according to the overflow policy. Returns `true` when the message was accepted. For `DropNewest` and `DropOldest`, `true` means the message was stored and the channel may have dropped another message to make room. Returns `false` only when an unexpected exception was caught; in that case `DroppedMessageCount` is incremented and an error is logged.

After a successful write, the method checks the watermark (see [High-watermark warnings](#high-watermark-warnings)).

**Throws:**
- `ObjectDisposedException` if the mailbox has been disposed, or if the channel is completed while `Wait` is blocked.
- `ArgumentNullException` if `envelope` is `null`. This check runs before the `try` block, so the exception is not counted or logged.
- `MailboxFullException` if the policy is `Fail` and the mailbox is full. Nothing is queued and `DroppedMessageCount` is not changed.

---

#### `public Task<Envelope?> DequeueAsync()`

Removes and returns the oldest message, or `null` if the mailbox is empty. It does not wait for a message to arrive.

**Throws:** `ObjectDisposedException` if the mailbox has been disposed.

---

#### `public int GetSize()`

Returns the number of queued messages (`ChannelReader.Count`). A snapshot.

#### `public double GetLoadFactor()`

Returns `GetSize() / Capacity` as a value from `0` (empty) to `1` (full).

#### `public void Dispose()`

Releases the mailbox. Disposing completes the channel writer and makes one `TryRead` call on the reader. Afterwards, `EnqueueAsync` and `DequeueAsync` throw `ObjectDisposedException`. Messages still in the channel are not reachable through the mailbox. `MailboxService.RemoveMailbox` and `Clear` call this method.

The finalizer calls `Dispose(false)`, which only marks the mailbox as disposed. It does not touch the channel.

## High-watermark warnings

After each successful enqueue, the mailbox logs a warning if the load factor is at or above `HighWatermarkWarningThreshold` and the mailbox is not empty. The message is:

```
Mailbox for actor {ActorId} has reached high watermark: {CurrentLoadPercent}% full (threshold: {Threshold}%)
```

Behavior to be aware of:

- A warning is logged on **every** enqueue while the load stays at or above the threshold, not once. The `DequeueAsync` code that was meant to reset a "warned" state is empty, so it has no effect.
- A threshold of `0` warns on every non-empty enqueue.
- Warnings require a non-null `logger`. `MailboxService` does not pass one (see below), so mailboxes it creates do not log warnings.

## Logging and the logger parameter

`MailboxService` casts its own `ILogger<MailboxService>` to `ILogger<BoundedMailbox>`. That cast is always `null`, because `ILogger<T>` is not covariant across different `T`. Mailboxes created through `MailboxService` therefore log nothing. Callers who construct `BoundedMailbox` directly can pass a logger to get warnings and error logs.

## Known discrepancies

These are places where the code, its XML comments, or other docs disagree. This document describes the code as it runs.

1. **`DropNewest` semantics.** `docs/MailboxService.md` says `DropNewest` drops "the incoming message". The channel actually drops the most recently queued message and keeps the incoming one (see the table above). This was checked against `System.Threading.Channels` with a standalone probe.
2. **Exception types in the constructor.** The XML comment lists `ArgumentException` for the capacity and policy checks. The code throws `MailboxConfigurationException` (which derives from `ConfigurationException`, not `ArgumentException`).
3. **Warning frequency.** The `IMailbox.HighWatermarkWarningThreshold` comment says a warning is "logged once until it drops below" the threshold. The code logs on every enqueue at or above the threshold.
4. **`Dispose` drain.** The comment says "Drain the channel", but the code performs a single `TryRead`, so at most one message is removed.
5. **Counter thread safety.** `DroppedMessageCount` is incremented with `++` and no `Interlocked`. Under concurrent failures, the count can be lower than the true number. Treat it as a diagnostic, not an exact count.

## Example

```csharp
using DotNetActorFramework.Enums;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

var actorId = Guid.NewGuid();

// Keep the 100 newest messages; older ones are evicted when full.
using var mailbox = new BoundedMailbox(actorId, 100, MailboxOverflowPolicy.DropOldest);

await mailbox.EnqueueAsync(envelope);

var next = await mailbox.DequeueAsync();
```

For back-pressure instead of eviction, use `Fail` and handle the exception:

```csharp
using var mailbox = new BoundedMailbox(actorId, 100, MailboxOverflowPolicy.Fail);

try
{
    await mailbox.EnqueueAsync(envelope);
}
catch (MailboxFullException)
{
    // The mailbox is full; the message was not queued.
}
```

`envelope` stands for an `Envelope` created by the caller.
