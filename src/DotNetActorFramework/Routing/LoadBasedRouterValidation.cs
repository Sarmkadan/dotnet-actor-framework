// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DotNetActorFramework.Routing;

/// <summary>
/// Provides validation helpers for <see cref="LoadBasedRouter"/> instances.
/// </summary>
public static class LoadBasedRouterValidation
{
    /// <summary>
    /// Validates the specified <see cref="LoadBasedRouter"/> instance.
    /// </summary>
    /// <param name="value">The router instance to validate.</param>
    /// <returns>
    /// An empty list if the router is valid; otherwise, a list of human-readable
    /// problem descriptions.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> Validate([NotNull] this LoadBasedRouter? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // LoadBasedRouter validates all its state in the constructor
        // This method exists for API consistency with other validators
        // No additional runtime validation needed beyond null checks

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="LoadBasedRouter"/> instance is valid.
    /// </summary>
    /// <param name="value">The router instance to validate.</param>
    /// <returns>
    /// <c>true</c> if the router is valid; otherwise, <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    public static bool IsValid([NotNullWhen(true)] this LoadBasedRouter? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="LoadBasedRouter"/> instance is valid.
    /// </summary>
    /// <param name="value">The router instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the router is not valid. The exception message contains a
    /// newline-separated list of all validation problems.
    /// </exception>
    public static void EnsureValid([NotNull] this LoadBasedRouter? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
            return;

        throw new ArgumentException(
            $"LoadBasedRouter is not valid:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}",
            nameof(value));
    }
}