// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetActorFramework.Events;

/// <summary>
/// Domain event interface for pub/sub messaging.
/// Events represent significant occurrences in the actor system.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the event occurred.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Gets the logical type name of the event (for example, <c>actor.created</c>).
    /// </summary>
    string EventType { get; }
}

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier of this event instance.
    /// </summary>
    public Guid EventId { get; }

    /// <summary>
    /// Gets the UTC timestamp at which the event was created.
    /// </summary>
    public DateTime OccurredAt { get; }

    /// <summary>
    /// Gets the logical type name of the event.
    /// </summary>
    public abstract string EventType { get; }

    /// <summary>
    /// Initializes a new instance with a new <see cref="EventId"/> and the current UTC time.
    /// </summary>
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Event handler delegate.
/// </summary>
public delegate Task EventHandler<in TEvent>(TEvent @event) where TEvent : IDomainEvent;

/// <summary>
/// Event bus for publishing and subscribing to domain events.
/// Provides pub/sub capabilities for loosely coupled actor components.
/// </summary>
public class EventBus
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    public void Subscribe<TEvent>(EventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        lock (_lockObject)
        {
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = [];
                _subscribers[eventType] = handlers;
            }

            handlers.Add(handler);
        }
    }

    /// <summary>
    /// Subscribes to the first event of a specific type and automatically unsubscribes
    /// the handler before it is invoked.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to subscribe to.</typeparam>
    /// <param name="handler">The handler to invoke at most once.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="handler"/> is <see langword="null"/>.
    /// </exception>
    public void SubscribeOnce<TEvent>(EventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        EventHandler<TEvent>? oneTimeHandler = null;
        var invoked = 0;

        oneTimeHandler = async @event =>
        {
            if (Interlocked.Exchange(ref invoked, 1) != 0)
                return;

            Unsubscribe(oneTimeHandler!);
            await handler(@event);
        };

        Subscribe(oneTimeHandler);
    }

    /// <summary>
    /// Unsubscribes from events of a specific type.
    /// </summary>
    public void Unsubscribe<TEvent>(EventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        if (handler == null)
            return;

        var eventType = typeof(TEvent);

        lock (_lockObject)
        {
            if (_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                if (handlers.Count == 0)
                    _subscribers.TryRemove(eventType, out _);
            }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// Executes handlers asynchronously in parallel.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        // Look up by the static type parameter so it matches the key used by Subscribe;
        // handlers are stored as EventHandler<TEvent> and would otherwise be silently skipped.
        var eventType = typeof(TEvent);
        List<Delegate>? handlers = null;

        lock (_lockObject)
        {
            if (_subscribers.TryGetValue(eventType, out var subs))
            {
                handlers = new List<Delegate>(subs);
            }
        }

        if (handlers != null)
        {
            var tasks = handlers
                .OfType<EventHandler<TEvent>>()
                .Select(h => h(@event))
                .ToList();

            await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers and captures failures from each
    /// handler without preventing the remaining handlers from running.
    /// Executes handlers asynchronously in parallel.
    /// </summary>
    /// <typeparam name="TEvent">The type of domain event to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>
    /// A task whose result contains the exceptions thrown by handlers. The collection
    /// is empty when every handler completes successfully.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="event"/> is <see langword="null"/>.
    /// </exception>
    public async Task<IReadOnlyList<Exception>> TryPublishAsync<TEvent>(TEvent @event)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = typeof(TEvent);
        List<Delegate>? handlers = null;

        lock (_lockObject)
        {
            if (_subscribers.TryGetValue(eventType, out var subs))
            {
                handlers = new List<Delegate>(subs);
            }
        }

        if (handlers == null)
            return Array.Empty<Exception>();

        var tasks = handlers
            .OfType<EventHandler<TEvent>>()
            .Select(InvokeHandlerAsync)
            .ToList();

        var exceptions = await Task.WhenAll(tasks);
        return exceptions.OfType<Exception>().ToList();

        async Task<Exception?> InvokeHandlerAsync(EventHandler<TEvent> handler)
        {
            try
            {
                await handler(@event);
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
        }
    }

    /// <summary>
    /// Gets the number of subscribers for an event type.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent);
        lock (_lockObject)
        {
            return _subscribers.TryGetValue(eventType, out var handlers) ? handlers.Count : 0;
        }
    }

    /// <summary>
    /// Clears all subscribers.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _subscribers.Clear();
        }
    }
}

/// <summary>
/// Base class for actor system events.
/// </summary>
public abstract class ActorSystemEvent : DomainEvent
{
    /// <summary>
    /// Gets or sets the name of the actor system that raised the event.
    /// </summary>
    public string SystemName { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the actor system that raised the event.
    /// </summary>
    public Guid SystemId { get; set; }
}

/// <summary>
/// Event fired when an actor is created.
/// </summary>
public class ActorCreatedEvent : ActorSystemEvent
{
    /// <summary>
    /// Gets the event type name, <c>actor.created</c>.
    /// </summary>
    public override string EventType => "actor.created";

    /// <summary>
    /// Gets or sets the path of the created actor.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the created actor.
    /// </summary>
    public Guid ActorId { get; set; }
}

/// <summary>
/// Event fired when an actor is terminated.
/// </summary>
public class ActorTerminatedEvent : ActorSystemEvent
{
    /// <summary>
    /// Gets the event type name, <c>actor.terminated</c>.
    /// </summary>
    public override string EventType => "actor.terminated";

    /// <summary>
    /// Gets or sets the path of the terminated actor.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the terminated actor.
    /// </summary>
    public Guid ActorId { get; set; }
}

/// <summary>
/// Event fired when an actor encounters an error.
/// </summary>
public class ActorErrorEvent : ActorSystemEvent
{
    /// <summary>
    /// Gets the event type name, <c>actor.error</c>.
    /// </summary>
    public override string EventType => "actor.error";

    /// <summary>
    /// Gets or sets the path of the actor that encountered the error.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the actor that encountered the error.
    /// </summary>
    public Guid ActorId { get; set; }

    /// <summary>
    /// Gets or sets the message of the error that occurred.
    /// </summary>
    public string ErrorMessage { get; set; } = null!;

    /// <summary>
    /// Gets or sets the stack trace of the error that occurred.
    /// </summary>
    public string StackTrace { get; set; } = null!;
}

/// <summary>
/// Event fired when a message is processed.
/// </summary>
public class MessageProcessedEvent : ActorSystemEvent
{
    /// <summary>
    /// Gets the event type name, <c>message.processed</c>.
    /// </summary>
    public override string EventType => "message.processed";

    /// <summary>
    /// Gets or sets the path of the actor that processed the message.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the identifier of the processed message.
    /// </summary>
    public Guid MessageId { get; set; }

    /// <summary>
    /// Gets or sets the type name of the processed message.
    /// </summary>
    public string MessageType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the time taken to process the message, in milliseconds.
    /// </summary>
    public long ElapsedMilliseconds { get; set; }
}
