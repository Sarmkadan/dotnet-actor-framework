# MailboxService

`MailboxService` (`src/DotNetActorFramework/Services/MailboxService.cs`) owns the per-actor mailboxes of an actor system. It maps each actor's `Guid` to one `IMailbox`, creates mailboxes from `ActorSystemOptions` defaults or per-call overrides, and forwards enqueue/dequeue calls to the mailbox it finds. It wraps unexpected failures in `MailboxException`.

The service does not implement `IDisposable`. Mailboxes are released explicitly through `RemoveMailbox` or `Clear`.

## Mailbox lifecycle

1. **Create.** `CreateMailbox(actorId, ...)` validates the effective settings, builds a `BoundedPriorityMailbox` (when `MailboxType.Priority`) or a `BoundedMailbox` (otherwise), and registers it under `actorId`. A second `CreateMailbox` call for the same actor throws `InvalidOperationException`; the new instance is disposed before the throw.
2. **Use.** `EnqueueAsync` and `DequeueAsync` route through the registered mailbox. `GetMailbox` returns the same instance for direct access.
3. **Remove.** `RemoveMailbox(actorId)` deregisters the mailbox and disposes it. Messages still queued are discarded. Later service calls for that actor fail with `MailboxException` ("Mailbox not found"). Holders of an earlier `IMailbox` reference get `ObjectDisposedException` on further use.
4. **Clear.** `Clear()` disposes every registered mailbox and empties the registry. The service stays usable, and new mailboxes can be created afterwards.

## Configuration

Defaults come from the `ActorSystemOptions` passed to the constructor:

| Option | Default | Used when |
|---|---|---|
| `DefaultMailboxCapacity` | `ActorConstants.DefaultMailboxCapacity` (1000) | `capacity` argument is `0` |
| `DefaultMailboxType` | `MailboxType.FIFO` | `mailboxType` argument is `null` |
| `DefaultMailboxOverflowPolicy` | `MailboxOverflowPolicy.DropNewest` | `overflowPolicy` argument is `null` |
| `HighWatermarkWarningThreshold` | `80` | always (not overridable per mailbox) |

Validation uses `MailboxOptions.Validate()`:

- The constructor validates the defaults derived from `ActorSystemOptions` and throws `ArgumentOutOfRangeException` if any are invalid.
- `CreateMailbox` validates the effective settings after defaults are applied. Negative capacity, undefined enum values, and thresholds outside 0-100 throw `ArgumentOutOfRangeException`.

## Overflow policies

Each policy applies to `BoundedMailbox` and `BoundedPriorityMailbox`, which are backed by `System.Threading.Channels`:

| Policy | Behavior when the mailbox is full |
|---|---|
| `DropNewest` | The incoming message is dropped. `EnqueueAsync` on the mailbox still returns `true`. |
| `DropOldest` | The oldest queued message is dropped to make room. `EnqueueAsync` still returns `true`. |
| `Wait` | The writer waits asynchronously until space is available. There is no timeout. |
| `Fail` | The mailbox throws `MailboxFullException`. `MailboxService.EnqueueAsync` surfaces it as a `MailboxException` whose inner exception is the `MailboxFullException`. |

Messages dropped by `DropNewest` or `DropOldest` do not increment `DroppedMessageCount`, because the channel drops them without throwing. Other exceptions during a mailbox enqueue are logged and increment `DroppedMessageCount`, and `EnqueueAsync` returns `false`. The service then throws `MailboxException` with the message "Mailbox is full". That text is misleading for these non-capacity failures.

## Public API

### `MailboxService` class

#### `public MailboxService()` (protected)
Parameterless constructor for testing and mocking. It uses a new `ActorSystemOptions` with default values and no logger. Subclasses can call it; external callers cannot.

---

#### `public MailboxService(ActorSystemOptions options, ILogger<MailboxService>? logger = null)`
Creates a service that uses `options` for defaults.

- **options**: The actor system options. Must not be `null`.
- **logger**: Optional logger. It is stored, but mailboxes do not receive it: `CreateMailbox` casts it to `ILogger<BoundedMailbox>` / `ILogger<BoundedPriorityMailbox>`, and that cast is always `null` because `ILogger<T>` does not allow converting between different `T`. Mailboxes therefore log nothing through this logger.

**Throws:** `ArgumentNullException` if `options` is `null`; `ArgumentOutOfRangeException` if the mailbox-related options are invalid.

---

#### `public IMailbox CreateMailbox(Guid actorId, int capacity = 0, MailboxType? mailboxType = null, MailboxOverflowPolicy? overflowPolicy = null)`
Creates a mailbox for an actor and registers it. Registration uses an atomic `ConcurrentDictionary.TryAdd`, so two concurrent calls for the same actor cannot both succeed.

- **actorId**: The actor that owns the mailbox.
- **capacity**: Maximum number of messages. `0` uses `DefaultMailboxCapacity`. Negative values are rejected.
- **mailboxType**: `FIFO` or `Priority`. `null` uses `DefaultMailboxType`.
- **overflowPolicy**: The overflow policy. `null` uses `DefaultMailboxOverflowPolicy`.
- **Returns:** The new mailbox. It is an `IMailbox`, and its runtime type is `BoundedPriorityMailbox` or `BoundedMailbox`.

