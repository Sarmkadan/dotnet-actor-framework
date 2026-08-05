// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
//
// xUnit tests for InMemoryEventJournal covering append/replay order,
// sequence numbers, and replay from offset functionality.
// =============================================================================

using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence.InMemory;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests.Persistence;

public class InMemoryEventJournalTests
{
    private readonly Guid _testActorId = Guid.NewGuid();
    private readonly string _testActorPath = "/test/actor";
    private readonly InMemoryEventJournal _journal;

    public InMemoryEventJournalTests()
    {
        _journal = new InMemoryEventJournal();
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldAddEventsWithCorrectSequenceNumbers()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1", Data = "First" }),
            new(_testActorId, _testActorPath, 2L, DateTime.UtcNow.AddSeconds(1), new { Type = "Event2", Data = "Second" }),
            new(_testActorId, _testActorPath, 3L, DateTime.UtcNow.AddSeconds(2), new { Type = "Event3", Data = "Third" })
        };

        // Act
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Assert
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 3L);
        loadedEvents.Should().HaveCount(3);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldStoreEventsInCorrectOrder()
    {
        // Arrange
        var timestamp1 = DateTime.UtcNow;
        var timestamp2 = timestamp1.AddSeconds(1);
        var timestamp3 = timestamp1.AddSeconds(2);

        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 10L, timestamp1, new { Type = "Event10", Value = 100 }),
            new(_testActorId, _testActorPath, 20L, timestamp2, new { Type = "Event20", Value = 200 }),
            new(_testActorId, _testActorPath, 30L, timestamp3, new { Type = "Event30", Value = 300 })
        };

        // Act
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Assert - Events should be returned in ascending sequence number order
        var loadedEvents = (await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 50L)).ToList();
        loadedEvents.Should().HaveCount(3);
        loadedEvents[0].SequenceNr.Should().Be(10L);
        loadedEvents[1].SequenceNr.Should().Be(20L);
        loadedEvents[2].SequenceNr.Should().Be(30L);
    }

    [Fact]
    public async Task ReadEventsAsync_ShouldReturnEmptyCollection_WhenNoEventsExist()
    {
        // Act
        var events = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 100L);

        // Assert
        events.Should().BeEmpty();
    }

    [Fact]
    public async Task ReadEventsAsync_ShouldReturnEventsFromSpecifiedOffset()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 5L, DateTime.UtcNow, new { Type = "Event5" }),
            new(_testActorId, _testActorPath, 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath, 15L, DateTime.UtcNow, new { Type = "Event15" }),
            new(_testActorId, _testActorPath, 20L, DateTime.UtcNow, new { Type = "Event20" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act - Read events starting from sequence 11
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 11L, 20L);

        // Assert
        loadedEvents.Should().HaveCount(2); // Events 15 and 20
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(15L);
        eventList[1].SequenceNr.Should().Be(20L);
    }

    [Fact]
    public async Task ReadEventsAsync_ShouldRespectToSequenceNrLimit()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(_testActorId, _testActorPath, 2L, DateTime.UtcNow, new { Type = "Event2" }),
            new(_testActorId, _testActorPath, 3L, DateTime.UtcNow, new { Type = "Event3" }),
            new(_testActorId, _testActorPath, 4L, DateTime.UtcNow, new { Type = "Event4" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act - Read events from 1 to 2 (should only get events 1 and 2)
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 2L);

        // Assert
        loadedEvents.Should().HaveCount(2);
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(1L);
        eventList[1].SequenceNr.Should().Be(2L);
    }

    [Fact]
    public async Task ReadEventsAsync_ShouldReturnEventsInAscendingOrder()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 100L, DateTime.UtcNow, new { Type = "Event100" }),
            new(_testActorId, _testActorPath, 50L, DateTime.UtcNow, new { Type = "Event50" }),
            new(_testActorId, _testActorPath, 75L, DateTime.UtcNow, new { Type = "Event75" }),
            new(_testActorId, _testActorPath, 25L, DateTime.UtcNow, new { Type = "Event25" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 150L);

        // Assert - Events should be sorted by sequence number in ascending order
        loadedEvents.Should().HaveCount(4);
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(25L);
        eventList[1].SequenceNr.Should().Be(50L);
        eventList[2].SequenceNr.Should().Be(75L);
        eventList[3].SequenceNr.Should().Be(100L);
    }

    [Fact]
    public async Task ReadEventsBackwardAsync_ShouldReturnEventsInDescendingOrder()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(_testActorId, _testActorPath, 2L, DateTime.UtcNow, new { Type = "Event2" }),
            new(_testActorId, _testActorPath, 3L, DateTime.UtcNow, new { Type = "Event3" }),
            new(_testActorId, _testActorPath, 4L, DateTime.UtcNow, new { Type = "Event4" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act - Read events backward from sequence 4 to 1
        var loadedEvents = await _journal.ReadEventsBackwardAsync(_testActorId, _testActorPath, 1L, 4L);

        // Assert - Events should be sorted by sequence number in descending order
        loadedEvents.Should().HaveCount(4);
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(4L);
        eventList[1].SequenceNr.Should().Be(3L);
        eventList[2].SequenceNr.Should().Be(2L);
        eventList[3].SequenceNr.Should().Be(1L);
    }

    [Fact]
    public async Task ReadEventsBackwardAsync_ShouldRespectRangeLimits()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath, 20L, DateTime.UtcNow, new { Type = "Event20" }),
            new(_testActorId, _testActorPath, 30L, DateTime.UtcNow, new { Type = "Event30" }),
            new(_testActorId, _testActorPath, 40L, DateTime.UtcNow, new { Type = "Event40" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act - Read events backward from 25 to 15 (should only get event 20)
        var loadedEvents = await _journal.ReadEventsBackwardAsync(_testActorId, _testActorPath, 15L, 25L);

        // Assert
        loadedEvents.Should().HaveCount(1);
        loadedEvents.First().SequenceNr.Should().Be(20L);
    }

    [Fact]
    public async Task DeleteEventsAsync_ShouldRemoveEventsUpToMaxSequenceNr()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 5L, DateTime.UtcNow, new { Type = "Event5" }),
            new(_testActorId, _testActorPath, 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath, 15L, DateTime.UtcNow, new { Type = "Event15" }),
            new(_testActorId, _testActorPath, 20L, DateTime.UtcNow, new { Type = "Event20" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Verify events exist
        var initialEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 20L);
        initialEvents.Should().HaveCount(4);

        // Act - Delete events up to sequence 15
        await _journal.DeleteEventsAsync(_testActorId, _testActorPath, 15L);

        // Assert - Only events 20 should remain
        var remainingEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 20L);
        remainingEvents.Should().HaveCount(1);
        remainingEvents.First().SequenceNr.Should().Be(20L);
    }

    [Fact]
    public async Task DeleteEventsAsync_ShouldDeleteEventsUpToMaxSequenceNr()
    {
        // Arrange - Use a fresh actor ID to avoid any state leakage
        var actorId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        var events = new List<ActorEvent>
        {
            new(actorId, _testActorPath, 5L, baseTime, new { Type = "Event5" }),
            new(actorId, _testActorPath, 10L, baseTime.AddSeconds(1), new { Type = "Event10" }),
            new(actorId, _testActorPath, 15L, baseTime.AddSeconds(2), new { Type = "Event15" }),
            new(actorId, _testActorPath, 20L, baseTime.AddSeconds(3), new { Type = "Event20" })
        };
        await _journal.AppendEventsAsync(actorId, _testActorPath, events);

        // Verify all 4 events were added
        var allEvents = await _journal.ReadEventsAsync(actorId, _testActorPath, 1L, 20L);
        allEvents.Should().HaveCount(4);

        // Act - Delete events up to sequence 12 (events 5 and 10 should be deleted since both <= 12)
        await _journal.DeleteEventsAsync(actorId, _testActorPath, 12L);

        // Assert - Events 15 and 20 should remain (10 was also <= 12 so it gets deleted)
        var remainingEvents = await _journal.ReadEventsAsync(actorId, _testActorPath, 1L, 20L);
        remainingEvents.Should().HaveCount(2);
        var eventList = remainingEvents.ToList();
        eventList[0].SequenceNr.Should().Be(15L);
        eventList[1].SequenceNr.Should().Be(20L);
    }

    [Fact]
    public async Task DeleteAllEventsAsync_ShouldRemoveAllEventsForActor()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(_testActorId, _testActorPath, 2L, DateTime.UtcNow, new { Type = "Event2" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Verify events exist
        var initialEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 2L);
        initialEvents.Should().HaveCount(2);

        // Act
        await _journal.DeleteAllEventsAsync(_testActorId, _testActorPath);

        // Assert
        var remainingEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 2L);
        remainingEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteAllEventsAsync_ShouldNotAffectOtherActors()
    {
        // Arrange - Create events for two different actors
        var actorId1 = Guid.NewGuid();
        var actorId2 = Guid.NewGuid();

        var events1 = new List<ActorEvent>
        {
            new(actorId1, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(actorId1, _testActorPath, 2L, DateTime.UtcNow, new { Type = "Event2" })
        };
        var events2 = new List<ActorEvent>
        {
            new(actorId2, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(actorId2, _testActorPath, 2L, DateTime.UtcNow, new { Type = "Event2" })
        };

        await _journal.AppendEventsAsync(actorId1, _testActorPath, events1);
        await _journal.AppendEventsAsync(actorId2, _testActorPath, events2);

        // Verify both actors have events
        var actor1Events = await _journal.ReadEventsAsync(actorId1, _testActorPath, 1L, 2L);
        var actor2Events = await _journal.ReadEventsAsync(actorId2, _testActorPath, 1L, 2L);
        actor1Events.Should().HaveCount(2);
        actor2Events.Should().HaveCount(2);

        // Act - Delete events for actor1 only
        await _journal.DeleteAllEventsAsync(actorId1, _testActorPath);

        // Assert - Actor1 should have no events, Actor2 should still have events
        var remainingActor1Events = await _journal.ReadEventsAsync(actorId1, _testActorPath, 1L, 2L);
        var remainingActor2Events = await _journal.ReadEventsAsync(actorId2, _testActorPath, 1L, 2L);
        remainingActor1Events.Should().BeEmpty();
        remainingActor2Events.Should().HaveCount(2);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldThrow_WhenSequenceNumberAlreadyExists()
    {
        // Arrange - Try to add events with duplicate sequence numbers
        var events1 = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" })
        };
        var events2 = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1Duplicate" })
        };

        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events1);

        // Act & Assert
        Func<Task> act = async () => await _journal.AppendEventsAsync(_testActorId, _testActorPath, events2);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ReadEventsAsync_ShouldHandleLargeSequenceNumberGaps()
    {
        // Arrange - Add events with large sequence number gaps
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Event1" }),
            new(_testActorId, _testActorPath, 100L, DateTime.UtcNow, new { Type = "Event100" }),
            new(_testActorId, _testActorPath, 1000L, DateTime.UtcNow, new { Type = "Event1000" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act - Read all events
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 1000L);

        // Assert - All events should be returned in correct order
        loadedEvents.Should().HaveCount(3);
        var eventList = loadedEvents.ToList();
        eventList[0].SequenceNr.Should().Be(1L);
        eventList[1].SequenceNr.Should().Be(100L);
        eventList[2].SequenceNr.Should().Be(1000L);
    }

    [Fact]
    public async Task ReadEventsAsync_ShouldHandleEmptyRange()
    {
        // Arrange
        var events = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 10L, DateTime.UtcNow, new { Type = "Event10" }),
            new(_testActorId, _testActorPath, 20L, DateTime.UtcNow, new { Type = "Event20" })
        };
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, events);

        // Act - Read with range that doesn't include any events
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 25L, 30L);

        // Assert
        loadedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task MultipleAppends_ShouldMaintainCorrectOrder()
    {
        // Arrange - Append events in multiple batches
        var batch1 = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Batch1Event1" }),
            new(_testActorId, _testActorPath, 2L, DateTime.UtcNow, new { Type = "Batch1Event2" })
        };
        var batch2 = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 3L, DateTime.UtcNow, new { Type = "Batch2Event1" }),
            new(_testActorId, _testActorPath, 4L, DateTime.UtcNow, new { Type = "Batch2Event2" })
        };
        var batch3 = new List<ActorEvent>
        {
            new(_testActorId, _testActorPath, 5L, DateTime.UtcNow, new { Type = "Batch3Event1" })
        };

        await _journal.AppendEventsAsync(_testActorId, _testActorPath, batch1);
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, batch2);
        await _journal.AppendEventsAsync(_testActorId, _testActorPath, batch3);

        // Act - Read all events
        var allEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 10L);

        // Assert - All events should be in correct sequence order
        allEvents.Should().HaveCount(5);
        var eventList = allEvents.ToList();
        for (int i = 0; i < 5; i++)
        {
            eventList[i].SequenceNr.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldBeThreadSafe_WhenAppendingConcurrentlyWithDistinctSequenceNumbers()
    {
        // Arrange - many concurrent callers each append a single event with a unique,
        // pre-assigned sequence number for the same actor.
        const int concurrentAppends = 200;
        var tasks = new List<Task>(concurrentAppends);

        for (var i = 1; i <= concurrentAppends; i++)
        {
            var sequenceNr = (long)i;
            var ev = new ActorEvent(_testActorId, _testActorPath, sequenceNr, DateTime.UtcNow, new { Type = "ConcurrentEvent", SequenceNr = sequenceNr });
            tasks.Add(Task.Run(() => _journal.AppendEventsAsync(_testActorId, _testActorPath, new[] { ev })));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert - no events lost, no duplicates, strictly increasing sequence numbers.
        var loadedEvents = (await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, concurrentAppends)).ToList();
        loadedEvents.Should().HaveCount(concurrentAppends);
        for (var i = 0; i < concurrentAppends; i++)
        {
            loadedEvents[i].SequenceNr.Should().Be(i + 1);
        }
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldRejectExactlyOneWinner_WhenConcurrentCallersRaceForSameSequenceNumber()
    {
        // Arrange - two concurrent callers race to append an event with the same sequence
        // number for the same actor; exactly one must win and the other must be rejected,
        // never both silently succeeding or both being lost.
        var ev1 = new ActorEvent(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Racer1" });
        var ev2 = new ActorEvent(_testActorId, _testActorPath, 1L, DateTime.UtcNow, new { Type = "Racer2" });

        var task1 = Task.Run(async () =>
        {
            try { await _journal.AppendEventsAsync(_testActorId, _testActorPath, new[] { ev1 }); return true; }
            catch (InvalidOperationException) { return false; }
        });
        var task2 = Task.Run(async () =>
        {
            try { await _journal.AppendEventsAsync(_testActorId, _testActorPath, new[] { ev2 }); return true; }
            catch (InvalidOperationException) { return false; }
        });

        // Act
        var results = await Task.WhenAll(task1, task2);

        // Assert - exactly one of the two racing appends succeeded.
        results.Count(r => r).Should().Be(1);
        var loadedEvents = await _journal.ReadEventsAsync(_testActorId, _testActorPath, 1L, 1L);
        loadedEvents.Should().HaveCount(1);
    }

    [Fact]
    public async Task DifferentActorPaths_ShouldIsolateEvents()
    {
        // Arrange - Use different actor paths
        var path1 = "/test/actor1";
        var path2 = "/test/actor2";

        var events1 = new List<ActorEvent>
        {
            new(_testActorId, path1, 1L, DateTime.UtcNow, new { Type = "Path1Event" }),
            new(_testActorId, path1, 2L, DateTime.UtcNow, new { Type = "Path1Event2" })
        };
        var events2 = new List<ActorEvent>
        {
            new(_testActorId, path2, 1L, DateTime.UtcNow, new { Type = "Path2Event" }),
            new(_testActorId, path2, 2L, DateTime.UtcNow, new { Type = "Path2Event2" })
        };

        await _journal.AppendEventsAsync(_testActorId, path1, events1);
        await _journal.AppendEventsAsync(_testActorId, path2, events2);

        // Act - Read events for each path
        var path1Events = await _journal.ReadEventsAsync(_testActorId, path1, 1L, 2L);
        var path2Events = await _journal.ReadEventsAsync(_testActorId, path2, 1L, 2L);

        // Assert - Each path should have its own isolated events
        path1Events.Should().HaveCount(2);
        path2Events.Should().HaveCount(2);

        path1Events.First().ActorPath.Should().Be(path1);
        path2Events.First().ActorPath.Should().Be(path2);
    }
}