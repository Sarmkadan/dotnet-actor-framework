# IRemoteActorInvoker

The `IRemoteActorInvoker` interface defines the contract for executing remote procedure calls against actors hosted on external endpoints within the `dotnet-actor-framework`. It provides mechanisms for synchronous registration, asynchronous invocation with typed results, fire-and-forget messaging, and health checking via ping operations. Additionally, it integrates a circuit breaker pattern to manage fault tolerance, exposing detailed execution metrics such as elapsed time, success status, and error messaging to facilitate robust distributed system interactions.

## API

### Core Invocation Members

#### `RegisterRemoteActor`
Registers a specific actor instance or type mapping within the invoker's internal routing table.
*   **Parameters**: Implementation-specific parameters defining the actor identity and target endpoint.
*   **Return Value**: `void`.
*   **Exceptions**: May throw if the actor ID is already registered or if the configuration is invalid.

#### `InvokeAsync<T>`
Executes a remote method call and waits for a typed response.
*   **Parameters**: Accepts arguments required to identify the target actor and the method payload (specifics depend on implementation).
*   **Return Value**: `Task<T?>` containing the deserialized result of the remote operation, or `null` if no result is returned.
*   **Exceptions**: Throws if the network request fails, the remote actor throws an exception, or the circuit breaker is open.

#### `SendAsync`
Sends a message to a remote actor without expecting a return value (fire-and-forget).
*   **Parameters**: Accepts arguments defining the target actor and the message payload.
*   **Return Value**: `Task` that completes when the message has been successfully dispatched.
*   **Exceptions**: Throws if the message cannot be serialized or if the transport layer fails immediately.

#### `PingAsync`
Verifies the connectivity and liveness of the remote actor endpoint.
*   **Parameters**: None (context inferred from the invoker state).
*   **Return Value**: `Task<bool>` indicating whether the remote endpoint responded successfully.
*   **Exceptions**: Throws on network timeouts or unreachable hosts.

#### `Dispose`
Releases unmanaged resources and closes active network connections associated with the invoker.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Exceptions**: Should not throw; implementations must handle internal cleanup errors gracefully.

### Execution Result Properties
*These properties typically reflect the state of the most recent operation or the current context.*

#### `Result`
Gets the result object of the last executed operation.
*   **Type**: `T?`
*   **Remarks**: Returns `null` if the last operation failed or returned no data.

#### `IsSuccess`
Indicates whether the last operation completed without errors.
*   **Type**: `bool`
*   **Remarks**: `true` if the operation succeeded; `false` otherwise.

#### `ErrorMessage`
Contains the error message string if the last operation failed.
*   **Type**: `string?`
*   **Remarks**: `null` if `IsSuccess` is `true`.

#### `ElapsedMilliseconds`
Reports the duration of the last operation in milliseconds.
*   **Type**: `long`
*   **Remarks**: Useful for performance monitoring and latency analysis.

### Circuit Breaker Members
*These members manage and expose the state of the `RemoteActorCircuitBreaker` to prevent cascading failures.*

#### `RecordSuccess`
Notifies the circuit breaker that a recent call succeeded, potentially resetting the failure count or closing an open circuit.
*   **Parameters**: None.
*   **Return Value**: `void`.

#### `RecordFailure`
Notifies the circuit breaker that a recent call failed, incrementing the failure count and potentially opening the circuit.
*   **Parameters**: None.
*   **Return Value**: `void`.

#### `CanCall`
Determines if a new call is permitted based on the current circuit breaker state.
*   **Type**: `bool`
*   **Remarks**: Returns `false` if the circuit is open and the timeout has not elapsed.

#### `IsOpen`
Indicates whether the circuit breaker is currently in the "Open" state, blocking all requests.
*   **Type**: `bool`

#### `FailureCount`
Gets the current number of consecutive failures recorded.
*   **Type**: `int`

#### `OpenedAt`
Gets the timestamp when the circuit breaker transitioned to the Open state.
*   **Type**: `DateTime`

#### `LastSuccessAt`
Gets the timestamp of the last successful operation.
*   **Type**: `DateTime?`
*   **Remarks**: `null` if no successful operations have occurred yet.

## Usage

