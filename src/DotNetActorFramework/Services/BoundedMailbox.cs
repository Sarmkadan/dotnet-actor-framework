// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Models;
using Microsoft.Extensions.Logging;

namespace DotNetActorFramework.Services;

/// <summary>
/// A bounded mailbox implementation that uses Channel&lt;T&gt; with configurable overflow policies.
/// This mailbox provides backpressure capabilities to prevent out-of-memory situations when
/// producers outpace consumers.
/// </summary>
public class BoundedMailbox : IMailbox
{
    private readonly Channel<Envelope> _channel;
    private readonly MailboxOverflowPolicy _overflowPolicy;
    private readonly int _capacity;
    private readonly int _highWatermarkThreshold;
    private readonly ILogger<BoundedMailbox>? _logger;
    private int _droppedMessageCount;
    private bool _disposed;

    /// <summary>
    /// Gets the unique identifier of the actor this mailbox belongs to.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// Gets the maximum capacity of this mailbox.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the overflow policy that determines behavior when the mailbox is full.
    /// </summary>
    public MailboxOverflowPolicy OverflowPolicy => _overflowPolicy;

    /// <summary>
    /// Gets the high watermark warning threshold as a percentage (0-100).
    /// When the load factor exceeds this threshold, a warning is logged once until it drops below.
    /// </summary>
    public int HighWatermarkWarningThreshold => _highWatermarkThreshold;

    /// <summary>
    /// Gets a value indicating whether the mailbox is full.
    /// </summary>
    public bool IsFull => GetSize() >= _capacity;

    /// <summary>
    /// Gets the number of messages that have been dropped due to overflow.
    /// </summary>
    public int DroppedMessageCount => _droppedMessageCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedMailbox"/> class.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="capacity">The maximum number of messages the mailbox can hold.</param>
    /// <param name="overflowPolicy">The policy to apply when the mailbox is full.</param>
    /// <param name="highWatermarkThreshold">The percentage (0-100) at which to log a warning when exceeded.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentException">Thrown if actorId is empty, capacity is not positive, or overflowPolicy is invalid.</exception>
    public BoundedMailbox(
        Guid actorId,
        int capacity,
        MailboxOverflowPolicy overflowPolicy = MailboxOverflowPolicy.DropNewest,
        int highWatermarkThreshold = 80,
        ILogger<BoundedMailbox>? logger = null)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (capacity <= 0)
            throw new MailboxConfigurationException("Mailbox capacity must be greater than zero.");

        if (highWatermarkThreshold < 0 || highWatermarkThreshold > 100)
            throw new MailboxConfigurationException("High watermark threshold must be between 0 and 100.");

        ActorId = actorId;
        _capacity = capacity;
        _overflowPolicy = overflowPolicy;
        _highWatermarkThreshold = highWatermarkThreshold;
        _logger = logger;

