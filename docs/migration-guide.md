# Migration Guide

This document covers breaking changes and migration steps between major versions of the DotNet Actor Framework.

## Migrating to v2.x from v1.x

### Breaking Changes

#### 1. Actor base class - virtual method signatures

The `ReceiveAsync` method has been replaced with `OnReceiveAsync` to better reflect the lifecycle semantics:

```csharp
// v1.x
public class MyActor : Actor
{
    public override Task ReceiveAsync(Message message) { ... }
}

// v2.x
public class MyActor : Actor
{
    protected override Task OnReceiveAsync(Message message) { ... }
}
```

All lifecycle hooks now use the `On` prefix consistently:
- `OnInitializeAsync()` (unchanged)
- `OnReceiveAsync()` (renamed from `ReceiveAsync`)
- `OnErrorAsync()` (new - replaces manual try/catch in receive)
- `OnStopAsync()` (unchanged)

#### 2. Message records replace classes

Messages are now `record` types instead of `class` types. This provides built-in value equality and `with` expression support:

```csharp
// v1.x
var msg = new ControlMessage("process");

// v2.x - same construction, but you can now use 'with'
var msg = new ControlMessage("process");
var copy = msg with { Priority = 5 };
```

#### 3. DI registration API

The `AddActorSystem` extension method has been renamed to `AddActorFramework` with new overloads:

```csharp
// v1.x
services.AddActorSystem(options => { ... });

// v2.x
services.AddActorFramework(options => { ... });
services.AddActorFrameworkHighPerformance();
services.AddActorFrameworkReliable("connection-string");
services.AddActorFrameworkCluster(options => { ... });
```

#### 4. Envelope is now required for dispatch

Direct `SendAsync(ActorRef, Message)` still works, but internally all messages are wrapped in an `Envelope`. If you were using the dispatcher directly, update any code that bypasses the envelope:

```csharp
// v2.x - explicit envelope usage
var envelope = new Envelope(message, recipientRef, senderRef);
await dispatcher.DispatchAsync(envelope);
```

### New Features in v2.x

- **Middleware pipeline**: Register `IActorMiddleware` implementations for cross-cutting concerns (logging, metrics, auth, rate limiting)
- **Dead letter queue**: Undeliverable messages are captured in the dispatcher's dead letter queue
- **Health summaries**: `ActorSystem.GetHealthSummary()` provides aggregate system health
- **Message batching**: `MessageBatcher` utility for high-throughput scenarios

## Migrating to v1.x from v0.x

### Breaking Changes

#### 1. Namespace reorganization

All types have moved from `ActorFramework.*` to `DotNetActorFramework.*`:

```csharp
// v0.x
using ActorFramework.Models;

// v1.x
using DotNetActorFramework.Models;
```

#### 2. ActorPath is now a value object

`ActorPath` changed from a simple string wrapper to a hierarchical value object that supports parent-child relationships:

```csharp
// v0.x
var path = "/user/worker";

// v1.x
var path = new ActorPath("/user/worker");
bool isChild = path.IsDescendantOf(new ActorPath("/user"));
```

#### 3. Supervision is configured at the system level

Per-actor supervision has been replaced with system-wide supervision strategies configured through `ActorSystemOptions`:

```csharp
services.AddActorFramework(options =>
{
    options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
    options.BackoffInitialDelayMs = 100;
    options.BackoffMaxDelayMs = 30000;
});
```
