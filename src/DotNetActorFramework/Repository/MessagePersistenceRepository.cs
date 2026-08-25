// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Repository;

/// <summary>
/// Repository for persisting messages for durability and replay.
/// Provides append-only log semantics for message storage.
/// </summary>
public class MessagePersistenceRepository
{
    private readonly ConnectionManager _connectionManager;
    private readonly Queue<PersistedMessage> _messageLog = [];
    private readonly object _lockObject = new();
    private long _sequenceNumber;

    public MessagePersistenceRepository(ConnectionManager connectionManager)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _sequenceNumber = 0;
    }

    /// <summary>
    /// Persists a message envelope.
    /// </summary>
    public async Task<bool> PersistAsync(Envelope envelope)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        try
        {
            var persistedMessage = new PersistedMessage
            {
                EnvelopeId = envelope.EnvelopeId,
                MessageType = envelope.Message.GetType().FullName ?? "Unknown",
                SenderId = envelope.Sender?.Id,
                RecipientId = envelope.Recipient.Id,
                PersistedAt = DateTime.UtcNow,
                IsDelivered = envelope.IsDelivered,
                SequenceNumber = Interlocked.Increment(ref _sequenceNumber)
            };

            lock (_lockObject)
            {
                _messageLog.Enqueue(persistedMessage);

                // Keep only the last 100,000 messages to prevent memory issues
                while (_messageLog.Count > 100000)
                {
                    _messageLog.Dequeue();
                }
            }

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets messages for a specific actor.
    /// </summary>
    public Task<IReadOnlyList<PersistedMessage>> GetActorMessagesAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        IReadOnlyList<PersistedMessage> messages;
        lock (_lockObject)
        {
            messages = _messageLog
                .Where(m => m.RecipientId == actorId)
                .ToList()
                .AsReadOnly();
        }

        return Task.FromResult(messages);
    }

    /// <summary>
    /// Gets undelivered messages.
    /// </summary>
    public Task<IReadOnlyList<PersistedMessage>> GetUndeliveredMessagesAsync()
    {
        IReadOnlyList<PersistedMessage> messages;
        lock (_lockObject)
        {
            messages = _messageLog
                .Where(m => !m.IsDelivered)
                .ToList()
                .AsReadOnly();
        }

        return Task.FromResult(messages);
    }

    /// <summary>
    /// Gets messages between two sequence numbers.
    /// </summary>
    public Task<IReadOnlyList<PersistedMessage>> GetMessagesAsync(long fromSequence, long toSequence)
    {
        if (fromSequence < 0)
            throw new ArgumentException("From sequence cannot be negative.", nameof(fromSequence));

        if (toSequence < fromSequence)
            throw new ArgumentException("To sequence must be greater than or equal to from sequence.", nameof(toSequence));

        IReadOnlyList<PersistedMessage> messages;
        lock (_lockObject)
        {
            messages = _messageLog
                .Where(m => m.SequenceNumber >= fromSequence && m.SequenceNumber <= toSequence)
                .ToList()
                .AsReadOnly();
        }

        return Task.FromResult(messages);
    }

    /// <summary>
    /// Gets the total count of persisted messages.
    /// </summary>
    public long GetMessageCount()
    {
        lock (_lockObject)
        {
            return _messageLog.Count;
        }
    }

    /// <summary>
    /// Gets the current sequence number.
    /// </summary>
    public long GetCurrentSequenceNumber()
    {
        lock (_lockObject)
        {
            return _sequenceNumber;
        }
    }

    /// <summary>
    /// Gets persistence statistics.
    /// </summary>
    public PersistenceStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            var undelivered = _messageLog.Count(m => !m.IsDelivered);
            var delivered = _messageLog.Count - undelivered;

            return new PersistenceStatistics
            {
                TotalMessages = _messageLog.Count,
                DeliveredMessages = delivered,
                UndeliveredMessages = undelivered,
                CurrentSequenceNumber = _sequenceNumber,
                OldestMessageTime = _messageLog.FirstOrDefault()?.PersistedAt,
                NewestMessageTime = _messageLog.LastOrDefault()?.PersistedAt
            };
        }
    }

    /// <summary>
    /// Marks a message as delivered.
    /// </summary>
    public async Task<bool> MarkAsDeliveredAsync(Guid envelopeId)
    {
        if (envelopeId == Guid.Empty)
            throw new ArgumentException("Envelope ID cannot be empty.", nameof(envelopeId));

        lock (_lockObject)
        {
            var message = _messageLog.FirstOrDefault(m => m.EnvelopeId == envelopeId);
            if (message != null)
            {
                message.IsDelivered = true;
            }
        }

        await Task.CompletedTask;
        return true;
    }

    /// <summary>
    /// Clears all persisted messages.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _messageLog.Clear();
            _sequenceNumber = 0;
        }
    }

    /// <summary>
    /// Returns a concise, informative representation of the repository,
    /// describing its most recently persisted message.
    /// </summary>
    public override string ToString()
    {
        lock (_lockObject)
        {
            var last = _messageLog.LastOrDefault();
            return $"MessagePersistenceRepository {{ EnvelopeId = {last?.EnvelopeId}, MessageType = {last?.MessageType ?? "Unknown"}, SenderId = {last?.SenderId}, RecipientId = {last?.RecipientId}, PersistedAt = {last?.PersistedAt}, IsDelivered = {last?.IsDelivered} }}";
        }
    }
}

/// <summary>
/// Represents a persisted message.
/// </summary>
public class PersistedMessage
{
    public Guid EnvelopeId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public Guid? SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public DateTime PersistedAt { get; set; }
    public bool IsDelivered { get; set; }
    public long SequenceNumber { get; set; }
}

/// <summary>
/// Statistics about message persistence.
/// </summary>
public class PersistenceStatistics
{
    public long TotalMessages { get; set; }
    public long DeliveredMessages { get; set; }
    public long UndeliveredMessages { get; set; }
    public long CurrentSequenceNumber { get; set; }
    public DateTime? OldestMessageTime { get; set; }
    public DateTime? NewestMessageTime { get; set; }

    public double GetDeliveryRate()
    {
        if (TotalMessages == 0) return 0;
        return (double)DeliveredMessages / TotalMessages * 100;
    }
}
