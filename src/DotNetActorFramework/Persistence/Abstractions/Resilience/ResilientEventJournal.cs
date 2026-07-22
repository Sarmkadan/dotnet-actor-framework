// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence.Abstractions.Resilience;
using Microsoft.Extensions.Logging;

namespace DotNetActorFramework.Persistence.Abstractions;

/// <summary>
/// A resilient decorator for <see cref="IEventJournal"/> that adds retry and circuit breaker patterns.
/// </summary>
public class ResilientEventJournal : IEventJournal
{
    private readonly IEventJournal _innerJournal;
    private readonly ILogger<ResilientEventJournal>? _logger;
    private readonly ResilienceOptions _options;
    private readonly CircuitBreakerState _circuitBreaker;
    private readonly Random _random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResilientEventJournal"/> class.
    /// </summary>
    /// <param name="innerJournal">The underlying event journal to wrap.</param>
    /// <param name="options">Resilience configuration options.</param>
    /// <param name="logger">Optional logger.</param>
    public ResilientEventJournal(
        IEventJournal innerJournal,
        ResilienceOptions? options = null,
        ILogger<ResilientEventJournal>? logger = null)
    {
        _innerJournal = innerJournal ?? throw new ArgumentNullException(nameof(innerJournal));
        _logger = logger;
        _options = options ?? ResilienceOptions.Default;
        _circuitBreaker = new CircuitBreakerState(_options.CircuitBreakerFailureThreshold, _options.CircuitBreakerCooldownPeriod);
    }

