using System.Text.Json;
using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence;
using DotNetActorFramework.Persistence.Abstractions;
using Microsoft.Extensions.Logging;

namespace DotNetActorFramework.Repository;

/// <summary>
/// Repository for persisting and retrieving actor state.
/// Provides CRUD operations for actor state snapshots via PersistenceService.
/// </summary>
public class ActorStateRepository
{
    private readonly PersistenceService _persistenceService;
    private readonly ILogger<ActorStateRepository>? _logger;

    public ActorStateRepository(PersistenceService persistenceService, ILogger<ActorStateRepository>? logger = null)
    {
        _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        _logger = logger;
    }

    /// <summary>
    /// Gets the actor ID associated with this repository instance.
    /// </summary>
    public Guid ActorId { get; } = Guid.Empty;

    /// <summary>
    /// Gets the actor path associated with this repository instance.
    /// </summary>
    public ActorPath ActorPath { get; } = ActorPath.Parse("/default");

    /// <summary>
    /// Gets the current state stored in this repository.
    /// </summary>
    public object State { get; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets the timestamp when the state was last saved.
    /// </summary>
    public DateTime SavedAt { get; } = DateTime.MinValue;

    /// <summary>
    /// Gets the sequence number for state persistence.
    /// </summary>
    public long SequenceNr { get; } = 0;

    /// <summary>
    /// Gets the version number of the state.
    /// </summary>
    public int Version { get; } = 0;

    /// <summary>
    /// Saves the state of an actor.
    /// </summary>
    public async Task<bool> SaveStateAsync(Guid actorId, ActorPath actorPath, Dictionary<string, object> state, long sequenceNr)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
        if (actorPath == null)
            throw new ArgumentNullException(nameof(actorPath));
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        try
        {
            // Serialize the dictionary state into a single object (e.g., JSON string)
            var serializedState = JsonSerializer.Serialize(state);
            await _persistenceService.SaveSnapshotAsync(actorId, actorPath, serializedState, sequenceNr);
            _logger?.LogDebug("Saved state for actor {ActorId} at path {ActorPath} with sequence {SequenceNr}", actorId, actorPath, sequenceNr);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to save state for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return false;
        }
    }

    /// <summary>
    /// Loads the state of an actor.
    /// </summary>
    public async Task<Dictionary<string, object>?> LoadStateAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
        if (actorPath == null)
            throw new ArgumentNullException(nameof(actorPath));

        try
        {
            var snapshot = await _persistenceService.LoadLatestSnapshotAsync(actorId, actorPath);
            if (snapshot?.State is string serializedState)
            {
                _logger?.LogDebug("Loaded state for actor {ActorId} at path {ActorPath} with sequence {SequenceNr}", actorId, actorPath, snapshot.SequenceNr);
                // Deserialize the object (JSON string) back to Dictionary<string, object>
                return JsonSerializer.Deserialize<Dictionary<string, object>>(serializedState);
            }
            _logger?.LogDebug("No state found for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load state for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return null;
        }
    }

    /// <summary>
    /// Deletes the state of an actor.
    /// </summary>
    public async Task<bool> DeleteStateAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
        if (actorPath == null)
            throw new ArgumentNullException(nameof(actorPath));

        try
        {
            await _persistenceService.DeleteAllSnapshotsAsync(actorId, actorPath);
            _logger?.LogDebug("Deleted all state for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to delete state for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return false;
        }
    }

    /// <summary>
    /// Gets the state snapshot for an actor.
    /// </summary>
    public async Task<ActorStateSnapshot?> GetSnapshotAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
        if (actorPath == null)
            throw new ArgumentNullException(nameof(actorPath));

        try
        {
            var snapshot = await _persistenceService.LoadLatestSnapshotAsync(actorId, actorPath);
            if (snapshot != null)
            {
                _logger?.LogDebug("Retrieved snapshot for actor {ActorId} at path {ActorPath} with sequence {SequenceNr}", actorId, actorPath, snapshot.SequenceNr);
                return new ActorStateSnapshot(snapshot.ActorId, ActorPath.Parse(snapshot.ActorPath), JsonSerializer.Deserialize<Dictionary<string, object>>(snapshot.State as string ?? "{}") ?? new Dictionary<string, object>(), snapshot.SequenceNr, snapshot.Timestamp);
            }
            _logger?.LogDebug("No snapshot found for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get snapshot for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return null;
        }
    }

    /// <summary>
    /// Checks if state exists for an actor.
    /// </summary>
    public async Task<bool> HasState(Guid actorId, ActorPath actorPath)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));
        if (actorPath == null)
            throw new ArgumentNullException(nameof(actorPath));

        try
        {
            var snapshot = await _persistenceService.LoadLatestSnapshotAsync(actorId, actorPath);
            return snapshot != null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to check state existence for actor {ActorId} at path {ActorPath}", actorId, actorPath);
            return false;
        }
    }
}

/// <summary>
/// Represents a snapshot of actor state.
/// This model is specific to ActorStateRepository's API, and is mapped to/from ActorSnapshot abstraction.
/// </summary>
public class ActorStateSnapshot
{
    public Guid ActorId { get; }
    public ActorPath ActorPath { get; }
    public object State { get; } // Changed to object to hold deserialized state
    public DateTime SavedAt { get; }
    public long SequenceNr { get; } // Added SequenceNr
    public int Version { get; } // Kept for metadata

    public ActorStateSnapshot(Guid actorId, ActorPath actorPath, object state, long sequenceNr, DateTime savedAt, int version = 1)
    {
        ActorId = actorId;
        ActorPath = actorPath;
        State = state;
        SequenceNr = sequenceNr;
        SavedAt = savedAt;
        Version = version;
    }
}