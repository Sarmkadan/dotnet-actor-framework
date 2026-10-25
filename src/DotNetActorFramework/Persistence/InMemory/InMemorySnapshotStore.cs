// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Linq;
using DotNetActorFramework.Persistence.Abstractions;

namespace DotNetActorFramework.Persistence.InMemory;

/// <summary>
/// An in-memory implementation of <see cref="ISnapshotStore"/> for testing and development.
/// </summary>
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, ActorSnapshot>> _snapshots = new();

    /// <summary>
    /// Saves an actor snapshot, keeping only the most recent snapshot for the actor.
    /// </summary>
    /// <param name="snapshot">The actor snapshot to save.</param>
    /// <returns>A completed task once the snapshot has been stored.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
    public Task SaveSnapshotAsync(ActorSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var actorSnapshots = _snapshots.GetOrAdd(GetActorKey(snapshot.ActorId, snapshot.ActorPath), _ => new ConcurrentDictionary<long, ActorSnapshot>());
        actorSnapshots[snapshot.SequenceNr] = snapshot;

        // Prune older snapshots – keep only the most recent one per actor
        var maxSeq = actorSnapshots.Keys.Max();
        foreach (var seq in actorSnapshots.Keys.Where(k => k != maxSeq).ToList())
        {
            actorSnapshots.TryRemove(seq, out _);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the latest snapshot (highest sequence number) for a given actor.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task whose result is the latest <see cref="ActorSnapshot"/>, or null if none is stored.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorPath"/> is null.</exception>
    public Task<ActorSnapshot?> LoadLatestSnapshotAsync(Guid actorId, string actorPath)
    {
        ArgumentNullException.ThrowIfNull(actorPath);

        var actorSnapshots = _snapshots.GetValueOrDefault(GetActorKey(actorId, actorPath));
        if (actorSnapshots == null || actorSnapshots.IsEmpty)
        {
            return Task.FromResult<ActorSnapshot?>(null);
        }

        var latestSnapshot = actorSnapshots.Values.OrderByDescending(s => s.SequenceNr).FirstOrDefault();
        return Task.FromResult(latestSnapshot);
    }

    /// <summary>
    /// Deletes snapshots for a given actor with a sequence number up to and including <paramref name="maxSequenceNr"/>.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="maxSequenceNr">The maximum sequence number of snapshots to delete (inclusive).</param>
    /// <returns>A completed task once the matching snapshots have been removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorPath"/> is null.</exception>
    public Task DeleteSnapshotsAsync(Guid actorId, string actorPath, long maxSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(actorPath);

        var actorKey = GetActorKey(actorId, actorPath);
        if (_snapshots.TryGetValue(actorKey, out var actorSnapshots))
        {
            foreach (var kvp in actorSnapshots.Where(s => s.Key <= maxSequenceNr).ToList())
            {
                actorSnapshots.TryRemove(kvp.Key, out _);
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes all snapshots stored for a given actor.
    /// </summary>
    /// <param name="actorId">The ID of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A completed task once all of the actor's snapshots have been removed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorPath"/> is null.</exception>
    public Task DeleteAllSnapshotsAsync(Guid actorId, string actorPath)
    {
        ArgumentNullException.ThrowIfNull(actorPath);

        _snapshots.TryRemove(GetActorKey(actorId, actorPath), out _);
        return Task.CompletedTask;
    }

    private static string GetActorKey(Guid actorId, string actorPath) => $"{actorId}_{actorPath}";
}
