// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Provides validation helpers for <see cref="ErrorHandlingMiddleware"/> instances.
/// </summary>
public static class ErrorHandlingMiddlewareValidation
{
    /// <summary>
    /// Validates an <see cref="ErrorHandlingMiddleware"/> instance.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <returns>A list of validation errors; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ErrorHandlingMiddleware? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Name
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            errors.Add("Name cannot be null or whitespace.");
        }

        // Validate Order
        if (value.Order < 0)
        {
            errors.Add("Order must be a non-negative integer.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ErrorHandlingMiddleware"/> is valid.
    /// </summary>
    /// <param name="value">The middleware instance to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ErrorHandlingMiddleware? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ErrorHandlingMiddleware"/> is valid.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing the validation errors.</exception>
    public static void EnsureValid(this ErrorHandlingMiddleware? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"ErrorHandlingMiddleware is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}