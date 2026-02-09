# BankAccountActor

The `BankAccountActor` is a specialized actor within the `dotnet-actor-framework` designed to encapsulate the state and behavior of a bank account. It manages the lifecycle of account operations through asynchronous message processing, ensuring that state mutations occur sequentially within the actor's context. This class extends the base actor functionality to provide specific hooks for initialization, message reception, and graceful shutdown, making it suitable for maintaining consistent financial state in a distributed environment.

## API

### `public BankAccountActor(ActorPath path) : base(path)`
Initializes a new instance of the `BankAccountActor` class.
*   **Purpose**: Constructs the actor and associates it with a specific location in the actor hierarchy.
*   **Parameters**:
    *   `path` (`ActorPath`): The unique path identifying this actor within the system.
*   **Return Value**: None (Constructor).
*   **Throws**: Throws an exception if the provided `path` is null or invalid, as enforced by the base class constructor.

### `public override async Task OnInitializeAsync()`
Executes initialization logic when the actor is first started.
*   **Purpose**: Provides a hook to set up initial state, such as setting a zero balance or loading persisted data, before the actor begins processing messages.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous initialization operation. The actor will not process messages until this task completes successfully.
*   **Throws**: May throw exceptions if underlying resources (e.g., database connections) are unavailable during startup, causing the actor to fail initialization.

### `public override async Task ReceiveAsync()`
Processes incoming messages dispatched to the actor.
*   **Purpose**: The core entry point for handling business logic related to the bank account, such as deposits, withdrawals, or balance inquiries. Implementation details depend on the specific message types handled within the method body.
*   **Parameters**: None (implicitly accesses the current message context via the actor framework).
*   **Return Value**: A `Task` representing the asynchronous processing of the current message.
*   **Throws**: May throw exceptions if message processing fails (e.g., insufficient funds, invalid transaction format), which typically triggers the framework's error handling or supervision strategies.

### `public override async Task OnStopAsync()`
Executes cleanup logic when the actor is being stopped.
*   **Purpose**: Allows for graceful shutdown procedures, such as flushing pending transactions to persistent storage or releasing external resources.
*   **Parameters**: None.
*   **Return Value**: A `Task` representing the asynchronous cleanup operation. The actor is not considered fully stopped until this task completes.
*   **Throws**: May throw exceptions if cleanup operations fail, potentially logging errors but generally proceeding with the termination sequence.

## Usage

### Example 1: Spawning and Initializing an Actor
This example demonstrates how to instantiate the `BankAccountActor` with a specific path and rely on the framework to invoke `OnInitializeAsync`.

```csharp
using DotNetActorFramework;
using DotNetActorFramework.Hosting;

// Define the unique path for the new account
var accountPath = new ActorPath("bank", "accounts", "acc-12345");

// Spawn the actor via the system context
var actorSystem = ActorSystem.Create("BankingSystem");
var accountActor = await actorSystem.SpawnAsync<BankAccountActor>(accountPath);

// At this point, OnInitializeAsync has completed.
// The actor is ready to receive messages.
```

### Example 2: Sending a Transaction Message
This example illustrates sending a command to the actor, which triggers the `ReceiveAsync` method to process the logic.

```csharp
using DotNetActorFramework;

// Assume 'accountActor' is a running instance of BankAccountActor
var depositCommand = new DepositMessage(amount: 500.00m, transactionId: "tx-998877");

// Send the message asynchronously; ReceiveAsync will handle the logic
await accountActor.TellAsync(depositCommand);

// The framework ensures ReceiveAsync completes before processing the next message
Console.WriteLine("Deposit request submitted to actor.");
```

## Notes

*   **Thread Safety**: Instances of `BankAccountActor` are inherently thread-safe regarding their internal state because the `dotnet-actor-framework` guarantees that `ReceiveAsync` is never executed concurrently for the same actor instance. Messages are processed sequentially in the order they are received.
*   **Asynchronous Lifecycle**: Both `OnInitializeAsync` and `OnStopAsync` are asynchronous. The framework will not deliver messages to `ReceiveAsync` until `OnInitializeAsync` completes. Similarly, the actor resource is not fully released until `OnStopAsync` finishes. Long-running operations in these methods can delay actor startup or shutdown.
*   **Exception Propagation**: Unhandled exceptions thrown within `ReceiveAsync` do not crash the entire application but are typically handled by the configured supervision strategy (e.g., restarting the actor). However, exceptions in `OnInitializeAsync` usually result in the immediate failure of the actor creation request.
*   **State Consistency**: Since `BankAccountActor` relies on sequential message processing, race conditions on internal fields (like account balance) are avoided without explicit locking, provided all state modifications occur strictly within `ReceiveAsync` or methods called exclusively by it.
