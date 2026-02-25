// ... (rest of the file remains unchanged)

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

### Properties

- `string Name { get; }`: Gets the unique name of the actor system.
- `Guid Id { get; }`: Gets the unique identifier of the actor system instance.
- `DateTime CreatedAt { get; }`: Gets the UTC timestamp when the system was initialized.
- `DateTime? ShutdownAt { get; }`: Gets the UTC timestamp when the system was shut down, if applicable.
- `bool IsRunning { get; }`: Gets a value indicating whether the actor system is currently running and accepting messages.

### Methods

- `ActorSystem(string name)`: Initializes a new instance of the `ActorSystem` class.
- `Task<ActorRef> CreateActorAsync(ActorPath path, ActorRef? supervisor = null)`: Creates and registers a new actor within the system.
- `ActorRef? GetActorRef(ActorPath path)`: Gets an actor reference by its path.
- `IReadOnlyList<ActorRef> GetActorsByParent(ActorPath parentPath)`: Gets all actor references for a given parent path.
- `IReadOnlyList<ActorRef> GetAllActors()`: Gets all registered actors.
- `Task TerminateActorAsync(ActorRef actorRef)`: Terminates an actor by its reference.
- `int GetActorCount()`: Gets the total number of actors in the system.
- `IReadOnlyList<ActorRef> GetErrorActors()`: Gets actors that are in an error state.
- `ActorMetricsSummary? GetActorMetricsSummary(ActorPath path)`: Gets the metrics summary for a specific actor.
- `SystemHealthSummary GetHealthSummary()`: Gets a health summary of all actors.
- `Task ShutdownAsync()`: Gracefully shuts down the actor system.
- `override string ToString()`: Returns a string representation of the actor system.

### Nested Types

- `SystemHealthSummary`: Summary of the actor system health with properties like `SystemId`, `SystemName`, `TotalActors`, `HealthyActors`, `UnhealthyActors`, `ErrorActors`, `TotalMessages`, `TotalErrors`, and methods like `GetHealthPercentage()`, `GetErrorRate()`, and `IsHealthy`.
