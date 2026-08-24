// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;
using DotNetActorFramework.Constants;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using Microsoft.Extensions.Logging;

namespace DotNetActorFramework.Services;

/// <summary>
/// Manages message mailboxes for actors.
/// Each actor has a FIFO mailbox where messages are queued and processed sequentially.
/// </summary>
public class MailboxService
{
    private readonly ConcurrentDictionary<Guid, IMailbox> _mailboxes = [];
    private readonly ActorSystemOptions _options; // Storing options
    private readonly int _defaultCapacity;
    private readonly ILogger<MailboxService>? _logger;

    public MailboxService(ActorSystemOptions options, ILogger<MailboxService>? logger = null) // Injected options
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        if (_options.DefaultMailboxCapacity <= 0)
            throw new ArgumentException("Default capacity must be greater than zero.", nameof(_options.DefaultMailboxCapacity));

        if (_options.HighWatermarkWarningThreshold < 0 || _options.HighWatermarkWarningThreshold > 100)
            throw new ArgumentException("High watermark threshold must be between 0 and 100.", nameof(_options.HighWatermarkWarningThreshold));

        _defaultCapacity = _options.DefaultMailboxCapacity;
    }

    /// <summary>
    /// Creates a new mailbox for an actor.
    /// Uses atomic TryAdd to prevent TOCTOU race between existence check and insertion.
    /// </summary>
    /// <param name="actorId">The ID of the actor for which to create the mailbox.</param>
    /// <param name="capacity">Optional: the capacity of the mailbox. If 0, uses default capacity.</param>
    /// <param name="mailboxType">Optional: the type of mailbox to create. If not specified, uses default from options.</param>
    /// <param name="overflowPolicy">Optional: the overflow policy for the mailbox. If not specified, uses default from options.</param>
    /// <returns>The newly created mailbox.</returns>
    /// <exception cref="ArgumentException">Thrown when actorId is empty or capacity is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a mailbox already exists for the actor.</exception>
    public IMailbox CreateMailbox(
        Guid actorId,
        int capacity = 0,
        MailboxType? mailboxType = null,
        MailboxOverflowPolicy? overflowPolicy = null)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        var actualCapacity = capacity > 0 ? capacity : _defaultCapacity;
        var actualMailboxType = mailboxType ?? _options.DefaultMailboxType;
        var actualOverflowPolicy = overflowPolicy ?? _options.DefaultMailboxOverflowPolicy;

        IMailbox mailbox;
        var highWatermarkThreshold = _options.HighWatermarkWarningThreshold;
        if (actualMailboxType == MailboxType.Priority)
        {
            mailbox = new BoundedPriorityMailbox(
                actorId,
                actualCapacity,
                actualOverflowPolicy,
                highWatermarkThreshold,
                _logger as ILogger<BoundedPriorityMailbox>);
        }
        else
        {
            mailbox = new BoundedMailbox(
                actorId,
                actualCapacity,
                actualOverflowPolicy,
                highWatermarkThreshold,
                _logger as ILogger<BoundedMailbox>);
        }

        if (!_mailboxes.TryAdd(actorId, mailbox))
        {
            mailbox.Dispose(); // Dispose the created mailbox if not added
            throw new InvalidOperationException($"Mailbox already exists for actor: {actorId}");
        }

        return mailbox;
    }

    /// <summary>
    /// Gets the mailbox for an actor.
    /// </summary>
    public IMailbox? GetMailbox(Guid actorId)
    {
        _mailboxes.TryGetValue(actorId, out var mailbox);
        return mailbox;
    }

    /// <summary>
    /// Enqueues a message into an actor's mailbox.
    /// </summary>
    /// <exception cref="MailboxException">Thrown when mailbox operations fail.</exception>
    /// <exception cref="ArgumentNullException">Thrown when actorId or envelope is null.</exception>
    public async Task EnqueueAsync(Guid actorId, Envelope envelope)
    {
        try
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

            if (envelope == null)
                throw new ArgumentNullException(nameof(envelope));

            var mailbox = GetMailbox(actorId);
            if (mailbox == null)
                throw new MailboxException(actorId, $"Mailbox not found for actor: {actorId}");

            if (!await mailbox.EnqueueAsync(envelope))
                throw new MailboxException(actorId, $"Mailbox is full for actor: {actorId}");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not MailboxException)
        {
            throw new MailboxException(actorId, $"Failed to enqueue message to actor {actorId}", ex);
        }
    }

    /// <summary>
    /// Dequeues the next message from an actor's mailbox.
    /// </summary>
    /// <exception cref="MailboxException">Thrown when mailbox operations fail.</exception>
    /// <exception cref="ArgumentException">Thrown when actorId is empty.</exception>
    public async Task<Envelope?> DequeueAsync(Guid actorId)
    {
        try
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

            var mailbox = GetMailbox(actorId);
            if (mailbox == null)
                throw new MailboxException(actorId, $"Mailbox not found for actor: {actorId}");

            return await mailbox.DequeueAsync();
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not MailboxException)
        {
            throw new MailboxException(actorId, $"Failed to dequeue message from actor {actorId}", ex);
        }
    }

    /// <summary>
    /// Gets the number of messages in an actor's mailbox.
    /// </summary>
    public int GetMailboxSize(Guid actorId)
    {
        var mailbox = GetMailbox(actorId);
        return mailbox?.GetSize() ?? 0;
    }

    /// <summary>
    /// Checks if an actor's mailbox is full.
    /// </summary>
    public bool IsMailboxFull(Guid actorId)
    {
        var mailbox = GetMailbox(actorId);
        return mailbox?.IsFull ?? false;
    }

    /// <summary>
    /// Removes a mailbox for an actor.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when actorId is empty.</exception>
    public void RemoveMailbox(Guid actorId)
    {
        try
        {
            if (actorId == Guid.Empty)
                throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

            if (_mailboxes.TryRemove(actorId, out var mailbox))
            {
                mailbox.Dispose();
            }
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MailboxException(actorId, $"Failed to remove mailbox for actor {actorId}", ex);
        }
    }

    /// <summary>
    /// Gets statistics about all mailboxes.
    /// </summary>
    public MailboxStatistics GetStatistics()
    {
        var totalMessages = _mailboxes.Values.Sum(m => m.GetSize());
        var totalCapacity = _mailboxes.Values.Sum(m => m.Capacity);
        var fullMailboxes = _mailboxes.Values.Count(m => m.IsFull);

        return new MailboxStatistics
        {
            TotalMailboxes = _mailboxes.Count,
            TotalMessages = totalMessages,
            TotalCapacity = totalCapacity,
            FullMailboxes = fullMailboxes,
            AverageLoadFactor = _mailboxes.Count > 0 ? (double)totalMessages / totalCapacity : 0
        };
    }

    /// <summary>
    /// Clears all mailboxes.
    /// </summary>
    public void Clear()
    {
        foreach (var mailbox in _mailboxes.Values)
        {
            mailbox.Dispose();
        }
        _mailboxes.Clear();
    }

    /// <summary>
    /// Returns a concise, informative representation of the current mailbox statistics.
    /// </summary>
    public override string ToString()
    {
        var stats = GetStatistics();
        return $"MailboxService {{ TotalMailboxes = {stats.TotalMailboxes}, TotalMessages = {stats.TotalMessages}, TotalCapacity = {stats.TotalCapacity}, FullMailboxes = {stats.FullMailboxes}, AverageLoadFactor = {stats.AverageLoadFactor} }}";
    }
}

