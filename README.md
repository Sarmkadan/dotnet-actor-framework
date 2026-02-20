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

// ... (rest of the file remains unchanged)