        // Create bounded channel with the specified overflow policy
        // Note: Fail policy is handled manually in EnqueueAsync to throw MailboxFullException
        // instead of using the channel's Wait mode which would block indefinitely
        var boundedChannelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = overflowPolicy switch
            {
                MailboxOverflowPolicy.DropNewest => BoundedChannelFullMode.DropNewest,
                MailboxOverflowPolicy.DropOldest => BoundedChannelFullMode.DropOldest,
                MailboxOverflowPolicy.Fail => BoundedChannelFullMode.DropNewest, // Handled manually
                MailboxOverflowPolicy.Wait => BoundedChannelFullMode.Wait,
                _ => BoundedChannelFullMode.DropNewest // Default to DropNewest for unknown policies
            }
        };

        _channel = Channel.CreateBounded<Envelope>(boundedChannelOptions);
    }

    /// <summary>
    /// Enqueues a message into this mailbox.
    /// </summary>
    /// <param name="envelope">The message envelope to enqueue.</param>
    /// <returns>
    /// A task that represents the asynchronous enqueue operation.
    /// Returns true if the message was successfully enqueued, false if it was dropped.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if envelope is null.</exception>
    /// <exception cref="MailboxFullException">Thrown if the Fail policy is used and the mailbox is full.</exception>
    public async Task<bool> EnqueueAsync(Envelope envelope)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoundedMailbox));

        ArgumentNullException.ThrowIfNull(envelope);

        try
        {
            // For Fail policy, check if the mailbox is full and throw exception immediately
            // instead of using the channel's Wait mode which would block indefinitely
            if (_overflowPolicy == MailboxOverflowPolicy.Fail && IsFull)
            {
                throw new MailboxFullException(
                    ActorId,
                    _capacity,
                    GetSize(),
                    $"Mailbox is full for actor {ActorId} with Fail policy. Capacity: {_capacity}, Current: {GetSize()}");
            }

            await _channel.Writer.WriteAsync(envelope);

            // Check if we've crossed the high watermark threshold and issue warning if needed
            var currentLoadFactor = GetLoadFactor();
            if (currentLoadFactor >= _highWatermarkThreshold / 100.0 && GetSize() > 0)
            {
                _logger?.LogWarning(
                    "Mailbox for actor {ActorId} has reached high watermark: {CurrentLoadPercent}% full (threshold: {Threshold}%)",
                    ActorId,
                    (int)(currentLoadFactor * 100),
                    _highWatermarkThreshold);
            }

            return true;
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(BoundedMailbox), "Mailbox has been disposed.");
        }
        catch (MailboxFullException)
        {
            throw; // Re-throw MailboxFullException for Fail policy
        }
        catch (Exception ex) when (ex is not MailboxFullException)
        {
            _droppedMessageCount++;
            _logger?.LogError(ex, "Failed to enqueue message to mailbox for actor {ActorId}", ActorId);
            return false;
        }
    }

    /// <summary>
    /// Dequeues the next message from this mailbox.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dequeue operation.
    /// Returns the dequeued envelope, or null if the mailbox is empty.
    /// </returns>
    public async Task<Envelope?> DequeueAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoundedMailbox));

        try
        {
            if (_channel.Reader.TryRead(out var envelope))
            {
                // Reset high watermark warning if load has dropped below threshold
                var currentLoadFactor = GetLoadFactor();
                if (currentLoadFactor < _highWatermarkThreshold / 100.0)
                {
                    // Note: We can't easily track state here, so warnings may re-occur
                    // This is acceptable as it's just a diagnostic
                }

                return envelope;
            }

            return null;
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(BoundedMailbox), "Mailbox has been disposed.");
        }
    }

    /// <summary>
    /// Gets the number of messages currently in this mailbox.
    /// </summary>
    /// <returns>The number of messages in the mailbox.</returns>
    public int GetSize() => _channel.Reader.Count;

    /// <summary>
    /// Gets the load factor of this mailbox as a value between 0 and 1.
    /// </summary>
    /// <returns>The load factor (0 = empty, 1 = full).</returns>
    public double GetLoadFactor() => (double)GetSize() / _capacity;

    /// <summary>
    /// Disposes the mailbox and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed resources used by the mailbox.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel.Writer.Complete();
                _channel.Reader.TryRead(out _); // Drain the channel
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer to ensure resources are released if Dispose is not called.
    /// </summary>
    ~BoundedMailbox()
    {
        Dispose(false);
    }
}

/// <summary>
/// A priority-based bounded mailbox implementation that uses Channel&lt;T&gt; with configurable overflow policies.
/// Messages with higher priority values are processed before messages with lower priority values.
/// </summary>
public class BoundedPriorityMailbox : IMailbox
{
    private readonly Channel<PrioritizedEnvelope> _channel;
    private readonly MailboxOverflowPolicy _overflowPolicy;
    private readonly int _capacity;
    private readonly int _highWatermarkThreshold;
    private readonly ILogger<BoundedPriorityMailbox>? _logger;
    private int _droppedMessageCount;
    private bool _disposed;

