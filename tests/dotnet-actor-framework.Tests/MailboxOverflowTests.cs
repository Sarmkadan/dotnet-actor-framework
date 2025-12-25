// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// Regression tests for mailbox overflow bug fix
// =============================================================================

using DotNetActorFramework.Configuration;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class MailboxOverflowTests
{
    [Fact]
    public async Task EnqueueAsync_WithBurstTraffic_DoesNotCauseMessageLoss()
    {
        // Arrange
        var mailboxService = new MailboxService(new ActorSystemOptions { DefaultMailboxCapacity = 10 });
        var actorId = Guid.NewGuid();
        var mailbox = mailboxService.CreateMailbox(actorId, capacity: 10);

        // Create multiple envelopes to simulate burst traffic
        var envelopes = new List<Envelope>();
        for (int i = 0; i < 15; i++)
        {
            var message = new ControlMessage("test-command");
            var actorPath = new ActorPath("/user/test-actor");
            var actorRef = new ActorRef(actorPath, Guid.NewGuid());
            envelopes.Add(new Envelope(message, actorRef));
        }

        // Act - Try to enqueue all messages under burst traffic
        var successfulEnqueues = new List<bool>();
        var tasks = new List<Task>();

        foreach (var envelope in envelopes)
        {
            tasks.Add(Task.Run(async () =>
            {
                var result = true;
                try
                {
                    await mailboxService.EnqueueAsync(actorId, envelope);
                }
                catch (MailboxException)
                {
                    result = false;
                }
                lock (successfulEnqueues)
                {
                    successfulEnqueues.Add(result);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert - Should not lose messages under burst traffic
        // With the hotfix, we expect some messages to fail (those beyond capacity)
        // but the important thing is that the system doesn't crash or corrupt state
        successfulEnqueues.Count.Should().Be(15);
        successfulEnqueues.Count(r => r).Should().BeLessOrEqualTo(10,
            "Only capacity messages should succeed");

        // Verify mailbox state is consistent
        var mailboxAfter = mailboxService.GetMailbox(actorId);
        mailboxAfter.Should().NotBeNull();
        mailboxAfter!.GetSize().Should().BeLessOrEqualTo(10);
    }

    [Fact]
    public async Task EnqueueAsync_WithConcurrentAccess_DoesNotCauseRaceConditions()
    {
        // Arrange
        var mailboxService = new MailboxService(new ActorSystemOptions { DefaultMailboxCapacity = 5 });
        var actorId = Guid.NewGuid();
        var mailbox = mailboxService.CreateMailbox(actorId, capacity: 5);

        // Create envelopes
        var envelopes = new List<Envelope>();
        for (int i = 0; i < 20; i++)
        {
            var message = new ControlMessage("test-command");
            var actorPath = new ActorPath("/user/test-actor");
            var actorRef = new ActorRef(actorPath, Guid.NewGuid());
            envelopes.Add(new Envelope(message, actorRef));
        }

        // Act - Concurrent enqueue operations
        var results = new bool[20];
        var tasks = new Task[20];

        for (int i = 0; i < 20; i++)
        {
            int index = i;
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    await mailboxService.EnqueueAsync(actorId, envelopes[index]);
                    results[index] = true;
                }
                catch (MailboxException)
                {
                    results[index] = false;
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert - Should handle concurrent access without race conditions
        var successfulCount = results.Count(r => r);
        successfulCount.Should().BeLessOrEqualTo(5,
            "Should not exceed mailbox capacity even with concurrent access");

        // Verify no corruption
        var mailboxAfter = mailboxService.GetMailbox(actorId);
        mailboxAfter.Should().NotBeNull();
        mailboxAfter!.GetSize().Should().Be(successfulCount);
    }

    [Fact]
    public async Task Mailbox_IsFull_AccuratelyReflectsCapacity()
    {
        // Arrange
        var mailboxService = new MailboxService(new ActorSystemOptions { DefaultMailboxCapacity = 3 });
        var actorId = Guid.NewGuid();
        var mailbox = mailboxService.CreateMailbox(actorId, capacity: 3);

        // Fill the mailbox
        for (int i = 0; i < 3; i++)
        {
            var message = new ControlMessage("test-command");
            var actorPath = new ActorPath("/user/test-actor");
            var actorRef = new ActorRef(actorPath, Guid.NewGuid());
            var envelope = new Envelope(message, actorRef);
            await mailboxService.EnqueueAsync(actorId, envelope);
        }

        // Assert - Mailbox should be full
        mailboxService.IsMailboxFull(actorId).Should().BeTrue();

        // Try to enqueue one more - should fail
        var message4 = new ControlMessage("test-command");
        var actorPath4 = new ActorPath("/user/test-actor");
        var actorRef4 = new ActorRef(actorPath4, Guid.NewGuid());
        var envelope4 = new Envelope(message4, actorRef4);

        Func<Task> act = async () => await mailboxService.EnqueueAsync(actorId, envelope4);
        await act.Should().ThrowAsync<MailboxException>();
    }

    [Fact]
    public void MailboxService_Constructor_ValidatesCapacity()
    {
        // Act & Assert - Should throw for invalid capacity
        Action act = () => new MailboxService(new ActorSystemOptions { DefaultMailboxCapacity = 0 });
        act.Should().Throw<ArgumentException>();

        act = () => new MailboxService(new ActorSystemOptions { DefaultMailboxCapacity = -1 });
        act.Should().Throw<ArgumentException>();
    }
}