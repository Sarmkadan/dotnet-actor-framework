// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Base exception for validation-related errors in the actor framework.
/// </summary>
public class ValidationException : DotnetActorFrameworkException
{
    public ValidationException(string? message) : base(message)
    {
    }

    public ValidationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a validation exception with formatted message.
    /// </summary>
    public new static ValidationException Create(string format, params object?[] args)
    {
        return new ValidationException(string.Format(format, args));
    }
}

/// <summary>
/// Thrown when actor path validation fails.
/// </summary>
public class InvalidActorPathException : ValidationException
{
    public string InvalidPath { get; }

    public InvalidActorPathException(string path) : base($"Invalid actor path: {path}")
    {
        InvalidPath = path;
    }

    public InvalidActorPathException(string path, string? message) : base(message ?? $"Invalid actor path: {path}")
    {
        InvalidPath = path;
    }
}

/// <summary>
/// Thrown when message validation fails.
/// </summary>
public class InvalidMessageException : ValidationException
{
    public InvalidMessageException(string? message) : base(message ?? "Message validation failed")
    {
    }

    public InvalidMessageException(string? message, Exception? innerException) : base(message ?? "Message validation failed", innerException)
    {
    }
}

/// <summary>
/// Thrown when actor reference validation fails.
/// </summary>
public class InvalidActorReferenceException : ValidationException
{
    public InvalidActorReferenceException(string? message) : base(message ?? "Actor reference is invalid")
    {
    }

    public InvalidActorReferenceException(string? message, Exception? innerException) : base(message ?? "Actor reference is invalid", innerException)
    {
    }
}