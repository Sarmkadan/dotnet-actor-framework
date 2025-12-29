// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the ActorMetrics class.
/// </summary>
public class ActorMetricsTests
{
    /// <summary>
    /// Creates a new instance of ActorMetrics with a specified path.
    /// </summary>
    /// <param name="pathStr">The path of the actor.</param>
    /// <returns>A new instance of ActorMetrics.</returns>
    private static ActorMetrics CreateMetrics(string pathStr = "/system/actor")
        => new ActorMetrics(Guid.NewGuid(), new ActorPath(pathStr));

    /// <summary>
    /// Verifies that RecordMessageReceived increments the message count.
    /// </summary>
    [Fact]
    public void RecordMessageReceived_CalledMultipleTimes_IncrementsMessageCount()
    {
        // Arrange
        var metrics = CreateMetrics();

        // Act
        metrics.RecordMessageReceived();
        metrics.RecordMessageReceived();
        metrics.RecordMessageReceived();

        // Assert
        metrics.MessageCount.Should().Be(3);
        metrics.LastMessageTime.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that GetErrorRate returns the correct error rate when there are errors.
    /// </summary>
    [Fact]
    public void GetErrorRate_WithFiftyPercentErrors_ReturnsFiftyPercent()
    {
        // Arrange
        var metrics = CreateMetrics();
        metrics.RecordMessageReceived();
        metrics.RecordMessageReceived();
        metrics.RecordError();

        // Act
        var errorRate = metrics.GetErrorRate();

        // Assert
        errorRate.Should().BeApproximately(50.0, 0.001);
        metrics.GetSuccessRate().Should().BeApproximately(50.0, 0.001);
    }

    /// <summary>
    /// Verifies that GetErrorRate returns 0 when there are no messages.
    /// </summary>
    [Fact]
    public void GetErrorRate_WithNoMessages_ReturnsZeroWithoutDivisionError()
    {
        // Arrange
        var metrics = CreateMetrics();

        // Act & Assert – must not throw DivideByZeroException
        var act = metrics.GetErrorRate;
        act.Should().NotThrow();
        metrics.GetErrorRate().Should().Be(0);
    }

    /// <summary>
    /// Verifies that RecordProcessingTime averages the processing times correctly.
    /// </summary>
    [Fact]
    public void RecordProcessingTime_WithThreeTimings_AveragesCorrectly()
    {
        // Arrange
        var metrics = CreateMetrics();

        // Act
        metrics.RecordProcessingTime(100);
        metrics.RecordProcessingTime(200);
        metrics.RecordProcessingTime(300);

        // Assert
        metrics.ProcessedCount.Should().Be(3);
        metrics.AverageProcessingTimeMs.Should().BeApproximately(200.0, 0.001);
    }

    /// <summary>
    /// Verifies that IsUnhealthy returns true when the error rate exceeds the threshold.
    /// </summary>
    [Fact]
    public void IsUnhealthy_WhenErrorRateExceedsThreshold_ReturnsTrue()
    {
        // Arrange – 10 messages, 8 errors → 80 % error rate
        var metrics = CreateMetrics();
        for (var i = 0; i < 10; i++) metrics.RecordMessageReceived();
        for (var i = 0; i < 8; i++) metrics.RecordError();

        // Act & Assert
        metrics.IsUnhealthy(25.0).Should().BeTrue("80 % > 25 % threshold");
        metrics.IsUnhealthy(90.0).Should().BeFalse("80 % < 90 % threshold");
    }

    /// <summary>
    /// Verifies that GetSummary returns the correct summary.
    /// </summary>
    [Fact]
    public void GetSummary_ReflectsCurrentMetricState()
    {
        // Arrange
        var path = new ActorPath("/system/actor");
        var metrics = new ActorMetrics(Guid.NewGuid(), path);
        metrics.RecordMessageReceived();
        metrics.RecordError();
        metrics.RecordProcessingTime(150);

        // Act
        var summary = metrics.GetSummary();

        // Assert
        summary.ActorPath.Should().Be(path);
        summary.MessageCount.Should().Be(1);
        summary.ErrorCount.Should().Be(1);
        summary.ProcessedCount.Should().Be(1);
        summary.AverageProcessingTimeMs.Should().BeApproximately(150.0, 0.001);
    }
}

/// <summary>
/// Tests for the MessageDeduplicator class.
/// </summary>
public class MessageDeduplicatorTests
{
    /// <summary>
    /// Verifies that IsDuplicate returns false for an unregistered message ID.
    /// </summary>
    [Fact]
    public void IsDuplicate_ForUnregisteredMessageId_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new MessageDeduplicator();
        var newId = Guid.NewGuid();

        // Act & Assert
        deduplicator.IsDuplicate(newId).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsDuplicate returns true for a registered message ID.
    /// </summary>
    [Fact]
    public void IsDuplicate_AfterRegisterMessage_ReturnsTrueForSameId()
    {
        // Arrange
        var deduplicator = new MessageDeduplicator();
        var id = Guid.NewGuid();

        // Act
        deduplicator.RegisterMessage(id);

        // Assert
        deduplicator.IsDuplicate(id).Should().BeTrue();
        deduplicator.IsDuplicate(Guid.NewGuid()).Should().BeFalse("different IDs must not match");
    }

    /// <summary>
    /// Verifies that Clear removes all registered message IDs.
    /// </summary>
    [Fact]
    public void Clear_AfterRegisteringMultipleIds_RemovesAllRecords()
    {
        // Arrange
        var deduplicator = new MessageDeduplicator();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        deduplicator.RegisterMessage(id1);
        deduplicator.RegisterMessage(id2);

        // Act
        deduplicator.Clear();

        // Assert
        deduplicator.IsDuplicate(id1).Should().BeFalse();
        deduplicator.IsDuplicate(id2).Should().BeFalse();
    }
}
