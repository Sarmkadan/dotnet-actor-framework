// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Base exception for all actor-related errors.
/// </summary>
public class ActorException : Exception
{
    public ActorException(string? message) : base(message)
    {
    }

    public ActorException(string? message, Exception? innerException)
        : base(message, innerException)
    {
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
}

/// <summary>
/// Thrown when a mailbox operation fails.
/// </summary>
public class MailboxException : ActorException
{
    public MailboxException(string? message) : base(message)
    {
    }

    public MailboxException(string? message, Exception? innerException)
        : base(message, innerException)
    {
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
