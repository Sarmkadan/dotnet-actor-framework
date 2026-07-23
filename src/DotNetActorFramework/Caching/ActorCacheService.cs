// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Linq;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Caching;

/// <summary>
/// Caching service for actor references and paths.
/// Reduces lookups and improves performance when frequently accessing the same actors.
/// Uses LRU (Least Recently Used) eviction when capacity is reached.
/// </summary>
public class ActorCacheService
{
    private readonly ConcurrentDictionary<string, CachedActorRef> _cache;
    private readonly int _maxCapacity;
    private readonly TimeSpan _ttl;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ActorCacheService"/>.
    /// </summary>
    /// <param name="maxCapacity">Maximum number of cached entries. Must be positive.</param>
    /// <param name="ttl">Time‑to‑live for each entry. If <c>null</c>, defaults to 5 minutes.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxCapacity"/> is not positive.</exception>
    public ActorCacheService(int maxCapacity = 1000, TimeSpan? ttl = null)
    {
        if (maxCapacity <= 0)
            throw new ArgumentException("Max capacity must be positive.", nameof(maxCapacity));

        _maxCapacity = maxCapacity;
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
        _cache = new ConcurrentDictionary<string, CachedActorRef>();
    }

    /// <summary>
    /// Adds or updates an actor reference in the cache.
    /// </summary>
    /// <param name="path">The actor path to cache.</param>
    /// <param name="actorRef">The actor reference to associate with the path.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="actorRef"/> is <c>null</c>.</exception>
    public void Set(ActorPath path, ActorRef actorRef)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(actorRef);

        var key = path.ToString();
        var cached = new CachedActorRef(actorRef);

        lock (_lockObject)
        {
            if (_cache.Count >= _maxCapacity)
            {
                EvictLRU();
            }
        }

        _cache.AddOrUpdate(key, cached, (_, _) => cached);
    }

    /// <summary>
    /// Retrieves an actor reference from the cache.
    /// Returns <c>null</c> if not found or if the cache entry has expired.
    /// </summary>
    /// <param name="path">The actor path to look up.</param>
    /// <returns>The cached <see cref="ActorRef"/> or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    public ActorRef? Get(ActorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var key = path.ToString();
        if (!_cache.TryGetValue(key, out var cached))
            return null;

        if (DateTime.UtcNow - cached.CachedAt > _ttl)
        {
            _cache.TryRemove(key, out _);
            return null;
        }

        cached.LastAccessedAt = DateTime.UtcNow;
        return cached.ActorRef;
    }

    /// <summary>
    /// Checks if a path is in the cache.
    /// </summary>
    /// <param name="path">The actor path to check.</param>
    /// <returns><c>true</c> if the path exists and is not expired; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    public bool Contains(ActorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var key = path.ToString();
        if (!_cache.TryGetValue(key, out var cached))
            return false;

        if (DateTime.UtcNow - cached.CachedAt > _ttl)
        {
            _cache.TryRemove(key, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Removes an entry from the cache.
    /// </summary>
    /// <param name="path">The actor path to remove.</param>
    /// <returns><c>true</c> if the entry was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    public bool Remove(ActorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var key = path.ToString();
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Invalidates a cached actor reference, typically called when the actor terminates or restarts.
    /// </summary>
    /// <param name="path">The actor path whose cache entry should be removed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    public void Invalidate(ActorPath path)
    {
        // Alias for Remove – kept for semantic clarity.
        Remove(path);
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear() => _cache.Clear();

    /// <summary>
    /// Gets the number of items currently in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    /// <returns>The number of entries removed.</returns>
    public int RemoveExpired()
    {
        var removed = 0;
        foreach (var kvp in _cache)
        {
            if (DateTime.UtcNow - kvp.Value.CachedAt > _ttl)
            {
                if (_cache.TryRemove(kvp.Key, out _))
                    removed++;
            }
        }
        return removed;
    }

    private void EvictLRU()
    {
        var lruItem = _cache.OrderBy(x => x.Value.LastAccessedAt).FirstOrDefault();
        if (!string.IsNullOrEmpty(lruItem.Key))
        {
            _cache.TryRemove(lruItem.Key, out _);
        }
    }

    private class CachedActorRef
    {
        public ActorRef ActorRef { get; }
        public DateTime CachedAt { get; }
        public DateTime LastAccessedAt { get; set; }

        public CachedActorRef(ActorRef actorRef)
        {
            ActorRef = actorRef;
            CachedAt = DateTime.UtcNow;
            LastAccessedAt = DateTime.UtcNow;
        }
    }
}
