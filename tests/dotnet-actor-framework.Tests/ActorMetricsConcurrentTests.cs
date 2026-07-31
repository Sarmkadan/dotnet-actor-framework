// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using FluentAssertions;
using Xunit;

/// <summary>
/// Concurrent tests for the ActorMetrics class.
/// </summary>
public class ActorMetricsConcurrentTests
{
    [Fact]
    public async Task ConcurrentRecordMessageReceived_UpdatesCountCorrectly()
    {
        // Arrange
        var metrics = new ActorMetrics(Guid.NewGuid(), new ActorPath("/test/actor"));
        int iterations = 1000;
        int taskCount = 10;
        
        // Act
        await Task.WhenAll(Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                metrics.RecordMessageReceived();
            }
        })));
        
        // Assert
        metrics.MessageCount.Should().Be(taskCount * iterations);
    }

    [Fact]
    public async Task ConcurrentRecordError_UpdatesCountCorrectly()
    {
        // Arrange
        var metrics = new ActorMetrics(Guid.NewGuid(), new ActorPath("/test/actor"));
        int iterations = 1000;
        int taskCount = 10;
        
        // Act
        await Task.WhenAll(Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                metrics.RecordError();
            }
        })));
        
        // Assert
        metrics.ErrorCount.Should().Be(taskCount * iterations);
    }
}
