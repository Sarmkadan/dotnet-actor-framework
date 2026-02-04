# Frequently Asked Questions

## General Questions

### What is the actor model?

The actor model is a programming paradigm for concurrent systems where actors are isolated units of computation that communicate exclusively through asynchronous message passing. Each actor has its own state and mailbox, processes messages sequentially, and can create child actors. This eliminates many concurrency issues inherent in shared-memory multithreading.

### When should I use the DotNet Actor Framework?

Use it for:
- Real-time data processing pipelines
- Distributed job processing
- Event-driven systems
- Systems requiring fault tolerance
- WebSocket/SignalR servers
- Long-running background workers
- Game servers
- Systems with many concurrent entities

### Can I use this in production?

Yes. The framework is designed for production use with features like supervision, persistence, clustering, and comprehensive monitoring. Follow the Deployment Guide for production best practices.

### How does it compare to other frameworks?

| Framework | Actor Model | Clustering | Persistence | .NET |
|-----------|------------|-----------|-------------|------|
| DotNetActorFramework | ✓ | ✓ | ✓ | Native |
| Proto.Actor | ✓ | ✓ | ✓ | Native |
| Orleans | ✓ (virtual) | ✓ | ✓ | Native |
| Akka.NET | ✓ | ✓ | ✓ | Native |

Key differences:
- **Lightweight**: Minimal dependencies, small footprint
- **Modern**: Built on .NET 10 with async/await throughout
- **Flexible**: Extensive customization points
- **Observable**: Built-in metrics and diagnostics

## Architecture Questions

### How are actors scheduled?

Actors are processed on thread pool threads. The framework:
1. Dequeues messages from mailbox
2. Calls actor's ReceiveAsync()
3. Continues with next message
4. No dedicated threads per actor (efficient)

### What's the difference between actors and tasks?

| Aspect | Actor | Task |
|--------|-------|------|
| State | Encapsulated | Shared |
| Communication | Messages | Direct method calls |
| Concurrency | Sequential per actor | Concurrent |
| Lifetime | Long-lived | Short-lived |
| Supervision | Built-in | Manual |

### Can actors call each other synchronously?

No. Communication is always asynchronous through messages. This prevents deadlocks and improves scalability.

To get synchronous-like behavior, use `SendAndWaitAsync()`:

```csharp
var response = await dispatcher.SendAndWaitAsync(actorRef, message, timeout);
```

### What happens if an actor throws an exception?

The exception is caught by the error handling middleware. The supervision strategy then determines the actor's recovery:
- **Restart**: Actor is restarted after delay
- **Stop**: Actor is terminated
- **Resume**: Exception is logged, processing continues
- **Escalate**: Parent supervisor handles it
- **Backoff**: Restart with exponential backoff

### Can actors have parent-child relationships?

Yes. When creating an actor, specify its supervisor:

```csharp
var childRef = await registry.CreateActorAsync(childPath, supervisorRef);
```

The parent receives notifications when children fail and can implement custom recovery logic.

## Message and Communication Questions

### What message types does the framework support?

Built-in types:
- **ControlMessage**: Generic commands with parameters
- **ResponseMessage**: Request-response pattern
- **FailureMessage**: Exception propagation
- **Custom messages**: Define your own

### How do I implement request-response?

Use `SendAndWaitAsync()`:

```csharp
var request = new ControlMessage("query");
var response = await dispatcher.SendAndWaitAsync(actorRef, request, 
    timeout: TimeSpan.FromSeconds(5));

if (response is ResponseMessage rm)
{
    Console.WriteLine($"Result: {rm.Data}");
}
```

### Can messages be lost?

By default, messages are lost if:
- Actor is terminated before processing
- System crashes before persistence

To prevent loss, enable message persistence:

```csharp
services.AddActorFrameworkReliable("connection-string");
```

### What's the throughput?

Typical throughput:
- **Single actor**: 10k-50k messages/sec
- **Multiple actors**: Scales linearly with CPU cores
- **With persistence**: 1k-5k messages/sec (database-dependent)

### What's the latency?

Typical latencies:
- **In-process**: <1ms p50, <5ms p95
- **Cross-process**: 1-10ms p50, 10-50ms p95
- **Network**: 10-100ms p50 (network-dependent)

## Configuration Questions

### What configuration preset should I use?

| Use Case | Preset |
|----------|--------|
| Development | Default |
| Low latency | HighPerformance |
| Critical data | Reliable |
| Distributed | Cluster |

### How do I configure persistence?

```csharp
// Using connection string
services.AddActorFrameworkReliable("Server=localhost;Database=ActorFramework");

// Or custom configuration
services.Configure<ActorSystemOptions>(options =>
{
    options.EnableMessagePersistence = true;
    options.ConnectionString = "...";
});
```

### Can I change configuration at runtime?

Limited changes are possible:
- Supervision strategy: Yes
- Message queue size: No (affects new actors only)
- Max actor count: No
- Persistence: No

It's recommended to set configuration before system starts.

### What's the default supervision strategy?

`SupervisionStrategy.Restart` with:
- Initial delay: 100ms
- Max delay: 30000ms (exponential backoff)

Override with:

```csharp
services.Configure<ActorSystemOptions>(options =>
{
    options.DefaultSupervisionStrategy = SupervisionStrategy.Stop;
});
```

## Persistence Questions

### How does persistence work?

1. Actor calls `SaveSnapshotAsync()` in `OnStopAsync()`
2. State is serialized and saved to database
3. On restart, snapshot is loaded in `OnInitializeAsync()`
4. Actor resumes from saved state

### Do I need to implement snapshots?

Not required, but recommended for:
- Stateful actors
- Long-running actors
- Critical state

