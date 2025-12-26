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
    /// <param name="exception">The InvalidActorPathException instance</param>
    /// <param name="additionalContext">Optional additional context to include in the message</param>
    /// <returns>A new ValidationException with enhanced error information</returns>
    public static InvalidActorPathException WithContext(this InvalidActorPathException exception, string? additionalContext = null)
    {
        var message = additionalContext == null
            ? $"Invalid actor path: {exception.InvalidPath}"
            : $"Invalid actor path: {exception.InvalidPath}. Context: {additionalContext}";

        return new InvalidActorPathException(exception.InvalidPath, message);
    }

    /// <summary>
    /// Creates a ValidationException with a formatted message containing the invalid message content.
    /// </summary>
    /// <param name="exception">The InvalidMessageException instance</param>
    /// <param name="expectedFormat">Expected message format for validation</param>
    /// <returns>A new ValidationException with enhanced error information</returns>
    public static InvalidMessageException WithExpectedFormat(this InvalidMessageException exception, string? expectedFormat = null)
    {
        var message = expectedFormat == null
            ? $"Message validation failed: {exception.Message}"
            : $"Message validation failed: {exception.Message}. Expected format: {expectedFormat}";

        return new InvalidMessageException(message);
    }

    /// <summary>
    /// Creates a ValidationException with a formatted message containing the invalid actor reference.
    /// </summary>
    /// <param name="exception">The InvalidActorReferenceException instance</param>
    /// <param name="actorType">Expected type of the actor</param>
    /// <returns>A new ValidationException with enhanced error information</returns>
    public static InvalidActorReferenceException WithActorType(this InvalidActorReferenceException exception, string? actorType = null)
    {
        var message = actorType == null
            ? $"Actor reference is invalid: {exception.Message}"
            : $"Actor reference is invalid for {actorType}: {exception.Message}";

        return new InvalidActorReferenceException(message);
    }

    /// <summary>
    /// Creates a new ValidationException that combines this exception with additional validation errors.
    /// </summary>
    /// <param name="exception">The ValidationException instance</param>
    /// <param name="additionalErrors">Additional error messages to combine</param>
    /// <returns>A new ValidationException with combined error messages</returns>
    public static ValidationException CombineWith(this ValidationException exception, params string[] additionalErrors)
    {
        if (additionalErrors == null || additionalErrors.Length == 0)
        {
            return new ValidationException(exception.Message, exception);
        }

        var combinedMessage = $"{exception.Message}{Environment.NewLine}{string.Join(Environment.NewLine, additionalErrors.Select(e => $"  - {e}"))}";
        return new ValidationException(combinedMessage, exception);
    }

    /// <summary>
    /// Determines whether this exception represents a specific type of validation failure.
    /// </summary>
    /// <param name="exception">The ValidationException instance</param>
    /// <param name="validationType">The type of validation to check for</param>
    /// <returns>True if this exception is of the specified validation type</returns>
    public static bool IsValidationType(this ValidationException exception, Type validationType)
    {
        if (validationType == null)
        {
            throw new ArgumentNullException(nameof(validationType));
        }

        return validationType.IsAssignableFrom(exception.GetType());
    }
}