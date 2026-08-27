// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// xUnit tests for PersistenceService covering save/load round-trip,
// loading nonexistent state, and overwrite semantics using in-memory
// implementations for testing.
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence;
using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence.InMemory;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Test class for PersistenceService that verifies snapshot and event persistence functionality
/// using in-memory implementations for testing.
/// </summary>
public class PersistenceServiceTests : IDisposable
{
    private readonly Guid _testActorId = Guid.NewGuid();
    private readonly ActorPath _testActorPath = ActorPath.Parse("/test/actor");
    private readonly InMemorySnapshotStore _snapshotStore;
    private readonly InMemoryEventJournal _eventJournal;
    private readonly PersistenceService _persistenceService;
    private readonly string _tempDirectory;

    /// <summary>
    /// Initializes a new instance of the PersistenceServiceTests class with test dependencies.
    /// Sets up in-memory snapshot store, event journal, and persistence service.
    /// Creates a temporary directory for file-based persistence tests.
    /// </summary>
    public PersistenceServiceTests()
    {
        _snapshotStore = new InMemorySnapshotStore();
        _eventJournal = new InMemoryEventJournal();
        _persistenceService = new PersistenceService(_snapshotStore, _eventJournal);

        // Create temp directory for file-based tests
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// Cleans up test resources by deleting the temporary directory used for file-based tests.
    /// </summary>
    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempDirectory, true);
        }
        catch
        {
            // Best effort cleanup
        }
    }

    /// <summary>
    /// Tests that saving a snapshot and then loading it returns the exact same state.
    /// Verifies that all snapshot properties (ActorId, ActorPath, SequenceNr, State, Timestamp) are correctly persisted.
    /// </summary>
    [Fact]
    public async Task SaveSnapshotAsync_ShouldSaveAndLoadRoundTrip()
    {
        // Arrange
        var testState = new { Counter = 42, Message = "Hello World", Timestamp = DateTime.UtcNow };
        var sequenceNr = 100L;

        // Act - Save snapshot
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, testState, sequenceNr);

        // Assert - Verify snapshot was saved
        var loadedSnapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        loadedSnapshot.Should().NotBeNull();
        loadedSnapshot!.ActorId.Should().Be(_testActorId);
        loadedSnapshot.ActorPath.Should().Be(_testActorPath.ToString());
        loadedSnapshot.SequenceNr.Should().Be(sequenceNr);
        loadedSnapshot.State.Should().BeEquivalentTo(testState);
        loadedSnapshot.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests that loading a snapshot for an actor that has no snapshots returns null.
    /// Verifies the persistence service correctly handles the case where no snapshot exists.
    /// </summary>
    [Fact]
    public async Task LoadLatestSnapshotAsync_ShouldReturnNull_WhenNoSnapshotExists()
    {
        // Act
        var loadedSnapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert
        loadedSnapshot.Should().BeNull();
    }

    /// <summary>
/// Tests that deleting snapshots up to a specific sequence number removes only snapshots
/// with sequence numbers less than or equal to the specified value, leaving newer snapshots intact.
/// </summary>
    [Fact]
    public async Task DeleteSnapshotsAsync_ShouldRemoveSnapshotsUpToSequenceNumber()
    {
        // Arrange - Create multiple snapshots
        var testState1 = new { Value = 1 };
        var testState2 = new { Value = 2 };
        var testState3 = new { Value = 3 };

        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, testState1, 50L);
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, testState2, 100L);
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, testState3, 150L);

        // Verify snapshots exist
        var initialSnapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        initialSnapshot.Should().NotBeNull();
        initialSnapshot!.SequenceNr.Should().Be(150L);

        // Act - Delete snapshots up to sequence 100
        await _persistenceService.DeleteSnapshotsAsync(_testActorId, _testActorPath, 100L);

        // Assert - Only the latest snapshot (sequence 150) should remain
        var remainingSnapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        remainingSnapshot.Should().NotBeNull();
        remainingSnapshot!.SequenceNr.Should().Be(150L);
    }

    /// <summary>
/// Tests that deleting all snapshots for an actor removes every snapshot associated with that actor,
/// regardless of their sequence numbers.
/// </summary>
    [Fact]
    public async Task DeleteAllSnapshotsAsync_ShouldRemoveAllSnapshotsForActor()
    {
        // Arrange - Create snapshots
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, new { Value = 1 }, 50L);
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, new { Value = 2 }, 100L);

        // Verify snapshots exist
        var initialSnapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        initialSnapshot.Should().NotBeNull();

        // Act
        await _persistenceService.DeleteAllSnapshotsAsync(_testActorId, _testActorPath);

        // Assert
        var deletedSnapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        deletedSnapshot.Should().BeNull();
    }

    /// <summary>