### Example 1: Typed Invocation with Circuit Breaker Handling
This example demonstrates invoking a remote method to retrieve data, explicitly checking the circuit breaker state before attempting the call and handling potential failures.

```csharp
using System;
using System.Threading.Tasks;
using DotNetActorFramework;

public class OrderService
{
    private readonly IRemoteActorInvoker _invoker;

    public OrderService(IRemoteActorInvoker invoker)
    {
        _invoker = invoker;
    }

    public async Task<decimal?> GetOrderTotalAsync(string orderId)
    {
        // Check circuit breaker state before invoking
        if (!_invoker.CanCall)
        {
            Console.WriteLine($"Circuit is open. Last failure count: {_invoker.FailureCount}");
            return null;
        }

        try
        {
            var result = await _invoker.InvokeAsync<decimal?>("OrderActor", "GetTotal", orderId);
            
            if (_invoker.IsSuccess)
            {
                _invoker.RecordSuccess();
                return _invoker.Result;
            }
            else
            {
                _invoker.RecordFailure();
                Console.WriteLine($"Remote call failed: {_invoker.ErrorMessage}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _invoker.RecordFailure();
            Console.WriteLine($"Exception during invocation: {ex.Message}");
            throw;
        }
    }
}
```

### Example 2: Fire-and-Forget with Health Check
This example shows sending a notification message without waiting for a result, followed by a periodic ping to verify endpoint health.

```csharp
using System;
using System.Threading.Tasks;
using DotNetActorFramework;

public class NotificationDispatcher
{
    private readonly IRemoteActorInvoker _invoker;

    public NotificationDispatcher(IRemoteActorInvoker invoker)
    {
        _invoker = invoker;
    }

    public async Task DispatchAlertAsync(string alertMessage)
    {
        if (!_invoker.CanCall)
        {
            Console.WriteLine("Skipping dispatch: Circuit breaker is open.");
            return;
        }

        try
        {
            await _invoker.SendAsync("AlertActor", "Notify", alertMessage);
            
            // Assuming SendAsync updates internal state upon completion
            if (_invoker.IsSuccess)
            {
                _invoker.RecordSuccess();
                Console.WriteLine($"Alert sent in {_invoker.ElapsedMilliseconds}ms");
            }
            else
            {
                _invoker.RecordFailure();
            }
        }
        catch
        {
            _invoker.RecordFailure();
            throw;
        }
    }

    public async Task<bool> CheckHealthAsync()
    {
        try
        {
            bool isAlive = await _invoker.PingAsync();
            if (isAlive)
            {
                _invoker.RecordSuccess();
            }
            else
            {
                _invoker.RecordFailure();
            }
            return isAlive;
        }
        catch
        {
            _invoker.RecordFailure();
            return false;
        }
    }
}
```

## Notes

*   **Thread Safety**: The properties reflecting execution state (`Result`, `IsSuccess`, `ErrorMessage`, `ElapsedMilliseconds`) are likely not thread-safe for concurrent read/write access across different logical operations unless the implementation explicitly synchronizes access. In multi-threaded scenarios, capture these values immediately after an `await` completes before yielding control.
*   **Circuit Breaker State Consistency**: The `RecordSuccess` and `RecordFailure` methods must be called explicitly by the consumer after every operation attempt to maintain accurate `FailureCount` and `IsOpen` states. Failure to call these methods will result in the circuit breaker state becoming stale, potentially allowing requests through a failing endpoint or blocking a recovered one.
*   **Disposable Pattern**: As `IRemoteActorInvoker` implements `Dispose`, consumers are responsible for disposing of the instance when it is no longer needed to ensure network sockets are closed cleanly. Do not invoke methods like `InvokeAsync` or `SendAsync` after `Dispose` has been called.
*   **Nullability**: The generic return type `T?` and `ErrorMessage` utilize nullable reference types. Consumers should check `IsSuccess` before accessing `Result` or `ErrorMessage` to avoid null reference exceptions, as `Result` will be `null` on failure and `ErrorMessage` will be `null` on success.
*   **Time Precision**: `ElapsedMilliseconds` provides a `long` integer representation of time. For high-frequency trading or ultra-low latency requirements, note that this granularity may not capture sub-millisecond variations. `OpenedAt` and `LastSuccessAt` use `DateTime`, so ensure consistent time zone handling if comparing these values across distributed nodes.
