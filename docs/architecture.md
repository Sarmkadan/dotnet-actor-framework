# Architecture Guide

## System Architecture Overview

The DotNet Actor Framework follows a modular, layered architecture designed for scalability and maintainability.

```
┌──────────────────────────────────────────────────────────────┐
│                      Application Layer                        │
│                   (Your Actor Code)                           │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                    Framework Facade Layer                     │
│              (ActorSystem, Configuration)                     │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                    Service Layer                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ActorRegistry │  │MessageDispatcher│ActorCacheService    │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │MailboxService│  │SupervisionService│  EventBus   │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                   Middleware Layer                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │LoggingMiddleware │MetricsMiddleware│AuthMiddleware│      │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
│  ┌──────────────┐  ┌──────────────┐                         │
│  │RateLimitMiddleware│ErrorHandlingMiddleware│              │
│  └──────────────┘  └──────────────┘                         │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│                  Persistence Layer                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │StateRepository │MessageRepository│MetricsRepository    │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└──────────────────────────────────────────────────────────────┘
                              ↓
┌──────────────────────────────────────────────────────────────┐
│              Infrastructure Layer                             │
│              (Databases, Network)                            │
└──────────────────────────────────────────────────────────────┘
```

## Core Components

### ActorSystem

The top-level coordinator and entry point for the framework.

**Responsibilities:**
- Lifecycle management (start, shutdown)
- Delegation to subsystems
- Health monitoring
- Statistics aggregation

**Key Methods:**
```csharp
public class ActorSystem
{
    public Task StartAsync();
    public Task ShutdownAsync();
    public ActorRef? GetActor(ActorPath path);
    public IEnumerable<ActorRef> GetAllActors();
    public Task<SystemStatistics> GetStatisticsAsync();
    public HealthSummary GetHealthSummary();
}
```

### Actor (Base Class)

The fundamental unit of concurrency and state.

**Characteristics:**
- Owns isolated state (thread-safe via synchronization)
- Processes messages sequentially
- Has defined lifecycle states
- Can have parent-child relationships
- Generates metrics

**Lifecycle:**
```
Created → Initializing → Started ⟷ Suspended
                          ↓
                      Stopping → Terminated
                          ↑
                        Error
```

**Key Methods:**
```csharp
public abstract class Actor
{
    public Guid Id { get; }
    public ActorRef Ref { get; }
    public ActorPath Path { get; }
    public ActorState State { get; }
    public ActorMetrics Metrics { get; }

    public virtual async Task OnInitializeAsync() { }
    public abstract Task ReceiveAsync(Message message);
    public virtual async Task OnStopAsync() { }
}
```

### ActorRegistry

Manages actor creation, lookup, and lifecycle.

**Responsibilities:**
- Create actors with proper initialization
- Maintain hierarchical index of actors
- Handle actor termination
- Garbage collection of terminated actors
- Performance: O(1) path-based lookup

**Implementation Details:**
```csharp
public class ActorRegistry
{
    private readonly ConcurrentDictionary<string, ActorRef> _index;
    
    // Fast path-based lookup
    public ActorRef? GetActorByPath(ActorPath path);
    
    // Hierarchical queries
    public IEnumerable<ActorRef> GetActorsByPath(ActorPath parentPath);
}
```

### MailboxService

Message queue management per actor.

**Characteristics:**
- FIFO ordering guarantee
- Lock-free ConcurrentQueue implementation
- Semaphore-based capacity management
- Priority support (High/Normal/Low)
- Backpressure handling

**Design:**
```csharp
public class MailboxService
{
    private readonly Dictionary<ActorRef, Mailbox> _mailboxes;
    
    public async Task EnqueueAsync(ActorRef actor, Message message);
    public async Task<Message?> DequeueAsync(ActorRef actor, TimeSpan timeout);
    public int GetQueueSize(ActorRef actor);
}
```

### MessageDispatcher

Routes and delivers messages to their destinations.

**Responsibilities:**
- Message routing logic
- Sender/recipient tracking
- Request-response correlation
- Timeout handling
- Metrics collection

**Process:**
```
Input Message
    ↓
[Validate]
    ↓
[Apply Middleware]
    ↓
[Route]
    ↓
[Enqueue to Mailbox]
    ↓
[Wait for Processing]
    ↓
[Record Metrics]
```