    /// <inheritdoc/>
    public async Task AppendEventsAsync(Guid actorId, string actorPath, IEnumerable<ActorEvent> events)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);
        ArgumentNullException.ThrowIfNull(events);

        await ExecuteWithResilienceAsync(async () =>
        {
            await _innerJournal.AppendEventsAsync(actorId, actorPath, events);
        }, async () => await _innerJournal.AppendEventsAsync(actorId, actorPath, events),
        nameof(AppendEventsAsync), actorId, actorPath);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActorEvent>> ReadEventsAsync(Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        return await ExecuteWithResilienceAsync(async () =>
        {
            return await _innerJournal.ReadEventsAsync(actorId, actorPath, fromSequenceNr, toSequenceNr);
        }, async () => await _innerJournal.ReadEventsAsync(actorId, actorPath, fromSequenceNr, toSequenceNr),
        nameof(ReadEventsAsync), actorId, actorPath, fromSequenceNr, toSequenceNr);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ActorEvent>> ReadEventsBackwardAsync(Guid actorId, string actorPath, long fromSequenceNr, long toSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        return await ExecuteWithResilienceAsync(async () =>
        {
            return await _innerJournal.ReadEventsBackwardAsync(actorId, actorPath, fromSequenceNr, toSequenceNr);
        }, async () => await _innerJournal.ReadEventsBackwardAsync(actorId, actorPath, fromSequenceNr, toSequenceNr),
        nameof(ReadEventsBackwardAsync), actorId, actorPath, fromSequenceNr, toSequenceNr);
    }

    /// <inheritdoc/>
    public async Task DeleteEventsAsync(Guid actorId, string actorPath, long maxSequenceNr)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        await ExecuteWithResilienceAsync(async () =>
        {
            await _innerJournal.DeleteEventsAsync(actorId, actorPath, maxSequenceNr);
        }, async () => await _innerJournal.DeleteEventsAsync(actorId, actorPath, maxSequenceNr),
        nameof(DeleteEventsAsync), actorId, actorPath, maxSequenceNr);
    }

    /// <inheritdoc/>
    public async Task DeleteAllEventsAsync(Guid actorId, string actorPath)
    {
        ArgumentNullException.ThrowIfNull(actorId);
        ArgumentException.ThrowIfNullOrEmpty(actorPath);

        await ExecuteWithResilienceAsync(async () =>
        {
            await _innerJournal.DeleteAllEventsAsync(actorId, actorPath);
        }, async () => await _innerJournal.DeleteAllEventsAsync(actorId, actorPath),
        nameof(DeleteAllEventsAsync), actorId, actorPath);
    }

    private async Task ExecuteWithResilienceAsync(
        Func<Task> operation,
        Func<Task> fallbackOperation,
        string operationName,
        params object?[] operationContext)
    {
        if (_circuitBreaker.IsOpen)
        {
            _logger?.LogWarning("Circuit breaker is open, failing fast for {OperationName} with context: {Context}",
                operationName, string.Join(", ", operationContext));
            throw PersistenceUnavailableException.Create(
                "Event journal persistence is unavailable. Circuit breaker is open after {FailureCount} consecutive failures. Try again later.",
                _circuitBreaker.ConsecutiveFailureCount);
        }

        var attempt = 0;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;

            try
            {
                await operation();
                _circuitBreaker.RecordSuccess();
                _logger?.LogDebug("Successfully executed {OperationName} after {Attempt} attempt(s)", operationName, attempt);
                return;
            }
            catch (Exception ex) when (IsTransientException(ex))
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Transient failure on {OperationName} attempt {Attempt}/{MaxAttempts} with context: {Context}",
                    operationName, attempt, _options.MaxRetryAttempts, string.Join(", ", operationContext));

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = CalculateBackoffDelay(attempt);
                    await Task.Delay(delay);
                }
            }
        }

        // All retry attempts exhausted
        _logger?.LogError(lastException, "All {MaxAttempts} retry attempts failed for {OperationName}", _options.MaxRetryAttempts, operationName);
        _circuitBreaker.RecordFailure();

        // Try one final attempt as fallback
        try
        {
            await fallbackOperation();
            _circuitBreaker.RecordSuccess();
            _logger?.LogDebug("Fallback operation succeeded for {OperationName} after all retries failed", operationName);
            return;
        }
        catch (Exception fallbackEx)
        {
            _logger?.LogError(fallbackEx, "Fallback operation also failed for {OperationName}", operationName);
            _circuitBreaker.RecordFailure();
            throw PersistenceUnavailableException.Create(fallbackEx,
                "Failed to persist events after {MaxAttempts} attempts with transient errors. Last error: {ErrorMessage}",
                _options.MaxRetryAttempts, lastException?.Message ?? "unknown");
        }
    }

    private async Task<T> ExecuteWithResilienceAsync<T>(
        Func<Task<T>> operation,
        Func<Task<T>> fallbackOperation,
        string operationName,
        params object?[] operationContext)
    {
        if (_circuitBreaker.IsOpen)
        {
            _logger?.LogWarning("Circuit breaker is open, failing fast for {OperationName} with context: {Context}",
                operationName, string.Join(", ", operationContext));
            throw PersistenceUnavailableException.Create(
                "Event journal persistence is unavailable. Circuit breaker is open after {FailureCount} consecutive failures. Try again later.",
                _circuitBreaker.ConsecutiveFailureCount);
        }

        var attempt = 0;
        var stopwatch = Stopwatch.StartNew();
        Exception? lastException = null;

        while (attempt < _options.MaxRetryAttempts)
        {
            attempt++;

            try
            {
                var result = await operation();
                _circuitBreaker.RecordSuccess();
                _logger?.LogDebug("Successfully executed {OperationName} after {Attempt} attempt(s)", operationName, attempt);
                return result;
            }
            catch (Exception ex) when (IsTransientException(ex))
            {
                lastException = ex;
                _logger?.LogWarning(ex, "Transient failure on {OperationName} attempt {Attempt}/{MaxAttempts} with context: {Context}",
                    operationName, attempt, _options.MaxRetryAttempts, string.Join(", ", operationContext));

                if (attempt < _options.MaxRetryAttempts)
                {
                    var delay = CalculateBackoffDelay(attempt);
                    await Task.Delay(delay);
                }
            }
        }

        // All retry attempts exhausted
        _logger?.LogError(lastException, "All {MaxAttempts} retry attempts failed for {OperationName}", _options.MaxRetryAttempts, operationName);
        _circuitBreaker.RecordFailure();

        // Try one final attempt as fallback
        try
        {
            var result = await fallbackOperation();
            _circuitBreaker.RecordSuccess();
            _logger?.LogDebug("Fallback operation succeeded for {OperationName} after all retries failed", operationName);
            return result;
        }
        catch (Exception fallbackEx)
        {
            _logger?.LogError(fallbackEx, "Fallback operation also failed for {OperationName}", operationName);
            _circuitBreaker.RecordFailure();
            throw PersistenceUnavailableException.Create(fallbackEx,
                "Failed to read events after {MaxAttempts} attempts with transient errors. Last error: {ErrorMessage}",
                _options.MaxRetryAttempts, lastException?.Message ?? "unknown");
        }
    }

    private bool IsTransientException(Exception ex)
    {
        // Check for common transient failure patterns
        return ex is PersistenceUnavailableException
            || ex is TimeoutException
            || ex is OperationCanceledException
            || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("network", StringComparison.OrdinalIgnoreCase);
    }

    private TimeSpan CalculateBackoffDelay(int attempt)
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromMilliseconds(Math.Min(
            _options.BaseDelayMilliseconds * Math.Pow(_options.BackoffExponent, attempt - 1),
            _options.MaxDelayMilliseconds
        ));

        // Add jitter to prevent thundering herd
        var jitter = TimeSpan.FromMilliseconds(_random.Next(0, (int)baseDelay.TotalMilliseconds / 2));
        var totalDelay = baseDelay + jitter;

        return totalDelay;
    }

    private class CircuitBreakerState
    {
        private readonly int _failureThreshold;
        private readonly TimeSpan _cooldownPeriod;
        private int _consecutiveFailureCount;
        private DateTime? _lastFailureTime;
        private readonly object _lock = new();

        public CircuitBreakerState(int failureThreshold, TimeSpan cooldownPeriod)
        {
            _failureThreshold = failureThreshold;
            _cooldownPeriod = cooldownPeriod;
        }

        public int ConsecutiveFailureCount => _consecutiveFailureCount;

        public bool IsOpen
        {
            get
            {
                lock (_lock)
                {
                    if (_consecutiveFailureCount < _failureThreshold)
                    {
                        return false;
                    }

                    if (_lastFailureTime == null)
                    {
                        return true;
                    }

                    return DateTime.UtcNow - _lastFailureTime < _cooldownPeriod;
                }
            }
        }

        public void RecordSuccess()
        {
            lock (_lock)
            {
                _consecutiveFailureCount = 0;
                _lastFailureTime = null;
            }
        }

        public void RecordFailure()
        {
            lock (_lock)
            {
                _consecutiveFailureCount++;
                _lastFailureTime = DateTime.UtcNow;
            }
        }
    }
}
