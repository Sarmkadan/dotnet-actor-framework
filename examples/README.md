# DotNet Actor Framework - Examples

This directory contains practical examples demonstrating various features and patterns of the DotNet Actor Framework.

## Overview

| Example | Topic | Difficulty | Description |
|---------|-------|------------|-------------|
| [01-hello-world.cs](01-hello-world.cs) | Basics | Beginner | Simple actor creation and message sending |
| [02-request-response.cs](02-request-response.cs) | Communication | Beginner | Request-response patterns between actors |
| [03-supervision.cs](03-supervision.cs) | Fault-Tolerance | Intermediate | Supervision strategies and error recovery |
| [04-stateful-actor.cs](04-stateful-actor.cs) | State Management | Intermediate | Actors maintaining and managing state |
| [05-metrics-monitoring.cs](05-metrics-monitoring.cs) | Monitoring | Intermediate | System health monitoring and metrics collection |
| [06-batch-processing.cs](06-batch-processing.cs) | Optimization | Advanced | Message batching for throughput optimization |
| [07-parent-child-hierarchy.cs](07-parent-child-hierarchy.cs) | Architecture | Advanced | Supervised hierarchies and work distribution |
| [08-basic-usage.cs](08-basic-usage.cs) | Basics | Beginner | Minimal setup and first actor initialization |
| [09-advanced-usage.cs](09-advanced-usage.cs) | Configuration | Intermediate | Custom options, middleware, and metrics |
| [10-integration-example.cs](10-integration-example.cs) | Integration | Intermediate | Wiring into ASP.NET DI |

## Running the Examples

Each example is a standalone C# file that can be compiled and executed independently.

### Prerequisites

- .NET 10 SDK or later
- DotNetActorFramework NuGet package

### Installation

```bash
# Option 1: Using NuGet
dotnet add package DotNetActorFramework

# Option 2: Build from source
cd ..
dotnet build src/DotNetActorFramework/DotNetActorFramework.csproj -c Release
```

### Compilation and Execution

```bash
# Compile a single example
csc -reference:$(find ~/.nuget/packages -name 'DotNetActorFramework*.dll' | head -1) examples/01-hello-world.cs

# Or use dotnet directly
dotnet run --project examples/01-hello-world.cs

# Compile all examples
for file in *.cs; do
  dotnet build-project "$file"
done
```

## Example Descriptions

### 1. Hello World (Beginner)

**Topics**: Basic actor creation, message sending, system initialization

**What You'll Learn:**
- How to set up dependency injection
- Creating and initializing the actor system
- Creating your first actor
- Sending messages to actors

**Key Code:**
```csharp
var path = new ActorPath("/user/hello");
var helloRef = await config.CreateActorAsync(path);

var message = new ControlMessage("greet", new Dictionary<string, object> { { "name", "Alice" } });
await dispatcher.SendAsync(helloRef, message);
```

### 2. Request-Response (Beginner)

**Topics**: Bidirectional communication, message handling

**What You'll Learn:**
- Implementing request-response patterns
- Handling different message types
- Error handling in actors
- Basic metrics recording

**Key Code:**
```csharp
public override async Task ReceiveAsync(Message message)
{
    if (message is ControlMessage cm)
    {
        var result = cm.Command switch
        {
            "add" => (int)cm.Parameters["a"] + (int)cm.Parameters["b"],
            "multiply" => (int)cm.Parameters["a"] * (int)cm.Parameters["b"],
            // ...
        };
    }
}
```

### 3. Supervision (Intermediate)

**Topics**: Fault tolerance, supervision strategies, error recovery

**What You'll Learn:**
- Configuring supervision strategies
- Handling actor failures
- Automatic recovery mechanisms
- Supervisor-worker patterns

**Key Code:**
```csharp
services.AddActorFramework(options =>
{
    options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
    options.BackoffInitialDelayMs = 100;
});
```

### 4. Stateful Actor (Intermediate)

**Topics**: State management, actor lifecycle, persistent state

**What You'll Learn:**
- Maintaining state within actors
- Lifecycle hooks (initialization, cleanup)
- State transitions and updates
- Account/counter examples

**Key Code:**
```csharp
private decimal _balance = 0m;

public override async Task ReceiveAsync(Message message)
{
    if (message is ControlMessage cm)
    {
        switch (cm.Command)
        {
            case "deposit":
                _balance += (decimal)cm.Parameters["amount"];
                break;
        }
    }
}
```

### 5. Metrics & Monitoring (Intermediate)

**Topics**: System observability, health monitoring, performance metrics

**What You'll Learn:**
- Collecting and analyzing metrics
- System health monitoring
- Performance tracking
- Real-time metrics reporting

