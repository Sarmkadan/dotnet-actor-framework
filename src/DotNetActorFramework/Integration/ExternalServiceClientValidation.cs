// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Provides validation helpers for <see cref="ExternalServiceClient"/> instances.
/// </summary>
public static class ExternalServiceClientValidation
{
    /// <summary>
    /// Validates an <see cref="ExternalServiceClient"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The client to validate.</param>
    /// <returns>A read-only list of validation problems; empty if the client is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ExternalServiceClient? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate base URL (derived from constructor parameters)
        // The base URL is validated in the constructor, but we check the field value here
        if (string.IsNullOrWhiteSpace(value.GetBaseUrl()))
        {
            problems.Add("Base URL is null, empty, or whitespace.");
        }

        // Validate max retries
        if (value.GetMaxRetries() < 0)
        {
            problems.Add("Max retries cannot be negative.");
        }

        // Validate retry delay
        var retryDelay = value.GetRetryDelay();
        if (retryDelay <= TimeSpan.Zero)
        {
            problems.Add("Retry delay must be greater than zero.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ExternalServiceClient"/> is valid.
    /// </summary>
    /// <param name="value">The client to check.</param>
    /// <returns><see langword="true"/> if the client is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ExternalServiceClient? value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ExternalServiceClient"/> is valid.
    /// </summary>
    /// <param name="value">The client to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the client is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this ExternalServiceClient? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ExternalServiceClient is invalid:{Environment.NewLine} - {string.Join($"{Environment.NewLine} - ", problems)}");
        }
    }

    // Reflection-based accessors for private fields to avoid breaking encapsulation
    private static string GetBaseUrl(this ExternalServiceClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        var field = typeof(ExternalServiceClient).GetField("_baseUrl", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(client) as string ?? string.Empty;
    }

    private static int GetMaxRetries(this ExternalServiceClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        var field = typeof(ExternalServiceClient).GetField("_maxRetries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(client) as int? ?? 0;
    }

    private static TimeSpan GetRetryDelay(this ExternalServiceClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        var field = typeof(ExternalServiceClient).GetField("_retryDelay", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field?.GetValue(client) as TimeSpan? ?? TimeSpan.Zero;
    }
}
