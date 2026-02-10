# WebhookConfigExtensions

The `WebhookConfigExtensions` static class provides extension methods for the `WebhookConfig` type. These methods enable a fluent, declarative style for configuring webhook behavior—specifically toggling activation, attaching a retry policy, and querying the age of a configuration. All methods are designed to be used in a chain, returning a new or modified `WebhookConfig` instance.

## API

### `Activate`

```csharp
public static WebhookConfig Activate(this WebhookConfig config)
```

Sets the webhook configuration to an active state.  
**Parameters:**  
- `config` – The `WebhookConfig` instance to activate. Must not be `null`.  

**Returns:** A `WebhookConfig` instance with the activation flag set to `true`. If the original instance is immutable, a new instance is returned; otherwise the same instance may be mutated.  

**Throws:**  
- `ArgumentNullException` – if `config` is `null`.

---

### `Deactivate`

```csharp
public static WebhookConfig Deactivate(this WebhookConfig config)
```

Sets the webhook configuration to an inactive state.  
**Parameters:**  
- `config` – The `WebhookConfig` instance to deactivate. Must not be `null`.  

**Returns:** A `WebhookConfig` instance with the activation flag set to `false`.  

**Throws:**  
- `ArgumentNullException` – if `config` is `null`.

---

### `WithRetryPolicy`

```csharp
public static WebhookConfig WithRetryPolicy(this WebhookConfig config, RetryPolicy policy)
```

Attaches a retry policy to the webhook configuration.  
**Parameters:**  
- `config` – The `WebhookConfig` instance. Must not be `null`.  
- `policy` – A `RetryPolicy` object defining the retry behavior (e.g., number of attempts, backoff strategy). Must not be `null`.  

**Returns:** A `WebhookConfig` instance with the specified retry policy applied.  

**Throws:**  
- `ArgumentNullException` – if `config` or `policy` is `null`.

---

### `GetAge`

```csharp
public static TimeSpan GetAge(this WebhookConfig config)
```

Returns the elapsed time since the webhook configuration was created or last reset.  
**Parameters:**  
- `config` – The `WebhookConfig` instance to query. Must not be `null`.  

**Returns:** A `TimeSpan` representing the age of the configuration.  

**Throws:**  
- `ArgumentNullException` – if `config` is `null`.

## Usage

### Example 1: Activate a webhook with a retry policy

```csharp
using ActorFramework.Webhooks;

var config = new WebhookConfig("https://example.com/hook")
    .Activate()
    .WithRetryPolicy(new RetryPolicy(3, TimeSpan.FromSeconds(5)));

Console.WriteLine($"Webhook active: {config.IsActive}");   // true
Console.WriteLine($"Retry attempts: {config.RetryPolicy.MaxAttempts}"); // 3
```

### Example 2: Deactivate a webhook and check its age

```csharp
using ActorFramework.Webhooks;

var config = new WebhookConfig("https://example.com/hook");
Thread.Sleep(2000); // simulate time passing

var age = config.GetAge();
Console.WriteLine($"Age: {age.TotalSeconds:F1}s"); // ~2.0s

config.Deactivate();
Console.WriteLine($"Webhook active: {config.IsActive}"); // false
```

## Notes

- **Null safety:** All extension methods throw `ArgumentNullException` if the `config` parameter is `null`. The `WithRetryPolicy` method also throws if `policy` is `null`. Always validate arguments before calling these methods.
- **Immutability:** The `WebhookConfig` type is designed as an immutable record or class. Therefore, `Activate`, `Deactivate`, and `WithRetryPolicy` return new instances rather than modifying the original. The original instance remains unchanged after any of these calls.
- **Thread safety:** Because the methods are static and the underlying `WebhookConfig` is immutable, these extension methods are inherently thread-safe. Multiple threads can safely call them on the same or different instances without synchronization. However, if `WebhookConfig` were mutable, concurrent calls would require external locking.
- **Age calculation:** `GetAge` relies on a timestamp stored internally when the `WebhookConfig` is constructed. If the configuration is later reactivated or modified, the age is not reset unless explicitly documented otherwise. The returned `TimeSpan` is based on the system clock (`DateTime.UtcNow`), so it is subject to clock skew and daylight saving time adjustments.