**Key Code:**
```csharp
var health = config.GetHealthSummary();
var stats = await config.GetStatisticsAsync();

Console.WriteLine($"Actors: {health.TotalActors}");
Console.WriteLine($"Avg Latency: {stats.DispatcherStats?.AverageLatency}ms");
```

### 6. Batch Processing (Advanced)

**Topics**: Performance optimization, message batching, throughput

**What You'll Learn:**
- Implementing efficient batching strategies
- Periodic flushing mechanisms
- Throughput optimization
- Timer-based operations

**Key Code:**
```csharp
private readonly List<Message> _batch = new();

public override async Task ReceiveAsync(Message message)
{
    _batch.Add(message);
    if (_batch.Count >= _batchSize)
    {
        await FlushBatchAsync();
    }
}
```

### 7. Parent-Child Hierarchy (Advanced)

**Topics**: Actor hierarchies, supervision trees, work distribution

**What You'll Learn:**
- Creating hierarchical actor structures
- Parent-child relationships
- Delegated work distribution
- Actor shutdown ordering

**Key Code:**
```csharp
public override async Task OnInitializeAsync()
{
    for (int i = 0; i < 3; i++)
    {
        var workerPath = new ActorPath($"{Path}/worker-{i}");
        var workerRef = await ActorSystem.CreateActorAsync(workerPath, Ref);
        _workers.Add(workerRef);
    }
}
```

### 8. Basic Usage (Beginner)

**Topics**: Minimal setup, actor initialization

**What You'll Learn:**
- Minimal actor system creation
- Defining actor paths
- Basic initialization patterns

**Key Code:**
```csharp
var builder = new ActorSystemBuilder("SystemName");
var system = builder.Build();
```

### 9. Advanced Usage (Intermediate)

**Topics**: Custom configuration, middleware, metrics, caching

**What You'll Learn:**
- Configuring complex middleware pipelines
- Enabling metrics and caching
- Customizing error handling

**Key Code:**
```csharp
builder
    .WithLogging()
    .WithRateLimiting(500)
    .WithMetrics();
```

### 10. Integration Example (Intermediate)

**Topics**: Dependency injection, ASP.NET Core integration

**What You'll Learn:**
- Wiring the actor framework into DI
- Configuring the system via options delegates

**Key Code:**
```csharp
services.AddActorFramework(options =>
{
    options.DefaultMailboxCapacity = 1000;
});
```

## Common Patterns

### Error Handling

```csharp
try
{
    // Processing
}
catch (Exception ex)
{
    Metrics.RecordError(ex);
    throw; // Supervision will handle recovery
}
```

### State Initialization

```csharp
public override async Task OnInitializeAsync()
{
    // Setup resources
    await Task.CompletedTask;
}

public override async Task OnStopAsync()
{
    // Cleanup resources
    await Task.CompletedTask;
}
```

### Message Dispatching

```csharp
var dispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();
var message = new ControlMessage("command", parameters);
await dispatcher.SendAsync(actorRef, message);
```

## Learning Path

**For Beginners:**
1. Start with Example 1 (Hello World)
2. Move to Example 2 (Request-Response)
3. Explore Example 4 (Stateful Actor)

**For Intermediate:**
4. Study Example 3 (Supervision)
5. Learn from Example 5 (Metrics)

**For Advanced:**
6. Understand Example 6 (Batch Processing)
7. Master Example 7 (Hierarchies)

## Next Steps

After exploring these examples:

1. **Read the Documentation**
   - Start with [Getting Started](../docs/getting-started.md)
   - Review [Architecture Guide](../docs/architecture.md)
   - Check [API Reference](../docs/api-reference.md)

2. **Build Your Own**
   - Create custom actors for your use case
   - Implement specific supervision strategies
   - Integrate with your existing systems

3. **Deploy**
   - Follow [Deployment Guide](../docs/deployment.md)
   - Use Docker for containerization
   - Set up clustering if needed

## Troubleshooting

### Compilation Issues

```bash
# Ensure NuGet package is installed
dotnet add package DotNetActorFramework

# Or reference the local project
csc -reference:../src/DotNetActorFramework/bin/Release/net10.0/DotNetActorFramework.dll
```

### Runtime Issues

- Check that all dependencies are installed
- Verify .NET 10 is installed: `dotnet --version`
- Run with debug logging: Set `DOTNET_LOG_LEVEL=Debug`

### Performance

- Examples run best on systems with 4+ CPU cores
- Reduce iteration counts if system is slow
- Check system resources while running

## Contributing

Have improvements to these examples? Submit a pull request with:
- Clear description of changes
- Updated comments if needed
- Test output showing it works

## See Also

- [README.md](../README.md) - Project overview
- [Examples in GitHub](https://github.com/Sarmkadan/dotnet-actor-framework/tree/main/examples)
- [Live Demos](https://sarmkadan.com) - More complex examples
