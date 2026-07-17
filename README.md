# DotNetActorFramework

An in-process actor framework for .NET: addressable actors with per-actor
mailboxes, supervision strategies, middleware, metrics and pluggable
persistence, composed via `Microsoft.Extensions.DependencyInjection` or a
fluent builder.

## ActorCacheService

The `ActorCacheService` class provides caching functionality for actor references and paths, reducing lookup overhead and improving performance when frequently accessing the same actors. It implements an LRU (Least Recently Used) eviction policy when capacity is reached and supports time-based expiration for cached entries.

### Usage Example

```csharp
// Initialize the actor cache service with default settings (1000 entries, 5 minute TTL)
var actorCache = new ActorCacheService();

// Create an actor system and actors
var actorSystem = new ActorSystem("CachingDemoSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Cache actor references for frequent access
actorCache.Set(rootActor.Path, rootActor);
actorCache.Set(workerActor.Path, workerActor);

// Retrieve cached actor references
var cachedRoot = actorCache.Get(rootActor.Path);
if (cachedRoot != null)
{
    Console.WriteLine($"Found cached actor: {cachedRoot.Path}");
}

// Check if an actor is in cache
bool containsWorker = actorCache.Contains(workerActor.Path);
Console.WriteLine($"Worker in cache: {containsWorker}");

// Remove an entry from cache
bool removed = actorCache.Remove(rootActor.Path);
Console.WriteLine($"Removed from cache: {removed}");

// Clear all cached entries
actorCache.Clear();

// Get cache statistics
Console.WriteLine($"Cache contains {actorCache.Count} entries");

// Remove expired entries
int expiredRemoved = actorCache.RemoveExpired();
Console.WriteLine($"Removed {expiredRemoved} expired entries");
```

### Properties and Methods

- `ActorCacheService(int maxCapacity = 1000, TimeSpan? ttl = null)`: Initializes a new actor cache service with configurable maximum capacity and time-to-live for cached entries.
- `void Set(ActorPath path, ActorRef actorRef)`: Adds or updates an actor reference in the cache.
- `ActorRef? Get(ActorPath path)`: Retrieves an actor reference from the cache, returning null if not found or expired.
- `bool Contains(ActorPath path)`: Checks if a path is in the cache.
- `bool Remove(ActorPath path)`: Removes an entry from the cache.
- `void Clear()`: Clears all cached entries.
- `int Count { get; }`: Gets the number of items currently in the cache.
- `int RemoveExpired()`: Removes expired entries from the cache and returns the count of removed entries.

### CachedActorRef Class

The internal `CachedActorRef` class represents a cached actor reference with metadata:

- `ActorRef ActorRef { get; }`: Gets the actor reference.
- `DateTime CachedAt { get; }`: Gets the timestamp when the actor was cached.
- `DateTime LastAccessedAt { get; set; }`: Gets or sets the timestamp of the last access.

## WorkerActor

The `WorkerActor` class represents a leaf node in the actor hierarchy that performs individual work items within a parent-child supervision structure. Worker actors are designed to handle specific tasks delegated by their parent supervisor, process messages asynchronously, and maintain their own internal state. Each worker tracks its work items and metrics independently, making it ideal for parallelizable workloads and distributed processing scenarios.

Worker actors participate in the actor lifecycle through initialization, message processing, and graceful shutdown, with supervision strategies automatically handling failures according to the parent's configuration.

### Usage Example

```csharp
// Create an actor system and worker actor
var actorSystem = new ActorSystem("WorkerDemoSystem");
var workerPath = new ActorPath("/user/workers/data-processor");
var workerActor = new WorkerActor(workerPath);

// Initialize the actor system and worker
var config = new ActorSystemConfiguration();
await config.InitializeAsync("WorkerSystem");
var workerRef = await config.CreateActorAsync(workerPath);

// Send work messages to the worker
var dispatcher = new MessageDispatcher(new MailboxService(new ActorSystemOptions()), new ActorRegistry());

// Process individual work items
for (int i = 0; i < 5; i++)
{
    var workMessage = new ControlMessage("work", new Dictionary<string, object>
    {
        { "taskId", i },
        { "duration", 100 },
        { "data", $"task-data-{i}" }
    });
    
    await dispatcher.SendAsync(workerRef, workMessage);
}

// Monitor worker metrics
var metrics = workerRef.GetMetricsSummary();
Console.WriteLine($"Worker processed {metrics.MessageCount} messages with {metrics.ErrorCount} errors");

// Gracefully terminate the worker
await workerRef.TerminateAsync();
```

### Key Members

- `WorkerActor(ActorPath path)`: Initializes a new worker actor with the specified hierarchical path.
- `ReceiveAsync(Message message)`: Processes incoming work messages and handles task execution.
- `OnStopAsync()`: Called when the actor is stopping to perform cleanup operations.

### Behavior

The `WorkerActor` is designed as a worker node in a supervised hierarchy:

- Receives work messages via `ControlMessage` with command "work"
- Tracks work item count internally via `_workItems` field
- Processes work asynchronously with configurable duration
- Records success metrics upon completion
- Provides visibility into processed work items during shutdown

Worker actors are typically created and managed by parent supervisor actors, which distribute work across a pool of workers for parallel processing and load balancing.


## UnreliableActor

The `UnreliableActor` class demonstrates fault tolerance patterns by intentionally failing on early attempts before eventually succeeding. This pattern is useful for testing supervision strategies and understanding how actors recover from transient failures. The actor tracks its attempt count internally and throws exceptions on the first few attempts before completing successfully, allowing supervision mechanisms to demonstrate their effectiveness.

### Usage Example

```csharp
// Create an actor system with supervision enabled
var services = new ServiceCollection();
services.AddActorFramework(options =>
{
    options.SystemName = "FaultToleranceDemo";
    options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
    options.BackoffInitialDelayMs = 100;
    options.BackoffMaxDelayMs = 5000;
});

var sp = services.BuildServiceProvider();
var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
var system = await config.InitializeAsync();

// Create an unreliable actor
var unreliablePath = new ActorPath("/user/unreliable-worker");
var unreliableActor = await config.CreateActorAsync(unreliablePath);

// Send a risky operation message that will fail initially
var riskyOperation = new ControlMessage("risky-operation");
await unreliableActor.ReceiveAsync(riskyOperation);

// The actor will fail on attempts 1 and 2, then succeed on attempt 3
// Supervision will automatically restart the actor between failures

// Check health after supervision
await Task.Delay(2000); // Allow supervision to work
var health = config.GetHealthSummary();
Console.WriteLine($"System recovered: {health.RunningActors} actors running, {health.ErroredActors} errors");

// Shutdown
await system.ShutdownAsync();
```

### Key Members

- `UnreliableActor(ActorPath path)`: Initializes a new unreliable actor with the specified path.
- `ReceiveAsync(Message message)`: Handles incoming messages, intentionally failing on early attempts before succeeding.
- `OnStopAsync()`: Called when the actor is stopped, reporting the total attempt count.

### Behavior

The `UnreliableActor` is designed to simulate transient failures that can be recovered through supervision strategies:

- First 2 attempts: Throws `InvalidOperationException`
- Attempt 3+: Successfully processes the message
- Tracks attempt count internally via `_attemptCount` field
- Reports final attempt count when stopped via `OnStopAsync()`

This pattern is particularly useful for testing supervision strategies like `Restart`, `Resume`, or `Escalate` without requiring external failure injection.

## DateTimeExtensions

The `DateTimeExtensions` class provides utility methods for working with `DateTime` objects, offering convenient ways to calculate elapsed time, check time conditions, and format timestamps. These extensions are particularly useful for actor lifecycle management, timeout handling, and logging within the actor framework.

### Usage Example

```csharp
// Create a timestamp for "now" in actor processing
var messageReceivedAt = DateTime.UtcNow;

// Check if a timeout has elapsed
if (messageReceivedAt.HasElapsed(TimeSpan.FromSeconds(30)))
{
    Console.WriteLine("Timeout elapsed for message processing");
}

// Get elapsed time for metrics
var elapsed = messageReceivedAt.GetElapsed();
Console.WriteLine($"Processing took {elapsed.TotalMilliseconds}ms");

// Check if a scheduled event should fire
var scheduledTime = DateTime.UtcNow.AddMinutes(5);
if (scheduledTime.IsPast())
{
    Console.WriteLine("Scheduled event should fire now");
}

// Get human-readable time description for logs
var oldTimestamp = DateTime.UtcNow.AddHours(-2);
Console.WriteLine($"Message processed at: {oldTimestamp.GetTimeAgoDescription()}");

// Format timestamp for log files
var logTimestamp = DateTime.UtcNow;
Console.WriteLine($"[{logTimestamp.GetLogTimestamp()}] Processing started");

// Round to nearest second for consistent timestamps
var preciseTime = DateTime.UtcNow.AddMilliseconds(500);
var roundedTime = preciseTime.RoundToSecond();
Console.WriteLine($"Rounded time: {roundedTime}");

// Check if within a time window
var windowStart = DateTime.UtcNow.AddMinutes(-10);
var windowEnd = DateTime.UtcNow.AddMinutes(10);
if (DateTime.UtcNow.IsWithinWindow(windowStart, windowEnd))
{
    Console.WriteLine("Current time is within the specified window");
}
```

### Available Methods

- `TimeSpan GetElapsed(this DateTime dateTime)`: Gets the elapsed time since this datetime.
- `long GetElapsedMilliseconds(this DateTime dateTime)`: Gets the elapsed time in milliseconds.
- `bool HasElapsed(this DateTime dateTime, TimeSpan duration)`: Determines if the specified duration has elapsed since this datetime.
- `bool IsPast(this DateTime dateTime)`: Determines if this datetime is in the past.
- `bool IsFuture(this DateTime dateTime)`: Determines if this datetime is in the future.
- `string GetTimeAgoDescription(this DateTime dateTime)`: Gets a human-readable description of how long ago this datetime was.
- `DateTime RoundToSecond(this DateTime dateTime)`: Truncates a datetime to whole seconds, discarding milliseconds.
- `bool IsWithinWindow(this DateTime dateTime, DateTime start, DateTime end)`: Determines if the time is within the specified window.
- `string GetLogTimestamp(this DateTime dateTime)`: Gets a formatted timestamp suitable for logging (ISO 8601 with milliseconds UTC).

## EnvelopeExtensions

The `EnvelopeExtensions` class provides extension methods for working with `Envelope` objects, enabling type-safe message extraction, envelope cloning, and formatted logging. These utilities simplify common envelope operations throughout the actor system, particularly for message processing, debugging, and monitoring scenarios.

### Usage Example

```csharp
// Create a message and envelope
var message = new Message("process-order", new Dictionary<string, object>
{
    { "orderId", "order-123" },
    { "amount", 99.99 }
});

var recipient = new ActorRef(new ActorPath("/user/order-processor"), Guid.NewGuid());
var sender = new ActorRef(new ActorPath("/user/api-gateway"), Guid.NewGuid());
var envelope = new Envelope(message, recipient, sender);

// Check if envelope contains a specific message type
bool isOrderMessage = envelope.IsMessageType<Message>();
Console.WriteLine($"Is message type: {isOrderMessage}");

// Try to extract message with type checking
if (envelope.TryGetMessage<Message>(out var typedMessage))
{
    Console.WriteLine($"Extracted message: {typedMessage.Command}");
}

// Clone an envelope for retry scenarios
var clonedEnvelope = envelope.Clone();
Console.WriteLine($"Original: {envelope.EnvelopeId:N}, Clone: {clonedEnvelope.EnvelopeId:N}");

// Get formatted log string for debugging
string logEntry = envelope.ToLogString(includeMessageContent: true);
Console.WriteLine(logEntry);

// Get human-readable age of envelope
string ageString = envelope.GetAgeString();
Console.WriteLine($"Envelope age: {ageString}");
```

### Available Methods

- `bool IsMessageType<T>(this Envelope envelope)`: Determines whether the envelope contains a message of the specified type.
- `bool TryGetMessage<T>(this Envelope envelope, out T? message)`: Attempts to cast the message to the specified type.
- `Envelope Clone(this Envelope envelope)`: Creates a shallow copy of the envelope with a new unique identifier.
- `string ToLogString(this Envelope envelope, bool includeMessageContent = false)`: Formats the envelope for logging purposes with detailed information.
- `string GetAgeString(this Envelope envelope)`: Gets the age of the envelope in a human-readable format.

## ActorSystemBuilder

The `ActorSystemBuilder` class provides a fluent interface for constructing and configuring an `ActorSystem` instance. It allows you to chain configuration methods to customize various aspects of the actor system such as logging, error handling, rate limiting, metrics collection, authentication, caching, event bus, background workers, and mailbox capacity. The builder pattern enables clean, readable configuration that can be easily modified and extended.

### Usage Example

```csharp
// Create and configure an actor system using the builder pattern
var actorSystem = new ActorSystemBuilder("ProductionSystem")
    .WithLogging()
    .WithErrorHandling()
    .WithRateLimiting(tokensPerSecond: 1000, bucketCapacity: 10000)
    .WithMetrics()
    .WithAuthentication(new TokenAuthenticationProvider("secret-token-123"))
    .WithCaching(TimeSpan.FromMinutes(5))
    .WithEventBus(new InMemoryEventBus())
    .AddBackgroundWorker(new MetricsCollectorWorker(actorSystem, new MetricsCollector()))
    .WithMailboxCapacity(2000)
    .Build();

// Alternative: Build middleware pipeline separately for advanced configuration
var middlewarePipeline = new ActorSystemBuilder("MiddlewareDemo")
    .WithLogging()
    .WithErrorHandling()
    .WithRateLimiting()
    .BuildMiddlewarePipeline();

// Create actor system with custom middleware
var actorSystemWithMiddleware = new ActorSystemBuilder("CustomSystem")
    .WithLogging()
    .WithErrorHandling()
    .WithRateLimiting()
    .Build();

// Access built services for monitoring and management
var metricsCollector = new ActorSystemBuilder("MetricsDemo")
    .WithMetrics()
    .Build()
    .GetMetricsCollector();

if (metricsCollector != null)
{
    var metrics = metricsCollector.GetSystemMetrics();
    Console.WriteLine($"System metrics: {metrics.TotalMessagesProcessed} messages processed");
}
```

## ActorSystemOptions

The `ActorSystemOptions` class provides centralized configuration for the DotNetActorFramework, controlling system-wide behavior such as mailbox capacity, supervision strategies, persistence settings, cluster configuration, and performance tuning. These options are used during actor system initialization and can be customized for different deployment scenarios (high-performance, reliable, or cluster-aware systems).

### Usage Example

```csharp
// Initialize with default configuration
var defaultOptions = new ActorSystemOptions
{
    SystemName = "ProductionSystem",
    DefaultMailboxCapacity = 2000,
    DefaultMailboxType = MailboxType.FIFO,
    DefaultTimeoutSeconds = 30,
    MaxMessageRetries = 3,
    MaxActorDepth = 10,
    DefaultSupervisionStrategy = SupervisionStrategy.Restart,
    EnableMessagePersistence = true,
    EnableMetricsCollection = true,
    EnableActorStateSnapshotting = true,
    SnapshotIntervalSeconds = 300,
    DefaultPersistenceBackend = PersistenceBackend.InMemory,
    DatabaseConnectionString = "Server=localhost;Database=ActorFramework;User Id=sa;Password=your_password;",
    EnableClusterMode = false,
    ClusterAddress = "127.0.0.1:8080",
    MaxClusterNodes = 5,
    UnhealthyErrorRateThreshold = 0.25,
    CriticalErrorRateThreshold = 0.5,
    InitialBackoffDelayMs = 100,
    MaxBackoffDelayMs = 10000,
    BackoffMultiplier = 2.0,
    EnableDetailedLogging = false
};

// Create actor system with custom configuration
var actorSystem = new ActorSystem(defaultOptions);

// Use predefined configuration profiles for common scenarios
var highPerformanceOptions = ActorSystemOptions.CreateHighPerformance();
var reliableOptions = ActorSystemOptions.CreateReliable();
var clusterOptions = ActorSystemOptions.CreateCluster("node1.example.com:8080");

// Validate configuration before use
try
{
    defaultOptions.Validate();
    Console.WriteLine("Configuration is valid!");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Configuration error: {ex.Message}");
}
```

### Properties

- `string SystemName`: Gets or sets the name of the actor system (default: "DefaultActorSystem")
- `int DefaultMailboxCapacity`: Gets or sets the default capacity for actor mailboxes (default: 1000)
- `MailboxType DefaultMailboxType`: Gets or sets the default mailbox type (FIFO, Priority, or LIFO) (default: MailboxType.FIFO)
- `int DefaultTimeoutSeconds`: Gets or sets the default timeout for message operations in seconds (default: 30)
- `int MaxMessageRetries`: Gets or sets the maximum number of retry attempts for failed message deliveries (default: 3)
- `int MaxActorDepth`: Gets or sets the maximum depth of the actor hierarchy (default: 10)
- `SupervisionStrategy DefaultSupervisionStrategy`: Gets or sets the default supervision strategy for handling actor failures (default: SupervisionStrategy.Restart)
- `bool EnableMessagePersistence`: Gets or sets whether message persistence is enabled (default: true)
- `bool EnableMetricsCollection`: Gets or sets whether metrics collection is enabled (default: true)
- `bool EnableActorStateSnapshotting`: Gets or sets whether actor state snapshotting is enabled (default: true)
- `int SnapshotIntervalSeconds`: Gets or sets the interval between state snapshots in seconds (default: 300)
- `PersistenceBackend DefaultPersistenceBackend`: Gets or sets the default persistence backend (InMemory, SqlServer, PostgreSql) (default: PersistenceBackend.InMemory)
- `string? DatabaseConnectionString`: Gets or sets the database connection string for persistence (default: null)
- `bool EnableClusterMode`: Gets or sets whether cluster mode is enabled (default: false)
- `string ClusterAddress`: Gets or sets the cluster address for distributed actor systems (default: "127.0.0.1:8080")
- `int MaxClusterNodes`: Gets or sets the maximum number of nodes in the cluster (default: 10)
- `double UnhealthyErrorRateThreshold`: Gets or sets the error rate threshold for marking actors as unhealthy (default: 0.25)
- `double CriticalErrorRateThreshold`: Gets or sets the error rate threshold for marking actors as critical (default: 0.5)
- `int InitialBackoffDelayMs`: Gets or sets the initial delay for exponential backoff in milliseconds (default: 100)
- `int MaxBackoffDelayMs`: Gets or sets the maximum delay for exponential backoff in milliseconds (default: 10000)
- `double BackoffMultiplier`: Gets or sets the multiplier for exponential backoff (default: 2.0)
- `bool EnableDetailedLogging`: Gets or sets whether detailed logging is enabled (default: false)

