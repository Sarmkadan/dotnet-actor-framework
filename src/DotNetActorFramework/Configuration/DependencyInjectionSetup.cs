// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetActorFramework.Services;
using DotNetActorFramework.Repository;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// Extension methods for configuring dependency injection.
/// </summary>
public static class DependencyInjectionSetup
{
    /// <summary>
    /// Adds actor framework services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddActorFramework(
        this IServiceCollection services,
        Action<ActorSystemOptions>? configureOptions = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Configure options
        var options = ActorSystemOptions.CreateDefault();
        configureOptions?.Invoke(options);
        options.Validate();

        services.AddSingleton(options);

        // Register repositories
        services.AddSingleton<ConnectionManager>();
        services.AddSingleton<ActorStateRepository>();
        services.AddSingleton<MessagePersistenceRepository>();
        services.AddSingleton<ActorMetricsRepository>();

        // Register services
        services.AddSingleton<ActorRegistry>();
        services.AddSingleton<MailboxService>(sp => new MailboxService(sp.GetRequiredService<ActorSystemOptions>()));
        services.AddSingleton<MessageDispatcher>();
        services.AddSingleton<SupervisionService>();

        if (options.EnableClusterMode)
        {
            services.AddSingleton<ClusterActorRegistry>();
        }

        return services;
    }

    /// <summary>
    /// Adds actor framework with high-performance configuration.
    /// </summary>
    public static IServiceCollection AddActorFrameworkHighPerformance(
        this IServiceCollection services)
    {
        return services.AddActorFramework(options =>
        {
            options.DefaultMailboxCapacity = 5000;
            options.EnableMessagePersistence = false;
            options.EnableMetricsCollection = false;
            options.SnapshotIntervalSeconds = 600;
        });
    }

    /// <summary>
    /// Adds actor framework with reliable configuration.
    /// </summary>
    public static IServiceCollection AddActorFrameworkReliable(
        this IServiceCollection services,
        string? connectionString = null)
    {
        return services.AddActorFramework(options =>
        {
            options.DefaultMailboxCapacity = 500;
            options.EnableMessagePersistence = true;
            options.EnableActorStateSnapshotting = true;
            options.SnapshotIntervalSeconds = 60;
            options.MaxMessageRetries = 5;
            options.DatabaseConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Adds actor framework with cluster configuration.
    /// </summary>
    public static IServiceCollection AddActorFrameworkCluster(
        this IServiceCollection services,
        string clusterAddress = "127.0.0.1:8080",
        string? connectionString = null)
    {
        return services.AddActorFramework(options =>
        {
            options.EnableClusterMode = true;
            options.ClusterAddress = clusterAddress;
            options.EnableMessagePersistence = true;
            options.EnableMetricsCollection = true;
            options.DatabaseConnectionString = connectionString;
        });
    }

    /// <summary>
    /// Configures the actor framework with custom options.
    /// </summary>
    public static IServiceCollection ConfigureActorFramework(
        this IServiceCollection services,
        Action<ActorSystemOptions> configureOptions)
    {
        if (configureOptions == null)
            throw new ArgumentNullException(nameof(configureOptions));

        services.Configure<ActorSystemOptions>(configureOptions);
        return services;
    }
}
