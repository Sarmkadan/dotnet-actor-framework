// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Middleware;
using DotNetActorFramework.BackgroundWorkers;
using DotNetActorFramework.Events;
using DotNetActorFramework.Caching;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// A fluent builder for configuring and creating actor systems.
/// Provides methods to define middleware, services, background workers, and system-level options.
/// </summary>
public class ActorSystemBuilder
{
    private readonly string _systemName;
    private readonly List<IActorMiddleware> _middleware = [];
    private readonly List<IBackgroundWorker> _backgroundWorkers = [];
    private readonly ActorSystemOptions _options;
    private MetricsCollector? _metricsCollector;
    private EventBus? _eventBus;
    private ActorCacheService? _cacheService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorSystemBuilder"/> class.
    /// </summary>
    /// <param name="systemName">The unique name for the actor system being built.</param>
    /// <exception cref="ArgumentException">Thrown if the provided system name is null or whitespace.</exception>
    public ActorSystemBuilder(string systemName)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            throw new ArgumentException("System name cannot be empty.", nameof(systemName));

        _systemName = systemName;
        _options = new ActorSystemOptions();
    }

    /// <summary>
    /// Adds logging middleware to the actor system pipeline.
    /// </summary>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ActorSystemBuilder WithLogging()
    {
        _middleware.Add(new LoggingMiddleware(
            new NullLogger<LoggingMiddleware>()));
        return this;
    }

    /// <summary>
    /// Adds error handling middleware with the specified strategy to the pipeline.
    /// </summary>
    /// <param name="strategy">The <see cref="ErrorHandlingStrategy"/> to use for handling message processing errors.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    public ActorSystemBuilder WithErrorHandling(ErrorHandlingStrategy strategy)
    {
        _middleware.Add(new ErrorHandlingMiddleware(strategy));
        return this;
    }

    /// <summary>
    /// Adds rate limiting middleware.
    /// </summary>
    public ActorSystemBuilder WithRateLimiting(int tokensPerSecond = 1000)
    {
        var rateLimiter = new RateLimiter(tokensPerSecond);
        _middleware.Add(new RateLimitingMiddleware(rateLimiter));
        return this;
    }

    /// <summary>
    /// Adds metrics collection middleware.
    /// </summary>
    public ActorSystemBuilder WithMetrics()
    {
        _metricsCollector ??= new MetricsCollector();
        _middleware.Add(new MetricsCollectionMiddleware(_metricsCollector));
        return this;
    }

    /// <summary>
    /// Adds authentication middleware.
    /// </summary>
    public ActorSystemBuilder WithAuthentication(IAuthenticationProvider authProvider)
    {
        _middleware.Add(new AuthenticationMiddleware(authProvider));
        return this;
    }

    /// <summary>
    /// Adds caching service.
    /// </summary>
    public ActorSystemBuilder WithCaching(int maxCapacity = 1000)
    {
        _cacheService = new ActorCacheService(maxCapacity);
        return this;
    }

    /// <summary>
    /// Adds event bus for pub/sub messaging.
    /// </summary>
    public ActorSystemBuilder WithEventBus()
    {
        _eventBus = new EventBus();
        return this;
    }

    /// <summary>
    /// Adds a background worker.
    /// </summary>
    public ActorSystemBuilder AddBackgroundWorker(IBackgroundWorker worker)
    {
        if (worker != null)
            _backgroundWorkers.Add(worker);
        return this;
    }

    /// <summary>
    /// Configures mailbox settings.
    /// </summary>
    public ActorSystemBuilder WithMailboxCapacity(int capacity)
    {
        _options.DefaultMailboxCapacity = capacity;
        return this;
    }

    /// <summary>
    /// Builds the actor system with all configured components.
    /// </summary>
    public ActorSystem Build()
    {
        var actorSystem = new ActorSystem(_systemName);

        // Store references for later use
        if (_eventBus != null)
            actorSystem.SetProperty("EventBus", _eventBus);

        if (_cacheService != null)
            actorSystem.SetProperty("CacheService", _cacheService);

        if (_metricsCollector != null)
            actorSystem.SetProperty("MetricsCollector", _metricsCollector);

        return actorSystem;
    }

    /// <summary>
    /// Gets the configured middleware pipeline.
    /// </summary>
    public MiddlewarePipeline BuildMiddlewarePipeline()
    {
        var pipeline = new MiddlewarePipeline();
        foreach (var middleware in _middleware.OrderBy(m => m.Order))
            pipeline.Register(middleware);
        return pipeline;
    }

    /// <summary>
    /// Builds the background worker service.
    /// </summary>
    public BackgroundWorkerService BuildBackgroundWorkers()
    {
        var service = new BackgroundWorkerService();
        foreach (var worker in _backgroundWorkers)
            service.RegisterWorker(worker);
        return service;
    }

    /// <summary>
    /// Gets the metrics collector (if configured).
    /// </summary>
    public MetricsCollector? GetMetricsCollector() => _metricsCollector;

    /// <summary>
    /// Gets the event bus (if configured).
    /// </summary>
    public EventBus? GetEventBus() => _eventBus;

    /// <summary>
    /// Gets the cache service (if configured).
    /// </summary>
    public ActorCacheService? GetCacheService() => _cacheService;
}

/// <summary>
/// Null logger for testing and internal use.
/// </summary>
public class NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // No-op
    }

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

/// <summary>
/// Extension methods for actor system configuration.
/// </summary>
public static class ActorSystemExtensions
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ActorSystem, System.Collections.Concurrent.ConcurrentDictionary<string, object>> PropertyBags = new();

    /// <summary>
    /// Sets a property on the actor system. Properties are stored per system instance
    /// and released automatically when the system is garbage collected.
    /// </summary>
    public static void SetProperty(this ActorSystem system, string key, object value)
    {
        if (system == null || string.IsNullOrWhiteSpace(key))
            return;

        var bag = PropertyBags.GetOrCreateValue(system);
        bag[key] = value;
    }

    /// <summary>
    /// Gets a property from the actor system, or <c>null</c> if the key has not been set.
    /// </summary>
    public static object? GetProperty(this ActorSystem system, string key)
    {
        if (system == null || string.IsNullOrWhiteSpace(key))
            return null;

        return PropertyBags.TryGetValue(system, out var bag) && bag.TryGetValue(key, out var value)
            ? value
            : null;
    }
}
