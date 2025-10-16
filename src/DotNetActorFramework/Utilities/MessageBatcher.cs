// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Batches messages together for more efficient processing.
/// Groups messages by type or destination and processes them in batches.
/// </summary>
public class MessageBatcher
{
    private readonly int _batchSize;
    private readonly TimeSpan _batchTimeout;
    private readonly ConcurrentDictionary<string, MessageBatch> _batches = [];
    private readonly Timer _flushTimer;

    public MessageBatcher(int batchSize = 100, TimeSpan? batchTimeout = null)
    {
        if (batchSize <= 0)
            throw new ArgumentException("Batch size must be positive.", nameof(batchSize));

        _batchSize = batchSize;
        _batchTimeout = batchTimeout ?? TimeSpan.FromSeconds(5);

        // Flush pending batches periodically
        _flushTimer = new Timer(_ => FlushExpiredBatches(), null, _batchTimeout, _batchTimeout);
    }

    /// <summary>
    /// Adds a message to a batch and returns it when the batch is full or should be flushed.
    /// </summary>
    public IEnumerable<Message>? AddMessage(string batchKey, Message message)
    {
        if (string.IsNullOrWhiteSpace(batchKey) || message == null)
            return null;

        var batch = _batches.GetOrAdd(batchKey, _ => new MessageBatch(_batchSize, _batchTimeout));

        batch.Add(message);

        if (batch.Count >= _batchSize)
        {
            _batches.TryRemove(batchKey, out var fullBatch);
            return fullBatch?.Messages.ToList();
        }

        return null;
    }

    /// <summary>
    /// Flushes all pending messages in a batch.
    /// </summary>
    public IEnumerable<Message>? FlushBatch(string batchKey)
    {
        if (!_batches.TryRemove(batchKey, out var batch))
            return null;

        return batch.Count > 0 ? batch.Messages.ToList() : null;
    }

    /// <summary>
    /// Flushes all batches.
    /// </summary>
    public Dictionary<string, IEnumerable<Message>> FlushAll()
    {
        var result = new Dictionary<string, IEnumerable<Message>>();

        foreach (var kvp in _batches)
        {
            if (_batches.TryRemove(kvp.Key, out var batch) && batch.Count > 0)
                result[kvp.Key] = batch.Messages.ToList();
        }

        return result;
    }

    private void FlushExpiredBatches()
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _batches
            .Where(kvp => now - kvp.Value.CreatedAt > _batchTimeout && kvp.Value.Count > 0)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
            _batches.TryRemove(key, out _);
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
    }

    private class MessageBatch
    {
        private readonly List<Message> _messages = [];
        public int Capacity { get; }
        public DateTime CreatedAt { get; }

        public MessageBatch(int capacity, TimeSpan timeout)
        {
            Capacity = capacity;
            CreatedAt = DateTime.UtcNow;
        }

        public int Count => _messages.Count;
        public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

        public void Add(Message message)
        {
            _messages.Add(message);
        }
    }
}

/// <summary>
/// Throttles message processing to a maximum rate.
/// Useful for rate limiting and backpressure management.
/// </summary>
public class MessageThrottler
{
    private readonly int _messagesPerSecond;
    private DateTime _nextAllowedTime = DateTime.UtcNow;
    private readonly object _lockObject = new();

    public MessageThrottler(int messagesPerSecond)
    {
        if (messagesPerSecond <= 0)
            throw new ArgumentException("Messages per second must be positive.", nameof(messagesPerSecond));

        _messagesPerSecond = messagesPerSecond;
    }

    /// <summary>
    /// Waits if necessary to maintain the message rate.
    /// </summary>
    public async Task ThrottleAsync()
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            if (now < _nextAllowedTime)
            {
                var delayMs = (long)(_nextAllowedTime - now).TotalMilliseconds;
                _nextAllowedTime = _nextAllowedTime.AddMilliseconds(1000.0 / _messagesPerSecond);

                Task.Delay((int)delayMs).Wait();
            }
            else
            {
                _nextAllowedTime = now.AddMilliseconds(1000.0 / _messagesPerSecond);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// Tries to process a message without waiting.
    /// Returns true if the message can be processed now, false if throttled.
    /// </summary>
    public bool TryProcess()
    {
        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            if (now >= _nextAllowedTime)
            {
                _nextAllowedTime = now.AddMilliseconds(1000.0 / _messagesPerSecond);
                return true;
            }
            return false;
        }
    }
}

/// <summary>
/// Deduplicates messages based on message ID or custom key.
/// </summary>
public class MessageDeduplicator
{
    private readonly HashSet<Guid> _processedIds = [];
    private readonly int _maxCapacity;
    private readonly TimeSpan _deduplicationWindow;
    private readonly List<(Guid id, DateTime timestamp)> _timestampedIds = [];
    private readonly object _lockObject = new();

    public MessageDeduplicator(int maxCapacity = 10000, TimeSpan? deduplicationWindow = null)
    {
        if (maxCapacity <= 0)
            throw new ArgumentException("Max capacity must be positive.", nameof(maxCapacity));

        _maxCapacity = maxCapacity;
        _deduplicationWindow = deduplicationWindow ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Checks if a message has been seen before.
    /// </summary>
    public bool IsDuplicate(Guid messageId)
    {
        lock (_lockObject)
        {
            // Clean up old entries
            var cutoff = DateTime.UtcNow - _deduplicationWindow;
            _timestampedIds.RemoveAll(x => x.timestamp < cutoff);

            return _processedIds.Contains(messageId);
        }
    }

    /// <summary>
    /// Registers a message as processed.
    /// </summary>
    public void RegisterMessage(Guid messageId)
    {
        lock (_lockObject)
        {
            if (_processedIds.Count >= _maxCapacity)
            {
                // Remove oldest entry
                if (_timestampedIds.Count > 0)
                {
                    var oldest = _timestampedIds[0];
                    _timestampedIds.RemoveAt(0);
                    _processedIds.Remove(oldest.id);
                }
            }

            _processedIds.Add(messageId);
            _timestampedIds.Add((messageId, DateTime.UtcNow));
        }
    }

    /// <summary>
    /// Clears all deduplication records.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _processedIds.Clear();
            _timestampedIds.Clear();
        }
    }
}