### SupervisionService

Handles failure detection and recovery strategies.

**Strategies:**
- **Restart**: Automatic actor restart with delay
- **Stop**: Terminate without recovery
- **Resume**: Ignore failure, continue processing
- **Escalate**: Delegate to parent supervisor
- **Backoff**: Exponential backoff retry

**State Transitions:**
```
Processing Message
    ↓
[Exception Thrown]
    ↓
[SupervisionService Invoked]
    ↓
    ├─→ [Restart] → Delay → Initialize → Started
    ├─→ [Stop] → Stopping → Terminated
    ├─→ [Resume] → Started (continue)
    ├─→ [Escalate] → Parent Supervisor
    └─→ [Backoff] → Suspended → Delay → Restart
```

### Persistence Layer

Handles durable storage of state and messages.

**Repositories:**
- **ActorStateRepository**: Snapshot-based state persistence
- **MessagePersistenceRepository**: Durable message log
- **ActorMetricsRepository**: Aggregated metrics storage

**Event Sourcing Pattern:**
```csharp
public class PersistenceService
{
    // Save state snapshots
    public async Task SaveSnapshotAsync(ActorSnapshot snapshot);
    
    // Recover from snapshots
    public async Task<ActorSnapshot?> GetLatestSnapshotAsync(ActorPath path);
    
    // Persist messages
    public async Task AppendMessageAsync(Envelope message);
    
    // Query message log
    public async Task<List<Message>> GetMessagesAsync(ActorPath path, 
        DateTime from, DateTime to);
}
```

### Middleware Pipeline

Intercepts messages for cross-cutting concerns.

**Built-in Middleware:**

1. **LoggingMiddleware**: Logs all message activity
2. **MetricsCollectionMiddleware**: Collects performance metrics
3. **AuthenticationMiddleware**: Validates message senders
4. **RateLimitingMiddleware**: Enforces message rate limits
5. **ErrorHandlingMiddleware**: Wraps processing with try-catch

**Pipeline Order:**
```
Message
    ↓
[LoggingMiddleware]
    ↓
[AuthenticationMiddleware]
    ↓
[RateLimitingMiddleware]
    ↓
[MetricsCollectionMiddleware]
    ↓
[ErrorHandlingMiddleware]
    ↓
[Actor.ReceiveAsync()]
```

## Message Flow

### Send Message Flow

```csharp
var message = new ControlMessage("process");
await dispatcher.SendAsync(actorRef, message);
```

**Execution Path:**
```
1. MessageDispatcher.SendAsync(actorRef, message)
2. Create Envelope { Message, Sender, Recipient }
3. For each middleware in pipeline:
   - PreProcess(envelope)
4. MailboxService.EnqueueAsync(actorRef, envelope)
5. For each registered message listener:
   - Notify of enqueued message
6. Wait for actor to process or timeout
7. For each middleware in pipeline:
   - PostProcess(envelope, result)
8. Return to caller
```

### Receive Message Flow

```csharp
public override async Task ReceiveAsync(Message message)
{
    // Processing logic
}
```

**Execution Path:**
```
1. MailboxService.DequeueAsync(actorRef, timeout)
2. Retrieve message from queue
3. Call Actor.ReceiveAsync(message)
4. Update metrics
5. If exception:
   - SupervisionService.ApplySupervisionStrategyAsync(ex)
6. Continue to next message
```

## Concurrency Model

### Thread Safety

**Actor State**
- Protected by internal lock
- Only accessed from actor's mailbox processing thread
- No concurrent access allowed

**Shared Resources**
- ActorRegistry: ConcurrentDictionary (lock-free read)
- Mailboxes: ConcurrentQueue (lock-free)
- Metrics: Interlocked operations

### Message Ordering

Within a single actor:
- Messages processed sequentially
- FIFO order guaranteed
- No concurrent message processing

Across actors:
- No ordering guarantee between different actors
- Parent and child process independently

## Performance Characteristics

### Time Complexity

| Operation | Complexity | Notes |
|-----------|------------|-------|
| Send Message | O(1) | Direct enqueue |
| Create Actor | O(log n) | Hierarchical lookup |
| Get Actor | O(1) | Path-based hash lookup |
| Terminate Actor | O(1) | Mark and remove |

