// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Extension methods for ValidationException and its derived exception types.
/// Provides utility methods for creating and working with validation exceptions.
/// </summary>
public static class ValidationExceptionExtensions
{
    /// <summary>
    /// Creates a ValidationException with a formatted message containing the invalid path.
    /// </summary>
    /// <param name="exception">The InvalidActorPathException instance.</param>
    /// <param name="additionalContext">Optional additional context to include in the message.</param>
    /// <returns>A new InvalidActorPathException with enhanced error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static InvalidActorPathException WithContext(this InvalidActorPathException exception, string? additionalContext = null)
        => additionalContext == null
            ? new InvalidActorPathException(exception.InvalidPath, $"Invalid actor path: {exception.InvalidPath}")
            : new InvalidActorPathException(exception.InvalidPath, $"Invalid actor path: {exception.InvalidPath}. Context: {additionalContext}");

    /// <summary>
    /// Creates a ValidationException with a formatted message containing the invalid message content.
    /// </summary>
    /// <param name="exception">The InvalidMessageException instance.</param>
    /// <param name="expectedFormat">Expected message format for validation.</param>
    /// <returns>A new InvalidMessageException with enhanced error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static InvalidMessageException WithExpectedFormat(this InvalidMessageException exception, string? expectedFormat = null)
        => expectedFormat == null
            ? new InvalidMessageException($"Message validation failed: {exception.Message}")
            : new InvalidMessageException($"Message validation failed: {exception.Message}. Expected format: {expectedFormat}");

    /// <summary>
    /// Creates a ValidationException with a formatted message containing the invalid actor reference.
    /// </summary>
    /// <param name="exception">The InvalidActorReferenceException instance.</param>
    /// <param name="actorType">Expected type of the actor.</param>
    /// <returns>A new InvalidActorReferenceException with enhanced error information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static InvalidActorReferenceException WithActorType(this InvalidActorReferenceException exception, string? actorType = null)
        => actorType == null
            ? new InvalidActorReferenceException($"Actor reference is invalid: {exception.Message}")
            : new InvalidActorReferenceException($"Actor reference is invalid for {actorType}: {exception.Message}");

    /// <summary>
    /// Creates a new ValidationException that combines this exception with additional validation errors.
    /// </summary>
    /// <param name="exception">The ValidationException instance.</param>
    /// <param name="additionalErrors">Additional error messages to combine.</param>
    /// <returns>A new ValidationException with combined error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static ValidationException CombineWith(this ValidationException exception, params string[] additionalErrors)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (additionalErrors == null || additionalErrors.Length == 0)
        {
            return new ValidationException(exception.Message, exception);
        }

        var combinedMessage = $"{exception.Message}{Environment.NewLine}{string.Join(Environment.NewLine, additionalErrors.Select(e => $" - {e}"))}";
        return new ValidationException(combinedMessage, exception);
    }

    /// <summary>
    /// Determines whether this exception represents a specific type of validation failure.
    /// </summary>
    /// <param name="exception">The ValidationException instance.</param>
    /// <param name="validationType">The type of validation to check for.</param>
    /// <returns>True if this exception is of the specified validation type; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> or <paramref name="validationType"/> is null.</exception>
    public static bool IsValidationType(this ValidationException exception, Type validationType)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(validationType);

        return validationType.IsAssignableFrom(exception.GetType());
    }
}