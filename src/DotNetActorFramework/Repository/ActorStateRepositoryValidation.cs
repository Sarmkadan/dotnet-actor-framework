using System;
using System.Collections.Generic;
using System.Globalization;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Repository;

/// <summary>
/// Provides validation helpers for <see cref="ActorStateRepository"/> instances.
/// </summary>
public static class ActorStateRepositoryValidation
{
    /// <summary>
    /// Validates the specified actor state repository.
    /// </summary>
    /// <param name="value">The actor state repository to validate.</param>
    /// <returns>A list of validation problems; empty if the repository is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ActorStateRepository value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate ActorId
        if (value.ActorId == Guid.Empty)
        {
            problems.Add("ActorId cannot be empty (Guid.Empty).");
        }

        // Validate ActorPath
        if (value.ActorPath is null)
        {
            problems.Add("ActorPath cannot be null.");
        }
        else
        {
            try
            {
                // Test if ActorPath is valid by attempting to parse it
                ActorPath.Parse(value.ActorPath.Path);
            }
            catch (Exception ex)
            {
                problems.Add($"ActorPath is invalid: {ex.Message}");
            }
        }

        // Validate State (if not null)
        if (value.State is Dictionary<string, object> stateDict)
        {
            if (stateDict.Count == 0)
            {
                problems.Add("State dictionary cannot be empty.");
            }

            foreach (var kvp in stateDict)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                {
                    problems.Add("State dictionary contains an entry with null or empty key.");
                    break;
                }
            }
        }

        // Validate SavedAt
        if (value.SavedAt == default)
        {
            problems.Add("SavedAt cannot be default (DateTime.MinValue).");
        }
        else if (value.SavedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("SavedAt cannot be in the future.");
        }

        // Validate SequenceNr
        if (value.SequenceNr < 0)
        {
            problems.Add("SequenceNr cannot be negative.");
        }

        // Validate Version
        if (value.Version < 0)
        {
            problems.Add("Version cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified actor state repository is valid.
    /// </summary>
    /// <param name="value">The actor state repository to check.</param>
    /// <returns><see langword="true"/> if the repository is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ActorStateRepository value) => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that the specified actor state repository is valid.
    /// </summary>
    /// <param name="value">The actor state repository to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the repository has validation problems.</exception>
    public static void EnsureValid(this ActorStateRepository value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ActorStateRepository is invalid. Problems: {string.Join("; ", problems)}",
                nameof(value)
            );
        }
    }
}