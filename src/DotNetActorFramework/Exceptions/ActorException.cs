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
    /// <summary>
    /// Initializes a new instance of the <see cref="ActorException"/> class.
    /// </summary>
    public ActorException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ActorException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
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
    /// <summary>
    /// Gets the path of the actor that could not be found.
    /// </summary>
    public string ActorPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorNotFoundException"/> class for the specified actor path.
    /// </summary>
    /// <param name="actorPath">The path of the actor that could not be found.</param>
    public ActorNotFoundException(string actorPath)
        : base($"Actor not found: {actorPath}")
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ActorPath = actorPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorNotFoundException"/> class for the specified actor path
    /// and inner exception.
    /// </summary>
    /// <param name="actorPath">The path of the actor that could not be found.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public ActorNotFoundException(string actorPath, Exception? innerException)
        : base($"Actor not found: {actorPath}", innerException)
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ActorPath = actorPath;
    }
}

/// <summary>
/// Thrown when a mailbox operation fails.
/// </summary>
public class MailboxException : ActorException
{
    /// <summary>
    /// Gets the identifier of the actor whose mailbox operation failed.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MailboxException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public MailboxException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxException"/> class for the specified actor with a message.
    /// </summary>
    /// <param name="actorId">The identifier of the actor whose mailbox operation failed.</param>
    /// <param name="message">The message that describes the error.</param>
    public MailboxException(Guid actorId, string? message) : base(message)
    {
        ActorId = actorId;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxException"/> class for the specified actor with a message
    /// and inner exception.
    /// </summary>
    /// <param name="actorId">The identifier of the actor whose mailbox operation failed.</param>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
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
    /// <summary>
    /// Initializes a new instance of the <see cref="SupervisionException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public SupervisionException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SupervisionException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
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
    /// <summary>
    /// Initializes a new instance of the <see cref="ActorSystemException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ActorSystemException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorSystemException"/> class with a message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
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
    /// <summary>
    /// Gets the HTTP status code returned by the actor endpoint, if one was received.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    /// <summary>
    /// Gets the URL of the request that failed, if known.
    /// </summary>
    public string? RequestUrl { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpActorCommunicationException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public HttpActorCommunicationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpActorCommunicationException"/> class with a message and
    /// inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public HttpActorCommunicationException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpActorCommunicationException"/> class for a failed request.
    /// </summary>
    /// <param name="requestUrl">The URL of the request that failed.</param>
    /// <param name="statusCode">The HTTP status code returned by the endpoint.</param>
    /// <param name="responseContent">The response body, truncated in the message if long.</param>
    public HttpActorCommunicationException(string requestUrl, HttpStatusCode statusCode, string? responseContent)
        : base($"HTTP communication failed for {requestUrl}. Status: {statusCode}, Response: {responseContent?.Truncate(200)}")
    {
        ArgumentNullException.ThrowIfNull(requestUrl);
        RequestUrl = requestUrl;
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpActorCommunicationException"/> class for a failed request
    /// with an inner exception.
    /// </summary>
    /// <param name="requestUrl">The URL of the request that failed.</param>
    /// <param name="statusCode">The HTTP status code returned by the endpoint.</param>
    /// <param name="responseContent">The response body, truncated in the message if long.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public HttpActorCommunicationException(string requestUrl, HttpStatusCode statusCode, string? responseContent, Exception? innerException)
        : base($"HTTP communication failed for {requestUrl}. Status: {statusCode}, Response: {responseContent?.Truncate(200)}", innerException)
    {
        ArgumentNullException.ThrowIfNull(requestUrl);
        RequestUrl = requestUrl;
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thrown when external service integration fails.
/// </summary>
public class ExternalServiceException : ActorException
{
    /// <summary>
    /// Gets the name of the external service that failed.
    /// </summary>
    public string ServiceName { get; }

    /// <summary>
    /// Gets the endpoint of the external service that failed.
    /// </summary>
    public string Endpoint { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class for the specified service and endpoint.
    /// </summary>
    /// <param name="serviceName">The name of the external service that failed.</param>
    /// <param name="endpoint">The endpoint of the external service that failed.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    public ExternalServiceException(string serviceName, string endpoint, string? message)
        : base(message ?? $"External service '{serviceName}' failed at endpoint '{endpoint}'")
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(endpoint);
        ServiceName = serviceName;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExternalServiceException"/> class for the specified service and
    /// endpoint with an inner exception.
    /// </summary>
    /// <param name="serviceName">The name of the external service that failed.</param>
    /// <param name="endpoint">The endpoint of the external service that failed.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public ExternalServiceException(string serviceName, string endpoint, string? message, Exception? innerException)
        : base(message ?? $"External service '{serviceName}' failed at endpoint '{endpoint}'", innerException)
    {
        ArgumentNullException.ThrowIfNull(serviceName);
        ArgumentNullException.ThrowIfNull(endpoint);
        ServiceName = serviceName;
        Endpoint = endpoint;
    }
}

/// <summary>
/// Thrown when message serialization/deserialization fails.
/// </summary>
public class SerializationException : ActorException
{
    /// <summary>
    /// Gets the content type that failed to serialize or deserialize.
    /// </summary>
    public string ContentType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationException"/> class for the specified content type.
    /// </summary>
    /// <param name="contentType">The content type that failed to serialize or deserialize.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    public SerializationException(string contentType, string? message) : base(message ?? $"Serialization failed for content type: {contentType}")
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ContentType = contentType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SerializationException"/> class for the specified content type
    /// with an inner exception.
    /// </summary>
    /// <param name="contentType">The content type that failed to serialize or deserialize.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public SerializationException(string contentType, string? message, Exception? innerException)
        : base(message ?? $"Serialization failed for content type: {contentType}", innerException)
    {
        ArgumentNullException.ThrowIfNull(contentType);
        ContentType = contentType;
    }
}

/// <summary>
/// Thrown when persistence operations fail.
/// </summary>
public class PersistenceException : ActorException
{
    /// <summary>
    /// Gets the path of the actor whose persistence operation failed.
    /// </summary>
    public string ActorPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceException"/> class for the specified actor path.
    /// </summary>
    /// <param name="actorPath">The path of the actor whose persistence operation failed.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    public PersistenceException(string actorPath, string? message) : base(message ?? $"Persistence operation failed for actor: {actorPath}")
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ActorPath = actorPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceException"/> class for the specified actor path
    /// with an inner exception.
    /// </summary>
    /// <param name="actorPath">The path of the actor whose persistence operation failed.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public PersistenceException(string actorPath, string? message, Exception? innerException)
        : base(message ?? $"Persistence operation failed for actor: {actorPath}", innerException)
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ActorPath = actorPath;
    }
}

/// <summary>
/// Thrown when cluster operations fail.
/// </summary>
public class ClusterException : ActorException
{
    /// <summary>
    /// Gets the address of the cluster node that failed.
    /// </summary>
    public string NodeAddress { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterException"/> class for the specified node address.
    /// </summary>
    /// <param name="nodeAddress">The address of the cluster node that failed.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    public ClusterException(string nodeAddress, string? message) : base(message ?? $"Cluster operation failed for node: {nodeAddress}")
    {
        ArgumentNullException.ThrowIfNull(nodeAddress);
        NodeAddress = nodeAddress;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ClusterException"/> class for the specified node address
    /// with an inner exception.
    /// </summary>
    /// <param name="nodeAddress">The address of the cluster node that failed.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public ClusterException(string nodeAddress, string? message, Exception? innerException)
        : base(message ?? $"Cluster operation failed for node: {nodeAddress}", innerException)
    {
        ArgumentNullException.ThrowIfNull(nodeAddress);
        NodeAddress = nodeAddress;
    }
}

/// <summary>
/// Thrown when message dispatch operations fail.
/// </summary>
public class MessageDispatchException : ActorException
{
    /// <summary>
    /// Gets the path of the actor the message could not be dispatched to.
    /// </summary>
    public string ActorPath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDispatchException"/> class for the specified actor path.
    /// </summary>
    /// <param name="actorPath">The path of the actor the message could not be dispatched to.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    public MessageDispatchException(string actorPath, string? message) : base(message ?? $"Failed to dispatch message to actor: {actorPath}")
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ActorPath = actorPath;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessageDispatchException"/> class for the specified actor path
    /// with an inner exception.
    /// </summary>
    /// <param name="actorPath">The path of the actor the message could not be dispatched to.</param>
    /// <param name="message">The message that describes the error. A default message is used when <c>null</c>.</param>
    /// <param name="innerException">The exception that caused the current exception.</param>
    public MessageDispatchException(string actorPath, string? message, Exception? innerException)
        : base(message ?? $"Failed to dispatch message to actor: {actorPath}", innerException)
    {
        ArgumentNullException.ThrowIfNull(actorPath);
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
