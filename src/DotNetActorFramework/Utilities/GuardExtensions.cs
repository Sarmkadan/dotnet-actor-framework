// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Guard clause extensions for common validation patterns.
/// These reduce boilerplate validation code throughout the framework.
/// </summary>
public static class GuardExtensions
{
    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if the value is null.
    /// </summary>
    /// <typeparam name="T">The type of reference.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    public static T NotNull<T>(this T? value, string paramName) where T : class
        => value ?? throw new ArgumentNullException(paramName);

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the string is null or empty.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    public static string NotNullOrEmpty(this string? value, string paramName)
        => string.IsNullOrEmpty(value)
            ? throw new ArgumentException($"{paramName} cannot be null or empty.", paramName)
            : value;

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the string is null, empty, or whitespace.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or whitespace.</exception>
    public static string NotNullOrWhiteSpace(this string? value, string paramName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException($"{paramName} cannot be null or empty.", paramName)
            : value;

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the Guid is empty.
    /// </summary>
    /// <param name="value">The Guid to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated Guid.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty.</exception>
    public static Guid NotEmpty(this Guid value, string paramName)
        => value == Guid.Empty
            ? throw new ArgumentException($"{paramName} cannot be empty.", paramName)
            : value;

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the integer is not positive.
    /// </summary>
    /// <param name="value">The integer to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated integer.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not positive.</exception>
    public static int MustBePositive(this int value, string paramName)
        => value <= 0
            ? throw new ArgumentException($"{paramName} must be positive.", paramName)
            : value;

    /// <summary>
    /// Throws <see cref="ArgumentException"/> if the integer is negative.
    /// </summary>
    /// <param name="value">The integer to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated integer.</returns>
    /// <exception cref="ArgumentException"><paramref name="value"/> is negative.</exception>
    public static int MustBeNonNegative(this int value, string paramName)
        => value < 0
            ? throw new ArgumentException($"{paramName} cannot be negative.", paramName)
            : value;

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if the collection is null.
    /// Throws <see cref="ArgumentException"/> if the collection is empty.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    /// <param name="collection">The collection to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <returns>The validated collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> is empty.</exception>
    public static IEnumerable<T> NotEmpty<T>(this IEnumerable<T>? collection, string paramName)
        => collection is null || !collection.Any()
            ? throw new ArgumentException($"{paramName} cannot be empty.", paramName)
            : collection;

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="message"/> is null.
    /// Throws <see cref="ArgumentException"/> if the condition is false.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The exception message if validation fails.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="condition"/> is false.</exception>
    public static void MustBeTrue(this bool condition, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (!condition)
            throw new ArgumentException(message);
    }

    /// <summary>
    /// Throws <see cref="ArgumentNullException"/> if <paramref name="message"/> is null.
    /// Throws <see cref="ArgumentException"/> if the condition is true.
    /// </summary>
    /// <param name="condition">The condition to validate.</param>
    /// <param name="message">The exception message if validation fails.</param>
    /// <exception cref="ArgumentNullException"><paramref name="message"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="condition"/> is true.</exception>
    public static void MustBeFalse(this bool condition, string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        if (condition)
            throw new ArgumentException(message);
    }
}