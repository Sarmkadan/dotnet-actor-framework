# ActorRef
An `ActorRef` is a lightweight handle to an actor instance within the `dotnet-actor-framework`. It provides the essential operations needed to interact with an actor—sending messages, requesting replies, and inspecting identity and lifecycle state—without exposing the actor’s internal implementation.

## API
### Path
- **Purpose:** Returns the hierarchical address of the actor within the actor system.
- **Return value:** An `ActorPath` instance representing the actor’s location.
- **Throws:** None; the property is read-only and always returns a valid path.

### Id
- **Purpose:** Provides a unique identifier for the actor instance.
- **Return value:** A `Guid` that distinguishes this actor from all others, even after restart.
- **Throws:** None.

### IsAlive
- **Purpose:** Indicates whether the actor is currently able to process messages.
- **Return value:** `true` if the actor has been started and not yet stopped; otherwise `false`.
- **Throws:** None; the value may change concurrently as the actor’s lifecycle progresses.

### CreatedAt
- **Purpose:** Retrieves the timestamp when the actor reference was first created.
- **Return value:** A `DateTime` (UTC) marking the creation moment.
- **Throws:** None; the value is immutable after construction.

### SendAsync
- **Purpose:** Asynchronously sends a message to the referenced actor.
- **Return value:** A `Task` that completes when the message has been enqueued for processing.
- **Throws:** 
  - `ObjectDisposedException` if the actor has been stopped.
  - `InvalidOperationException` if the actor system is shutting down.

### AskAsync
- **Purpose:** Asynchronously sends a message to the referenced actor and awaits a reply.
- **Return value:** A `Task<object?>` that completes with the actor’s response (or `null` if no response is defined).
- **Throws:** 
  - `ObjectDisposedException` if the actor has been stopped.
  - `TimeoutException` if the reply does not arrive within the configured timeout.
  - `InvalidOperationException` if the actor system is unavailable.

### GetParent
- **Purpose:** Retrieves the reference to the actor’s parent in the supervision hierarchy.
- **Return value:** An `ActorRef` representing the parent, or `null` if the actor has no parent (e.g., a root guardian).
- **Throws:** None.

### ToString
- **Purpose:** Provides a human‑readable representation of the actor reference.
- **Return value:** A string typically containing the actor’s path and identifier.
- **Throws:** None.

### Equals (object?)
- **Purpose:** Determines whether the specified object is another `ActorRef`ActorRef` same actor.
- **Parameters:** `object? obj` – the object to compare with.
- **Return value:** `true` if `obj` is an `ActorRef` with the same `Id` and `Path`; otherwise `false`.
- **Throws:** None.

### Equals (ActorRef)
- **Purpose:** Determines whether the specified `ActorRef` represents the same actor.
- **Parameters:** `ActorRef other` – the reference to compare with.
- **Return value:** `true` if `other` has the same `Id` and `Path`; otherwise `false`.
- **Throws:** None.

### GetHashCode
- **Purpose:** Serves as the default hash function for use in hash‑based collections.
- **Return value:** An integer hash code derived from the actor’s `Id`.
- **Throws:** None.

## Usage
```csharp
using DotNetActorFramework;

// Assume `system` is an initialized ActorSystem and `greeterRef` is an ActorRef to a Greeter actor.
ActorRef greeterRef to an actor.
ActorRef greeterRef = system.ActorOf<Greeter>("greeterRef isreaterRef.IsAlive; // true

// Send a fire‑and‑forget message
await greeterRef.SendAsync(new Greet("Alice"));

// Request a reply
object? reply = await greeterRef.AskAsync(new GetGreeting());
// reply might be a string like "Hello, Alice"

// Inspect identity
Console.WriteLine($"Actor Id: {greeterRef.Id}");
Console.WriteLine($"Created at: {greeterRef.CreatedAt:O}");

// Obtain parent (may be null for top‑level actors)
ActorRef? parent = greeterRef.GetParent;
if (parent != null)
{
    Console.WriteLine($"Parent path: {parent.Path}");
}
```

```csharp
using System;
using System.Threading.Tasks;
using DotNetActorFramework;

public class PingActor : Actor
{
    protected override Task OnReceiveAsync(object message)
    {
        if (message is Ping ping)
        {
            Sender.Tell(new Pong(ping.CorrelationId));
        }
        return Task.CompletedTask;
    }
}

// Somewhere in an actor system
ActorRef pingRef pingRef = system.ActorOf<PingActor>("ping");

// Check liveness before interacting
if (pingRef.IsAlive)
{
    var ping = new Ping(Guid.NewGuid());
    Task<Pong?> askTask = pingRef.AskAsync<Pong>(ping);
    Pong? pong = await askTask;

    if (pong != null)
    {
        Console.WriteLine($"Received Pong for correlation {pong.CorrelationId}");
    }
}
else
{
    Console.WriteLine("Ping actor is not alive.");
}
```

## Notes
- The `Path` and `Id` properties are immutable for the lifetime of the reference; they can be safely cached.
- `IsAlive` may change from `true` to `false` concurrently with calls to `SendAsync` or `AskAsync`; after an actor stops, subsequent invocations will throw `ObjectDisposedException`.
- `SendAsync` does not guarantee message delivery; it only guarantees that the message has been placed in the actor’s mailbox. Reliability semantics depend on the underlying dispatcher.
- `AskAsync` captures the original sender internally; the caller must await the returned task to obtain the response. Failure to await may result in unobserved exceptions.
- `GetParent` returns `null` for actors created directly under the system guardian; the parent reference itself is subject to the same lifecycle rules as any other `ActorRef`.
- Equality and hash‑code implementations are based solely on `Id` (and implicitly `Path`), making `ActorRef` suitable for use as a key in dictionaries or hash sets.
- All instance members are thread‑safe for concurrent reads; mutable state (`IsAlive`) is updated internally with appropriate synchronization, so callers do not need external locks when accessing these members from multiple threads.
