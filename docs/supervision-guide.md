# Supervision Trees and Restart Strategies

Effective error handling is crucial for building robust and resilient actor systems. The DotNet Actor Framework provides powerful supervision mechanisms that allow parent actors to define how they respond to failures in their child actors. This guide delves into the core concepts of supervision, available strategies, and best practices for configuring fault-tolerant actor hierarchies.

## 1. Understanding Supervision

In the actor model, child actors are supervised by their parent actors. When a child actor encounters an unhandled exception, it suspends its processing and reports the failure to its supervisor. The supervisor then decides how to handle the failure based on its configured **SupervisionStrategy**. This hierarchical approach ensures that failures are contained and managed locally, preventing cascading failures across the entire system.

Supervision promotes the "let it crash" philosophy: instead of trying to recover from every error within the actor itself, the actor can simply fail, and its supervisor will determine the appropriate recovery action.

## 2. Supervision Strategies in Detail

The framework offers several built-in supervision strategies, each suited for different failure scenarios:

### Escalate Strategy (`SupervisionStrategy.Escalate`)

*   **When to Use:** When a supervisor cannot meaningfully resolve a child's failure and needs to delegate the decision to its own supervisor. This is the default strategy.
*   **What it Does:** The failing child actor is stopped, and the failure is passed up the supervision hierarchy to the supervisor's supervisor. If there's no higher supervisor, the actor is stopped.
*   **Example:**
    ```csharp
    public class ParentActor : Actor
    {
        public ParentActor(ActorPath path) : base(path) { }

        protected override async Task OnReceiveAsync(Message message)
        {
            if (message is ControlMessage { Command: "create_child" })
            {
                // Child will escalate failures to this ParentActor
                var child = await Context.ActorSystem.CreateActorAsync(
                    new ActorPath(Path, "child1"),
                    Ref // This actor is the supervisor
                );
                // ParentActor would then need to handle the escalated failure from child1
            }
        }

        // ParentActor's OnErrorAsync will be invoked if a child escalates a failure
        protected override Task OnErrorAsync(Message message, Exception exception)
        {
            Console.WriteLine($"ParentActor received escalated error from child: {exception.Message}. Escalating further.");
            // Default behavior for ParentActor is to also escalate if not handled here.
            // If ParentActor doesn't define its own strategy for child failures,
            // it implicitly uses its system's default, often Escalate.
            return Task.CompletedTask; // In a real scenario, this would likely involve re-throwing or a specific action.
        }
    }
    ```

### Restart Strategy (`SupervisionStrategy.Restart`)

*   **When to Use:** For transient failures where restarting the actor (resetting its internal state) might resolve the issue. This is suitable for stateless workers or actors whose state can be easily reloaded.
*   **What it Does:** The failed child actor is stopped, a new instance of the child actor is created, and its state is reinitialized (e.g., via `OnInitializeAsync`). The new instance then replaces the old one. If an actor exceeds a configured restart threshold (default 5 restarts), the strategy automatically escalates.
*   **Example:**
    ```csharp
    public class WorkerActor : Actor
    {
        private int _taskCount = 0;
        public WorkerActor(ActorPath path) : base(path) { }

        protected override Task OnInitializeAsync()
        {
            _taskCount = 0; // State is reset on restart
            Console.WriteLine($"{Path} initialized/restarted. Task count reset.");
            return Task.CompletedTask;
        }

        protected override Task OnReceiveAsync(Message message)
        {
            _taskCount++;
            if (_taskCount % 3 == 0) // Simulate failure every 3 tasks
            {
                throw new InvalidOperationException("Simulated worker failure!");
            }
            Console.WriteLine($"{Path} processed task {_taskCount}.");
            return Task.CompletedTask;
        }
    }

    // Somewhere in the system setup:
    // systemBuilder.WithDefaultSupervisionStrategy(SupervisionStrategy.Restart);
    // var worker = await system.CreateActorAsync(new ActorPath("/user/worker"), supervisor: null);
    // // Send messages to worker; it will restart on every 3rd message.
    ```

### Stop Strategy (`SupervisionStrategy.Stop`)

