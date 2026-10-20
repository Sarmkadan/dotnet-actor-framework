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
    /// <summary>
    /// Gets the name of this middleware.
    /// </summary>
    public string Name => "ErrorHandlingMiddleware";

    /// <summary>
    /// Gets the execution order of this middleware in the pipeline.
    /// </summary>
    public int Order => 100;

    private readonly ErrorHandlingStrategy _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingMiddleware"/> class with the specified error handling strategy.
    /// </summary>
    /// <param name="strategy">The strategy used to handle errors during message processing.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="strategy"/> is null.</exception>
    public ErrorHandlingMiddleware(ErrorHandlingStrategy strategy)
    {
        _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
    }

    /// <summary>
    /// Intercepts message processing, delegating to the next handler and catching any exceptions
    /// via the configured <see cref="ErrorHandlingStrategy"/>.
    /// </summary>
    /// <param name="envelope">The message envelope being processed.</param>
    /// <param name="next">The next handler in the middleware pipeline.</param>
    /// <returns><c>true</c> if the message was processed successfully or the error was handled; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is null.</exception>
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
    /// </summary>
    /// <param name="envelope">The message envelope that caused the error.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <returns><c>true</c> if the error was handled and processing can continue; <c>false</c> if it should be treated as a failure.</returns>
    public abstract Task<bool> HandleErrorAsync(Envelope envelope, Exception exception);
}

/// <summary>
/// Silently suppresses errors (fire-and-forget semantics).
/// </summary>
public class SuppressErrorStrategy : ErrorHandlingStrategy
{
    /// <inheritdoc />
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

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryErrorStrategy"/> class.
    /// </summary>
    /// <param name="maxRetries">The maximum number of retry attempts before giving up.</param>
    /// <param name="initialDelay">The delay before the first retry. Defaults to 100 milliseconds.</param>
    /// <param name="backoffMultiplier">The multiplier applied to the delay on each subsequent retry.</param>
    public RetryErrorStrategy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0)
    {
        _maxRetries = maxRetries;
        _initialDelay = initialDelay ?? TimeSpan.FromMilliseconds(100);
        _backoffMultiplier = backoffMultiplier;
    }

    /// <inheritdoc />
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
    /// <inheritdoc />
    public override Task<bool> HandleErrorAsync(Envelope envelope, Exception exception)
    {
        throw new InvalidOperationException(
            $"Message processing failed for {envelope.Recipient.Path}",
            exception);
    }
}
