// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ActorMetricsTests
{
    private static ActorMetrics CreateMetrics(string pathStr = "/system/actor")
        => new ActorMetrics(Guid.NewGuid(), new ActorPath(pathStr));

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

public class MessageDeduplicatorTests
{
    [Fact]
    public void IsDuplicate_ForUnregisteredMessageId_ReturnsFalse()
    {
        // Arrange
        var deduplicator = new MessageDeduplicator();
        var newId = Guid.NewGuid();

        // Act & Assert
        deduplicator.IsDuplicate(newId).Should().BeFalse();
    }

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
