# RateLimitingMiddleware
The `RateLimitingMiddleware` class is designed to handle rate limiting in the dotnet-actor-framework project. It provides a mechanism to limit the number of requests or actions within a specified time frame, preventing abuse or overload of system resources. This middleware is essential for maintaining the stability and performance of the system.

## API
* `public RateLimitingMiddleware`: The constructor for the `RateLimitingMiddleware` class.
* `public async Task<bool> InvokeAsync`: Invokes the middleware asynchronously, returning a boolean value indicating whether the request was successful. This method does not have any parameters.
* `public RateLimiter`: A property that exposes the underlying rate limiter.
* `public bool TryConsumeToken`: Attempts to consume a token from the rate limiter, returning a boolean value indicating whether the token was successfully consumed.
* `public RateLimitStatus GetStatus`: Retrieves the current status of the rate limiter.
* `public void Dispose`: Disposes of the rate limiting middleware, releasing any system resources.
* `public TokenBucket`: A property that exposes the underlying token bucket.
* `public bool TryConsumeToken`: Attempts to consume a token from the token bucket, returning a boolean value indicating whether the token was successfully consumed.
* `public void AddTokens`: Adds tokens to the token bucket.
* `public int CurrentTokens`: Gets the current number of tokens in the token bucket.
* `public int Capacity`: Gets the capacity of the token bucket.
* `public bool IsLimited`: Gets a value indicating whether the rate limiter is currently limiting requests.

## Usage
The following examples demonstrate how to use the `RateLimitingMiddleware` class:
```csharp
// Example 1: Creating a rate limiting middleware
var rateLimitingMiddleware = new RateLimitingMiddleware();
var result = await rateLimitingMiddleware.InvokeAsync();
if (result)
{
    Console.WriteLine("Request successful");
}
else
{
    Console.WriteLine("Rate limit exceeded");
}

// Example 2: Using the token bucket
var tokenBucket = rateLimitingMiddleware.TokenBucket;
if (tokenBucket.TryConsumeToken())
{
    Console.WriteLine("Token consumed successfully");
}
else
{
    Console.WriteLine("No tokens available");
}
```

## Notes
When using the `RateLimitingMiddleware` class, it is essential to consider the following edge cases and thread-safety remarks:
* The `InvokeAsync` method may throw an exception if the rate limiter is not properly configured or if there is an issue with the underlying system resources.
* The `TryConsumeToken` method may return false if the rate limiter is currently limiting requests or if there are no tokens available in the token bucket.
* The `TokenBucket` property is not thread-safe, and access to it should be synchronized to prevent concurrent modifications.
* The `RateLimitingMiddleware` class is designed to be used in a single-threaded or synchronized multi-threaded environment to ensure accurate rate limiting and token bucket management.
