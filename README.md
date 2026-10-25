## Getting Started

Create a console app and add the package (targets .NET 10):

```bash
dotnet new console -n Greeting
cd Greeting
dotnet add package Zaiets.dotnet.actor.framework --version 1.0.0
```

Replace `Program.cs` with the following. It defines a `Greeter` actor that prints a greeting for each `Greet` message, sends it three messages, and shuts the system down:

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

public sealed record Greet(string Name) : Message;

public class Greeter(ActorPath path) : Actor(path)
{
    protected override Task OnReceiveAsync(Message message)
    {
        if (message is Greet greet)
            Console.WriteLine($"Hello, {greet.Name}!");
        return Task.CompletedTask;
    }
}

public static class Program
{
    public static async Task Main()
    {
        var system = new ActorSystem("demo");
        var mailboxes = new MailboxService(new ActorSystemOptions());
        var registry = new ActorRegistry();
        var dispatcher = new MessageDispatcher(mailboxes, registry, system);

        var greeter = new Greeter(new ActorPath("/user/greeter"));
        await greeter.InitializeAsync();
        mailboxes.CreateMailbox(greeter.Id);
        registry.Register(greeter.Ref);

        await dispatcher.SendAsync(greeter.Ref, new Greet("Alice"));
        await dispatcher.SendAsync(greeter.Ref, new Greet("Bob"));
        await dispatcher.SendAsync(greeter.Ref, new Greet("Carol"));

        // Messages are delivered to the mailbox; the actor handles them one at a time
        while (await dispatcher.GetNextMessageAsync(greeter.Id) is { } envelope)
            await greeter.ProcessMessageAsync(envelope.Message);

        await greeter.TerminateAsync();
        await system.ShutdownAsync();
        Console.WriteLine("System shut down.");
    }
}
```

Run it with `dotnet run`. Expected output:

```text
Hello, Alice!
Hello, Bob!
Hello, Carol!
System shut down.
```

## Supervision and fault tolerance

An actor's message handler can throw. `Actor.ProcessMessageAsync` catches the exception, records it in the actor's metrics, and calls your `OnErrorAsync` override. The framework does not apply a supervision strategy on its own. Your actor reports the failure by calling `SupervisionService.HandleFailureAsync(ActorRef actor, Exception exception, SupervisionStrategy strategy)`, which sends a control message (`restart`, `stop`, `resume`) through `MessageDispatcher`, or a `FailureMessage` to the parent for escalation. The actor's `OnReceiveAsync` has to handle the control commands it receives. Nothing resets its state for you.

### Strategies

`SupervisionStrategy` (in `DotNetActorFramework.Enums`) has five values:

| Strategy | What `HandleFailureAsync` does |
|---|---|
| `Restart` | Sends a `restart` control message. Your `OnReceiveAsync` resets the actor's state. After more than 5 restarts, further failures escalate instead. |
| `Stop` | Sends a `stop` control message with the exception message as the `failure_reason` parameter. |
| `Resume` | Sends a `resume` control message. The actor keeps its state and carries on. |
| `Escalate` | Sends a `FailureMessage` to the parent actor (derived from the path). If there is no parent, the actor is stopped. |
| `Backoff` | Waits a delay that grows with each failure (100 ms base, capped at 30 s), then sends `resume`. |

### Defaults

| Setting | Default | Notes |
|---|---|---|
| Strategy | Chosen by the caller on each `HandleFailureAsync` call | `ActorSystemOptions.DefaultSupervisionStrategy` is `Restart`, but nothing in the framework reads it |
| Max restarts | 5 per actor (the 6th restart request escalates) | Hard-coded in `SupervisionService`; not configurable |
| Restart time window | None. The count is not decayed by time | Cleared only by `SupervisionService.ResetContext(Guid actorId)` |
| Backoff base delay / cap | 100 ms / 30,000 ms | From `ActorConstants`; the `ActorSystemOptions` backoff properties are not read by `SupervisionService` |

### Example

The parent creates a `Worker` child and picks its strategy. The worker throws when it receives a `"poison"` job. Its `OnErrorAsync` hands the failure to `SupervisionService` with `Restart`. The `restart` control message then resets the worker's counter, and the worker keeps processing the jobs that follow. `DrainAsync` plays the role of a dispatcher loop: it processes one message at a time, so the restart runs before the next job.

Create a console app as in [Getting Started](#getting-started) and replace `Program.cs` with the following:

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

public sealed record Job(string Payload) : Message;

public class Worker : Actor
{
    private readonly SupervisionService _supervision;
    private readonly SupervisionStrategy _strategy;
    private int _processed;

    public Worker(ActorPath path, ActorRef supervisor, SupervisionService supervision, SupervisionStrategy strategy)
        : base(path, supervisor)
    {
        _supervision = supervision;
        _strategy = strategy;
    }

    protected override Task OnReceiveAsync(Message message)
    {
        switch (message)
        {
            case Job { Payload: "poison" }:
                throw new InvalidOperationException("Cannot process 'poison' job");

            case Job job:
                _processed++;
                Console.WriteLine($"[{Path}] processed '{job.Payload}' (total since start: {_processed})");
                break;

            case ControlMessage { Command: "restart" }:
                _processed = 0;
                Console.WriteLine($"[{Path}] restarted, state reset");
                break;
        }

        return Task.CompletedTask;
    }

    protected override async Task OnErrorAsync(Message message, Exception exception)
    {
        Console.WriteLine($"[{Path}] failed: {exception.Message}");
        await _supervision.HandleFailureAsync(Ref, exception, _strategy);
    }
}

public class Parent : Actor
{
    private readonly MailboxService _mailboxes;
    private readonly ActorRegistry _registry;
    private readonly MessageDispatcher _dispatcher;
    private readonly SupervisionService _supervision;

    public Parent(
        ActorPath path,
        MailboxService mailboxes,
        ActorRegistry registry,
        MessageDispatcher dispatcher,
        SupervisionService supervision) : base(path)
    {
        _mailboxes = mailboxes;
        _registry = registry;
        _dispatcher = dispatcher;
        _supervision = supervision;
    }

    public async Task<Worker> SpawnWorkerAsync(string name, SupervisionStrategy strategy)
    {
        var worker = new Worker(Path.GetChild(name), Ref, _supervision, strategy);
        await worker.InitializeAsync();
        _mailboxes.CreateMailbox(worker.Id);
        _registry.Register(worker.Ref);
        return worker;
    }

    protected override Task OnReceiveAsync(Message message)
    {
        if (message is FailureMessage failure)
            Console.WriteLine($"[{Path}] escalated failure: {failure.Reason}");

        return Task.CompletedTask;
    }
}

public static class Program
{
    public static async Task Main()
    {
        var system = new ActorSystem("supervision-demo");
        var mailboxes = new MailboxService(new ActorSystemOptions());
        var registry = new ActorRegistry();
        var dispatcher = new MessageDispatcher(mailboxes, registry, system);
        var supervision = new SupervisionService(registry, dispatcher);

        var parent = new Parent(new ActorPath("/user/parent"), mailboxes, registry, dispatcher, supervision);
        await parent.InitializeAsync();
        mailboxes.CreateMailbox(parent.Id);
        registry.Register(parent.Ref);

        var worker = await parent.SpawnWorkerAsync("worker", SupervisionStrategy.Restart);

        await dispatcher.SendAsync(worker.Ref, new Job("a"));
        await dispatcher.SendAsync(worker.Ref, new Job("b"));
        await dispatcher.SendAsync(worker.Ref, new Job("poison"));
        await DrainAsync(dispatcher, worker);

        await dispatcher.SendAsync(worker.Ref, new Job("c"));
        await DrainAsync(dispatcher, worker);

        var context = supervision.GetContext(worker.Id);
        Console.WriteLine($"Restarts: {context?.RestartCount ?? 0}");

        await system.ShutdownAsync();
    }

    private static async Task DrainAsync(MessageDispatcher dispatcher, Actor actor)
    {
        while (await dispatcher.GetNextMessageAsync(actor.Id) is { } envelope)
            await actor.ProcessMessageAsync(envelope.Message);
    }
}
```

