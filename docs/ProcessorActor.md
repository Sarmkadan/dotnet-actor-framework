# ProcessorActor

`ProcessorActor` is a concrete actor class that processes messages asynchronously. It inherits from a base actor class and provides overrides for lifecycle and message handling. Each instance is identified by an `ActorPath` and can optionally be associated with a `MonitorActor` for supervision. The actor runtime invokes `ReceiveAsync` to deliver messages, and the lifecycle methods `OnInitializeAsync` and `OnStopAsync` are called when the actor starts and stops, respectively.

## API

### `ProcessorActor(ActorPath path)`

Initializes a new instance of the `ProcessorActor` class.

- **Parameters**  
  `path` – The `ActorPath` that uniquely identifies this actor. Must not be `null`.

- **Returns**  
  Nothing.

- **Throws**  
  `ArgumentNullException` if `path` is `null`.

### `public override async Task ReceiveAsync`

Processes the next message from the actor’s mailbox. This method is called by the actor runtime when a message is available. The default implementation is empty; override it to define message handling logic.

- **Parameters**  
  None.

- **Returns**  
  A `Task` that completes when message processing is finished.

- **Throws**  
  Any exception thrown by the message handling logic will be propagated to the actor’s supervision strategy.

### `public MonitorActor MonitorActor`

Gets the `MonitorActor` instance associated with this processor actor. The monitor actor is used for supervision and health tracking. This property is set externally (e.g., during actor system configuration) and may be `null` if no monitor is assigned.

- **Type**  
  `MonitorActor`

- **Returns**  
  The current `MonitorActor` reference, or `null`.

### `public override async Task OnInitializeAsync`

Called once when the actor is initialized, before any messages are processed. Override this method to perform asynchronous setup logic (e.g., opening connections, loading state).

- **Parameters**  
  None.

- **Returns**  
  A `Task` that completes when initialization is done.

- **Throws**  
  Any exception thrown during initialization will prevent the actor from starting and will be reported to the actor system.

### `public override async Task OnStopAsync`

Called when the actor is being stopped. Override this method to perform asynchronous cleanup (e.g., closing resources, persisting final state). The actor will not process further messages after this method completes.

- **Parameters**  
  None.

- **Returns**  
  A `Task` that completes when cleanup is finished.

- **Throws**  
  Exceptions thrown during stop are logged but do not prevent the actor from being removed.

## Usage

### Example 1: Simple message logging

```csharp
public class LoggingActor : ProcessorActor
{
    public LoggingActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync()
    {
        // Assume the runtime provides a current message via context
        var message = Context.CurrentMessage;
        Console.WriteLine($"[{Path}] Received: {message}");
        await Task.CompletedTask;
    }
}
```

### Example 2: Using MonitorActor for supervision

```csharp
public class SupervisedProcessor : ProcessorActor
{
    public SupervisedProcessor(ActorPath path) : base(path) { }

    public override async Task OnInitializeAsync()
    {
        if (MonitorActor != null)
        {
            await MonitorActor.RegisterAsync(this);
        }
    }

    public override async Task ReceiveAsync()
    {
        try
        {
            // Process message
            await Task.Delay(10);
        }
        catch (Exception ex)
        {
            MonitorActor?.ReportFailure(this, ex);
        }
    }

    public override async Task OnStopAsync()
    {
        MonitorActor?.Unregister(this);
        await Task.CompletedTask;
    }
}
```

## Notes

- The `ActorPath` passed to the constructor must not be `null`; doing so throws `ArgumentNullException`.
- `ReceiveAsync` is invoked by the actor runtime on a single-threaded context. It must not be called concurrently from user code. Override implementations should avoid blocking calls to preserve the actor’s responsiveness.
- `OnInitializeAsync` is called exactly once before any `ReceiveAsync` call. If it throws, the actor will not start and the exception is propagated to the actor system’s error handling.
- `OnStopAsync` is called exactly once when the actor is stopped. It is guaranteed to run after all pending `ReceiveAsync` invocations have completed. Exceptions thrown here are logged but do not prevent the actor from being removed.
- The `MonitorActor` property may be `null` if no monitor is configured. Always check for `null` before invoking its members.
- Thread safety is guaranteed by the actor model: all public methods (`ReceiveAsync`, `OnInitializeAsync`, `OnStopAsync`) are executed sequentially on the actor’s dedicated context. No additional synchronization is required inside these methods.
