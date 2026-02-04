// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace DotNetActorFramework.Services;

/// <summary>
/// Provides validation helpers for <see cref="MessageDispatcher"/> instances.
/// </summary>
public static class MessageDispatcherValidation
{
    /// <summary>
    /// Validates a <see cref="MessageDispatcher"/> instance for common issues.
    /// </summary>
    /// <param name="value">The message dispatcher to validate.</param>
    /// <returns>A list of human-readable validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static IReadOnlyList<string> Validate(this MessageDispatcher value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate statistics properties
        var stats = value.GetStatistics();

        if (stats.TotalDelivered < 0)
        {
            problems.Add($"TotalDelivered cannot be negative (current: {stats.TotalDelivered:N0}).");
        }

        if (stats.TotalFailed < 0)
        {
            problems.Add($"TotalFailed cannot be negative (current: {stats.TotalFailed:N0}).");
        }

        if (stats.TotalProcessed < 0)
        {
            problems.Add($"TotalProcessed cannot be negative (current: {stats.TotalProcessed:N0}).");
        }

        if (stats.DeadLetterCount < 0)
        {
            problems.Add($"DeadLetterCount cannot be negative (current: {stats.DeadLetterCount:N0}).");
        }

        if (stats.SuccessRate < 0 || stats.SuccessRate > 100)
        {
            problems.Add($"SuccessRate must be between 0 and 100 (current: {stats.SuccessRate:N2}%).");
        }

        // Validate dead letters collection
        var deadLetters = value.GetDeadLetters();
        if (deadLetters.Count > 10000)
        {
            problems.Add($"Dead letter queue contains {deadLetters.Count} items, which exceeds the maximum of 10000.");
        }

        foreach (var envelope in deadLetters)
        {
            if (envelope == null)
            {
                problems.Add("Dead letter queue contains a null envelope.");
                break;
            }

            if (envelope.Recipient == null)
            {
                problems.Add("A dead letter envelope has a null recipient.");
                break;
            }

            if (envelope.Message == null)
            {
                problems.Add("A dead letter envelope has a null message.");
                break;
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="MessageDispatcher"/> is valid.
    /// </summary>
    /// <param name="value">The message dispatcher to check.</param>
    /// <returns>true if the dispatcher is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    public static bool IsValid(this MessageDispatcher value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="MessageDispatcher"/> is valid, throwing an exception if it is not.
    /// </summary>
    /// <param name="value">The message dispatcher to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the dispatcher is invalid, containing a list of validation problems.</exception>
    public static void EnsureValid(this MessageDispatcher value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"MessageDispatcher is invalid. Problems: {string.Join("; ", problems)}",
            nameof(value)
        );
    }
}