Run it with `dotnet run`. Expected output:

```text
[/user/parent/worker] processed 'a' (total since start: 1)
[/user/parent/worker] processed 'b' (total since start: 2)
[/user/parent/worker] failed: Cannot process 'poison' job
[/user/parent/worker] restarted, state reset
[/user/parent/worker] processed 'c' (total since start: 1)
Restarts: 1
```

To try another strategy, change the `SupervisionStrategy.Restart` argument. This `Worker` only handles `restart`, so with `Resume` or `Stop` the control message is ignored until you add a `case` for it.

## EventBusTests

The `EventBusTests` class provides unit tests for the EventBus pub/sub functionality, verifying that subscribers can subscribe and unsubscribe, events are published to all subscribers, handler exceptions are handled appropriately, and event types are properly isolated.

### Usage Example

```csharp
// Arrange
var eventBus = new EventBus();
var handlerCalled = false;
global::DotNetActorFramework.Events.EventHandler<TestEvent> handler = (@event) =>
{
    handlerCalled = true;
    return Task.CompletedTask;
};

// Act
eventBus.Subscribe(handler);

// Assert
eventBus.GetSubscriberCount<TestEvent>().Should().Be(1);
```

### Test Methods

- `Subscribe_ShouldAddHandlerForEventType`: Tests that Subscribe adds a handler for a specific event type.
- `Unsubscribe_ShouldRemoveHandlerForEventType`: Tests that Unsubscribe removes a handler for a specific event type.
- `Unsubscribe_WithNullHandler_ShouldNotThrow`: Tests that Unsubscribe with null handler does nothing.
- `Subscribe_WithNullHandler_ShouldThrowArgumentNullException`: Tests that Subscribe with null handler throws ArgumentNullException.
- `PublishAsync_ShouldInvokeAllSubscribedHandlers`: Tests that PublishAsync invokes all subscribed handlers.
- `PublishAsync_WithNullEvent_ShouldThrowArgumentNullException`: Tests that PublishAsync throws ArgumentNullException when event is null.
- `PublishAsync_ShouldInvokeHandlersInParallel`: Tests that handlers are invoked in parallel.
- `PublishAsync_WithHandlerException_ShouldThrowException`: Tests that handler exceptions are thrown when any handler fails.
- `PublishAsync_WithNoSubscribers_ShouldCompleteSuccessfully`: Tests that PublishAsync completes successfully when there are no subscribers.
- `GetSubscriberCount_WithSubscribers_ShouldReturnCorrectCount`: Tests that GetSubscriberCount returns correct count for event type with subscribers.
- `GetSubscriberCount_WithNoSubscribers_ShouldReturnZero`: Tests that GetSubscriberCount returns 0 for event type with no subscribers.
- `Clear_ShouldRemoveAllSubscribers`: Tests that Clear removes all subscribers.
- `PublishAsync_WithMultipleSubscribers_ShouldNotifyAll`: Tests that multiple subscribers for same event type all receive the event.
- `Unsubscribe_OneHandler_ShouldNotAffectOtherHandlers`: Tests that unsubscribing one handler doesn't affect other handlers for same event type.
- `Subscribe_SameHandlerMultipleTimes_ShouldInvokeMultipleTimes`: Tests that subscribing same handler multiple times results in multiple invocations.
- `PublishAsync_DifferentEventTypes_ShouldIsolateSubscribers`: Tests that different event types are properly isolated.
- `PublishAsync_ActorSystemEvent_ShouldWorkCorrectly`: Tests that EventBus properly handles actor system events.
- `PublishAsync_HandlerException_ShouldIsolateExceptions`: Tests that handler exceptions are properly isolated and other handlers still execute.
- `Subscribe_DifferentEventTypes_ShouldNotInterfere`: Tests that handlers for different event types don't interfere with each other.

