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
    /// <exception cref="ArgumentNullException">Thrown when journal or event is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static Task AppendEventAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, ActorEvent @event)
    {
        ArgumentNullException.ThrowIfNull(journal);
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
    /// <exception cref="ArgumentNullException">Thrown when journal is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static Task<IEnumerable<ActorEvent>> ReadAllEventsAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        return journal.ReadEventsAsync(actorId, actorPath, long.MinValue, long.MaxValue);
    }

    /// <summary>
    /// Counts the events for a specific actor within the specified sequence range.
    /// </summary>
    /// <param name="journal">The <see cref="InMemoryEventJournal"/> instance.</param>
    /// <param name="actorId">The <see cref="Guid"/> of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="fromSequenceNr">The starting sequence number.</param>
    /// <param name="toSequenceNr">The ending sequence number.</param>
    /// <returns>A task containing the count of events.</returns>
    /// <exception cref="ArgumentNullException">Thrown when journal is null.</exception>
    /// <exception cref="ArgumentException">Thrown when actorPath is null or empty.</exception>
    public static async Task<long> CountEventsAsync(this InMemoryEventJournal journal, Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(journal);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        var events = await journal.ReadEventsAsync(actorId, actorPath, fromSequenceNr, toSequenceNr);
        return events.LongCount();
    }
}
