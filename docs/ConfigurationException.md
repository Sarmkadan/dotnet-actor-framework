# ConfigurationException

The `ConfigurationException` and its related derived types represent errors encountered during the configuration phase of the `dotnet-actor-framework`. These exceptions are thrown when invalid settings are provided for an actor system, mailbox, or persistence layer, preventing the framework components from initializing or operating within defined parameters.

## API

### ConfigurationException

*   `ConfigurationException(string? message)`: Initializes a new instance of the `ConfigurationException` class with a specified error message.
*   `ConfigurationException(string? message, Exception? innerException)`: Initializes a new instance of the `ConfigurationException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

### ActorSystemConfigurationException

*   `ActorSystemConfigurationException(string? message)`: Initializes a new instance of the `ActorSystemConfigurationException` class with a specified error message, specific to actor system configuration failures.
*   `ActorSystemConfigurationException(string? message, Exception? innerException)`: Initializes a new instance of the `ActorSystemConfigurationException` class with a specified error message and a reference to the inner exception.

### MailboxConfigurationException

*   `MailboxConfigurationException(string? message)`: Initializes a new instance of the `MailboxConfigurationException` class with a specified error message, specific to mailbox configuration failures.
*   `MailboxConfigurationException(string? message, Exception? innerException)`: Initializes a new instance of the `MailboxConfigurationException` class with a specified error message and a reference to the inner exception.

### PersistenceConfigurationException

*   `PersistenceConfigurationException(string? message)`: Initializes a new instance of the `PersistenceConfigurationException` class with a specified error message, specific to persistence layer configuration failures.
*   `PersistenceConfigurationException(string? message, Exception? innerException)`: Initializes a new instance of the `PersistenceConfigurationException` class with a specified error message and a reference to the inner exception.

## Usage

### Example 1: Validating actor system settings
```csharp
public void ConfigureSystem(ActorSystemSettings settings)
{
    if (settings.MaxActors <= 0)
    {
        throw new ActorSystemConfigurationException("MaxActors must be a positive integer.");
    }
    // Proceed with initialization
}
```

### Example 2: Handling nested persistence errors
```csharp
try
{
    InitializePersistence(connectionString);
}
catch (SqlException ex)
{
    throw new PersistenceConfigurationException("Failed to connect to the persistence store.", ex);
}
```

## Notes

*   **Exceptions and State**: These exceptions are intended to be thrown during the bootstrapping or configuration update phases. If caught, the system state is generally considered invalid or incomplete; developers should treat these as unrecoverable errors during the initialization process.
*   **Inner Exceptions**: When catching low-level framework exceptions (e.g., IO exceptions, database connection errors), wrap them in the appropriate `ConfigurationException` type to provide context-specific debugging information while preserving the original stack trace via the `innerException` parameter.
*   **Thread Safety**: These exception types are designed to be immutable once constructed. Instances of these exceptions are thread-safe and can be safely thrown and caught across different threads during system initialization.
