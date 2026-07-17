// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetActorFramework.Api;

/// <summary>
/// Provides validation helpers for <see cref="HealthSummary"/>, <see cref="MessageTypeMetricsInfo"/>,
/// and <see cref="ActorMetricsInfo"/> instances returned by <see cref="SystemMetricsApi"/> methods.
/// Validates all public members for null values, empty strings, out-of-range numbers,
/// and default/invalid dates.
/// </summary>
public static class SystemMetricsApiValidation
{
    /// <summary>
    /// Validates the specified <see cref="HealthSummary"/> instance.
    /// </summary>
    /// <param name="value">The health summary to validate.</param>
    /// <returns>A list of validation errors; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this HealthSummary value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate SystemName
        if (string.IsNullOrWhiteSpace(value.SystemName))
        {
            errors.Add($"SystemName must be a non-empty string, but was '{(value.SystemName == null ? "null" : value.SystemName)}'");
        }

        // Validate SystemId (Guid.Empty is invalid)
        if (value.SystemId == Guid.Empty)
        {
            errors.Add("SystemId must be a non-empty Guid, but was Guid.Empty");
        }

        // Validate TotalActors
        if (value.TotalActors < 0)
        {
            errors.Add($"TotalActors must be non-negative, but was {value.TotalActors}");
        }

        // Validate HealthyActors
        if (value.HealthyActors < 0)
        {
            errors.Add($"HealthyActors must be non-negative, but was {value.HealthyActors}");
        }

        // Validate UnhealthyActors
        if (value.UnhealthyActors < 0)
        {
            errors.Add($"UnhealthyActors must be non-negative, but was {value.UnhealthyActors}");
        }

        // Validate ErrorActors
        if (value.ErrorActors < 0)
        {
            errors.Add($"ErrorActors must be non-negative, but was {value.ErrorActors}");
        }

        // Validate TotalMessages
        if (value.TotalMessages < 0)
        {
            errors.Add($"TotalMessages must be non-negative, but was {value.TotalMessages}");
        }

        // Validate TotalErrors
        if (value.TotalErrors < 0)
        {
            errors.Add($"TotalErrors must be non-negative, but was {value.TotalErrors}");
        }

