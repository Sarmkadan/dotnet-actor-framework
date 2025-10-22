// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
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
    public void Set(ActorPath path, ActorRef actorRef)
    {
        if (path == null || actorRef == null)
            return;

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
    /// Returns null if not found or if the cache entry has expired.
    /// </summary>
    public ActorRef? Get(ActorPath path)
    {
        if (path == null) return null;

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
    public bool Contains(ActorPath path)
    {
        if (path == null) return false;
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
    public bool Remove(ActorPath path)
    {
        if (path == null) return false;
        var key = path.ToString();
        return _cache.TryRemove(key, out _);
    }

    /// <summary>
    /// Clears all cached entries.
    /// </summary>
    public void Clear()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Gets the number of items currently in the cache.
    /// </summary>
    public int Count => _cache.Count;

    /// <summary>
    /// Removes expired entries from the cache.
    /// </summary>
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

/// <summary>
/// Caching service for messages to support deduplication and replay scenarios.
/// </summary>
public class MessageCacheService
{
    private readonly ConcurrentDictionary<Guid, CachedMessage> _cache;
    private readonly int _maxCapacity;
    private readonly TimeSpan _ttl;

    public MessageCacheService(int maxCapacity = 5000, TimeSpan? ttl = null)
    {
        if (maxCapacity <= 0)
            throw new ArgumentException("Max capacity must be positive.", nameof(maxCapacity));

        _maxCapacity = maxCapacity;
        _ttl = ttl ?? TimeSpan.FromMinutes(10);
        _cache = new ConcurrentDictionary<Guid, CachedMessage>();
    }

    /// <summary>
    /// Caches a message for deduplication purposes.
    /// </summary>
    public void Cache(Message message)
    {
        if (message == null) return;

        if (_cache.Count >= _maxCapacity)
        {
            // Simple eviction: remove oldest entry
            var oldest = _cache.OrderBy(x => x.Value.CachedAt).FirstOrDefault();
            if (oldest.Key != Guid.Empty)
                _cache.TryRemove(oldest.Key, out _);
        }

        _cache[message.MessageId] = new CachedMessage(message);
    }

    /// <summary>
    /// Checks if a message with the given ID has been cached.
    /// </summary>
    public bool IsCached(Guid messageId)
    {
        if (!_cache.TryGetValue(messageId, out var cached))
            return false;

        if (DateTime.UtcNow - cached.CachedAt > _ttl)
        {
            _cache.TryRemove(messageId, out _);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Clears all cached messages.
    /// </summary>
    public void Clear() => _cache.Clear();

    public int Count => _cache.Count;

    private class CachedMessage
    {
        public Message Message { get; }
        public DateTime CachedAt { get; }

        public CachedMessage(Message message)
        {
            Message = message;
            CachedAt = DateTime.UtcNow;
        }
    }
}
