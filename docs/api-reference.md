# API Reference

## ActorSystem

Main entry point for the actor framework.

```csharp
public class ActorSystem
{
    // Lifecycle
    public Task StartAsync();
    public Task ShutdownAsync();
    public ActorState GetState();
    
    // Actor Management
    public ActorRef? GetActor(ActorPath path);
    public IEnumerable<ActorRef> GetAllActors();
    public IEnumerable<ActorRef> GetActorsByPattern(string pathPattern);
    
    // Statistics
    public Task<SystemStatistics> GetStatisticsAsync();
    public HealthSummary GetHealthSummary();
}
```

### Methods

#### StartAsync()

Initializes and starts the actor system.

```csharp
var system = new ActorSystem(options);
await system.StartAsync();
```

#### ShutdownAsync()

Gracefully shuts down the actor system.

```csharp
await system.ShutdownAsync();
```

#### GetActor(ActorPath)

Retrieves an actor reference by path.

```csharp
var path = new ActorPath("/user/myactor");
var actor = system.GetActor(path);
if (actor != null)
{
    // Actor exists
}
```

#### GetAllActors()

Returns all active actors.

```csharp
var allActors = system.GetAllActors();
foreach (var actor in allActors)
{
    Console.WriteLine(actor.Path);
}
```

#### GetStatisticsAsync()

Returns comprehensive system statistics.

```csharp
var stats = await system.GetStatisticsAsync();
Console.WriteLine($"Messages processed: {stats.DispatcherStats.TotalProcessed}");
```

## ActorRegistry

Manages actor lifecycle and discovery.

```csharp
public interface IActorRegistry
{
    Task<ActorRef> CreateActorAsync(ActorPath path, ActorRef? supervisor = null);
    Task TerminateActorAsync(ActorRef actorRef);
    ActorRef? GetActorByPath(ActorPath path);
    IEnumerable<ActorRef> GetActorsByPath(ActorPath parentPath);
    Task<ActorMetrics> GetActorMetricsAsync(ActorRef actorRef);
}
```

### Methods

#### CreateActorAsync()

Creates a new actor instance.

```csharp
var path = new ActorPath("/user/worker");
var actorRef = await registry.CreateActorAsync(path);

// With supervisor
var supervisorPath = new ActorPath("/user/supervisor");
var supervisorRef = await registry.GetActorByPath(supervisorPath);
var childRef = await registry.CreateActorAsync(
    new ActorPath("/user/supervisor/child"), 
    supervisorRef
);
```

#### TerminateActorAsync()

Stops an actor gracefully.

```csharp
var actorRef = registry.GetActorByPath(path);
if (actorRef != null)
{
    await registry.TerminateActorAsync(actorRef);
}
```

#### GetActorByPath()

Retrieves an actor by path.

```csharp
var actorRef = registry.GetActorByPath(new ActorPath("/user/worker"));
```

#### GetActorsByPath()

Retrieves all actors under a path.

```csharp
var parentPath = new ActorPath("/user");
var children = registry.GetActorsByPath(parentPath);
```

## MessageDispatcher

Handles message routing and delivery.

```csharp
public interface IMessageDispatcher
{
    Task SendAsync(ActorRef recipient, Message message);
    Task SendAsync(ActorRef recipient, Message message, ActorRef sender);
    Task<bool> TrySendAsync(ActorRef recipient, Message message, TimeSpan timeout);
    Task PublishAsync(Message message);
    Task<Message?> SendAndWaitAsync(ActorRef recipient, Message message, 
        TimeSpan timeout);
}
```

### Methods

#### SendAsync()

Sends a message to an actor.

```csharp
var message = new ControlMessage("process");
await dispatcher.SendAsync(actorRef, message);
```

#### SendAsync(recipient, message, sender)

Sends a message with explicit sender information.

```csharp
var sender = registry.GetActorByPath(new ActorPath("/user/requester"));
await dispatcher.SendAsync(actorRef, message, sender);
```

#### TrySendAsync()

Sends a message with timeout.

```csharp
var success = await dispatcher.TrySendAsync(
    actorRef, 
    message, 
    timeout: TimeSpan.FromSeconds(5)
);

if (!success)
{
    Console.WriteLine("Message delivery timed out");
}
```

#### PublishAsync()

Publishes a message to all interested listeners.

```csharp
var eventMessage = new ControlMessage("event-occurred");
await dispatcher.PublishAsync(eventMessage);
```

#### SendAndWaitAsync()

Sends a message and waits for a response.

