# Getting Started with DotNet Actor Framework

This guide walks you through creating your first actor system from scratch.

## Prerequisites

- .NET 10 SDK or later
- Basic C# knowledge
- Visual Studio, VS Code, or any text editor

## Installation

### Using NuGet

```bash
dotnet new console -n MyActorApp
cd MyActorApp
dotnet add package DotNetActorFramework
```

### From Source

```bash
git clone https://github.com/sarmkadan/dotnet-actor-framework.git
cd dotnet-actor-framework
dotnet build
```

## Your First Actor System

### Step 1: Create a Console Application

```bash
dotnet new console -n HelloActors
cd HelloActors
dotnet add package DotNetActorFramework
```

### Step 2: Configure Services

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

var services = new ServiceCollection();
services.AddActorFramework(options =>
{
    options.SystemName = "HelloActors";
});

var serviceProvider = services.BuildServiceProvider();
```

### Step 3: Initialize the System

```csharp
var configuration = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(
    serviceProvider);
var actorSystem = await configuration.InitializeAsync();

Console.WriteLine($"Actor system '{configuration.Options.SystemName}' started!");
```

### Step 4: Create Your First Actor

```csharp
public class GreeterActor : Actor
{
    public GreeterActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm)
        {
            if (cm.Command == "greet" && cm.Parameters != null)
            {
                var name = cm.Parameters.GetValueOrDefault("name", "World");
                Console.WriteLine($"Hello, {name}!");
            }
        }
        await Task.CompletedTask;
    }
}
```

### Step 5: Send Messages

```csharp
var dispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();

// Create the actor
var path = new ActorPath("/user/greeter");
var greeterRef = await configuration.CreateActorAsync(path);

// Send a message
var message = new ControlMessage("greet", new Dictionary<string, object>
{
    { "name", "Alice" }
});

await dispatcher.SendAsync(greeterRef, message);
```

### Step 6: Shutdown Gracefully

```csharp
Console.WriteLine("Shutting down actor system...");
await actorSystem.ShutdownAsync();
```

## Complete Example

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Actor definition
public class GreeterActor : Actor
{
    public GreeterActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "greet")
        {
            var name = cm.Parameters?.GetValueOrDefault("name", "World") ?? "World";
            Console.WriteLine($"Hello, {name}!");
        }
        await Task.CompletedTask;
    }
}

// Main program
var services = new ServiceCollection();
services.AddActorFramework(options =>
{
    options.SystemName = "HelloActors";
});

var sp = services.BuildServiceProvider();
var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
var system = await config.InitializeAsync();

var dispatcher = sp.GetRequiredService<MessageDispatcher>();
var path = new ActorPath("/user/greeter");
var greeterRef = await config.CreateActorAsync(path);

await dispatcher.SendAsync(greeterRef, new ControlMessage("greet", 
    new Dictionary<string, object> { { "name", "Alice" } }));

await system.ShutdownAsync();
```

## Understanding Actor Paths

Actor paths follow a hierarchical structure:

- `/user` - Root for user-created actors
- `/user/myactor` - Direct child of user root
- `/user/parent/child` - Nested hierarchy
- `/system` - System actors (internal use)

```csharp
// Create actors at different levels
var path1 = new ActorPath("/user/worker");
var path2 = new ActorPath("/user/supervisor/worker");

var ref1 = await config.CreateActorAsync(path1);
var ref2 = await config.CreateActorAsync(path2);
```

## Actor Initialization and Cleanup

Actors have lifecycle hooks for setup and teardown:

```csharp
public class DatabaseActor : Actor
{
    private DbConnection _connection;

    public DatabaseActor(ActorPath path) : base(path) : base(path) { }

    // Called when actor starts
    public override async Task OnInitializeAsync()
    {
        _connection = new DbConnection();
        await _connection.OpenAsync();
        Console.WriteLine("Database connection opened");
    }

    // Called when actor stops
    public override async Task OnStopAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
            Console.WriteLine("Database connection closed");
        }
    }

    public override async Task ReceiveAsync(Message message)
    {
        // Process messages using _connection
        await Task.CompletedTask;
    }
}
```

## Message Types

The framework provides several built-in message types:

### ControlMessage

For commands and general messages:

```csharp
var msg = new ControlMessage("command", new Dictionary<string, object>
{
    { "param1", "value1" },
    { "param2", 42 }
});
```

### ResponseMessage

For request-response patterns:

```csharp
var response = new ResponseMessage(data: result, isSuccess: true);
var error = new ResponseMessage(data: null, isSuccess: false, 
    error: "Something went wrong");
```

### FailureMessage

For propagating exceptions:

```csharp
var failure = new FailureMessage("Processing failed", ex);
```

### Custom Messages

Define your own strongly-typed messages:

```csharp
public record OrderMessage(string OrderId, decimal Amount, string CustomerId) 
    : Message;

public record PaymentMessage(string TransactionId, decimal Amount, 
    string Status) : Message;
```

## Monitoring Your System

Check system health and metrics:

```csharp
// Get health summary
var health = config.GetHealthSummary();
Console.WriteLine($"Running actors: {health.RunningActors}");
Console.WriteLine($"Health percentage: {health.GetHealthPercentage()}%");

// Get detailed statistics
var stats = await config.GetStatisticsAsync();
Console.WriteLine($"Messages processed: {stats.DispatcherStats?.TotalProcessed}");
Console.WriteLine($"Average latency: {stats.DispatcherStats?.AverageLatency}ms");
```

## Common Patterns

### Request-Response

```csharp
public class Calculator : Actor
{
    public Calculator(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm)
        {
            int result = (int)cm.Parameters["a"] + (int)cm.Parameters["b"];
            // Send response
        }
        await Task.CompletedTask;
    }
}
```

### State Management

```csharp
public class Counter : Actor
{
    private int _count = 0;

    public Counter(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm)
        {
            switch (cm.Command)
            {
                case "inc":
                    _count++;
                    break;
                case "dec":
                    _count--;
                    break;
            }
        }
        await Task.CompletedTask;
    }
}
```

## Next Steps

- Read [Architecture Guide](architecture.md) for deep dive
- Explore [API Reference](api-reference.md)
- Check [examples/](../examples/) directory for more samples
- Review [Deployment Guide](deployment.md) for production setup
