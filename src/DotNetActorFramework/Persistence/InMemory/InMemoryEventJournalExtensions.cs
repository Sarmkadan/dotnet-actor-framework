using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetActorFramework.Persistence.Abstractions;

namespace DotNetActorFramework.Persistence.InMemory;

/// <summary>
/// Provides extension methods for <see cref="InMemoryEventJournal"/>.
/// </summary>
public static class InMemoryEventJournalExtensions
{
    /// <summary>
    /// Appends a single event to the journal.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="event">The <see cref="ActorEvent"/> to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, actorPath, or event is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static Task AppendEventAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, ActorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);
        ArgumentNullException.ThrowIfNull(@event);

        return journal.AppendEventsAsync(actorId, actorPath, new[] { @event });
    }

    /// <summary>
    /// Reads all events for a specific actor.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task containing an <see cref="IEnumerable{ActorEvent}"/> of all events.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, or actorPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static Task<IEnumerable<ActorEvent>> ReadAllEventsAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath)
        => journal.ReadEventsAsync(actorId, actorPath, long.MinValue, long.MaxValue);

    /// <summary>
    /// Counts the events for a specific actor within the specified sequence range.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="fromSequenceNr">The starting sequence number.</param>
    /// <param name="toSequenceNr">The ending sequence number.</param>
    /// <returns>A task containing the count of events.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, or actorPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static async Task<long> CountEventsAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        var events = await journal.ReadEventsAsync(actorId, actorPath, fromSequenceNr, toSequenceNr);
        return events.LongCount();
    }

    /// <summary>
    /// Determines whether any events exist for the specified actor.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task containing true if events exist; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, or actorPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static async Task<bool> HasEventsAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        var events = await journal.ReadEventsAsync(actorId, actorPath, long.MinValue, long.MaxValue);
        return events.Any();
    }

    /// <summary>
    /// Gets the first event for the specified actor within the sequence range.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="fromSequenceNr">The starting sequence number.</param>
    /// <param name="toSequenceNr">The ending sequence number.</param>
    /// <returns>A task containing the first event, or null if no events exist in the range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, or actorPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static async Task<ActorEvent?> GetFirstEventAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        var events = await journal.ReadEventsAsync(actorId, actorPath, fromSequenceNr, toSequenceNr);
        return events.OrderBy(e => e.SequenceNr).FirstOrDefault();
    }

    /// <summary>
    /// Gets the last event for the specified actor within the sequence range.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="fromSequenceNr">The starting sequence number.</param>
    /// <param name="toSequenceNr">The ending sequence number.</param>
    /// <returns>A task containing the last event, or null if no events exist in the range.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, or actorPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static async Task<ActorEvent?> GetLastEventAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        var events = await journal.ReadEventsAsync(actorId, actorPath, fromSequenceNr, toSequenceNr);
        return events.OrderByDescending(e => e.SequenceNr).FirstOrDefault();
    }

    /// <summary>
    /// Gets the event at the specified sequence number.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="sequenceNr">The sequence number to retrieve.</param>
    /// <returns>A task containing the event, or null if no event exists at the specified sequence number.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal, actorId, or actorPath is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static async Task<ActorEvent?> GetEventAtSequenceAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, long sequenceNr)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        var events = await journal.ReadEventsAsync(actorId, actorPath, sequenceNr, sequenceNr);
        return events.FirstOrDefault();
    }
}