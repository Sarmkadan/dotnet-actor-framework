namespace DotNetActorFramework.Tests;

using System;
using DotNetActorFramework.Models;
using Xunit;

public class EnvelopeTests
{
    [Fact]
    public void Constructor_ValidArguments_InitializesPropertiesCorrectly()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipientPath = new ActorPath("/recipient");
        var recipient = new ActorRef(recipientPath, Guid.NewGuid());
        var senderPath = new ActorPath("/sender");
        var sender = new ActorRef(senderPath, Guid.NewGuid());

        // Act
        var envelope = new Envelope(message, recipient, sender);

        // Assert
        Assert.Equal(message, envelope.Message);
        Assert.Equal(recipient, envelope.Recipient);
        Assert.Equal(sender, envelope.Sender);
        Assert.NotEqual(Guid.Empty, envelope.EnvelopeId);
        Assert.True(envelope.SentAt <= DateTime.UtcNow);
        Assert.Equal(0, envelope.RetryCount);
        Assert.False(envelope.IsDelivered);
    }

    [Fact]
    public void Constructor_NullSender_InitializesCorrectly()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipientPath = new ActorPath("/recipient");
        var recipient = new ActorRef(recipientPath, Guid.NewGuid());

        // Act
        var envelope = new Envelope(message, recipient, null);

        // Assert
        Assert.Null(envelope.Sender);
        Assert.Equal(recipient, envelope.Recipient);
    }

    [Fact]
    public void Constructor_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var recipientPath = new ActorPath("/recipient");
        var recipient = new ActorRef(recipientPath, Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Envelope(null!, recipient));
    }

    [Fact]
    public void Constructor_NullRecipient_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new ControlMessage("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Envelope(message, null!));
    }

    [Fact]
    public void MarkAsDelivered_SetsIsDeliveredToTrue()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipientPath = new ActorPath("/recipient");
        var recipient = new ActorRef(recipientPath, Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        envelope.MarkAsDelivered();

        // Assert
        Assert.True(envelope.IsDelivered);
    }

    [Fact]
    public void IncrementRetryCount_IncrementsRetryCount()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipientPath = new ActorPath("/recipient");
        var recipient = new ActorRef(recipientPath, Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        envelope.IncrementRetryCount();

        // Assert
        Assert.Equal(1, envelope.RetryCount);
    }
}
