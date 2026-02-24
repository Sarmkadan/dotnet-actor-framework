// ... (rest of the file remains unchanged)

## WebhookConfig

The `WebhookConfig` represents a configuration for a webhook endpoint. It defines how and when to dispatch events to an external service.

### Usage Example

```csharp
var webhookConfig = new WebhookConfig
{
  Url = "https://example.com/webhooks",
  EventType = "order.placed",
  IsActive = true,
  MaxRetries = 3,
  RetryDelay = TimeSpan.FromSeconds(5)
};

var webhookDispatcher = new WebhookDispatcher();
webhookDispatcher.RegisterWebhook(webhookConfig);
```

## IRemoteActorInvoker

The `IRemoteActorInvoker` interface provides the ability to invoke actors in remote systems across distributed environments. It enables communication between actors running in different processes or on different machines through HTTP-based remote calls. The interface supports both request-response patterns (`InvokeAsync`) and fire-and-forget messaging (`SendAsync`), along with health checking (`PingAsync`) to verify remote actor availability.



### Usage Example

```csharp
// Create an HTTP remote actor invoker pointing to the remote system
var remoteInvoker = new HttpRemoteActorInvoker("https://remote-actor-system:5000");

// Register a remote actor endpoint
remoteInvoker.RegisterRemoteActor("order-processor", "https://remote-system/actors/order-processor");

// Send a message to a remote actor (fire-and-forget)
await remoteInvoker.SendAsync("order-processor", new ProcessOrderMessage { OrderId = 123 });

// Invoke a remote actor and wait for response
var result = await remoteInvoker.InvokeAsync<OrderResult>(
    "order-processor",
    new GetOrderStatus { OrderId = 123 },
    TimeSpan.FromSeconds(30)
);

// Check if remote actor is reachable
var isReachable = await remoteInvoker.PingAsync("order-processor");
```

### Circuit Breaker Pattern

The `RemoteActorCircuitBreaker` class helps prevent cascading failures by tracking call failures and temporarily blocking calls to unhealthy remote actors. When the failure threshold is reached, subsequent calls are rejected until either the timeout expires or successful calls resume.


```csharp
var circuitBreaker = new RemoteActorCircuitBreaker(failureThreshold: 5, timeout: TimeSpan.FromMinutes(2));

// Track successful calls
circuitBreaker.RecordSuccess("order-processor");

// Track failed calls
circuitBreaker.RecordFailure("order-processor");

// Check if calls are allowed
if (circuitBreaker.CanCall("order-processor"))
{
    // Safe to make remote call
    await remoteInvoker.InvokeAsync<OrderResult>("order-processor", new GetOrderStatus { OrderId = 123 });
}
```

### Properties and Methods

- **HttpRemoteActorInvoker**: HTTP-based implementation of `IRemoteActorInvoker`
- **RegisterRemoteActor(string actorPath, string httpUrl)**: Registers a remote actor endpoint
- **InvokeAsync<T>(string remoteActorPath, Message message, TimeSpan? timeout)**: Invokes a remote actor and waits for response
- **SendAsync(string remoteActorPath, Message message)**: Sends a message without waiting for response
- **PingAsync(string remoteActorPath)**: Checks if remote actor is reachable
- **Dispose()**: Disposes the HTTP client
- **Result**: The result of the remote call (in `RemoteCallResult<T>`)
- **IsSuccess**: Whether the remote call succeeded
- **ErrorMessage**: Error message if the call failed
- **ElapsedMilliseconds**: Duration of the remote call in milliseconds
- **RemoteActorCircuitBreaker**: Circuit breaker for preventing cascading failures
- **RecordSuccess(string remoteActorPath)**: Records a successful call
- **RecordFailure(string remoteActorPath)**: Records a failed call
- **CanCall(string remoteActorPath)**: Checks if calls to the remote actor should be allowed
- **IsOpen**: Whether the circuit breaker is currently open
- **FailureCount**: Number of consecutive failures
- **OpenedAt**: When the circuit breaker was opened
- **LastSuccessAt**: When the last successful call occurred