*   **When to Use:** For non-recoverable failures that indicate a fundamental problem with the actor's logic or environment, or when an actor's failure should not affect its siblings.
*   **What it Does:** The failed child actor is permanently terminated without restarting. Its resources are released, and it will no longer process messages.
*   **Example:**
    ```csharp
    public class CriticalServiceActor : Actor
    {
        public CriticalServiceActor(ActorPath path) : base(path) { }

        protected override Task OnReceiveAsync(Message message)
        {
            // If a critical error occurs, stop this actor
            if (message is ControlMessage { Command: "critical_fail" })
            {
                throw new Exception("Critical unrecoverable error!");
            }
            return Task.CompletedTask;
        }
    }

    // Somewhere in the system setup:
    // systemBuilder.WithDefaultSupervisionStrategy(SupervisionStrategy.Stop);
    // var criticalService = await system.CreateActorAsync(new ActorPath("/user/critical_service"));
    // // Send "critical_fail" message to criticalService; it will stop.
    ```

### Resume Strategy (`SupervisionStrategy.Resume`)

*   **When to Use:** For minor, transient errors that can be safely ignored without affecting the actor's state or subsequent processing.
*   **What it Does:** The failed message is discarded, and the actor continues processing the next message in its mailbox without restarting or altering its state.
*   **Example:**
    ```csharp
    public class ResilientProcessor : Actor
    {
        public ResilientProcessor(ActorPath path) : base(path) { }

        protected override Task OnReceiveAsync(Message message)
        {
            if (message is Message<int> intMessage)
            {
                if (intMessage.Payload == 0)
                {
                    Console.WriteLine("Encountered zero, resuming.");
                    throw new ArgumentException("Cannot process zero!"); // This error will be resumed
                }
                Console.WriteLine($"Processing: {intMessage.Payload}");
            }
            return Task.CompletedTask;
        }
    }

    // Somewhere in the system setup:
    // systemBuilder.WithDefaultSupervisionStrategy(SupervisionStrategy.Resume);
    // var processor = await system.CreateActorAsync(new ActorPath("/user/processor"));
    // // Send messages, including Message<int>(0); the actor will skip 0 and continue.
    ```

### Backoff Strategy (`SupervisionStrategy.Backoff`)

*   **When to Use:** For failures that indicate an external dependency might be temporarily unavailable or overloaded. It restarts the actor with an exponentially increasing delay to give the dependency time to recover.
*   **What it Does:** After a failure, the actor is restarted, but with a delay that increases exponentially with each consecutive failure, up to a maximum delay. This prevents hammering a failing resource. The delay parameters are configurable via `ActorSystemOptions`.
*   **Example:**
    ```csharp
    public class ExternalServiceConsumer : Actor
    {
        public ExternalServiceConsumer(ActorPath path) : base(path) { }

        protected override async Task OnReceiveAsync(Message message)
        {
            // Simulate calling an external service that sometimes fails
            if (new Random().Next(0, 5) == 0)
            {
                throw new HttpRequestException("External service unavailable!");
            }
            Console.WriteLine($"{Path} successfully consumed external service.");
            await Task.CompletedTask;
        }
    }

    // Somewhere in the system setup:
    // systemBuilder.WithDefaultSupervisionStrategy(SupervisionStrategy.Backoff);
    // systemBuilder.WithInitialBackoffDelayMs(100); // Start with 100ms delay
    // systemBuilder.WithMaxBackoffDelayMs(5000);   // Max 5 seconds delay
    // systemBuilder.WithBackoffMultiplier(2.0);    // Double delay each time
    // var consumer = await system.CreateActorAsync(new ActorPath("/user/consumer"));
    // // Send messages; the consumer will restart with increasing delays on failure.
    ```

## 3. Configuring Supervision

The default supervision strategy for an actor system can be set when building the `ActorSystem`:

```csharp
var system = new ActorSystemBuilder("MySystem")
    .WithDefaultSupervisionStrategy(SupervisionStrategy.Restart)
    .Build();
```

Individual actors can have their supervisor explicitly set during creation. If no supervisor is specified, the actor defaults to the actor system's internal root supervisor.

```csharp
// Create a parent actor that will supervise its children
var parent = await system.CreateActorAsync(new ActorPath("/user/parent"));

// Create a child actor, explicitly assigning 'parent' as its supervisor
var child = await system.CreateActorAsync(new ActorPath("/user/parent/child"), parent.Ref);
```

