// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Repository;

/// <summary>
/// Repository for persisting and retrieving actor state.
/// Provides CRUD operations for actor state snapshots.
/// </summary>
public class ActorStateRepository
{
    private readonly ConnectionManager _connectionManager;
    private readonly Dictionary<string, ActorStateSnapshot> _stateCache = [];
    private readonly object _lockObject = new();

    public ActorStateRepository(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
    }

    /// <summary>
    /// Saves the state of an actor.
    /// </summary>
    public async Task<bool> SaveStateAsync(Guid actorId, Dictionary<string, object> state)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (state == null)
            throw new ArgumentNullException(nameof(state));

        try
        {
            var snapshot = new ActorStateSnapshot
            {
                ActorId = actorId,
                State = new Dictionary<string, string>(),
                SavedAt = DateTime.UtcNow,
                Version = 1
            };

            foreach (var kvp in state)
            {
                snapshot.State[kvp.Key] = JsonSerializer.Serialize(kvp.Value);
            }

            lock (_lockObject)
            {
                _stateCache[actorId.ToString()] = snapshot;
            }

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads the state of an actor.
    /// </summary>
    public async Task<Dictionary<string, object>?> LoadStateAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        lock (_lockObject)
        {
            if (_stateCache.TryGetValue(actorId.ToString(), out var snapshot))
            {
                var state = new Dictionary<string, object>();
                foreach (var kvp in snapshot.State)
                {
                    try
                    {
                        state[kvp.Key] = JsonSerializer.Deserialize<object>(kvp.Value) ?? kvp.Value;
                    }
                    catch
                    {
                        state[kvp.Key] = kvp.Value;
                    }
                }

                return state;
            }
        }

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Deletes the state of an actor.
    /// </summary>
    public async Task<bool> DeleteStateAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        lock (_lockObject)
        {
            _stateCache.Remove(actorId.ToString());
        }

        await Task.CompletedTask;
        return true;
    }

    /// <summary>
    /// Gets the state snapshot for an actor.
    /// </summary>
    public async Task<ActorStateSnapshot?> GetSnapshotAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        lock (_lockObject)
        {
            _stateCache.TryGetValue(actorId.ToString(), out var snapshot);
            return snapshot;
        }

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Gets all state snapshots.
    /// </summary>
    public async Task<IReadOnlyList<ActorStateSnapshot>> GetAllSnapshotsAsync()
    {
        lock (_lockObject)
        {
            return _stateCache.Values.ToList().AsReadOnly();
        }

        await Task.CompletedTask;
        return [];
    }

    /// <summary>
    /// Checks if state exists for an actor.
    /// </summary>
    public bool HasState(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        lock (_lockObject)
        {
            return _stateCache.ContainsKey(actorId.ToString());
        }
    }

    /// <summary>
    /// Gets the number of saved states.
    /// </summary>
    public int GetStateCount()
    {
        lock (_lockObject)
        {
            return _stateCache.Count;
        }
    }

    /// <summary>
    /// Clears all states.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _stateCache.Clear();
        }
    }
}

/// <summary>
/// Represents a snapshot of actor state.
/// </summary>
public class ActorStateSnapshot
{
    public Guid ActorId { get; set; }
    public Dictionary<string, string> State { get; set; } = [];
    public DateTime SavedAt { get; set; }
    public int Version { get; set; }

    public long GetStateSize()
    {
        return State.Sum(kvp => kvp.Key.Length + kvp.Value.Length);
    }
}
