# ValidationException

ValidationException is the base exception type for the dotnet-actor-framework, representing errors encountered during the validation of actor paths, messages, or references within the system. It serves as the root for specific validation-related failure scenarios, allowing consumers to catch broader validation issues or differentiate between specific actor-related failures.

## API

### ValidationException(string? message)
Initializes a new instance of the `ValidationException` class with a specified error message.

### ValidationException(string? message, Exception? innerException)
Initializes a new instance of the `ValidationException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

### new static ValidationException Create
A static factory method used to create and return an instance of `ValidationException` or its derived types, typically used when building exceptions with structured data.

### string InvalidPath
Gets the actor path that failed validation. This property is primarily populated by `InvalidActorPathException`.

### InvalidActorPathException(string path)
Initializes a new instance of the `InvalidActorPathException` class for the specified actor path.

### InvalidActorPathException(string path, string? message)
Initializes a new instance of the `InvalidActorPathException` class with the specified actor path and a custom error message.

### InvalidMessageException(string? message)
Initializes a new instance of the `InvalidMessageException` class with a specified error message.

### InvalidMessageException(string? message, Exception? innerException)
Initializes a new instance of the `InvalidMessageException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

### InvalidActorReferenceException(string? message)
Initializes a new instance of the `InvalidActorReferenceException` class with a specified error message.

### InvalidActorReferenceException(string? message, Exception? innerException)
Initializes a new instance of the `InvalidActorReferenceException` class with a specified error message and a reference to the inner exception that is the cause of this exception.

## Usage

```csharp
// Example 1: Throwing a ValidationException directly
public void ValidateActorName(string name)
{
    if (string.IsNullOrWhiteSpace(name))
    {
        throw new ValidationException("Actor name cannot be null or whitespace.");
    }
}

// Example 2: Catching a specific exception type derived from ValidationException
try
{
    var actorRef = _actorSystem.Resolve("/user/non-existent-actor");
}
catch (InvalidActorPathException ex)
{
    Logger.LogError($"Invalid actor path: {ex.InvalidPath}");
}
catch (ValidationException ex)
{
    Logger.LogError($"A validation error occurred: {ex.Message}");
}
```

## Notes

- **Thread Safety**: Instances of `ValidationException` and its derived classes are immutable once constructed. They are thread-safe to be thrown across different threads, though the standard exception handling mechanisms in C# apply.
- **Inner Exceptions**: Always check for `null` when inspecting the `InnerException` property, as constructors accept `Exception?`.
- **Inheritance**: All derived exceptions, such as `InvalidActorPathException`, `InvalidMessageException`, and `InvalidActorReferenceException`, inherit from `ValidationException`. Catching `ValidationException` will also catch all its derived types.