### Methods

- `void Validate()`: Validates the configuration options and throws exceptions for invalid values
- `static ActorSystemOptions CreateDefault()`: Creates a default configuration
- `static ActorSystemOptions CreateHighPerformance()`: Creates a high-performance configuration optimized for throughput
- `static ActorSystemOptions CreateReliable()`: Creates a reliable configuration optimized for durability
- `static ActorSystemOptions CreateCluster(string clusterAddress = "127.0.0.1:8080")`: Creates a cluster-optimized configuration

### Configuration Profiles

The framework provides several predefined configuration profiles:

- **Default**: Balanced configuration suitable for most use cases
- **HighPerformance**: Optimized for maximum throughput with minimal overhead (persistence and metrics disabled)
- **Reliable**: Optimized for durability and fault tolerance (persistence and snapshotting enabled)
- **Cluster**: Configured for distributed actor systems with cluster mode enabled

## GuardExtensions

The `GuardExtensions` class provides guard clause methods for validating method parameters and enforcing common validation patterns throughout the actor framework. These extension methods reduce boilerplate validation code by throwing appropriate exceptions when validation fails, making the code more concise and readable while maintaining robust input validation.

### Usage Example

```csharp
// Validate method parameters using guard extensions
public class ActorConfiguration
{
    public ActorConfiguration(string name, int mailboxCapacity)
    {
        // Validate parameters using guard extensions
        Name = name.NotNullOrWhiteSpace(nameof(name));
        MailboxCapacity = mailboxCapacity.MustBePositive(nameof(mailboxCapacity));
    }

    public string Name { get; }
    public int MailboxCapacity { get; }
}

// Validate collections and GUIDs
public void RegisterActor(Guid actorId, IEnumerable<IActor> actors)
{
    actorId = actorId.NotEmpty(nameof(actorId));
    actors = actors.NotEmpty(nameof(actors));

    // Validate boolean conditions
    bool isValid = ValidateActor(actorId);
    isValid.MustBeTrue("Actor validation failed");
}

// Validate that conditions are false
public void ValidateConfiguration(bool shouldSkipValidation)
{
    shouldSkipValidation.MustBeFalse("Validation should not be skipped in production");
}

// Validate non-negative and positive numbers
public void ConfigureTimeout(int timeoutMs)
{
    timeoutMs = timeoutMs.MustBeNonNegative(nameof(timeoutMs));
    
    int maxRetries = 3;
    maxRetries = maxRetries.MustBePositive(nameof(maxRetries));
}
```

### Available Guard Methods

- `T NotNull<T>(this T? value, string paramName)`: Throws `ArgumentNullException` if the value is null.
- `string NotNullOrEmpty(this string? value, string paramName)`: Throws `ArgumentNullException` or `ArgumentException` if the string is null or empty.
- `string NotNullOrWhiteSpace(this string? value, string paramName)`: Throws `ArgumentNullException` or `ArgumentException` if the string is null, empty, or whitespace.
- `Guid NotEmpty(this Guid value, string paramName)`: Throws `ArgumentException` if the Guid is empty.
- `int MustBePositive(this int value, string paramName)`: Throws `ArgumentException` if the integer is not positive.
- `int MustBeNonNegative(this int value, string paramName)`: Throws `ArgumentException` if the integer is negative.
- `IEnumerable<T> NotEmpty<T>(this IEnumerable<T>? collection, string paramName)`: Throws `ArgumentNullException` or `ArgumentException` if the collection is null or empty.
- `void MustBeTrue(this bool condition, string message)`: Throws `ArgumentNullException` or `ArgumentException` if the condition is false.
- `void MustBeFalse(this bool condition, string message)`: Throws `ArgumentNullException` or `ArgumentException` if the condition is true.

## ConcurrentCollectionExtensions

The `ConcurrentCollectionExtensions` class provides extension methods for concurrent collections (`ConcurrentDictionary` and `ConcurrentQueue`) to simplify common operations when working with thread-safe collections in the actor framework. These extensions reduce boilerplate code and provide thread-safe access patterns for managing collections in concurrent scenarios.

### Usage Example

```csharp
// Initialize concurrent collections for message processing
var messageQueue = new ConcurrentQueue<Message>();
var actorRegistry = new ConcurrentDictionary<Guid, ActorRef>();

// Enqueue multiple messages at once
var messages = new List<Message>
{
    new Message("process-order", new Dictionary<string, object> { { "orderId", "order-123" } }),
    new Message("process-payment", new Dictionary<string, object> { { "paymentId", "pay-456" } }),
    new Message("process-shipping", new Dictionary<string, object> { { "trackingId", "track-789" } })
};

messageQueue.EnqueueRange(messages);

// Get all messages from the queue
var allMessages = messageQueue.DequeueAll();
Console.WriteLine($"Processed {allMessages.Count} messages");

// Register actors in a thread-safe registry
var rootActor = new ActorRef(Guid.NewGuid(), new ActorPath("/root"));
var workerActor = new ActorRef(Guid.NewGuid(), new ActorPath("/root/worker"));

actorRegistry.TryAdd(rootActor.Id, rootActor);
actorRegistry.TryAdd(workerActor.Id, workerActor);

// Get all registered actors
var allActors = actorRegistry.GetAllValues().ToList();
Console.WriteLine($"Registered {allActors.Count} actors");

// Get actor by ID with default fallback
var unknownActor = actorRegistry.GetValueOrDefault(Guid.NewGuid(), rootActor);
Console.WriteLine($"Fallback actor: {unknownActor.Path}");

// Remove actors matching a predicate
int removedCount = actorRegistry.RemoveWhere((id, actor) => actor.Path.ToString().Contains("temp"));
Console.WriteLine($"Removed {removedCount} temporary actors");

// Get current collection sizes
int queueSize = messageQueue.GetCount();
int registrySize = actorRegistry.GetCount();
Console.WriteLine($"Queue size: {queueSize}, Registry size: {registrySize}");

// Clear collections when needed
messageQueue.Clear();
actorRegistry.ClearAll();
```

### Available Methods

- `IEnumerable<TValue> GetAllValues<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`: Gets all values from a concurrent dictionary.
- `IEnumerable<TKey> GetAllKeys<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`: Gets all keys from a concurrent dictionary.
- `TValue? GetValueOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue = default)`: Gets a value with a default fallback if not found.
- `int GetCount<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`: Gets the count of items in a concurrent dictionary.
- `void ClearAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`: Clears all items from a concurrent dictionary.
- `int RemoveWhere<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, Func<TKey, TValue, bool> predicate)`: Removes entries matching a predicate.
- `void EnqueueRange<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)`: Enqueues multiple items to a concurrent queue.
- `List<T> DequeueAll<T>(this ConcurrentQueue<T> queue)`: Dequeues all available items from a concurrent queue.
- `int GetCount<T>(this ConcurrentQueue<T> queue)`: Gets the current count of items in a concurrent queue.

## ActorPathTests

The `ActorPathTests` class provides unit tests for the `ActorPath` class, verifying that actor path construction, validation, hierarchy operations, and equality comparison work correctly. These tests ensure that actor paths are properly validated, can form hierarchical relationships, correctly identify parent-child relationships, and maintain proper equality semantics throughout the actor system.

### Usage Example

```csharp
// Test valid nested path construction and properties
var path = new ActorPath("/system/workers/processor");

// Verify path properties
Console.WriteLine($"Path: {path.Path}");
Console.WriteLine($"Name: {path.Name}");
Console.WriteLine($"Segments: {string.Join("/", path.Segments)}");
Console.WriteLine($"Depth: {path.GetDepth()}");
Console.WriteLine($"Parent: {path.Parent?.Path}");

// Test path validation with exception handling
try
{
    var invalidPath = new ActorPath("no-leading-slash");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
}

// Test child path creation
var parent = new ActorPath("/system/workers");
var child = parent.GetChild("processor");
Console.WriteLine($"Child path: {child.Path}");

// Test hierarchy relationships
var root = new ActorPath("/system");
var nested = new ActorPath("/system/workers/processor");
var unrelated = new ActorPath("/monitoring");

Console.WriteLine($"Nested is descendant of root: {nested.IsDescendantOf(root)}");
Console.WriteLine($"Unrelated is descendant of root: {unrelated.IsDescendantOf(root)}");
Console.WriteLine($"Root is descendant of nested: {root.IsDescendantOf(nested)}");

// Test equality
var path1 = new ActorPath("/system/workers");
var path2 = new ActorPath("/system/workers");
Console.WriteLine($"Paths are equal: {path1 == path2}");
Console.WriteLine($"Hash codes match: {path1.GetHashCode() == path2.GetHashCode()}");

// Test IsChildOf extension method
Console.WriteLine($"Direct child is child of parent: {child.IsChildOf(parent)}");
Console.WriteLine($"Deep descendant is child of parent: {nested.IsChildOf(root)}");
Console.WriteLine($"Parent is child of child: {parent.IsChildOf(child)}");
```

### Test Methods

- `Constructor_WithValidNestedPath_SetsNameSegmentsAndDepth`: Verifies that constructing an `ActorPath` with a valid nested path correctly sets the name, segments, depth, and parent path.

- `Constructor_WithNullOrWhitespacePath_ThrowsArgumentException`: Validates that constructing an `ActorPath` with a null or whitespace string throws an `ArgumentException`.

- `Constructor_WithInvalidPathFormat_ThrowsWithDescriptiveMessage`: Ensures that constructing an `ActorPath` with an invalid path format throws an `ArgumentException` with a descriptive message.

- `GetChild_WithValidChildName_BuildsCorrectHierarchy`: Verifies that `ActorPath.GetChild(string)` correctly builds a child path and sets the appropriate properties.

- `IsDescendantOf_WhenPathNested_ReturnsTrue_AndSiblingReturnsFalse`: Validates that `ActorPath.IsDescendantOf(ActorPath)` correctly identifies descendant relationships and distinguishes siblings.

- `Equality_WithIdenticalPathStrings_PathsAreEqual`: Verifies that two `ActorPath` instances with identical path strings are considered equal and have the same hash code.

- `IsChildOf_ExtensionMethod_ReturnsTrueForAnyDescendant`: Verifies that the `ActorPath.IsChildOf(ActorPath)` extension method correctly identifies any descendant as a child and does not consider a parent as a child.

## ActorPathExtensions

The `ActorPathExtensions` class provides utility methods for working with `ActorPath` objects, enabling common operations like hierarchy traversal, parent-child relationship checks, and relative path calculation. These extensions simplify path manipulation throughout the actor system and are essential for supervision hierarchies, actor discovery, and message routing.

### Usage Example

```csharp
// Create actor paths in a hierarchy
var rootPath = new ActorPath("/root");
var userPath = new ActorPath("/root/users");
var user1Path = new ActorPath("/root/users/user1");
var orderPath = new ActorPath("/root/orders/order123");

// Check parent-child relationships
bool isChild = user1Path.IsChildOf(rootPath);      // true
bool isChild2 = orderPath.IsChildOf(rootPath);      // true
bool isChild3 = user1Path.IsChildOf(userPath);      // true
bool isChild4 = user1Path.IsChildOf(orderPath);      // false

// Get relative paths
string? relativeToRoot = user1Path.GetRelativePath(rootPath);      // "users/user1"
string? relativeToUsers = user1Path.GetRelativePath(userPath);      // "user1"
string? relativeToOrders = user1Path.GetRelativePath(orderPath);     // null (not a descendant)

// Get all ancestors including current path
var ancestors = user1Path.GetAncestors().ToList();
Console.WriteLine($"Ancestors count: {ancestors.Count}");  // 3: /root/users/user1, /root/users, /root

// Check if paths are in hierarchy
bool isDescendant = user1Path.IsDescendantOf(rootPath);  // true
bool isDescendant2 = rootPath.IsDescendantOf(user1Path); // false
```

### Available Methods

- `string? GetRelativePath(ActorPath parent)`: Gets the relative path from a parent actor to this actor. Returns null if this path is not a descendant of the specified parent.
- `IEnumerable<ActorPath> GetAncestors()`: Gets all ancestor paths from this path to the root, including the current path itself.
- `bool IsChildOf(ActorPath parent)`: Determines if this path is a child of the specified parent path.
- `bool IsDescendantOf(ActorPath ancestor)`: Determines if this path is a descendant of the specified ancestor path.

## MessageBatcher

The `MessageBatcher` class provides efficient message batching functionality for grouping messages together to reduce processing overhead and improve throughput. It groups messages by a batch key (such as message type or destination) and processes them in configurable batch sizes or after a timeout period. The batcher automatically flushes batches when they reach capacity or when the timeout expires, with optional event notification for expired batches.

### Usage Example

```csharp
// Initialize a message batcher with batch size of 50 messages and 3-second timeout
var batcher = new MessageBatcher(batchSize: 50, TimeSpan.FromSeconds(3));

// Subscribe to batch expiration events (optional)
batcher.BatchExpired += (batchKey, messages) => 
{
    Console.WriteLine($"Batch '{batchKey}' expired with {messages.Count} messages");
    // Process the batch of messages
    foreach (var message in messages)
    {
        Console.WriteLine($"Processing message: {message.Command}");
    }
};

// Add messages to batches
var message1 = new Message("process-order", new Dictionary<string, object> { { "orderId", "order-123" } });
var message2 = new Message("process-order", new Dictionary<string, object> { { "orderId", "order-456" } });

// Messages with the same batch key are grouped together
var batch1 = batcher.AddMessage("order-processing", message1);
var batch2 = batcher.AddMessage("order-processing", message2);

// When batch reaches capacity (50 messages), AddMessage returns the batch contents
if (batch1 != null)
{
    Console.WriteLine($"Batch completed with {batch1.Count()} messages");
}

// Flush a specific batch manually
var flushedMessages = batcher.FlushBatch("order-processing");
if (flushedMessages != null)
{
    Console.WriteLine($"Manually flushed batch with {flushedMessages.Count()} messages");
}

// Flush all batches
var allBatches = batcher.FlushAll();
foreach (var kvp in allBatches)
{
    Console.WriteLine($"Flushed batch '{kvp.Key}' with {kvp.Value.Count()} messages");
}

// Dispose when done to clean up resources
batcher.Dispose();
```

### Properties and Methods

- `MessageBatcher(int batchSize = 100, TimeSpan? batchTimeout = null)`: Initializes a new message batcher with configurable batch size and timeout.
- `IEnumerable<Message>? AddMessage(string batchKey, Message message)`: Adds a message to a batch. Returns the batch contents when the batch is full, or null while the batch is still filling.
- `IEnumerable<Message>? FlushBatch(string batchKey)`: Flushes all pending messages in a specific batch. Returns the messages if any were flushed.
- `Dictionary<string, IEnumerable<Message>> FlushAll()`: Flushes all batches and returns a dictionary of batch keys to their message collections.
- `void Dispose()`: Disposes the batcher and cleans up resources.
- `event Action<string, IReadOnlyList<Message>>? BatchExpired`: Event raised when a batch exceeds the batch timeout (optional notification).

### Batch Behavior

- Messages are grouped by their batch key
- Batches automatically flush when they reach the configured batch size
- Batches also flush when the batch timeout expires (default: 5 seconds)
- Expired batches trigger the `BatchExpired` event if a handler is attached
- If no handler is attached to `BatchExpired`, batches are retained until filled or explicitly flushed to prevent message loss

## SerializationExtensions

The `SerializationExtensions` class provides utility methods for serializing and deserializing objects to and from JSON format. It offers convenient extension methods for converting objects to JSON strings, byte arrays, and back to objects with proper error handling and validation. These utilities are particularly useful for message serialization, state persistence, and data transfer scenarios within the actor framework.

### Usage Example

