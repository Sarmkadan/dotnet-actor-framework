// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace DotNetActorFramework.Models;

/// <summary>
/// Base class for all actor messages. Every message exchanged between actors
/// must derive from this type.
/// </summary>
/// <remarks>
/// Messages are immutable records. Custom message types should add domain-specific
/// properties while keeping the base metadata (<see cref="MessageId"/>,
/// <see cref="CreatedAt"/>, <see cref="Priority"/>) intact.
/// The framework ships with several built-in subtypes:
/// <list type="bullet">
///   <item><see cref="ControlMessage"/> - command-style messages with string command and parameters</item>
///   <item><see cref="ResponseMessage"/> - request/response pattern replies</item>
///   <item><see cref="FailureMessage"/> - error propagation between actors</item>
///   <item><see cref="Message{T}"/> - strongly-typed payload carrier</item>
/// </list>
/// </remarks>
[JsonDerivedType(typeof(Message), typeDiscriminator: "message")]
public abstract record Message
{
    /// <summary>Globally unique identifier for correlation and deduplication.</summary>
    public Guid MessageId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Correlation identifier used to link a reply (<see cref="ResponseMessage"/> or
    /// <see cref="FailureMessage"/>) back to the request that caused it. Defaults to
    /// <see cref="Guid.Empty"/> for messages that do not answer a prior request.
    /// </summary>
    public Guid CorrelationId { get; init; } = Guid.Empty;

    /// <summary>UTC timestamp of when the message was created.</summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Delivery priority hint. Higher values are processed first when the
    /// mailbox supports priority ordering. Default is <c>0</c> (normal).
    /// </summary>
    public int Priority { get; init; } = 0;

    protected Message()
    {
    }

    protected Message(Guid correlationId)
    {
        CorrelationId = correlationId;
    }
}

/// <summary>
/// A strongly-typed message that carries a payload of type <typeparamref name="T"/>.
/// Use this when you want compile-time type safety for message content rather
/// than relying on dictionary-based parameters.
/// </summary>
/// <typeparam name="T">The type of the payload. Must be a reference type.</typeparam>
/// <example>
/// <code>
/// public record OrderPayload(string OrderId, decimal Amount);
/// var msg = new Message&lt;OrderPayload&gt;(new OrderPayload("ORD-1", 49.99m));
/// await dispatcher.SendAsync(actorRef, msg);
/// </code>
/// </example>
[JsonDerivedType(typeof(Message<>), typeDiscriminator: "typedMessage")]
public record Message<T> : Message where T : class
{
    /// <summary>The typed payload carried by this message.</summary>
    public T Payload { get; init; }

    public Message(T payload)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}

/// <summary>
/// A command-style message carrying a string command name and optional key/value parameters.
/// This is the most common message type for general actor communication.
/// </summary>
/// <example>
/// <code>
/// var msg = new ControlMessage("processOrder", new Dictionary&lt;string, object&gt;
/// {
///     { "orderId", "ORD-123" },
///     { "priority", 1 }
/// });
/// </code>
/// </example>
public record ControlMessage : Message
{
    /// <summary>The command verb identifying the action to perform (e.g. "start", "process", "shutdown").</summary>
    public string Command { get; init; }

    /// <summary>Optional key/value parameters associated with the command.</summary>
    public Dictionary<string, object> Parameters { get; init; } = [];

    public ControlMessage(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be null or empty.", nameof(command));

        Command = command;
    }

    public ControlMessage(string command, Dictionary<string, object> parameters)
        : this(command)
    {
        Parameters = parameters ?? [];
    }
}

/// <summary>
/// A response message sent back from an actor, answering a prior request.
/// Kept as an untyped base for wire compatibility; prefer <see cref="ResponseMessage{T}"/>
/// when the response payload has a known type.
/// </summary>
/// <example>
/// <code>
/// var reply = new ResponseMessage(result, isSuccess: true) { CorrelationId = request.MessageId };
/// </code>
/// </example>
public record ResponseMessage : Message
{
    /// <summary>The untyped response payload, or <c>null</c> when the operation produced no value.</summary>
    public object? Response { get; init; }

    /// <summary>Whether the operation the response answers completed successfully.</summary>
    public bool IsSuccess { get; init; }