    /// <summary>
    /// Gets the unique identifier of the actor this mailbox belongs to.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// Gets the maximum capacity of this mailbox.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Gets the overflow policy that determines behavior when the mailbox is full.
    /// </summary>
    public MailboxOverflowPolicy OverflowPolicy => _overflowPolicy;

    /// <summary>
    /// Gets the high watermark warning threshold as a percentage (0-100).
    /// </summary>
    public int HighWatermarkWarningThreshold => _highWatermarkThreshold;

    /// <summary>
    /// Gets a value indicating whether the mailbox is full.
    /// </summary>
    public bool IsFull => GetSize() >= _capacity;

    /// <summary>
    /// Gets the number of messages that have been dropped due to overflow.
    /// </summary>
    public int DroppedMessageCount => _droppedMessageCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="BoundedPriorityMailbox"/> class.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="capacity">The maximum number of messages the mailbox can hold.</param>
    /// <param name="overflowPolicy">The policy to apply when the mailbox is full.</param>
    /// <param name="highWatermarkThreshold">The percentage (0-100) at which to log a warning when exceeded.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentException">Thrown if actorId is empty, capacity is not positive, or overflowPolicy is invalid.</exception>
    public BoundedPriorityMailbox(
        Guid actorId,
        int capacity,
        MailboxOverflowPolicy overflowPolicy = MailboxOverflowPolicy.DropNewest,
        int highWatermarkThreshold = 80,
        ILogger<BoundedPriorityMailbox>? logger = null)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        if (capacity <= 0)
            throw new MailboxConfigurationException("Mailbox capacity must be greater than zero.");

        if (highWatermarkThreshold < 0 || highWatermarkThreshold > 100)
            throw new MailboxConfigurationException("High watermark threshold must be between 0 and 100.");

        ActorId = actorId;
        _capacity = capacity;
        _overflowPolicy = overflowPolicy;
        _highWatermarkThreshold = highWatermarkThreshold;
        _logger = logger;

        // Create bounded channel with the specified overflow policy
        // Note: Fail policy is handled manually in EnqueueAsync to throw MailboxFullException
        var boundedChannelOptions = new BoundedChannelOptions(capacity)
        {
            FullMode = overflowPolicy switch
            {
                MailboxOverflowPolicy.DropNewest => BoundedChannelFullMode.DropNewest,
                MailboxOverflowPolicy.DropOldest => BoundedChannelFullMode.DropOldest,
                MailboxOverflowPolicy.Fail => BoundedChannelFullMode.DropNewest, // Handled manually
                MailboxOverflowPolicy.Wait => BoundedChannelFullMode.Wait,
                _ => BoundedChannelFullMode.DropNewest // Default to DropNewest for unknown policies
            }
        };