```csharp
// Create a sample data object to serialize
var order = new
{
    OrderId = "ORD-12345",
    CustomerId = "CUST-789",
    Items = new[] { "Laptop", "Mouse", "Keyboard" },
    TotalAmount = 2999.99,
    IsPriority = true
};

// Serialize to compact JSON string
string jsonCompact = order.ToJson();
Console.WriteLine(jsonCompact);

// Serialize to pretty-printed JSON string
string jsonPretty = order.ToJsonPretty();
Console.WriteLine(jsonPretty);

// Serialize to JSON bytes
byte[] jsonBytes = order.ToJsonBytes();
Console.WriteLine($"Serialized to {jsonBytes.Length} bytes");

// Deserialize from JSON string
var deserializedOrder = jsonCompact.FromJson<Order>();
Console.WriteLine($"Deserialized order: {deserializedOrder?.OrderId}");

// Deserialize from JSON bytes
var orderFromBytes = jsonBytes.FromJsonBytes<Order>();
Console.WriteLine($"Deserialized from bytes: {orderFromBytes?.OrderId}");

// Validate JSON before deserialization
bool isValid = jsonCompact.IsValidJson();
Console.WriteLine($"Is valid JSON: {isValid}");

// Create a deep copy of an object
var orderCopy = order.DeepCopy();
Console.WriteLine($"Deep copy created: {orderCopy?.OrderId}");

// Try deserialization with error handling
if (jsonCompact.TryFromJson(out Order? result, out string? error))
{
    Console.WriteLine($"Successfully deserialized: {result?.OrderId}");
}
else
{
    Console.WriteLine($"Deserialization failed: {error}");
}
```

### Available Methods

- `string ToJson<T>(this T obj)`: Serializes an object to a compact JSON string. Returns "null" for null objects.
- `string ToJsonPretty<T>(this T obj)`: Serializes an object to a pretty-printed JSON string. Returns "null" for null objects.
- `byte[] ToJsonBytes<T>(this T obj)`: Serializes an object to UTF-8 encoded JSON bytes. Returns an empty array for null objects.
- `T? FromJson<T>(this string json)`: Deserializes a JSON string to an object. Returns null if deserialization fails or input is null/empty.
- `T? FromJsonBytes<T>(this byte[] data)`: Deserializes JSON bytes to an object. Returns null if deserialization fails or input is null/empty.
- `bool TryFromJson<T>(this string json, out T? result, out string? error)`: Attempts to deserialize JSON with error information. Returns true if successful.
- `bool IsValidJson(this string json)`: Determines if a string is valid JSON.
- `T? DeepCopy<T>(this T obj)`: Creates a deep copy of an object by serializing and deserializing it.

## ProcessorActor

The `ProcessorActor` class processes control messages with configurable delays and integrates with the metrics collection system. It serves as a worker actor that handles processing tasks while the `MonitorActor` tracks system health metrics. The processor demonstrates how to implement custom actor behavior with lifecycle management and metrics integration.

### Usage Example

```csharp
// Create an actor system with metrics enabled
var services = new ServiceCollection();
services.AddActorFramework(options =>
{
    options.SystemName = "ProcessingSystem";
    options.EnableMetricsCollection = true;
});

var sp = services.BuildServiceProvider();
var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
var system = await config.InitializeAsync();

// Create a processor actor
var processorPath = new ActorPath("/user/data-processor");
var processorRef = await config.CreateActorAsync(processorPath);

// Send a control message to process with a delay
var processMessage = new ControlMessage("process",
    new Dictionary<string, object> { { "delay", 250 } });
await config.SendMessageAsync(processorRef, processorRef, processMessage);

// Create a monitor actor to track system metrics
var monitorPath = new ActorPath("/user/monitor");
var monitorRef = await config.CreateActorAsync(monitorPath);

// Let the system run for monitoring
await Task.Delay(10000);

// Shutdown
await system.ShutdownAsync();
```

### Key Members

- `ProcessorActor(ActorPath path)`: Initializes a new processor actor with the specified hierarchical path.
- `ReceiveAsync(Message message)`: Processes incoming control messages, particularly the "process" command with configurable delays.

### Behavior

The `ProcessorActor` is designed to handle processing tasks within a monitored actor system:

- Receives `ControlMessage` with command "process" and processes them with configurable delays
- Integrates with the metrics system via `Metrics.RecordSuccess()` to track successful operations
- Works alongside `MonitorActor` for system health monitoring and metrics reporting
- Demonstrates actor lifecycle integration with the metrics collection system

Processor actors are typically used in scenarios where you need to process tasks while simultaneously monitoring system health and performance metrics.


## Architecture


See [docs/architecture.md](docs/architecture.md) for the full picture: module
breakdown, message flow, concurrency model, design decisions with their
trade-offs, extension points, and an honest list of current limitations.

Short version: `MessageDispatcher` wraps messages in envelopes and enqueues
them into bounded per-actor mailboxes (`MailboxService`); the host pulls
envelopes and invokes `Actor.ReceiveAsync`; failures are routed to
`SupervisionService` (restart / stop / resume / escalate / backoff). Everything
below is API-level reference for the individual types.

## MailboxService

The `MailboxService` class manages message mailboxes for actors in the actor system. Each actor has a bounded FIFO mailbox where messages are queued and processed sequentially by the actor's `ReceiveAsync` method. The service supports both standard FIFO mailboxes and priority-based mailboxes, with configurable capacity and automatic backpressure when mailboxes are full.

### Usage Example

```csharp
// Initialize the actor system with mailbox configuration
var options = new ActorSystemOptions
{
    DefaultMailboxCapacity = 1000,
    DefaultMailboxType = MailboxType.Standard
};

var actorSystem = new ActorSystem(options);
var mailboxService = new MailboxService(options);

// Create an actor
var actorId = Guid.NewGuid();
var actorPath = new ActorPath("/user/order-processor");

// Create a mailbox for the actor with custom capacity
var mailbox = mailboxService.CreateMailbox(actorId, capacity: 500);

// Enqueue messages to the actor
for (int i = 0; i < 10; i++)
{
    var message = new Message("process-order", new Dictionary<string, object>
    {
        { "orderId", $"order-{i}" },
        { "amount", i * 10.5 }
    });
    
    var envelope = new Envelope(message, actorPath);
    await mailboxService.EnqueueAsync(actorId, envelope);
}

// Check mailbox status
Console.WriteLine($"Mailbox size: {mailboxService.GetMailboxSize(actorId)}");
Console.WriteLine($"Is mailbox full: {mailboxService.IsMailboxFull(actorId)}");
Console.WriteLine($"Mailbox load factor: {mailbox.GetLoadFactor():F2}");

// Dequeue and process messages
while (await mailboxService.DequeueAsync(actorId) is { } envelope)
{
    Console.WriteLine($"Processing message: {envelope.Message.Command}");
    // Process the message...
}

// Get system-wide mailbox statistics
var stats = mailboxService.GetStatistics();
Console.WriteLine($"Total mailboxes: {stats.TotalMailboxes}");
Console.WriteLine($"Total messages: {stats.TotalMessages}");
Console.WriteLine($"Average load factor: {stats.AverageLoadFactor:F2}");

// Clean up when actor is terminated
mailboxService.RemoveMailbox(actorId);
```

### Properties and Methods

- `MailboxService(ActorSystemOptions options)`: Initializes a new mailbox service with the specified configuration.
- `IMailbox CreateMailbox(Guid actorId, int capacity = 0, MailboxType? mailboxType = null)`: Creates a new mailbox for an actor with optional custom capacity and mailbox type.
- `IMailbox? GetMailbox(Guid actorId)`: Gets the mailbox for an actor, or null if it doesn't exist.
- `Task EnqueueAsync(Guid actorId, Envelope envelope)`: Enqueues a message into an actor's mailbox.
- `Task<Envelope?> DequeueAsync(Guid actorId)`: Dequeues the next message from an actor's mailbox.
- `int GetMailboxSize(Guid actorId)`: Gets the number of messages in an actor's mailbox.
- `bool IsMailboxFull(Guid actorId)`: Checks if an actor's mailbox is full.
- `void RemoveMailbox(Guid actorId)`: Removes a mailbox for an actor.
- `MailboxStatistics GetStatistics()`: Gets statistics about all mailboxes in the system.
- `void Clear()`: Clears all mailboxes in the system.

### IMailbox Interface

The `IMailbox` interface defines the contract for actor mailboxes:

- `Guid ActorId { get; }`: Gets the unique identifier of the actor.
- `int Capacity { get; }`: Gets the capacity of the mailbox.
- `Task<bool> EnqueueAsync(Envelope envelope)`: Attempts to enqueue a message into the mailbox.
- `Task<Envelope?> DequeueAsync()`: Attempts to dequeue the next message from the mailbox.
- `int GetSize()`: Gets the current number of messages in the mailbox.
- `bool IsFull { get; }`: Gets whether the mailbox is full.
- `double GetLoadFactor()`: Gets the load factor of the mailbox (0-1).
- `void Dispose()`: Disposes the mailbox resources.

### MailboxStatistics Class

The `MailboxStatistics` class provides aggregated statistics about all mailboxes:

- `int TotalMailboxes { get; set; }`: Gets the total number of mailboxes.
- `long TotalMessages { get; set; }`: Gets the total number of messages across all mailboxes.
- `long TotalCapacity { get; set; }`: Gets the total capacity across all mailboxes.
- `int FullMailboxes { get; set; }`: Gets the number of mailboxes that are full.
- `double AverageLoadFactor { get; set; }`: Gets the average load factor across all mailboxes.

## ActorMetrics

The `ActorMetrics` class tracks performance and behavior metrics for an actor, providing insights into its message processing and error rates.

### Usage Example

```csharp
var metrics = new ActorMetrics(Guid.NewGuid(), new ActorPath("parent", "child"));
metrics.RecordMessageReceived();
metrics.RecordProcessingTime(100);
metrics.RecordError();

Console.WriteLine($"ActorId: {metrics.ActorId}, ActorPath: {metrics.ActorPath}, MessageCount: {metrics.MessageCount}, ErrorCount: {metrics.ErrorCount}");
Console.WriteLine($"ProcessedCount: {metrics.ProcessedCount}, AverageProcessingTimeMs: {metrics.AverageProcessingTimeMs}, CreatedAt: {metrics.CreatedAt}");
Console.WriteLine($"LastMessageTime: {metrics.LastMessageTime}, MailboxDepth: {metrics.MailboxDepth}");
Console.WriteLine($"ErrorRate: {metrics.GetErrorRate()}, SuccessRate: {metrics.GetSuccessRate()}, Uptime: {metrics.GetUptime()}");
Console.WriteLine($"IsUnhealthy: {metrics.IsUnhealthy()}, Summary: {metrics.GetSummary()}");
```

### Properties and Methods

- `Guid ActorId { get; }`: Gets the unique identifier of the actor.
- `ActorPath ActorPath { get; }`: Gets the path of the referenced actor.
- `long MessageCount { get; private set; }`: Gets the total number of messages processed by the actor.
- `long ErrorCount { get; private set; }`: Gets the total number of errors encountered by the actor.
- `long ProcessedCount { get; private set; }`: Gets the total number of messages processed by the actor.
- `double AverageProcessingTimeMs { get; private set; }`: Gets the average processing time of messages in milliseconds.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when the actor reference was created.
- `DateTime? LastMessageTime { get; private set; }`: Gets the UTC timestamp of the last message received by the actor.
- `int MailboxDepth { get; private set; }`: Gets the current number of messages waiting in the actor's mailbox.
- `void RecordMessageReceived()`: Records that a message was received.
- `void RecordProcessingTime(long elapsedMilliseconds)`: Records the processing time of a message in milliseconds.
- `void RecordError()`: Records that an error occurred processing a message.
- `void UpdateMailboxDepth(int depth)`: Updates the current mailbox depth snapshot.
- `double GetErrorRate()`: Gets the error rate as a percentage.
- `double GetSuccessRate()`: Gets the success rate as a percentage.
- `TimeSpan GetUptime()`: Gets the total uptime since creation.
- `bool IsUnhealthy(double errorRateThreshold = 0.25)`: Checks if the actor is experiencing high error rates.
- `ActorMetricsSummary GetSummary()`: Gets a summary of the metrics.

## ActorMetricsTests

The `ActorMetricsTests` class provides unit tests for the `ActorMetrics` class, verifying that metrics tracking functionality works correctly. It tests message counting, error rate calculation, processing time averaging, health status determination, and summary generation to ensure the metrics system operates as expected.

### Usage Example

```csharp
// Initialize test metrics
var metrics = new ActorMetrics(Guid.NewGuid(), new ActorPath("/system/test-actor"));

// Test message counting
metrics.RecordMessageReceived();
metrics.RecordMessageReceived();
Assert.Equal(2, metrics.MessageCount);

// Test error rate calculation
metrics.RecordError();
double errorRate = metrics.GetErrorRate();
Assert.Equal(33.33, errorRate, 0.01); // 1 error out of 3 messages = 33.33%

// Test processing time averaging
metrics.RecordProcessingTime(100);
metrics.RecordProcessingTime(200);
metrics.RecordProcessingTime(300);
Assert.Equal(200.0, metrics.AverageProcessingTimeMs); // (100+200+300)/3 = 200ms

// Test health status
bool isUnhealthy = metrics.IsUnhealthy(25.0); // Error rate 33.33% > 25% threshold
Assert.True(isUnhealthy);

// Test summary generation
var summary = metrics.GetSummary();
Assert.Equal(3, summary.MessageCount);
Assert.Equal(1, summary.ErrorCount);
```

## MessageDispatcher

The `MessageDispatcher` class handles message delivery and routing between actors in the DotNetActorFramework. It wraps messages in envelopes, manages delivery guarantees with retry logic and backoff strategies, and tracks failed deliveries in a dead letter queue. The dispatcher works in conjunction with the `MailboxService` to enqueue messages to bounded per-actor mailboxes and provides comprehensive statistics about message delivery performance.

### Usage Example

```csharp
// Initialize the actor system and dispatcher
var actorSystem = new ActorSystem("DispatcherDemoSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Create a message dispatcher with mailbox service and registry
var mailboxService = new MailboxService(new ActorSystemOptions());
var registry = new ActorRegistry();
registry.Register(rootActor);
registry.Register(workerActor);

var dispatcher = new MessageDispatcher(mailboxService, registry);

// Send a message from one actor to another
var orderMessage = new Message("process-order", new Dictionary<string, object>
{
    { "orderId", "order-123" },
    { "amount", 99.99 }
});

await dispatcher.SendAsync(rootActor, workerActor, orderMessage);

// Broadcast a message to multiple actors
var recipients = new List<ActorRef> { workerActor, rootActor };
var notification = new Message("system-notification", new Dictionary<string, object>
{
    { "message", "System maintenance scheduled" }
});

await dispatcher.BroadcastAsync(recipients, notification);

// Publish a control message
await dispatcher.PublishControlAsync(workerActor, "shutdown", new Dictionary<string, object>
{
    { "graceful", true },
    { "delayMs", 5000 }
});

// Get statistics about message delivery
var stats = dispatcher.GetStatistics();
Console.WriteLine($"Messages delivered: {stats.TotalDelivered}");
Console.WriteLine($"Messages failed: {stats.TotalFailed}");
Console.WriteLine($"Success rate: {stats.SuccessRate:F2}%");
Console.WriteLine($"Dead letters: {stats.DeadLetterCount}");

// Retrieve dead letters for investigation
var deadLetters = dispatcher.GetDeadLetters();
foreach (var envelope in deadLetters)
{
    Console.WriteLine($"Dead letter from: {envelope.Sender?.Path}, to: {envelope.Recipient.Path}");
}

// Get next message for an actor to process
var nextMessage = await dispatcher.GetNextMessageAsync(workerActor.Id);
if (nextMessage != null)
{
    Console.WriteLine($"Processing message: {nextMessage.Message.Command}");
}
```

### Properties and Methods

- `MessageDispatcher(MailboxService mailboxService, ActorRegistry registry)`: Initializes a new message dispatcher with the specified mailbox service and actor registry.
- `Task<bool> DispatchAsync(Envelope envelope)`: Dispatches a message to an actor asynchronously with retry logic and returns true if successful.
- `Task SendAsync(ActorRef sender, ActorRef recipient, Message message)`: Sends a message from one actor to another.
- `Task SendAsync(ActorRef recipient, Message message)`: Sends a message to an actor without a sender.
- `Task BroadcastAsync(IEnumerable<ActorRef> recipients, Message message, ActorRef? sender = null)`: Broadcasts a message to multiple actors.
- `Task PublishControlAsync(ActorRef recipient, string command, Dictionary<string, object>? parameters = null)`: Publishes a control message to an actor.
- `Task<Envelope?> GetNextMessageAsync(Guid actorId)`: Gets the next message for an actor to process from its mailbox.
- `IReadOnlyList<Envelope> GetDeadLetters()`: Gets messages from the dead letter queue for investigation.
- `DispatcherStatistics GetStatistics()`: Gets dispatcher statistics including delivery counts and success rate.

### DispatcherStatistics Class

The `DispatcherStatistics` class provides statistics about message dispatch operations.

#### Properties

- `long TotalDelivered { get; set; }`: Gets the total number of messages successfully delivered.
- `long TotalFailed { get; set; }`: Gets the total number of messages that failed to deliver.
- `long TotalProcessed { get; set; }`: Gets the total number of messages processed (delivered + failed).
- `int DeadLetterCount { get; set; }`: Gets the number of messages in the dead letter queue.
- `double SuccessRate { get; set; }`: Gets the success rate as a percentage.

## CoreFunctionalityTests

The `CoreFunctionalityTests` class provides unit tests for foundational functionality of the DotNetActorFramework. It verifies core components like actor registry operations, mailbox service behavior, and basic message handling to ensure the framework operates correctly at its most fundamental level. These tests serve as both validation of core functionality and documentation of expected behavior for framework users.

### Usage Example

