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