## InMemoryEventJournalTests

The `InMemoryEventJournalTests` class provides unit tests for the InMemoryEventJournal implementation, verifying that it correctly handles event appending, reading, and deletion operations with proper sequence number management and actor isolation.

### Usage Example

```csharp
// Arrange
var journal = new InMemoryEventJournal();
var actorId = Guid.NewGuid();
var actorPath = "/test/actor";
var events = new List<ActorEvent>
{
    new(actorId, actorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
    new(actorId, actorPath, 2L, DateTime.UtcNow, new { Type = "Event2" })
};

// Act
await journal.AppendEventsAsync(actorId, actorPath, events);

// Assert
var loadedEvents = await journal.ReadEventsAsync(actorId, actorPath, 1L, 2L);
loadedEvents.Should().HaveCount(2);
```

### Test Methods

- `AppendEventsAsync_ShouldAddEventsWithCorrectSequenceNumbers`: Tests that AppendEventsAsync adds events with correct sequence numbers.
- `AppendEventsAsync_ShouldStoreEventsInCorrectOrder`: Tests that AppendEventsAsync stores events in correct order.
- `ReadEventsAsync_ShouldReturnEmptyCollection_WhenNoEventsExist`: Tests that ReadEventsAsync returns empty collection when no events exist.
- `ReadEventsAsync_ShouldReturnEventsFromSpecifiedOffset`: Tests that ReadEventsAsync returns events from specified offset.
- `ReadEventsAsync_ShouldRespectToSequenceNrLimit`: Tests that ReadEventsAsync respects the to sequence number limit.
- `ReadEventsAsync_ShouldReturnEventsInAscendingOrder`: Tests that ReadEventsAsync returns events in ascending order.
- `ReadEventsBackwardAsync_ShouldReturnEventsInDescendingOrder`: Tests that ReadEventsBackwardAsync returns events in descending order.
- `ReadEventsBackwardAsync_ShouldRespectRangeLimits`: Tests that ReadEventsBackwardAsync respects range limits.
- `DeleteEventsAsync_ShouldRemoveEventsUpToMaxSequenceNr`: Tests that DeleteEventsAsync removes events up to max sequence number.
- `DeleteEventsAsync_ShouldDeleteEventsUpToMaxSequenceNr`: Tests that DeleteEventsAsync deletes events up to max sequence number.
- `DeleteAllEventsAsync_ShouldRemoveAllEventsForActor`: Tests that DeleteAllEventsAsync removes all events for actor.
- `DeleteAllEventsAsync_ShouldNotAffectOtherActors`: Tests that DeleteAllEventsAsync does not affect other actors.
- `AppendEventsAsync_ShouldThrow_WhenSequenceNumberAlreadyExists`: Tests that AppendEventsAsync throws when sequence number already exists.
- `ReadEventsAsync_ShouldHandleLargeSequenceNumberGaps`: Tests that ReadEventsAsync handles large sequence number gaps.
- `ReadEventsAsync_ShouldHandleEmptyRange`: Tests that ReadEventsAsync handles empty range.
- `MultipleAppends_ShouldMaintainCorrectOrder`: Tests that multiple appends maintain correct order.
- `DifferentActorPaths_ShouldIsolateEvents`: Tests that different actor paths isolate events.

