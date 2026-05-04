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
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
}

/// <summary>
/// Base class for domain events.
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    public Guid EventId { get; }
    public DateTime OccurredAt { get; }
    public abstract string EventType { get; }

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
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Subscribes to events of a specific type.
    /// </summary>
    public void Subscribe<TEvent>(EventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent).Name;

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
    /// Unsubscribes from events of a specific type.
    /// </summary>
    public void Unsubscribe<TEvent>(EventHandler<TEvent> handler) where TEvent : IDomainEvent
    {
        if (handler == null)
            return;

        var eventType = typeof(TEvent).Name;

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
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = @event.GetType().Name;
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
    /// Gets the number of subscribers for an event type.
    /// </summary>
    public int GetSubscriberCount<TEvent>() where TEvent : IDomainEvent
    {
        var eventType = typeof(TEvent).Name;
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
public class ActorSystemEvent : DomainEvent
{
    public string SystemName { get; set; }
    public Guid SystemId { get; set; }
}

/// <summary>
/// Event fired when an actor is created.
/// </summary>
public class ActorCreatedEvent : ActorSystemEvent
{
    public override string EventType => "actor.created";
    public string ActorPath { get; set; }
    public Guid ActorId { get; set; }
}

/// <summary>
/// Event fired when an actor is terminated.
/// </summary>
public class ActorTerminatedEvent : ActorSystemEvent
{
    public override string EventType => "actor.terminated";
    public string ActorPath { get; set; }
    public Guid ActorId { get; set; }
}

/// <summary>
/// Event fired when an actor encounters an error.
/// </summary>
public class ActorErrorEvent : ActorSystemEvent
{
    public override string EventType => "actor.error";
    public string ActorPath { get; set; }
    public Guid ActorId { get; set; }
    public string ErrorMessage { get; set; }
    public string StackTrace { get; set; }
}

/// <summary>
/// Event fired when a message is processed.
/// </summary>
public class MessageProcessedEvent : ActorSystemEvent
{
    public override string EventType => "message.processed";
    public string ActorPath { get; set; }
    public Guid MessageId { get; set; }
    public string MessageType { get; set; }
    public long ElapsedMilliseconds { get; set; }
}
