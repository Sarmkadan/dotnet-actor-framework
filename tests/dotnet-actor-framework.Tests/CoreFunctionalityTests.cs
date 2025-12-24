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

namespace DotNetActorFramework.Tests;

public class CoreFunctionalityTests
{
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

    [Fact]
    public async Task MailboxService_CreateAndEnqueue_ShouldHoldMessage()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 10 };
        var service = new MailboxService(options);
        var actorId = Guid.NewGuid();
        var mailbox = service.CreateMailbox(actorId);
        var envelope = new Envelope(new Message("test-data", 1), actorId);

        // Act
        await service.EnqueueAsync(actorId, envelope);

        // Assert
        service.GetMailboxSize(actorId).Should().Be(1);
    }

    [Fact]
    public async Task MailboxService_EnqueueAndDequeue_ShouldReturnSameMessage()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 10 };
        var service = new MailboxService(options);
        var actorId = Guid.NewGuid();
        service.CreateMailbox(actorId);
        var envelope = new Envelope(new Message("test-data", 1), actorId);
        await service.EnqueueAsync(actorId, envelope);

        // Act
        var dequeued = await service.DequeueAsync(actorId);

        // Assert
        dequeued.Should().NotBeNull();
        dequeued.Should().Be(envelope);
        service.GetMailboxSize(actorId).Should().Be(0);
    }

    [Fact]
    public async Task MailboxService_EnqueueToFullMailbox_ShouldFail()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 1 };
        var service = new MailboxService(options);
        var actorId = Guid.NewGuid();
        service.CreateMailbox(actorId);
        
        await service.EnqueueAsync(actorId, new Envelope(new Message("m1", 1), actorId));

        // Act
        var enqueueResult = await service.EnqueueAsync(actorId, new Envelope(new Message("m2", 1), actorId));

        // Assert
        // The MailboxService.EnqueueAsync method throws MailboxException if EnqueueAsync returns false
        await service.Invoking(s => s.EnqueueAsync(actorId, new Envelope(new Message("m2", 1), actorId)))
            .Should().ThrowAsync<Exceptions.MailboxException>();
    }
}
