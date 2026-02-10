# ActorSystemConfigurationExtensions

Provides extension methods for configuring and interacting with an `ActorSystem`. These static helpers simplify common asynchronous operations such as creating actors, sending messages, and querying the system’s health status without requiring direct access to lower‑level APIs.

## API

### `public static async Task<ActorRef> CreateActorAsync(...)`
**Purpose**  
Creates a new actor within the supplied `ActorSystem` and returns a reference to it.

**Parameters**  
- `system`: The `ActorSystem` in which the actor will be instantiated.  
- `props`: Configuration (`Props`) describing the actor’s behavior, mailbox, and supervision strategy.  
- `name` (optional): A human‑readable name for the actor; if omitted the system generates a unique identifier.  
- `cancellationToken` (optional): Allows the operation to be cancelled.

**Return Value**  
A `Task<ActorRef>` that completes with the reference to the newly created actor.

**Exceptions**  
- `ArgumentNullException` if `system` or `props` is `null`.  
- `OperationCanceledException` if the supplied `cancellationToken` is triggered before completion.  
- Any exception thrown by the underlying actor creation logic (e.g., invalid props) is propagated through the returned task.

### `public static async Task SendMessageAsync(...)`
**Purpose**  
Asynchronously sends a message to a target actor.

**Parameters**  
- `target`: The `ActorRef` of the destination actor.  
- `message`: The object to be delivered; must be serializable if the actor system uses remote transport.  
- `cancellationToken` (optional): Allows the send operation to be cancelled.

**Return Value**  
A `Task` that completes when the message has been enqueued for delivery. Note that completion does not guarantee the message has been processed.

**Exceptions**  
- `ArgumentNullException` if `target` or `message` is `null`.  
- `OperationCanceledException` if the `cancellationToken` is triggered.  
- Any exception from the underlying message‑send mechanism (e.g., association failures) is propagated via the returned task.

### `public static string GetHealthReport(...)`
**Purpose**  
Produces a textual report describing the current health of the actor system.

**Parameters**  
- `system`: The `ActorSystem` to inspect.

**Return Value**  
A multi‑line string containing details such as live actor count, mailbox sizes, and any detected anomalies.

**Exceptions**  
- `ArgumentNullException` if `system` is `null`.  
- The method does not throw for transient internal errors; instead, such conditions are reflected in the returned report.

### `public static bool IsHealthy(...)`
**Purpose**  
Determines whether the actor system is operating within healthy parameters.

**Parameters**  
- `system`: The `ActorSystem` to evaluate.

**Return Value**  
`true` if the system meets health criteria (e.g., no deadlocked mailboxes, acceptable message throughput); otherwise `false`.

**Exceptions**  
- `ArgumentNullException` if `system` is `null`.  
- No other exceptions are thrown; the method returns `false` when health checks cannot be performed.

## Usage

### Creating an actor and sending a message
```csharp
using DotNetActorFramework;

// Assume 'system' is a previously initialized ActorSystem
var props = Props.Create<MyActor>();
var actorRef = await ActorSystemConfigurationExtensions.CreateActorAsync(system, props, name: "my-actor");

var greeting = new GreetingMessage { Text = "Hello" };
await ActorSystemConfigurationExtensions.SendMessageAsync(actorRef, greeting);
```

### Checking system health
```csharp
using DotNetActorFramework;

string report = ActorSystemConfigurationExtensions.GetHealthReport(system);
Console.WriteLine(report);

bool healthy = ActorSystemConfigurationExtensions.IsHealthy(system);
if (!healthy)
{
    // Trigger alerts or fallback logic
    Logger.Warn("Actor system health degraded.");
}
```

## Notes
- All extension methods are **static** and therefore safe to invoke from any thread without additional synchronization, provided the supplied `ActorSystem` instance itself is thread‑safe (the framework guarantees this).  
- Passing `null` for required arguments (`system`, `props`, `target`, or `message`) will always result in an `ArgumentNullException`; callers should validate inputs beforehand if they wish to handle such cases gracefully.  
- The asynchronous methods respect the supplied `CancellationToken`; if cancellation is requested before the operation completes, the returned task will be faulted with `OperationCanceledException`.  
- `SendMessageAsync` only guarantees that the message has been placed onto the actor’s mailbox; it does **not** await processing. For request‑response patterns, consider using the built‑in ask pattern instead of relying solely on this method.  
- Health‑related methods (`GetHealthReport` and `IsHealthy`) are lightweight snapshots; they do not block or alter system state. Frequent invocation is permissible, but excessive polling may add negligible overhead.  
- In scenarios where the actor system is shutting down, these methods may throw or return unhealthy statuses; callers should observe the system’s lifecycle events and avoid invoking extensions after `ActorSystem.Terminate()` has been completed.
