# MessageDispatcher

`MessageDispatcher` (`src/DotNetActorFramework/Services/MessageDispatcher.cs`) is the component that delivers `Envelope`s to actor mailboxes. It checks that the recipient is registered, enqueues the envelope with retries, routes replies to pending `Ask` calls, keeps a bounded dead letter queue for undeliverable envelopes, and keeps delivery counters.

It does not run actors. Actors pull their own messages back out with `GetNextMessageAsync`.

## Responsibilities

- **Delivery:** `DispatchAsync` is the single delivery path. All send helpers route through it.
- **Recipient check:** It rejects envelopes whose recipient is not in the `ActorRegistry`.
- **Retry with backoff:** Mailbox enqueue failures are retried with exponential backoff.
- **Ask correlation:** `ResponseMessage` and `FailureMessage` that answer a pending `Ask` are completed in `AskRegistry` and are not enqueued.
- **Dead letters:** Undeliverable envelopes go to the dispatcher's own bounded queue and to the owning `ActorSystem`'s dead letter handler.
- **Statistics:** It counts delivered and failed envelopes.
- **Typed replies:** `ReplyAsync` and `ReplyWithFailureAsync` correlate responses to a request by `MessageId`.

## Construction

```csharp
public MessageDispatcher(MailboxService mailboxService, ActorRegistry registry, ActorSystem actorSystem)
```

All three arguments are required; `null` throws `ArgumentNullException`.

A `protected` parameterless constructor also exists for derived types. It leaves all dependencies `null`, so it is not usable on its own.

## Public API

### `Task<bool> DispatchAsync(Envelope envelope)`

Delivers one envelope. Steps, in order:

1. Throws `ArgumentNullException` if `envelope` is `null`.
2. If the message is a `ResponseMessage` or `FailureMessage` that completes a pending `Ask`, marks the envelope delivered, counts it as delivered, and returns `true`.
3. If `envelope.Recipient.Path` is not in the registry, the envelope is dead-lettered and `MessageDispatchException` is thrown with the `ActorNotFoundException` as its inner exception.
4. Otherwise it calls `MailboxService.EnqueueAsync(recipient.Id, envelope)`. On `MailboxException` or any other exception, it increments the envelope's retry count, waits, and tries again, up to `ActorConstants.MaxMessageRetries` (3) retries. The wait starts at `InitialBackoffDelayMs` (100 ms) and grows by `BackoffMultiplier` (1.5) per retry, capped at `MaxBackoffDelayMs` (30 000 ms).
5. On success, marks the envelope delivered, counts it, and returns `true`.
6. If every attempt fails, the envelope is counted as failed, passed to the `ActorSystem` dead letter handler, added to the dispatcher's dead letter queue, and `false` is returned. No exception is thrown.

Unexpected errors in steps 1–5 that are not handled above are also dead-lettered and rethrown as `MessageDispatchException`.

| Outcome | Result |
|---|---|
| Ask reply consumed | `true` |
| Enqueued | `true` |
| Recipient not registered | `MessageDispatchException` (inner: `ActorNotFoundException`) |
| Enqueue failed after all retries | `false` |
| Other unexpected error | `MessageDispatchException` |

### `Task SendAsync(ActorRef sender, ActorRef recipient, Message message)`

Wraps `message` in an `Envelope` with the given sender and calls `DispatchAsync`. The result of `DispatchAsync` is discarded. Only the `MessageDispatchException` path surfaces to the caller.

### `Task SendAsync(ActorRef recipient, Message message)`

Same as above, with no sender.

### `Task BroadcastAsync(IEnumerable<ActorRef> recipients, Message message, ActorRef? sender = null)`

Creates one envelope per recipient and dispatches them concurrently with `Task.WhenAll`. All dispatches run to completion even if one fails; the first exception is then rethrown. Each dispatch result is discarded, as in `SendAsync`.

### `Task PublishControlAsync(ActorRef recipient, string command, Dictionary<string, object>? parameters = null)`

Sends a `ControlMessage` with the given command name and optional parameters (an empty dictionary when `null`). Throws `ArgumentException` if `command` is null, empty, or whitespace.

### `Task ReplyAsync<T>(ActorRef recipient, T response, Message request, ActorRef? sender = null) where T : class`

Sends `response` as a `ResponseMessage<T>` whose correlation ID is `request.MessageId`. Throws `ArgumentNullException` if `recipient`, `response`, or `request` is `null`.

