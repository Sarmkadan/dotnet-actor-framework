// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that provides centralized error handling for message processing.
/// </summary>
public class ErrorHandlingMiddleware : IActorMiddleware
{
    public string Name => "ErrorHandlingMiddleware";
    public int Order => 100;

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
    public abstract Task<bool> HandleErrorAsync(Envelope envelope, Exception exception);
}

/// <summary>
/// Silently suppresses errors (fire-and-forget semantics).
/// </summary>
public class SuppressErrorStrategy : ErrorHandlingStrategy
{
    public override Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)
        => Task.FromResult(true);
}

/// <summary>
/// Retries with exponential backoff up to a configured maximum.
/// </summary>
public class RetryErrorStrategy : ErrorHandlingStrategy
{
    private readonly int _maxRetries;
    private readonly TimeSpan _initialDelay;
    private readonly double _backoffMultiplier;
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, int> _retryCounts = new();

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
        var id = envelope.EnvelopeId;
        var retryCount = _retryCounts.GetOrAdd(id, 0);

        if (retryCount >= _maxRetries)
        {
            _retryCounts.TryRemove(id, out _);
            return false;
        }

        _retryCounts[id] = retryCount + 1;
        var delay = TimeSpan.FromMilliseconds(_initialDelay.TotalMilliseconds * Math.Pow(_backoffMultiplier, retryCount));
        await Task.Delay(delay);
        return true;
    }
}

/// <summary>
/// Immediately re-throws the exception as an InvalidOperationException.
/// </summary>
public class FailFastErrorStrategy : ErrorHandlingStrategy
{
    public override Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)
    {
        throw new InvalidOperationException(
            $"Message processing failed for {envelope.Recipient.Path}",
            exception);
    }
}
