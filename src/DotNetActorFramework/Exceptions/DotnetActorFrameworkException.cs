// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Base exception for all errors in the DotNetActorFramework.
/// All framework-specific exceptions should inherit from this class.
/// </summary>
public class DotnetActorFrameworkException : Exception
{
    public DotnetActorFrameworkException()
    {
    }

    public DotnetActorFrameworkException(string? message) : base(message)
    {
    }

    public DotnetActorFrameworkException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a DotnetActorFrameworkException with formatted message.
    /// </summary>
    public static DotnetActorFrameworkException Create(string format, params object?[] args)
    {
        return new DotnetActorFrameworkException(string.Format(format, args));
    }

    /// <summary>
    /// Creates a DotnetActorFrameworkException with inner exception.
    /// </summary>
    public static DotnetActorFrameworkException Create(Exception innerException, string format, params object?[] args)
    {
        return new DotnetActorFrameworkException(string.Format(format, args), innerException);
    }
}