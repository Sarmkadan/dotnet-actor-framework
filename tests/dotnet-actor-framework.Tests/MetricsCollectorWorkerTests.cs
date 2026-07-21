// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetActorFramework.BackgroundWorkers;
using DotNetActorFramework.Middleware;
using DotNetActorFramework.Models;
using FluentAssertions;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Contains unit tests for MetricsCollectorWorker.
/// </summary>
public class MetricsCollectorWorkerTests
{
    /// <summary>
    /// Tests that MetricsCollectorWorker correctly initializes with required dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_InitializesCorrectly()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();

        // Act
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);

        // Assert
        worker.Should().NotBeNull();
        worker.WorkerId.Should().Be("metrics-collector");
    }

    /// <summary>
    /// Tests that MetricsCollectorWorker throws ArgumentNullException when actorSystem is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullActorSystem_ThrowsArgumentNullException()
    {
        // Arrange
        var metricsCollector = new MetricsCollector();

        // Act & Assert
        this.Invoking(_ => new MetricsCollectorWorker(null!, metricsCollector))
            .Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that MetricsCollectorWorker throws ArgumentNullException when metricsCollector is null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullMetricsCollector_ThrowsArgumentNullException()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");

        // Act & Assert
        this.Invoking(_ => new MetricsCollectorWorker(actorSystem, null!))
            .Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ExecuteAsync collects and aggregates metrics correctly.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CollectsAndAggregatesMetricsCorrectly()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);

        // Add some metrics to the collector
        metricsCollector.RecordMessageProcessed("/user/test-actor-1", "TestMessage", 100, true);
        metricsCollector.RecordMessageProcessed("/user/test-actor-2", "AnotherMessage", 200, false);
        metricsCollector.RecordMessageProcessed("/user/test-actor-1", "TestMessage", 150, true);

        // Act
        await worker.ExecuteAsync(CancellationToken.None);

        // Assert
        var snapshot = worker.GetLatestSnapshot();
        snapshot.Should().NotBeNull();
        snapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        snapshot.TotalMessages.Should().Be(0); // Actor system has no actors, so TotalMessages is 0
        snapshot.TotalErrors.Should().Be(0); // Actor system has no actors, so TotalErrors is 0
        snapshot.AverageLatencyMs.Should().Be(150.0); // Average of 100, 200, 150 = 150
        snapshot.ErrorRate.Should().BeApproximately(33.33, 0.01); // 1 error out of 3 messages = 33.33%
    }

    /// <summary>
    /// Tests that ExecuteAsync updates the snapshot with health information from actor system.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_IncludesHealthSummaryInSnapshot()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);

        // Act
        await worker.ExecuteAsync(CancellationToken.None);

        // Assert
        var snapshot = worker.GetLatestSnapshot();
        snapshot.Should().NotBeNull();
        snapshot.TotalActors.Should().Be(0); // No actors registered yet
        snapshot.HealthyActors.Should().Be(0);
        snapshot.ErrorActors.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetLatestSnapshot returns the same instance across multiple calls.
    /// </summary>
    [Fact]
    public void GetLatestSnapshot_ReturnsSameInstance()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);

        // Act
        var snapshot1 = worker.GetLatestSnapshot();
        var snapshot2 = worker.GetLatestSnapshot();

        // Assert
        snapshot1.Should().BeSameAs(snapshot2);
    }

    /// <summary>
    /// Tests that cancellation token stops ExecuteAsync execution.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RespectsCancellationToken()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        // Act
        await worker.ExecuteAsync(cts.Token);

        // Assert - The task should complete without throwing even when cancelled
        // The cancellation is handled by the Task.Run inside ExecuteAsync
    }

    /// <summary>
    /// Tests that MetricsSnapshot.IsHealthy correctly evaluates system health.
    /// </summary>
    [Fact]
    public void MetricsSnapshot_IsHealthy_EvaluatesCorrectly()
    {
        // Arrange
        var snapshot = new MetricsSnapshot();

        // Test healthy state (no errors, low error rate)
        snapshot.ErrorActors = 0;
        snapshot.ErrorRate = 4.0; // Less than 5%
        snapshot.IsHealthy.Should().BeTrue();

        // Test unhealthy state (has error actors)
        snapshot.ErrorActors = 1;
        snapshot.ErrorRate = 4.0;
        snapshot.IsHealthy.Should().BeFalse();

        // Test unhealthy state (high error rate)
        snapshot.ErrorActors = 0;
        snapshot.ErrorRate = 5.1; // Greater than or equal to 5%
        snapshot.IsHealthy.Should().BeFalse();
    }

    /// <summary>
    /// Tests that MetricsSnapshot properties are correctly set from ExecuteAsync.
    /// </summary>
    [Fact]
    public async Task MetricsSnapshot_PropertiesAreSetCorrectly()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);

        // Act
        await worker.ExecuteAsync(CancellationToken.None);
        var snapshot = worker.GetLatestSnapshot();

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        snapshot.TotalActors.Should().Be(0); // No actors in system yet
        snapshot.HealthyActors.Should().Be(0);
        snapshot.ErrorActors.Should().Be(0);
        snapshot.TotalMessages.Should().Be(0);
        snapshot.TotalErrors.Should().Be(0);
        snapshot.AverageLatencyMs.Should().Be(0);
        snapshot.ErrorRate.Should().Be(0);
    }

    /// <summary>
    /// Tests that ExecuteAsync can be called multiple times and updates the snapshot each time.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MultipleCalls_UpdatesSnapshotEachTime()
    {
        // Arrange
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        var worker = new MetricsCollectorWorker(actorSystem, metricsCollector);

        // First collection
        await worker.ExecuteAsync(CancellationToken.None);
        var snapshot1 = worker.GetLatestSnapshot();
        var timestamp1 = snapshot1.Timestamp;

        // Wait a bit
        await Task.Delay(10);

        // Second collection
        await worker.ExecuteAsync(CancellationToken.None);
        var snapshot2 = worker.GetLatestSnapshot();

        // Assert
        snapshot2.Should().NotBeNull();
        snapshot2.Timestamp.Should().BeAfter(timestamp1);
        // Timestamps should be different
    }
}
