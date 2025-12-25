// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Base exception for all actor-related errors.
/// All actor-specific exceptions should inherit from this class.
/// </summary>
public class ActorException : DotnetActorFrameworkException
{
    public ActorException(string? message) : base(message)
    {
    }

    public ActorException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates an ActorException with formatted message.
    /// </summary>
    public new static ActorException Create(string format, params object?[] args)
    {
        return new ActorException(string.Format(format, args));
    }

    /// <summary>
    /// Creates an ActorException with inner exception.
    /// </summary>
    public new static ActorException Create(Exception innerException, string format, params object?[] args)
    {
        return new ActorException(string.Format(format, args), innerException);
    }
}

/// <summary>
/// Thrown when an actor cannot be found in the system.
/// </summary>
public class ActorNotFoundException : ActorException
{
    public string ActorPath { get; }

    public ActorNotFoundException(string actorPath)
        : base($"Actor not found: {actorPath}")
    {
        ActorPath = actorPath;
    }

    public ActorNotFoundException(string actorPath, Exception? innerException)
        : base($"Actor not found: {actorPath}", innerException)
    {
        ActorPath = actorPath;
    }
}

/// <summary>
/// Thrown when a mailbox operation fails.
/// </summary>
public class MailboxException : ActorException
{
    public Guid ActorId { get; }

    public MailboxException(string? message) : base(message)
    {
    }

    public MailboxException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public MailboxException(Guid actorId, string? message) : base(message)
    {
        ActorId = actorId;
    }

    public MailboxException(Guid actorId, string? message, Exception? innerException)
        : base(message, innerException)
    {
        ActorId = actorId;
    }
}

/// <summary>
/// Thrown when a supervision operation fails.
/// </summary>
public class SupervisionException : ActorException
{
    public SupervisionException(string? message) : base(message)
    {
    }

    public SupervisionException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when an actor system operation fails.
/// </summary>
public class ActorSystemException : ActorException
{
    public ActorSystemException(string? message) : base(message)
    {
    }

    public ActorSystemException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when HTTP communication with actors fails.
/// </summary>
public class HttpActorCommunicationException : ActorException
{
    public HttpStatusCode? StatusCode { get; }
    public string? RequestUrl { get; }

    public HttpActorCommunicationException(string? message) : base(message)
    {
    }

    public HttpActorCommunicationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public HttpActorCommunicationException(string requestUrl, HttpStatusCode statusCode, string? responseContent)
        : base($"HTTP communication failed for {requestUrl}. Status: {statusCode}, Response: {responseContent?.Truncate(200)}")
    {
        RequestUrl = requestUrl;
        StatusCode = statusCode;
    }

    public HttpActorCommunicationException(string requestUrl, HttpStatusCode statusCode, string? responseContent, Exception? innerException)
        : base($"HTTP communication failed for {requestUrl}. Status: {statusCode}, Response: {responseContent?.Truncate(200)}", innerException)
    {
        RequestUrl = requestUrl;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thrown when external service integration fails.
/// </summary>
public class ExternalServiceException : ActorException
{
    public string ServiceName { get; }
    public string Endpoint { get; }

    public ExternalServiceException(string serviceName, string endpoint, string? message)
        : base(message ?? $"External service '{serviceName}' failed at endpoint '{endpoint}'")
    {
        ServiceName = serviceName;
        Endpoint = endpoint;
    }

    public ExternalServiceException(string serviceName, string endpoint, string? message, Exception? innerException)
        : base(message ?? $"External service '{serviceName}' failed at endpoint '{endpoint}'", innerException)
    {
        ServiceName = serviceName;
        Endpoint = endpoint;
    }
}

/// <summary>
/// Thrown when message serialization/deserialization fails.
/// </summary>
public class SerializationException : ActorException
{
    public string ContentType { get; }

    public SerializationException(string contentType, string? message) : base(message ?? $"Serialization failed for content type: {contentType}")
    {
        ContentType = contentType;
    }

    public SerializationException(string contentType, string? message, Exception? innerException)
        : base(message ?? $"Serialization failed for content type: {contentType}", innerException)
    {
        ContentType = contentType;
    }
}

/// <summary>
/// Thrown when persistence operations fail.
/// </summary>
public class PersistenceException : ActorException
{
    public string ActorPath { get; }

    public PersistenceException(string actorPath, string? message) : base(message ?? $"Persistence operation failed for actor: {actorPath}")
    {
        ActorPath = actorPath;
    }

    public PersistenceException(string actorPath, string? message, Exception? innerException)
        : base(message ?? $"Persistence operation failed for actor: {actorPath}", innerException)
    {
        ActorPath = actorPath;
    }
}

/// <summary>
/// Thrown when cluster operations fail.
/// </summary>
public class ClusterException : ActorException
{
    public string NodeAddress { get; }

    public ClusterException(string nodeAddress, string? message) : base(message ?? $"Cluster operation failed for node: {nodeAddress}")
    {
        NodeAddress = nodeAddress;
    }

    public ClusterException(string nodeAddress, string? message, Exception? innerException)
        : base(message ?? $"Cluster operation failed for node: {nodeAddress}", innerException)
    {
        NodeAddress = nodeAddress;
    }
}

/// <summary>
/// Thrown when message dispatch operations fail.
/// </summary>
public class MessageDispatchException : ActorException
{
    public string ActorPath { get; }

    public MessageDispatchException(string actorPath, string? message) : base(message ?? $"Failed to dispatch message to actor: {actorPath}")
    {
        ActorPath = actorPath;
    }

    public MessageDispatchException(string actorPath, string? message, Exception? innerException)
        : base(message ?? $"Failed to dispatch message to actor: {actorPath}", innerException)
    {
        ActorPath = actorPath;
    }
}

/// <summary>
/// Extension method for string truncation.
/// </summary>
internal static class StringExtensions
{
    public static string Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;

        return value[..maxLength] + "...";
    }
}