### `Task ReplyWithFailureAsync(ActorRef recipient, string reason, Exception exception, Message request, ActorRef? sender = null)`

Sends a `FailureMessage` with `reason` and `exception`, correlated to `request.MessageId`. Throws `ArgumentNullException` if `recipient`, `exception`, or `request` is `null`, and `ArgumentException` if `reason` is null, empty, or whitespace.

### `Task<Envelope?> GetNextMessageAsync(Guid actorId)`

Returns the result of `MailboxService.DequeueAsync(actorId)`, which is the next envelope in that actor's mailbox. Throws `ArgumentException` if `actorId` is `Guid.Empty`.

### `IReadOnlyList<Envelope> GetDeadLetters()`

Returns a snapshot copy of the dispatcher's dead letter queue, oldest first.

### `DispatcherStatistics GetStatistics()`

Returns a snapshot of the counters:

| Property | Type | Meaning |
|---|---|---|
| `TotalDelivered` | `long` | Envelopes delivered, including Ask replies. |
| `TotalFailed` | `long` | Envelopes that were dead-lettered. |
| `TotalProcessed` | `long` | `TotalDelivered + TotalFailed`. |
| `DeadLetterCount` | `int` | Current size of the dead letter queue. |
| `SuccessRate` | `double` | Percentage from 0 to 100. `0` when nothing has been processed yet. |

## Usage

### Setting up and draining a mailbox

This follows the pattern in the project `README.md`. `WorkerActor` stands in for your own `Actor` subclass.

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

var system = new ActorSystem("demo");
var mailboxes = new MailboxService(new ActorSystemOptions());
var registry = new ActorRegistry();
var dispatcher = new MessageDispatcher(mailboxes, registry, system);

var worker = new WorkerActor(new ActorPath("/user/worker"));
await worker.InitializeAsync();
mailboxes.CreateMailbox(worker.Id);
registry.Register(worker.Ref);

await dispatcher.SendAsync(worker.Ref, new ControlMessage("ping"));
await dispatcher.PublishControlAsync(worker.Ref, "stop");

// Drain the mailbox; the actor processes messages one at a time
while (await dispatcher.GetNextMessageAsync(worker.Id) is { } envelope)
    await worker.ProcessMessageAsync(envelope.Message);
```

### Replying to a request and checking health

```csharp
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

// request arrived in the worker's mailbox; reply to its sender
await dispatcher.ReplyAsync(caller.Ref, "pong", request, sender: worker.Ref);

// report a failure instead of a result
await dispatcher.ReplyWithFailureAsync(
    caller.Ref,
    "Work item could not be processed",
    new InvalidOperationException("Queue is full"),
    request,
    sender: worker.Ref);

var stats = dispatcher.GetStatistics();
Console.WriteLine($"Delivered: {stats.TotalDelivered}, failed: {stats.TotalFailed}");
Console.WriteLine($"Success rate: {stats.SuccessRate:F1}%");

foreach (var dead in dispatcher.GetDeadLetters())
    Console.WriteLine($"Dead letter for {dead.Recipient.Path}: {dead.Message.GetType().Name}");
```

## Notes

- **Failures are silent through `SendAsync` and `BroadcastAsync`.** An enqueue that fails after all retries returns `false` to `DispatchAsync`, but the send helpers discard that value. To detect these failures, call `DispatchAsync` directly, or check `GetStatistics()` and `GetDeadLetters()`.
- **Two dead letter stores.** `MessageDispatcher` keeps its own queue, capped at `MessageConstants.MaxDeadLetterQueueSize` (10 000). When full, the oldest entry is dropped. Each dead letter is also added to `ActorSystem.DeadLetters` and passed to the handler set with `ActorSystem.SetDeadLetterHandler`. The two stores are independent.
- **Retry timing.** A send that fails every attempt waits roughly 100 + 150 + 225 ms (about 0.5 s) before it is dead-lettered. Backoff is bounded by the 30 s cap.
- **Ask replies skip the mailbox.** A `ResponseMessage` or `FailureMessage` whose `MessageId` matches a pending `Ask` completes that `Ask` and is never enqueued, so the recipient's mailbox does not see it.
- **Thread safety.** The counters and the dead letter queue are guarded by a lock. Dispatch itself is not serialized, so concurrent calls are allowed.
- **Dependencies are not validated after construction.** Only the constructor checks for `null`. A `MessageDispatcher` created through the `protected` constructor will throw `NullReferenceException` if used.
