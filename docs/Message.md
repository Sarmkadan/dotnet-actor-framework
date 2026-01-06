# Message

The `Message` type is a transportable unit used within the dotnet-actor-framework to encapsulate data and control information exchanged between actors. It supports prioritized delivery, payload encapsulation, and failure reporting, enabling robust inter-actor communication patterns such as request-response and command execution.

## API

### `public Guid MessageId`
A unique identifier for the message, used to correlate requests with responses and track message flow across the actor system.

### `public DateTime CreatedAt`
The UTC timestamp indicating when the message was created. Used for ordering, timeouts, and diagnostics.

### `public int Priority`
An integer value indicating the message's relative priority. Higher values indicate higher priority for scheduling and processing. Defaults to 0.

### `public T Payload`
The encapsulated payload of type `T`. This is the primary data carried by the message. The type `T` must be serializable for transport across actor boundaries.

### `public Message()`
Constructs a new, empty message with default values:
- `MessageId` is initialized to a new `Guid`.
- `CreatedAt` is set to the current UTC time.
- `Priority` is set to 0.

### `public string Command`
An optional command string identifying the action to be performed. Used in command-pattern messaging to route messages to appropriate handlers.

### `public Dictionary<string, object> Parameters`
A collection of key-value pairs used to supply additional context or arguments for the command or operation. Keys are non-null strings; values are arbitrary objects (must be serializable).

### `public ControlMessage`
An optional control directive (e.g., `Start`, `Stop`, `Pause`) that alters the processing behavior of the receiving actor. Must be one of the defined control message types in the framework.

### `public object? Response`
When the message represents a request, this field holds the response payload returned by the handler. `null` if the message is not a response or if no response has been set.

### `public bool IsSuccess`
Indicates whether the operation associated with the message completed successfully. `true` if successful; otherwise, `false`. Only meaningful for response or result messages.

### `public string? ErrorMessage`
A human-readable error message describing why an operation failed. `null` if the operation succeeded or if no error occurred.

### `public ResponseMessage`
A predefined control message indicating that this message is a response to a prior request. Used internally for routing and correlation.

### `public string Reason`
A descriptive reason for control actions (e.g., shutdown, failure). Provided when `ControlMessage` is set to a terminal state.

### `public string? StackTrace`
A serialized stack trace captured at the point of failure. `null` if no failure occurred or if not applicable.

### `public DateTime FailureTime`
The UTC timestamp indicating when a failure occurred. Only meaningful if an error condition exists.

### `public FailureMessage`
A predefined control message indicating that this message represents a failure outcome. Used for error propagation and handling.

## Usage
