# ActorRefExtensions

Extension methods for working with `ActorRef` instances in the dotnet-actor-framework, providing common actor communication patterns and introspection utilities.

## API

### `AskAsync`

Sends an asynchronous message to the actor and awaits a response without a timeout.

- **Parameters**
  - `actorRef`: The target actor reference.
  - `message`: The message to send.
  - `cancellationToken` *(optional)*: A cancellation token to observe while waiting for a response.
- **Return Value**: A `Task<object?>` that resolves to the actor's response or `null` if no response is sent.
- **Exceptions**: Throws `ArgumentNullException` if `actorRef` or `message` is `null`.

### `AskWithTimeoutAsync`

Sends an asynchronous message to the actor and awaits a response with a specified timeout.

- **Parameters**
  - `actorRef`: The target actor reference.
  - `message`: The message to send.
  - `timeout`: The maximum duration to wait for a response.
  - `cancellationToken` *(optional)*: A cancellation token to observe while waiting for a response.
- **Return Value**: A `Task<object?>` that resolves to the actor's response or `null` if no response is sent within the timeout.
- **Exceptions**
  - Throws `ArgumentNullException` if `actorRef` or `message` is `null`.
  - Throws `TimeoutException` if the response is not received within the specified timeout.

### `IsSameInstance`

Determines whether two `ActorRef` instances refer to the same actor instance.

- **Parameters**
  - `left`: The first actor reference.
  - `right`: The second actor reference.
- **Return Value**: `true` if both references point to the same actor instance; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if either `left` or `right` is `null`.

### `GetAge`

Retrieves the age of the actor instance, measured from its creation time.

- **Parameters**
  - `actorRef`: The actor reference to inspect.
- **Return Value**: A `TimeSpan` representing the actor's age.
- **Exceptions**: Throws `ArgumentNullException` if `actorRef` is `null`.

### `ToDetailedString`

Generates a human-readable string representation of the actor reference, including metadata such as age and path.

- **Parameters**
  - `actorRef`: The actor reference to convert.
- **Return Value**: A `string` containing detailed information about the actor.
- **Exceptions**: Throws `ArgumentNullException` if `actorRef` is `null`.

## Usage

### Request-Response Pattern
