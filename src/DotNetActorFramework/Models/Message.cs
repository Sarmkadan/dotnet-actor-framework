// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;

namespace DotNetActorFramework.Models;

/// <summary>
/// Base class for all actor messages.
/// </summary>
[JsonDerivedType(typeof(Message), typeDiscriminator: "message")]
public abstract record Message
{
    public Guid MessageId { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int Priority { get; init; } = 0;

    protected Message()
    {
    }
}

/// <summary>
/// A typed message that carries a payload.
/// </summary>
[JsonDerivedType(typeof(Message<>), typeDiscriminator: "typedMessage")]
public record Message<T> : Message where T : class
{
    public T Payload { get; init; }

    public Message(T payload)
    {
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }
}

/// <summary>
/// A control message for system operations.
/// </summary>
public record ControlMessage : Message
{
    public string Command { get; init; }
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
