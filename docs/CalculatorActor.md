# CalculatorActor

`CalculatorActor` is a concrete actor implementation within the `dotnet-actor-framework` that processes arithmetic operation messages. It receives requests, performs the specified calculation, and dispatches the result back to the originating `RequestorActor`. This actor encapsulates the computational logic of the system, delegating all messaging concerns to the underlying framework infrastructure.

## API

### `public CalculatorActor(ActorPath path) : base(path)`

Initializes a new instance of the `CalculatorActor` class.

- **Parameters:**
  - `path` (`ActorPath`): The unique hierarchical address identifying this actor within the actor system. This path is passed to the base class constructor to establish the actor's identity and mailbox routing.
- **Return Value:** (Constructor)
- **Exceptions:** None thrown directly by this constructor. Exceptions may propagate from the base class if the provided `path` is malformed or violates system invariants.

### `public override async Task ReceiveAsync`

Asynchronously processes an incoming message delivered to this actor's mailbox.

- **Parameters:** (None — the message is obtained internally via the framework's message dispatch mechanism.)
- **Return Value:** A `Task` representing the asynchronous operation. The task completes when the message has been fully processed and any resulting response has been dispatched.
- **Exceptions:** May throw exceptions related to message deserialization if the incoming payload does not match the expected arithmetic request format. Arithmetic-specific exceptions (e.g., `DivideByZeroException`) are typically caught, packaged into an error response, and sent to the requestor rather than propagated.

### `public RequestorActor(ActorPath path, MessageDispatcher dispatcher) : base`

Constructs a `RequestorActor` instance. This member is documented here because it appears in the public surface of the file containing `CalculatorActor`, though it defines a separate actor type responsible for sending requests and receiving responses.

- **Parameters:**
  - `path` (`ActorPath`): The unique hierarchical address for this requestor actor.
  - `dispatcher` (`MessageDispatcher`): The dispatcher instance used to route outgoing request messages to target actors and deliver responses back to this actor.
- **Return Value:** (Constructor)
- **Exceptions:** None thrown directly. Exceptions may propagate from the base constructor if arguments are invalid.

### `public override async Task ReceiveAsync`

Processes an incoming response message delivered to the `RequestorActor`.

- **Parameters:** (None — the message is obtained internally.)
- **Return Value:** A `Task` representing the asynchronous handling of the response, typically completing the corresponding pending task completion source.
- **Exceptions:** May throw if the response payload cannot be deserialized or if the correlation identifier does not match any outstanding request.

## Usage

### Example 1: Creating a CalculatorActor and Sending a Request

```csharp
// Define the actor path for the calculator
var calculatorPath = new ActorPath("/system/calculators/calc-1");

// Instantiate the CalculatorActor
var calculatorActor = new CalculatorActor(calculatorPath);

// The actor is now registered and ready to receive messages.
// In a typical setup, the framework dispatches messages automatically.
// A RequestorActor would send an arithmetic request like:
//   { Operation: "Add", OperandA: 5, OperandB: 3, CorrelationId: "abc-123" }
```

### Example 2: Full Request-Response Cycle with RequestorActor

```csharp
// Create the message dispatcher (shared infrastructure)
var dispatcher = new MessageDispatcher();

// Create the requestor that will ask for calculations
var requestorPath = new ActorPath("/system/clients/client-1");
var requestor = new RequestorActor(requestorPath, dispatcher);

// Create the calculator that will process the requests
var calculatorPath = new ActorPath("/system/calculators/calc-1");
var calculator = new CalculatorActor(calculatorPath);

// The requestor sends a message; the framework routes it to the calculator.
// CalculatorActor.ReceiveAsync executes, computes the result,
// and dispatches the response back to the requestor.
// RequestorActor.ReceiveAsync then handles the response.
```

## Notes

- **Thread Safety:** Both `ReceiveAsync` methods are designed to be invoked sequentially by the actor framework's single-threaded-per-actor dispatch loop. They do not need to guard against concurrent invocations on the same actor instance. However, shared state accessed across different actors (e.g., the `MessageDispatcher`) must be thread-safe.
- **Edge Cases:**
  - If a `CalculatorActor` receives a message with an unsupported operation code, it should produce an error response rather than throwing an unhandled exception. The framework expects all message processing to complete gracefully.
  - Division by zero or numeric overflow during calculation should result in a fault response sent to the requestor, not an actor crash.
  - If the `RequestorActor` receives a response with a correlation ID that does not match any pending request, the implementation should silently discard it or log a warning to avoid orphaned task completions.
- **Actor Lifecycle:** Both actors derive from a base class that manages mailbox subscription and lifecycle. Destroying or stopping an actor externally while a `ReceiveAsync` operation is in flight may result in a `TaskCanceledException`; implementations should handle cancellation tokens if provided by the base infrastructure.
