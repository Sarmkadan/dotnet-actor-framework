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