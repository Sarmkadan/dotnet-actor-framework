// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contains unit tests for core functionality of the DotNetActorFramework.
/// </summary>
namespace DotNetActorFramework.Tests;

public class CoreFunctionalityTests
{
    /// <summary>
    /// Tests that an actor can be registered and retrieved from the actor registry.
    /// </summary>
    [Fact]
    public void ActorRegistry_RegisterAndGet_ShouldReturnCorrectActor()
    {
        // Arrange
        var registry = new ActorRegistry();
        var path = new ActorPath("/system/test-actor");
        var actorRef = new ActorRef(path, Guid.NewGuid());

        // Act
        registry.Register(actorRef);
        var retrieved = registry.GetByPath(path);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved.Should().Be(actorRef);
        registry.Contains(path).Should().BeTrue();
    }

    /// <summary>
    /// Tests that clearing the actor registry removes all actors.
    /// </summary>
    [Fact]
    public void ActorRegistry_Clear_ShouldRemoveAllActors()
    {
        // Arrange
        var registry = new ActorRegistry();
        registry.Register(new ActorRef(new ActorPath("/a"), Guid.NewGuid()));
        registry.Register(new ActorRef(new ActorPath("/b"), Guid.NewGuid()));

        // Act
        registry.Clear();

        // Assert
        registry.GetCount().Should().Be(0);
        registry.GetAll().Should().BeEmpty();
    }

    /// <summary>
    /// Tests that creating and enqueueing a message to a mailbox holds the message.
    /// </summary>
    [Fact]
    public async Task MailboxService_CreateAndEnqueue_ShouldHoldMessage()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 10 };
        var service = new MailboxService(options);
        var actorId = Guid.NewGuid();
        var mailbox = service.CreateMailbox(actorId);
        var actorRef = new ActorRef(new ActorPath("/user/test-actor"), actorId);
        var envelope = new Envelope(new Message<string>("test-data") { Priority = 1 }, actorRef);

        // Act
        await service.EnqueueAsync(actorId, envelope);

        // Assert
        service.GetMailboxSize(actorId).Should().Be(1);
    }

    /// <summary>
    /// Tests that enqueuing and dequeuing a message from a mailbox returns the same message.
    /// </summary>
    [Fact]
    public async Task MailboxService_EnqueueAndDequeue_ShouldReturnSameMessage()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 10 };
        var service = new MailboxService(options);
        var actorId = Guid.NewGuid();
        service.CreateMailbox(actorId);
        var actorRef = new ActorRef(new ActorPath("/user/test-actor"), actorId);
        var envelope = new Envelope(new Message<string>("test-data") { Priority = 1 }, actorRef);
        await service.EnqueueAsync(actorId, envelope);

        // Act
        var dequeued = await service.DequeueAsync(actorId);

        // Assert
        dequeued.Should().NotBeNull();
        dequeued.Should().Be(envelope);
        service.GetMailboxSize(actorId).Should().Be(0);
    }

    /// <summary>
    /// Tests that enqueuing to a full mailbox fails.
    /// </summary>
    [Fact]
    public async Task MailboxService_EnqueueToFullMailbox_ShouldFail()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 1 };
        var service = new MailboxService(options);
        var actorId = Guid.NewGuid();
        service.CreateMailbox(actorId);
        var actorRef = new ActorRef(new ActorPath("/user/test-actor"), actorId);

        await service.EnqueueAsync(actorId, new Envelope(new Message<string>("m1") { Priority = 1 }, actorRef));

        // Act & Assert
        // The MailboxService.EnqueueAsync method throws MailboxException if EnqueueAsync returns false
        await service.Invoking(s => s.EnqueueAsync(actorId, new Envelope(new Message<string>("m2") { Priority = 1 }, actorRef)))
            .Should().ThrowAsync<Exceptions.MailboxException>();
    }
}
