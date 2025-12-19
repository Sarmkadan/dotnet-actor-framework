// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Models;

/// <summary>
/// Represents an actor in the system.
/// Actors are lightweight, single-threaded entities that process messages sequentially
/// from their mailbox. Each actor encapsulates its own state, which is never shared
/// directly with other actors - all communication happens via asynchronous message passing.
/// </summary>
/// <remarks>
/// <para>
/// To create a custom actor, inherit from this class and override the virtual methods:
/// <list type="bullet">
///   <item><see cref="OnInitializeAsync"/> - one-time setup when the actor starts</item>
///   <item><see cref="OnReceiveAsync"/> - called for each incoming message</item>
///   <item><see cref="OnErrorAsync"/> - invoked when message processing throws</item>
///   <item><see cref="OnStopAsync"/> - cleanup before the actor is terminated</item>
/// </list>
/// </para>
/// <para>
/// Actors transition through a well-defined lifecycle:
/// <c>Created -> Initializing -> Started -> Stopping -> Terminated</c>.
/// If an unhandled exception occurs, the actor moves to <see cref="ActorState.Error"/>
/// and the configured supervision strategy determines recovery behavior.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class GreeterActor : Actor
/// {
///     public GreeterActor(ActorPath path) : base(path) { }
///
///     protected override Task OnReceiveAsync(Message message)
///     {
///         if (message is ControlMessage cm)
///             Console.WriteLine($"Hello, {cm.Command}!");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public class Actor
{
    /// <summary>Unique identifier assigned at construction time.</summary>
    public Guid Id { get; }

    /// <summary>A serializable reference that other actors use to send messages to this actor.</summary>
    public ActorRef Ref { get; }

    /// <summary>Hierarchical address within the actor system (e.g. <c>/user/orders/processor</c>).</summary>
    public ActorPath Path { get; }

    /// <summary>Current lifecycle state. See <see cref="ActorState"/> for possible values.</summary>
    public ActorState State { get; private set; }

    /// <summary>Performance counters tracking messages processed, errors, and latency.</summary>
    public ActorMetrics Metrics { get; }

    /// <summary>UTC timestamp of when this actor instance was created.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>UTC timestamp of when <see cref="TerminateAsync"/> completed, or <c>null</c> if still alive.</summary>
    public DateTime? TerminatedAt { get; private set; }

    /// <summary>
    /// Reference to the supervising actor. When this actor fails, the supervisor
    /// applies its <see cref="Enums.SupervisionStrategy"/> to decide whether to
    /// restart, stop, or escalate.
    /// </summary>
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
    /// Transitions the actor from <see cref="ActorState.Created"/> to
    /// <see cref="ActorState.Started"/> by invoking <see cref="OnInitializeAsync"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the actor is not in <see cref="ActorState.Created"/> state,
    /// or when <see cref="OnInitializeAsync"/> throws.
    /// </exception>
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
    /// Processes a single message by delegating to <see cref="OnReceiveAsync"/>.
    /// Records processing time and error counts in <see cref="Metrics"/>.
    /// </summary>
    /// <param name="message">The message to process. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="message"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the actor is not in <see cref="ActorState.Started"/> state.</exception>
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
    /// Stores a value in the actor's thread-safe internal state dictionary.
    /// If the key already exists, its value is overwritten.
    /// </summary>
    /// <param name="key">A non-empty key identifying the state entry.</param>
    /// <param name="value">The value to store.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
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
    /// Retrieves a value from the actor's internal state dictionary.
    /// Returns <c>null</c> if the key does not exist.
    /// </summary>
    /// <param name="key">A non-empty key identifying the state entry.</param>
    /// <returns>The stored value, or <c>null</c> if the key is not found.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
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
    /// Gracefully terminates this actor by invoking <see cref="OnStopAsync"/>,
    /// transitioning through <see cref="ActorState.Stopping"/> to
    /// <see cref="ActorState.Terminated"/>, and marking the <see cref="Ref"/> as dead.
    /// Calling this on an already-terminated actor is a safe no-op.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="OnStopAsync"/> fails - the actor enters
    /// <see cref="ActorState.Error"/> state in this case.
    /// </exception>
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
