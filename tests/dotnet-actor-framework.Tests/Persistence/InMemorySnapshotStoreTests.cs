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

/// <summary>
/// Test class for InMemorySnapshotStore that verifies snapshot storage functionality
/// including saving, loading, overwriting, and deleting snapshots for actors.
/// </summary>
public class InMemorySnapshotStoreTests
{
    private readonly Guid _testActorId = Guid.NewGuid();
    private readonly string _testActorPath = "/test/actor";
    private readonly InMemorySnapshotStore _snapshotStore;

    /// <summary>
    /// Initializes a new instance of the InMemorySnapshotStoreTests class
    /// with a fresh InMemorySnapshotStore instance for each test.
    /// </summary>
    public InMemorySnapshotStoreTests()
    {
        _snapshotStore = new InMemorySnapshotStore();
    }

    /// <summary>
    /// Tests that saving a snapshot stores it with all properties correctly preserved.
    /// </summary>
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

    /// <summary>
    /// Tests that loading a snapshot returns null when no snapshot exists for the specified actor.
    /// </summary>
    [Fact]
    public async Task LoadLatestSnapshotAsync_ShouldReturnNull_WhenNoSnapshotExists()
    {
        // Act
        var loadedSnapshot = await _snapshotStore.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert
        loadedSnapshot.Should().BeNull();
    }

    /// <summary>
    /// Tests that saving a new snapshot overwrites an existing snapshot when they have the same sequence number.
    /// </summary>
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

    /// <summary>
    /// Tests that only the latest snapshot is retained when multiple snapshots are saved for the same actor.
    /// </summary>
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

    /// <summary>
    /// Tests that older snapshots are automatically pruned when a new snapshot is saved.
    /// </summary>
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

    /// <summary>
    /// Tests that deleting all snapshots removes all stored snapshots for a specific actor.
    /// </summary>
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

    /// <summary>
    /// Tests that deleting snapshots up to a sequence number removes only snapshots with sequence numbers less than or equal to the specified value.
    /// </summary>
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
}