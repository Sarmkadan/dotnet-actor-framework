// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Guard clause extensions for common validation patterns.
/// These reduce boilerplate validation code throughout the framework.
/// </summary>
public static class GuardExtensions
{
    /// <summary>
    /// Throws ArgumentNullException if the value is null.
    /// </summary>
    public static T NotNull<T>(this T? value, string paramName) where T : class
    {
        if (value == null)
            throw new ArgumentNullException(paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the string is null or empty.
    /// </summary>
    public static string NotNullOrEmpty(this string? value, string paramName)
    {
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the string is null, empty, or whitespace.
    /// </summary>
    public static string NotNullOrWhiteSpace(this string? value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} cannot be null or empty.", paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the Guid is empty.
    /// </summary>
    public static Guid NotEmpty(this Guid value, string paramName)
    {
        if (value == Guid.Empty)
            throw new ArgumentException($"{paramName} cannot be empty.", paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the integer is not positive.
    /// </summary>
    public static int MustBePositive(this int value, string paramName)
    {
        if (value <= 0)
            throw new ArgumentException($"{paramName} must be positive.", paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the integer is negative.
    /// </summary>
    public static int MustBeNonNegative(this int value, string paramName)
    {
        if (value < 0)
            throw new ArgumentException($"{paramName} cannot be negative.", paramName);
        return value;
    }

    /// <summary>
    /// Throws ArgumentException if the collection is empty or null.
    /// </summary>
    public static IEnumerable<T> NotEmpty<T>(this IEnumerable<T>? collection, string paramName)
    {
        if (collection == null || !collection.Any())
            throw new ArgumentException($"{paramName} cannot be empty.", paramName);
        return collection;
    }

    /// <summary>
    /// Throws ArgumentException if the condition is false.
    /// </summary>
    public static void MustBeTrue(this bool condition, string message)
    {
        if (!condition)
            throw new ArgumentException(message);
    }

    /// <summary>
    /// Throws ArgumentException if the condition is true.
    /// </summary>
    public static void MustBeFalse(this bool condition, string message)
    {
        if (condition)
            throw new ArgumentException(message);
    }
}
