// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class DeadLetterQueueTests
{
    private static Envelope CreateEnvelope(string name = "recipient")
    {
        var recipient = new ActorRef(new ActorPath($"/{name}"), Guid.NewGuid());
        return new Envelope(new ControlMessage(name), recipient);
    }

    [Fact]
    public void Constructor_ZeroCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = () => new DeadLetterQueue(0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("capacity");
    }

    [Fact]
    public void Constructor_NegativeCapacity_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = () => new DeadLetterQueue(-1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("capacity");
    }

    [Fact]
    public void Constructor_ValidCapacity_ShouldExposeCapacityAndStartEmpty()
    {
        // Act
        var queue = new DeadLetterQueue(5);

        // Assert
        queue.Capacity.Should().Be(5);
        queue.Count.Should().Be(0);
        queue.EvictedCount.Should().Be(0);
        queue.GetSnapshot().Should().BeEmpty();
    }

    [Fact]
    public void Enqueue_NullEnvelope_ShouldThrowArgumentNullException()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);

        // Act
        var act = () => queue.Enqueue(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Enqueue_BelowCapacity_ShouldRetainEnvelopesInArrivalOrder()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        var first = CreateEnvelope("first");
        var second = CreateEnvelope("second");

        // Act
        queue.Enqueue(first);
        queue.Enqueue(second);

        // Assert
        queue.Count.Should().Be(2);
        queue.GetSnapshot().Should().ContainInOrder(first, second);
        queue.EvictedCount.Should().Be(0);
    }

    [Fact]
    public void Enqueue_AtCapacity_ShouldEvictOldestEnvelope()
    {
        // Arrange
        var queue = new DeadLetterQueue(2);
        var oldest = CreateEnvelope("oldest");
        var middle = CreateEnvelope("middle");
        var newest = CreateEnvelope("newest");
        queue.Enqueue(oldest);
        queue.Enqueue(middle);

        // Act
        queue.Enqueue(newest);

        // Assert
        queue.Count.Should().Be(2);
        queue.GetSnapshot().Should().ContainInOrder(middle, newest);
        queue.GetSnapshot().Should().NotContain(oldest);
        queue.EvictedCount.Should().Be(1);
    }

    [Fact]
    public void Enqueue_FarOverCapacity_ShouldNeverExceedCapacityAndCountEvictions()
    {
        // Arrange
        var queue = new DeadLetterQueue(3);

        // Act
        for (var i = 0; i < 10; i++)
        {
            queue.Enqueue(CreateEnvelope($"actor{i}"));
        }

        // Assert
        queue.Count.Should().Be(3);
        queue.EvictedCount.Should().Be(7);
    }

    [Fact]
    public void GetSnapshot_AfterFurtherEnqueues_ShouldNotChange()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        queue.Enqueue(CreateEnvelope("a"));
        var snapshot = queue.GetSnapshot();

        // Act
        queue.Enqueue(CreateEnvelope("b"));

        // Assert
        snapshot.Should().HaveCount(1);
        queue.GetSnapshot().Should().HaveCount(2);
    }

    [Fact]
    public void Clear_WithRetainedEnvelopes_ShouldRemoveAllButKeepEvictionCount()
    {
        // Arrange
        var queue = new DeadLetterQueue(1);
        queue.Enqueue(CreateEnvelope("a"));
        queue.Enqueue(CreateEnvelope("b"));

        // Act
        queue.Clear();

        // Assert
        queue.Count.Should().Be(0);
        queue.GetSnapshot().Should().BeEmpty();
        queue.EvictedCount.Should().Be(1);
    }

    [Fact]
    public void Subscribe_EnqueueAfterSubscribing_ShouldNotifySubscriber()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        var received = new List<Envelope>();
        var envelope = CreateEnvelope();
        using var _ = queue.Subscribe(received.Add);

        // Act
        queue.Enqueue(envelope);

        // Assert
        received.Should().ContainSingle().Which.Should().BeSameAs(envelope);
    }

    [Fact]
    public void Subscribe_NullHandler_ShouldThrowArgumentNullException()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);

        // Act
        var act = () => queue.Subscribe(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Dispose_Subscription_ShouldStopNotifications()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        var callCount = 0;
        var subscription = queue.Subscribe(_ => callCount++);
        queue.Enqueue(CreateEnvelope("before"));

        // Act
        subscription.Dispose();
        queue.Enqueue(CreateEnvelope("after"));

        // Assert
        callCount.Should().Be(1);
    }

    [Fact]
    public void Dispose_CalledTwice_ShouldNotThrow()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        var subscription = queue.Subscribe(_ => { });

        // Act
        var act = () =>
        {
            subscription.Dispose();
            subscription.Dispose();
        };

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Enqueue_FaultySubscriber_ShouldStillStoreAndNotifyOtherSubscribers()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        var healthyCalls = 0;
        using var faulty = queue.Subscribe(_ => throw new InvalidOperationException("boom"));
        using var healthy = queue.Subscribe(_ => healthyCalls++);

        // Act
        var act = () => queue.Enqueue(CreateEnvelope());

        // Assert
        act.Should().NotThrow();
        healthyCalls.Should().Be(1);
        queue.Count.Should().Be(1);
    }

    [Fact]
    public void Enqueue_SubscriberCallsBackIntoQueue_ShouldNotDeadlock()
    {
        // Arrange
        var queue = new DeadLetterQueue(5);
        var observedCount = -1;
        using var _ = queue.Subscribe(_ => observedCount = queue.Count);

        // Act
        var task = Task.Run(() => queue.Enqueue(CreateEnvelope()));

        // Assert
        task.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();
        observedCount.Should().Be(1);
    }

    [Fact]
    public async Task Enqueue_ConcurrentWriters_ShouldRespectCapacity()
    {
        // Arrange
        const int capacity = 100;
        const int total = 1000;
        var queue = new DeadLetterQueue(capacity);
        var envelopes = Enumerable.Range(0, total).Select(i => CreateEnvelope($"actor{i}")).ToArray();

        // Act
        await Parallel.ForEachAsync(envelopes, (envelope, _) =>
        {
            queue.Enqueue(envelope);
            return ValueTask.CompletedTask;
        });

        // Assert
        queue.Count.Should().Be(capacity);
        queue.EvictedCount.Should().Be(total - capacity);
    }
}
