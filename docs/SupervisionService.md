# SupervisionService

`SupervisionService` is the core runtime component responsible for monitoring, managing, and recovering from failures in actors within the `dotnet-actor-framework`. It tracks failure and restart counts per supervised actor, maintains aggregate statistics across all supervised actors, and provides the mechanism to handle failures asynchronously with configurable reset behavior.

## API

### SupervisionService

```csharp
public SupervisionService(Guid actorId)
```

Constructs a new supervision service instance bound to a specific actor. The service initializes all failure counters, restart counters, and aggregate statistics to zero. The `actorId` parameter identifies the actor this service will supervise and cannot be changed after construction.

- **Parameters**: `actorId` — the unique identifier of the actor to supervise.
- **Exceptions**: None.

---

### HandleFailureAsync

```csharp
public async Task HandleFailureAsync(Exception exception, SupervisionContext context)
```

Processes a failure reported by the supervised actor. This method increments the failure count for the actor, updates the `LastFailureTime`, and increments the total failure aggregate. Depending on the `SupervisionContext` directive, it may trigger an actor restart (incrementing `RestartCount` and `TotalRestarts`) or escalate the failure. The method executes asynchronously to allow restart strategies that involve delays or external coordination.

- **Parameters**:
  - `exception` — the exception that caused the failure.
  - `context` — the `SupervisionContext` containing the directive (e.g., restart, stop, escalate) and any associated state.
- **Returns**: A `Task` representing the asynchronous handling operation.
- **Exceptions**: May throw `ArgumentNullException` if `exception` or `context` is `null`.

---

### ResetContext

```csharp
public void ResetContext()
```

Resets the current `SupervisionContext` to its default state. This is typically invoked after a failure has been fully handled and the actor has resumed normal operation, ensuring that stale directives do not affect subsequent failure handling.

- **Exceptions**: None.

---

### GetStatistics

```csharp
public SupervisionStatistics GetStatistics()
```

Returns a snapshot of the current supervision statistics for the bound actor and the global aggregates. The returned `SupervisionStatistics` object includes per-actor counts (`FailureCount`, `RestartCount`, `LastFailureTime`) and global aggregates (`TotalActorsSupervised`, `TotalFailures`, `TotalRestarts`, `AverageFailuresPerActor`).

- **Returns**: A populated `SupervisionStatistics` instance.
- **Exceptions**: None.

---

### ActorId

```csharp
public Guid ActorId { get; }
```

Gets the unique identifier of the actor this supervision service is bound to. This value is set at construction and remains immutable for the lifetime of the service.

---

### FailureCount

```csharp
public int FailureCount { get; }
```

Gets the number of failures recorded for the bound actor since the last reset or construction. Incremented by `HandleFailureAsync`.

---

### RestartCount

```csharp
public int RestartCount { get; }
```

Gets the number of times the bound actor has been restarted as a result of supervision decisions. Incremented when `HandleFailureAsync` applies a restart directive.

---

### LastFailureTime

```csharp
public DateTime LastFailureTime { get; }
```

Gets the timestamp of the most recent failure handled by this service. Returns `DateTime.MinValue` if no failure has been recorded.

---

### SupervisionContext

```csharp
public SupervisionContext SupervisionContext { get; }
```

Gets the current supervision context, which holds the directive and any metadata for the next failure handling cycle. This context can be modified by external supervision strategies and is reset by `ResetContext`.

---

### ResetFailures

```csharp
public void ResetFailures()
```

Resets the per-actor `FailureCount` and `RestartCount` to zero, and sets `LastFailureTime` to `DateTime.MinValue`. Global aggregate counters (`TotalFailures`, `TotalRestarts`, `TotalActorsSupervised`) are not affected by this method.

- **Exceptions**: None.

---

### GetTimeSinceLastFailure

```csharp
public TimeSpan GetTimeSinceLastFailure()
```

Calculates the elapsed time since the last recorded failure. If no failure has been recorded (`LastFailureTime` is `DateTime.MinValue`), the returned `TimeSpan` represents the time since `DateTime.MinValue`, which callers should interpret as "no failure recorded."

