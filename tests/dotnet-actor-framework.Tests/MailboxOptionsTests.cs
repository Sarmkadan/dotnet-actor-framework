// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Services;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Tests for <see cref="MailboxOptions"/> validation and its use at startup and mailbox creation.
/// </summary>
public class MailboxOptionsTests
{
    /// <summary>
    /// Tests that the default mailbox settings pass validation.
    /// </summary>
    [Fact]
    public void Validate_DefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = new MailboxOptions();

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that a non-positive capacity is rejected.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveCapacity_ShouldThrowArgumentOutOfRangeException(int capacity)
    {
        // Arrange
        var options = new MailboxOptions { Capacity = capacity };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be(nameof(MailboxOptions.Capacity));
    }

    /// <summary>
    /// Tests that an overflow policy value outside the enum definition is rejected.
    /// </summary>
    [Fact]
    public void Validate_UnknownOverflowPolicy_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new MailboxOptions { OverflowPolicy = (MailboxOverflowPolicy)999 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be(nameof(MailboxOptions.OverflowPolicy));
    }

    /// <summary>
    /// Tests that a mailbox type value outside the enum definition is rejected.
    /// </summary>
    [Fact]
    public void Validate_UnknownMailboxType_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var options = new MailboxOptions { Type = (MailboxType)999 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be(nameof(MailboxOptions.Type));
    }

    /// <summary>
    /// Tests that a high-watermark threshold outside 0-100 is rejected.
    /// </summary>
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Validate_HighWatermarkOutsideRange_ShouldThrowArgumentOutOfRangeException(int threshold)
    {
        // Arrange
        var options = new MailboxOptions { HighWatermarkWarningThreshold = threshold };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .Which.ParamName.Should().Be(nameof(MailboxOptions.HighWatermarkWarningThreshold));
    }

    /// <summary>
    /// Tests that invalid default mailbox settings fail when the system options are validated at startup.
    /// </summary>
    [Fact]
    public void ActorSystemOptionsValidate_InvalidDefaultMailboxCapacity_ShouldThrow()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that the mailbox service constructor rejects an unknown default overflow policy.
    /// </summary>
    [Fact]
    public void MailboxService_Constructor_UnknownDefaultOverflowPolicy_ShouldThrow()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxOverflowPolicy = (MailboxOverflowPolicy)999 };

        // Act
        var act = () => new MailboxService(options);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that a negative per-mailbox capacity is rejected and no mailbox is registered for the actor.
    /// </summary>
    [Fact]
    public void MailboxService_CreateMailbox_NegativeCapacity_ShouldThrowAndNotRegisterMailbox()
    {
        // Arrange
        var service = new MailboxService(new ActorSystemOptions());
        var actorId = Guid.NewGuid();

        // Act
        var act = () => service.CreateMailbox(actorId, capacity: -5);
        var createWithDefaults = () => service.CreateMailbox(actorId);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
        createWithDefaults.Should().NotThrow();
    }

    /// <summary>
    /// Tests that an unknown per-mailbox overflow policy is rejected at mailbox creation.
    /// </summary>
    [Fact]
    public void MailboxService_CreateMailbox_UnknownOverflowPolicy_ShouldThrow()
    {
        // Arrange
        var service = new MailboxService(new ActorSystemOptions());

        // Act
        var act = () => service.CreateMailbox(Guid.NewGuid(), overflowPolicy: (MailboxOverflowPolicy)999);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