## InMemorySnapshotStoreTests

The `InMemorySnapshotStoreTests` class provides unit tests for the InMemorySnapshotStore implementation, verifying that it correctly saves, loads, overwrites, and deletes snapshots for actors. It covers latest-snapshot retrieval, pruning of older snapshots, deletion up to a max sequence number, and isolation between different actors.

### Usage Example

```csharp
// Arrange
var store = new InMemorySnapshotStore();
var actorId = Guid.NewGuid();
var actorPath = "/test/actor";
var snapshot = new ActorSnapshot(actorId, actorPath, new { Counter = 42 }, 100L, DateTime.UtcNow);

// Act
await store.SaveSnapshotAsync(snapshot);

// Assert
var loaded = await store.LoadLatestSnapshotAsync(actorId, actorPath);
loaded.Should().NotBeNull();
loaded!.SequenceNr.Should().Be(100L);
loaded.State.Should().BeEquivalentTo(new { Counter = 42 });
```

### Test Methods

- `SaveSnapshotAsync_ShouldSaveSnapshotWithCorrectProperties`: Tests that saving a snapshot stores it with all properties correctly preserved.
- `LoadLatestSnapshotAsync_ShouldReturnNull_WhenNoSnapshotExists`: Tests that loading a snapshot returns null when no snapshot exists for the specified actor.
- `SaveSnapshotAsync_ShouldOverwriteOlderSnapshot_WhenSavingNewerSnapshotWithSameActor`: Tests that saving a new snapshot overwrites an existing snapshot when they have the same sequence number.
- `SaveSnapshotAsync_ShouldKeepOnlyLatestSnapshot_WhenSavingMultipleSnapshots`: Tests that only the latest snapshot is retained when multiple snapshots are saved for the same actor.
- `SaveSnapshotAsync_ShouldPruneOlderSnapshots_WhenSavingNewSnapshot`: Tests that older snapshots are automatically pruned when a new snapshot is saved.
- `DeleteSnapshotsAsync_ShouldRemoveAllSnapshotsForActor`: Tests that deleting all snapshots removes all stored snapshots for a specific actor.
- `DeleteSnapshotsAsync_ShouldRemoveSnapshotsUpToMaxSequenceNumber`: Tests that deleting snapshots up to a sequence number removes only snapshots with sequence numbers less than or equal to the specified value.
- `DeleteAllSnapshotsAsync_ShouldNotAffectOtherActors`: Tests that deleting snapshots for one actor does not affect snapshots of other actors.
- `LoadLatestSnapshotAsync_ShouldReturnNull_WhenActorHasNoSnapshots`: Tests that loading a snapshot returns null when the actor has no snapshots.
- `SaveSnapshotAsync_ShouldHandleNullSnapshot`: Tests that saving a null snapshot is handled gracefully.
- `SaveSnapshotAsync_ShouldHandleDifferentActorPaths`: Tests that saving snapshots for different actor paths keeps them isolated.
- `SaveSnapshotAsync_ShouldHandleSameSequenceNumberDifferentActors`: Tests that snapshots with the same sequence number for different actors are stored independently.

