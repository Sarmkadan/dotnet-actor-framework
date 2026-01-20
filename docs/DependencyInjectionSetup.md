# DependencyInjectionSetup

The `DependencyInjectionSetup` static class provides extension methods for integrating the dotnet-actor-framework into an application’s dependency injection container. Each method registers the core actor system services on an `IServiceCollection` instance, enabling different runtime characteristics (default, high‑performance, reliable, or cluster‑aware). The `ConfigureActorFramework` method allows further customization of the registered services after the initial setup.

## API

### `AddActorFramework`

```csharp
public static IServiceCollection AddActorFramework(this IServiceCollection services)
```

Registers the default actor framework services, including actor activation, message dispatch, and lifecycle management. This is the standard entry point for most applications.

- **Parameters**  
  `services` – The `IServiceCollection` to which actor services are added.

- **Returns**  
  The same `IServiceCollection` instance, enabling method chaining.

- **Throws**  
  `ArgumentNullException` if `services` is `null`.

### `AddActorFrameworkHighPerformance`

```csharp
public static IServiceCollection AddActorFrameworkHighPerformance(this IServiceCollection services)
```

Registers actor services optimized for throughput and low latency. This variant may use lock‑free data structures, reduced logging, or alternative scheduling strategies.

- **Parameters**  
  `services` – The `IServiceCollection` to which actor services are added.

- **Returns**  
  The same `IServiceCollection` instance, enabling method chaining.

- **Throws**  
  `ArgumentNullException` if `services` is `null`.

### `AddActorFrameworkReliable`

```csharp
public static IServiceCollection AddActorFrameworkReliable(this IServiceCollection services)
```

Registers actor services with enhanced reliability guarantees, such as automatic retries, persistent actor state, and supervision strategies.

- **Parameters**  
  `services` – The `IServiceCollection` to which actor services are added.

- **Returns**  
  The same `IServiceCollection` instance, enabling method chaining.

- **Throws**  
  `ArgumentNullException` if `services` is `null`.

### `AddActorFrameworkCluster`

```csharp
public static IServiceCollection AddActorFrameworkCluster(this IServiceCollection services)
```

Registers actor services configured for distributed deployment across multiple nodes. Includes cluster membership, remote actor communication, and partition management.

- **Parameters**  
  `services` – The `IServiceCollection` to which actor services are added.

- **Returns**  
  The same `IServiceCollection` instance, enabling method chaining.

- **Throws**  
  `ArgumentNullException` if `services` is `null`.

### `ConfigureActorFramework`

```csharp
public static IServiceCollection ConfigureActorFramework(this IServiceCollection services)
```

Provides a hook to apply additional configuration to the actor framework services after they have been registered. This method is typically called after one of the `AddActorFramework*` methods.

- **Parameters**  
  `services` – The `IServiceCollection` containing the registered actor services.

- **Returns**  
  The same `IServiceCollection` instance, enabling method chaining.

- **Throws**  
  `ArgumentNullException` if `services` is `null`.

## Usage

### Example 1: Basic actor system setup

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework;

var services = new ServiceCollection();
services.AddActorFramework();

var serviceProvider = services.BuildServiceProvider();
// Use serviceProvider to resolve actor system components.
```

### Example 2: High‑performance configuration with custom options

```csharp
using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework;

var services = new ServiceCollection();
services.AddActorFrameworkHighPerformance();
services.ConfigureActorFramework();

var serviceProvider = services.BuildServiceProvider();
// The actor system is now optimized for throughput.
```

## Notes

- All methods are extension methods on `IServiceCollection` and are intended to be called during application startup, typically within a single‑threaded composition root. They are not thread‑safe and should not be invoked concurrently.
- Calling multiple `AddActorFramework*` methods on the same `IServiceCollection` is not supported and may result in duplicate or conflicting service registrations. Choose exactly one variant per application.
- The `ConfigureActorFramework` method assumes that at least one `AddActorFramework*` method has been called beforehand. Calling it without a prior registration will not throw, but no configuration will be applied.
- If the `services` parameter is `null`, all methods throw `ArgumentNullException`.
