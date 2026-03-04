// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Persistence.Abstractions;

namespace DotNetActorFramework.Persistence.InMemory;

/// <summary>
/// An in-memory implementation of <see cref="ISnapshotStore"/> for testing and development.
/// </summary>
public class InMemorySnapshotStore : ISnapshotStore
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<long, ActorSnapshot>> _snapshots = new();

    public Task SaveSnapshotAsync(ActorSnapshot snapshot)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

        var actorSnapshots = _snapshots.GetOrAdd(GetActorKey(snapshot.ActorId, snapshot.ActorPath), _ => new ConcurrentDictionary<long, ActorSnapshot>());
        actorSnapshots[snapshot.SequenceNr] = snapshot;
        return Task.CompletedTask;
    }

    public Task<ActorSnapshot?> LoadLatestSnapshotAsync(Guid actorId, string actorPath)
    {
        var actorSnapshots = _snapshots.GetValueOrDefault(GetActorKey(actorId, actorPath));
        if (actorSnapshots == null || actorSnapshots.IsEmpty)
        {
            return Task.FromResult<ActorSnapshot?>(null);
        }

        var latestSnapshot = actorSnapshots.Values.OrderByDescending(s => s.SequenceNr).FirstOrDefault();
        return Task.FromResult(latestSnapshot);
    }

    public Task DeleteSnapshotsAsync(Guid actorId, string actorPath, long maxSequenceNr)
    {
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

    public Task DeleteAllSnapshotsAsync(Guid actorId, string actorPath)
    {
        _snapshots.TryRemove(GetActorKey(actorId, actorPath), out _);
        return Task.CompletedTask;
    }

    private static string GetActorKey(Guid actorId, string actorPath) => $"{actorId}_{actorPath}";
}