// ... (rest of the file remains unchanged)

## ValidationException

The `ValidationException` is a custom exception class used to handle validation-related errors in the actor framework. It provides a way to specify a custom error message and an inner exception for more detailed error handling.

### Usage Example
```csharp
public class MyActor : Actor
{
    public async Task HandleMessage(Message message)
    {
        try
        {
            // Attempt to validate the actor path
            var path = ActorPath.Parse("InvalidPath");
        }
        catch (ValidationException ex)
        {
            Log.Error(ex.Message);
            // Handle the validation error
        }
    }
}
```

## ConfigurationException

The `ConfigurationException` is a base exception class used to handle configuration-related errors in the actor framework. It provides a way to specify a custom error message and an inner exception for more detailed error handling. This exception has several derived classes, including `ActorSystemConfigurationException`, `MailboxConfigurationException`, and `PersistenceConfigurationException`, which can be used to handle specific configuration-related errors.

### Usage Example
```csharp
public class MyConfigurator
{
    public void ConfigureActorSystem()
    {
        try
        {
            // Attempt to configure the actor system
            var config = new ActorSystemOptions();
            // ...
        }
        catch (ConfigurationException ex)
        {
            Log.Error(ex.Message);
            // Handle the configuration error
        }
        catch (ActorSystemConfigurationException ex)
        {
            Log.Error(ex.Message);
            // Handle the actor system configuration error
        }
    }
}
```

// ... (rest of the file remains unchanged)
