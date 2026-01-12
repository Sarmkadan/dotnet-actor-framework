# ErrorHandlingMiddleware
The `ErrorHandlingMiddleware` type is designed to handle errors that occur during the execution of actors in the dotnet-actor-framework. It provides a mechanism to catch and process exceptions, allowing for retry policies and custom error handling strategies to be implemented. This enables developers to build more robust and resilient actor systems that can recover from failures and exceptions.

## API
* `public ErrorHandlingMiddleware`: The constructor for the `ErrorHandlingMiddleware` class, used to create a new instance.
* `public async Task<bool> InvokeAsync`: An asynchronous method that invokes the middleware, allowing it to process errors and apply retry policies. Returns a boolean indicating whether the error was handled successfully.
* `public abstract Task<bool> HandleErrorAsync`: An abstract method that must be implemented by derived classes to provide custom error handling logic. Returns a boolean indicating whether the error was handled successfully.
* `public override Task<bool> HandleErrorAsync`: An overridden method that provides a default implementation for error handling. Returns a boolean indicating whether the error was handled successfully.
* `public RetryErrorStrategy`: A property that gets or sets the retry error strategy used by the middleware.
* `public override async Task<bool> HandleErrorAsync`: An overridden asynchronous method that provides a default implementation for error handling using the retry error strategy. Returns a boolean indicating whether the error was handled successfully.
* `public override Task<bool> HandleErrorAsync`: An overridden method that provides a default implementation for error handling. Returns a boolean indicating whether the error was handled successfully.

## Usage
The following examples demonstrate how to use the `ErrorHandlingMiddleware` class:
```csharp
// Example 1: Creating a custom error handling middleware
public class CustomErrorHandlingMiddleware : ErrorHandlingMiddleware
{
    public override async Task<bool> HandleErrorAsync(Exception ex)
    {
        // Custom error handling logic
        Console.WriteLine($"Error occurred: {ex.Message}");
        return true;
    }
}

// Example 2: Using the ErrorHandlingMiddleware with a retry policy
var middleware = new ErrorHandlingMiddleware();
middleware.RetryErrorStrategy = new RetryErrorStrategy(3, TimeSpan.FromSeconds(1));
await middleware.InvokeAsync();
```

## Notes
When using the `ErrorHandlingMiddleware`, it is essential to consider the following edge cases and thread-safety remarks:
* The `InvokeAsync` method may throw an exception if the error handling logic fails or if the retry policy is exceeded.
* The `HandleErrorAsync` method should be implemented to handle specific exception types and provide meaningful error messages.
* The `RetryErrorStrategy` property should be carefully configured to avoid infinite retry loops or excessive delays.
* The `ErrorHandlingMiddleware` class is designed to be thread-safe, but it is still important to ensure that the custom error handling logic and retry policies are properly synchronized to avoid concurrency issues.
