# ActorException

The `ActorException` serves as the base class for all exceptions generated within the `dotnet-actor-framework`, providing a structured hierarchy for identifying and handling failures in distributed actor systems. By including diagnostic metadata such as actor paths and identifiers, these exceptions enable developers to pinpoint the origin of failures within complex actor hierarchies and communication streams.

## API

### ActorException

*   `ActorException(string? message)`: Initializes a new instance of the `ActorException` class with a specified error message.
*   `ActorException()`: Initializes a new instance of the `ActorException` class.
*   `static ActorException Create(...)`: A static factory method for creating instances of `ActorException` or derived types.
*   `string ActorPath`: Gets the path of the actor associated with the exception.
*   `Guid ActorId`: Gets the unique identifier of the actor associated with the exception.

### ActorNotFoundException

*   `ActorNotFoundException()`: Initializes a new instance of the `ActorNotFoundException` class.

### MailboxException

*   `MailboxException(string? message)`: Initializes a new instance of the `MailboxException` class with a specified error message.
*   `MailboxException()`: Initializes a new instance of the `MailboxException` class.
*   `MailboxException(Guid actorId, string? message)`: Initializes a new instance of the `MailboxException` class with a specified actor identifier and error message.

### SupervisionException

*   `SupervisionException(string? message)`: Initializes a new instance of the `SupervisionException` class with a specified error message.
*   `SupervisionException()`: Initializes a new instance of the `SupervisionException` class.

### ActorSystemException

*   `ActorSystemException(string? message)`: Initializes a new instance of the `ActorSystemException` class with a specified error message.
*   `ActorSystemException()`: Initializes a new instance of the `ActorSystemException` class.

### HttpActorCommunicationException

*   `HttpActorCommunicationException(string? message)`: Initializes a new instance of the `HttpActorCommunicationException` class with a specified error message.
*   `HttpActorCommunicationException()`: Initializes a new instance of the `HttpActorCommunicationException` class.
*   `HttpStatusCode? StatusCode`: Gets the HTTP status code associated with the communication failure.
*   `string? RequestUrl`: Gets the URL of the failed communication request.

## Usage

```csharp
try
{
    var actor = await actorSystem.GetActorAsync(targetPath);
}
catch (ActorNotFoundException ex)
{
    // Handle the case where the actor does not exist at the given path
    logger.LogError("Actor not found at {Path}", targetPath);
}
```

```csharp
// Throwing a MailboxException when actor mailbox processing fails
if (isMailboxFull)
{
    throw new MailboxException(actorId, "The actor mailbox has exceeded capacity.");
}
```

## Notes

*   **Thread Safety**: These exception classes are designed to be immutable once instantiated. They are safe to be accessed from multiple threads once thrown.
*   **Serialization**: Derived exceptions should be marked with the `[Serializable]` attribute if they are intended to be propagated across AppDomain boundaries or serialized in a distributed environment.
*   **Best Practices**: When catching exceptions, prefer catching specific derived types (e.g., `MailboxException`) before the base `ActorException` to allow for granular error handling strategies.