## DotnetActorFrameworkExceptionTests

The `DotnetActorFrameworkExceptionTests` class verifies the framework exception's constructors, formatted factory methods, inheritance, message, and inner-exception behavior. Its public test methods can be run by xUnit or invoked directly when isolating a specific exception scenario; each method completes normally when its assertions pass.

### Usage Example

```csharp
using DotNetActorFramework.Tests;

var tests = new DotnetActorFrameworkExceptionTests();

tests.Constructor_WithMessage_ShouldCreateExceptionWithMessage();
tests.Create_WithInnerExceptionAndFormat_ShouldCreateExceptionWithInnerException();
```

## ActorSystem

`ActorSystem` (in `DotNetActorFramework.Models`) is the root coordinator that owns every actor in a single logical domain. It manages the actor registry, routes messages through its `MessageDispatcher`, tracks system health, and performs an orderly shutdown when you are done.

Each system has a unique `Name`, a generated `Id`, and an `IsRunning` flag that gates all operations. Actors are created with `CreateActorAsync`, looked up with `GetActorRef`, and removed with `TerminateActorAsync`. A dead-letter handler registered via `SetDeadLetterHandler` is called whenever a message cannot be delivered.

### Basic usage

```csharp
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Configuration;

// 1. Bootstrap the system and its services
var system = new ActorSystem("my-system");
var mailboxes = new MailboxService(new ActorSystemOptions());
var registry = new ActorRegistry();
var dispatcher = new MessageDispatcher(mailboxes, registry, system);

// 2. Optional: observe undeliverable messages
system.SetDeadLetterHandler(envelope =>
    Console.WriteLine($"Dead letter to {envelope.Recipient.Path}: {envelope.Message}"));

// 3. Create an actor
var path = new ActorPath("/user/worker");
var actorRef = await system.CreateActorAsync(path);

// 4. Query the system
Console.WriteLine($"Actor count: {system.GetActorCount()}");

var health = system.GetHealthSummary();
Console.WriteLine($"Healthy: {health.IsHealthy}, actors: {health.TotalActors}");

// 5. Shut down gracefully
await system.ShutdownAsync();
Console.WriteLine($"Shut down at {system.ShutdownAt}");
```

