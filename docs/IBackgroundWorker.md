# IBackgroundWorker

Provides an abstraction for managing a background worker within the actor framework, allowing registration, lifecycle control, and observation of execution statistics.

## API

### RegisterWorker
**Purpose**  
Registers the worker with the hosting environment so it can be started and managed.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` – if the worker is already registered.  
- `ObjectDisposedException` – if the instance has been disposed.

### UnregisterWorker
**Purpose**  
Unregisters the worker, preventing further starts until it is registered again.

**Parameters**  
None.

**Return value**  
`true` if the worker was successfully unregistered; `false` if it was not registered.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### StartAsync
**Purpose**  
Begins execution of the worker asynchronously.

**Parameters**  
None.

**Return value**  
A `Task` that completes when the worker has started running.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.  
- `InvalidOperationException` – if the worker is not registered or is already running.

### StopAsync
**Purpose**  
Requests asynchronous cessation of the worker’s execution.

**Parameters**  
None.

**Return value**  
A `Task` that completes when the worker has stopped.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.  
- `InvalidOperationException` – if the worker is not running.

### GetWorkerStatus
**Purpose**  
Retrieves the current status of the worker.

**Parameters**  
None.

**Return value**  
A nullable `WorkerStatus` enum indicating the state (e.g., `Running`, `Stopped`, `Faulted`). Returns `null` if the status cannot be determined.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### Dispose
**Purpose**  
Releases all resources used by the worker.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- None (calling Dispose multiple times is safe).

### Worker
**Purpose**  
Provides access to the underlying worker instance (often the same object).

**Parameters**  
None.

**Return value**  
An `IBackgroundWorker` reference.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### Task
**Purpose**  
Gets the `Task` that represents the worker’s execution, if any.

**Parameters**  
None.

**Return value**  
A `Task` when the worker is running; otherwise `null`.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### LastExecutedAt
**Purpose**  
Gets the timestamp of the most recent successful execution.

**Parameters**  
None.

**Return value**  
A nullable `DateTime`; `null` if the worker has never executed successfully.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### ExecutionCount
**Purpose**  
Gets the total number of successful executions performed by the worker.

**Parameters**  
None.

**Return value**  
A `long` count.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### ErrorCount
**Purpose**  
Gets the total number of errors encountered during worker execution.

**Parameters**  
None.

**Return value**  
A `long` count.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### LastError
**Purpose**  
Gets the message of the most recent error, if any.

**Parameters**  
None.

**Return value**  
A nullable `string` containing the error message; `null` if no error has occurred.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### WorkerTask
**Purpose**  
Gets the delegate or task definition that the worker executes.

**Parameters**  
None.

**Return value**  
A `WorkerTask` instance representing the work to be performed.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### WorkerId
**Purpose**  
Gets a unique identifier for the worker instance.

**Parameters**  
None.

**Return value**  
A `string` that uniquely identifies the worker.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

### IsRunning
**Purpose**  
Indicates whether the worker is currently executing.

**Parameters**  
None.

**Return value**  
`true` if the worker is running; otherwise `false`.

**Exceptions**  
- `ObjectDisposedException` – if the instance has been disposed.

## Usage

### Example 1: Basic lifecycle management
```csharp
using System;
using System.Threading.Tasks;
using DotNetActorFramework;

public class SampleService : IDisposable
{
    private readonly IBackgroundWorker _worker;

    public SampleService(IBackgroundWorker worker)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _worker.RegisterWorker(); // register with the host
    }

    public async Task StartProcessingAsync()
    {
        if (!_worker.IsRunning)
        {
            await _worker.StartAsync();
        }
    }

    public async Task StopProcessingAsync()
    {
        if (_worker.IsRunning)
        {
            await _worker.StopAsync();
        }
    }

    public void Dispose()
    {
        _worker.UnregisterWorker();
        _worker.Dispose();
    }
}
```

### Example 2: Monitoring execution statistics
```csharp
using System;
using System.Threading.Tasks;
using DotNetActorFramework;

public class WorkerMonitor
{
    private readonly IBackgroundWorker _worker;

    public WorkerMonitor(IBackgroundWorker worker)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
    }

    public async Task LogStatusAsync()
    {
        var status = _worker.GetWorkerStatus;
        Console.WriteLine($"Worker {_worker.WorkerId} status: {status ?? "Unknown"}");

        Console.WriteLine($"Last executed: {_worker.LastExecutedAt ?? DateTime.MinValue}");
        Console.WriteLine($"Execution count: {_worker.ExecutionCount}");
        Console.WriteLine($"Error count: {_worker.ErrorCount}");
        if (_worker.LastError != null)
        {
            Console.WriteLine($"Last error: {_worker.LastError}");
        }

        if (_worker.IsRunning)
        {
            Console.WriteLine("Worker is currently running.");
        }
        else
        {
            Console.WriteLine("Worker is idle.");
        }
    }
}
```

## Notes
- The `RegisterWorker` and `UnregisterWorker` methods are not thread‑safe; concurrent calls may result in undefined behavior. External synchronization is required if they are invoked from multiple threads.
- `StartAsync` and `StopAsync` should not be called concurrently; invoking `StartAsync` while a previous start is pending, or `StopAsync` while a stop is pending, may throw `InvalidOperationException`.
- All property getters are safe to invoke after the worker has been disposed; they will throw `ObjectDisposedException`. While the worker is running, reading properties such as `ExecutionCount` or `LastExecutedAt` may return stale values because updates occur asynchronously. For a consistent snapshot, consider locking around reads or using `Interlocked` reads if the underlying implementation provides them.
- The `Dispose` method may be called multiple times without effect; after disposal, any further interaction with the instance (including property access) will raise `ObjectDisposedException`.
- The `Worker` property typically returns the same instance on which it is accessed, but implementers may return a proxy or wrapper; consumers should not assume identity equality.
- `WorkerTask` represents the delegate or task that performs the unit of work; modifying it after registration has no effect on an already‑running worker. Changes should be made before calling `RegisterWorker` or while the worker is stopped.
