// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Constants;

namespace DotNetActorFramework.Services;

/// <summary>
/// Manages supervision strategies for handling actor failures.
/// Implements various strategies: restart, stop, escalate, resume, and backoff.
/// </summary>
public class SupervisionService
{
    private readonly ActorRegistry _registry;
    private readonly MessageDispatcher _dispatcher;
    private readonly Dictionary<Guid, SupervisionContext> _supervisionContexts = [];
    private readonly object _lockObject = new();

    public SupervisionService(ActorRegistry registry, MessageDispatcher dispatcher)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Handles an actor failure according to its supervision strategy.
    /// </summary>
    public async Task HandleFailureAsync(ActorRef actor, Exception exception, SupervisionStrategy strategy)
    {
        if (actor == null)
            throw new ArgumentNullException(nameof(actor));

        if (exception == null)
            throw new ArgumentNullException(nameof(exception));

        GetOrCreateContext(actor.Id);

        switch (strategy)
        {
            case SupervisionStrategy.Restart:
                await HandleRestartAsync(actor, exception);
                break;

            case SupervisionStrategy.Stop:
                await HandleStopAsync(actor, exception);
                break;

            case SupervisionStrategy.Resume:
                await HandleResumeAsync(actor, exception);
                break;

            case SupervisionStrategy.Escalate:
                await HandleEscalateAsync(actor, exception);
                break;

            case SupervisionStrategy.Backoff:
                await HandleBackoffAsync(actor, exception);
                break;

            default:
                throw new ArgumentException($"Unknown supervision strategy: {strategy}", nameof(strategy));
        }
    }

    /// <summary>
    /// Restarts a failed actor.
    /// </summary>
    private async Task HandleRestartAsync(ActorRef actor, Exception exception)
    {
        var context = GetOrCreateContext(actor.Id);
        context.RestartCount++;

        if (context.RestartCount > 5)
        {
            // Too many restarts, escalate instead
            await HandleEscalateAsync(actor, exception);
            return;
        }

        var controlMessage = new ControlMessage(MessageConstants.RestartCommand);
        await _dispatcher.SendAsync(actor, controlMessage);
    }

    /// <summary>
    /// Stops a failed actor.
    /// </summary>
    private async Task HandleStopAsync(ActorRef actor, Exception exception)
    {
        var controlMessage = new ControlMessage(
            MessageConstants.StopCommand,
            new Dictionary<string, object>
            {
                { MessageConstants.FailureReasonParam, exception.Message }
            }
        );

        await _dispatcher.SendAsync(actor, controlMessage);
    }

    /// <summary>
    /// Resumes processing after a failure.
    /// </summary>
    private async Task HandleResumeAsync(ActorRef actor, Exception exception)
    {
        var controlMessage = new ControlMessage(MessageConstants.ResumeCommand);
        await _dispatcher.SendAsync(actor, controlMessage);
    }

    /// <summary>
    /// Escalates the failure to the supervisor.
    /// </summary>
    private async Task HandleEscalateAsync(ActorRef actor, Exception exception)
    {
        var parent = actor.GetParent();
        if (parent != null)
        {
            var failureMessage = new FailureMessage(
                $"Actor {actor.Path} failed",
                exception
            );

            await _dispatcher.SendAsync(parent, failureMessage);
        }
        else
        {
            // No supervisor, stop the actor
            await HandleStopAsync(actor, exception);
        }
    }

    /// <summary>
    /// Implements exponential backoff for retries.
    /// </summary>
    private async Task HandleBackoffAsync(ActorRef actor, Exception exception)
    {
        var context = GetOrCreateContext(actor.Id);
        context.FailureCount++;

        var backoffDelay = CalculateBackoffDelay(context.FailureCount);
        await Task.Delay(backoffDelay);

        var controlMessage = new ControlMessage(MessageConstants.ResumeCommand);
        await _dispatcher.SendAsync(actor, controlMessage);
    }

    /// <summary>
    /// Gets the supervision context for an actor, creating it if necessary.
    /// </summary>
    private SupervisionContext GetOrCreateContext(Guid actorId)
    {
        lock (_lockObject)
        {
            if (!_supervisionContexts.TryGetValue(actorId, out var context))
            {
                context = new SupervisionContext(actorId);
                _supervisionContexts[actorId] = context;
            }

            return context;
        }
    }

    /// <summary>
    /// Calculates exponential backoff delay in milliseconds.
    /// </summary>
    private int CalculateBackoffDelay(int failureCount)
    {
        var baseDelay = ActorConstants.InitialBackoffDelayMs;
        var exponentialDelay = (int)Math.Pow(ActorConstants.BackoffMultiplier, failureCount) * baseDelay;
        return Math.Min(exponentialDelay, ActorConstants.MaxBackoffDelayMs);
    }

    /// <summary>
    /// Resets the supervision context for an actor.
    /// </summary>
    public void ResetContext(Guid actorId)
    {
        lock (_lockObject)
        {
            if (_supervisionContexts.TryGetValue(actorId, out var context))
            {
                context.ResetFailures();
            }
        }
    }

    /// <summary>
    /// Gets supervision statistics.
    /// </summary>
    public SupervisionStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            return new SupervisionStatistics
            {
                TotalActorsSupervised = _supervisionContexts.Count,
                TotalFailures = _supervisionContexts.Values.Sum(c => c.FailureCount),
                TotalRestarts = _supervisionContexts.Values.Sum(c => c.RestartCount),
                AverageFailuresPerActor = _supervisionContexts.Count > 0
                    ? (double)_supervisionContexts.Values.Sum(c => c.FailureCount) / _supervisionContexts.Count
                    : 0
            };
        }
    }

    /// <summary>
    /// Gets the supervision context for a specific actor.
    /// </summary>
    /// <param name="actorId">The actor ID</param>
    /// <returns>The supervision context or null if not found</returns>
    public SupervisionContext? GetContext(Guid actorId)
    {
        lock (_lockObject)
        {
            _supervisionContexts.TryGetValue(actorId, out var context);
            return context;
        }
    }

    /// <summary>
    /// Gets all supervision contexts.
    /// </summary>
    /// <returns>Dictionary of all supervision contexts keyed by actor ID</returns>
    public Dictionary<Guid, SupervisionContext> GetAllContexts()
    {
        lock (_lockObject)
        {
            return new Dictionary<Guid, SupervisionContext>(_supervisionContexts);
        }
    }
}

/// <summary>
/// Context for supervising an actor's failures.
/// </summary>
public class SupervisionContext
{
    public Guid ActorId { get; }
    public int FailureCount { get; set; }
    public int RestartCount { get; set; }
    public DateTime LastFailureTime { get; set; }

    public SupervisionContext(Guid actorId)
    {
        ActorId = actorId;
        FailureCount = 0;
        RestartCount = 0;
        LastFailureTime = DateTime.UtcNow;
    }

    public void ResetFailures()
    {
        FailureCount = 0;
        RestartCount = 0;
    }

    public TimeSpan GetTimeSinceLastFailure() => DateTime.UtcNow - LastFailureTime;
}

/// <summary>
/// Statistics for supervision operations.
/// </summary>
public class SupervisionStatistics
{
    public int TotalActorsSupervised { get; set; }
    public long TotalFailures { get; set; }
    public long TotalRestarts { get; set; }
    public double AverageFailuresPerActor { get; set; }
}
