// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Linq;
using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.Json; // For serializing/deserializing object State/EventData

namespace DotNetActorFramework.Persistence;

/// <summary>
/// A facade service for managing actor persistence, combining snapshot storage and event journaling.
/// Includes an auto‑snapshot policy that creates a snapshot after a configurable number of events
/// and prunes older snapshots to keep storage size bounded.
/// </summary>
public class PersistenceService
{
    private readonly ISnapshotStore _snapshotStore;
    private readonly IEventJournal _eventJournal;
    private readonly ILogger<PersistenceService>? _logger;

    // Auto‑snapshot configuration
    private readonly int _snapshotIntervalEvents;
    private readonly ConcurrentDictionary<string, int> _eventCounters = new();

    public PersistenceService(
        ISnapshotStore snapshotStore,
        IEventJournal eventJournal,
        ILogger<PersistenceService>? logger = null,
        int snapshotIntervalEvents = 100) // default: snapshot every 100 events
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _eventJournal = eventJournal ?? throw new ArgumentNullException(nameof(eventJournal));
        _logger = logger;
        _snapshotIntervalEvents = snapshotIntervalEvents;
    }

    // --- Snapshot Store Operations ---

    /// <summary>
    /// Saves an actor's state as a snapshot.
    /// </summary>
    public async Task SaveSnapshotAsync(Guid actorId, ActorPath actorPath, object state, long sequenceNr)
    {
        try
        {
            _logger?.LogInformation("Entering SaveSnapshotAsync for actor {ActorPath} at sequence {SequenceNr}", actorPath, sequenceNr);
            _logger?.LogDebug("Saving snapshot for actor {ActorPath} at sequence {SequenceNr}", actorPath, sequenceNr);
            var snapshot = new ActorSnapshot(actorId, actorPath.ToString(), state, sequenceNr, DateTime.UtcNow);
            await _snapshotStore.SaveSnapshotAsync(snapshot);
            _logger?.LogInformation("Exiting SaveSnapshotAsync for actor {ActorPath} at sequence {SequenceNr}", actorPath, sequenceNr);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save snapshot for actor {ActorPath} at sequence {SequenceNr}", actorPath, sequenceNr);
            throw;
        }
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
    /// After appending, the service checks the auto‑snapshot policy and creates a snapshot
    /// if the configured number of events has been reached.
    /// </summary>
    public async Task AppendEventsAsync(Guid actorId, ActorPath actorPath, IEnumerable<ActorEvent> events)
    {
        _logger?.LogDebug("Appending {EventCount} events for actor {ActorPath}", events.Count(), actorPath);
        await _eventJournal.AppendEventsAsync(actorId, actorPath.ToString(), events);

        // Update the per‑actor event counter
        var key = GetActorKey(actorId, actorPath);
        var added = events.Count();
        var newCount = _eventCounters.AddOrUpdate(key, added, (k, old) => old + added);

        // If the threshold is reached, create a snapshot (state is left as null for now)
        if (newCount >= _snapshotIntervalEvents)
        {
            // Use a simple sequence number based on the current timestamp
            var sequenceNr = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await SaveSnapshotAsync(actorId, actorPath, null, sequenceNr);

            // Reset the counter for this actor
            _eventCounters[key] = 0;
        }
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

    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------
    private static string GetActorKey(Guid actorId, ActorPath actorPath) => $"{actorId}_{actorPath}";
}
