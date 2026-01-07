# Actor

The `Actor` type represents the core execution unit in the dotnet‑actor‑framework. It encapsulates an identity, reference, path, lifecycle state, metrics, and supervision relationship, providing methods to initialize, process messages, manage state, and terminate the actor instance.

## API

### Id
- **Type:** `Guid`
- **Purpose:** Unique identifier for the actor instance, assigned at creation and immutable for the lifetime of the actor.
- **Throws:** None.

### Ref
- **Type:** `ActorRef`
- **Purpose:** Reference that other actors use to send messages to this actor. Guarantees thread‑safe message delivery.
- **Throws:** None.

### Path
- **Type:** `ActorPath`
- **Purpose:** Hierarchical path that identifies the actor’s location within the actor system (e.g., `/user/service/worker`).
- **Throws:** None.

### State
- **Type:** `ActorState`
- **Purpose:** Current logical state of the actor. The type `ActorState` is defined by the framework and can be inspected or replaced via `GetState`, `SetState`, and `HasState`.
- **Throws:** None.

### Metrics
- **Type:** `ActorMetrics`
- **Purpose:** Collection of runtime metrics (e.g., message count, processing time) gathered automatically by the framework.
- **Throws:** None.

### CreatedAt
- **Type:** `DateTime`
- **Purpose:** Timestamp indicating when the actor instance was instantiated.
- **Throws:** None.

### TerminatedAt
- **Type:** `DateTime?`
- **Purpose:** Timestamp indicating when the actor finished termination; `null` while the actor is alive.
- **Throws:** None.

### Supervisor
- **Type:** `ActorRef?`
- **Purpose:** Reference to the actor’s supervisor, responsible for lifecycle management; `null` for top‑level actors under the system guardian.
- **Throws:** None.

### Actor
- **Type:** `Actor`
- **Purpose:** Self‑reference property that returns the current actor instance; useful for internal callbacks or when an actor needs to pass itself as a message.
- **Parameters:** None.
- **Return:** The actor instance itself.
- **Throws:** None.

### InitializeAsync
- **Signature:** `public async Task InitializeAsync`
- **Purpose:** Called by the actor system after the actor is constructed but before it begins processing messages. Implementations should perform any required setup (e.g., opening resources, subscribing to events).
- **Parameters:** None.
- **Return:** A `Task` that completes when initialization finishes.
- **Throws:** May throw an exception if initialization fails; the exception is propagated to the supervisor’s supervision strategy.

### ProcessMessageAsync
- **Signature:** `public async Task ProcessMessageAsync`
- **Purpose:** Processes the next message from the actor’s mailbox. The concrete message type is handled by the derived actor’s implementation.
- **Parameters:** None.
- **Return:** A `Task` that completes when message processing finishes.
- **Throws:** May throw an exception if processing fails; the exception is handled according to the actor’s supervision strategy.

### SetState
- **Signature:** `public void SetState`
- **Purpose:** Replaces the actor’s current state with a new value supplied by the caller (the mechanism for providing the new value is encapsulated within the method’s implementation).
- **Parameters:** None.
- **Return:** `void`.
- **Throws:** May throw an `InvalidOperationException` if the new throw an `ArgumentException` if the supplied state is invalid for the actor’s type.

### GetState
- **Signature:** `public object? GetState`
- **Purpose:** Retrieves the current state object; returns `null` if no state has been set.
- **Parameters:** None.
- **Return:** The current state as an `object`, or `null`.
- **Throws:** None.

### HasState
- **Signature:** `public bool HasState`
- **Purpose:** Indicates whether the actor currently holds a non‑null state.
- **Parameters:** None.
- **Return:** `true` if state is set; otherwise `false`.
- **Throws:** None.

### TerminateAsync
- **Signature:** `public async Task TerminateAsync`
- **Purpose:** Initiates graceful shutdown of the actor. The method waits for any in‑flight message processing to complete, releases resources, and notifies the supervisor.
- **Parameters:** None.
- **Return:** A `Task` that completes when termination is finished.
- **Throws:** May throw an exception if termination encounters an error (e.g., resource cleanup failure).

### GetMetricsSummary
- **Signature:** `public ActorMetricsSummary GetMetricsSummary`
- **Purpose:** Returns a snapshot of the actor’s metrics suitable for monitoring or logging.
- **Parameters:** None.
- **Return:** An `ActorMetricsSummary` instance containing aggregated metric values.
- **Throws:** None.

### ToString
- **Signature:** `public override string ToString`
- **Purpose:** Provides a human‑readable representation of the actor, typically including its `Id`, `Path`, and current `State`.
- **Parameters:** None.
- **Return:** A `string` describing the actor.
- **Throws:** None.

## Usage

### Example 1: Creating and starting an actor
```csharp
using DotNetActorFramework;

// Assume MyActor inherits from Actor and overrides InitializeAsync/ProcessMessageAsync
var system = ActorSystem.Create("my-system");
var props = Props.Create<MyActor>();
var actorRef = system.ActorOf(props, "worker");

// The actor's Id, Ref, Path, and CreatedAt are now populated
Console.WriteLine($"Actor Id: {actorRef.Ref.Id}");
Console.WriteLine($"Actor Path: {actorRef.Ref.Path}");
Console.WriteLine($"Created at: {actorRef.Ref.CreatedAt}");
```

### Example 2: Managing state and terminating an actor
```csharp
using DotNetActorFramework;

public class CounterActor : Actor
{
    private int _count = 0;

    protected override Task InitializeAsync()
    {
        // Initialize state to zero
        SetState(0);
        return Task.CompletedTask;
    }

    protected override Task ProcessMessageAsync()
    {
        // Increment state for each message
        var current = (int?)GetState() ?? 0;
        SetState(current + 1);
        return Task.CompletedTask;
    }
}

// Usage
var system = ActorSystem.Create("demo");
var counterRef = system.ActorOf<CounterActor>("counter");

// Send a few messages (framework‑specific send omitted for brevity)
// ...

// Retrieve and display state
Console.WriteLine($"Current count: {counterRef.GetState()}");

// Gracefully stop the actor
await counterRef.TerminateAsync();
Console.WriteLine($"Terminated at: {counterRef.TerminatedAt}");
```

## Notes
- The fields `Id`, `Ref`, `Path`, `CreatedAt`, `TerminatedAt`, and `Supervisor` are effectively immutable after construction; only `TerminatedAt` may change from `null` to a timestamp upon termination.
- State‑related methods (`SetState`, `GetState`, `HasState`) are not thread‑safe by themselves; concurrent calls from multiple threads should be synchronized externally if the actor’s implementation permits parallel access.
- `InitializeAsync` and `ProcessMessageAsync` are intended to be overridden by concrete actor subclasses; calling them directly on the base `Actor` type will execute the default (no‑op) behavior.
- `TerminateAsync` should be called only once; subsequent calls may return a completed task without effect but could throw if resources have already been disposed.
- The `Actor` self‑reference property returns the exact instance; it can be safely used for comparison or as a message payload without risk of creating a reference cycle, as the framework treats it as a weak reference for supervision purposes.
