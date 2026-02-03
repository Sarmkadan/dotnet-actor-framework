# MiddlewarePipelineTests

Unit tests for the middleware pipeline behavior in the actor framework, verifying registration, execution, and control/failure message handling.

## API

### `Register_WithNullMiddleware_ThrowsArgumentNullException`
Ensures that registering a null middleware delegate throws an `ArgumentNullException`.

- **Parameters**: None
- **Throws**: `ArgumentNullException` if the middleware parameter is null.

### `Register_WithMockedMiddleware_MiddlewareAppearsInGetMiddleware`
Validates that a mocked middleware delegate is correctly stored and retrievable via the pipeline's middleware accessor.

- **Parameters**: None
- **Returns**: void
- **Throws**: None

### `ExecuteAsync_WithNoMiddleware_InvokesFinalHandlerAndReturnsTrue`
Confirms that executing the pipeline without any registered middleware invokes the final handler and returns a success status.

- **Parameters**: None
- **Returns**: `Task<bool>` where `true` indicates successful execution.
- **Throws**: None

### `ExecuteAsync_WhenRegisteredMiddlewareThrows_ReturnsFalse`
Checks that if a registered middleware throws an exception during execution, the pipeline returns a failure status.

- **Parameters**: None
- **Returns**: `Task<bool>` where `false` indicates failure.
- **Throws**: None

### `ExecuteAsync_WithNullEnvelope_ThrowsArgumentNullException`
Ensures that passing a null envelope to the pipeline throws an `ArgumentNullException`.

- **Parameters**: None
- **Throws**: `ArgumentNullException` if the envelope is null.

### `ControlMessage_WithEmptyCommand_ThrowsArgumentException`
Verifies that a control message with an empty command throws an `ArgumentException`.

- **Parameters**: None
- **Throws**: `ArgumentException` if the command is empty.

### `ControlMessage_WithValidCommand_StoresCommandAndDefaultsParameters`
Confirms that a control message with a valid command stores the command and initializes default parameters.

- **Parameters**: None
- **Returns**: void
- **Throws**: None

### `FailureMessage_WithValidReasonAndException_StoresReasonAndStackTrace`
Ensures that a failure message with a valid reason and exception stores the reason and stack trace.

- **Parameters**: None
- **Returns**: void
- **Throws**: None

## Usage
