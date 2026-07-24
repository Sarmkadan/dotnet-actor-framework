// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Messaging;

/// <summary>
/// Thrown when a request/response operation started via <see cref="AskExtensions"/>
/// does not receive a <see cref="ResponseMessage"/> or <see cref="FailureMessage"/>
/// within the configured timeout.
/// </summary>
public class AskTimeoutException : ActorException
{
    /// <summary>The path of the actor that was asked and failed to reply in time.</summary>
    public string ActorPath { get; }

    /// <summary>The CLR type of the request message that went unanswered.</summary>
    public Type RequestType { get; }

    /// <summary>The timeout that was allowed for the reply.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    /// Initializes a new <see cref="AskTimeoutException"/> naming the actor path,
    /// request message type, and configured timeout.
    /// </summary>
    /// <param name="actorPath">The path of the actor that did not reply in time.</param>
    /// <param name="requestType">The CLR type of the unanswered request message.</param>
    /// <param name="timeout">The timeout that was allowed for the reply.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorPath"/> or <paramref name="requestType"/> is <c>null</c>.</exception>
    public AskTimeoutException(string actorPath, Type requestType, TimeSpan timeout)
        : base($"Actor '{actorPath}' did not respond to '{requestType?.Name}' within {timeout.TotalSeconds:0.###}s.")
    {
        ArgumentNullException.ThrowIfNull(actorPath);
        ArgumentNullException.ThrowIfNull(requestType);

        ActorPath = actorPath;
        RequestType = requestType;
        Timeout = timeout;
    }
}

/// <summary>
/// Thrown when a request/response operation started via <see cref="AskExtensions"/>
/// receives a <see cref="FailureMessage"/> instead of a successful response.
/// </summary>
public class AskFailedException : ActorException
{
    /// <summary>The full type name of the originating exception, or <c>null</c> when none was supplied.</summary>
    public string? ExceptionType { get; }

    /// <summary>
    /// Initializes a new <see cref="AskFailedException"/> from the reason and originating
    /// exception detail carried by a <see cref="FailureMessage"/>.
    /// </summary>
    /// <param name="reason">Human-readable description of the failure.</param>
    /// <param name="exceptionType">The full type name of the originating exception, or <c>null</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is null, empty, or whitespace.</exception>
    public AskFailedException(string reason, string? exceptionType)
        : base(reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Reason cannot be null or empty.", nameof(reason));

        ExceptionType = exceptionType;
    }
}