/// <summary>
/// Represents a message mailbox for an actor.
/// </summary>
public class Mailbox : IMailbox
{
    private readonly ConcurrentQueue<Envelope> _queue = [];
    private readonly SemaphoreSlim _availableSemaphore;
    private readonly int _highWatermarkThreshold;
    private readonly ILogger<Mailbox>? _logger;
    private bool _highWatermarkWarningIssued = false;
    public Guid ActorId { get; }
    public int Capacity { get; }
    public int HighWatermarkWarningThreshold { get; }
    private bool _disposed = false;

    public Mailbox(Guid actorId, int capacity, int highWatermarkThreshold = 80, ILogger<Mailbox>? logger = null)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));

        if (highWatermarkThreshold < 0 || highWatermarkThreshold > 100)
            throw new ArgumentException("High watermark threshold must be between 0 and 100.", nameof(highWatermarkThreshold));

        ActorId = actorId;
        Capacity = capacity;
        _highWatermarkThreshold = highWatermarkThreshold;
        HighWatermarkWarningThreshold = highWatermarkThreshold;
        _logger = logger;
        _availableSemaphore = new SemaphoreSlim(capacity, capacity);
    }

    /// <summary>
    /// Enqueues a message into this mailbox.
    /// </summary>
    public async Task<bool> EnqueueAsync(Envelope envelope)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mailbox));
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        // Hotfix: Use WaitAsync with timeout instead of non-blocking check to prevent race conditions
        // under burst traffic where multiple threads could simultaneously pass the WaitAsync(0) check
        // before the semaphore count is properly decremented
        if (!await _availableSemaphore.WaitAsync(100))
            return false;

        _queue.Enqueue(envelope);

        // Check if we've crossed the high watermark threshold and issue warning if needed
        var currentLoadFactor = GetLoadFactor();
        if (currentLoadFactor >= _highWatermarkThreshold / 100.0 && !_highWatermarkWarningIssued)
        {
            _highWatermarkWarningIssued = true;
            _logger?.LogWarning("Mailbox for actor {ActorId} has reached high watermark: {CurrentLoadPercent}% full (threshold: {Threshold}%)",
                ActorId, (int)(currentLoadFactor * 100), _highWatermarkThreshold);
        }

        return true;
    }

    /// <summary>
    /// Dequeues the next message from this mailbox.
    /// </summary>
    public async Task<Envelope?> DequeueAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(Mailbox));
        await Task.Yield();
        if (_queue.TryDequeue(out var envelope))
        {
            _availableSemaphore.Release();

            // Reset the high watermark warning flag if load has dropped below threshold
            var currentLoadFactor = GetLoadFactor();
            if (currentLoadFactor < _highWatermarkThreshold / 100.0)
            {
                _highWatermarkWarningIssued = false;
            }

            return envelope;
        }

        return null;
    }

    /// <summary>
    /// Gets the number of messages in this mailbox.
    /// </summary>
    public int GetSize() => _queue.Count;

    /// <summary>
    /// Checks if this mailbox is full.
    /// </summary>
    public bool IsFull => _queue.Count >= Capacity;

    /// <summary>
    /// Gets the load factor of this mailbox (0-1).
    /// </summary>
    public double GetLoadFactor() => (double)_queue.Count / Capacity;

    /// <summary>
    /// Disposes the managed resources used by the Mailbox.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _availableSemaphore.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Represents a message mailbox for an actor that prioritizes messages.
