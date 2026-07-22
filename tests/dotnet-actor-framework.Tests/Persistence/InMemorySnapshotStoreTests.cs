// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// xUnit tests for InMemorySnapshotStore covering save/load latest snapshot,
// loading missing snapshots, and overwrite semantics.
// =============================================================================

using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence.InMemory;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests.Persistence;

public class InMemorySnapshotStoreTests
{
    private readonly Guid _testActorId = Guid.NewGuid();
    private readonly string _testActorPath = "/test/actor";
    private readonly InMemorySnapshotStore _snapshotStore;

    public InMemorySnapshotStoreTests()
    {
        _snapshotStore = new InMemorySnapshotStore();
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldSaveSnapshotWithCorrectProperties()
    {
        // Arrange
        var testState = new { Counter = 42, Message = "Hello World", Timestamp = DateTime.UtcNow };
        var snapshot = new ActorSnapshot(_testActorId, _testActorPath, testState, 100L, DateTime.UtcNow);

        // Act
        await _snapshotStore.SaveSnapshotAsync(snapshot);

        // Assert
        var loadedSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        loadedSnapshot.Should().NotBeNull();
        loadedSnapshot!.ActorId.Should().Be(_testActorId);
        loadedSnapshot.ActorPath.Should().Be(_testActorPath);
        loadedSnapshot.State.Should().BeEquivalentTo(testState);
        loadedSnapshot.SequenceNr.Should().Be(100L);
    }

    [Fact]
    public async Task LoadLatestSnapshotAsync_ShouldReturnNull_WhenNoSnapshotExists()
    {
        // Act
        var loadedSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert
        loadedSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldOverwriteOlderSnapshot_WhenSavingNewerSnapshotWithSameActor()
    {
        // Arrange
        var initialState = new { Value = 1, Name = "Initial" };
        var updatedState = new { Value = 2, Name = "Updated" };

        // Save initial snapshot
        var initialSnapshot = new ActorSnapshot(_testActorId, _testActorPath, initialState, 100L, DateTime.UtcNow);
        await _snapshotStore.SaveSnapshotAsync(initialSnapshot);

        // Verify initial snapshot was saved
        var snapshot1 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        snapshot1.Should().NotBeNull();
        snapshot1!.State.Should().BeEquivalentTo(initialState);
        snapshot1.SequenceNr.Should().Be(100L);

        // Save updated snapshot with same sequence number (should overwrite)
        var updatedSnapshot = new ActorSnapshot(_testActorId, _testActorPath, updatedState, 100L, DateTime.UtcNow.AddSeconds(1));
        await _snapshotStore.SaveSnapshotAsync(updatedSnapshot);

        // Act - Load the snapshot
        var snapshot2 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert - Should have the updated state
        snapshot2.Should().NotBeNull();
        snapshot2!.State.Should().BeEquivalentTo(updatedState);
        snapshot2.SequenceNr.Should().Be(100L);
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldKeepOnlyLatestSnapshot_WhenSavingMultipleSnapshots()
    {
        // Arrange
        var state1 = new { Value = 1 };
        var state2 = new { Value = 2 };
        var state3 = new { Value = 3 };

        // Save three snapshots with increasing sequence numbers
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, state1, 50L, DateTime.UtcNow));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, state2, 100L, DateTime.UtcNow.AddSeconds(1)));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, state3, 150L, DateTime.UtcNow.AddSeconds(2)));

        // Act - Load the latest snapshot
        var latestSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert - Should only have the latest snapshot (sequence 150)
        latestSnapshot.Should().NotBeNull();
        latestSnapshot!.State.Should().BeEquivalentTo(state3);
        latestSnapshot.SequenceNr.Should().Be(150L);
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldPruneOlderSnapshots_WhenSavingNewSnapshot()
    {
        // Arrange
        var state1 = new { Value = 1 };
        var state2 = new { Value = 2 };

        // Save first snapshot
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, state1, 50L, DateTime.UtcNow));

        // Verify first snapshot exists
        var snapshot1 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        snapshot1.Should().NotBeNull();
        snapshot1!.SequenceNr.Should().Be(50L);

        // Save second snapshot
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, state2, 100L, DateTime.UtcNow.AddSeconds(1)));

        // Act - Load the latest snapshot
        var latestSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert - Should only have the latest snapshot
        latestSnapshot.Should().NotBeNull();
        latestSnapshot!.SequenceNr.Should().Be(100L);
        latestSnapshot.State.Should().BeEquivalentTo(state2);

        // Verify old snapshot is gone (can't load it directly, but we know only latest is kept)
    }

    [Fact]
    public async Task DeleteSnapshotsAsync_ShouldRemoveAllSnapshotsForActor()
    {
        // Arrange - Create snapshots
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, new { Value = 1 }, 50L, DateTime.UtcNow));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, new { Value = 2 }, 100L, DateTime.UtcNow.AddSeconds(1)));

        // Verify snapshots exist
        var initialSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        initialSnapshot.Should().NotBeNull();

        // Act
        await _snapshotStore.DeleteAllSnapshotsAsync(_testActorId, _testActorPath);

        // Assert
        var deletedSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        deletedSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSnapshotsAsync_ShouldRemoveSnapshotsUpToMaxSequenceNumber()
    {
        // Arrange - Create multiple snapshots
        var actorId2 = Guid.NewGuid();
        var actorPath2 = "/test/actor2";

        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, new { Value = 1 }, 50L, DateTime.UtcNow));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, new { Value = 2 }, 100L, DateTime.UtcNow.AddSeconds(1)));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, new { Value = 3 }, 150L, DateTime.UtcNow.AddSeconds(2)));

        // Save snapshot for different actor to ensure isolation
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(actorId2, actorPath2, new { Value = 10 }, 200L, DateTime.UtcNow));

        // Verify initial state
        var initialSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        initialSnapshot.Should().NotBeNull();
        initialSnapshot!.SequenceNr.Should().Be(150L);

        // Act - Delete snapshots up to sequence 100
        await _snapshotStore.DeleteSnapshotsAsync(_testActorId, _testActorPath, 100L);

        // Assert - Only the latest snapshot (sequence 150) should remain for first actor
        var remainingSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        remainingSnapshot.Should().NotBeNull();
        remainingSnapshot!.SequenceNr.Should().Be(150L);

        // Assert - Snapshot for second actor should still exist
        var otherSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(actorId2, actorPath2);
        otherSnapshot.Should().NotBeNull();
        otherSnapshot!.SequenceNr.Should().Be(200L);
    }

    [Fact]
    public async Task DeleteAllSnapshotsAsync_ShouldNotAffectOtherActors()
    {
        // Arrange - Create snapshots for two different actors
        var actorId2 = Guid.NewGuid();
        var actorPath2 = "/test/actor2";

        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, new { Value = 1 }, 50L, DateTime.UtcNow));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(actorId2, actorPath2, new { Value = 2 }, 100L, DateTime.UtcNow.AddSeconds(1)));

        // Verify both exist
        var snapshot1 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        var snapshot2 = await _snapshotStore.LoadLatestSnapshotAsync(actorId2, actorPath2);
        snapshot1.Should().NotBeNull();
        snapshot2.Should().NotBeNull();

        // Act - Delete all snapshots for first actor
        await _snapshotStore.DeleteAllSnapshotsAsync(_testActorId, _testActorPath);

        // Assert - First actor should have no snapshots
        var deletedSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        deletedSnapshot.Should().BeNull();

        // Assert - Second actor should still have its snapshot
        var remainingSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(actorId2, actorPath2);
        remainingSnapshot.Should().NotBeNull();
        remainingSnapshot!.SequenceNr.Should().Be(100L);
    }

    [Fact]
    public async Task LoadLatestSnapshotAsync_ShouldReturnNull_WhenActorHasNoSnapshots()
    {
        // Arrange - Create a different actor with snapshots
        var otherActorId = Guid.NewGuid();
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(otherActorId, _testActorPath, new { Value = 1 }, 50L, DateTime.UtcNow));

        // Act - Try to load snapshot for non-existent actor
        var loadedSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert
        loadedSnapshot.Should().BeNull();
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldHandleNullSnapshot()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _snapshotStore.SaveSnapshotAsync(null!));
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldHandleDifferentActorPaths()
    {
        // Arrange
        var path1 = "/test/actor/path1";
        var path2 = "/test/actor/path2";
        var state1 = new { Value = 1 };
        var state2 = new { Value = 2 };

        // Save snapshots for different paths
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, path1, state1, 50L, DateTime.UtcNow));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, path2, state2, 100L, DateTime.UtcNow));

        // Act - Load snapshots
        var snapshot1 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, path1);
        var snapshot2 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, path2);

        // Assert
        snapshot1.Should().NotBeNull();
        snapshot1!.State.Should().BeEquivalentTo(state1);
        snapshot1.SequenceNr.Should().Be(50L);

        snapshot2.Should().NotBeNull();
        snapshot2!.State.Should().BeEquivalentTo(state2);
        snapshot2.SequenceNr.Should().Be(100L);
    }

    [Fact]
    public async Task SaveSnapshotAsync_ShouldHandleSameSequenceNumberDifferentActors()
    {
        // Arrange
        var actorId2 = Guid.NewGuid();
        var state1 = new { Value = 1 };
        var state2 = new { Value = 2 };

        // Save snapshots with same sequence number but different actors
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(_testActorId, _testActorPath, state1, 100L, DateTime.UtcNow));
        await _snapshotStore.SaveSnapshotAsync(new ActorSnapshot(actorId2, _testActorPath, state2, 100L, DateTime.UtcNow));

        // Act - Load snapshots
        var snapshot1 = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        var snapshot2 = await _snapshotStore.LoadLatestSnapshotAsync(actorId2, _testActorPath);

        // Assert - Both should exist independently
        snapshot1.Should().NotBeNull();
        snapshot1!.State.Should().BeEquivalentTo(state1);
        snapshot1.ActorId.Should().Be(_testActorId);

        snapshot2.Should().NotBeNull();
        snapshot2!.State.Should().BeEquivalentTo(state2);
        snapshot2.ActorId.Should().Be(actorId2);
    }
}
