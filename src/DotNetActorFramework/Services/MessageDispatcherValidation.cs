// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System.Diagnostics.CodeAnalysis;

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
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static IReadOnlyList<string> Validate([NotNull] this MessageDispatcher value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var problems = new List<string>();

		// Validate statistics properties
		var stats = value.GetStatistics() ?? throw new InvalidOperationException("GetStatistics() returned null.");

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

		if (stats.SuccessRate is < 0 or > 100)
		{
			problems.Add($"SuccessRate must be between 0 and 100 (current: {stats.SuccessRate:N2}%).");
		}

		// Validate dead letters collection
		var deadLetters = value.GetDeadLetters();
		ArgumentNullException.ThrowIfNull(deadLetters);

		if (deadLetters.Count > 10000)
		{
			problems.Add($"Dead letter queue contains {deadLetters.Count} items, which exceeds the maximum of 10000.");
		}

		foreach (var envelope in deadLetters)
		{
			if (envelope is null)
			{
				problems.Add("Dead letter queue contains a null envelope.");
				break;
			}

			if (envelope.Recipient is null)
			{
				problems.Add("A dead letter envelope has a null recipient.");
				break;
			}

			if (envelope.Message is null)
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
	/// <param name="value">The message dispatcher to validate.</param>
	/// <returns>true if the dispatcher is valid; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static bool IsValid([NotNullWhen(true)] this MessageDispatcher? value) => value?.Validate().Count == 0;

	/// <summary>
	/// Ensures that the specified <see cref="MessageDispatcher"/> is valid, throwing an exception if it is not.
	/// </summary>
	/// <param name="value">The message dispatcher to validate.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	/// <exception cref="ArgumentException">Thrown when the dispatcher is invalid, containing a list of validation problems.</exception>
	public static void EnsureValid([NotNull] this MessageDispatcher value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var problems = value.Validate();

		if (problems.Count is not 0)
		{
			throw new ArgumentException(
				$"MessageDispatcher is invalid. Problems: {string.Join("; ", problems)}",
				nameof(value)
			);
		}
	}
}