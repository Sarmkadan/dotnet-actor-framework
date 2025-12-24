// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Persistence.Abstractions;

namespace DotNetActorFramework.Persistence.InMemory;

/// <summary>
/// An in-memory implementation of <see cref="IEventJournal"/> for testing and development.
/// </summary>
public class InMemoryEventJournal : IEventJournal
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, ActorEvent>> _events = new();

    public Task AppendEventsAsync(Guid actorId, string actorPath, IEnumerable<ActorEvent> events)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));

        var actorEvents = _events.GetOrAdd(GetActorKey(actorId, actorPath), _ => new ConcurrentDictionary<long, ActorEvent>());
        foreach (var ev in events)
        {
            if (!actorEvents.TryAdd(ev.SequenceNr, ev))
            {
                // Event with this sequence number already exists, which should not happen in a valid journal
                throw new InvalidOperationException($"Event with sequence number {ev.SequenceNr} already exists for actor {actorId} at path {actorPath}");
            }
        }
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ActorEvent>> ReadEventsAsync(Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        var actorEvents = _events.GetValueOrDefault(GetActorKey(actorId, actorPath));
        if (actorEvents == null || actorEvents.IsEmpty)
        {
            return Task.FromResult(Enumerable.Empty<ActorEvent>());
        }

        var result = actorEvents.Values
            .Where(e => e.SequenceNr >= fromSequenceNr && e.SequenceNr <= toSequenceNr)
            .OrderBy(e => e.SequenceNr)
            .ToList();

        return Task.FromResult<IEnumerable<ActorEvent>>(result);
    }

    public Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync(Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        var actorEvents = _events.GetValueOrDefault(GetActorKey(actorId, actorPath));
        if (actorEvents == null || actorEvents.IsEmpty)
        {
            return Task.FromResult(Enumerable.Empty<ActorEvent>());
        }

        var result = actorEvents.Values
            .Where(e => e.SequenceNr >= fromSequenceNr && e.SequenceNr <= toSequenceNr)
            .OrderByDescending(e => e.SequenceNr)
            .ToList();

        return Task.FromResult<IEnumerable<ActorEvent>>(result);
    }

    public Task DeleteEventsAsync(Guid actorId, string actorPath, long maxSequenceNr)
    {
        var actorKey = GetActorKey(actorId, actorPath);
        if (_events.TryGetValue(actorKey, out var actorEvents))
        {
            foreach (var kvp in actorEvents.Where(e => e.Value.SequenceNr <= maxSequenceNr).ToList())
            {
                actorEvents.TryRemove(kvp.Key, out _);
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteAllEventsAsync(Guid actorId, string actorPath)
    {
        _events.TryRemove(GetActorKey(actorId, actorPath), out _);
        return Task.CompletedTask;
    }

    private static string GetActorKey(Guid actorId, string actorPath) => $"{actorId}_{actorPath}";
}