```csharp
// Initialize the actor registry and test actor operations
var registry = new ActorRegistry();

// Create and register an actor
var actorPath = new ActorPath("/system/test-actor");
var actorRef = new ActorRef(actorPath, Guid.NewGuid());
registry.Register(actorRef);

// Test actor registry operations
var retrievedActor = registry.GetByPath(actorPath);
Console.WriteLine($"Actor retrieved: {retrievedActor != null}");
Console.WriteLine($"Registry contains actor: {registry.Contains(actorPath)}");

// Test mailbox service operations
var options = new ActorSystemOptions { DefaultMailboxCapacity = 10 };
var mailboxService = new MailboxService(options);
var actorId = Guid.NewGuid();
var mailbox = mailboxService.CreateMailbox(actorId);

// Create and enqueue a message
await mailboxService.EnqueueAsync(actorId, 
    new Envelope(new Message<string>("test-data") { Priority = 1 }, actorRef));
Console.WriteLine($"Mailbox size: {mailboxService.GetMailboxSize(actorId)}");

// Dequeue the message
var dequeued = await mailboxService.DequeueAsync(actorId);
Console.WriteLine($"Message dequeued: {dequeued != null}");

// Clear the registry when done
registry.Clear();
Console.WriteLine($"Registry cleared. Count: {registry.GetCount()}");
```

### Test Methods

- `ActorRegistry_RegisterAndGet_ShouldReturnCorrectActor`: Verifies that actors can be registered and retrieved from the registry by their path.
- `ActorRegistry_Clear_ShouldRemoveAllActors`: Ensures that clearing the registry removes all registered actors.
- `MailboxService_CreateAndEnqueue_ShouldHoldMessage`: Tests that creating and enqueueing a message to a mailbox properly stores the message.
- `MailboxService_EnqueueAndDequeue_ShouldReturnSameMessage`: Validates that messages can be enqueued and dequeued, returning the same message.
- `MailboxService_EnqueueToFullMailbox_ShouldFail`: Confirms that attempting to enqueue to a full mailbox fails appropriately.

## MiddlewarePipelineTests

The `MiddlewarePipelineTests` class provides unit tests for the `MiddlewarePipeline` class and related message types (`ControlMessage`, `FailureMessage`) in the DotNetActorFramework. These tests verify that middleware registration, execution, and error handling work correctly, ensuring that the middleware pipeline properly processes messages and handles exceptions according to the framework's specifications.

### Usage Example

```csharp
// Initialize a middleware pipeline for testing
var pipeline = new MiddlewarePipeline();

// Create an actor reference for testing
var actorPath = new ActorPath("/system/test-actor");
var actorRef = new ActorRef(actorPath, Guid.NewGuid());

// Create an envelope with a control message
var controlMessage = new ControlMessage("start-processing");
var envelope = new Envelope(controlMessage, actorRef);

// Register middleware with the pipeline
var auditMiddleware = new AuditMiddleware();
pipeline.Register(auditMiddleware);

// Execute the middleware pipeline with a final handler
var executionResult = await pipeline.ExecuteAsync(envelope, async e =>
{
    Console.WriteLine($"Final handler processing message: {((ControlMessage)e.Message).Command}");
    await Task.CompletedTask;
});

Console.WriteLine($"Pipeline execution successful: {executionResult}");

// Test error handling with middleware that throws
var errorMiddleware = new ErrorThrowingMiddleware();
pipeline.Register(errorMiddleware);

var errorResult = await pipeline.ExecuteAsync(envelope, _ => Task.CompletedTask);
Console.WriteLine($"Error handling successful: {errorResult} (false indicates middleware exception was caught)");

// Test null envelope validation
try
{
    await pipeline.ExecuteAsync(null!, _ => Task.CompletedTask);
}
catch (ArgumentNullException ex)
{
    Console.WriteLine($"Null envelope validation works: {ex.ParamName == "envelope"}");
}

// Test message type validation
try
{
    var invalidMessage = new ControlMessage(string.Empty);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Empty command validation works: {ex.Message.Contains("Command cannot be null or empty")}");
}
```

### Test Methods

- `Register_WithNullMiddleware_ThrowsArgumentNullException`: Verifies that registering null middleware throws an `ArgumentNullException`.
- `Register_WithMockedMiddleware_MiddlewareAppearsInGetMiddleware`: Tests that registered middleware appears in the middleware list with correct name and order.
- `ExecuteAsync_WithNoMiddleware_InvokesFinalHandlerAndReturnsTrue`: Validates that executing a pipeline with no middleware invokes the final handler and returns true.
- `ExecuteAsync_WhenRegisteredMiddlewareThrows_ReturnsFalse`: Confirms that when middleware throws an exception, the pipeline returns false to indicate failure.
- `ExecuteAsync_WithNullEnvelope_ThrowsArgumentNullException`: Tests that passing a null envelope throws an `ArgumentNullException`.
- `ControlMessage_WithEmptyCommand_ThrowsArgumentException`: Validates that creating a `ControlMessage` with an empty command throws an `ArgumentException`.
- `ControlMessage_WithValidCommand_StoresCommandAndDefaultsParameters`: Ensures that a valid `ControlMessage` stores the command and has default parameters.
- `FailureMessage_WithValidReasonAndException_StoresReasonAndStackTrace`: Tests that a `FailureMessage` correctly stores the failure reason and exception stack trace.

## ClusterActorRegistry

The `ClusterActorRegistry` class manages actor references across different nodes in a cluster. It tracks which actors are hosted on which cluster nodes, enabling distributed actor discovery and management. This registry is essential for cluster-aware actor systems where actors can be distributed across multiple nodes and need to be discovered dynamically.

### Usage Example

```csharp
// Initialize the cluster actor registry
var registry = new ClusterActorRegistry();

// Create actors in the actor system
var actorSystem = new ActorSystem("ClusterDemoSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Register actors on specific cluster nodes (using node addresses as identifiers)
// In a real cluster, nodeAddress would be the actual node identifier like "node1.example.com:1234"
registry.RegisterActor("node1.example.com:1234", rootActor);
registry.RegisterActor("node2.example.com:5678", workerActor);

// Retrieve actors by node address
var actorsOnNode1 = registry.GetActorsByNode("node1.example.com:1234");
Console.WriteLine($"Actors on node1: {actorsOnNode1.Count()}");

var actorsOnNode2 = registry.GetActorsByNode("node2.example.com:5678");
Console.WriteLine($"Actors on node2: {actorsOnNode2.Count()}");

// Get all registered node addresses
var allNodes = registry.GetAllNodeAddresses();
Console.WriteLine($"Total nodes in cluster: {registry.GetNodeCount()}");
foreach (var node in allNodes)
{
    Console.WriteLine($"  - {node}");
}

// Unregister an actor from a node when it terminates
registry.UnregisterActor("node2.example.com:5678", workerActor);

// Remove a node when it disconnects from the cluster
registry.RemoveNode("node1.example.com:1234");

// Get updated node count
Console.WriteLine($"Nodes remaining: {registry.GetNodeCount()}");
```

### Properties and Methods

- `ClusterActorRegistry(ILogger<ClusterActorRegistry>? logger = null)`: Initializes a new cluster actor registry with optional logging.
- `void RegisterActor(string nodeAddress, ActorRef actorRef)`: Registers an actor as being hosted on a specific cluster node.
- `bool UnregisterActor(string nodeAddress, ActorRef actorRef)`: Unregisters a specific actor from a cluster node. Returns true if the actor was found and removed.
- `bool RemoveNode(string nodeAddress)`: Removes all actors associated with a disconnected or unreachable cluster node. Returns true if the node was found and removed.
- `IEnumerable<ActorRef> GetActorsByNode(string nodeAddress)`: Retrieves all actors registered on a specific cluster node.
- `IEnumerable<string> GetAllNodeAddresses()`: Gets all registered node addresses.
- `int GetNodeCount()`: Gets the total count of registered nodes.

## ActorRegistry

The `ActorRegistry` class serves as the central registry for managing actor registrations, lookups, and hierarchy indexing within the system. It provides thread-safe mechanisms to register, retrieve, and terminate actors based on their path or ID, enabling efficient actor discovery and management across the actor system.

### Usage Example

```csharp
// Initialize the actor system and registry
var actorSystem = new ActorSystem("RegistryDemoSystem");
var registry = new ActorRegistry();

// Create actors in the hierarchy
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("root", ""));
registry.Register(rootActor);

var userActor = await actorSystem.CreateActorAsync(new ActorPath("users", "user1"), rootActor);
registry.Register(userActor);

var orderActor = await actorSystem.CreateActorAsync(new ActorPath("orders", "order123"), rootActor);
registry.Register(orderActor);

// Retrieve actors by path
var retrievedUser = registry.GetByPath(new ActorPath("users", "user1"));
Console.WriteLine($"Found user actor: {retrievedUser?.Path}");

// Retrieve actors by ID
var userById = registry.GetById(userActor.Id);
Console.WriteLine($"Found user by ID: {userById?.Path}");

// Get child actors
var rootChildren = registry.GetChildren(new ActorPath("root", ""));
Console.WriteLine($"Root has {rootChildren.Count} children");

// Get descendant actors
var rootDescendants = registry.GetDescendants(new ActorPath("root", ""));
Console.WriteLine($"Root has {rootDescendants.Count} descendants");

// Get all registered actors
var allActors = registry.GetAll();
Console.WriteLine($"Total registered actors: {allActors.Count}");

// Check if actor exists
bool containsUser = registry.Contains(new ActorPath("users", "user1"));
Console.WriteLine($"Registry contains user actor: {containsUser}");

// Get actor count
int actorCount = registry.GetCount();
Console.WriteLine($"Total actors registered: {actorCount}");

// Remove an actor from registry
registry.Unregister(userActor);
Console.WriteLine($"Actors after unregister: {registry.GetCount()}");

// Clear the entire registry
registry.Clear();
Console.WriteLine($"Actors after clear: {registry.GetCount()}");
```

### Properties and Methods

- `void Register(ActorRef actorRef)`: Registers an actor in the registry and updates the hierarchy index.
- `void Unregister(ActorRef actorRef)`: Unregisters an actor from the registry.
- `ActorRef? GetByPath(ActorPath path)`: Gets an actor reference by its path.
- `ActorRef? GetById(Guid id)`: Gets an actor reference by its ID.
- `IReadOnlyList<ActorRef> GetChildren(ActorPath parentPath)`: Gets all child actors for a given parent path.
- `IReadOnlyList<ActorRef> GetDescendants(ActorPath parentPath)`: Gets all descendant actors for a given parent path.
- `IReadOnlyList<ActorRef> GetAll()`: Gets all registered actors.
- `bool Contains(ActorPath path)`: Checks if an actor is registered.
- `int GetCount()`: Gets the total number of registered actors.
- `void Clear()`: Clears all registrations.

## ActorStateRepository

The `ActorStateRepository` class provides functionality for persisting and retrieving actor state snapshots. It enables reliable state management for actors by allowing developers to save, load, delete, and inspect state snapshots associated with specific actor paths and IDs.

### Usage Example

```csharp
// Assuming 'repository' is an instance of ActorStateRepository
var actorId = Guid.NewGuid();
var actorPath = ActorPath.Parse("/my/actor");
var state = new Dictionary<string, object> { { "balance", 100.0 } };

// Save actor state
await repository.SaveStateAsync(actorId, actorPath, state, 1);

// Check if state exists
bool exists = await repository.HasState(actorId, actorPath);

// Load actor state
var loadedState = await repository.LoadStateAsync(actorId, actorPath);

// Retrieve a state snapshot
var snapshot = await repository.GetSnapshotAsync(actorId, actorPath);
if (snapshot != null)
{
    Console.WriteLine($"Snapshot loaded. Sequence: {snapshot.SequenceNr}, SavedAt: {snapshot.SavedAt}");
}

// Delete actor state
await repository.DeleteStateAsync(actorId, actorPath);
```

### Properties and Methods

- `Guid ActorId { get; }`: Gets the unique identifier of the actor associated with this repository instance.
- `ActorPath ActorPath { get; }`: Gets the path of the referenced actor.
- `object State { get; }`: Gets the current state stored in this repository.
- `DateTime SavedAt { get; }`: Gets the timestamp when the state was last saved.
- `long SequenceNr { get; }`: Gets the sequence number for state persistence.
- `int Version { get; }`: Gets the version number of the state.
- `Task<bool> SaveStateAsync(Guid actorId, ActorPath actorPath, Dictionary<string, object> state, long sequenceNr)`: Saves the state of an actor.
- `Task<Dictionary<string, object>?> LoadStateAsync(Guid actorId, ActorPath actorPath)`: Loads the state of an actor.
- `Task<bool> DeleteStateAsync(Guid actorId, ActorPath actorPath)`: Deletes the state of an actor.
- `Task<ActorStateSnapshot?> GetSnapshotAsync(Guid actorId, ActorPath actorPath)`: Gets the state snapshot for an actor.
- `Task<bool> HasState(Guid actorId, ActorPath actorPath)`: Checks if state exists for an actor.

### ActorStateSnapshot Class

The `ActorStateSnapshot` class represents a point-in-time snapshot of an actor's state.

- `Guid ActorId { get; }`: Gets the actor ID.
- `ActorPath ActorPath { get; }`: Gets the actor path.
- `object State { get; }`: Gets the deserialized state.
- `DateTime SavedAt { get; }`: Gets the timestamp when the state was saved.
- `long SequenceNr { get; }`: Gets the sequence number of the snapshot.
- `int Version { get; }`: Gets the snapshot version.

## IBackgroundWorker

The `IBackgroundWorker` interface defines a contract for background work tasks that execute asynchronously. Background workers are designed to handle non-blocking work on a scheduled interval, making them ideal for periodic tasks such as data synchronization, cleanup operations, or polling services.

### Usage Example

```csharp
// Define a custom background worker
public class DataCleanupWorker : IBackgroundWorker
{
    public string WorkerId => "data-cleanup-worker";
    
    public TimeSpan Interval => TimeSpan.FromMinutes(30);
    
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"[{WorkerId}] Starting cleanup at {DateTime.UtcNow}");
        
        // Perform cleanup logic here
        await Task.Delay(1000, cancellationToken); // Simulate work
        
        Console.WriteLine($"[{WorkerId}] Cleanup completed successfully");
    }
}

// Set up the background worker service
var workerService = new BackgroundWorkerService();

// Register the worker
var cleanupWorker = new DataCleanupWorker();
workerService.RegisterWorker(cleanupWorker);

// Start all workers
await workerService.StartAsync();

// Monitor worker status
var status = workerService.GetWorkerStatus("data-cleanup-worker");
if (status != null)
{
    Console.WriteLine($"Worker Status: Running={status.IsRunning}, " +
                     $"Executions={status.ExecutionCount}, " +
                     $"LastError={status.LastError ?? "None"}");
}

// Stop all workers when application shuts down
await workerService.StopAsync();
```

### Properties and Methods

- `string WorkerId { get; }`: Gets the unique identifier for the worker.
- `TimeSpan Interval { get; }`: Gets the interval at which this worker should execute.
- `Task ExecuteAsync(CancellationToken cancellationToken)`: Executes the background work.
- `Task OnStartAsync()`: Called when the worker is starting (default implementation returns `Task.CompletedTask`).
- `Task OnStopAsync()`: Called when the worker is stopping (default implementation returns `Task.CompletedTask`).

### Related Classes

- `BackgroundWorkerService`: Manages and executes background workers with lifecycle control.
- `WorkerStatus`: Provides status information for a background worker including execution counts, error tracking, and runtime state.

## MessagePersistenceRepository

The `MessagePersistenceRepository` class provides append-only log semantics for persisting and retrieving messages in the actor system. It enables durable message storage with sequence tracking, delivery status monitoring, and statistics collection, making it ideal for message replay, recovery scenarios, and system monitoring.

### Usage Example

```csharp
// Initialize the message persistence repository
var connectionManager = new ConnectionManager();
var messageRepository = new MessagePersistenceRepository(connectionManager);

// Create actors and send messages
var actorSystem = new ActorSystem("PersistenceDemoSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

var message = new Message("process", new Dictionary<string, object> { { "data", 42 } });
var envelope = new Envelope(message, workerActor, rootActor);

// Persist the message
bool persisted = await messageRepository.PersistAsync(envelope);
Console.WriteLine($"Message persisted: {persisted}");

// Retrieve messages for an actor
var actorMessages = await messageRepository.GetActorMessagesAsync(workerActor.Id);
Console.WriteLine($"Messages for actor: {actorMessages.Count}");

// Get undelivered messages
var undelivered = await messageRepository.GetUndeliveredMessagesAsync();
Console.WriteLine($"Undelivered messages: {undelivered.Count}");

// Mark message as delivered
await messageRepository.MarkAsDeliveredAsync(envelope.EnvelopeId);

// Get statistics
var stats = messageRepository.GetStatistics();
Console.WriteLine($"Total: {stats.TotalMessages}, Delivered: {stats.DeliveredMessages}, Delivery Rate: {stats.GetDeliveryRate():F2}%");
Console.WriteLine($"Sequence: {stats.CurrentSequenceNumber}, Oldest: {stats.OldestMessageTime}, Newest: {stats.NewestMessageTime}");

// Get messages by sequence range
var rangeMessages = await messageRepository.GetMessagesAsync(1, 100);
Console.WriteLine($"Messages in range: {rangeMessages.Count}");

// Clear repository when needed
messageRepository.Clear();
```

### Properties and Methods

- `void Clear()`: Clears all persisted messages and resets the sequence number.
- `long GetCurrentSequenceNumber()`: Gets the current sequence number.
- `long GetMessageCount()`: Gets the total count of persisted messages.
- `Task<IReadOnlyList<PersistedMessage>> GetActorMessagesAsync(Guid actorId)`: Gets messages for a specific actor.
- `Task<IReadOnlyList<PersistedMessage>> GetMessagesAsync(long fromSequence, long toSequence)`: Gets messages between two sequence numbers.
- `Task<IReadOnlyList<PersistedMessage>> GetUndeliveredMessagesAsync()`: Gets undelivered messages.
- `PersistenceStatistics GetStatistics()`: Gets persistence statistics.
- `Task<bool> MarkAsDeliveredAsync(Guid envelopeId)`: Marks a message as delivered.
- `Task<bool> PersistAsync(Envelope envelope)`: Persists a message envelope.

