# DotNet Actor Framework

A lightweight, production-ready actor model framework for .NET with mailboxes, supervision trees, clustering support, and message persistence.

## Features

- **Actor Model**: Lightweight actors with mailbox-based message processing
- **Supervision**: Configurable supervision strategies (Restart, Stop, Resume, Escalate, Backoff)
- **Message Persistence**: Durable message storage and replay capabilities
- **Metrics Collection**: Comprehensive performance tracking and health monitoring
- **Actor State Management**: Persist and restore actor state snapshots
- **Clustering**: Support for distributed actor systems
- **Type-Safe Messages**: Strongly-typed message handling with inheritance support
- **Dependency Injection**: Full Microsoft.Extensions.DependencyInjection integration
- **High Performance**: Optimized mailbox implementation with priority support

## Installation

```bash
dotnet add package DotNetActorFramework
```

## Quick Start

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Setup dependency injection
var services = new ServiceCollection();
services.AddActorFramework(options =>
{
    options.SystemName = "MyActorSystem";
    options.EnableMessagePersistence = true;
});

var serviceProvider = services.BuildServiceProvider();

// Initialize actor system
var configuration = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(serviceProvider);
var actorSystem = await configuration.InitializeAsync();

// Create actors
var actorPath = new ActorPath("/user/myactor");
var actorRef = await configuration.CreateActorAsync(actorPath);

// Send messages
var message = new ControlMessage("process");
await messageDispatcher.SendAsync(actorRef, message);
```

## Configuration

### Default Configuration
```csharp
services.AddActorFramework();
```

### High Performance
```csharp
services.AddActorFrameworkHighPerformance();
```

### Reliable (Durable)
```csharp
services.AddActorFrameworkReliable("connection-string");
```

### Cluster Mode
```csharp
services.AddActorFrameworkCluster("127.0.0.1:8080");
```

## Architecture

### Core Components

- **ActorSystem**: Main system coordinator managing all actors
- **Actor**: Individual actor instances with state and lifecycle management
- **ActorRef**: Reference to an actor that can be used to send messages
- **Message**: Type-safe message payload abstraction
- **Envelope**: Message wrapper with sender/recipient metadata
- **Mailbox**: FIFO message queue per actor

### Services

- **ActorRegistry**: Actor lifecycle and lookup management
- **MailboxService**: Message mailbox management
- **MessageDispatcher**: Message routing and delivery
- **SupervisionService**: Failure handling and recovery
- **ActorStateRepository**: Actor state persistence
- **MessagePersistenceRepository**: Message log storage
- **ActorMetricsRepository**: Performance metrics storage

### Configuration

- **ActorSystemOptions**: Configurable system parameters
- **ActorSystemConfiguration**: System initialization coordinator
- **DependencyInjectionSetup**: DI container configuration

## Message Types

### Control Messages
```csharp
var controlMessage = new ControlMessage("initialize", 
    new Dictionary<string, object> { { "param", "value" } });
```

### Typed Messages
```csharp
public record MyMessage : Message<MyPayload> { }
```

### Failure Messages
```csharp
var failureMessage = new FailureMessage("reason", exception);
```

### Response Messages
```csharp
var response = new ResponseMessage(data, isSuccess: true);
```

## Supervision Strategies

- **Restart**: Restart the failed actor
- **Stop**: Terminate the actor without restarting
- **Resume**: Continue operation, ignoring the failure
- **Escalate**: Forward failure to parent supervisor
- **Backoff**: Retry with exponential backoff delay

## Monitoring & Metrics

```csharp
// Get system health
var health = configuration.GetHealthSummary();
Console.WriteLine($"Health: {health.GetHealthPercentage()}%");
Console.WriteLine($"Error Rate: {health.GetErrorRate()}%");

// Get comprehensive statistics
var stats = configuration.GetStatistics();
Console.WriteLine($"Actors: {stats.Health?.TotalActors}");
Console.WriteLine($"Messages: {stats.DispatcherStats?.TotalProcessed}");
Console.WriteLine($"Success Rate: {stats.DispatcherStats?.SuccessRate}%");
```

## Actor Lifecycle

1. **Created**: Actor instantiated but not initialized
2. **Initializing**: OnInitializeAsync() is being called
3. **Started**: Actor ready to process messages
4. **Stopping**: OnStopAsync() is being called
5. **Terminated**: Actor shut down and unavailable
6. **Error**: Actor encountered an error
7. **Suspended**: Actor temporarily suspended

## Actor Paths

Actors are organized in a hierarchical path structure:
- `/user` - User-created actors
- `/system` - System actors
- `/user/parent/child` - Parent-child relationships

## Error Handling

The framework provides comprehensive error handling:

- **ActorException**: Base exception for all actor errors
- **ActorNotFoundException**: Actor not found in registry
- **MailboxException**: Mailbox operation failure
- **SupervisionException**: Supervision strategy failure
- **ActorSystemException**: System-level errors

## Performance Characteristics

- **Mailbox**: Lock-free ConcurrentQueue with semaphore-based capacity
- **Registry**: O(1) path lookup with hierarchical index
- **Supervision**: Configurable backoff with exponential delay
- **Metrics**: Lock-based aggregation with minimal overhead

## Building from Source

```bash
git clone https://github.com/sarmkadan/dotnet-actor-framework.git
cd dotnet-actor-framework
dotnet build
```

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

## Author

**Vladyslav Zaiets**
- Website: https://sarmkadan.com
- CTO & Software Architect
