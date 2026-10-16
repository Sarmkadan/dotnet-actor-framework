// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Tests for <see cref="ActorSystemOptions.Validate"/>, which must report every violation in a single exception.
/// </summary>
public class ActorSystemOptionsTests
{
    /// <summary>
    /// Tests that the default settings pass validation.
    /// </summary>
    [Fact]
    public void Validate_DefaultValues_ShouldNotThrow()
    {
        // Arrange
        var options = ActorSystemOptions.CreateDefault();

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that several simultaneous violations are all reported in one exception.
    /// </summary>
    [Fact]
    public void Validate_MultipleViolations_ShouldReportAllInOneException()
    {
        // Arrange
        var options = new ActorSystemOptions
        {
            SystemName = " ",
            DefaultMailboxCapacity = 0,
            HighWatermarkWarningThreshold = 150,
            DefaultTimeoutSeconds = 0,
            MaxMessageRetries = -1,
            MaxActorDepth = 0,
            SnapshotIntervalSeconds = 0,
            InitialBackoffDelayMs = 0,
            BackoffMultiplier = 1.0
        };

        // Act
        var act = () => options.Validate();

        // Assert
        var exception = act.Should().ThrowExactly<OptionsValidationException>().Which;
        exception.OptionsName.Should().Be(nameof(ActorSystemOptions));
        exception.Failures.Should().HaveCount(9);
        exception.Message.Should().Contain("9 problem(s)");
        foreach (var failure in exception.Failures)
        {
            exception.Message.Should().Contain(failure);
        }
    }

    /// <summary>
    /// Tests that the message names each violated rule, so the user sees every problem at once.
    /// </summary>
    [Fact]
    public void Validate_MultipleViolations_MessageListsEachViolation()
    {
        // Arrange
        var options = new ActorSystemOptions
        {
            DefaultTimeoutSeconds = -5,
            SnapshotIntervalSeconds = 0,
            InitialBackoffDelayMs = -100
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().ThrowExactly<OptionsValidationException>()
            .WithMessage("*Default timeout must be greater than zero.*")
            .WithMessage("*Snapshot interval must be greater than zero.*")
            .WithMessage("*Initial backoff delay must be greater than zero.*");
    }

    /// <summary>
    /// Tests that a single violation is still reported through the same exception type.
    /// </summary>
    [Fact]
    public void Validate_SingleViolation_ShouldReportExactlyThatViolation()
    {
        // Arrange
        var options = new ActorSystemOptions { MaxActorDepth = 0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().ThrowExactly<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle()
            .Which.Should().Be("Max actor depth must be greater than zero.");
    }

    /// <summary>
    /// Tests that non-positive durations are rejected.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveTimeout_ShouldThrowOptionsValidationException(int timeoutSeconds)
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultTimeoutSeconds = timeoutSeconds };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().ThrowExactly<OptionsValidationException>()
            .Which.Failures.Should().ContainSingle()
            .Which.Should().Be("Default timeout must be greater than zero.");
    }

    /// <summary>
    /// Tests that a mailbox that is invalid in several ways reports every mailbox violation, not just the first.
    /// </summary>
    [Fact]
    public void Validate_MultipleMailboxViolations_ShouldReportEachOne()
    {
        // Arrange
        var options = new ActorSystemOptions
        {
            DefaultMailboxCapacity = 0,
            DefaultMailboxType = (MailboxType)999,
            DefaultMailboxOverflowPolicy = (MailboxOverflowPolicy)999
        };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().ThrowExactly<OptionsValidationException>()
            .Which.Failures.Should().HaveCount(3)
            .And.Contain("Mailbox capacity must be greater than zero.");
    }

    /// <summary>
    /// Tests that a valid mailbox configuration is not reported as a violation.
    /// </summary>
    [Fact]
    public void Validate_ValidMailboxSettings_ShouldNotReportMailboxFailures()
    {
        // Arrange
        var options = new ActorSystemOptions { DefaultMailboxCapacity = 1, HighWatermarkWarningThreshold = 0 };

        // Act
        var act = () => options.Validate();

        // Assert
        act.Should().NotThrow();
    }
}