    /// <summary>Human-readable error description when <see cref="IsSuccess"/> is <c>false</c>.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Initializes a new <see cref="ResponseMessage"/>.
    /// </summary>
    /// <param name="response">The untyped response payload, or <c>null</c>.</param>
    /// <param name="isSuccess">Whether the answered operation succeeded. Defaults to <c>true</c>.</param>
    /// <param name="errorMessage">Optional error description when <paramref name="isSuccess"/> is <c>false</c>.</param>
    /// <param name="correlationId">Optional correlation ID of the request being answered.</param>
    public ResponseMessage(object? response, bool isSuccess = true, string? errorMessage = null, Guid correlationId = default)
        : base(correlationId)
    {
        Response = response;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// A strongly-typed response message that carries a response payload of type
/// <typeparamref name="T"/> and correlates back to the request it answers via
/// <see cref="Message.CorrelationId"/>.
/// </summary>
/// <typeparam name="T">The type of the response payload. Must be a reference type.</typeparam>
/// <example>
/// <code>
/// var reply = new ResponseMessage&lt;OrderResult&gt;(result, request.MessageId);
/// </code>
/// </example>
public record ResponseMessage<T> : ResponseMessage where T : class
{
    /// <summary>The strongly-typed response payload.</summary>
    public T Payload { get; init; }

    /// <summary>
    /// Initializes a new successful <see cref="ResponseMessage{T}"/> that answers the request
    /// identified by <paramref name="correlationId"/>.
    /// </summary>
    /// <param name="response">The typed response payload.</param>
    /// <param name="correlationId">The <see cref="Message.MessageId"/> of the request being answered.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="response"/> is <c>null</c>.</exception>
    public ResponseMessage(T response, Guid correlationId)
        : base(response, isSuccess: true, correlationId: correlationId)
    {
        ArgumentNullException.ThrowIfNull(response);

        Payload = response;
    }
}

/// <summary>
/// A failure message indicating an error occurred while processing a request. Carries
/// enough serializable detail about the originating exception (type name, message, stack
/// trace) to survive persistence via <c>IEventJournal</c> without requiring the raw
/// <see cref="Exception"/> instance to be preserved.
/// </summary>
public record FailureMessage : Message
{
    /// <summary>Human-readable description of what went wrong.</summary>
    public string Reason { get; init; }

    /// <summary>The full type name of the originating exception, or <c>null</c> when none was supplied.</summary>
    public string? ExceptionType { get; init; }

    /// <summary>The <see cref="Exception.Message"/> of the originating exception, or <c>null</c> when none was supplied.</summary>
    public string? ExceptionMessage { get; init; }

    /// <summary>The <see cref="Exception.StackTrace"/> of the originating exception, or <c>null</c> when none was supplied.</summary>
    public string? StackTrace { get; init; }

    /// <summary>UTC timestamp of when the failure was recorded.</summary>
    public DateTime FailureTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Initializes a new <see cref="FailureMessage"/> without an associated exception.
    /// </summary>
    /// <param name="reason">Human-readable description of the failure.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
    public FailureMessage(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));

        Reason = reason;
    }

    /// <summary>
    /// Initializes a new <see cref="FailureMessage"/> capturing the type name, message and
    /// stack trace of <paramref name="exception"/> so the failure remains serializable
    /// for journaling purposes.
    /// </summary>
    /// <param name="reason">Human-readable description of the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <c>null</c>.</exception>
    public FailureMessage(string reason, Exception exception)
        : this(reason)
    {
        ArgumentNullException.ThrowIfNull(exception);

        ExceptionType = exception.GetType().FullName;
        ExceptionMessage = exception.Message;
        StackTrace = exception.StackTrace;
    }

    /// <summary>
    /// Initializes a new <see cref="FailureMessage"/> that answers the request identified by
    /// <paramref name="correlationId"/>, capturing serializable detail about <paramref name="exception"/>.
    /// </summary>
    /// <param name="reason">Human-readable description of the failure.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="correlationId">The <see cref="Message.MessageId"/> of the request being answered.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is <c>null</c>.</exception>
    public FailureMessage(string reason, Exception exception, Guid correlationId)
        : base(correlationId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));

        ArgumentNullException.ThrowIfNull(exception);

        Reason = reason;
        ExceptionType = exception.GetType().FullName;
        ExceptionMessage = exception.Message;
        StackTrace = exception.StackTrace;
    }
}
