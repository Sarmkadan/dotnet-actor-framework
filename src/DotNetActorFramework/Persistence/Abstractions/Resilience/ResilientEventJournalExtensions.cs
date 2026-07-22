// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence.Abstractions.Resilience;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace DotNetActorFramework.Persistence;

/// <summary>
/// Extension methods for registering resilient event journal decorators with dependency injection.
/// </summary>
public static class ResilientEventJournalExtensions
{
    /// <summary>
    /// Adds a resilient event journal decorator to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for resilience options.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddResilientEventJournal(
        this IServiceCollection services,
        Action<ResilienceOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Configure options
        var options = ResilienceOptions.Default.Clone();
        configureOptions?.Invoke(options);

        // Register the decorator
        services.TryAddTransient<IEventJournal>(provider =>
        {
            var innerJournal = provider.GetRequiredService<IEventJournal>();
            var logger = provider.GetService<ILogger<ResilientEventJournal>>();
            return new ResilientEventJournal(innerJournal, options, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds a resilient event journal decorator with production-optimized settings.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddResilientEventJournalProduction(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddResilientEventJournal(options =>
        {
            options.MaxRetryAttempts = ResilienceOptions.Production.MaxRetryAttempts;
            options.BaseDelayMilliseconds = ResilienceOptions.Production.BaseDelayMilliseconds;
            options.MaxDelayMilliseconds = ResilienceOptions.Production.MaxDelayMilliseconds;
            options.BackoffExponent = ResilienceOptions.Production.BackoffExponent;
            options.CircuitBreakerFailureThreshold = ResilienceOptions.Production.CircuitBreakerFailureThreshold;
            options.CircuitBreakerCooldownPeriod = ResilienceOptions.Production.CircuitBreakerCooldownPeriod;
        });
    }
}