### What if snapshot is old?

Load the latest snapshot and replay messages since that timestamp:

```csharp
public override async Task OnInitializeAsync()
{
    var snapshot = await persistence.GetLatestSnapshotAsync(Path);
    if (snapshot != null)
    {
        RestoreState(snapshot.State);
        
        // Replay messages since snapshot
        var since = snapshot.Timestamp;
        var messages = await persistence.GetMessagesAsync(Path, since, DateTime.UtcNow);
        foreach (var msg in messages)
        {
            await ProcessEventAsync(msg);
        }
    }
}
```

### Can I use a custom database?

Yes. Implement the persistence interfaces:

```csharp
public class CustomRepository : IActorRepository
{
    public async Task SaveAsync(ActorPath path, object state) { }
    public async Task<object?> LoadAsync(ActorPath path) { }
}
```

## Clustering Questions

### How does clustering work?

1. Nodes discover each other via seed nodes
2. Nodes exchange gossip about actor locations
3. Remote actors are transparently routed to their home node
4. Messages cross network boundaries automatically

### What's the overhead of clustering?

- CPU: ~5-10% for gossip protocol
- Memory: ~1KB per remote actor reference
- Network: ~1KB/sec per active actor

### How do I handle node failures?

Enable supervision and implement recovery:

```csharp
services.Configure<ActorSystemOptions>(options =>
{
    options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
});

// Actors automatically restart on other nodes after failure
```

### Can I do cross-cluster communication?

Not directly. Clusters are isolated systems. To communicate:

1. Use HTTP/gRPC between clusters
2. Implement external message bus
3. Use database-mediated communication

## Performance Questions

### How can I improve performance?

1. Use `AddActorFrameworkHighPerformance()` preset
2. Reduce persistence overhead (disable if not needed)
3. Optimize middleware (remove unnecessary middleware)
4. Increase message queue size
5. Profile with metrics collection

### What's the memory overhead per actor?

~2KB per actor for:
- Metadata (path, ID, state)
- Mailbox reference
- Metrics collector

With state, overhead depends on state size.

### Can I pool actors?

The framework doesn't provide built-in pooling, but you can implement it:

```csharp
public class ActorPool
{
    private Queue<ActorRef> _available = new();
    private Semaphore _semaphore;

    public async Task<ActorRef> LeaseAsync()
    {
        await _semaphore.WaitAsync();
        return _available.Dequeue();
    }

    public void Return(ActorRef actor)
    {
        _available.Enqueue(actor);
        _semaphore.Release();
    }
}
```

### How do I handle backpressure?

1. Use `TrySendAsync()` with timeout
2. Implement queue monitoring
3. Implement rate limiting middleware
4. Reduce message volume at source

## Troubleshooting Questions

### Actors aren't receiving messages

Check:
1. Actor state is `Started` (not `Suspended` or `Error`)
2. No exceptions in middleware
3. Message is actually being sent
4. Correct actor path/reference

Debug with logging:

```csharp
services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Debug);
});
```

### Memory is growing unbounded

Investigate:
1. Check for actor leaks (actors not terminating)
2. Monitor queue sizes with metrics
3. Reduce `MaxMessageQueueSize`
4. Implement periodic cleanup

```csharp
services.Configure<ActorSystemOptions>(options =>
{
    options.MaxMessageQueueSize = 50000;
});
```

### Database is full

Solutions:
1. Archive old messages: `DeleteMessagesAsync(path, before: oldDate)`
2. Implement message retention policy
3. Increase database size
4. Implement cleanup job

### Cluster node isn't joining

Check:
1. Network connectivity between nodes
2. Same cluster configuration
3. Seed node is reachable
4. Firewall allows port 8080

### Performance degrades over time

Possible causes:
1. Memory leak in actor state
2. Growing queue sizes
3. Database connection pool exhaustion
4. Garbage collection pauses

Monitor with metrics and profile.

## Security Questions

### Is the framework secure?

The framework provides:
- Message authentication middleware
- Actor path-based access control
- Connection security for clustering
- No remote code execution vulnerabilities

It doesn't provide:
- Encryption (implement in middleware)
- User authentication (implement separately)
- Network security (use firewalls/VPN)

### How do I authenticate messages?

Implement authentication middleware:

```csharp
public class AuthMiddleware : IActorMiddleware
{
    public async Task<EnvelopeProcessingResult> ProcessAsync(
        Envelope envelope, Func<Envelope, Task<EnvelopeProcessingResult>> next)
    {
        if (!ValidateAuthentication(envelope))
            throw new UnauthorizedAccessException();

        return await next(envelope);
    }
}
```

### Can I encrypt actor state?

Yes. Implement encryption in persistence layer:

```csharp
public class EncryptedRepository : IActorRepository
{
    public async Task SaveAsync(ActorPath path, object state)
    {
        var encrypted = Encrypt(state);
        await _innerRepository.SaveAsync(path, encrypted);
    }

    public async Task<object?> LoadAsync(ActorPath path)
    {
        var encrypted = await _innerRepository.LoadAsync(path);
        return Decrypt(encrypted);
    }
}
```

## License and Contributing

### What license is this under?

MIT License. Free for commercial and personal use.

### How do I contribute?

1. Fork the repository
2. Create feature branch
3. Write tests (aim for 80%+ coverage)
4. Submit pull request
5. Follow code style guidelines

See [Contributing](../README.md#contributing) section in README.

### Where do I report bugs?

Use GitHub Issues: https://github.com/Sarmkadan/dotnet-actor-framework/issues

Include:
- Minimal reproducible example
- .NET version
- OS/platform
- Framework version
- Expected vs actual behavior