### Key members

| Member | Description |
|---|---|
| `CreateActorAsync(path, supervisor?)` | Creates and registers a new actor; throws if the path is taken or the system is stopped. |
| `GetActorRef(path)` | Returns the `ActorRef` at the given path, or `null`. |
| `GetAllActors()` | Returns every registered `ActorRef`. |
| `GetActorsByParent(parentPath)` | Returns actors whose path descends from `parentPath`. |
| `TerminateActorAsync(actorRef)` | Stops and unregisters an actor. |
| `GetActorCount()` | Number of live actors. |
| `GetHealthSummary()` | Returns a `SystemHealthSummary` with counts and error rates. |
| `GetActorMetricsSummary(path)` | Per-actor metrics (message count, error count, etc.). |
| `GetErrorActors()` | Actors currently in the `Error` state. |
| `SetDeadLetterHandler(handler)` | Registers (or clears) a callback for undeliverable messages. |
| `DeadLetters` | Bounded `DeadLetterQueue` holding undeliverable envelopes (oldest evicted when full). Use `Subscribe(handler)` to observe them. |
| `ShutdownAsync()` | Terminates all actors and marks the system as stopped. |

## ActorRef

`ActorRef` (in `DotNetActorFramework.Models`) is an immutable, thread-safe handle to a single actor. Callers use it instead of the actor itself, so they can send messages and check liveness without touching the actor's internal state. An `ActorRef` is never constructed directly: you get one from `ActorSystem.CreateActorAsync` or `ActorSystem.GetActorRef`.

Each reference exposes the actor's `Path` (an `ActorPath` that identifies it in the hierarchy), its `Id` (a `Guid`), its `CreatedAt` timestamp (UTC), and an `IsAlive` flag. `IsAlive` starts as `true` and is set to `false` when the actor is terminated, after which sending operations throw `InvalidOperationException`.

Its messaging methods are:

| Method | Description |
|---|---|
| `SendAsync(message)` | Sends a message to the actor. Throws `ArgumentNullException` for a null message and `InvalidOperationException` if the actor is not alive. |
| `AskAsync(message, timeout)` | Request-reply without a result type. Returns `null` once the timeout elapses. |
| `AskAsync<T>(message, timeout)` | Request-reply with a typed result. Throws `TimeoutException` if the actor does not respond in time. |
| `AskAsync<T>(message)` | Same as above, using the default timeout (30 seconds). |
| `GetParent()` | Returns an `ActorRef` for the parent path, or `null` for a root actor. |

Equality is value-based: two references are equal when they have the same `Path` and `Id`. `ActorRef` implements `IEquatable<ActorRef>`, overloads `==` and `!=`, and `ToString()` returns `"{Path} ({Id:N})"`.

> **Note:** the messaging methods are still being wired to the mailbox layer. In the current code, `SendAsync` validates its arguments and returns without delivering the message, and the `AskAsync` overloads wait for the timeout without receiving a reply. Treat the examples below as the intended API shape until delivery is implemented.

### Usage example

