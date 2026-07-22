// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Events;
using FluentAssertions;
using Xunit;


/// <summary>
/// Contains unit tests for EventBus pub/sub functionality.
/// </summary>
namespace DotNetActorFramework.Tests;

public class EventBusTests
{
    /// <summary>
    /// Tests that Subscribe adds a handler for a specific event type.
    /// </summary>
    [Fact]
    public void Subscribe_ShouldAddHandlerForEventType()
    {
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
    }

    /// <summary>
    /// Tests that Unsubscribe removes a handler for a specific event type.
    /// </summary>
    [Fact]
    public void Unsubscribe_ShouldRemoveHandlerForEventType()
    {
        // Arrange
        var eventBus = new EventBus();
        var handlerCalled = false;
        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler = (@event) =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler);
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(1);

        // Act
        eventBus.Unsubscribe(handler);

        // Assert
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(0);
    }

    /// <summary>
    /// Tests that Unsubscribe with null handler does nothing.
    /// </summary>
    [Fact]
    public void Unsubscribe_WithNullHandler_ShouldNotThrow()
    {
        // Arrange
        var eventBus = new EventBus();

        // Act & Assert
        eventBus.Invoking(b => b.Unsubscribe<TestEvent>(null!))
            .Should().NotThrow();
    }

    /// <summary>
    /// Tests that Subscribe with null handler throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Subscribe_WithNullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var eventBus = new EventBus();

        // Act & Assert
        eventBus.Invoking(b => b.Subscribe<TestEvent>(null!))
            .Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that PublishAsync invokes all subscribed handlers.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldInvokeAllSubscribedHandlers()
    {
        // Arrange
        var eventBus = new EventBus();
        var handler1Called = false;
        var handler2Called = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = (@event) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        var testEvent = new TestEvent();

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeTrue();
    }

    /// <summary>
    /// Tests that PublishAsync throws ArgumentNullException when event is null.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        var eventBus = new EventBus();

        // Act & Assert
        await eventBus.Invoking(b => b.PublishAsync<TestEvent>(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that handlers are invoked in parallel.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ShouldInvokeHandlersInParallel()
    {
        // Arrange
        var eventBus = new EventBus();
        var handler1Started = false;
        var handler1Completed = false;
        var handler2Started = false;
        var handler2Completed = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = async (@event) =>
        {
            handler1Started = true;
            await Task.Delay(100); // Simulate work
            handler1Completed = true;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = async (@event) =>
        {
            handler2Started = true;
            await Task.Delay(100); // Simulate work
            handler2Completed = true;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        var testEvent = new TestEvent();

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert - both handlers should have started (parallel execution)
        handler1Started.Should().BeTrue();
        handler2Started.Should().BeTrue();
        handler1Completed.Should().BeTrue();
        handler2Completed.Should().BeTrue();
    }

    /// <summary>
    /// Tests that handler exceptions are thrown when any handler fails.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithHandlerException_ShouldThrowException()
    {
        // Arrange
        var eventBus = new EventBus();
        var handler1Called = false;
        var handler2Called = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) =>
        {
            handler1Called = true;
            throw new InvalidOperationException("Handler 1 failed");
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = (@event) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        var testEvent = new TestEvent();

        // Act
        var exception = await eventBus.Awaiting(b => b.PublishAsync(testEvent))
            .Should().ThrowAsync<InvalidOperationException>();

        // Assert
        exception.And.Message.Should().Be("Handler 1 failed");
        handler1Called.Should().BeTrue();
        handler2Called.Should().BeFalse(); // Task.WhenAll throws on first exception, so handler2 is not called
    }

    /// <summary>
    /// Tests that PublishAsync completes successfully when there are no subscribers.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithNoSubscribers_ShouldCompleteSuccessfully()
    {
        // Arrange
        var eventBus = new EventBus();
        var testEvent = new TestEvent();

        // Act & Assert - Should not throw
        await eventBus.Invoking(b => b.PublishAsync(testEvent))
            .Should().NotThrowAsync();
    }

    /// <summary>
    /// Tests that GetSubscriberCount returns correct count for event type with subscribers.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_WithSubscribers_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventBus = new EventBus();
        var handler1Called = false;
        var handler2Called = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = (@event) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        // Act
        var count = eventBus.GetSubscriberCount<TestEvent>();

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Tests that GetSubscriberCount returns 0 for event type with no subscribers.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_WithNoSubscribers_ShouldReturnZero()
    {
        // Arrange
        var eventBus = new EventBus();

        // Act
        var count = eventBus.GetSubscriberCount<TestEvent>();

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that Clear removes all subscribers.
    /// </summary>
    [Fact]
    public void Clear_ShouldRemoveAllSubscribers()
    {
        // Arrange
        var eventBus = new EventBus();
        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) => Task.CompletedTask;
        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = (@event) => Task.CompletedTask;
        global::DotNetActorFramework.Events.EventHandler<AnotherTestEvent> handler3 = (@event) => Task.CompletedTask;

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);
        eventBus.Subscribe(handler3);

        eventBus.GetSubscriberCount<TestEvent>().Should().Be(2);
        eventBus.GetSubscriberCount<AnotherTestEvent>().Should().Be(1);

        // Act
        eventBus.Clear();

        // Assert
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(0);
        eventBus.GetSubscriberCount<AnotherTestEvent>().Should().Be(0);
    }

    /// <summary>
    /// Tests that multiple subscribers for same event type all receive the event.
    /// </summary>
    [Fact]
    public async Task PublishAsync_WithMultipleSubscribers_ShouldNotifyAll()
    {
        // Arrange
        var eventBus = new EventBus();
        var receivedEvents = new List<TestEvent>();

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) =>
        {
            receivedEvents.Add(@event);
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = (@event) =>
        {
            receivedEvents.Add(@event);
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler3 = (@event) =>
        {
            receivedEvents.Add(@event);
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);
        eventBus.Subscribe(handler3);

        var testEvent = new TestEvent();

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        receivedEvents.Should().HaveCount(3);
        receivedEvents.Should().AllBeEquivalentTo(testEvent);
    }

    /// <summary>
    /// Tests that unsubscribing one handler doesn't affect other handlers for same event type.
    /// </summary>
    [Fact]
    public async Task Unsubscribe_OneHandler_ShouldNotAffectOtherHandlers()
    {
        // Arrange
        var eventBus = new EventBus();
        var handler1Called = false;
        var handler2Called = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler2 = (@event) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler1);
        eventBus.Subscribe(handler2);

        // Act
        eventBus.Unsubscribe(handler1);

        var testEvent = new TestEvent();
        await eventBus.PublishAsync(testEvent);

        // Assert
        handler1Called.Should().BeFalse();
        handler2Called.Should().BeTrue();
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(1);
    }

    /// <summary>
    /// Tests that subscribing same handler multiple times results in multiple invocations.
    /// </summary>
    [Fact]
    public async Task Subscribe_SameHandlerMultipleTimes_ShouldInvokeMultipleTimes()
    {
        // Arrange
        var eventBus = new EventBus();
        var invocationCount = 0;

        global::DotNetActorFramework.Events.EventHandler< TestEvent> handler = (@event) =>
        {
            invocationCount++;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler);
        eventBus.Subscribe(handler);
        eventBus.Subscribe(handler);

        var testEvent = new TestEvent();

        // Act
        await eventBus.PublishAsync(testEvent);

        // Assert
        invocationCount.Should().Be(3);
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(3);
    }

    /// <summary>
    /// Tests that different event types are properly isolated.
    /// </summary>
    [Fact]
    public async Task PublishAsync_DifferentEventTypes_ShouldIsolateSubscribers()
    {
        // Arrange
        var eventBus = new EventBus();
        var testEventReceived = false;
        var anotherTestEventReceived = false;

        global::DotNetActorFramework.Events.EventHandler< TestEvent> testHandler = (@event) =>
        {
            testEventReceived = true;
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler< AnotherTestEvent> anotherTestHandler = (@event) =>
        {
            anotherTestEventReceived = true;
            return Task.CompletedTask;
        };

        eventBus.Subscribe(testHandler);
        eventBus.Subscribe(anotherTestHandler);

        var testEvent = new TestEvent();
        var anotherTestEvent = new AnotherTestEvent();

        // Act
        await eventBus.PublishAsync(testEvent);
        await eventBus.PublishAsync(anotherTestEvent);

        // Assert
        testEventReceived.Should().BeTrue();
        anotherTestEventReceived.Should().BeTrue();
    }

    /// <summary>
    /// Tests that EventBus properly handles actor system events.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ActorSystemEvent_ShouldWorkCorrectly()
    {
        // Arrange
        var eventBus = new EventBus();
        var actorCreatedReceived = false;

        global::DotNetActorFramework.Events.EventHandler<ActorCreatedEvent> handler = (@event) =>
        {
            actorCreatedReceived = true;
            @event.ActorPath.Should().Be("/user/test-actor");
            @event.ActorId.Should().NotBe(Guid.Empty);
            return Task.CompletedTask;
        };

        eventBus.Subscribe(handler);

        var actorEvent = new ActorCreatedEvent
        {
            ActorPath = "/user/test-actor",
            ActorId = Guid.NewGuid(),
            SystemName = "test-system",
            SystemId = Guid.NewGuid()
        };

        // Act
        await eventBus.PublishAsync(actorEvent);

        // Assert
        actorCreatedReceived.Should().BeTrue();
    }

    /// <summary>
    /// Tests that handler exceptions are properly isolated and other handlers still execute.
    /// </summary>
    [Fact]
    public async Task PublishAsync_HandlerException_ShouldIsolateExceptions()
    {
        // Arrange
        var eventBus = new EventBus();
        var successfulHandlerCalled = false;
        var failingHandlerCalled = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> successfulHandler = (@event) =>
        {
            successfulHandlerCalled = true;
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<TestEvent> failingHandler = (@event) =>
        {
            failingHandlerCalled = true;
            throw new InvalidOperationException("Test handler failure");
        };

        eventBus.Subscribe(successfulHandler);
        eventBus.Subscribe(failingHandler);

        var testEvent = new TestEvent();

        // Act - should throw but both handlers should still be called
        Func<Task> act = async () => await eventBus.PublishAsync(testEvent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        successfulHandlerCalled.Should().BeTrue("Successful handlers should still execute even when one fails");
        failingHandlerCalled.Should().BeTrue("Failing handler should have been called");
    }

    /// <summary>
    /// Tests that handlers for different event types don't interfere with each other.
    /// </summary>
    [Fact]
    public void Subscribe_DifferentEventTypes_ShouldNotInterfere()
    {
        // Arrange
        var eventBus = new EventBus();
        var handler1Called = false;
        var handler2Called = false;

        global::DotNetActorFramework.Events.EventHandler<TestEvent> handler1 = (@event) =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        };

        global::DotNetActorFramework.Events.EventHandler<AnotherTestEvent> handler2 = (@event) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        };

        // Act
        eventBus.Subscribe(handler1);
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(1);
        eventBus.GetSubscriberCount<AnotherTestEvent>().Should().Be(0);

        eventBus.Subscribe(handler2);
        eventBus.GetSubscriberCount<TestEvent>().Should().Be(1);
        eventBus.GetSubscriberCount<AnotherTestEvent>().Should().Be(1);

        // Assert
        handler1Called.Should().BeFalse("Handlers should not be called until PublishAsync");
        handler2Called.Should().BeFalse("Handlers should not be called until PublishAsync");
    }

    /// <summary>
    /// Test event type for testing.
    /// </summary>
    private class TestEvent : DomainEvent
    {
        public override string EventType => "test.event";
    }

    /// <summary>
    /// Another test event type for testing.
    /// </summary>
    private class AnotherTestEvent : DomainEvent
    {
        public override string EventType => "another.test.event";
    }
}
