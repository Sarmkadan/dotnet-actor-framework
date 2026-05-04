// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that provides centralized error handling for message processing.
/// Catches exceptions and applies recovery strategies based on configuration.
/// </summary>
public class ErrorHandlingMiddleware : IActorMiddleware
{
    public string Name => "ErrorHandlingMiddleware";
    public int Order => 100; // Run after logging middleware

    private readonly ErrorHandlingStrategy _strategy;

    public ErrorHandlingMiddleware(ErrorHandlingStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    public async Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        try
        {
            await next(envelope);
            return true;
        }
        catch (Exception ex)
        {
            return await _strategy.HandleErrorAsync(envelope, ex);
        }
    }
}

/// <summary>
/// Strategy for handling errors during message processing.
/// </summary>
public abstract class ErrorHandlingStrategy
{
    /// <summary>
    /// Handles an error that occurred during message processing.
    /// Return true to continue, false to stop processing.
    /// </summary>
    public abstract Task<bool> HandleErrorAsync(Envelope envelope, Exception exception);
}

/// <summary>
/// Error handling strategy that logs and suppresses errors (fire-and-forget semantics).
/// </summary>
public class SuppressErrorStrategy : ErrorHandlingStrategy
{
    public override Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)
    {
        // Silently suppress the error - message is lost
        return Task.FromResult(true);
    }
}

/// <summary>
/// Error handling strategy that retries with exponential backoff.
/// </summary>
public class RetryErrorStrategy : ErrorHandlingStrategy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;

    public RetryErrorStrategy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        _backoffMultiplier = backoffMultiplier;
    }

    public override async Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)
    {
        var retryCount = envelope.GetMetadata("retry-count")?.FromJson<int>() ?? 0;

        if (retryCount >= _maxRetries)
            return false; // Max retries exceeded

        // Calculate delay with exponential backoff
        var delay = TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, retryCount));
        await Task.Delay(delay);

        // Update retry count in metadata
        envelope.SetMetadata("retry-count", (retryCount + 1).ToJson());
        return true; // Indicate retry should happen
    }
}

/// <summary>
/// Error handling strategy that immediately fails and re-throws the exception.
/// </summary>
public class FailFastErrorStrategy : ErrorHandlingStrategy
{
    public override Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)
    {
        throw new InvalidOperationException(
            $"Message processing failed for {envelope.RecipientPath}",
            exception);
    }
}
