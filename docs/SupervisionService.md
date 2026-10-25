# SupervisionService

`SupervisionService` applies a `SupervisionStrategy` to an actor that has failed. It sends the matching control message through the `MessageDispatcher`, escalates to the parent when needed, and keeps per-actor failure and restart counters that you can read back.

`SupervisionService` lives in the `DotNetActorFramework.Services` namespace. The same file also defines `SupervisionContext` (per-actor counters) and `SupervisionStatistics` (aggregate counters). `DependencyInjectionSetup.AddActorFramework` registers the service as a singleton.

The framework does not call `HandleFailureAsync` for you. Nothing in `src/` invokes it, so your code (or a custom supervisor actor) must decide when a failure occurs and which strategy to apply. Supervision settings in `ActorSystemOptions`, such as `DefaultSupervisionStrategy` and the backoff options, are not read by this service.

## Supervision strategies

`HandleFailureAsync` takes one of the five values of the `SupervisionStrategy` enum:

| Strategy | What happens | Counters changed |
|----------|--------------|------------------|
| `Restart` | Increments `RestartCount` and sends a `ControlMessage` with the `restart` command. If `RestartCount` is greater than 5, the failure escalates instead, so the sixth restart request escalates. | `RestartCount` |
| `Stop` | Sends a `ControlMessage` with the `stop` command. The message carries the exception text in the `failure_reason` parameter. | None |
| `Resume` | Sends a `ControlMessage` with the `resume` command. The failure is otherwise ignored. | None |
| `Escalate` | If the actor has a parent (`ActorRef.GetParent()`), sends a `FailureMessage` to the parent with the text `Actor {Path} failed` and the exception. If there is no parent, falls back to `Stop`. | None |
| `Backoff` | Increments `FailureCount`, waits for an exponential delay (see below), then sends a `ControlMessage` with the `resume` command. The wait happens inside `HandleFailureAsync`, so the caller is blocked until it finishes. | `FailureCount` |

### Backoff delay

The delay for failure number *n* is `(int)Math.Pow(1.5, n) * 100` milliseconds, capped at 30,000 ms. The integer cast happens before the multiplication, so the values are not exactly 1.5ⁿ × 100. The first delays are:

| Failure *n* | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | … | 14 | 15 and later |
|-------------|---|---|---|---|---|---|---|---|---|----|--------------|
| Delay (ms)  | 100 | 200 | 300 | 500 | 700 | 1100 | 1700 | 2500 | … | 29100 | 30000 |

The service reads these values from `ActorConstants` (`InitialBackoffDelayMs = 100`, `BackoffMultiplier = 1.5`, `MaxBackoffDelayMs = 30000`), not from `ActorSystemOptions`.

## API

### `SupervisionService` constructor

```csharp
public SupervisionService(ActorRegistry registry, MessageDispatcher dispatcher)
```

Creates a service that looks up actors through `registry` and delivers control and failure messages through `dispatcher`.

- **Parameters**
  - `registry`: The actor registry. Must not be `null`.
  - `dispatcher`: The message dispatcher. Must not be `null`.
- **Throws**
  - `ArgumentNullException`: If `registry` or `dispatcher` is `null`.

A `protected` parameterless constructor also exists for subclasses. It leaves `registry` and `dispatcher` set to `null`, so the subclass must not use the inherited members that depend on them.

### `Task HandleFailureAsync(ActorRef actor, Exception exception, SupervisionStrategy strategy)`

