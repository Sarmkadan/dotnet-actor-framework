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
/// A response message sent back from an actor.
/// </summary>
public record ResponseMessage : Message
{
    public object? Response { get; init; }
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public ResponseMessage(object? response, bool isSuccess = true, string? errorMessage = null)
    {
        Response = response;
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// A failure message indicating an error occurred.
/// </summary>
public record FailureMessage : Message
{
    public string Reason { get; init; }
    public string? StackTrace { get; init; }
    public DateTime FailureTime { get; init; } = DateTime.UtcNow;

    public FailureMessage(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));

        Reason = reason;
    }

    public FailureMessage(string reason, Exception exception)
        : this(reason)
    {
        StackTrace = exception?.StackTrace;
    }
}