/// </summary>
public class PriorityMailbox : IMailbox
{
    private readonly PriorityQueue<Envelope, int> _queue = new();
    private readonly object _queueLock = new();
    private readonly SemaphoreSlim _availableSemaphore;
    private readonly int _highWatermarkThreshold;
    private readonly ILogger<PriorityMailbox>? _logger;
    private bool _highWatermarkWarningIssued = false;
    public Guid ActorId { get; }
    public int Capacity { get; }
    public int HighWatermarkWarningThreshold { get; }
    private bool _disposed = false;

    public PriorityMailbox(Guid actorId, int capacity, int highWatermarkThreshold = 80, ILogger<PriorityMailbox>? logger = null)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));

        if (highWatermarkThreshold < 0 || highWatermarkThreshold > 100)
            throw new ArgumentException("High watermark threshold must be between 0 and 100.", nameof(highWatermarkThreshold));

        ActorId = actorId;
        Capacity = capacity;
        _highWatermarkThreshold = highWatermarkThreshold;
        HighWatermarkWarningThreshold = highWatermarkThreshold;
        _logger = logger;
        _availableSemaphore = new SemaphoreSlim(capacity, capacity);
    }

    /// <summary>
    /// Enqueues a message into this priority mailbox.
    /// </summary>
    public async Task<bool> EnqueueAsync(Envelope envelope)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PriorityMailbox));
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        if (!await _availableSemaphore.WaitAsync(100))
            return false;

        // PriorityQueue dequeues the smallest priority value first, so negate the
        // message priority to make higher-priority messages dequeue first.
        // PriorityQueue is not thread-safe, so guard it with a lock.
        lock (_queueLock)
        {
            _queue.Enqueue(envelope, -envelope.Message.Priority);
        }

        // Check if we've crossed the high watermark threshold and issue warning if needed
        var currentLoadFactor = GetLoadFactor();
        if (currentLoadFactor >= _highWatermarkThreshold / 100.0 && !_highWatermarkWarningIssued)
        {
            _highWatermarkWarningIssued = true;
            _logger?.LogWarning("Priority mailbox for actor {ActorId} has reached high watermark: {CurrentLoadPercent}% full (threshold: {Threshold}%)",
                ActorId, (int)(currentLoadFactor * 100), _highWatermarkThreshold);
        }

        return true;
    }

    /// <summary>
    /// Dequeues the next message from this priority mailbox.
    /// </summary>
    public async Task<Envelope?> DequeueAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PriorityMailbox));
        await Task.Yield(); // Yield to allow other tasks to run

        bool dequeued;
        Envelope? envelope;
        lock (_queueLock)
        {
            dequeued = _queue.TryDequeue(out envelope, out _);
        }

        if (dequeued)
        {
            _availableSemaphore.Release();

            // Reset the high watermark warning flag if load has dropped below threshold
            var currentLoadFactor = GetLoadFactor();
            if (currentLoadFactor < _highWatermarkThreshold / 100.0)
            {
                _highWatermarkWarningIssued = false;
            }

            return envelope;
        }

        return null;
    }

    /// <summary>
    /// Gets the number of messages in this priority mailbox.
    /// </summary>
    public int GetSize()
    {
        lock (_queueLock)
        {
            return _queue.Count;
        }
    }

    /// <summary>
    /// Checks if this priority mailbox is full.
    /// </summary>
    public bool IsFull => GetSize() >= Capacity;

    /// <summary>
    /// Gets the load factor of this priority mailbox (0-1).
    /// </summary>
    public double GetLoadFactor() => (double)GetSize() / Capacity;

    /// <summary>
    /// Disposes the managed resources used by the PriorityMailbox.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _availableSemaphore.Dispose();
            }
            _disposed = true;
        }
    }
}

/// <summary>
/// Statistics about mailboxes in the system.
/// </summary>
public class MailboxStatistics
{
    public int TotalMailboxes { get; set; }
    public long TotalMessages { get; set; }
    public long TotalCapacity { get; set; }
    public int FullMailboxes { get; set; }
    public double AverageLoadFactor { get; set; }
}
