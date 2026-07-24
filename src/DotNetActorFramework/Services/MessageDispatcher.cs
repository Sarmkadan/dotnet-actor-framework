// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Constants;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Messaging;

namespace DotNetActorFramework.Services;

/// <summary>
/// Handles message delivery and routing between actors.
/// Provides functionality for sending messages, managing delivery guarantees, and handling failures.
/// </summary>
public class MessageDispatcher
{
    private readonly MailboxService _mailboxService;
    private readonly ActorRegistry _registry;
    private readonly ActorSystem _actorSystem;
    private readonly Queue<Envelope> _deadLetterQueue = [];
    private int _totalDelivered;
    private int _totalFailed;
    private readonly object _lockObject = new();

    public MessageDispatcher(MailboxService mailboxService, ActorRegistry registry, ActorSystem actorSystem)
    {
        _mailboxService = mailboxService ?? throw new ArgumentNullException(nameof(mailboxService));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));
    }

    /// <summary>
    /// Dispatches a message to an actor asynchronously.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when envelope is null.</exception>
    /// <exception cref="ActorNotFoundException">Thrown when recipient actor doesn't exist.</exception>
    /// <exception cref="MailboxException">Thrown when mailbox operations fail.</exception>
    public async Task<bool> DispatchAsync(Envelope envelope)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        try
        {
            // Replies to a pending Ask (see AskExtensions.AskAsync) are consumed by the
            // waiting caller instead of being enqueued to the recipient's mailbox.
            if (envelope.Message is ResponseMessage or FailureMessage && AskRegistry.TryComplete(envelope.Message))
            {
                envelope.MarkAsDelivered();
                IncrementDelivered();
                return true;
            }

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
                catch (MailboxException ex) when (attempt < maxRetries)
                {
                    envelope.IncrementRetryCount();
                    await Task.Delay(backoffDelay);
                    backoffDelay = (int)Math.Min(
                        backoffDelay * ActorConstants.BackoffMultiplier,
                        ActorConstants.MaxBackoffDelayMs
                    );
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    // Log unexpected errors during enqueue attempts
                    envelope.IncrementRetryCount();
                    await Task.Delay(backoffDelay);
                    backoffDelay = (int)Math.Min(
                        backoffDelay * ActorConstants.BackoffMultiplier,
                        ActorConstants.MaxBackoffDelayMs
                    );
                }
            }

            // Failed after all retries - invoke dead-letter handler
            IncrementFailed();
            _actorSystem.InvokeDeadLetterHandler(envelope);
            AddToDeadLetterQueue(envelope);
            return false;
        }
        catch (ActorNotFoundException ex)
        {
            // Invoke dead-letter handler for undeliverable messages
            _actorSystem.InvokeDeadLetterHandler(envelope);
            IncrementFailed();
            AddToDeadLetterQueue(envelope);
            throw new MessageDispatchException(envelope.Recipient.Path.ToString(), "Recipient actor not found", ex);
        }
        catch (Exception ex)
        {
            // Invoke dead-letter handler for undeliverable messages
            _actorSystem.InvokeDeadLetterHandler(envelope);
            IncrementFailed();
            AddToDeadLetterQueue(envelope);
            throw new MessageDispatchException(envelope.Recipient.Path.ToString(), "Failed to dispatch message", ex);
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
    /// Sends a strongly-typed response back to the sender of <paramref name="request"/>,
    /// automatically correlating it via <see cref="Message.MessageId"/>.
    /// </summary>
    /// <typeparam name="T">The type of the response payload. Must be a reference type.</typeparam>
    /// <param name="recipient">The actor the response is sent to (typically the original sender).</param>
    /// <param name="response">The typed response payload.</param>
    /// <param name="request">The request message being answered.</param>
    /// <param name="sender">The actor sending the response, or <c>null</c> when unspecified.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="recipient"/>, <paramref name="response"/> or <paramref name="request"/> is <c>null</c>.</exception>
    public async Task ReplyAsync<T>(ActorRef recipient, T response, Message request, ActorRef? sender = null) where T : class
    {
        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        ArgumentNullException.ThrowIfNull(response);

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var responseMessage = new ResponseMessage<T>(response, request.MessageId);

        if (sender != null)
            await SendAsync(sender, recipient, responseMessage);
        else
            await SendAsync(recipient, responseMessage);
    }

    /// <summary>
    /// Sends a <see cref="FailureMessage"/> back to the sender of <paramref name="request"/>,
    /// automatically correlating it via <see cref="Message.MessageId"/> and capturing
    /// serializable detail from <paramref name="exception"/>.
    /// </summary>
    /// <param name="recipient">The actor the failure is sent to (typically the original sender).</param>
    /// <param name="reason">Human-readable description of the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="request">The request message that failed to be answered.</param>
    /// <param name="sender">The actor sending the failure, or <c>null</c> when unspecified.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="recipient"/>, <paramref name="exception"/> or <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
    public async Task ReplyWithFailureAsync(ActorRef recipient, string reason, Exception exception, Message request, ActorRef? sender = null)
    {
        if (recipient == null)
            throw new ArgumentNullException(nameof(recipient));

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var failureMessage = new FailureMessage(reason, exception, request.MessageId);

        if (sender != null)
            await SendAsync(sender, recipient, failureMessage);
        else
            await SendAsync(recipient, failureMessage);
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
