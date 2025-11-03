// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Persistence.Abstractions;

/// <summary>
/// Represents a snapshot of an actor's state at a specific point in time.
/// </summary>
public record ActorSnapshot(
    Guid ActorId,
    string ActorPath,
    object State,
    long SequenceNr,
    DateTime Timestamp
);

/// <summary>
/// Defines the contract for storing and loading actor state snapshots.
/// </summary>
public interface ISnapshotStore
{
    /// <summary>
    /// Saves an actor snapshot.
    /// </summary>
    /// <param name="snapshot">The actor snapshot to save.</param>
    /// <returns>A task that represents the asynchronous save operation.</returns>
    Task SaveSnapshotAsync(ActorSnapshot snapshot);

    /// <summary>
    /// Loads the latest snapshot for a given actor.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task that represents the asynchronous load operation,
    /// returning the latest ActorSnapshot or null if none is found.</returns>
    Task<ActorSnapshot?> LoadLatestSnapshotAsync(Guid actorId, string actorPath);

    /// <summary>
    /// Deletes snapshots for a given actor up to a specified sequence number.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="maxSequenceNr">The maximum sequence number of snapshots to delete (inclusive).</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteSnapshotsAsync(Guid actorId, string actorPath, long maxSequenceNr);

    /// <summary>
    /// Deletes all snapshots for a given actor.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAllSnapshotsAsync(Guid actorId, string actorPath);
}

/// <summary>
/// Represents an event that occurred for an actor.
/// </summary>
public record ActorEvent(
    Guid ActorId,
    string ActorPath,
    long SequenceNr,
    DateTime Timestamp,
    object EventData
);

/// <summary>
/// Defines the contract for persisting and replaying actor events.
/// </summary>
public interface IEventJournal
{
    /// <summary>
    /// Appends events to an actor's event journal.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="events">The events to append.</param>
    /// <returns>A task that represents the asynchronous append operation.</returns>
    Task AppendEventsAsync(Guid actorId, string actorPath, IEnumerable<ActorEvent> events);

    /// <summary>
    /// Reads events for a given actor from a specified sequence number.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="fromSequenceNr">The starting sequence number (inclusive).</param>
    /// <param name="toSequenceNr">The ending sequence number (inclusive).</param>
    /// <returns>A task that represents the asynchronous read operation,
    /// returning an enumerable of ActorEvent.</returns>
    Task<IEnumerable<ActorEvent>> ReadEventsAsync(Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr);

    /// <summary>
    /// Reads events for a given actor in backward order from a specified sequence number.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="fromSequenceNr">The starting sequence number (inclusive).</param>
    /// <param name="toSequenceNr">The ending sequence number (inclusive).</param>
    /// <returns>A task that represents the asynchronous read operation,
    /// returning an enumerable of ActorEvent in reverse chronological order.</returns>
    Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync(Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr);

    /// <summary>
    /// Deletes events for a given actor up to a specified sequence number.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="maxSequenceNr">The maximum sequence number of events to delete (inclusive).</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteEventsAsync(Guid actorId, string actorPath, long maxSequenceNr);

    /// <summary>
    /// Deletes all events for a given actor.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteAllEventsAsync(Guid actorId, string actorPath);
}