Applies `strategy` to a failure of `actor`. See [Supervision strategies](#supervision-strategies) for the behavior of each value.

- **Parameters**
  - `actor`: The failed actor. Must not be `null`.
  - `exception`: The exception that caused the failure. Must not be `null`.
  - `strategy`: The strategy to apply.
- **Returns**
  - A `Task` that completes when the strategy's work is finished. For `Backoff`, this includes the delay.
- **Throws**
  - `ArgumentNullException`: If `actor` or `exception` is `null`.
  - `ArgumentException`: If `strategy` is not a defined `SupervisionStrategy` value.
  - `MessageDispatchException`: If the dispatcher cannot deliver a message, for example when the actor has no mailbox.
- **Notes**
  - The actor's supervision context is created on entry, before the strategy is checked. An invalid strategy still leaves a context behind.
  - `Stop`, `Resume`, and `Escalate` do not change `FailureCount`. Only `Backoff` increments it, so `TotalFailures` in the statistics counts only backoff failures.

### `SupervisionContext? GetContext(Guid actorId)`

Returns the supervision context for an actor.

- **Parameters**
  - `actorId`: The actor's `Id`.
- **Returns**
  - The context, or `null` if the actor has never been supervised.

### `Dictionary<Guid, SupervisionContext> GetAllContexts()`

Returns a copy of all supervision contexts, keyed by actor ID. Changes to the returned dictionary do not affect the service. The context objects are shared, so changes to their counters are visible to the service.

### `SupervisionStatistics GetStatistics()`

Returns aggregate counters across all contexts.

- **Returns**
  - A `SupervisionStatistics` with `TotalActorsSupervised` (number of contexts), `TotalFailures` (sum of `FailureCount`), `TotalRestarts` (sum of `RestartCount`), and `AverageFailuresPerActor` (`TotalFailures` divided by the number of contexts, or `0` when there are none).

### `void ResetContext(Guid actorId)`

Resets the counters of one actor's context by calling `SupervisionContext.ResetFailures()`. Does nothing if the actor has no context.

- **Notes**
  - Contexts are never removed. `ResetContext` clears the counters but keeps the entry, so `TotalActorsSupervised` does not decrease.
  - `Restart` counts `RestartCount` cumulatively. Without a reset, an actor that has been restarted six times escalates on every later restart request, so call `ResetContext` after recovery if that is not what you want.

## `SupervisionContext`

Per-actor counters. The service creates these; you read them through `GetContext` or `GetAllContexts`.

| Member | Type | Description |
|--------|------|-------------|
| `ActorId` | `Guid` | The supervised actor's ID. Set by the constructor. |
| `FailureCount` | `int` | Backoff failures recorded for the actor. Incremented only by `Backoff`. |
| `RestartCount` | `int` | Restarts requested for the actor. Incremented only by `Restart`. |
| `LastFailureTime` | `DateTime` | UTC time at which the context was created. The service never updates it when a failure occurs. |

- `public SupervisionContext(Guid actorId)`: Creates a context with zero counters and `LastFailureTime` set to `DateTime.UtcNow`.
- `public void ResetFailures()`: Sets `FailureCount` and `RestartCount` to zero. `LastFailureTime` is not changed.
- `public TimeSpan GetTimeSinceLastFailure()`: Returns `DateTime.UtcNow - LastFailureTime`. Because `LastFailureTime` is set only at construction, this is the time since the context was created, not since the last failure.

## `SupervisionStatistics`

Aggregate counters returned by `GetStatistics()`.

| Property | Type | Description |
|----------|------|-------------|
| `TotalActorsSupervised` | `int` | Number of supervision contexts. |
| `TotalFailures` | `long` | Sum of `FailureCount` across contexts. |
| `TotalRestarts` | `long` | Sum of `RestartCount` across contexts. |
| `AverageFailuresPerActor` | `double` | `TotalFailures` divided by `TotalActorsSupervised`, or `0` when there are none. |

## Thread safety

Reads and writes of the context dictionary are serialized on an internal lock, so `GetContext`, `GetAllContexts`, `GetStatistics`, and `ResetContext` are safe to call concurrently. Counter updates in `HandleFailureAsync` (`RestartCount++`, `FailureCount++`) happen outside that lock. Two failures of the same actor handled at the same moment can therefore lose an increment.

## Usage example

The example wires the service by hand, because `AddActorFramework` does not register `ActorSystem`, which the dispatcher needs. It was compiled and run against this repository.

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

var options = ActorSystemOptions.CreateDefault();
var registry = new ActorRegistry();
var mailboxes = new MailboxService(options);
var system = new ActorSystem("supervision-demo", options: options);
var dispatcher = new MessageDispatcher(mailboxes, registry, system);
var supervision = new SupervisionService(registry, dispatcher);

// The actor needs an initialized state and a mailbox before it can receive control messages.
var worker = new WorkerActor(ActorPath.Parse("/app/worker"));
await worker.InitializeAsync();
mailboxes.CreateMailbox(worker.Id, options.DefaultMailboxCapacity);
registry.Register(worker.Ref);

var error = new InvalidOperationException("database timeout");

// Sends the "restart" control message and increments RestartCount.
await supervision.HandleFailureAsync(worker.Ref, error, SupervisionStrategy.Restart);

// Waits about 100 ms, increments FailureCount, then sends the "resume" control message.
await supervision.HandleFailureAsync(worker.Ref, error, SupervisionStrategy.Backoff);

var context = supervision.GetContext(worker.Id);
Console.WriteLine($"Restarts: {context?.RestartCount}, failures: {context?.FailureCount}");
// Output: Restarts: 1, failures: 1

var stats = supervision.GetStatistics();
Console.WriteLine($"Supervised: {stats.TotalActorsSupervised}, failures: {stats.TotalFailures}, restarts: {stats.TotalRestarts}");
// Output: Supervised: 1, failures: 1, restarts: 1

supervision.ResetContext(worker.Id);

public class WorkerActor : Actor
{
    public WorkerActor(ActorPath path) : base(path) { }

    protected override Task OnReceiveAsync(Message message) => Task.CompletedTask;
}
```

## See also

- [SupervisionServiceExtensions](SupervisionServiceExtensions.md): Helper methods built on `GetContext` and `GetAllContexts`.
- [ActorRegistry](ActorRegistry.md): Source of the `ActorRef` lookups used for parent resolution.
- [supervision-guide](supervision-guide.md): Higher-level guide to supervision in the framework.
