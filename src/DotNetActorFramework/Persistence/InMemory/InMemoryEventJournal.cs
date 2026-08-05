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

    // Per-actor gate to make a whole AppendEventsAsync batch atomic under concurrent callers.
    // Without this, two threads appending to the same actor could interleave TryAdd calls
    // (e.g. both partially succeed before one hits a duplicate sequence number), leaving the
    // journal with a partially-applied, non-monotonic batch instead of an all-or-nothing append.
    private readonly ConcurrentDictionary<string, object> _actorLocks = new();

    public Task AppendEventsAsync(Guid actorId, string actorPath, IEnumerable<ActorEvent> events)
    {
        if (events == null) throw new ArgumentNullException(nameof(events));

        var actorKey = GetActorKey(actorId, actorPath);
        var eventList = events.ToList();
        var actorEvents = _events.GetOrAdd(actorKey, _ => new ConcurrentDictionary<long, ActorEvent>());
        var actorLock = _actorLocks.GetOrAdd(actorKey, _ => new object());

        lock (actorLock)
        {
            // Validate the whole batch first so a duplicate later in the batch doesn't leave
            // earlier events in this same call already committed to the journal.
            foreach (var ev in eventList)
            {
                if (actorEvents.ContainsKey(ev.SequenceNr))
                {
                    throw new InvalidOperationException($"Event with sequence number {ev.SequenceNr} already exists for actor {actorId} at path {actorPath}");
                }
            }

            foreach (var ev in eventList)
            {
                if (!actorEvents.TryAdd(ev.SequenceNr, ev))
                {
                    // Should be unreachable: the lock plus the pre-check above guarantee
                    // exclusive, monotonic access to this actor's sequence space.
                    throw new InvalidOperationException($"Event with sequence number {ev.SequenceNr} already exists for actor {actorId} at path {actorPath}");
                }
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