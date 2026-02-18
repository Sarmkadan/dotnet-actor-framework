// ... (rest of the file remains unchanged)

## ValidationExceptionExtensions

## ValidationExceptionExtensions
The ValidationExceptionExtensions provide a set of convenience extension methods to create validation-related exceptions. These extensions simplify the process of throwing informative and context-rich exceptions during validation.

### Usage Example
```csharp
public class MyActor : Actor
{
    public override async Task ReceiveAsync(Message message)
    {
        var result = int.TryParse(message.Data.ToString(), out var numericValue);
        if (!result)
        {
            var ex = InvalidMessageException.WithExpectedFormat("expected a numeric value");
            throw ex;
        }

        // More processing...
    }
}
```

### Available Extensions
- `WithContext`: Adds contextual information to an InvalidActorPathException.
- `WithExpectedFormat`: Creates an InvalidMessageException indicating the expected message format.
- `WithActorType`: Generates an InvalidActorReferenceException specifying the expected actor type.
- `CombineWith`: Merges multiple validation exceptions into a single ValidationException.
- `IsValidationType`: Checks if an exception is of a validation type.

// ... (rest of the file remains unchanged)
