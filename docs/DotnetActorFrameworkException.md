# DotnetActorFrameworkException

`DotnetActorFrameworkException` is a specialized exception type used throughout the dotnet-actor-framework library to represent error conditions that arise during actor lifecycle management, message processing, or supervision logic. It derives from `System.Exception` and provides both constructors and factory methods for consistent instantiation.

## API

### `DotnetActorFrameworkException(string? message)`

- **Purpose**: Initializes a new instance of the exception with an optional descriptive message.
- **Parameters**:
  - `message`: A string that describes the error; may be `null`.
- **Return Value**: A new `DotnetActorFrameworkException` instance.
- **Exceptions**: None beyond those thrown by the base `Exception` constructor (which does not throw for a `null` message).

### `DotnetActorFrameworkException(string? message, Exception? innerException)`

- **Purpose**: Initializes a new instance of the exception with an optional message and a reference to the inner exception that caused this exception.
- **Parameters**:
  - `message`: A string that describes the error; may be `null`.
  - `innerException`: The exception that is the cause of the current exception; may be `null`.
- **Return Value**: A new `DotnetActorFrameworkException` instance.
- **Exceptions**: None beyond those thrown by the base `Exception` constructor.

### `static DotnetActorFrameworkException Create(string? message)`

- **Purpose**: Factory method that creates a new `DotnetActorFrameworkException` with the supplied message.
- **Parameters**:
  - `message`: A string that describes the error; may be `null`.
- **Return Value**: A new `DotnetActorFrameworkException` instance initialized with the given message.
- **Exceptions**: None.

### `static DotnetActorFrameworkException Create(string? message, Exception? innerException)`

- **Purpose**: Factory method that creates a new `DotnetActorFrameworkException` with the supplied message and inner exception.
- **Parameters**:
  - `message`: A string that describes the error; may be `null`.
  - `innerException`: The exception that is the cause of the current exception; may be `null`.
- **Return Value**: A new `DotnetActorFrameworkException` instance initialized with the given message and inner exception.
- **Exceptions**: None.

## Usage

```csharp
using DotnetActorFramework;

public class SampleActor
{
    public void ProcessMessage(object msg)
    {
        try
        {
            // Actor-specific logic that may fail
            if (msg == null)
            {
                // Using the constructor directly
                throw new DotnetActorFrameworkException("Received null message.");
            }
        }
        catch (DotnetActorFrameworkException ex) when (ex.Message.Contains("null"))
        {
            // Handle or log the actor-specific error
            Console.WriteLine($"Actor error: {ex.Message}");
        }
    }
}
```

```csharp
using DotnetActorFramework;
using System;

public class Supervisor
{
    public void HandleFailure(Exception inner)
    {
        // Using the factory method to create an exception with an inner cause
        var ex = DotnetActorFrameworkException.Create(
            "Actor terminated due to an unrecoverable error.",
            inner);

        // Propagate or log the enriched exception
        throw ex;
    }
}
```

## Notes

- Passing `null` for `message` results in an exception whose `Message` property is `null` or empty, depending on the base class behavior; callers should guard against this if a non‑informative messages are required.
- The `innerException` parameter may be `null`; in such cases the resulting exception will have no inner exception, which is semantically equivalent to constructing the exception without that argument.
- Instances of `DotnetActorFrameworkException` are immutable after construction, making them safe to publish or share across threads without additional synchronization.
- The static `Create` methods are thread-safe; they merely allocate and return new objects and do not rely on mutable shared state.