### PersistedMessage Class

The `PersistedMessage` class represents a persisted message in the repository.

#### Properties

- `Guid EnvelopeId { get; set; }`: Gets or sets the envelope ID.
- `string MessageType { get; set; }`: Gets or sets the message type.
- `Guid? SenderId { get; set; }`: Gets or sets the sender ID.
- `Guid RecipientId { get; set; }`: Gets or sets the recipient ID.
- `DateTime PersistedAt { get; set; }`: Gets or sets the timestamp when persisted.
- `bool IsDelivered { get; set; }`: Gets or sets the delivery status.
- `long SequenceNumber { get; set; }`: Gets or sets the sequence number.

### PersistenceStatistics Class

The `PersistenceStatistics` class provides statistics about message persistence.

#### Properties

- `long TotalMessages { get; set; }`: Gets or sets the total message count.
- `long DeliveredMessages { get; set; }`: Gets or sets the delivered message count.
- `long UndeliveredMessages { get; set; }`: Gets or sets the undelivered message count.
- `long CurrentSequenceNumber { get; set; }`: Gets or sets the current sequence number.
- `DateTime? OldestMessageTime { get; set; }`: Gets or sets the oldest message timestamp.
- `DateTime? NewestMessageTime { get; set; }`: Gets or sets the newest message timestamp.

#### Methods

- `double GetDeliveryRate()`: Gets the delivery rate as a percentage.

## IMessageFormatter

The `IMessageFormatter` interface defines a contract for formatting messages into different output formats such as JSON, text, or CSV. Formatters implement this interface to provide consistent message serialization across the actor system, enabling features like message logging, monitoring, and export functionality.

### Usage Example

```csharp
// Create a message to format
var message = new Message("process-order", new Dictionary<string, object>
{
    { "orderId", "order-123" },
    { "amount", 99.99 },
    { "customerId", "cust-456" }
});

// Use the built-in JSON formatter
var jsonFormatter = new JsonMessageFormatter();
string jsonOutput = jsonFormatter.Format(message);
Console.WriteLine(jsonOutput);

// Use the text formatter for human-readable output
var textFormatter = new TextMessageFormatter();
string textOutput = textFormatter.Format(message);
Console.WriteLine(textOutput);

// Use the CSV formatter for data export
var csvFormatter = new CsvMessageFormatter();
csvFormatter.IncludeHeaders = true;
string csvOutput = csvFormatter.Format(message);
Console.WriteLine(csvOutput);

// Access content type information
Console.WriteLine($"JSON Content-Type: {jsonFormatter.ContentType}");
Console.WriteLine($"Text Content-Type: {textFormatter.ContentType}");
Console.WriteLine($"CSV Content-Type: {csvFormatter.ContentType}");
```

### Properties and Methods

- `string ContentType { get; }`: Gets the MIME content type that this formatter produces (e.g., "application/json", "text/plain", "text/csv").
- `string Format(Message message)`: Formats the given message into the appropriate output format.

### MessageFormatterFactory

The `MessageFormatterFactory` class provides a registry for creating and retrieving formatters by key. It includes built-in formatters for JSON, text, and CSV formats.

#### Usage Example

```csharp
// Create a formatter factory
var factory = new MessageFormatterFactory();

// Register a custom formatter
factory.Register("xml", new XmlMessageFormatter());

// Get a formatter by key
var jsonFormatter = factory.GetFormatter("json");
if (jsonFormatter != null)
{
    string formatted = jsonFormatter.Format(message);
    Console.WriteLine(formatted);
}

// Format a message using the factory
string output = factory.Format(message, "text");
Console.WriteLine(output);
```

#### Properties and Methods

- `MessageFormatterFactory()`: Initializes a new formatter factory with built-in formatters for JSON, text, and CSV formats.
- `void Register(string key, IMessageFormatter formatter)`: Registers a formatter with a specific key.
- `IMessageFormatter? GetFormatter(string key)`: Gets a formatter by its registration key.
- `string Format(Message message, string formatterKey)`: Formats a message using the formatter registered under the specified key.

### Built-in Formatters

- **JsonMessageFormatter**: Formats messages as pretty-printed JSON with `ContentType = "application/json"`.
- **TextMessageFormatter**: Formats messages as human-readable text with headers and metadata with `ContentType = "text/plain"`.
- **CsvMessageFormatter**: Formats messages as CSV with optional headers with `ContentType = "text/csv"`. Includes `IncludeHeaders` property to control header output.

## IMessageSerializer

The `IMessageSerializer` interface defines a contract for serializing and deserializing actor messages to and from byte arrays. This enables message persistence, network transmission, and interoperability with different storage systems or message brokers.

### Usage Example

```csharp
// Create a message to serialize
var message = new Message("process-order", new Dictionary<string, object>
{
    { "orderId", "order-123" },
    { "amount", 99.99 },
    { "customerId", "cust-456" }
});

// Use the built-in JSON serializer
var serializer = new JsonMessageSerializer();

// Serialize the message to bytes
byte[] serializedData = serializer.Serialize(message);
Console.WriteLine($"Serialized message size: {serializedData.Length} bytes");

// Deserialize the message back
Message? deserializedMessage = serializer.Deserialize(serializedData);
if (deserializedMessage != null)
{
    Console.WriteLine("Deserialized message:");
    Console.WriteLine($"  Command: {deserializedMessage.Command}");
    Console.WriteLine($"  Data: {string.Join(", ", deserializedMessage.Data.Select(kvp => $"${kvp.Key}={kvp.Value}"))}");
}

// Serialize and deserialize with null handling
var controlMessage = new ControlMessage("shutdown", new Dictionary<string, object>
{
    { "graceful", true },
    { "delayMs", 5000 }
});

byte[] controlBytes = serializer.Serialize(controlMessage);
Message? controlResult = serializer.Deserialize(controlBytes);
Console.WriteLine($"Control message deserialized: {controlResult != null}");
```

### Properties and Methods

- `byte[] Serialize(Message message)`: Serializes a message to a byte array using the configured serialization strategy.
- `Message? Deserialize(byte[] data)`: Deserializes a message from a byte array, returning null if deserialization fails.

### Implementations

- **JsonMessageSerializer**: JSON-based serializer using System.Text.Json with camelCase naming policy and null value handling.

### Related Types

- `Message`: The message class containing command and data payload.
- `ControlMessage`: Special message type for system control operations.
- `IStateSerializer`: Interface for serializing actor state objects.
- `IEnvelopeSerializer`: Interface for serializing message envelopes.

## ActorSystemConfiguration

The `ActorSystemConfiguration` class serves as the central configuration service for the DotNetActorFramework actor system. It coordinates the setup and initialization of all core services including actor registry, mailbox service, message dispatcher, supervision service, and metrics collection. This configuration service maintains runtime statistics about system health, message throughput, actor status, and resource utilization, providing a comprehensive view of the actor system's operational state.

### Usage Example

```csharp
// Initialize the actor system configuration
var actorSystemConfig = new ActorSystemConfiguration();

// Initialize the actor system with configuration
var actorSystem = await actorSystemConfig.InitializeAsync("ProductionSystem");

// Create actors in the hierarchy
var rootActor = await actorSystemConfig.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystemConfig.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Send messages between actors
await actorSystemConfig.SendMessageAsync(rootActor, workerActor, 
    new Message("process-data", new Dictionary<string, object> { { "data", 42 } }));

// Monitor system health and statistics
var healthSummary = await actorSystemConfig.GetHealthSummary();
Console.WriteLine($"System Health: {healthSummary.GetHealthPercentage()}%");
Console.WriteLine($"Total Messages: {healthSummary.TotalMessages}, Total Errors: {healthSummary.TotalErrors}");

var statistics = actorSystemConfig.GetStatistics();
Console.WriteLine($"Total Actors: {statistics.TotalActors}");
Console.WriteLine($"Message Throughput: {statistics.MessagesPerSecond:F2} msg/s");

// Shutdown the system gracefully
await actorSystemConfig.ShutdownAsync();
```

## DependencyInjectionSetup

The `DependencyInjectionSetup` class provides extension methods for configuring the DotNetActorFramework with Microsoft.Extensions.DependencyInjection. These methods simplify the setup of the actor system, persistence services, and other framework components by registering them as services in the dependency injection container. The extension methods support different configuration profiles (high-performance, reliable, cluster) and allow custom configuration through callback functions.

### Usage Example

```csharp
// Configure services with default settings
var services = new ServiceCollection();
services.AddActorFramework();

// Configure services with custom options
var customServices = new ServiceCollection();
customServices.AddActorFramework(options =>
{
    options.DefaultMailboxCapacity = 2000;
    options.EnableMessagePersistence = true;
    options.EnableMetricsCollection = true;
    options.SnapshotIntervalSeconds = 300;
    options.MaxMessageRetries = 3;
});

// Configure high-performance services (optimized for throughput)
var highPerformanceServices = new ServiceCollection();
highPerformanceServices.AddActorFrameworkHighPerformance();

// Configure reliable services (optimized for durability and fault tolerance)
var reliableServices = new ServiceCollection();
reliableServices.AddActorFrameworkReliable("Server=localhost;Database=ActorFramework;User Id=sa;Password=your_password;");

// Configure cluster services (for distributed actor systems)
var clusterServices = new ServiceCollection();
clusterServices.AddActorFrameworkCluster(
    clusterAddress: "node1.example.com:8080",
    connectionString: "Server=localhost;Database=ActorFramework;User Id=sa;Password=your_password;"
);

// Build the service provider and resolve required services
var serviceProvider = services.BuildServiceProvider();

// Resolve the actor system configuration
var actorSystemConfig = serviceProvider.GetRequiredService<ActorSystemConfiguration>();

// Resolve other framework services
var actorRegistry = serviceProvider.GetRequiredService<ActorRegistry>();
var mailboxService = serviceProvider.GetRequiredService<MailboxService>();
var messageDispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();
var supervisionService = serviceProvider.GetRequiredService<SupervisionService>();

// For cluster mode
if (serviceProvider.GetService<ClusterActorRegistry>() != null)
{
    var clusterRegistry = serviceProvider.GetRequiredService<ClusterActorRegistry>();
}
```

### Extension Methods

- `AddActorFramework(this IServiceCollection services, Action<ActorSystemOptions>? configureOptions = null)`: Adds actor framework services with customizable configuration. Registers the actor system options, persistence services, repositories, and core services like ActorRegistry, MailboxService, MessageDispatcher, and SupervisionService.

- `AddActorFrameworkHighPerformance(this IServiceCollection services)`: Adds actor framework services optimized for high throughput with minimal overhead. Disables message persistence and metrics collection for maximum performance.

- `AddActorFrameworkReliable(this IServiceCollection services, string? connectionString = null)`: Adds actor framework services optimized for reliability and fault tolerance. Enables message persistence, actor state snapshotting, and sets appropriate retry limits.

- `AddActorFrameworkCluster(this IServiceCollection services, string clusterAddress = "127.0.0.1:8080", string? connectionString = null)`: Adds actor framework services configured for cluster mode. Enables cluster mode, message persistence, metrics collection, and registers the ClusterActorRegistry service.

- `ConfigureActorFramework(this IServiceCollection services, Action<ActorSystemOptions> configureOptions)`: Configures actor framework options using the Microsoft.Extensions.Options pattern. Allows updating options after initial registration.

### Related Types

- `ActorSystemOptions`: Configuration options for the actor system including mailbox capacity, persistence settings, and cluster configuration.
- `ActorSystemConfiguration`: Main configuration service that coordinates the setup of the actor system.
- `ActorRegistry`: Service for managing actor registrations and lookups.
- `MailboxService`: Manages per-actor mailboxes for message queuing.
- `MessageDispatcher`: Handles message routing and delivery between actors.
- `SupervisionService`: Manages failure handling and supervision strategies for actors.
- `ClusterActorRegistry`: Service for managing actors across a cluster of nodes.

## ActorDiscoveryService

The `ActorDiscoveryService` class provides capability-based actor discovery, enabling actors to be located dynamically by their registered capabilities and metadata tags. This service maintains indices of actors by their capabilities and tags, allowing for efficient discovery without coupling callers to concrete actor paths or identifiers.

### Usage Example

```csharp
// Initialize the actor system and discovery service
var actorSystem = new ActorSystem("DiscoveryDemoSystem");
var discoveryService = new ActorDiscoveryService();

// Create actors with different capabilities
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
discoveryService.Register(rootActor, new[] { "root", "admin" }, new[] { "system", "core" });

var userActor = await actorSystem.CreateActorAsync(new ActorPath("/root/users"), rootActor);
discoveryService.Register(userActor, new[] { "user-management", "read" }, new[] { "users", "management" });

var orderActor = await actorSystem.CreateActorAsync(new ActorPath("/root/orders"), rootActor);
discoveryService.Register(orderActor, new[] { "order-processing", "write" }, new[] { "orders", "processing" });

// Discover actors by capability
var rootActors = discoveryService.Discover("root");
Console.WriteLine($"Root actors found: {rootActors.Count}"); // Should find rootActor

var userManagementActors = discoveryService.Discover("user-management");
Console.WriteLine($"User management actors: {userManagementActors.Count}"); // Should find userActor

// Discover actors by tag
var systemActors = discoveryService.DiscoverByTag("system");
Console.WriteLine($"System actors: {systemActors.Count}"); // Should find rootActor

var managementActors = discoveryService.DiscoverByTag("management");
Console.WriteLine($"Management actors: {managementActors.Count}"); // Should find userActor

// Get all registered entries
var allEntries = discoveryService.GetAll();
Console.WriteLine($"Total registered entries: {allEntries.Count}");

// Unregister an actor when it terminates
if (discoveryService.Unregister(userActor))
{
    Console.WriteLine("User actor successfully unregistered");
}

// The service automatically filters out dead actors during discovery
```

### Properties and Methods

- `void Register(ActorRef actorRef, IEnumerable<string> capabilities, IEnumerable<string>? tags = null)`: Registers an actor with one or more capabilities and optional metadata tags. Subsequent registrations for the same actor overwrite previous entries.
- `bool Unregister(ActorRef actorRef)`: Removes an actor from all capability and tag indices. Returns true if the actor was found and removed.
- `IReadOnlyList<ActorRef> Discover(string capability)`: Returns all live actors registered under the specified capability (case-insensitive).
- `IReadOnlyList<ActorRef> DiscoverByTag(string tag)`: Returns all live actors that carry the given metadata tag (case-insensitive).
- `IReadOnlyList<ActorDiscoveryEntry> GetAll()`: Returns a point-in-time snapshot of all registered discovery entries.

### ActorDiscoveryEntry Record

The `ActorDiscoveryEntry` record captures an actor's discovery registration details:

- `ActorRef ActorRef { get; }`: The registered actor reference.
- `string[] Capabilities { get; }`: Capability identifiers under which this actor is discoverable.
- `string[] Tags { get; }`: Metadata tags associated with this actor.
- `DateTime RegisteredAt { get; }`: The UTC timestamp when this entry was created.

## ActorMetricsRepository

### Usage Example

```csharp
// Initialize metrics repository for a specific actor
var actorId = Guid.NewGuid();
var actorPath = new ActorPath("/user/order-processor");
var metricsRepository = new ActorMetricsRepository(actorId, actorPath);

// Record metrics as the actor processes messages
metricsRepository.RecordMetricsAsync(100, 5, 2, 150.5).Wait();

// Retrieve historical metrics
var history = await metricsRepository.GetHistoryAsync();
Console.WriteLine($"Total snapshots: {history.Count}");

// Get current metrics
var currentMetrics = await metricsRepository.GetMetricsAsync();
Console.WriteLine($"Actor {metricsRepository.ActorId}: Messages={metricsRepository.MessageCount}, Errors={metricsRepository.ErrorCount}, ErrorRate={metricsRepository.ErrorRate:P2}");

// Get aggregated metrics
var aggregate = await metricsRepository.GetAggregateMetricsAsync();
Console.WriteLine($"Average processing time: {aggregate.AverageProcessingTimeMs:F2}ms, Success rate: {aggregate.SuccessRate:P2}");

// Get latest snapshots
var latest = await metricsRepository.GetLatestSnapshotsAsync(5);
foreach (var snapshot in latest)
{
    Console.WriteLine($"[{snapshot.RecordedAt}] Messages: {snapshot.MessageCount}, Errors: {snapshot.ErrorCount}");
}

// Clear metrics history when needed
metricsRepository.ClearHistory();
```

### Properties and Methods

- `Guid ActorId { get; }`: Gets the unique identifier of the actor being tracked.
- `string ActorPath { get; }`: Gets the path of the actor in the actor system.
- `long MessageCount { get; }`: Gets the total number of messages processed by the actor.
- `long ProcessedCount { get; }`: Gets the total number of messages successfully processed.
- `long ErrorCount { get; }`: Gets the total number of errors encountered.
- `double ErrorRate { get; }`: Gets the error rate as a percentage (0-1).
- `double SuccessRate { get; }`: Gets the success rate as a percentage (0-1).
- `double AverageProcessingTimeMs { get; }`: Gets the average processing time in milliseconds.
- `DateTime RecordedAt { get; }`: Gets the timestamp when metrics were recorded.
- `int TotalActorsTracked { get; }`: Gets the total number of actors being tracked by this repository.
- `int TotalSnapshots { get; }`: Gets the total number of metrics snapshots stored.
- `long TotalMessages { get; }`: Gets the total number of messages across all tracked actors.

