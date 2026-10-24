// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Caching;

/// <summary>
/// Caching service for actor references and paths.
/// Reduces lookups and improves performance when frequently accessing the same actors.
/// Uses LRU (Least Recently Used) eviction when capacity is reached. Capacity is a hard cap:
/// no insert can leave more than <c>maxCapacity</c> entries in the cache.
/// Expired entries are swept at most once per TTL period from <see cref="Set"/>, so entries that are
/// never read again are still reclaimed without a background timer.
/// </summary>
public class ActorCacheService
{
    private readonly Dictionary<string, LinkedListNode<Entry>> _index = new(StringComparer.Ordinal);
    private readonly LinkedList<Entry> _lruOrder = new();
    private readonly int _maxCapacity;
    private readonly TimeSpan _ttl;
    private readonly object _lockObject = new();
    private DateTime _lastSweepUtc;

    /// <summary>
    /// Initializes a new instance of <see cref="ActorCacheService"/>.
    /// </summary>
    /// <param name="maxCapacity">Hard cap on the number of cached entries. Must be positive.</param>
    /// <param name="ttl">Time‑to‑live for each entry. If <c>null</c>, defaults to 5 minutes. Must be positive when provided.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxCapacity"/> is not positive.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="ttl"/> is zero or negative.</exception>
    public ActorCacheService(int maxCapacity = 1000, TimeSpan? ttl = null)
    {
        if (maxCapacity <= 0)
            throw new ArgumentException("Max capacity must be positive.", nameof(maxCapacity));

        if (ttl is { } explicitTtl && explicitTtl <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(ttl), explicitTtl, "TTL must be positive.");

        _maxCapacity = maxCapacity;
        _ttl = ttl ?? TimeSpan.FromMinutes(5);
        _lastSweepUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds or updates an actor reference in the cache.
    /// If the cache is full, expired entries are swept first when a sweep is due; if it is still full,
    /// the least recently used entry is evicted. Both steps are O(1) amortized.
    /// </summary>
    /// <param name="path">The actor path to cache. Must not be null, empty, or exceed 260 characters.</param>
    /// <param name="actorRef">The actor reference to associate with the path. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> or <paramref name="actorRef"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or exceeds 260 characters.</exception>
    public void Set(ActorPath path, ActorRef actorRef)
    {
        ArgumentNullException.ThrowIfNull(actorRef);
        string key = NormalizeAndValidatePath(path);
        DateTime nowUtc = DateTime.UtcNow;

        lock (_lockObject)
        {
            if (_index.TryGetValue(key, out var existing))
            {
                existing.Value = new Entry(key, actorRef, nowUtc);
                MoveToFront(existing);
                return;
            }

            if (IsSweepDue(nowUtc))
                SweepExpiredLocked(nowUtc);

            if (_index.Count >= _maxCapacity)
                EvictLeastRecentlyUsedLocked();

            _index[key] = _lruOrder.AddFirst(new Entry(key, actorRef, nowUtc));
        }
    }

    /// <summary>
    /// Retrieves an actor reference from the cache and marks it as most recently used.
    /// Returns <c>null</c> if not found or if the cache entry has expired.
    /// </summary>
    /// <param name="path">The actor path to look up. Must not be null, empty, or exceed 260 characters.</param>
    /// <returns>The cached <see cref="ActorRef"/> or <c>null</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or exceeds 260 characters.</exception>
    public ActorRef? Get(ActorPath path)
    {
        string key = NormalizeAndValidatePath(path);
        DateTime nowUtc = DateTime.UtcNow;

        lock (_lockObject)
        {
            if (!_index.TryGetValue(key, out var node))
                return null;

            if (IsExpired(node.Value, nowUtc))
            {
                RemoveNodeLocked(node);
                return null;
            }

            MoveToFront(node);
            return node.Value.ActorRef;
        }
    }

    /// <summary>
    /// Checks if a path is in the cache.
    /// </summary>
    /// <param name="path">The actor path to check. Must not be null, empty, or exceed 260 characters.</param>
    /// <returns><c>true</c> if the path exists and is not expired; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or exceeds 260 characters.</exception>
    public bool Contains(ActorPath path)
    {
        string key = NormalizeAndValidatePath(path);
        DateTime nowUtc = DateTime.UtcNow;

        lock (_lockObject)
        {
            if (!_index.TryGetValue(key, out var node))
                return false;

            if (!IsExpired(node.Value, nowUtc))
                return true;

            RemoveNodeLocked(node);
            return false;
        }
    }

    /// <summary>
    /// Removes an entry from the cache.
    /// </summary>
    /// <param name="path">The actor path to remove. Must not be null, empty, or exceed 260 characters.</param>
    /// <returns><c>true</c> if the entry was removed; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or exceeds 260 characters.</exception>
    public bool Remove(ActorPath path)
    {
        string key = NormalizeAndValidatePath(path);

        lock (_lockObject)
        {
            if (!_index.TryGetValue(key, out var node))
                return false;

            RemoveNodeLocked(node);
            return true;
        }
    }

    /// <summary>
    /// Invalidates a cached actor reference, typically called when the actor terminates or restarts.
    /// Supervisors should invoke this method to purge stale references.
    /// </summary>
    /// <param name="path">The actor path whose cache entry should be removed. Must not be null, empty, or exceed 260 characters.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or exceeds 260 characters.</exception>
    public void Invalidate(ActorPath path) => Remove(path);

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _index.Clear();
            _lruOrder.Clear();
        }
    }

    /// <summary>
    /// Gets the number of items currently in the cache, including entries that have expired but
    /// have not yet been swept.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _index.Count;
            }
        }
    }

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
    /// <returns>The number of entries removed.</returns>
    public int RemoveExpired()
    {
        DateTime nowUtc = DateTime.UtcNow;

        lock (_lockObject)
        {
            return SweepExpiredLocked(nowUtc);
        }
    }

    private bool IsSweepDue(DateTime nowUtc) => nowUtc - _lastSweepUtc >= _ttl;

    private bool IsExpired(Entry entry, DateTime nowUtc) => nowUtc - entry.CachedAtUtc > _ttl;

    private int SweepExpiredLocked(DateTime nowUtc)
    {
        var removed = 0;
        var node = _lruOrder.Last;

        while (node is not null)
        {
            var previous = node.Previous;
            if (IsExpired(node.Value, nowUtc))
            {
                RemoveNodeLocked(node);
                removed++;
            }

            node = previous;
        }

        _lastSweepUtc = nowUtc;
        return removed;
    }

    private void EvictLeastRecentlyUsedLocked()
    {
        if (_lruOrder.Last is { } leastRecentlyUsed)
            RemoveNodeLocked(leastRecentlyUsed);
    }

    private void MoveToFront(LinkedListNode<Entry> node)
    {
        _lruOrder.Remove(node);
        _lruOrder.AddFirst(node);
    }

    private void RemoveNodeLocked(LinkedListNode<Entry> node)
    {
        _index.Remove(node.Value.Key);
        _lruOrder.Remove(node);
    }

    /// <summary>
    /// Normalizes and validates an actor path for use as a cache key.
    /// </summary>
    /// <param name="path">The actor path to normalize and validate.</param>
    /// <returns>The normalized path string suitable for use as a cache key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is empty or exceeds 260 characters.</exception>
    private static string NormalizeAndValidatePath(ActorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        string pathString = path.ToString();
        if (string.IsNullOrWhiteSpace(pathString))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (pathString.Length > 260)
            throw new ArgumentException($"Actor path exceeds maximum length of 260. Actual length: {pathString.Length}.", nameof(path));

        // Normalize to lowercase for case-insensitive caching
        return pathString.ToLowerInvariant();
    }

    private sealed record Entry(string Key, ActorRef ActorRef, DateTime CachedAtUtc);
}
