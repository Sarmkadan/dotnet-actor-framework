// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Models;

/// <summary>
/// Represents an actor in the system.
/// Actors are lightweight entities that process messages sequentially.
/// </summary>
public class Actor
{
    public Guid Id { get; }
    public ActorRef Ref { get; }
    public ActorPath Path { get; }
    public ActorState State { get; private set; }
    public ActorMetrics Metrics { get; }
    public DateTime CreatedAt { get; }
    public DateTime? TerminatedAt { get; private set; }
    public ActorRef? Supervisor { get; set; }

    private readonly Dictionary<string, object> _state = [];
    private readonly object _lockObject = new();

    public Actor(ActorPath path, ActorRef? supervisor = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Id = Guid.NewGuid();
        Ref = new ActorRef(path, Id);
        Supervisor = supervisor;
        State = ActorState.Created;
        Metrics = new ActorMetrics(Id, path);
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes the actor.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (State != ActorState.Created)
            throw new InvalidOperationException($"Cannot initialize actor in state: {State}");

        State = ActorState.Initializing;
        try
        {
            await OnInitializeAsync();
            State = ActorState.Started;
        }
        catch (Exception ex)
        {
            State = ActorState.Error;
            Metrics.RecordError();
            throw new InvalidOperationException($"Failed to initialize actor {Path}", ex);
        }
    }

    /// <summary>
    /// Processes a message received by this actor.
    /// </summary>
    public async Task ProcessMessageAsync(Message message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (State != ActorState.Started)
            throw new InvalidOperationException($"Actor {Path} is not in Started state.");

        var startTime = DateTime.UtcNow;
        try
        {
            Metrics.RecordMessageReceived();
            await OnReceiveAsync(message);
            var elapsed = (long)(DateTime.UtcNow - startTime).TotalMilliseconds;
            Metrics.RecordProcessingTime(elapsed);
        }
        catch (Exception ex)
        {
            Metrics.RecordError();
            await OnErrorAsync(message, ex);
        }
    }

    /// <summary>
    /// Stores a value in the actor's internal state.
    /// </summary>
    public void SetState(string key, object value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("State key cannot be null or empty.", nameof(key));

        lock (_lockObject)
        {
            _state[key] = value;
        }
    }

    /// <summary>
    /// Retrieves a value from the actor's internal state.
    /// </summary>
    public object? GetState(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("State key cannot be null or empty.", nameof(key));

        lock (_lockObject)
        {
            return _state.TryGetValue(key, out var value) ? value : null;
        }
    }

    /// <summary>
    /// Checks if a state key exists.
    /// </summary>
    public bool HasState(string key)
    {
        lock (_lockObject)
        {
            return _state.ContainsKey(key);
        }
    }

    /// <summary>
    /// Terminates this actor.
    /// </summary>
    public async Task TerminateAsync()
    {
        if (State == ActorState.Terminated)
            return;

        State = ActorState.Stopping;
        try
        {
            await OnStopAsync();
            State = ActorState.Terminated;
            TerminatedAt = DateTime.UtcNow;
            Ref.MarkAsDead();
        }
        catch (Exception ex)
        {
            State = ActorState.Error;
            throw new InvalidOperationException($"Error terminating actor {Path}", ex);
        }
    }

    /// <summary>
    /// Gets the current metrics summary.
    /// </summary>
    public ActorMetricsSummary GetMetricsSummary() => Metrics.GetSummary();

    /// <summary>
    /// Called when the actor is initialized. Override to add custom initialization logic.
    /// </summary>
    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    /// <summary>
    /// Called to process messages. Override to add message handling logic.
    /// </summary>
    protected virtual Task OnReceiveAsync(Message message) => Task.CompletedTask;

    /// <summary>
    /// Called when an error occurs processing a message.
    /// </summary>
    protected virtual Task OnErrorAsync(Message message, Exception exception) => Task.CompletedTask;

    /// <summary>
    /// Called when the actor is stopping. Override to add cleanup logic.
    /// </summary>
    protected virtual Task OnStopAsync() => Task.CompletedTask;

    public override string ToString() => $"Actor({Path})";
}