**Throws:**
- `ArgumentException` if `actorId` is `Guid.Empty`.
- `ArgumentOutOfRangeException` if the effective settings are invalid.
- `InvalidOperationException` if a mailbox already exists for `actorId`.

---

#### `public IMailbox? GetMailbox(Guid actorId)`
Returns the mailbox registered for `actorId`, or `null` if there is none. It does not validate `actorId`, so `Guid.Empty` simply returns `null`.

---

#### `public async Task EnqueueAsync(Guid actorId, Envelope envelope)`
Enqueues `envelope` into the mailbox of `actorId`.

- **actorId**: The target actor.
- **envelope**: The message to enqueue.
- **Returns:** A task that completes when the mailbox accepts the message.

The method returns normally if the mailbox accepted the message. A message dropped by `DropNewest` or `DropOldest` still counts as accepted. Under `Fail`, a full mailbox throws.

**Throws:**
- `ArgumentException` if `actorId` is `Guid.Empty`.
- `ArgumentNullException` if `envelope` is `null`. This is thrown directly, not wrapped.
- `MailboxException` if no mailbox exists for `actorId`, the mailbox reports failure (including a full `Fail` mailbox, where the `MailboxFullException` is the inner exception), or the mailbox is disposed.

---

#### `public async Task<Envelope?> DequeueAsync(Guid actorId)`
Takes the next message from the mailbox of `actorId`. It does not block.

- **actorId**: The actor whose mailbox to read.
- **Returns:** The next envelope, or `null` if the mailbox is empty.

For `Priority` mailboxes, the highest-priority message comes first (`Envelope.Message.Priority`, higher values first). For `FIFO` mailboxes, messages come out in arrival order.

**Throws:**
- `ArgumentException` if `actorId` is `Guid.Empty`.
- `MailboxException` if no mailbox exists for `actorId` or the dequeue fails unexpectedly.

---

#### `public int GetMailboxSize(Guid actorId)`
Returns the number of queued messages for `actorId`, or `0` if no mailbox exists. The value is a snapshot and can change immediately after it is returned.

---

#### `public bool IsMailboxFull(Guid actorId)`
Returns `true` if the mailbox for `actorId` is full, or `false` if the mailbox is not full or does not exist. Useful for backpressure checks. The result is a snapshot.

---

#### `public void RemoveMailbox(Guid actorId)`
Deregisters the mailbox for `actorId` and disposes it. Messages still queued are discarded. If no mailbox exists, the call does nothing.

**Throws:**
- `ArgumentException` if `actorId` is `Guid.Empty`.
- `MailboxException` if disposal fails unexpectedly.

---

#### `public MailboxStatistics GetStatistics()`
Returns a snapshot of all registered mailboxes:

- `TotalMailboxes`: number of registered mailboxes.
- `TotalMessages`: sum of queued messages.
- `TotalCapacity`: sum of capacities.
- `FullMailboxes`: number of mailboxes that are full.
- `AverageLoadFactor`: `TotalMessages / TotalCapacity`, or `0` when there are no mailboxes.

The values are gathered across mailboxes without a global lock, so they may not match one instant exactly.

---

#### `public void Clear()`
Disposes every registered mailbox and removes all registrations. Messages still queued are discarded. `Clear` reads the registry and then empties it, so a mailbox created by another thread in between is removed without being disposed. Call `Clear` during shutdown, or when no other thread is creating mailboxes.

---

#### `public override string ToString()`
Returns a one-line summary of `GetStatistics()`, for example `MailboxService { TotalMailboxes = 3, TotalMessages = 12, ... }`.

---

### `MailboxStatistics` class

A plain data class returned by `GetStatistics()`. All properties have public getters and setters.

| Property | Type | Description |
|---|---|---|
| `TotalMailboxes` | `int` | Number of registered mailboxes. |
| `TotalMessages` | `long` | Total queued messages. |
| `TotalCapacity` | `long` | Sum of mailbox capacities. |
| `FullMailboxes` | `int` | Number of full mailboxes. |
| `AverageLoadFactor` | `double` | Average load factor (0-1). |

---

## Other mailbox types in the same file

`MailboxService.cs` also defines `Mailbox` and `PriorityMailbox`. `MailboxService` does not create them. They are standalone `IMailbox` implementations built on `ConcurrentQueue` and `PriorityQueue` with a `SemaphoreSlim` for capacity. Their `EnqueueAsync` waits up to 100 ms for space and returns `false` if none becomes available. They have no overflow policy. Their constructors take a `highWatermarkThreshold` and an optional logger.

## Example

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

var options = new ActorSystemOptions { DefaultMailboxCapacity = 500 };
var mailboxes = new MailboxService(options);

var actorId = Guid.NewGuid();
mailboxes.CreateMailbox(actorId, mailboxType: MailboxType.Priority, overflowPolicy: MailboxOverflowPolicy.Fail);

try
{
    await mailboxes.EnqueueAsync(actorId, envelope);
}
catch (MailboxException ex) when (ex.InnerException is DotNetActorFramework.Exceptions.MailboxFullException)
{
    // Back off: the Fail policy rejected the message.
}

var next = await mailboxes.DequeueAsync(actorId);

mailboxes.RemoveMailbox(actorId);
mailboxes.Clear();
```

`envelope` stands for an `Envelope` created by the caller.