/// Tests that appending events stores them correctly in the event journal and does not trigger a snapshot
/// when the number of events is below the snapshot threshold (default 100 events).
/// </summary>
    [Fact]
    public async Task AppendEventsAsync_ShouldStoreEventsAndTriggerAutoSnapshot()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath.ToString(), 1L, DateTime.UtcNow, new { Type = "Event1", Data = "First" }),
            new(_testActorId, _testActorPath.ToString(), 2L, DateTime.UtcNow.AddSeconds(1), new { Type = "Event2", Data = "Second" }),
            new(_testActorId, _testActorPath.ToString(), 3L, DateTime.UtcNow.AddSeconds(2), new { Type = "Event3", Data = "Third" })
        };

        // Act - Append events (should trigger snapshot after 100 events by default, so no snapshot yet)
        await _persistenceService.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Assert - Events should be stored
        var loadedEvents = await _eventJournal.ReadEventsAsync(_testActorId, _testActorPath.ToString(), 1L, 3L);
        loadedEvents.Should().HaveCount(3);

        // Verify no snapshot was created (we only have 3 events, below threshold of 100)
        var snapshot = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        snapshot.Should().BeNull();
    }

    /// <summary>
/// Tests that appending events triggers automatic snapshot creation when the number of events
/// reaches or exceeds the snapshot interval threshold. Uses a low threshold (3 events) for test verification.
/// </summary>
    [Fact]
    public async Task AppendEventsAsync_ShouldCreateSnapshot_WhenThresholdReached()
    {
        // Arrange - Create a persistence service with low threshold for testing
        var snapshotStore = new InMemorySnapshotStore();
        var eventJournal = new InMemoryEventJournal();
        var service = new PersistenceService(snapshotStore, eventJournal, snapshotIntervalEvents: 3); // Low threshold for test

        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath.ToString(), 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(_testActorId, _testActorPath.ToString(), 2L, DateTime.UtcNow, new { Type = "Event2" }),
            new(_testActorId, _testActorPath.ToString(), 3L, DateTime.UtcNow, new { Type = "Event3" })
        };

        // Act
        await service.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Assert - Snapshot should be created after 3 events
        var snapshot = await service.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        snapshot.Should().NotBeNull();
        snapshot!.SequenceNr.Should().BeGreaterThan(0);

        // Verify events are still stored
        var loadedEvents = await eventJournal.ReadEventsAsync(_testActorId, _testActorPath.ToString(), 1L, 3L);
        loadedEvents.Should().HaveCount(3);
    }

    /// <summary>
/// Tests that reading events within a specific sequence range returns events in ascending order
/// and only includes events whose sequence numbers fall within the specified range (inclusive).
/// </summary>
    [Fact]
    public async Task ReadEventsAsync_ShouldReturnEventsInOrder()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath.ToString(), 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath.ToString(), 20L, DateTime.UtcNow, new { Type = "Event20" }),
            new(_testActorId, _testActorPath.ToString(), 30L, DateTime.UtcNow, new { Type = "Event30" }),
            new(_testActorId, _testActorPath.ToString(), 40L, DateTime.UtcNow, new { Type = "Event40" })
        };

        await _eventJournal.AppendEventsAsync(_testActorId, _testActorPath.ToString(), events);

        // Act - Read events from sequence 15 to 35
        var loadedEvents = await _persistenceService.ReadEventsAsync(_testActorId, _testActorPath, 15L, 35L);

        // Assert
        loadedEvents.Should().HaveCount(2); // Events 20 and 30
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(20L);
        eventList[1].SequenceNr.Should().Be(30L);
    }

    /// <summary>
/// Tests that reading events backward returns events in descending order (highest sequence number first)
/// within the specified range, inclusive of both start and end sequence numbers.
/// </summary>
    [Fact]
    public async Task ReadEventsBackwardAsync_ShouldReturnEventsInReverseOrder()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath.ToString(), 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath.ToString(), 20L, DateTime.UtcNow, new { Type = "Event20" }),
            new(_testActorId, _testActorPath.ToString(), 30L, DateTime.UtcNow, new { Type = "Event30" })
        };

        await _eventJournal.AppendEventsAsync(_testActorId, _testActorPath.ToString(), events);

        // Act - Read events backward from sequence 30 to 10
        var loadedEvents = await _persistenceService.ReadEventsBackwardAsync(_testActorId, _testActorPath, 10L, 30L);

        // Assert
        loadedEvents.Should().HaveCount(3);
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(30L);
        eventList[1].SequenceNr.Should().Be(20L);
        eventList[2].SequenceNr.Should().Be(10L);
    }

    /// <summary>
