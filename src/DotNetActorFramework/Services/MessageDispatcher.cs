// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Constants;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Services;

/// <summary>
/// Handles message delivery and routing between actors.
/// Provides functionality for sending messages, managing delivery guarantees, and handling failures.
/// </summary>
public class MessageDispatcher
{
    private readonly MailboxService _mailboxService;
    private readonly ActorRegistry _registry;
    private readonly Queue<Envelope> _deadLetterQueue = [];
    private int _totalDelivered;
    private int _totalFailed;
    private readonly object _lockObject = new();

    public MessageDispatcher(MailboxService mailboxService, ActorRegistry registry)
    {
        _mailboxService = mailboxService ?? throw new ArgumentNullException(nameof(mailboxService));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>
    /// Dispatches a message to an actor asynchronously.
    /// </summary>
    public async Task<bool> DispatchAsync(Envelope envelope)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        try
        {
            // Check if recipient exists
            if (!_registry.Contains(envelope.Recipient.Path))
                throw new ActorNotFoundException(envelope.Recipient.Path.ToString());

            // Try to enqueue the message
            var maxRetries = ActorConstants.MaxMessageRetries;
            var backoffDelay = ActorConstants.InitialBackoffDelayMs;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await _mailboxService.EnqueueAsync(envelope.Recipient.Id, envelope);
                    envelope.MarkAsDelivered();
                    IncrementDelivered();
                    return true;
                }
                catch (MailboxException) when (attempt < maxRetries)
                {
                    envelope.IncrementRetryCount();
                    await Task.Delay(backoffDelay);
                    backoffDelay = (int)Math.Min(
                        backoffDelay * ActorConstants.BackoffMultiplier,
                        ActorConstants.MaxBackoffDelayMs
                    );
                }
            }

            // Failed after all retries
            IncrementFailed();
            AddToDeadLetterQueue(envelope);
            return false;
        }
        catch (ActorNotFoundException)
        {
            IncrementFailed();
            AddToDeadLetterQueue(envelope);
            return false;
        }
    }

    /// <summary>
    /// Sends a message from one actor to another.
    /// </summary>
    public async Task SendAsync(ActorRef sender, ActorRef recipient, Message message)
    {
        if (sender == null)
            throw new ArgumentNullException(nameof(sender));

        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var envelope = new Envelope(message, recipient, sender);
        await DispatchAsync(envelope);
    }

    /// <summary>
    /// Sends a message to an actor without a sender.
    /// </summary>
    public async Task SendAsync(ActorRef recipient, Message message)
    {
        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var envelope = new Envelope(message, recipient);
        await DispatchAsync(envelope);
    }

    /// <summary>
    /// Broadcasts a message to multiple actors.
    /// </summary>
    public async Task BroadcastAsync(IEnumerable<ActorRef> recipients, Message message, ActorRef? sender = null)
    {
        if (recipients == null)
            throw new ArgumentNullException(nameof(recipients));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var tasks = recipients
            .Select(recipient => new Envelope(message, recipient, sender))
            .Select(envelope => DispatchAsync(envelope));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Publishes a control message to an actor.
    /// </summary>
    public async Task PublishControlAsync(ActorRef recipient, string command, Dictionary<string, object>? parameters = null)
    {
        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));

        var controlMessage = new ControlMessage(command, parameters ?? []);
        await SendAsync(recipient, controlMessage);
    }

    /// <summary>
    /// Gets the next message for an actor to process.
    /// </summary>
    public async Task<Envelope?> GetNextMessageAsync(Guid actorId)
    {
        if (actorId == Guid.Empty)
            throw new ArgumentException("Actor ID cannot be empty.", nameof(actorId));

        return await _mailboxService.DequeueAsync(actorId);
    }

    /// <summary>
    /// Gets messages from the dead letter queue.
    /// </summary>
    public IReadOnlyList<Envelope> GetDeadLetters()
    {
        lock (_lockObject)
        {
            return _deadLetterQueue.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets dispatcher statistics.
    /// </summary>
    public DispatcherStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            var total = _totalDelivered + _totalFailed;
            return new DispatcherStatistics
            {
                TotalDelivered = _totalDelivered,
                TotalFailed = _totalFailed,
                TotalProcessed = total,
                DeadLetterCount = _deadLetterQueue.Count,
                SuccessRate = total > 0 ? (double)_totalDelivered / total * 100 : 0
            };
        }
    }

    private void IncrementDelivered()
    {
        lock (_lockObject)
        {
            _totalDelivered++;
        }
    }

    private void IncrementFailed()
    {
        lock (_lockObject)
        {
            _totalFailed++;
        }
    }

    private void AddToDeadLetterQueue(Envelope envelope)
    {
        lock (_lockObject)
        {
            _deadLetterQueue.Enqueue(envelope);
            if (_deadLetterQueue.Count > 10000)
            {
                _deadLetterQueue.Dequeue(); // Remove oldest
            }
        }
    }
}

/// <summary>
/// Statistics for message dispatch operations.
/// </summary>
public class DispatcherStatistics
{
    public long TotalDelivered { get; set; }
    public long TotalFailed { get; set; }
    public long TotalProcessed { get; set; }
    public int DeadLetterCount { get; set; }
    public double SuccessRate { get; set; }
}
