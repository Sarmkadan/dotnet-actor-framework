# ActorSystemBuilder

Fluent builder used to configure and instantiate an `ActorSystem` together with its optional middleware pipelines, background workers, and shared services.

## API

### `public ActorSystemBuilder()`
Creates a new builder instance with default settings. No parameters. Throws no exceptions.

### `public ActorSystemBuilder WithLogging()`
Enables logging for the actor system. Returns the same builder instance to allow chaining. Throws `ArgumentNullException` if a logging provider is null when internally accessed; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithErrorHandling()`
Configures global error handling middleware. Returns the builder for further configuration. Throws `ArgumentNullException` if the supplied error handler is null; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithRateLimiting()`
Adds rate‑limiting middleware to the pipeline. Returns the builder. Throws `ArgumentOutOfRangeException` if limit values are invalid; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithMetrics()`
Enables collection and reporting of metrics. Returns the builder. Throws `ArgumentNullException` if a metrics collector is null when accessed; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithAuthentication()`
Adds authentication middleware to the pipeline. Returns the builder. Throws `ArgumentNullException` if an authentication scheme is null; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithCaching()`
Configures an actor‑level cache service. Returns the builder. Throws `ArgumentNullException` if a cache implementation is null; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithEventBus()`
Attaches an event bus for inter‑actor communication. Returns the builder. Throws `ArgumentNullException` if the event bus is null; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder AddBackgroundWorker()`
Registers a background worker service with the system. Returns the builder. Throws `ArgumentNullException` if the worker delegate is null; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystemBuilder WithMailboxCapacity()`
Sets the default mailbox capacity for actors created by the system. Returns the builder. Throws `ArgumentOutOfRangeException` if the capacity is less than or equal to zero; throws `InvalidOperationException` if called after `Build`.

### `public ActorSystem Build()`
Creates an immutable `ActorSystem` instance based on the configuration accumulated in the builder. Returns the new `ActorSystem`. Throws `InvalidOperationException` if required services (e.g., logging when `WithLogging` was called) have not been properly configured.

### `public MiddlewarePipeline BuildMiddlewarePipeline()`
Constructs the middleware pipeline from the enabled features (logging, error handling, rate limiting, metrics, authentication, caching, event bus). Returns a `MiddlewarePipeline` ready for use. Throws `InvalidOperationException` if called before any pipeline‑affecting method has been invoked.

### `public BackgroundWorkerService BuildBackgroundWorkers()`
Instantiates a container for all background workers added via `AddBackgroundWorker`. Returns a `BackgroundWorkerService`. Throws `InvalidOperationException` if no workers have been added.

### `public MetricsCollector? GetMetricsCollector()`
Retrieves the metrics collector if `WithMetrics` was previously called; otherwise returns `null`. No parameters. Throws no exceptions.

### `public EventBus? GetEventBus()`
Retrieves the event bus if `WithEventBus` was previously called; otherwise returns `null`. No parameters. Throws no exceptions.

### `public ActorCacheService? GetCacheService()`
Retrieves the actor cache service if `WithCaching` was previously called; otherwise returns `null`. No parameters. Throws no exceptions.

### `public void Log<TState>()`
Writes a log entry using the configured logging infrastructure. The generic `TState` allows state‑rich logging without boxing. Returns `void`. Throws `ObjectDisposedException` if the underlying logger has been disposed; throws `InvalidOperationException` if logging was not enabled.

### `public bool IsEnabled`
Indicates whether the builder (or the built system) has logging enabled. Read‑only property. Returns `true` when logging is active; otherwise `false`. No exceptions.

### `public IDisposable? BeginScope<TState>()`
Begins a logical operation scope for logging, returning an `IDisposable` that ends the scope when disposed. Returns `null` if logging is disabled. Throws `ArgumentNullException` if the state argument is null.

### `public static void SetProperty()`
Sets a static property on the builder type (e.g., a global configuration flag). Returns `void`. Throws `ArgumentException` if the property name is unknown or the value is invalid; throws `NotSupportedException` if the property is read‑only.

## Usage

### Basic actor system with logging and metrics
```csharp
using DotNetActorFramework;

var builder = new ActorSystemBuilder()
    .WithLogging()
    .WithMetrics();

var system = builder.Build();

// Use the system...
var metrics = builder.GetMetricsCollector();
metrics?.Record("ActorSystem.Started");
```

### Advanced configuration with background workers and custom mailbox size
```csharp
using DotNetActorFramework;
using System.Threading;

var builder = new ActorSystemBuilder()
    .WithLogging()
    .WithErrorHandling()
    .WithRateLimiting()
    .WithAuthentication()
    .WithCaching()
    .WithEventBus()
    .AddBackgroundWorker(() =>
    {
        // Worker logic
        Thread.Sleep(Timeout.Infinite);
    })
    .WithMailboxCapacity(1024);

var system = builder.Build();

var pipeline = builder.BuildMiddlewarePipeline();
var workers  = builder.BuildBackgroundWorkers();
var bus      = builder.GetEventBus();   // non‑null because WithEventBus was called
```

## Notes

- The builder is **not thread‑safe**. All configuration methods should be invoked from a single thread before calling `Build`. Concurrent calls to configuration methods may result in undefined state.
- Once `Build` has been invoked, the builder instance enters an immutable state; further calls to any `With*` or `Add*` method will throw `InvalidOperationException`.
- Services retrieved via `GetMetricsCollector`, `GetEventBus`, or `GetCacheService` return `null` when the corresponding feature was not enabled. Consumers must check for `null` before use.
- The `Log<TState>` method and `BeginScope<TState>` are no‑ops when logging is disabled (`IsEnabled` returns `false`). They are safe to call on a built system; however, invoking them after the logger has been disposed will throw `ObjectDisposedException`.
- The static `SetProperty` method modifies global state that affects all subsequently created builder instances. It should be used only during application start‑up and is not thread‑safe; external synchronization is required if called from multiple threads.
