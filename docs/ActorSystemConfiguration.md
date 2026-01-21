# ActorSystemConfiguration

The `ActorSystemConfiguration` class serves as the primary entry point and management interface for initializing, configuring, and monitoring an actor system within the .NET Actor Framework. It encapsulates the lifecycle operations required to bootstrap the system, create actor instances, and facilitate message passing, while also providing real-time access to system-wide statistics, health summaries, and performance metrics across mailboxes, dispatchers, and persistence layers.

## API

### Constructors

#### `public ActorSystemConfiguration()`
Initializes a new instance of the `ActorSystemConfiguration` class. This constructor sets up the internal state required to manage actor system options and statistics, though the system is not active until `InitializeAsync` is called.

### Methods

#### `public async Task<ActorSystem> InitializeAsync()`
Asynchronously initializes the actor system based on the configured options.
*   **Returns**: A task that resolves to the initialized `ActorSystem` instance.
*   **Throws**: May throw exceptions if the configuration is invalid, required resources are unavailable, or the initialization process fails.

#### `public async Task<ActorRef> CreateActorAsync()`
Asynchronously creates a new actor within the initialized system.
*   **Returns**: A task that resolves to an `ActorRef` representing the newly created actor.
*   **Throws**: May throw if the system is not initialized, actor creation limits are reached, or the underlying factory fails.

#### `public ActorSystem GetActorSystem()`
Retrieves the currently active `ActorSystem` instance.
*   **Returns**: The `ActorSystem` object.
*   **Throws**: May throw if the system has not been initialized or has already been shut down.

#### `public async Task SendMessageAsync()`
Asynchronously sends a message to a target actor.
*   **Returns**: A task representing the completion of the send operation.
*   **Throws**: May throw if the target actor is unreachable, the system is shutting down, or the message serialization fails.

#### `public SystemHealthSummary GetHealthSummary()`
Synchronously retrieves a summary of the current system health status.
*   **Returns**: A `SystemHealthSummary` object containing health indicators.
*   **Throws**: Generally does not throw unless internal state is corrupted.

#### `public async Task ShutdownAsync()`
Asynchronously shuts down the actor system, terminating all active actors and releasing resources.
*   **Returns**: A task representing the completion of the shutdown process.
*   **Throws**: May throw if the shutdown process encounters critical errors while terminating actors.

#### `public SystemStatistics GetStatistics()`
Synchronously retrieves comprehensive statistics about the system's current state.
*   **Returns**: A `SystemStatistics` object containing aggregated metrics.
*   **Throws**: Generally does not throw unless internal state is corrupted.

### Properties

#### `public ActorSystemOptions? Options`
Gets or sets the configuration options used to initialize the actor system. This property may be null if no specific options have been assigned prior to initialization.

#### `public SystemHealthSummary? Health`
Gets the latest cached health summary of the system. This property may be null if health data has not yet been collected or if the system is not running.

#### `public MailboxStatistics? MailboxStats`
Gets the current statistics regarding mailbox throughput and queue depths. This property may be null if statistics collection is disabled or not yet initialized.

#### `public DispatcherStatistics? DispatcherStats`
Gets the current statistics regarding dispatcher performance and thread utilization. This property may be null if statistics collection is disabled or not yet initialized.

#### `public SupervisionStatistics? SupervisionStats`
Gets the current statistics regarding supervision events, such as restarts and failures. This property may be null if statistics collection is disabled or not yet initialized.

#### `public PersistenceStatistics? PersistenceStats`
Gets the current statistics regarding persistence operations, including snapshot and event store latency. This property may be null if persistence is not configured or statistics are unavailable.

#### `public ConnectionStatistics? ConnectionStats`
Gets the current statistics regarding network connections and remote messaging. This property may be null if remote communication is not enabled or statistics are unavailable.

#### `public DateTime CollectedAt`
Gets the timestamp indicating when the statistical data properties (such as `Health`, `MailboxStats`, etc.) were last updated or collected.

## Usage

### Example 1: System Initialization and Actor Creation
The following example demonstrates how to configure, initialize, and create an actor within the system.

```csharp
var config = new ActorSystemConfiguration();
config.Options = new ActorSystemOptions 
{ 
    Name = "ProductionSystem", 
    DefaultDispatcher = "ThreadPool" 
};

// Initialize the system
var system = await config.InitializeAsync();

// Create a new actor instance
var workerRef = await config.CreateActorAsync();

// Send an initial message
await config.SendMessageAsync(workerRef, new StartProcessingCommand());

// Retrieve current statistics
var stats = config.GetStatistics();
Console.WriteLine($"System initialized at: {config.CollectedAt}");
```

### Example 2: Monitoring Health and Graceful Shutdown
This example illustrates how to monitor system health metrics and perform a graceful shutdown.

```csharp
var config = new ActorSystemConfiguration();
await config.InitializeAsync();

// Periodically check health
var health = config.GetHealthSummary();
if (!health.IsHealthy)
{
    Console.WriteLine($"System unhealthy: {health.StatusMessage}");
}

// Access specific subsystem stats
if (config.MailboxStats != null)
{
    Console.WriteLine($"Pending messages: {config.MailboxStats.QueueSize}");
}

// Graceful shutdown
try
{
    await config.ShutdownAsync();
    Console.WriteLine("System shutdown completed successfully.");
}
catch (Exception ex)
{
    Console.WriteLine($"Shutdown failed: {ex.Message}");
}
```

## Notes

*   **Initialization State**: Methods such as `CreateActorAsync`, `SendMessageAsync`, and `GetActorSystem` require the system to be in an initialized state. Invoking these prior to calling `InitializeAsync` or after `ShutdownAsync` has completed will result in exceptions.
*   **Thread Safety**: The statistical properties (`Health`, `MailboxStats`, `DispatcherStats`, etc.) represent snapshots in time indicated by `CollectedAt`. While reading these properties is generally safe, they may not reflect real-time changes occurring concurrently during high-load operations.
*   **Nullability**: Statistical properties are nullable. Consumers must check for `null` before accessing nested members, as statistics may be unavailable if the corresponding subsystem (e.g., Persistence or Remote Connections) is not configured or active.
*   **Asynchronous Lifecycle**: Both initialization and shutdown are asynchronous operations. It is critical to `await` these tasks to ensure resources are fully allocated or released before proceeding with dependent logic.