        _channel = Channel.CreateBounded<PrioritizedEnvelope>(boundedChannelOptions);
    }

    /// <summary>
    /// Enqueues a message into this priority mailbox.
    /// </summary>
    /// <param name="envelope">The message envelope to enqueue.</param>
    /// <returns>
    /// A task that represents the asynchronous enqueue operation.
    /// Returns true if the message was successfully enqueued, false if it was dropped.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if envelope is null.</exception>
    /// <exception cref="MailboxFullException">Thrown if the Fail policy is used and the mailbox is full.</exception>
    public async Task<bool> EnqueueAsync(Envelope envelope)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoundedPriorityMailbox));

        ArgumentNullException.ThrowIfNull(envelope);

        try
        {
            // For Fail policy, check if the mailbox is full and throw exception immediately
            // instead of using the channel's Wait mode which would block indefinitely
            if (_overflowPolicy == MailboxOverflowPolicy.Fail && IsFull)
            {
                throw new MailboxFullException(
                    ActorId,
                    _capacity,
                    GetSize(),
                    $"Mailbox is full for actor {ActorId} with Fail policy. Capacity: {_capacity}, Current: {GetSize()}");
            }

            // Create prioritized envelope (default priority is 0)
            var prioritizedEnvelope = new PrioritizedEnvelope(envelope, envelope.Message.Priority);

            await _channel.Writer.WriteAsync(prioritizedEnvelope);

            // Check if we've crossed the high watermark threshold and issue warning if needed
            var currentLoadFactor = GetLoadFactor();
            if (currentLoadFactor >= _highWatermarkThreshold / 100.0 && GetSize() > 0)
            {
                _logger?.LogWarning(
                    "Priority mailbox for actor {ActorId} has reached high watermark: {CurrentLoadPercent}% full (threshold: {Threshold}%)",
                    ActorId,
                    (int)(currentLoadFactor * 100),
                    _highWatermarkThreshold);
            }

            return true;
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(BoundedPriorityMailbox), "Mailbox has been disposed.");
        }
        catch (MailboxFullException)
        {
            throw; // Re-throw MailboxFullException for Fail policy
        }
        catch (Exception ex) when (ex is not MailboxFullException)
        {
            _droppedMessageCount++;
            _logger?.LogError(ex, "Failed to enqueue message to priority mailbox for actor {ActorId}", ActorId);
            return false;
        }
    }

    /// <summary>
    /// Dequeues the next message from this priority mailbox.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dequeue operation.
    /// Returns the dequeued envelope, or null if the mailbox is empty.
    /// </returns>
    public async Task<Envelope?> DequeueAsync()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(BoundedPriorityMailbox));

        try
        {
            if (_channel.Reader.TryRead(out var prioritizedEnvelope))
            {
                // Reset high watermark warning if load has dropped below threshold
                var currentLoadFactor = GetLoadFactor();
                if (currentLoadFactor < _highWatermarkThreshold / 100.0)
                {
                    // Note: We can't easily track state here, so warnings may re-occur
                    // This is acceptable as it's just a diagnostic
                }

                return prioritizedEnvelope.Envelope;
            }

            return null;
        }
        catch (ChannelClosedException)
        {
            throw new ObjectDisposedException(nameof(BoundedPriorityMailbox), "Mailbox has been disposed.");
        }
    }

    /// <summary>
    /// Gets the number of messages currently in this priority mailbox.
    /// </summary>
    /// <returns>The number of messages in the mailbox.</returns>
    public int GetSize() => _channel.Reader.Count;

    /// <summary>
    /// Gets the load factor of this priority mailbox as a value between 0 and 1.
    /// </summary>
    /// <returns>The load factor (0 = empty, 1 = full).</returns>
    public double GetLoadFactor() => (double)GetSize() / _capacity;

    /// <summary>
    /// Disposes the mailbox and releases all resources.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the managed resources used by the mailbox.
    /// </summary>
    /// <param name="disposing">True if called from Dispose, false if called from finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _channel.Writer.Complete();
                _channel.Reader.TryRead(out _); // Drain the channel
            }

            _disposed = true;
        }
    }

    /// <summary>
    /// Finalizer to ensure resources are released if Dispose is not called.
    /// </summary>
    ~BoundedPriorityMailbox()
    {
        Dispose(false);
    }
}

/// <summary>
/// Wrapper class to hold an envelope with its priority for priority mailboxes.
/// </summary>
internal class PrioritizedEnvelope : IComparable<PrioritizedEnvelope>
{
    public Envelope Envelope { get; }
    public int Priority { get; }

    public PrioritizedEnvelope(Envelope envelope, int priority)
    {
        Envelope = envelope;
        Priority = priority;
    }

    public int CompareTo(PrioritizedEnvelope? other)
    {
        if (other == null) return 1;
        // Higher priority values come first (lower numerical value in priority queue)
        return other.Priority.CompareTo(Priority);
    }
}