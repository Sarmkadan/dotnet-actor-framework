// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json; // For serializing/deserializing object State/EventData

namespace DotNetActorFramework.Persistence;

/// <summary>
/// A facade service for managing actor persistence, combining snapshot storage and event journaling.
/// </summary>
public class PersistenceService
{
    private readonly ISnapshotStore _snapshotStore;
    private readonly IEventJournal _eventJournal;
    private readonly ILogger<PersistenceService>? _logger;

    public PersistenceService(
        ISnapshotStore snapshotStore,
        IEventJournal eventJournal,
        ILogger<PersistenceService>? logger = null)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _eventJournal = eventJournal ?? throw new ArgumentNullException(nameof(eventJournal));
        _logger = logger;
    }

    // --- Snapshot Store Operations ---

    /// <summary>
    /// Saves an actor's state as a snapshot.
    /// </summary>
    public async Task SaveSnapshotAsync(Guid actorId, ActorPath actorPath, object state, long sequenceNr)
    {
        _logger?.LogDebug("Saving snapshot for actor {ActorPath} at sequence {SequenceNr}", actorPath, sequenceNr);
        var snapshot = new ActorSnapshot(actorId, actorPath.ToString(), state, sequenceNr, DateTime.UtcNow);
        await _snapshotStore.SaveSnapshotAsync(snapshot);
    }

    /// <summary>
    /// Loads the latest snapshot for a given actor.
    /// </summary>
    public async Task<ActorSnapshot?> LoadLatestSnapshotAsync(Guid actorId, ActorPath actorPath)
    {
        _logger?.LogDebug("Loading latest snapshot for actor {ActorPath}", actorPath);
        return await _snapshotStore.LoadLatestSnapshotAsync(actorId, actorPath.ToString());
    }

    /// <summary>
    /// Deletes snapshots for a given actor up to a specified sequence number.
    /// </summary>
    public async Task DeleteSnapshotsAsync(Guid actorId, ActorPath actorPath, long maxSequenceNr)
    {
        _logger?.LogDebug("Deleting snapshots for actor {ActorPath} up to sequence {SequenceNr}", actorPath, maxSequenceNr);
        await _snapshotStore.DeleteSnapshotsAsync(actorId, actorPath.ToString(), maxSequenceNr);
    }

    /// <summary>
    /// Deletes all snapshots for a given actor.
    /// </summary>
    public async Task DeleteAllSnapshotsAsync(Guid actorId, ActorPath actorPath)
    {
        _logger?.LogDebug("Deleting all snapshots for actor {ActorPath}", actorPath);
        await _snapshotStore.DeleteAllSnapshotsAsync(actorId, actorPath.ToString());
    }

    // --- Event Journal Operations ---

    /// <summary>
    /// Appends events to an actor's event journal.
    /// </summary>
    public async Task AppendEventsAsync(Guid actorId, ActorPath actorPath, IEnumerable<ActorEvent> events)
    {
        _logger?.LogDebug("Appending {EventCount} events for actor {ActorPath}", events.Count(), actorPath);
        await _eventJournal.AppendEventsAsync(actorId, actorPath.ToString(), events);
    }

    /// <summary>
    /// Reads events for a given actor from a specified sequence number.
    /// </summary>
    public async Task<IEnumerable<ActorEvent>> ReadEventsAsync(Guid actorId, ActorPath actorPath, long fromSequenceNr, long toSequenceNr)
    {
        _logger?.LogDebug("Reading events for actor {ActorPath} from {From} to {To}", actorPath, fromSequenceNr, toSequenceNr);
        return await _eventJournal.ReadEventsAsync(actorId, actorPath.ToString(), fromSequenceNr, toSequenceNr);
    }

    /// <summary>
    /// Reads events for a given actor in backward order from a specified sequence number.
    /// </summary>
    public async Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync(Guid actorId, ActorPath actorPath, long fromSequenceNr, long toSequenceNr)
    {
        _logger?.LogDebug("Reading events backward for actor {ActorPath} from {From} to {To}", actorPath, fromSequenceNr, toSequenceNr);
        return await _eventJournal.ReadEventsBackwardAsync(actorId, actorPath.ToString(), fromSequenceNr, toSequenceNr);
    }

    /// <summary>
    /// Deletes events for a given actor up to a specified sequence number.
    /// </summary>
    public async Task DeleteEventsAsync(Guid actorId, ActorPath actorPath, long maxSequenceNr)
    {
        _logger?.LogDebug("Deleting events for actor {ActorPath} up to sequence {SequenceNr}", actorPath, maxSequenceNr);
        await _eventJournal.DeleteEventsAsync(actorId, actorPath.ToString(), maxSequenceNr);
    }

    /// <summary>
    /// Deletes all events for a given actor.
    /// </summary>
    public async Task DeleteAllEventsAsync(Guid actorId, ActorPath actorPath)
    {
        _logger?.LogDebug("Deleting all events for actor {ActorPath}", actorPath);
        await _eventJournal.DeleteAllEventsAsync(actorId, actorPath.ToString());
    }
}