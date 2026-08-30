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
public class MessageBatcher : IDisposable
{
    private readonly int _batchSize;
    private readonly TimeSpan _batchTimeout;
    private readonly ConcurrentDictionary<string, MessageBatch> _batches = [];
    private readonly Timer _flushTimer;
    private volatile bool _disposed;

    /// <summary>
    /// Raised by the background timer when a batch exceeds the batch timeout.
    /// When no handler is attached, expired batches are retained until they are
    /// filled or explicitly flushed, so no messages are lost.
    /// </summary>
    public event Action<string, IReadOnlyList<Message>>? BatchExpired;

    public MessageBatcher(int batchSize = 100, TimeSpan? batchTimeout = null)
    {
        if (batchSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be positive.");

        if (batchTimeout.HasValue && batchTimeout.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(batchTimeout), batchTimeout, "Batch timeout must be positive.");

        _batchSize = batchSize;
        _batchTimeout = batchTimeout ?? TimeSpan.FromSeconds(5);

        // Flush pending batches periodically
        _flushTimer = new Timer(_ => FlushExpiredBatches(), null, _batchTimeout, _batchTimeout);
    }

    /// <summary>
    /// Adds a message to a batch and returns the batch contents when the batch is full.
    /// Returns <c>null</c> while the batch is still filling.
    /// </summary>
    public IEnumerable<Message>? AddMessage(string batchKey, Message message)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBatcher));

        if (string.IsNullOrWhiteSpace(batchKey))
            throw new ArgumentException("Batch key cannot be null or whitespace.", nameof(batchKey));

        ArgumentNullException.ThrowIfNull(message);

        while (true)
        {
            var batch = _batches.GetOrAdd(batchKey, _ => new MessageBatch());
            var count = batch.TryAdd(message);

            if (count < 0)
            {
                // The batch was flushed concurrently; drop the stale entry and retry
                _batches.TryRemove(new KeyValuePair<string, MessageBatch>(batchKey, batch));
                continue;
            }

            if (count >= _batchSize)
            {
                _batches.TryRemove(new KeyValuePair<string, MessageBatch>(batchKey, batch));
                var messages = batch.Close();
                return messages.Count > 0 ? messages : null;
            }

            return null;
        }
    }

    /// <summary>
    /// Flushes all pending messages in a batch.
    /// </summary>
    public IEnumerable<Message>? FlushBatch(string batchKey)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MessageBatcher));

        if (string.IsNullOrWhiteSpace(batchKey))
            throw new ArgumentException("Batch key cannot be null or whitespace.", nameof(batchKey));

        if (!_batches.TryRemove(batchKey, out var batch))
            return null;

        var messages = batch.Close();
        return messages.Count > 0 ? messages : null;
    }

    /// <summary>
    /// Flushes all batches.
    /// </summary>
    public Dictionary<string, IEnumerable<Message>> FlushAll()
    {
        var result = new Dictionary<string, IEnumerable<Message>>();

        foreach (var kvp in _batches)
        {
            if (_batches.TryRemove(kvp.Key, out var batch))
            {
                var messages = batch.Close();
                if (messages.Count > 0)
                    result[kvp.Key] = messages;
            }
        }

        return result;
    }

    private void FlushExpiredBatches()
    {
        var handler = BatchExpired;
        if (handler == null)
            return; // Nobody consumes expired batches; keep them so messages are not lost

        var now = DateTime.UtcNow;

        foreach (var kvp in _batches)
        {
            if (now - kvp.Value.CreatedAt <= _batchTimeout || kvp.Value.Count == 0)
                continue;

            if (!_batches.TryRemove(kvp.Key, out var batch))
                continue;

            var messages = batch.Close();
            if (messages.Count == 0)
                continue;

            try
            {
                handler(kvp.Key, messages);
            }
            catch (Exception ex)
            {
                // A faulty subscriber must not stop the flush timer
                System.Diagnostics.Debug.WriteLine($"BatchExpired handler failed for '{kvp.Key}': {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _flushTimer.Dispose();
    }

    private sealed class MessageBatch
    {
        private readonly List<Message> _messages = [];
        private readonly object _sync = new();
        private bool _closed;

        public DateTime CreatedAt { get; } = DateTime.UtcNow;

        public int Count
        {
            get
            {
                lock (_sync)
                {
                    return _messages.Count;
                }
            }
        }

        /// <summary>
        /// Adds a message and returns the resulting count, or -1 when the batch is already closed.
        /// </summary>
        public int TryAdd(Message message)
        {
            lock (_sync)
            {
                if (_closed)
                    return -1;

                _messages.Add(message);
                return _messages.Count;
            }
        }

        /// <summary>
        /// Closes the batch and returns its contents. Returns an empty list when already closed.
        /// </summary>
        public List<Message> Close()
        {
            lock (_sync)
            {
                if (_closed)
                    return [];

                _closed = true;
                return [.. _messages];
            }
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
            throw new ArgumentOutOfRangeException(nameof(messagesPerSecond), messagesPerSecond, "Messages per second must be positive.");

        _messagesPerSecond = messagesPerSecond;
    }

    /// <summary>
    /// Waits if necessary to maintain the message rate.
    /// </summary>
    public async Task ThrottleAsync()
    {
        TimeSpan delay;

        lock (_lockObject)
        {
            var now = DateTime.UtcNow;
            if (now < _nextAllowedTime)
            {
                delay = _nextAllowedTime - now;
                _nextAllowedTime = _nextAllowedTime.AddMilliseconds(1000.0 / _messagesPerSecond);
            }
            else
            {
                delay = TimeSpan.Zero;
                _nextAllowedTime = now.AddMilliseconds(1000.0 / _messagesPerSecond);
            }
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay);
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
            throw new ArgumentOutOfRangeException(nameof(maxCapacity), maxCapacity, "Max capacity must be positive.");

        if (deduplicationWindow.HasValue && deduplicationWindow.Value <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(deduplicationWindow), deduplicationWindow, "Deduplication window must be positive.");

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
            // Clean up entries that have left the deduplication window.
            // _timestampedIds is ordered by insertion time, so expired entries form a prefix.
            var cutoff = DateTime.UtcNow - _deduplicationWindow;
            var expiredCount = 0;
            while (expiredCount < _timestampedIds.Count && _timestampedIds[expiredCount].timestamp < cutoff)
            {
                _processedIds.Remove(_timestampedIds[expiredCount].id);
                expiredCount++;
            }

            if (expiredCount > 0)
                _timestampedIds.RemoveRange(0, expiredCount);

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