### Space Complexity

| Component | Space | Notes |
|-----------|-------|-------|
| ActorRegistry | O(n) | n = number of actors |
| Mailbox (per actor) | O(m) | m = queue size |
| Metrics | O(1) | Fixed-size stats |

### Memory Characteristics

- **Per-Actor Overhead**: ~2KB (metrics, state dict, mailbox ref)
- **Per-Message Overhead**: ~512 bytes (envelope, metadata)
- **Typical Throughput**: 10k-100k messages/second/core

## Clustering Architecture

### Node Communication

```
Node 1 (ActorSystem)
    ↓ (Network)
Node 2 (ActorSystem)
    ↓ (Network)
Node 3 (ActorSystem)
```

### Remote Actor References

```csharp
// Local actor
var path = new ActorPath("/user/local");
var ref = await config.CreateActorAsync(path);

// Remote actor reference
var remotePath = new ActorPath("/user/remote@node-2");
var remoteRef = await config.ResolveActorAsync(remotePath);

// Transparent message sending
await dispatcher.SendAsync(remoteRef, message);
// Framework handles serialization and network routing
```

### Consistency Guarantees

- **At-Most-Once**: Default delivery semantics
- **Local Strong Consistency**: Within single node
- **Eventual Consistency**: Across cluster

## State Management Patterns

### Stateless Actors

```csharp
public class StatelessActor : Actor
{
    public override async Task ReceiveAsync(Message message)
    {
        // No state modification
        // Idempotent operations
    }
}
```

### Stateful Actors

```csharp
public class StatefulActor : Actor
{
    private Dictionary<string, object> _state = new();

    public override async Task ReceiveAsync(Message message)
    {
        // Modify _state
        // Non-idempotent operations
    }
}
```

### Event-Sourced Actors

```csharp
public class EventSourcedActor : Actor
{
    private List<object> _events = new();

    public override async Task ReceiveAsync(Message message)
    {
        // Record event
        _events.Add(message);
        
        // Rebuild state from events
        await RebuildStateAsync();
    }
}
```

## Dependency Injection Integration

The framework integrates seamlessly with Microsoft.Extensions.DependencyInjection:

```csharp
services.AddActorFramework();
services.AddScoped<MyService>();
services.AddTransient<DatabaseClient>();

// Services available in actor constructors
public class MyActor : Actor
{
    private readonly MyService _service;
    
    public MyActor(ActorPath path, MyService service) : base(path)
    {
        _service = service;
    }
}
```

## Extension Points

### Custom Middleware

```csharp
public class CustomMiddleware : IActorMiddleware
{
    public async Task<EnvelopeProcessingResult> ProcessAsync(
        Envelope envelope, Func<Envelope, Task<EnvelopeProcessingResult>> next)
    {
        // Pre-processing
        var result = await next(envelope);
        // Post-processing
        return result;
    }
}

services.AddActorFramework(options =>
{
    options.Middleware.Add(typeof(CustomMiddleware));
});
```

### Custom Supervision

```csharp
public class CustomSupervisionStrategy : SupervisionStrategy
{
    public override async Task<SupervisionDecision> DecideAsync(
        ActorRef actor, Exception exception)
    {
        // Custom recovery logic
        return SupervisionDecision.Restart(delay: 1000);
    }
}
```

### Custom Persistence

Implement `IActorRepository` for custom storage backends:

```csharp
public class CustomRepository : IActorRepository
{
    public async Task SaveAsync(ActorPath path, object state);
    public async Task<object?> LoadAsync(ActorPath path);
}
```

## Configuration Layers

1. **Default**: Balanced for typical use cases
2. **HighPerformance**: Optimized for throughput
3. **Reliable**: Emphasizes durability
4. **Cluster**: Multi-node distributed setup
5. **Custom**: Full control over all options

## Design Principles

1. **Simplicity**: Core concepts are straightforward
2. **Composability**: Small, focused components
3. **Observability**: Built-in monitoring and diagnostics
4. **Reliability**: Fault-tolerance through supervision
5. **Performance**: Minimal overhead, lock-free where possible
6. **Flexibility**: Extensive customization points
