// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for Envelope class
// =============================================================================

using DotNetActorFramework.Models;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests.Models;

public class EnvelopeTests
{
    /// <summary>
    /// Tests that Envelope constructor with null message throws ArgumentNullException
    /// </summary>
    [Fact]
    public void Envelope_Constructor_WithNullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Envelope(null!, recipient));
    }

    /// <summary>
    /// Tests that Envelope constructor with null recipient throws ArgumentNullException
    /// </summary>
    [Fact]
    public void Envelope_Constructor_WithNullRecipient_ThrowsArgumentNullException()
    {
        // Arrange
        var message = new ControlMessage("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Envelope(message, null!));
    }

    /// <summary>
    /// Tests that Envelope constructor with valid parameters initializes all properties
    /// </summary>
    [Fact]
    public void Envelope_Constructor_WithValidParameters_InitializesAllProperties()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var sender = new ActorRef(new ActorPath("/user/sender"), Guid.NewGuid());

        // Act
        var envelope = new Envelope(message, recipient, sender);

        // Assert
        envelope.Message.Should().BeSameAs(message);
        envelope.Recipient.Should().BeSameAs(recipient);
        envelope.Sender.Should().BeSameAs(sender);
        envelope.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        envelope.EnvelopeId.Should().NotBe(Guid.Empty);
        envelope.RetryCount.Should().Be(0);
        envelope.IsDelivered.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Envelope constructor with null sender initializes Sender as null
    /// </summary>
    [Fact]
    public void Envelope_Constructor_WithNullSender_InitializesSenderAsNull()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());

        // Act
        var envelope = new Envelope(message, recipient, null);

        // Assert
        envelope.Message.Should().NotBeNull();
        envelope.Recipient.Should().NotBeNull();
        envelope.Sender.Should().BeNull();
        envelope.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        envelope.EnvelopeId.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Tests that Envelope constructor with only required parameters works
    /// </summary>
    [Fact]
    public void Envelope_Constructor_WithRequiredParameters_WorksCorrectly()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());

        // Act
        var envelope = new Envelope(message, recipient);

        // Assert
        envelope.Message.Should().BeSameAs(message);
        envelope.Recipient.Should().BeSameAs(recipient);
        envelope.Sender.Should().BeNull();
        envelope.SentAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        envelope.EnvelopeId.Should().NotBe(Guid.Empty);
        envelope.RetryCount.Should().Be(0);
        envelope.IsDelivered.Should().BeFalse();
    }

    /// <summary>
    /// Tests that EnvelopeId is unique across multiple instances
    /// </summary>
    [Fact]
    public void Envelope_Constructor_EnvelopeIdShouldBeUnique()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());

        // Act
        var envelope1 = new Envelope(message, recipient);
        var envelope2 = new Envelope(message, recipient);
        var envelope3 = new Envelope(message, recipient);

        // Assert
        envelope1.EnvelopeId.Should().NotBe(envelope2.EnvelopeId);
        envelope2.EnvelopeId.Should().NotBe(envelope3.EnvelopeId);
        envelope1.EnvelopeId.Should().NotBe(envelope3.EnvelopeId);
    }

    /// <summary>
    /// Tests that SentAt is set to current UTC time
    /// </summary>
    [Fact]
    public void Envelope_Constructor_SentAtShouldBeUtcNow()
    {
        // Arrange & Act
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Assert
        envelope.SentAt.Kind.Should().Be(DateTimeKind.Utc);
        envelope.SentAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Tests that MarkAsDelivered sets IsDelivered to true
    /// </summary>
    [Fact]
    public void MarkAsDelivered_SetsIsDeliveredToTrue()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        envelope.MarkAsDelivered();

        // Assert
        envelope.IsDelivered.Should().BeTrue();
    }

    /// <summary>
    /// Tests that MarkAsDelivered can be called multiple times
    /// </summary>
    [Fact]
    public void MarkAsDelivered_CanBeCalledMultipleTimes()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        envelope.MarkAsDelivered();
        envelope.MarkAsDelivered();
        envelope.MarkAsDelivered();

        // Assert
        envelope.IsDelivered.Should().BeTrue();
    }

    /// <summary>
    /// Tests that IncrementRetryCount increments the counter
    /// </summary>
    [Fact]
    public void IncrementRetryCount_IncrementsRetryCount()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        envelope.IncrementRetryCount();

        // Assert
        envelope.RetryCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that IncrementRetryCount can be called multiple times
    /// </summary>
    [Fact]
    public void IncrementRetryCount_CanBeCalledMultipleTimes()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        envelope.IncrementRetryCount();
        envelope.IncrementRetryCount();
        envelope.IncrementRetryCount();

        // Assert
        envelope.RetryCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetElapsedTime returns a positive TimeSpan
    /// </summary>
    [Fact]
    public void GetElapsedTime_ReturnsPositiveTimeSpan()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        var elapsed = envelope.GetElapsedTime();

        // Assert
        elapsed.Should().BePositive();
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that GetElapsedTime returns increasing values over time
    /// </summary>
    [Fact]
    public void GetElapsedTime_ReturnsIncreasingValues()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act - wait a bit
        Thread.Sleep(10);
        var elapsed1 = envelope.GetElapsedTime();
        Thread.Sleep(10);
        var elapsed2 = envelope.GetElapsedTime();

        // Assert
        elapsed2.Should().BeGreaterThan(elapsed1);
    }

    /// <summary>
    /// Tests that HasExceededRetryLimit returns false when retry count is below limit
    /// </summary>
    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    [InlineData(3, 3)]
    public void HasExceededRetryLimit_ReturnsFalse_WhenRetryCountBelowLimit(int retryCount, int maxRetries)
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Set retry count
        for (int i = 0; i < retryCount; i++)
        {
            envelope.IncrementRetryCount();
        }

        // Act
        var hasExceeded = envelope.HasExceededRetryLimit(maxRetries);

        // Assert
        hasExceeded.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasExceededRetryLimit returns true when retry count exceeds limit
    /// </summary>
    [Theory]
    [InlineData(4, 3)]
    [InlineData(5, 3)]
    [InlineData(10, 3)]
    public void HasExceededRetryLimit_ReturnsTrue_WhenRetryCountExceedsLimit(int retryCount, int maxRetries)
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Set retry count
        for (int i = 0; i < retryCount; i++)
        {
            envelope.IncrementRetryCount();
        }

        // Act
        var hasExceeded = envelope.HasExceededRetryLimit(maxRetries);

        // Assert
        hasExceeded.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasExceededRetryLimit uses default value of 3 when no parameter provided
    /// </summary>
    [Fact]
    public void HasExceededRetryLimit_UsesDefaultValueOf3()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Set retry count to 4 (exceeds default limit of 3)
        envelope.IncrementRetryCount();
        envelope.IncrementRetryCount();
        envelope.IncrementRetryCount();
        envelope.IncrementRetryCount();

        // Act
        var hasExceeded = envelope.HasExceededRetryLimit();

        // Assert
        hasExceeded.Should().BeTrue();
    }

    /// <summary>
    /// Tests that GetDeliveryPriority returns a positive value
    /// </summary>
    [Fact]
    public void GetDeliveryPriority_ReturnsPositiveValue()
    {
        // Arrange
        var message = new ControlMessage("test") { Priority = 5 };
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act
        var priority = envelope.GetDeliveryPriority();

        // Assert
        priority.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that GetDeliveryPriority incorporates message priority
    /// </summary>
    [Fact]
    public void GetDeliveryPriority_IncorporatesMessagePriority()
    {
        // Arrange
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var messageLow = new ControlMessage("test") { Priority = 1 };
        var messageHigh = new ControlMessage("test") { Priority = 10 };
        var envelopeLow = new Envelope(messageLow, recipient);
        var envelopeHigh = new Envelope(messageHigh, recipient);

        // Act
        var priorityLow = envelopeLow.GetDeliveryPriority();
        var priorityHigh = envelopeHigh.GetDeliveryPriority();

        // Assert
        priorityHigh.Should().BeGreaterThan(priorityLow);
    }

    /// <summary>
    /// Tests that ToString returns a non-empty string with expected format
    /// </summary>
    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test-actor"), Guid.NewGuid());
        var sender = new ActorRef(new ActorPath("/user/sender-actor"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient, sender);

        // Act
        var result = envelope.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("sender-actor");
        result.Should().Contain("test-actor");
        result.Should().Contain("ControlMessage");
        result.Should().Contain(envelope.EnvelopeId.ToString("N"));
    }

    /// <summary>
    /// Tests that ToString handles null sender correctly
    /// </summary>
    [Fact]
    public void ToString_HandlesNullSender()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test-actor"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient, null);

        // Act
        var result = envelope.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("System"); // Should show "System" when sender is null
        result.Should().Contain("test-actor");
        result.Should().Contain("ControlMessage");
    }

    /// <summary>
    /// Tests envelope with different message types
    /// </summary>
    [Fact]
    public void Envelope_WorksWithDifferentMessageTypes()
    {
        // Arrange
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());

        // Act
        var controlEnvelope = new Envelope(new ControlMessage("test"), recipient);
        var responseEnvelope = new Envelope(new ResponseMessage("response"), recipient);
        var failureEnvelope = new Envelope(new FailureMessage("reason"), recipient);
        var typedEnvelope = new Envelope(new Message<string>("payload"), recipient);

        // Assert - all should be created successfully
        controlEnvelope.Should().NotBeNull();
        responseEnvelope.Should().NotBeNull();
        failureEnvelope.Should().NotBeNull();
        typedEnvelope.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that RetryCount is immutable (no setter)
    /// </summary>
    [Fact]
    public void Envelope_RetryCountShouldBeImmutable()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act - increment via method
        envelope.IncrementRetryCount();

        // Assert - should only change via IncrementRetryCount method
        envelope.RetryCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that IsDelivered is immutable (no setter)
    /// </summary>
    [Fact]
    public void Envelope_IsDeliveredShouldBeImmutable()
    {
        // Arrange
        var message = new ControlMessage("test");
        var recipient = new ActorRef(new ActorPath("/user/test"), Guid.NewGuid());
        var envelope = new Envelope(message, recipient);

        // Act - mark as delivered
        envelope.MarkAsDelivered();

        // Assert - should only change via MarkAsDelivered method
        envelope.IsDelivered.Should().BeTrue();
    }
}