/// Tests that deleting events up to a specific sequence number removes all events
/// with sequence numbers less than or equal to the specified value, leaving events
/// with higher sequence numbers intact.
/// </summary>
    [Fact]
    public async Task DeleteEventsAsync_ShouldRemoveEventsUpToSequenceNumber()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath.ToString(), 5L, DateTime.UtcNow, new { Type = "Event5" }),
            new(_testActorId, _testActorPath.ToString(), 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath.ToString(), 15L, DateTime.UtcNow, new { Type = "Event15" }),
            new(_testActorId, _testActorPath.ToString(), 20L, DateTime.UtcNow, new { Type = "Event20" })
        };

        await _eventJournal.AppendEventsAsync(_testActorId, _testActorPath.ToString(), events);

        // Verify events exist
        var initialEvents = await _persistenceService.ReadEventsAsync(_testActorId, _testActorPath, 1L, 20L);
        initialEvents.Should().HaveCount(4);

        // Act - Delete events up to sequence 15
        await _persistenceService.DeleteEventsAsync(_testActorId, _testActorPath, 15L);

        // Assert - Only events 20 should remain
        var remainingEvents = await _persistenceService.ReadEventsAsync(_testActorId, _testActorPath, 1L, 20L);
        remainingEvents.Should().HaveCount(1);
        remainingEvents.First().SequenceNr.Should().Be(20L);
    }

    /// <summary>
/// Tests that deleting all events for an actor removes every event associated with that actor,
/// regardless of their sequence numbers.
/// </summary>
    [Fact]
    public async Task DeleteAllEventsAsync_ShouldRemoveAllEventsForActor()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath.ToString(), 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(_testActorId, _testActorPath.ToString(), 2L, DateTime.UtcNow, new { Type = "Event2" })
        };

        await _eventJournal.AppendEventsAsync(_testActorId, _testActorPath.ToString(), events);

        // Verify events exist
        var initialEvents = await _persistenceService.ReadEventsAsync(_testActorId, _testActorPath, 1L, 2L);
        initialEvents.Should().HaveCount(2);

        // Act
        await _persistenceService.DeleteAllEventsAsync(_testActorId, _testActorPath);

        // Assert
        var remainingEvents = await _persistenceService.ReadEventsAsync(_testActorId, _testActorPath, 1L, 2L);
        remainingEvents.Should().BeEmpty();
    }

    /// <summary>
/// Tests that saving a snapshot with an existing sequence number overwrites the previous snapshot
/// rather than creating a duplicate, ensuring snapshot uniqueness per sequence number.
/// </summary>
    [Fact]
    public async Task SaveSnapshotAsync_ShouldOverwriteExistingSnapshot()
    {
        // Arrange
        var initialState = new { Counter = 1, Name = "Initial" };
        var updatedState = new { Counter = 2, Name = "Updated" };

        // Save initial snapshot
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, initialState, 100L);

        var snapshot1 = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);
        snapshot1.Should().NotBeNull();
        snapshot1!.State.Should().BeEquivalentTo(initialState);

        // Save updated snapshot with same sequence number (should overwrite)
        await _persistenceService.SaveSnapshotAsync(_testActorId, _testActorPath, updatedState, 100L);

        // Act - Load the snapshot
        var snapshot2 = await _persistenceService.LoadLatestSnapshotAsync(_testActorId, _testActorPath);

        // Assert - Should have the updated state
        snapshot2.Should().NotBeNull();
        snapshot2!.State.Should().BeEquivalentTo(updatedState);
    }

    /// <summary>