- **Returns**: A `TimeSpan` representing the duration since `LastFailureTime`.
- **Exceptions**: None.

---

### TotalActorsSupervised

```csharp
public int TotalActorsSupervised { get; }
```

Gets the total number of actors that have been supervised across all instances of `SupervisionService` in the current process. This is a static or shared aggregate counter.

---

### TotalFailures

```csharp
public long TotalFailures { get; }
```

Gets the total number of failures recorded across all supervised actors. Incremented each time any `SupervisionService` instance processes a failure via `HandleFailureAsync`.

---

### TotalRestarts

```csharp
public long TotalRestarts { get; }
```

Gets the total number of actor restarts triggered across all supervised actors. Incremented when any `HandleFailureAsync` invocation results in a restart directive.

---

### AverageFailuresPerActor

```csharp
public double AverageFailuresPerActor { get; }
```

Gets the average number of failures per supervised actor, calculated as `TotalFailures / TotalActorsSupervised`. Returns `0.0` if `TotalActorsSupervised` is zero.

## Usage

### Example 1: Basic Supervision with Restart

```csharp
var service = new SupervisionService(actor.Id);

try
{
    await actor.ProcessMessageAsync(message);
}
catch (Exception ex)
{
    var context = new SupervisionContext
    {
        Directive = SupervisionDirective.Restart,
        Reason = ex.Message
    };

    await service.HandleFailureAsync(ex, context);

    // After handling, reset context for next cycle
    service.ResetContext();

    Console.WriteLine($"Actor {service.ActorId} failures: {service.FailureCount}, restarts: {service.RestartCount}");
}
```

### Example 2: Monitoring and Statistics Reporting

```csharp
var service = new SupervisionService(actor.Id);

// Simulate failures over time
for (int i = 0; i < 3; i++)
{
    try { throw new InvalidOperationException("Simulated failure"); }
    catch (Exception ex)
    {
        await service.HandleFailureAsync(ex, new SupervisionContext
        {
            Directive = SupervisionDirective.Restart
        });
    }
}

var stats = service.GetStatistics();
var timeSinceLast = service.GetTimeSinceLastFailure();

Console.WriteLine($"Per-actor failures: {stats.FailureCount}");
Console.WriteLine($"Global total failures: {stats.TotalFailures}");
Console.WriteLine($"Average failures per actor: {stats.AverageFailuresPerActor:F2}");
Console.WriteLine($"Time since last failure: {timeSinceLast.TotalSeconds:F1}s");

// Reset per-actor counters after a successful recovery window
if (timeSinceLast.TotalMinutes > 5)
{
    service.ResetFailures();
}
```

## Notes

- **Thread Safety**: `HandleFailureAsync`, `ResetFailures`, and `ResetContext` are designed to be safe for concurrent access from the actor’s message-processing pipeline. The aggregate counters (`TotalFailures`, `TotalRestarts`, `TotalActorsSupervised`) rely on atomic operations or locking to ensure consistency across multiple `SupervisionService` instances. `GetStatistics` returns a point-in-time snapshot and may reflect values that change immediately after the call.
- **Edge Cases**:
  - Calling `ResetFailures` does not affect global aggregates; per-actor and global counters can diverge after a reset.
  - `GetTimeSinceLastFailure` returns a large `TimeSpan` when no failure has occurred (`LastFailureTime` is `DateTime.MinValue`). Callers should check `FailureCount > 0` before interpreting this value as meaningful.
  - `AverageFailuresPerActor` returns `0.0` when `TotalActorsSupervised` is zero, avoiding division-by-zero exceptions.
  - `HandleFailureAsync` with a `SupervisionContext` carrying a `Stop` or `Escalate` directive does not increment `RestartCount` or `TotalRestarts`.
- **Lifecycle**: The service is intended to be long-lived and bound to a single actor. Reusing a `SupervisionService` instance across multiple actors is not supported and will produce misleading per-actor statistics.
