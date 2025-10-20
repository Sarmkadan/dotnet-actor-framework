// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;
using DotNetActorFramework.Constants;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Services;

/// <summary>
/// Manages message mailboxes for actors.
/// Each actor has a FIFO mailbox where messages are queued and processed sequentially.
/// </summary>
public class MailboxService
{
    private readonly ConcurrentDictionary<Guid, Mailbox> _mailboxes = [];
    private readonly int _defaultCapacity;

    public MailboxService(int defaultCapacity = ActorConstants.DefaultMailboxCapacity)
    {
        if (defaultCapacity <= 0)
            throw new ArgumentException("Default capacity must be greater than zero.", nameof(defaultCapacity));

        _defaultCapacity = defaultCapacity;
    }

    /// <summary>
    /// Creates a new mailbox for an actor.
    /// </summary>
    public Mailbox CreateMailbox(Guid actorId, int capacity = 0)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (_mailboxes.ContainsKey(actorId))
            throw new InvalidOperationException($"Mailbox already exists for actor: {actorId}");

        var mailbox = new Mailbox(actorId, capacity > 0 ? capacity : _defaultCapacity);
        _mailboxes.TryAdd(actorId, mailbox);
        return mailbox;
    }

    /// <summary>
    /// Gets the mailbox for an actor.
    /// </summary>
    public Mailbox? GetMailbox(Guid actorId)
    {
        _mailboxes.TryGetValue(actorId, out var mailbox);
        return mailbox;
    }

    /// <summary>
    /// Enqueues a message into an actor's mailbox.
    /// </summary>
    public async Task EnqueueAsync(Guid actorId, Envelope envelope)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        var mailbox = GetMailbox(actorId);
        if (mailbox == null)
            throw new MailboxException($"Mailbox not found for actor: {actorId}");

        if (!await mailbox.EnqueueAsync(envelope))
            throw new MailboxException($"Mailbox is full for actor: {actorId}");
    }

    /// <summary>
    /// Dequeues the next message from an actor's mailbox.
    /// </summary>
    public async Task<Envelope?> DequeueAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        var mailbox = GetMailbox(actorId);
        if (mailbox == null)
            throw new MailboxException($"Mailbox not found for actor: {actorId}");

        return await mailbox.DequeueAsync();
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
    public void RemoveMailbox(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        _mailboxes.TryRemove(actorId, out _);
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
        _mailboxes.Clear();
    }
}

/// <summary>
/// Represents a message mailbox for an actor.
/// </summary>
public class Mailbox
{
    private readonly ConcurrentQueue<Envelope> _queue = [];
    private readonly SemaphoreSlim _availableSemaphore;
    public Guid ActorId { get; }
    public int Capacity { get; }

    public Mailbox(Guid actorId, int capacity)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (capacity <= 0)
            throw new ArgumentException("Capacity must be greater than zero.", nameof(capacity));

        ActorId = actorId;
        Capacity = capacity;
        _availableSemaphore = new SemaphoreSlim(capacity, capacity);
    }

    /// <summary>
    /// Enqueues a message into this mailbox.
    /// </summary>
    public async Task<bool> EnqueueAsync(Envelope envelope)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        // Hotfix: Use WaitAsync with timeout instead of non-blocking check to prevent race conditions
        // under burst traffic where multiple threads could simultaneously pass the WaitAsync(0) check
        // before the semaphore count is properly decremented
        if (!await _availableSemaphore.WaitAsync(100))
            return false;

        _queue.Enqueue(envelope);
        return true;
    }

    /// <summary>
    /// Dequeues the next message from this mailbox.
    /// </summary>
    public async Task<Envelope?> DequeueAsync()
    {
        await Task.Yield();
        if (_queue.TryDequeue(out var envelope))
        {
            _availableSemaphore.Release();
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