- `Task<bool> RecordMetricsAsync(long messageCount, long processedCount, long errorCount, double averageProcessingTimeMs)`: Records metrics for the actor.
- `Task<IReadOnlyList<MetricsSnapshot>> GetHistoryAsync()`: Retrieves the complete history of metrics snapshots.
- `Task<IReadOnlyList<MetricsSnapshot>> GetMetricsAsync()`: Retrieves current metrics snapshots.
- `Task<AggregateMetrics> GetAggregateMetricsAsync()`: Retrieves aggregated metrics across all snapshots.
- `Task<IReadOnlyList<MetricsSnapshot>> GetLatestSnapshotsAsync(int count)`: Retrieves the most recent metrics snapshots.
- `void ClearHistory()`: Clears all stored metrics history.
- `void Clear()`: Clears all metrics data.

### MetricsSnapshot Class

The `MetricsSnapshot` class represents a point-in-time snapshot of an actor's metrics.

#### Properties

- `Guid ActorId { get; set; }`: Gets or sets the actor ID.
- `string ActorPath { get; set; }`: Gets or sets the actor path.
- `long MessageCount { get; set; }`: Gets or sets the message count.
- `long ProcessedCount { get; set; }`: Gets or sets the processed count.
- `long ErrorCount { get; set; }`: Gets or sets the error count.
- `double ErrorRate { get; set; }`: Gets or sets the error rate.
- `double SuccessRate { get; set; }`: Gets or sets the success rate.
- `double AverageProcessingTimeMs { get; set; }`: Gets or sets the average processing time in milliseconds.
- `DateTime RecordedAt { get; set; }`: Gets or sets the timestamp when the snapshot was recorded.

### AggregateMetrics Class

The `AggregateMetrics` class represents aggregated metrics across multiple snapshots.

#### Properties

- `double AverageProcessingTimeMs { get; set; }`: Gets or sets the average processing time across all snapshots.
- `double ErrorRate { get; set; }`: Gets or sets the average error rate.
- `double SuccessRate { get; set; }`: Gets or sets the average success rate.

## SupervisionService

The `SupervisionService` class manages supervision strategies for handling actor failures in the DotNetActorFramework. It implements various strategies including restart, stop, escalate, resume, and backoff to provide fault tolerance and recovery mechanisms for actors. The service tracks failure statistics, restart counts, and implements exponential backoff for failed actors, enabling robust error handling and system recovery.

### Usage Example

```csharp
// Initialize the actor system and supervision service
var actorSystem = new ActorSystem("SupervisionDemoSystem");
var registry = new ActorRegistry();
var mailboxService = new MailboxService(new ActorSystemOptions());
var dispatcher = new MessageDispatcher(mailboxService, registry);

var supervisionService = new SupervisionService(registry, dispatcher);

// Create actors in the hierarchy
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Simulate a failure and handle it with different strategies
try
{
    // Actor processing that might fail
    await workerActor.ReceiveAsync(new Message("process-data", new Dictionary<string, object>()));
}
catch (Exception ex)
{
    // Handle failure with restart strategy
    await supervisionService.HandleFailureAsync(workerActor, ex, SupervisionStrategy.Restart);
    
    // Get statistics after handling failure
    var stats = supervisionService.GetStatistics();
    Console.WriteLine($"Total failures: {stats.TotalFailures}");
    Console.WriteLine($"Total restarts: {stats.TotalRestarts}");
    Console.WriteLine($"Average failures per actor: {stats.AverageFailuresPerActor:F2}");
}

// Reset failure context for an actor
supervisionService.ResetContext(workerActor.Id);

// Get supervision context for a specific actor
var context = supervisionService.GetContext(workerActor.Id);
if (context != null)
{
    Console.WriteLine($"Actor ID: {context.ActorId}");
    Console.WriteLine($"Failure count: {context.FailureCount}");
    Console.WriteLine($"Restart count: {context.RestartCount}");
    Console.WriteLine($"Time since last failure: {context.GetTimeSinceLastFailure()}");
}
```

### Properties and Methods

- `SupervisionService(ActorRegistry registry, MessageDispatcher dispatcher)`: Initializes a new supervision service with the specified actor registry and message dispatcher.

- `Task HandleFailureAsync(ActorRef actor, Exception exception, SupervisionStrategy strategy)`: Handles an actor failure according to the specified supervision strategy.

- `void ResetContext(Guid actorId)`: Resets the supervision context for a specific actor.

- `SupervisionStatistics GetStatistics()`: Gets supervision statistics including total failures, restarts, and average failures per actor.

- `SupervisionContext? GetContext(Guid actorId)`: Gets the supervision context for a specific actor.

- `Dictionary<Guid, SupervisionContext> GetAllContexts()`: Gets all supervision contexts.


### SupervisionContext Class

The `SupervisionContext` class tracks failure and restart statistics for a supervised actor.

- `Guid ActorId { get; }`: Gets the unique identifier of the supervised actor.

- `int FailureCount { get; set; }`: Gets or sets the number of failures encountered by the actor.

- `int RestartCount { get; set; }`: Gets or sets the number of times the actor has been restarted.

- `DateTime LastFailureTime { get; set; }`: Gets or sets the timestamp of the last failure.

- `void ResetFailures()`: Resets the failure and restart counters.

- `TimeSpan GetTimeSinceLastFailure()`: Gets the time elapsed since the last failure.

### SupervisionStatistics Class

The `SupervisionStatistics` class provides aggregated statistics about supervision operations.

- `int TotalActorsSupervised { get; set; }`: Gets or sets the total number of actors being supervised.

- `long TotalFailures { get; set; }`: Gets or sets the total number of failures across all supervised actors.

- `long TotalRestarts { get; set; }`: Gets or sets the total number of restarts across all supervised actors.

- `double AverageFailuresPerActor { get; set; }`: Gets or sets the average number of failures per actor.

### SupervisionStrategy Enum

The `SupervisionStrategy` enum defines the available supervision strategies:

- `Restart`: Restarts the failed actor.
- `Stop`: Stops the failed actor.
- `Resume`: Resumes processing after a failure without restarting.
- `Escalate`: Escalates the failure to the supervisor.
- `Backoff`: Implements exponential backoff before resuming.

## SystemMetricsApi

The `SystemMetricsApi` class provides a centralized API for accessing system metrics and health information from the DotNetActorFramework. It aggregates data from the actor system and metrics collector to provide comprehensive insights into system performance, actor health, message processing statistics, and error rates. This API is designed for monitoring, alerting, and diagnostic purposes, enabling developers to track system health and identify performance bottlenecks.

### Usage Example

```csharp
// Initialize the actor system and metrics collector
var actorSystem = new ActorSystem("MonitoringDemoSystem");
var metricsCollector = new MetricsCollector(actorSystem);

// Create the SystemMetricsApi instance
var metricsApi = new SystemMetricsApi(actorSystem, metricsCollector);

// Get overall system health summary
var healthSummary = metricsApi.GetSystemHealth();
Console.WriteLine($"System Health: {healthSummary.IsHealthy}");
Console.WriteLine($"Total Actors: {healthSummary.TotalActors}, Healthy: {healthSummary.HealthyActors}");
Console.WriteLine($"Total Messages: {healthSummary.TotalMessages}, Errors: {healthSummary.TotalErrors}");
Console.WriteLine($"Error Rate: {healthSummary.ErrorRate:P2}, Health: {healthSummary.HealthPercentage:P2}");
Console.WriteLine($"Average Latency: {healthSummary.AverageLatencyMs:F2}ms");

// Get metrics for specific message types
var messageMetrics = metricsApi.GetMessageTypeMetrics("ProcessOrder");
if (messageMetrics != null)
{
    Console.WriteLine($"Message Type: {messageMetrics.MessageType}");
    Console.WriteLine($"  Processed: {messageMetrics.ProcessedCount}");
    Console.WriteLine($"  Errors: {messageMetrics.ErrorCount}");
    Console.WriteLine($"  Avg Latency: {messageMetrics.AverageLatencyMs:F2}ms");
    Console.WriteLine($"  Error Rate: {messageMetrics.ErrorRate:P2}");
}

// Get metrics for specific actors
var actorMetrics = metricsApi.GetActorMetrics("/root/order-processor");
if (actorMetrics != null)
{
    Console.WriteLine($"Actor: {actorMetrics.ActorPath}");
    Console.WriteLine($"  Processed: {actorMetrics.ProcessedCount}");
    Console.WriteLine($"  Errors: {actorMetrics.ErrorCount}");
    Console.WriteLine($"  Avg Latency: {actorMetrics.AverageLatencyMs:F2}ms");
    Console.WriteLine($"  Error Rate: {actorMetrics.ErrorRate:P2}");
}

// Get top message types by volume
var topMessageTypes = metricsApi.GetTopMessageTypes(5);
Console.WriteLine($"Top 5 Message Types:");
foreach (var msgType in topMessageTypes)
{
    Console.WriteLine($"  {msgType.MessageType}: {msgType.ProcessedCount} messages");
}

// Get slowest actors by latency
var slowestActors = metricsApi.GetSlowesttActors(5);
Console.WriteLine($"Slowest 5 Actors:");
foreach (var actor in slowestActors)
{
    Console.WriteLine($"  {actor.ActorPath}: {actor.AverageLatencyMs:F2}ms avg latency");
}

// Get most error-prone actors
var errorProneActors = metricsApi.GetErrorProneActors(5);
Console.WriteLine($"Most Error-Prone 5 Actors:");
foreach (var actor in errorProneActors)
{
    Console.WriteLine($"  {actor.ActorPath}: {actor.ErrorRate:P2} error rate");
}

// Reset metrics for benchmarking
metricsApi.ResetMetrics();
Console.WriteLine("Metrics reset for fresh measurements.");
```

## MetricsCollectorWorker

The `MetricsCollectorWorker` is a background worker that periodically collects and aggregates metrics from the actor system. It provides real-time monitoring of system health, actor status, message throughput, and error rates, enabling proactive performance analysis and alerting.

### Usage Example

```csharp
// Initialize the actor system and metrics collector
var actorSystem = new ActorSystem("MetricsDemoSystem");
var metricsCollector = new MetricsCollector(actorSystem);

// Create and register the metrics collector worker
var metricsWorker = new MetricsCollectorWorker(actorSystem, metricsCollector);
metricsWorker.Interval = TimeSpan.FromSeconds(15); // Collect metrics every 15 seconds

var workerService = new BackgroundWorkerService();
workerService.RegisterWorker(metricsWorker);

await workerService.StartAsync();

// Monitor system metrics over time
var snapshot1 = metricsWorker.GetLatestSnapshot();
Console.WriteLine($"Snapshot at {snapshot1.Timestamp}: {snapshot1.TotalActors} actors, {snapshot1.TotalMessages} messages, {snapshot1.ErrorRate:P} error rate");

await Task.Delay(TimeSpan.FromMinutes(1));

var snapshot2 = metricsWorker.GetLatestSnapshot();
Console.WriteLine($"Snapshot at {snapshot2.Timestamp}: {snapshot2.TotalActors} actors, {snapshot2.TotalMessages} messages, {snapshot2.ErrorRate:P} error rate");

// Check if system is healthy
if (snapshot2.IsHealthy)
{
    Console.WriteLine("System is healthy!");
}
else
{
    Console.WriteLine("Warning: System has issues!");
}

// Shutdown
await workerService.StopAsync();
```

## ActorSystemDiagnostics

The `ActorSystemDiagnostics` class provides comprehensive monitoring and diagnostics capabilities for the DotNetActorFramework actor system. It captures performance snapshots, memory statistics, garbage collection metrics, actor hierarchy analysis, and system-wide health information. This class is essential for production monitoring, performance tuning, and troubleshooting actor system behavior by providing both real-time diagnostics and historical data through snapshots.

### Usage Example

```csharp
// Initialize the actor system diagnostics
var actorSystem = new ActorSystem("DiagnosticsDemoSystem");
var diagnostics = new ActorSystemDiagnostics(actorSystem);

// Create actors in the hierarchy
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);
var userActor = await actorSystem.CreateActorAsync(new ActorPath("/root/users/user1"), rootActor);

// Take a performance snapshot
var snapshot = diagnostics.TakeSnapshot();
Console.WriteLine($"Snapshot taken at: {snapshot.Timestamp}");
Console.WriteLine($"Total actors: {snapshot.TotalActors}, Healthy: {snapshot.HealthyActors}, Errors: {snapshot.ErrorActors}");
Console.WriteLine($"Total messages: {snapshot.TotalMessages}, Total errors: {snapshot.TotalErrors}");
Console.WriteLine($"Memory usage: {snapshot.MemoryUsageMb} MB");
Console.WriteLine($"CPU usage: {snapshot.CpuUsagePercent:F2}%");

// Get the latest snapshot
var latestSnapshot = diagnostics.GetLatestSnapshot();
if (latestSnapshot != null)
{
    Console.WriteLine($"Latest snapshot: {latestSnapshot.TotalMessages} messages processed");
}

// Get historical snapshots since a specific time
var historicalSnapshots = diagnostics.GetSnapshotsSince(DateTime.UtcNow.AddMinutes(-30));
Console.WriteLine($"Historical snapshots available: {historicalSnapshots.Count}");

// Get memory statistics
var memoryStats = diagnostics.GetMemoryStatistics();
Console.WriteLine($"Working set: {memoryStats.WorkingSetMb} MB");
Console.WriteLine($"Private memory: {memoryStats.PrivateMemoryMb} MB");
Console.WriteLine($"Managed heap: {memoryStats.ManagedHeapMb} MB");

// Get garbage collection statistics
var gcStats = diagnostics.GetGcStatistics();
Console.WriteLine($"GC collections: Gen0={gcStats.Gen0Collections}, Gen1={gcStats.Gen1Collections}, Gen2={gcStats.Gen2Collections}");
Console.WriteLine($"GC pause time: {gcStats.TotalPauseTimeMs} ms");

// Analyze actor hierarchy
var hierarchyAnalysis = diagnostics.AnalyzeActorHierarchy();
Console.WriteLine($"Hierarchy depth: {hierarchyAnalysis.MaxDepth}");
Console.WriteLine($"Total actors in hierarchy: {hierarchyAnalysis.TotalActors}");

// Find heaviest actors (most messages processed)
var heaviestActors = diagnostics.FindHeaviestActors(10);
Console.WriteLine($"Top 10 heaviest actors:");
foreach (var actorInfo in heaviestActors)
{
    Console.WriteLine($"  {actorInfo.ActorPath}: {actorInfo.MessageCount} messages");
}

// Clear snapshots when needed
diagnostics.ClearSnapshots();
Console.WriteLine("Snapshots cleared.");
```

### Properties

- `DateTime Timestamp`: Gets the timestamp when the diagnostics were captured.
- `int TotalActors`: Gets the total number of actors in the system.
- `int HealthyActors`: Gets the count of healthy actors.
- `int ErrorActors`: Gets the count of actors with errors.
- `long TotalMessages`: Gets the total number of messages processed.
- `long TotalErrors`: Gets the total number of errors encountered.
- `long MemoryUsageMb`: Gets the memory usage in megabytes.
- `double CpuUsagePercent`: Gets the CPU usage percentage.

### Methods

- `ActorSystemDiagnostics(ActorSystem actorSystem)`: Initializes a new diagnostics instance for the specified actor system.
- `PerformanceSnapshot TakeSnapshot()`: Takes a performance snapshot of the current system state.
- `PerformanceSnapshot? GetLatestSnapshot()`: Gets the most recent performance snapshot, or null if none exist.
- `IReadOnlyList<PerformanceSnapshot> GetSnapshotsSince(DateTime since)`: Gets all performance snapshots taken since the specified timestamp.
- `MemoryStatistics GetMemoryStatistics()`: Gets current memory usage statistics.
- `GcStatistics GetGcStatistics()`: Gets garbage collection statistics.
- `ActorPathAnalysis AnalyzeActorHierarchy()`: Analyzes the actor hierarchy structure.
- `List<ActorLoadInfo> FindHeaviestActors(int count = 10)`: Finds the actors with the highest message processing load.
- `void ClearSnapshots()`: Clears all stored performance snapshots.

### PerformanceSnapshot Class

The `PerformanceSnapshot` class represents a point-in-time snapshot of actor system performance:

- `DateTime Timestamp`: Gets the timestamp when the snapshot was taken.
- `int TotalActors`: Gets the total number of actors.
- `int HealthyActors`: Gets the count of healthy actors.
- `int ErrorActors`: Gets the count of actors with errors.
- `long TotalMessages`: Gets the total number of messages processed.
- `long TotalErrors`: Gets the total number of errors encountered.
- `long MemoryUsageMb`: Gets the memory usage in megabytes.
- `double CpuUsagePercent`: Gets the CPU usage percentage.
- `long WorkingSetMb`: Gets the working set memory usage.
- `long PrivateMemoryMb`: Gets the private memory usage.
- `long ManagedHeapMb`: Gets the managed heap memory usage.

### MemoryStatistics Class

The `MemoryStatistics` class provides detailed memory usage information:

- `long WorkingSetMb`: Gets the working set memory size in megabytes.
- `long PrivateMemoryMb`: Gets the private memory size in megabytes.
- `long ManagedHeapMb`: Gets the managed heap memory size in megabytes.

### GcStatistics Class

The `GcStatistics` class provides garbage collection metrics:

- `int Gen0Collections`: Gets the number of Gen 0 garbage collections.
- `int Gen1Collections`: Gets the number of Gen 1 garbage collections.
- `int Gen2Collections`: Gets the number of Gen 2 garbage collections.
- `long TotalPauseTimeMs`: Gets the total garbage collection pause time in milliseconds.

### ActorPathAnalysis Class

The `ActorPathAnalysis` class provides actor hierarchy analysis:

- `int MaxDepth`: Gets the maximum depth of the actor hierarchy.
- `int TotalActors`: Gets the total number of actors in the hierarchy.
- `List<string> RootPaths`: Gets the root paths in the hierarchy.

### ActorLoadInfo Class

The `ActorLoadInfo` class represents actor processing load information:

- `string ActorPath`: Gets the actor path.
- `long MessageCount`: Gets the number of messages processed.
- `long ErrorCount`: Gets the number of errors encountered.

### Properties and Methods

- `WorkerId { get; }`: Gets the unique identifier for the worker ("metrics-collector").
- `Interval { get; set; }`: Gets or sets the interval at which metrics are collected.
- `Task ExecuteAsync(CancellationToken cancellationToken)`: Collects and updates the latest metrics snapshot.
- `GetLatestSnapshot() => MetricsSnapshot`: Gets the most recent metrics snapshot.

### MetricsSnapshot Class

The `MetricsSnapshot` class represents a point-in-time snapshot of system metrics.

#### Properties

- `DateTime Timestamp { get; set; }`: Gets or sets the timestamp when the snapshot was taken.
- `int TotalActors { get; set; }`: Gets or sets the total number of actors in the system.
- `int HealthyActors { get; set; }`: Gets or sets the number of healthy actors.
- `int ErrorActors { get; set; }`: Gets or sets the number of actors with errors.
- `long TotalMessages { get; set; }`: Gets or sets the total number of messages processed.
- `long TotalErrors { get; set; }`: Gets or sets the total number of errors encountered.
- `double AverageLatencyMs { get; set; }`: Gets or sets the average message processing latency in milliseconds.
- `double ErrorRate { get; set; }`: Gets or sets the error rate as a percentage.
- `bool IsHealthy { get; }`: Gets whether the system is healthy (no errors and error rate < 5%).

## MetricsCollectionMiddleware

The `MetricsCollectionMiddleware` class collects detailed metrics about message processing within the actor system. It tracks per-actor and per-message-type latency, throughput, and error rates, providing valuable insights into system performance and identifying potential bottlenecks. This middleware runs at the end of the pipeline (Order = 200) to capture complete end-to-end processing times after all other middleware has executed.

Metrics are stored in an associated `MetricsCollector` instance which provides methods to query specific actor or message type metrics, as well as overall system metrics.

### Usage Example

```csharp
// Initialize the actor system and metrics collector
var actorSystem = new ActorSystem("MetricsDemoSystem");
var metricsCollector = new MetricsCollector();

// Create the metrics collection middleware
var metricsMiddleware = new MetricsCollectionMiddleware(metricsCollector);

// Register the middleware in the actor system configuration
// (assuming actor system supports middleware pipeline configuration)

// Process some messages through the system
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Messages will be automatically tracked by the middleware

// Retrieve metrics after processing
var messageMetrics = metricsCollector.GetMessageTypeMetrics("ProcessMessage");
if (messageMetrics != null)
{
    Console.WriteLine($"Message Type: {messageMetrics.MessageType}");
    Console.WriteLine($"  Processed: {messageMetrics.ProcessedCount}");
    Console.WriteLine($"  Errors: {messageMetrics.ErrorCount}");
    Console.WriteLine($"  Avg Latency: {messageMetrics.GetAverageLatencyMs():F2}ms");
    Console.WriteLine($"  Error Rate: {messageMetrics.GetErrorRate():F2}%");
}

var actorMetrics = metricsCollector.GetActorMetrics("/root/worker");
if (actorMetrics != null)
{
    Console.WriteLine($"Actor: {actorMetrics.ActorPath}");
    Console.WriteLine($"  Processed: {actorMetrics.ProcessedCount}");
    Console.WriteLine($"  Errors: {actorMetrics.ErrorCount}");
    Console.WriteLine($"  Avg Latency: {actorMetrics.GetAverageLatencyMs():F2}ms");
    Console.WriteLine($"  Error Rate: {actorMetrics.GetErrorRate():F2}%");
}

// Get overall system metrics
var systemMetrics = metricsCollector.GetSystemMetrics();
Console.WriteLine($"System Metrics:");
Console.WriteLine($"  Total Messages: {systemMetrics.TotalMessagesProcessed}");
Console.WriteLine($"  Total Errors: {systemMetrics.TotalErrors}");
Console.WriteLine($"  Avg Latency: {systemMetrics.AverageLatencyMs:F2}ms");
Console.WriteLine($"  Message Types: {systemMetrics.MessageTypeCount}");
Console.WriteLine($"  Actors: {systemMetrics.ActorCount}");

// Reset metrics when needed
metricsCollector.Reset();
```

### Properties and Methods

- `MetricsCollectionMiddleware(MetricsCollector collector)`: Initializes a new instance with the specified metrics collector.
- `Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)`: Middleware entry point that records message processing metrics.

#### MetricsCollector Class

The `MetricsCollector` class collects and stores metrics about message processing.

- `void RecordMessageProcessed(string actorPath, string messageType, long elapsedMs, bool success)`: Records that a message was processed with timing and success information.
- `MessageTypeMetrics? GetMessageTypeMetrics(string messageType)`: Gets metrics for a specific message type.
- `ActorMetrics? GetActorMetrics(string actorPath)`: Gets metrics for a specific actor.
- `IReadOnlyList<MessageTypeMetrics> GetAllMessageMetrics()`: Gets all message type metrics.
- `IReadOnlyList<ActorMetrics> GetAllActorMetrics()`: Gets all actor metrics.
- `SystemMetrics GetSystemMetrics()`: Gets overall system metrics.
- `void Reset()`: Resets all collected metrics.

#### MessageTypeMetrics Class

The `MessageTypeMetrics` class tracks metrics for a specific message type.

- `string MessageType { get; set; }`: Gets or sets the message type name.
- `long ProcessedCount { get; set; }`: Gets or sets the number of messages processed.
- `long ErrorCount { get; set; }`: Gets or sets the number of errors encountered.
- `long TotalLatencyMs { get; set; }`: Gets or sets the total latency in milliseconds.
- `double GetAverageLatencyMs()`: Gets the average processing latency in milliseconds.
- `double GetErrorRate()`: Gets the error rate as a percentage.

#### ActorMetrics Class

The `ActorMetrics` class tracks metrics for a specific actor.

- `string ActorPath { get; set; }`: Gets or sets the actor path.
- `long ProcessedCount { get; set; }`: Gets or sets the number of messages processed.
- `long ErrorCount { get; set; }`: Gets or sets the number of errors encountered.
- `long TotalLatencyMs { get; set; }`: Gets or sets the total latency in milliseconds.
- `double GetAverageLatencyMs()`: Gets the average processing latency in milliseconds.
- `double GetErrorRate()`: Gets the error rate as a percentage.

#### SystemMetrics Class

The `SystemMetrics` class provides overall system metrics summary.

- `long TotalMessagesProcessed { get; set; }`: Gets or sets the total number of messages processed.
- `long TotalErrors { get; set; }`: Gets or sets the total number of errors encountered.
- `double AverageLatencyMs { get; set; }`: Gets or sets the average latency in milliseconds.
- `int MessageTypeCount { get; set; }`: Gets or sets the number of distinct message types processed.
- `int ActorCount { get; set; }`: Gets or sets the number of actors that have processed messages.
- `double GetErrorRate()`: Gets the overall error rate as a percentage.

## AuthenticationMiddleware

The `AuthenticationMiddleware` class provides authentication for message senders in the actor system. It validates that messages come from authorized sources before passing them to subsequent middleware or the target actor, preventing unauthorized message processing.

### Usage Example

```csharp
// Create an authentication provider (token-based)
var authProvider = new TokenAuthenticationProvider("secret-token-123");

// Register allowed senders with their tokens
authProvider.RegisterSender("order-processor", "secret-token-123");

// Create the authentication middleware
var authMiddleware = new AuthenticationMiddleware(authProvider);

// Use in actor system configuration
var actorSystem = new ActorSystem("SecureSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));

// The middleware will automatically validate sender tokens
// Only messages from registered senders with valid tokens will be processed

// Example with whitelist provider
var whitelistProvider = new WhitelistAuthenticationProvider("trusted-worker-1", "trusted-worker-2");
whitelistProvider.AddSender("new-trusted-worker");

var whitelistMiddleware = new AuthenticationMiddleware(whitelistProvider);
```

### Properties and Methods

- `AuthenticationMiddleware(IAuthenticationProvider authProvider)`: Initializes a new instance of the authentication middleware.
- `Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)`: Authenticates the envelope's sender before forwarding to the next pipeline stage.

### Authentication Providers

#### IAuthenticationProvider Interface

- `Task<bool> AuthenticateAsync(string senderId)`: Authenticates a sender ID.
- `Task<bool> ValidateTokenAsync(string token)`: Validates an authentication token.

#### TokenAuthenticationProvider Class

- `TokenAuthenticationProvider(params string[] validTokens)`: Initializes a new instance with valid tokens.
- `void RegisterSender(string senderId, string token)`: Registers a sender with an authentication token.
- `Task<bool> AuthenticateAsync(string senderId)`: Authenticates a sender by checking their registered token.
- `Task<bool> ValidateTokenAsync(string token)`: Validates a token against the allowed tokens.

#### WhitelistAuthenticationProvider Class

- `WhitelistAuthenticationProvider(params string[] allowedSenders)`: Initializes a new instance with allowed senders.
- `void AddSender(string senderId)`: Adds a sender to the whitelist.
- `void RemoveSender(string senderId)`: Removes a sender from the whitelist.
- `Task<bool> AuthenticateAsync(string senderId)`: Authenticates a sender by checking if they're in the whitelist.
- `Task<bool> ValidateTokenAsync(string token)`: Always returns false (not used by whitelist provider).

#### NoOpAuthenticationProvider Class

- `Task<bool> AuthenticateAsync(string senderId)`: Always returns true (allows all senders).
- `Task<bool> ValidateTokenAsync(string token)`: Always returns true (allows all tokens).

## RateLimitingMiddleware

The `RateLimitingMiddleware` class enforces per-actor rate limiting on message delivery using a token bucket algorithm. Each actor has its own bucket that refills at a fixed rate and holds up to a configurable maximum number of tokens, allowing for controlled message throughput and burst absorption.

When the rate limit is exceeded, messages are silently dropped and the middleware returns `false` without calling the next pipeline stage. This prevents system overload while maintaining predictable performance characteristics.

### Usage Example

```csharp
// Create a rate limiter with 1000 tokens per second (1000 messages per second per actor)
// Default bucket capacity is 10 seconds worth of burst (10000 tokens)
var rateLimiter = new RateLimiter(tokensPerSecond: 1000);

// Create the rate limiting middleware
var rateLimitingMiddleware = new RateLimitingMiddleware(rateLimiter);

// Use in actor system configuration
var actorSystem = new ActorSystem("RateLimitedSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));

// Messages will be rate-limited per recipient actor
// Example: sending 2000 messages to the same actor in one second
// First 1000 messages succeed, next 1000 are dropped

// Check rate limit status for an actor
var status = rateLimiter.GetStatus(rootActor.Path);
Console.WriteLine($"Current tokens: {status.CurrentTokens}/{status.Capacity}, IsLimited: {status.IsLimited}");

// Manually add tokens to a bucket (useful for testing or recovery scenarios)
// Note: In production, tokens are automatically refilled every 100ms
```

### Properties and Methods

- `RateLimitingMiddleware(RateLimiter rateLimiter)`: Initializes a new instance with the specified rate limiter.
- `Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)`: Attempts to consume a rate-limit token for the envelope's recipient. Returns `true` when forwarded, `false` when dropped due to rate limiting.

- `RateLimiter(int tokensPerSecond = 1000, int? bucketCapacity = null)`: Initializes a new rate limiter with configurable tokens per second and bucket capacity.
- `bool TryConsumeToken(ActorPath path)`: Attempts to consume a token from the actor's rate limit bucket.
- `RateLimitStatus GetStatus(ActorPath path)`: Gets the current rate limit status for an actor.
- `void Dispose()`: Disposes the rate limiter and stops the refill timer.

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides centralized error handling for message processing in the actor system. It wraps message processing in a try-catch block and delegates error handling to configurable strategies, enabling consistent error management across all actors. This middleware is particularly useful for implementing retry logic, suppressing non-critical errors, or fail-fast behavior.

### Usage Example

```csharp
// Create an actor system
var actorSystem = new ActorSystem("ErrorHandlingDemoSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));

// Create a worker actor
var workerActor = await actorSystem.CreateActorAsync(
    new ActorPath("/root/worker"),
    rootActor
);

// Configure error handling strategy (retry with exponential backoff)
var retryStrategy = new RetryErrorStrategy(
    maxRetries: 5,
    initialDelay: TimeSpan.FromMilliseconds(100),
    backoffMultiplier: 2.0
);

// Create error handling middleware
var errorMiddleware = new ErrorHandlingMiddleware(retryStrategy);

// Use middleware in actor configuration
// (assuming actor system supports middleware pipeline)

// Example with different strategies
var suppressStrategy = new SuppressErrorStrategy(); // Fire-and-forget
var failFastStrategy = new FailFastErrorStrategy(); // Immediate failure
```

### Properties and Methods

- `ErrorHandlingMiddleware(ErrorHandlingStrategy strategy)`: Initializes a new instance with the specified error handling strategy.
- `Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)`: Wraps message processing in error handling logic.

### ErrorHandlingStrategy

The abstract base class for all error handling strategies.

- `abstract Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)`: Handles an error that occurred during message processing.

### SuppressErrorStrategy

Silently suppresses errors (fire-and-forget semantics).

- `Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)`: Returns `true` to indicate the error was handled (suppressed).

### RetryErrorStrategy

Retries message processing with exponential backoff up to a configured maximum number of attempts.

- `RetryErrorStrategy(int maxRetries = 3, TimeSpan? initialDelay = null, double backoffMultiplier = 2.0)`: Initializes a new instance with configurable retry parameters.
- `Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)`: Increments retry count, waits with exponential backoff, and returns `true` if retries remain, `false` if max retries exceeded.

### FailFastErrorStrategy

Immediately re-throws the exception wrapped in an `InvalidOperationException`.

- `Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)`: Throws an `InvalidOperationException` containing the original exception.

## ConnectionManager

The `ConnectionManager` class manages database connections and provides connection pooling capabilities. It maintains a pool of reusable database connections, tracks connection usage statistics, and provides methods for validating, opening, and closing connections. The connection manager is designed to optimize database resource usage by reusing connections rather than creating new ones for each operation.

### Usage Example

```csharp
// Initialize the connection manager with a connection string
var connectionManager = new ConnectionManager();
connectionManager.Initialize("Server=localhost;Database=MyDatabase;User Id=sa;Password=your_password;");

// Get a connection from the pool
var connection = connectionManager.GetConnection("orders-db");

try
{
    // Open the connection
    connection.Open();
    
    // Use the connection for database operations
    Console.WriteLine($"Connection opened: {connection.Key}");
    Console.WriteLine($"Created at: {connection.CreatedAt}");
    Console.WriteLine($"Last used: {connection.LastUsedAt}");
    Console.WriteLine($"Is open: {connection.IsOpen}");
    
    // Perform database operations here...
}
finally
{
    // Close the connection (returns it to the pool)
    connection.Close();
    connectionManager.ReleaseConnection("orders-db");
}

// Validate the connection
bool isValid = await connectionManager.ValidateConnectionAsync();
Console.WriteLine("Connection valid: {isValid}");

// Get connection statistics
var stats = connectionManager.GetStatistics();
Console.WriteLine($"Pool size: {stats.PoolSize}, Active: {stats.ActiveConnections}, Connected: {stats.IsConnected}");
Console.WriteLine($"Connection created at: {stats.CreatedAt}");

// Check idle time for a connection
var pooledConnection = connectionManager.GetConnection("orders-db");
var idleTime = pooledConnection.GetIdleTime();
Console.WriteLine($"Idle time: {idleTime.TotalSeconds} seconds");
```

### Properties and Methods

- `string Key { get; }`: Gets the unique identifier for this connection.
- `string ConnectionString { get; }`: Gets the connection string associated with this connection.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when this connection was created.
- `DateTime LastUsedAt { get; }`: Gets the UTC timestamp when this connection was last used.
- `bool IsOpen { get; }`: Gets whether the connection is currently open.

- `void Initialize(string connectionString)`: Initializes the connection manager with a connection string.
- `PooledConnection GetConnection(string key = "default")`: Gets or creates a connection from the pool.
- `void ReleaseConnection(string key = "default")`: Releases a connection back to the pool.
- `Task<bool> ValidateConnectionAsync()`: Validates the current connection by opening and closing it.
- `ConnectionStatistics GetStatistics()`: Gets connection statistics.
- `void Dispose()`: Clears the connection pool and disposes all connections.

### PooledConnection Class

The `PooledConnection` class represents a connection from the connection pool.

#### Properties

- `string Key { get; }`: Gets the connection key.
- `string ConnectionString { get; }`: Gets the connection string.
- `DateTime CreatedAt { get; }`: Gets the creation timestamp.
- `DateTime LastUsedAt { get; }`: Gets the last used timestamp.
- `bool IsOpen { get; }`: Gets whether the connection is open.

