// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Repository;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Tests for the ActorMetricsRepository class.
/// </summary>
public class ActorMetricsRepositoryTests
{
    private readonly ConnectionManager _connectionManager;
    private readonly ActorMetricsRepository _repository;

    public ActorMetricsRepositoryTests()
    {
        // ConnectionManager is a dependency for ActorMetricsRepository
        // Based on its usage, it seems to just be a placeholder for dependencies.
        _connectionManager = new ConnectionManager(); 
        _repository = new ActorMetricsRepository(_connectionManager);
    }

    private static ActorMetricsSummary CreateSampleSummary(string path = "/system/actor")
        => new()
        {
            ActorPath = new ActorPath(path),
            MessageCount = 10,
            ProcessedCount = 5,
            ErrorCount = 2,
            ErrorRate = 20.0,
            SuccessRate = 80.0,
            AverageProcessingTimeMs = 50.0
        };

    [Fact]
    public async Task RecordMetricsAsync_ValidInput_ReturnsTrue()
    {
        var actorId = Guid.NewGuid();
        var summary = CreateSampleSummary();

        var result = await _repository.RecordMetricsAsync(actorId, summary);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task RecordMetricsAsync_EmptyActorId_ThrowsArgumentException()
    {
        var summary = CreateSampleSummary();

        await Assert.ThrowsAsync<ArgumentException>(async () => await _repository.RecordMetricsAsync(Guid.Empty, summary));
    }

    [Fact]
    public async Task GetHistoryAsync_AfterRecording_ReturnsSnapshots()
    {
        var actorId = Guid.NewGuid();
        var summary = CreateSampleSummary();
        await _repository.RecordMetricsAsync(actorId, summary);

        var history = await _repository.GetHistoryAsync(actorId);

        history.Should().NotBeEmpty();
        history.First().ActorId.Should().Be(actorId);
    }

    [Fact]
    public async Task GetAggregateMetricsAsync_WithMultipleActors_AggregatesCorrectly()
    {
        var actorId1 = Guid.NewGuid();
        var actorId2 = Guid.NewGuid();
        
        await _repository.RecordMetricsAsync(actorId1, CreateSampleSummary("/a1"));
        await _repository.RecordMetricsAsync(actorId2, CreateSampleSummary("/a2"));

        var aggregate = await _repository.GetAggregateMetricsAsync();

        aggregate.TotalActorsTracked.Should().Be(2);
        aggregate.TotalSnapshots.Should().Be(2);
    }

    [Fact]
    public void Clear_RemovesAllHistory()
    {
        var actorId = Guid.NewGuid();
        _repository.RecordMetricsAsync(actorId, CreateSampleSummary()).Wait();

        _repository.Clear();

        var history = _repository.GetHistoryAsync(actorId).Result;
        history.Should().BeEmpty();
    }
}
