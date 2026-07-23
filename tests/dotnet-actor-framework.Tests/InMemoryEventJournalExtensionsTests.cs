namespace DotNetActorFramework.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence.InMemory;
using FluentAssertions;
using Xunit;

public class InMemoryEventJournalExtensionsTests
{
    private readonly InMemoryEventJournal _journal;
    private readonly Guid _actorId;
    private readonly string _actorPath;

    public InMemoryEventJournalExtensionsTests()
    {
        _journal = new InMemoryEventJournal();
        _actorId = Guid.NewGuid();
        _actorPath = "test/actor/path";
    }

    private ActorEvent CreateEvent(long sequenceNr, string data = "data")
    {
        return new ActorEvent(_actorId, _actorPath, sequenceNr, DateTime.UtcNow, data);
    }

    [Fact]
    public async Task AppendEventAsync_ShouldAppendEvent()
    {
        var @event = CreateEvent(1);
        await _journal.AppendEventAsync(_actorId, _actorPath, @event);

        var events = await _journal.ReadAllEventsAsync(_actorId, _actorPath);
        events.Should().ContainSingle().Which.Should().Be(@event);
    }

    [Fact]
    public async Task ReadAllEventsAsync_ShouldReturnAllEvents()
    {
        var e1 = CreateEvent(1);
        var e2 = CreateEvent(2);
        await _journal.AppendEventAsync(_actorId, _actorPath, e1);
        await _journal.AppendEventAsync(_actorId, _actorPath, e2);

        var events = await _journal.ReadAllEventsAsync(_actorId, _actorPath);
        events.Should().HaveCount(2).And.Contain(new[] { e1, e2 });
    }

    [Fact]
    public async Task CountEventsAsync_ShouldReturnCorrectCountInRange()
    {
        await _journal.AppendEventAsync(_actorId, _actorPath, CreateEvent(1));
        await _journal.AppendEventAsync(_actorId, _actorPath, CreateEvent(2));
        await _journal.AppendEventAsync(_actorId, _actorPath, CreateEvent(3));

        var count = await _journal.CountEventsAsync(_actorId, _actorPath, 1, 2);
        count.Should().Be(2);
    }

    [Fact]
    public async Task HasEventsAsync_ShouldReturnTrueWhenEventsExist()
    {
        await _journal.AppendEventAsync(_actorId, _actorPath, CreateEvent(1));
        var hasEvents = await _journal.HasEventsAsync(_actorId, _actorPath);
        hasEvents.Should().BeTrue();
    }

    [Fact]
    public async Task GetFirstEventAsync_ShouldReturnFirstEventInRange()
    {
        var e1 = CreateEvent(1);
        var e2 = CreateEvent(2);
        await _journal.AppendEventAsync(_actorId, _actorPath, e1);
        await _journal.AppendEventAsync(_actorId, _actorPath, e2);

        var first = await _journal.GetFirstEventAsync(_actorId, _actorPath, 1, 2);
        first.Should().Be(e1);
    }

    [Fact]
    public async Task GetLastEventAsync_ShouldReturnLastEventInRange()
    {
        var e1 = CreateEvent(1);
        var e2 = CreateEvent(2);
        await _journal.AppendEventAsync(_actorId, _actorPath, e1);
        await _journal.AppendEventAsync(_actorId, _actorPath, e2);

        var last = await _journal.GetLastEventAsync(_actorId, _actorPath, 1, 2);
        last.Should().Be(e2);
    }

    [Fact]
    public async Task GetEventAtSequenceAsync_ShouldReturnSpecificEvent()
    {
        var e1 = CreateEvent(1);
        var e2 = CreateEvent(2);
        await _journal.AppendEventAsync(_actorId, _actorPath, e1);
        await _journal.AppendEventAsync(_actorId, _actorPath, e2);

        var ev = await _journal.GetEventAtSequenceAsync(_actorId, _actorPath, 2);
        ev.Should().Be(e2);
    }

    [Fact]
    public async Task AppendEventAsync_ShouldThrowWhenJournalIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((InMemoryEventJournal)null!).AppendEventAsync(_actorId, _actorPath, CreateEvent(1)));
    }
}
