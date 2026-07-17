// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics.CodeAnalysis;

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Provides validation helpers for <see cref="ValidationException"/> and its derived types.
/// </summary>
public static class ValidationExceptionValidation
{
    /// <summary>
    /// Validates a <see cref="ValidationException"/> and returns a list of human-readable validation problems.
    /// </summary>
    /// <param name="value">The validation exception to validate.</param>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ArgumentNullException.ThrowIfNull provides validation")]
    public static IReadOnlyList<string> Validate(this ValidationException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        switch (value)
        {
            case InvalidActorPathException invalidPathException:
                ValidateInvalidActorPathException(invalidPathException, problems);
                break;

            case InvalidMessageException invalidMessageException:
                ValidateInvalidMessageException(invalidMessageException, problems);
                break;

            case InvalidActorReferenceException invalidActorReferenceException:
                ValidateInvalidActorReferenceException(invalidActorReferenceException, problems);
                break;
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ValidationException"/> is valid.
    /// </summary>
    /// <param name="value">The validation exception to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ArgumentNullException.ThrowIfNull provides validation")]
    public static bool IsValid(this ValidationException value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ValidationException"/> is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The validation exception to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is not valid, containing the validation problems.</exception>
    [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "ArgumentNullException.ThrowIfNull provides validation")]
    public static void EnsureValid(this ValidationException value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ValidationException is not valid. Problems:{Environment.NewLine}" +
                string.Join(Environment.NewLine, problems),
                nameof(value));
        }
    }

    private static void ValidateInvalidActorPathException(InvalidActorPathException exception, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(problems);

        if (string.IsNullOrWhiteSpace(exception.InvalidPath))
        {
            problems.Add("InvalidActorPathException.InvalidPath cannot be null, empty, or whitespace.");
        }
        else if (exception.InvalidPath.Length > 1024)
        {
            problems.Add("InvalidActorPathException.InvalidPath exceeds maximum length of 1024 characters.");
        }

        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            problems.Add("InvalidActorPathException.Message cannot be null, empty, or whitespace.");
        }
    }

    private static void ValidateInvalidMessageException(InvalidMessageException exception, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(problems);

        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            problems.Add("InvalidMessageException.Message cannot be null, empty, or whitespace.");
        }
        else if (exception.Message.Length > 10485760) // 10MB
        {
            problems.Add("InvalidMessageException.Message exceeds maximum length of 10485760 characters (10MB).");
        }
    }

    private static void ValidateInvalidActorReferenceException(InvalidActorReferenceException exception, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(problems);

        if (string.IsNullOrWhiteSpace(exception.Message))
        {
            problems.Add("InvalidActorReferenceException.Message cannot be null, empty, or whitespace.");
        }
    }
}