### Adjusting Backoff Parameters

The behavior of the `Backoff` strategy is controlled by parameters in `ActorSystemOptions`:

*   `InitialBackoffDelayMs`: The initial delay after the first failure (default 100ms).
*   `MaxBackoffDelayMs`: The maximum delay that exponential backoff can reach (default 60 seconds).
*   `BackoffMultiplier`: The factor by which the delay increases after each consecutive failure (default 2.0).

These can be configured via the `ActorSystemBuilder`:

```csharp
var system = new ActorSystemBuilder("MySystem")
    .WithDefaultSupervisionStrategy(SupervisionStrategy.Backoff)
    .WithInitialBackoffDelayMs(200)
    .WithMaxBackoffDelayMs(10000) // 10 seconds
    .WithBackoffMultiplier(1.5)
    .Build();
```

## 4. Nested Supervision Trees

Supervision is hierarchical, meaning supervisors can also be supervised. This forms a "supervision tree". When a child actor escalates a failure, its supervisor's supervision strategy is applied to *itself* (the supervisor, now the failing entity in the eyes of its own parent).

Consider this hierarchy: `/user/grandparent -> /user/parent -> /user/parent/child`.
If `/user/parent/child` fails and escalates:
1.  `/user/parent` receives the failure from `child`.
2.  `/user/parent` applies its own supervision strategy (which could be to restart `child`, stop `child`, or escalate further).
3.  If `/user/parent` also decides to escalate (e.g., if it uses `Escalate` strategy, or `Restart` with too many restarts), the failure is passed to `/user/grandparent`.

This allows for fine-grained control over failure recovery at different levels of abstraction in your application.

## 5. Persistence and Restarts

When an actor is configured for persistence (`EnableMessagePersistence` or `EnableActorStateSnapshotting` in `ActorSystemOptions`), its state can be reloaded after a restart.

*   **Snapshots:** If `EnableActorStateSnapshotting` is true, the actor's latest saved state snapshot will be loaded upon restart, restoring it to a previous known-good state.
*   **Event Sourcing:** If `EnableMessagePersistence` is true and an event journal is used, the actor can replay its past events to rebuild its state from scratch after a restart.

Combining persistence with an appropriate supervision strategy is powerful. For example, using `SupervisionStrategy.Backoff` with a persistent actor ensures that if an external database (used for persistence) is temporarily down, the actor will wait and retry loading its state, rather than continuously failing or being permanently stopped.

## 6. Troubleshooting Common Supervision Issues

*   **Actor not restarting as expected?**
    *   Check the `DefaultSupervisionStrategy` configured for the system or the specific supervisor.
    *   For `Restart` strategy, ensure the `MaxMessageRetries` (or internal `RestartCount` in `SupervisionService`) limit hasn't been exceeded, which would lead to escalation instead.
*   **Supervisor isn't receiving failure messages?**
    *   Ensure the child actor explicitly sets its supervisor when created (`await system.CreateActorAsync(path, supervisorRef)`).
    *   Verify the supervisor's `OnErrorAsync` method is implemented correctly to handle incoming `FailureMessage`s from children or escalated exceptions.
*   **Deadlocks during actor restarts?**
    *   This often indicates that an actor or its underlying components are holding locks (`SemaphoreSlim`, `lock` statements) across `async` operations that are not properly released during the actor's termination/restart sequence. Ensure `IDisposable` resources are correctly disposed of.
*   **Excessive restarts/backoffs?**
    *   Review the `InitialBackoffDelayMs`, `MaxBackoffDelayMs`, and `BackoffMultiplier` settings in `ActorSystemOptions`.
    *   Consider if the underlying cause of failure is transient or requires a different strategy (e.g., `Stop` for unrecoverable errors).
*   **State not reloading after restart?**
    *   Verify `EnableMessagePersistence` and/or `EnableActorStateSnapshotting` are set to `true` in `ActorSystemOptions`.
    *   Ensure the persistence backend (e.g., `ISnapshotStore`, `IEventJournal`) is correctly configured and working.
    *   Confirm the actor's `OnInitializeAsync` or similar methods correctly load state from the persistence mechanism.

This enhanced documentation provides a comprehensive guide for users to effectively leverage the framework's supervision capabilities.