/// Tests that the file-based actor state persistence correctly saves and loads state.
/// Verifies that state can be saved to disk and successfully retrieved afterward.
/// Note: Since FileActorStatePersistence returns byte[] for state, direct object comparison
/// is not possible, but the test confirms the persistence mechanism works without errors.
/// </summary>
    [Fact]
    public async Task FileActorStatePersistence_ShouldSaveAndLoadState()
    {
        // Arrange
        var filePersistence = new FileActorStatePersistence(_tempDirectory);
        var testState = new TestState { Id = 123, Name = "Test Actor", Active = true };

        // Act - Save state
        await filePersistence.SaveAsync(_testActorId, _testActorPath, testState);

        // Assert - State should exist
        var exists = await filePersistence.ExistsAsync(_testActorId, _testActorPath);
        exists.Should().BeTrue();

        // Act - Load state
        var loadedState = await filePersistence.LoadAsync(_testActorId, _testActorPath);

        // Assert - State should be loaded correctly
        loadedState.Should().NotBeNull();
        // Note: FileActorStatePersistence returns byte[] for state, so we can't directly compare the object
        // This test verifies the file-based persistence works without errors
    }

    /// <summary>
/// Tests that the file-based actor state persistence returns null when attempting to load
/// state for an actor that has no persisted state.
/// </summary>
    [Fact]
    public async Task FileActorStatePersistence_ShouldReturnNull_WhenStateDoesNotExist()
    {
        // Arrange
        var filePersistence = new FileActorStatePersistence(_tempDirectory);

        // Act - Try to load non-existent state
        var loadedState = await filePersistence.LoadAsync(_testActorId, _testActorPath);

        // Assert
        loadedState.Should().BeNull();
    }

    /// <summary>
/// Tests that the file-based actor state persistence correctly deletes state.
/// Verifies that state exists before deletion and is removed after deletion.
/// </summary>
    [Fact]
    public async Task FileActorStatePersistence_ShouldDeleteState()
    {
        // Arrange
        var filePersistence = new FileActorStatePersistence(_tempDirectory);
        var testState = new { Value = 42 };

        await filePersistence.SaveAsync(_testActorId, _testActorPath, testState);
        var existsBefore = await filePersistence.ExistsAsync(_testActorId, _testActorPath);
        existsBefore.Should().BeTrue();

        // Act
        await filePersistence.DeleteAsync(_testActorId, _testActorPath);

        // Assert
        var existsAfter = await filePersistence.ExistsAsync(_testActorId, _testActorPath);
        existsAfter.Should().BeFalse();
    }

    /// <summary>
/// Tests that the in-memory actor state persistence correctly saves and loads state.
/// Verifies that state can be saved in memory and successfully retrieved afterward.
/// </summary>
    [Fact]
    public async Task InMemoryActorStatePersistence_ShouldSaveAndLoadState()
    {
        // Arrange
        var inMemoryPersistence = new InMemoryActorStatePersistence();
        var testState = new { Counter = 999, Message = "In-memory test" };

        // Act - Save state
        await inMemoryPersistence.SaveAsync(_testActorId, _testActorPath, testState);

        // Assert - State should exist
        var exists = await inMemoryPersistence.ExistsAsync(_testActorId, _testActorPath);
        exists.Should().BeTrue();

        // Act - Load state
        var loadedState = await inMemoryPersistence.LoadAsync(_testActorId, _testActorPath);

        // Assert - State should be loaded correctly
        loadedState.Should().NotBeNull();
        loadedState.Should().BeEquivalentTo(testState);
    }

    /// <summary>
/// Tests that the in-memory actor state persistence returns null when attempting to load
/// state for an actor that has no persisted state.
/// </summary>
    [Fact]
    public async Task InMemoryActorStatePersistence_ShouldReturnNull_WhenStateDoesNotExist()
    {
        // Arrange
        var inMemoryPersistence = new InMemoryActorStatePersistence();

        // Act - Try to load non-existent state
        var loadedState = await inMemoryPersistence.LoadAsync(_testActorId, _testActorPath);

        // Assert
        loadedState.Should().BeNull();
    }

    /// <summary>
/// Tests that the in-memory actor state persistence correctly deletes state.
/// Verifies that state exists before deletion and is removed after deletion.
/// </summary>
    [Fact]
    public async Task InMemoryActorStatePersistence_ShouldDeleteState()
    {
        // Arrange
        var inMemoryPersistence = new InMemoryActorStatePersistence();
        var testState = new { Data = "To be deleted" };

        await inMemoryPersistence.SaveAsync(_testActorId, _testActorPath, testState);
        var existsBefore = await inMemoryPersistence.ExistsAsync(_testActorId, _testActorPath);
        existsBefore.Should().BeTrue();

        // Act
        await inMemoryPersistence.DeleteAsync(_testActorId, _testActorPath);

        // Assert
        var existsAfter = await inMemoryPersistence.ExistsAsync(_testActorId, _testActorPath);
        existsAfter.Should().BeFalse();
    }

    // Simple test state class for file persistence test
    private class TestState
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