```csharp
using DotNetActorFramework.Models;

var system = new ActorSystem("my-system");

// Obtain references from the system; ActorRef cannot be constructed directly.
var created = await system.CreateActorAsync(new ActorPath("/user/worker"));

// The same actor can be looked up later by path.
var found = system.GetActorRef(new ActorPath("/user/worker"));

Console.WriteLine(created == found);          // True: same Path and Id
Console.WriteLine(created);                   // "/user/worker (<guid without dashes>)"
Console.WriteLine($"Created at {created.CreatedAt:O}, alive: {created.IsAlive}");

// Fire-and-forget send (see the note above about current delivery behavior).
await created.SendAsync("hello");

// Request-reply with a typed response and a 5-second timeout.
try
{
    var reply = await created.AskAsync<string>("ping", TimeSpan.FromSeconds(5));
    Console.WriteLine(reply);
}
catch (TimeoutException ex)
{
    Console.WriteLine(ex.Message);
}

// Walk up the hierarchy; null for a root actor.
var parent = created.GetParent();

await system.ShutdownAsync();
```

## ActorRegistry

`ActorRegistry` (in `DotNetActorFramework.Services`, source in `src/DotNetActorFramework/Services/ActorRegistry.cs`) is the in-memory index of actors that the framework uses to resolve them. It keeps three indexes under a single lock: a path index (`ActorPath` to `ActorRef`), an id index (`Guid` to `ActorRef`), and a hierarchy index (parent path to child paths), which lets you walk the actor tree. `MessageDispatcher` takes a registry in its constructor.

Registration is explicit. `Register` adds an `ActorRef` and throws `InvalidOperationException` if the path is already taken. `Unregister` removes an `ActorRef` by its path and id; it does nothing if the actor is not present. Single-actor lookups return `null` when nothing matches instead of throwing. Collection-returning methods return read-only snapshots, so callers can iterate them without holding the registry's lock.

| Member | Description |
|---|---|
| `Register(actorRef)` | Adds the actor under its path and id and records it under its parent. Throws `ArgumentNullException` for null and `InvalidOperationException` for a duplicate path. |
| `Unregister(actorRef)` | Removes the actor from the path, id, and hierarchy indexes. Child actors stay registered. |
| `GetByPath(path)` | Returns the `ActorRef` at the path, or `null`. |
| `GetById(id)` | Returns the `ActorRef` with the given `Guid`, or `null`. |
| `Contains(path)` | `true` if an actor is registered at the path. |
| `GetChildren(parentPath)` | Returns the direct children of the parent path. |
| `GetDescendants(parentPath)` | Returns all descendants of the parent path, depth-first. |
| `GetAll()` | Returns every registered `ActorRef`. |
| `GetCount()` | Number of registered actors. |
| `Clear()` | Removes all registrations. |

`ActorRef` cannot be constructed directly, so the registry is fed with references returned by `ActorSystem.CreateActorAsync`. A registry is a separate index: registering an actor here does not change what `ActorSystem` tracks.

### Usage example

```csharp
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

var system = new ActorSystem("my-system");
var registry = new ActorRegistry();

// Create actors through the system, then index them in the registry.
var parent = await system.CreateActorAsync(new ActorPath("/user/parent"));
var child = await system.CreateActorAsync(new ActorPath("/user/parent/child"));
registry.Register(parent);
registry.Register(child);

// Lookups return null when nothing matches.
var byPath = registry.GetByPath(new ActorPath("/user/parent"));
var byId = registry.GetById(child.Id);
Console.WriteLine(byPath == parent);                                   // True
Console.WriteLine(registry.Contains(new ActorPath("/user/missing")));  // False

// Hierarchy queries.
var children = registry.GetChildren(new ActorPath("/user/parent"));    // [child]
var descendants = registry.GetDescendants(new ActorPath("/user"));     // [parent, child]
Console.WriteLine($"Registered: {registry.GetCount()}");               // 2

// A second registration at the same path is rejected.
try
{
    registry.Register(parent);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine(ex.Message);  // Actor already registered at path: /user/parent
}

// Unregistering a parent does not remove its children.
registry.Unregister(parent);
Console.WriteLine(registry.Contains(child.Path));                      // True

await system.ShutdownAsync();
```

