// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.BackgroundWorkers;
using DotNetActorFramework.Middleware;
using DotNetActorFramework.Models;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class MetricsCollectorWorkerExtensionsTests
{
    private MetricsCollectorWorker CreateWorker()
    {
        var actorSystem = new ActorSystem("test-system");
        var metricsCollector = new MetricsCollector();
        return new MetricsCollectorWorker(actorSystem, metricsCollector);
    }

    [Fact]
    public void CloneLatestSnapshot_ThrowsArgumentNullException_WhenWorkerIsNull()
    {
        Action act = () => ((MetricsCollectorWorker)null!).CloneLatestSnapshot();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CloneLatestSnapshot_ReturnsCloneOfSnapshot()
    {
        var worker = CreateWorker();
        var snapshot = worker.CloneLatestSnapshot();
        snapshot.Should().NotBeNull();
        snapshot.Should().NotBeSameAs(worker.GetLatestSnapshot());
    }

    [Fact]
    public void GetHealthPercentage_ThrowsArgumentNullException_WhenWorkerIsNull()
    {
        Action act = () => ((MetricsCollectorWorker)null!).GetHealthPercentage();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHealthPercentage_ReturnsCorrectPercentage()
    {
        var worker = CreateWorker();
        var health = worker.GetHealthPercentage();
        health.Should().BeInRange(0, 100);
    }

    [Fact]
    public void GetFormattedMetrics_ThrowsArgumentNullException_WhenWorkerIsNull()
    {
        Action act = () => ((MetricsCollectorWorker)null!).GetFormattedMetrics();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetFormattedMetrics_ReturnsFormattedString()
    {
        var worker = CreateWorker();
        var metrics = worker.GetFormattedMetrics();
        metrics.Should().NotBeNullOrWhiteSpace();
        metrics.Should().Contain("Metrics Snapshot");
    }

    [Fact]
    public void ToJson_ThrowsArgumentNullException_WhenWorkerIsNull()
    {
        Action act = () => ((MetricsCollectorWorker)null!).ToJson();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToJson_ReturnsJsonString()
    {
        var worker = CreateWorker();
        var json = worker.ToJson();
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"totalActors\":");
    }
}