```csharp
var response = await dispatcher.SendAndWaitAsync(
    actorRef,
    new ControlMessage("query"),
    timeout: TimeSpan.FromSeconds(10)
);

if (response is ResponseMessage rm)
{
    Console.WriteLine($"Response: {rm.Data}");
}
```

## MailboxService

Manages message queues for actors.

```csharp
public interface IMailboxService
{
    Task EnqueueAsync(ActorRef actor, Message message, MessagePriority priority = MessagePriority.Normal);
    Task<Message?> DequeueAsync(ActorRef actor, TimeSpan timeout);
    int GetQueueSize(ActorRef actor);
    Task ClearQueueAsync(ActorRef actor);
}
```

### MessagePriority

```csharp
public enum MessagePriority
{
    Low = 0,
    Normal = 1,
    High = 2
}
```

### Methods

#### EnqueueAsync()

Adds a message to an actor's mailbox.

```csharp
await mailbox.EnqueueAsync(actorRef, message, MessagePriority.High);
```

#### DequeueAsync()

Retrieves the next message from mailbox.

```csharp
var message = await mailbox.DequeueAsync(actorRef, timeout: TimeSpan.FromSeconds(5));
```

#### GetQueueSize()

Returns current mailbox size.

```csharp
int size = mailbox.GetQueueSize(actorRef);
Console.WriteLine($"Queue size: {size}");
```

## SupervisionService

Handles failure recovery.

```csharp
public interface ISupervisionService
{
    Task ApplySupervisionStrategyAsync(ActorRef actor, Exception exception);
    void RegisterSupervisionHandler(Func<ActorRef, Exception, Task> handler);
}
```

### Methods

#### ApplySupervisionStrategyAsync()

Applies the configured supervision strategy.

```csharp
try
{
    // Processing
}
catch (Exception ex)
{
    await supervision.ApplySupervisionStrategyAsync(actorRef, ex);
}
```

## Persistence APIs

### ActorStatePersistence

```csharp
public interface IActorStatePersistence
{
    Task SaveSnapshotAsync(ActorSnapshot snapshot);
    Task<ActorSnapshot?> GetLatestSnapshotAsync(ActorPath path);
    Task<List<ActorSnapshot>> GetSnapshotsAsync(ActorPath path, 
        int maxCount = 10);
    Task DeleteSnapshotAsync(ActorPath path, DateTime timestamp);
}
```

### MessagePersistenceRepository

```csharp
public interface IMessagePersistenceRepository
{
    Task AppendMessageAsync(Envelope envelope);
    Task<List<Envelope>> GetMessagesAsync(ActorPath path, 
        DateTime from, DateTime to);
    Task<List<Envelope>> GetMessagesByIdAsync(params Guid[] messageIds);
    Task DeleteMessagesAsync(ActorPath path, DateTime before);
}
```

### ActorMetricsRepository

```csharp
public interface IActorMetricsRepository
{
    Task RecordMetricsAsync(ActorMetrics metrics);
    Task<ActorMetrics?> GetMetricsAsync(Guid actorId);
    Task<List<ActorMetrics>> GetMetricsAsync(ActorPath path);
}
```

## Message Classes

### Message (Base)

```csharp
public abstract record Message
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public ActorRef? Sender { get; init; }
    public string? CorrelationId { get; init; }
}
```

### ControlMessage

```csharp
public record ControlMessage(
    string Command,
    Dictionary<string, object>? Parameters = null
) : Message;

// Usage
var msg = new ControlMessage("process", new Dictionary<string, object>
{
    { "input", "data" },
    { "timeout", 30 }
});
```

### ResponseMessage

```csharp
public record ResponseMessage(
    object? Data,
    bool IsSuccess,
    string? Error = null
) : Message;

// Success response
var success = new ResponseMessage(result, isSuccess: true);

// Error response
var error = new ResponseMessage(null, isSuccess: false, 
    error: "Processing failed");
```

### FailureMessage

```csharp
public record FailureMessage(
    string Reason,
    Exception? Exception = null
) : Message;

// Usage
var failure = new FailureMessage("Database error", exception);
```

## Actor Base Class

