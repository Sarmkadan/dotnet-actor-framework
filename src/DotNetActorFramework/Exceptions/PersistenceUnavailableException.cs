// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Exception thrown when event journal persistence is unavailable due to transient failures
/// and the circuit breaker is open.
/// </summary>
public class PersistenceUnavailableException : PersistenceConfigurationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public PersistenceUnavailableException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PersistenceUnavailableException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public PersistenceUnavailableException(string? message, Exception? innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a PersistenceUnavailableException with formatted message.
    /// </summary>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    /// <returns>A new PersistenceUnavailableException instance.</returns>
    public static PersistenceUnavailableException Create(string format, params object?[] args)
    {
        return new PersistenceUnavailableException(string.Format(format, args));
    }

    /// <summary>
    /// Creates a PersistenceUnavailableException with inner exception and formatted message.
    /// </summary>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="format">The format string.</param>
    /// <param name="args">The format arguments.</param>
    /// <returns>A new PersistenceUnavailableException instance.</returns>
    public static PersistenceUnavailableException Create(Exception innerException, string format, params object?[] args)
    {
        return new PersistenceUnavailableException(string.Format(format, args), innerException);
    }
}