        // Validate ErrorRate (should be between 0 and 1 inclusive)
        if (value.ErrorRate < 0 || value.ErrorRate > 1)
        {
            errors.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"ErrorRate must be between 0 and 1 inclusive, but was {value.ErrorRate:F6}"));
        }

        // Validate HealthPercentage (should be between 0 and 100 inclusive)
        if (value.HealthPercentage < 0 || value.HealthPercentage > 100)
        {
            errors.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"HealthPercentage must be between 0 and 100 inclusive, but was {value.HealthPercentage:F6}"));
        }

        // Validate AverageLatencyMs (should be non-negative)
        if (value.AverageLatencyMs < 0)
        {
            errors.Add($"AverageLatencyMs must be non-negative, but was {value.AverageLatencyMs}");
        }

        // Validate Timestamp (should not be default DateTime)
        if (value.Timestamp == default)
        {
            errors.Add("Timestamp must be set to a valid DateTime, but was default");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates the specified <see cref="MessageTypeMetricsInfo"/> instance.
    /// </summary>
    /// <param name="value">The message type metrics info to validate.</param>
    /// <returns>A list of validation errors; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this MessageTypeMetricsInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate MessageType
        if (string.IsNullOrWhiteSpace(value.MessageType))
        {
            errors.Add($"MessageType must be a non-empty string, but was '{(value.MessageType == null ? "null" : value.MessageType)}'");
        }

        // Validate ProcessedCount
        if (value.ProcessedCount < 0)
        {
            errors.Add($"ProcessedCount must be non-negative, but was {value.ProcessedCount}");
        }

        // Validate ErrorCount
        if (value.ErrorCount < 0)
        {
            errors.Add($"ErrorCount must be non-negative, but was {value.ErrorCount}");
        }

        // Validate AverageLatencyMs (should be non-negative)
        if (value.AverageLatencyMs < 0)
        {
            errors.Add($"AverageLatencyMs must be non-negative, but was {value.AverageLatencyMs}");
        }

        // Validate ErrorRate (should be between 0 and 1 inclusive)
        if (value.ErrorRate < 0 || value.ErrorRate > 1)
        {
            errors.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"ErrorRate must be between 0 and 1 inclusive, but was {value.ErrorRate:F6}"));
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates the specified <see cref="ActorMetricsInfo"/> instance.
    /// </summary>
    /// <param name="value">The actor metrics info to validate.</param>
    /// <returns>A list of validation errors; empty if the instance is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ActorMetricsInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate ActorPath
        if (string.IsNullOrWhiteSpace(value.ActorPath))
        {
            errors.Add($"ActorPath must be a non-empty string, but was '{(value.ActorPath == null ? "null" : value.ActorPath)}'");
        }

        // Validate ProcessedCount
        if (value.ProcessedCount < 0)
        {
            errors.Add($"ProcessedCount must be non-negative, but was {value.ProcessedCount}");
        }

        // Validate ErrorCount
        if (value.ErrorCount < 0)
        {
            errors.Add($"ErrorCount must be non-negative, but was {value.ErrorCount}");
        }

        // Validate AverageLatencyMs (should be non-negative)
        if (value.AverageLatencyMs < 0)
        {
            errors.Add($"AverageLatencyMs must be non-negative, but was {value.AverageLatencyMs}");
        }

        // Validate ErrorRate (should be between 0 and 1 inclusive)
        if (value.ErrorRate < 0 || value.ErrorRate > 1)
        {
            errors.Add(string.Create(
                CultureInfo.InvariantCulture,
                $"ErrorRate must be between 0 and 1 inclusive, but was {value.ErrorRate:F6}"));
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="HealthSummary"/> instance is valid.
    /// </summary>
    /// <param name="value">The health summary to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this HealthSummary value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Determines whether the specified <see cref="MessageTypeMetricsInfo"/> instance is valid.
    /// </summary>
    /// <param name="value">The message type metrics info to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this MessageTypeMetricsInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Determines whether the specified <see cref="ActorMetricsInfo"/> instance is valid.
    /// </summary>
    /// <param name="value">The actor metrics info to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ActorMetricsInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="HealthSummary"/> instance is valid,
    /// throwing an <see cref="ArgumentException"/> with detailed validation errors if it is not.
    /// </summary>
    /// <param name="value">The health summary to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid.</exception>
    public static void EnsureValid(this HealthSummary value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"HealthSummary instance is invalid:{Environment.NewLine}- {
                string.Join(
                    $"\n- ",
                    errors
                )
            }");
    }

    /// <summary>
    /// Ensures that the specified <see cref="MessageTypeMetricsInfo"/> instance is valid,
    /// throwing an <see cref="ArgumentException"/> with detailed validation errors if it is not.
    /// </summary>
    /// <param name="value">The message type metrics info to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid.</exception>
    public static void EnsureValid(this MessageTypeMetricsInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"MessageTypeMetricsInfo instance is invalid:{Environment.NewLine}- {
                string.Join(
                    $"\n- ",
                    errors
                )
            }");
    }

    /// <summary>
    /// Ensures that the specified <see cref="ActorMetricsInfo"/> instance is valid,
    /// throwing an <see cref="ArgumentException"/> with detailed validation errors if it is not.
    /// </summary>
    /// <param name="value">The actor metrics info to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value"/> is invalid.</exception>
    public static void EnsureValid(this ActorMetricsInfo value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"ActorMetricsInfo instance is invalid:{Environment.NewLine}- {
                string.Join(
                    $"\n- ",
                    errors
                )
            }");
    }
}