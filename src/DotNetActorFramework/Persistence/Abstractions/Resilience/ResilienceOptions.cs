// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotNetActorFramework.Persistence.Abstractions.Resilience;

/// <summary>
/// Configuration options for resilience patterns (retry and circuit breaker).
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Default resilience options with sensible defaults for development/testing.
    /// </summary>
    public static ResilienceOptions Default { get; } = new ResilienceOptions
    {
        MaxRetryAttempts = 3,
        BaseDelayMilliseconds = 100,
        MaxDelayMilliseconds = 5000,
        BackoffExponent = 2.0,
        CircuitBreakerFailureThreshold = 5,
        CircuitBreakerCooldownPeriod = TimeSpan.FromSeconds(30)
    };

    /// <summary>
    /// Default resilience options optimized for production environments.
    /// </summary>
    public static ResilienceOptions Production { get; } = new ResilienceOptions
    {
        MaxRetryAttempts = 5,
        BaseDelayMilliseconds = 200,
        MaxDelayMilliseconds = 10000,
        BackoffExponent = 2.5,
        CircuitBreakerFailureThreshold = 10,
        CircuitBreakerCooldownPeriod = TimeSpan.FromMinutes(1)
    };

    /// <summary>
    /// Gets or sets the maximum number of retry attempts before considering the operation failed.
    /// Default: 3
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the base delay in milliseconds for the first retry attempt.
    /// Default: 100ms
    /// </summary>
    public int BaseDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Gets or sets the maximum delay in milliseconds for any retry attempt (cap on exponential backoff).
    /// Default: 5000ms (5 seconds)
    /// </summary>
    public int MaxDelayMilliseconds { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the exponent for exponential backoff calculation.
    /// Default: 2.0
    /// </summary>
    public double BackoffExponent { get; set; } = 2.0;

    /// <summary>
    /// Gets or sets the number of consecutive failures required to open the circuit breaker.
    /// Default: 5
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the cooldown period for the circuit breaker once it's opened.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan CircuitBreakerCooldownPeriod { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Creates a copy of these options.
    /// </summary>
    /// <returns>A new ResilienceOptions instance with the same values.</returns>
    public ResilienceOptions Clone()
    {
        return new ResilienceOptions
        {
            MaxRetryAttempts = MaxRetryAttempts,
            BaseDelayMilliseconds = BaseDelayMilliseconds,
            MaxDelayMilliseconds = MaxDelayMilliseconds,
            BackoffExponent = BackoffExponent,
            CircuitBreakerFailureThreshold = CircuitBreakerFailureThreshold,
            CircuitBreakerCooldownPeriod = CircuitBreakerCooldownPeriod
        };
    }
}