> **Note:** `Unregister` removes whatever is indexed under the given path and id, and it does not cascade to child actors. Remove children explicitly if you want them gone too.

## Envelope

`Envelope` (in `DotNetActorFramework.Models`, source in `src/DotNetActorFramework/Models/Envelope.cs`) is the wrapper that travels through the mailbox layer. It pairs a `Message` with its routing information: the `Recipient` it is addressed to and the optional `Sender` that produced it. It also stamps the send time and a unique identifier, and carries the delivery-tracking state (retry count and delivered flag) that the dispatcher needs to decide what to do with a message.

`Envelope` is a class, not a struct, so it lives on the heap and can be passed by reference between the dispatcher, mailboxes, and dead-letter handling without copying.

### Structure

| Member | Kind | Description |
|---|---|---|
| `Message` | `readonly` field | The payload being delivered. |
| `Sender` | `readonly` field, `ActorRef?` | The actor that sent the message, or `null` for system messages. |
| `Recipient` | `readonly` field, `ActorRef` | The actor the message is addressed to. |
| `SentAt` | `readonly` field, `DateTime` | UTC timestamp taken when the envelope was created. |
| `EnvelopeId` | `readonly` field, `Guid` | Unique identifier for this envelope. |
| `RetryCount` | public field, `int` | Number of failed delivery attempts. Starts at `0`. |
| `IsDelivered` | public field, `bool` | Whether delivery succeeded. Starts as `false`. |

The constructor `Envelope(Message message, ActorRef recipient, ActorRef? sender = null)` throws `ArgumentNullException` if `message` or `recipient` is `null`. It sets `SentAt` to `DateTime.UtcNow` and generates a new `EnvelopeId`.

The state-changing and query methods are:

| Method | Description |
|---|---|
| `MarkAsDelivered()` | Sets `IsDelivered` to `true`. |
| `IncrementRetryCount()` | Adds one to `RetryCount`. |
| `HasExceededRetryLimit(maxRetries = 3)` | Returns `true` when `RetryCount` is greater than `maxRetries`. |
| `GetElapsedTime()` | Returns the time since `SentAt`. |
| `GetDeliveryPriority()` | Returns `Message.Priority * 100` plus the elapsed time in whole seconds. Higher values sort as more urgent. |
| `ToString()` | Returns a diagnostic string in the form `[<EnvelopeId:N>] <sender or System> -> <recipient name>: <message type name>`. |

### Usage example

```csharp
using DotNetActorFramework.Models;

var system = new ActorSystem("my-system");
var worker = await system.CreateActorAsync(new ActorPath("/user/worker"));
var client = await system.CreateActorAsync(new ActorPath("/user/client"));

var envelope = new Envelope(new ControlMessage { Command = "start" }, worker, client);

Console.WriteLine(envelope);                 // "[<guid without dashes>] client -> worker: ControlMessage"
Console.WriteLine(envelope.IsDelivered);     // False

// A system message has no sender.
var systemEnvelope = new Envelope(new ControlMessage { Command = "shutdown" }, worker);
Console.WriteLine(systemEnvelope.Sender is null); // True

// Delivery bookkeeping.
envelope.IncrementRetryCount();
if (envelope.HasExceededRetryLimit())
{
    Console.WriteLine("Giving up after too many attempts.");
}
else
{
    envelope.MarkAsDelivered();
}

await system.ShutdownAsync();
```

> **Note:** `RetryCount` and `IsDelivered` are public fields, so code can assign them directly. Prefer `IncrementRetryCount()` and `MarkAsDelivered()`, which express the intent and keep the call sites consistent.

## License

Released under the [MIT License](LICENSE). Copyright (c) 2026 Vladyslav Zaiets.
