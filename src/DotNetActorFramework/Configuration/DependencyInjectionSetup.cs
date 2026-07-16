// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetActorFramework.Services;
using DotNetActorFramework.Repository;
using DotNetActorFramework.Persistence;
using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence.InMemory; // For InMemory implementations
using DotNetActorFramework.Enums; // For PersistenceBackend enum

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

        // Register persistence services based on configuration
        RegisterPersistenceServices(services, options);

        // Register repositories
        services.AddSingleton<ConnectionManager>();
        services.AddSingleton<MessagePersistenceRepository>();
        services.AddSingleton<ActorMetricsRepository>();

        // Register PersistenceService-dependent repositories
        services.AddSingleton<ActorStateRepository>();

        // Register services
        services.AddSingleton<ActorRegistry>();
        services.AddSingleton<MailboxService>(sp => new MailboxService(sp.GetRequiredService<ActorSystemOptions>()));
        services.AddSingleton<MessageDispatcher>();
        services.AddSingleton<SupervisionService>();

        // The init coordinator itself - consumers should be able to just
        // GetRequiredService it instead of ActivatorUtilities gymnastics.
        services.AddSingleton<ActorSystemConfiguration>();

        if (options.EnableClusterMode)
        {
            services.AddSingleton<ClusterActorRegistry>();
        }

        return services;
    }

    private static void RegisterPersistenceServices(IServiceCollection services, ActorSystemOptions options)
    {
        switch (options.DefaultPersistenceBackend)
        {
            case PersistenceBackend.InMemory:
                services.AddSingleton<ISnapshotStore, InMemorySnapshotStore>();
                services.AddSingleton<IEventJournal, InMemoryEventJournal>();
                break;
            case PersistenceBackend.File:
                // TODO: Implement FileSnapshotStore and FileEventJournal
                throw new NotImplementedException("File persistence backend is not yet implemented.");
            case PersistenceBackend.LiteDb:
                // TODO: Implement LiteDbSnapshotStore and LiteDbEventJournal
                throw new NotImplementedException("LiteDB persistence backend is not yet implemented.");
            case PersistenceBackend.PostgreSql:
                // TODO: Implement PostgreSqlSnapshotStore and PostgreSqlEventJournal
                throw new NotImplementedException("PostgreSQL persistence backend is not yet implemented.");
            default:
                throw new ArgumentOutOfRangeException(nameof(options.DefaultPersistenceBackend), "Unknown persistence backend.");
        }

        // Register the PersistenceService facade
        services.AddSingleton<PersistenceService>();
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
