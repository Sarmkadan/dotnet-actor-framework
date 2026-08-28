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