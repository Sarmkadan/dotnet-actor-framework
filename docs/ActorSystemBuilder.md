# ActorSystemBuilder

`ActorSystemBuilder` is a fluent builder for assembling an `ActorSystem` together with its middleware pipeline, background workers, and shared services (event bus, cache, metrics). It lives in the `DotNetActorFramework.Configuration` namespace.

The builder only records configuration. Services are created when the corresponding `With*` method is called, and `Build*` methods assemble the final objects from what was recorded.

## Constructor

### `public ActorSystemBuilder(string systemName)`
Creates a builder for an actor system with the given name.

- `systemName`: the name passed to the `ActorSystem` created by `Build()`.
- Throws `ArgumentException` if `systemName` is `null`, empty, or whitespace.

## Fluent configuration methods

Every method below returns the same builder instance, so calls can be chained.

| Method | Effect |
|---|---|
| `WithLogging()` | Adds a `LoggingMiddleware` to the pipeline. It is wired to an internal `NullLogger`, so it currently produces no output. |
| `WithErrorHandling(ErrorHandlingStrategy strategy)` | Adds an `ErrorHandlingMiddleware` that uses `strategy`. Throws `ArgumentNullException` if `strategy` is `null`. |
| `WithRateLimiting(int tokensPerSecond = 1000)` | Adds a `RateLimitingMiddleware` backed by a new `RateLimiter(tokensPerSecond)`. |
| `WithMetrics()` | Adds a `MetricsCollectionMiddleware` that shares one `MetricsCollector`. The collector is created on the first call and reused by later calls. |
| `WithAuthentication(IAuthenticationProvider authProvider)` | Adds an `AuthenticationMiddleware` that uses `authProvider`. Throws `ArgumentNullException` if `authProvider` is `null`. |
| `WithCaching(int maxCapacity = 1000)` | Creates an `ActorCacheService(maxCapacity)`. Calling it again replaces the previous service. |
| `WithEventBus()` | Creates an `EventBus`. Calling it again replaces the previous bus. |
| `AddBackgroundWorker(IBackgroundWorker worker)` | Queues `worker` for `BuildBackgroundWorkers()`. A `null` argument is ignored silently. |
| `WithMailboxCapacity(int capacity)` | Stores the default mailbox capacity in the builder's internal `ActorSystemOptions`. See the note on `Build()` below. |
| `WithMailboxOverflowPolicy(MailboxOverflowPolicy policy)` | Stores the default overflow policy in the builder's internal `ActorSystemOptions`. See the note on `Build()` below. |

Middleware is ordered by each middleware's `Order` value when the pipeline is built, not by the order of the calls:

| Middleware | `Order` |
|---|---|
| `LoggingMiddleware` | 0 |
| `AuthenticationMiddleware` | 10 |
| `RateLimitingMiddleware` | 50 |
| `ErrorHandlingMiddleware` | 100 |
| `MetricsCollectionMiddleware` | 200 |

### Error handling strategies

`WithErrorHandling` takes an `ErrorHandlingStrategy`, which is an abstract class. The framework provides these implementations:

- `SuppressErrorStrategy`: swallows the error.
- `RetryErrorStrategy(int maxRetries = 3, TimeSpan? initialDelay = null, double backoffMultiplier = 2.0)`: retries with exponential backoff. The default initial delay is 100 ms.
- `FailFastErrorStrategy`: throws an `InvalidOperationException` that wraps the original error.

## Build methods

### `public ActorSystem Build()`
Creates a new `ActorSystem` with the builder's name. The builder registers the shared services on it as properties, which can be read with `GetProperty` (see below):

- `"EventBus"`, if `WithEventBus()` was called.
- `"CacheService"`, if `WithCaching()` was called.
- `"MetricsCollector"`, if `WithMetrics()` was called.

Each call creates a new `ActorSystem`, but the event bus, cache, and metrics collector are the same instances across calls.

> **Note:** `Build()` does not pass the builder's internal `ActorSystemOptions` to the `ActorSystem`. Values set by `WithMailboxCapacity` and `WithMailboxOverflowPolicy` are therefore not applied by `Build()` in the current version.

### `public MiddlewarePipeline BuildMiddlewarePipeline()`
Creates a new `MiddlewarePipeline` from the recorded middleware, sorted by `Order`. Each call creates a new pipeline, but the middleware instances are shared.

### `public BackgroundWorkerService BuildBackgroundWorkers()`
Creates a new `BackgroundWorkerService` and registers every worker added with `AddBackgroundWorker`, in the order they were added.

## Accessors

These methods return the services created by the corresponding `With*` call, or `null` if that call was not made.

| Method | Returns |
|---|---|
| `MetricsCollector? GetMetricsCollector()` | The collector created by `WithMetrics()`. |
| `EventBus? GetEventBus()` | The bus created by `WithEventBus()`. |
| `ActorCacheService? GetCacheService()` | The service created by `WithCaching()`. |

## Actor system properties

`ActorSystemExtensions` (in the same file) provides per-instance property storage for `ActorSystem`. Properties are held in a `ConditionalWeakTable`, so they are released when the system is garbage collected.

- `public static void SetProperty(this ActorSystem system, string key, object value)`: stores a value. Does nothing if `system` is `null` or `key` is empty or whitespace.
- `public static object? GetProperty(this ActorSystem system, string key)`: returns the stored value, or `null` if the key is not set.

## Usage

### Basic setup with metrics and an event bus

```csharp
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Middleware;

var builder = new ActorSystemBuilder("orders")
    .WithLogging()
    .WithErrorHandling(new RetryErrorStrategy(maxRetries: 5))
    .WithRateLimiting(tokensPerSecond: 500)
    .WithMetrics()
    .WithEventBus()
    .WithCaching(maxCapacity: 2048);

var system = builder.Build();
var pipeline = builder.BuildMiddlewarePipeline();

var metrics = builder.GetMetricsCollector(); // non-null: WithMetrics() was called
var bus = builder.GetEventBus();             // non-null: WithEventBus() was called
var cache = builder.GetCacheService();       // non-null: WithCaching() was called
```

### Authentication and a background worker

```csharp
using DotNetActorFramework.BackgroundWorkers;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Middleware;

var builder = new ActorSystemBuilder("payments")
    .WithAuthentication(new TokenAuthProvider())
    .WithErrorHandling(new FailFastErrorStrategy())
    .AddBackgroundWorker(new HeartbeatWorker());

var system = builder.Build();
var workers = builder.BuildBackgroundWorkers();
```

The provider and worker types used above are your own implementations of `IAuthenticationProvider` and `IBackgroundWorker`.

## Notes

- **Not thread-safe.** The builder has no internal synchronization. Call its methods from one thread, before `Build*` is called.
- **Repeated calls.** Calling `WithMetrics()` twice adds two middleware instances that share one collector. Calling `WithRateLimiting()` twice adds two limiters. Calling `WithCaching()` or `WithEventBus()` again replaces the earlier service, so systems built before the second call keep the old instance.
- **Services are null when not configured.** Check the result of `GetMetricsCollector`, `GetEventBus`, and `GetCacheService` before use.
- **Mailbox settings are not applied.** See the note on `Build()`. Until `Build()` passes `ActorSystemOptions` through, `WithMailboxCapacity` and `WithMailboxOverflowPolicy` have no effect on the built system.
- **Logging is a no-op.** `WithLogging()` uses `NullLogger<T>`, which discards all entries and reports every level as disabled.
- **Background workers passed as `null`** are dropped without an error.