#### Methods

- `void Open()`: Opens the connection.
- `void Close()`: Closes the connection.
- `void UpdateLastUsed()`: Updates the last used timestamp.
- `TimeSpan GetIdleTime()`: Gets the idle time since last use.
- `void Dispose()`: Disposes the connection.

### ConnectionStatistics Class

The `ConnectionStatistics` class provides statistics about connections.

#### Properties

- `bool IsConnected { get; set; }`: Gets whether the connection manager is connected.
- `int PoolSize { get; set; }`: Gets the current pool size.
- `int ActiveConnections { get; set; }`: Gets the number of active connections.
- `string? ConnectionString { get; set; }`: Gets the connection string.
- `DateTime CreatedAt { get; set; }`: Gets the creation timestamp.

## IHealthCheckFormatter

The `IHealthCheckFormatter` interface defines a contract for formatting system health check information into different output formats such as JSON, text, or CSV. Formatters implement this interface to provide consistent health information serialization across the actor system, enabling features like health monitoring, logging, and export functionality.

### Usage Example

```csharp
// Create a system health summary
var healthSummary = new SystemHealthSummary(
    systemName: "ProductionSystem",
    systemId: Guid.NewGuid(),
    createdAt: DateTime.UtcNow,
    totalActors: 100,
    healthyActors: 95,
    unhealthyActors: 3,
    errorActors: 2,
    totalMessages: 5000,
    totalErrors: 15
);

// Use the built-in JSON formatter
var jsonFormatter = new JsonHealthCheckFormatter();
string jsonOutput = jsonFormatter.Format(healthSummary);
Console.WriteLine(jsonOutput);

// Use the text formatter for human-readable output
var textFormatter = new TextHealthCheckFormatter();
string textOutput = textFormatter.Format(healthSummary);
Console.WriteLine(textOutput);

// Use the CSV formatter for data export
var csvFormatter = new CsvHealthCheckFormatter();
string csvOutput = csvFormatter.Format(healthSummary);
Console.WriteLine(csvOutput);

// Access content type information
Console.WriteLine($"JSON Content-Type: {jsonFormatter.ContentType}");
Console.WriteLine($"Text Content-Type: {textFormatter.ContentType}");
Console.WriteLine($"CSV Content-Type: {csvFormatter.ContentType}");

// Use the formatter factory for dynamic formatter selection
var factory = new HealthCheckFormatterFactory();
string output = factory.Format(healthSummary, "text");
Console.WriteLine(output);

// Register a custom formatter
factory.Register("xml", new CustomHealthCheckFormatter());
```

### Properties and Methods

- `string ContentType { get; }`: Gets the MIME content type that this formatter produces (e.g., "application/json", "text/plain", "text/csv").
- `string Format(SystemHealthSummary health)`: Formats the given health summary into the appropriate output format.

### HealthCheckFormatterFactory

The `HealthCheckFormatterFactory` class provides a registry for creating and retrieving health check formatters by key. It includes built-in formatters for JSON, text, and CSV formats.

#### Usage Example

```csharp
// Create a formatter factory
var factory = new HealthCheckFormatterFactory();

// Register a custom formatter
factory.Register("custom", new CustomHealthCheckFormatter());

// Get a formatter by key
var jsonFormatter = factory.GetFormatter("json");
if (jsonFormatter != null)
{
    string formatted = jsonFormatter.Format(healthSummary);
    Console.WriteLine(formatted);
}

// Format a health summary using the factory
string output = factory.Format(healthSummary, "csv");
Console.WriteLine(output);
```

#### Properties and Methods

- `HealthCheckFormatterFactory()`: Initializes a new formatter factory with built-in formatters for JSON, text, and CSV formats.
- `void Register(string key, IHealthCheckFormatter formatter)`: Registers a formatter with a specific key.
- `IHealthCheckFormatter? GetFormatter(string key)`: Gets a formatter by its registration key.
- `string Format(SystemHealthSummary health, string formatterKey)`: Formats a health summary using the formatter registered under the specified key.

### Built-in Formatters

- **JsonHealthCheckFormatter**: Formats health summaries as pretty-printed JSON with `ContentType = "application/json"`.
- **TextHealthCheckFormatter**: Formats health summaries as human-readable text with headers and metrics with `ContentType = "text/plain"`.
- **CsvHealthCheckFormatter**: Formats health summaries as CSV with headers with `ContentType = "text/csv"`.


## Envelope

The `Envelope` class wraps a message with metadata about sender and recipient, providing essential information for message delivery, tracking, and retry logic within the actor system.


### Usage Example

```csharp
// Create a message to send
var message = new ControlMessage("processOrder", new Dictionary<string, object>
{
    { "orderId", "12345" },
    { "amount", 99.99 }
});

// Create actor references
var actorSystem = new ActorSystem("OrderProcessingSystem");
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var orderProcessor = await actorSystem.CreateActorAsync(new ActorPath("/root/orderProcessor"), rootActor);

// Create an envelope wrapping the message
var envelope = new Envelope(message, orderProcessor, rootActor);

Console.WriteLine($"Envelope created: {envelope}");
Console.WriteLine($"Message: {envelope.Message.Command}");
Console.WriteLine($"Sent at: {envelope.SentAt}");
Console.WriteLine($"Envelope ID: {envelope.EnvelopeId}");
Console.WriteLine($"Initial retry count: {envelope.RetryCount}");

// Simulate message processing
Console.WriteLine($"Time elapsed: {envelope.GetElapsedTime()}");
Console.WriteLine($"Delivery priority: {envelope.GetDeliveryPriority()}");

// Mark as delivered when processing completes
envelope.MarkAsDelivered();
Console.WriteLine($"Is delivered: {envelope.IsDelivered}");

// Simulate retry on failure
envelope.IncrementRetryCount();
Console.WriteLine($"Retry count after failure: {envelope.RetryCount}");
Console.WriteLine($"Has exceeded limit: {envelope.HasExceededRetryLimit(3)}");
```

### Properties and Methods

- `Message Message { get; }`: Gets the message being transported.
- `ActorRef? Sender { get; }`: Gets the sender actor reference, or null if sent from system.
- `ActorRef Recipient { get; }`: Gets the recipient actor reference.
- `DateTime SentAt { get; }`: Gets the UTC timestamp when the envelope was created.
- `Guid EnvelopeId { get; }`: Gets the unique identifier for this envelope.
- `int RetryCount { get; private set; }`: Gets the current retry count for delivery attempts.
- `bool IsDelivered { get; private set; }`: Gets whether the message has been successfully delivered.
- `Envelope(Message message, ActorRef recipient, ActorRef? sender = null)`: Initializes a new envelope.
- `void MarkAsDelivered()`: Marks this envelope as delivered.
- `void IncrementRetryCount()`: Increments the retry count for failed delivery attempts.
- `TimeSpan GetElapsedTime()`: Gets the time elapsed since this message was sent.
- `bool HasExceededRetryLimit(int maxRetries = 3)`: Checks if this envelope has exceeded the retry limit.
- `int GetDeliveryPriority()`: Gets priority-adjusted delivery order information.
- `override string ToString()`: Returns a string representation of the envelope.

## ActorPath

The `ActorPath` class represents the hierarchical path to an actor in the actor system. Paths are immutable and uniquely identify an actor within a system, enabling parent-child relationships and hierarchical navigation.


### Usage Example

```csharp
// Create a root actor path
var rootPath = new ActorPath("/root");
Console.WriteLine(rootPath.Path); // Output: /root
Console.WriteLine(rootPath.Name); // Output: root
Console.WriteLine(rootPath.GetDepth()); // Output: 1

// Create a child actor path
var userPath = rootPath.GetChild("users");
Console.WriteLine(userPath.Path); // Output: /root/users
Console.WriteLine(userPath.Name); // Output: users
Console.WriteLine(userPath.GetDepth()); // Output: 2

// Access parent-child relationships
Console.WriteLine(userPath.Parent?.Path); // Output: /root
Console.WriteLine(userPath.Parent?.Name); // Output: root

// Parse a path from string
var parsedPath = ActorPath.Parse("/root/users/user1");
Console.WriteLine(parsedPath.Path); // Output: /root/users/user1

// Check hierarchical relationships
var orderPath = rootPath.GetChild("orders").GetChild("order123");
Console.WriteLine(orderPath.IsDescendantOf(rootPath)); // Output: True
Console.WriteLine(orderPath.IsDescendantOf(userPath)); // Output: False

// Compare paths
var path1 = new ActorPath("/root/users/user1");
var path2 = new ActorPath("/root/users/user1");
var path3 = new ActorPath("/root/users/user2");
Console.WriteLine(path1.Equals(path2)); // Output: True
Console.WriteLine(path1 == path2); // Output: True
Console.WriteLine(path1.Equals(path3)); // Output: False
```

### Properties and Methods

- `string Path { get; }`: Gets the full path string (e.g., "/root/users/user1").
- `string Name { get; }`: Gets the name of this path segment (e.g., "user1" for "/root/users/user1").
- `ActorPath? Parent { get; }`: Gets the parent path, or null if this is a root path.
- `IReadOnlyList<string> Segments { get; }`: Gets the path segments as a read-only list.
- `ActorPath(string path)`: Initializes a new instance of the `ActorPath` class.
- `static ActorPath Parse(string path)`: Parses a string into an `ActorPath` instance.
- `ActorPath GetChild(string childName)`: Creates a child path from this path.
- `bool IsDescendantOf(ActorPath other)`: Checks if this path is a descendant of another path.
- `int GetDepth()`: Gets the depth of this path in the hierarchy.
- `override string ToString()`: Returns the path string.
- `override bool Equals(object? obj)`: Override of Object.Equals.
- `bool Equals(ActorPath? other)`: Compares this path with another path.
- `override int GetHashCode()`: Override of Object.GetHashCode.

## ActorSystem

The `ActorSystem` class serves as the root coordinator for the actor framework, managing actor lifecycle, message routing, and system health monitoring. It maintains a registry of all actors and provides methods for creating, querying, and terminating actors within the hierarchy.


### Usage Example

```csharp
// Initialize the actor system
var actorSystem = new ActorSystem("MyActorSystem");

// Create actors in the hierarchy
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("root", ""));
var userActor = await actorSystem.CreateActorAsync(new ActorPath("users", "user1"), rootActor);
var orderActor = await actorSystem.CreateActorAsync(new ActorPath("orders", "order123"), rootActor);

// Get actor references by path
var retrievedUser = actorSystem.GetActorRef(new ActorPath("users", "user1"));

// Get all actors under a specific parent
var userActors = actorSystem.GetActorsByParent(new ActorPath("users", ""));

// Get system-wide metrics
var healthSummary = actorSystem.GetHealthSummary();
Console.WriteLine($"System Health: {healthSummary.GetHealthPercentage()}%");
Console.WriteLine($"Total Messages: {healthSummary.TotalMessages}, Total Errors: {healthSummary.TotalErrors}");

// Terminate an actor
if (retrievedUser != null)
{
    await actorSystem.TerminateActorAsync(retrievedUser);
}

// Shutdown the system
await actorSystem.ShutdownAsync();
```

## InMemoryEventJournalExtensions

The `InMemoryEventJournalExtensions` class provides extension methods for working with in-memory event journals. It offers convenient methods for appending, reading, counting, and checking events in the journal, making it easier to manage event data within the actor system.

### Usage Example

```csharp
// Create an in-memory event journal
var journal = new InMemoryEventJournal();

// Append an event to the journal
var event1 = new ActorEvent("order-created", new Dictionary<string, object> { { "orderId", "order-123" } }, 1);
await journal.AppendEventAsync(event1);

// Append another event
var event2 = new ActorEvent("payment-processed", new Dictionary<string, object> { { "orderId", "order-123" }, { "amount", 99.99 } }, 2);
await journal.AppendEventAsync(event2);

// Read all events from the journal
var allEvents = await journal.ReadAllEventsAsync();
Console.WriteLine($"Events in journal: {allEvents.Count()}");

// Count events in the journal
var eventCount = await journal.CountEventsAsync();
Console.WriteLine($"Total events: {eventCount}");

// Check if events exist in the journal
bool hasEvents = await journal.HasEventsAsync();
Console.WriteLine($"Has events: {hasEvents}");

// Get first event
var firstEvent = await journal.GetFirstEventAsync();
if (firstEvent != null)
{
    Console.WriteLine($"First event: {firstEvent.EventType}");
}

// Get last event
var lastEvent = await journal.GetLastEventAsync();
if (lastEvent != null)
{
    Console.WriteLine($"Last event: {lastEvent.EventType}");
}

// Get event at specific sequence number
var eventAtSeq = await journal.GetEventAtSequenceAsync(1);
if (eventAtSeq != null)
{
    Console.WriteLine($"Event at sequence 1: {eventAtSeq.EventType}");
}
```

### Available Methods

- `Task AppendEventAsync(ActorEvent @event)`: Appends an event to the in-memory journal.
- `Task<IEnumerable<ActorEvent>> ReadAllEventsAsync()`: Retrieves all events from the journal.
- `Task<long> CountEventsAsync()`: Gets the total count of events in the journal.
- `Task<bool> HasEventsAsync()`: Checks if the journal contains any events.
- `Task<ActorEvent?> GetFirstEventAsync()`: Gets the first event in the journal.
- `Task<ActorEvent?> GetLastEventAsync()`: Gets the last event in the journal.
- `Task<ActorEvent?> GetEventAtSequenceAsync(long sequenceNumber)`: Gets the event at the specified sequence number.

## ActorManagementApi

The `ActorManagementApi` class provides a centralized API for managing and monitoring actors within the DotNetActorFramework. It enables programmatic inspection of actor states, metrics retrieval, error tracking, and lifecycle management including graceful termination. This API is designed for operational tooling, monitoring dashboards, and administrative interfaces that need to interact with the actor system at runtime.

### Usage Example

```csharp
// Initialize the actor system and management API
var actorSystem = new ActorSystem("ManagementDemoSystem");
var managementApi = new ActorManagementApi(actorSystem);

// Create actors in the hierarchy
var rootActor = await actorSystem.CreateActorAsync(new ActorPath("/root"));
var workerActor = await actorSystem.CreateActorAsync(new ActorPath("/root/worker"), rootActor);

// Get information about a specific actor
var actorInfo = managementApi.GetActor(workerActor.Id);
if (actorInfo != null)
{
    Console.WriteLine($"Actor: {actorInfo.Path}");
    Console.WriteLine($"Is Alive: {actorInfo.IsAlive}");
    Console.WriteLine($"Created At: {actorInfo.CreatedAt}");
}

// List all actors in the system
var allActors = managementApi.ListActors();
Console.WriteLine($"Total actors: {allActors.Total}");

// List actors by parent path
var rootChildren = managementApi.ListActorsByParent(new ActorPath("/root"));
Console.WriteLine($"Root has {rootChildren.Actors.Count} child actors");

// Get metrics for all actors
var metricsSummary = managementApi.GetActorMetrics();
if (metricsSummary != null)
{
    Console.WriteLine($"Total actors: {metricsSummary.Total}");
    Console.WriteLine($"Messages processed: {metricsSummary.MessagesProcessed}");
    Console.WriteLine($"Error rate: {metricsSummary.ErrorRate:P2}");
}

// Terminate an actor gracefully
var response = await managementApi.TerminateActorAsync(workerActor.Id);
if (response.Success)
{
    Console.WriteLine($"Actor terminated: {response.Message}");
}

// Get error actors (actors with high error rates)
var errorActors = managementApi.GetErrorActors();
Console.WriteLine($"Actors with errors: {errorActors.Actors.Count}");

// Get total actor count
var actorCount = managementApi.GetActorCount();
Console.WriteLine($"Total actors in system: {actorCount}");
```

## MockActorContext

The `MockActorContext` class facilitates unit testing by allowing developers to isolate actors and inspect their message interactions. It records sent and received messages, providing a controlled environment to verify actor behavior without a fully functional actor system.

### Usage Example

```csharp
// Setup the mock context for an actor path
var path = new ActorPath("/test/actor");
var mockContext = new MockActorContext(path);

// Simulate message processing
var message = new Message("process", new Dictionary<string, object> { { "data", 42 } });
mockContext.RecordReceivedMessage(message);

// Verify interactions
Console.WriteLine($"ActorId: {mockContext.ActorId}");
Console.WriteLine($"Messages Received: {mockContext.GetMessageCount()}");
Console.WriteLine($"Did receive 'Message' type: {mockContext.DidReceiveMessageType("Message")}");

// Inspect captured messages
var received = mockContext.GetReceivedMessages();
Console.WriteLine($"First message data: {received[0].Data["data"]}");
```

### Properties and Methods

- `ActorPath ActorPath { get; }`: Gets the path of the mocked actor.
- `Guid ActorId { get; }`: Gets the unique ID of the mock context.
- `void RecordReceivedMessage(Message message)`: Records a message received by the actor.
- `void RecordSentMessage(Message message)`: Records a message sent by the actor.
- `IReadOnlyList<Message> GetReceivedMessages()`: Returns all received messages.
- `IReadOnlyList<Message> GetSentMessages()`: Returns all sent messages.
- `IReadOnlyList<Message> GetReceivedMessagesOfType(string messageType)`: Returns received messages of a specific type.
- `int GetMessageCount()`: Gets total received message count.
- `int GetSentMessageCount()`: Gets total sent message count.
- `void Clear()`: Clears all recorded messages.
- `bool DidReceiveMessageType(string messageType)`: Checks if a specific message type was received.
- `bool DidReceiveMessageCount(int count)`: Checks if the received message count matches.