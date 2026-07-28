namespace DotNetActorFramework.Tests;

using System;
using System.Threading.Tasks;
using DotNetActorFramework.Events;
using DotNetActorFramework.Integration;
using Xunit;

public class IntegrationEventPublisherTests : IDisposable
{
    private readonly WebhookDispatcher _webhookDispatcher;
    private readonly IntegrationEventPublisher _publisher;

    public IntegrationEventPublisherTests()
    {
        _webhookDispatcher = new WebhookDispatcher();
        _publisher = new IntegrationEventPublisher(_webhookDispatcher);
    }

    private class TestEvent : DomainEvent
    {
        public override string EventType => "TestEvent";
    }

    [Fact]
    public void Constructor_NullDispatcher_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new IntegrationEventPublisher(null!));
    }

    [Fact]
    public async Task PublishAsync_NullEvent_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _publisher.PublishAsync(null!));
    }

    [Fact]
    public async Task PublishAsync_ValidEvent_AddsToQueue()
    {
        // Arrange
        var @event = new TestEvent();

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        Assert.Equal(1, _publisher.GetQueueLength());
    }

    [Fact]
    public async Task GetQueueLength_MultipleEvents_ReturnsCorrectCount()
    {
        // Act
        await _publisher.PublishAsync(new TestEvent());
        await _publisher.PublishAsync(new TestEvent());

        // Assert
        Assert.Equal(2, _publisher.GetQueueLength());
    }

    public void Dispose()
    {
        _publisher.Dispose();
        _webhookDispatcher.Dispose();
    }
}