```csharp
public abstract class Actor
{
    // Properties
    public Guid Id { get; }
    public ActorRef Ref { get; }
    public ActorPath Path { get; }
    public ActorState State { get; }
    public ActorMetrics Metrics { get; }
    public DateTime CreatedAt { get; }
    public DateTime? TerminatedAt { get; }
    public ActorRef? Supervisor { get; }
    
    // Constructor
    public Actor(ActorPath path, ActorRef? supervisor = null);
    
    // Lifecycle methods (override as needed)
    public virtual Task OnInitializeAsync() => Task.CompletedTask;
    public abstract Task ReceiveAsync(Message message);
    public virtual Task OnStopAsync() => Task.CompletedTask;
    public virtual Task OnErrorAsync(Exception ex) => Task.CompletedTask;
    
    // State management
    public void SetState(string key, object value);
    public object? GetState(string key);
    public Dictionary<string, object> GetAllState();
}
```

## ActorState Enum

```csharp
public enum ActorState
{
    Created = 0,        // Initial state
    Initializing = 1,   // OnInitializeAsync running
    Started = 2,        // Ready to process messages
    Suspended = 3,      // Temporarily paused
    Stopping = 4,       // OnStopAsync running
    Terminated = 5,     // Shut down
    Error = 6          // Error state
}
```

## SupervisionStrategy Enum

```csharp
public enum SupervisionStrategy
{
    Restart = 0,    // Restart the actor
    Stop = 1,       // Terminate the actor
    Resume = 2,     // Continue without restart
    Escalate = 3,   // Delegate to parent
    Backoff = 4     // Restart with exponential backoff
}
```

## Configuration Classes

### ActorSystemOptions

```csharp
public class ActorSystemOptions
{
    public string SystemName { get; set; } = "ActorSystem";
    public int MaxActorCount { get; set; } = 10000;
    public int MaxMessageQueueSize { get; set; } = 100000;
    
    public bool EnableMessagePersistence { get; set; } = false;
    public string? ConnectionString { get; set; }
    
    public SupervisionStrategy DefaultSupervisionStrategy { get; set; } = 
        SupervisionStrategy.Restart;
    public int BackoffInitialDelayMs { get; set; } = 100;
    public int BackoffMaxDelayMs { get; set; } = 30000;
    
    public bool EnableMetricsCollection { get; set; } = true;
    public bool EnableLogging { get; set; } = true;
    public int MetricsFlushIntervalMs { get; set; } = 5000;
}
```

## Statistics Classes

### SystemStatistics

```csharp
public class SystemStatistics
{
    public HealthSummary? Health { get; set; }
    public ActorRegistryStatistics? ActorRegistryStats { get; set; }
    public DispatcherStatistics? DispatcherStats { get; set; }
    public MailboxStatistics? MailboxStats { get; set; }
    public SupervisionStatistics? SupervisionStats { get; set; }
}
```

### HealthSummary

```csharp
public class HealthSummary
{
    public int TotalActors { get; set; }
    public int RunningActors { get; set; }
    public int TerminatedActors { get; set; }
    public int ErroredActors { get; set; }
    public int SuspendedActors { get; set; }
    
    public double GetHealthPercentage();
    public double GetErrorRate();
}
```

### DispatcherStatistics

```csharp
public class DispatcherStatistics
{
    public long TotalProcessed { get; set; }
    public long TotalFailed { get; set; }
    public double SuccessRate { get; set; }
    public double AverageLatency { get; set; }
    public double P95Latency { get; set; }
    public double P99Latency { get; set; }
}
```

## Extension Methods

### ActorPathExtensions

```csharp
public static class ActorPathExtensions
{
    public static ActorPath Parent(this ActorPath path);
    public static ActorPath Child(this ActorPath path, string name);
    public static bool IsAncestorOf(this ActorPath parent, ActorPath child);
    public static string GetName(this ActorPath path);
    public static string[] GetSegments(this ActorPath path);
}
```

## Error Classes

```csharp
public class ActorException : Exception { }
public class ActorNotFoundException : ActorException { }
public class MailboxException : ActorException { }
public class SupervisionException : ActorException { }
public class ActorSystemException : ActorException { }
public class ActorStateException : ActorException { }
public class MessageSerializationException : ActorException { }
```

## Service Registration

### Extension Methods

```csharp
public static class ServiceCollectionExtensions
{
    // Default configuration
    public static IServiceCollection AddActorFramework(
        this IServiceCollection services,
        Action<ActorSystemOptions>? configure = null);
    
    // High performance configuration
    public static IServiceCollection AddActorFrameworkHighPerformance(
        this IServiceCollection services);
    
    // Reliable (durable) configuration
    public static IServiceCollection AddActorFrameworkReliable(
        this IServiceCollection services,
        string connectionString);
    
    // Cluster configuration
    public static IServiceCollection AddActorFrameworkCluster(
        this IServiceCollection services,
        Action<ClusterOptions>? configure = null);
}
```
