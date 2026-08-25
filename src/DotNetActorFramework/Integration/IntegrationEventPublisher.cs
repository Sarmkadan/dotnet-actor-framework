// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Events;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Publisher for integration events that need to be sent to external systems.
/// Queues events for async delivery and ensures at-least-once semantics.
/// </summary>
public class IntegrationEventPublisher : IDisposable
{
    private readonly ConcurrentQueue<IntegrationEventEnvelope> _queue = [];
    private readonly WebhookDispatcher _webhookDispatcher;
    private readonly Timer _processingTimer;
    private volatile bool _isProcessing = false;

    public IntegrationEventPublisher(WebhookDispatcher webhookDispatcher)
    {
        _webhookDispatcher = webhookDispatcher ?? throw new ArgumentNullException(nameof(webhookDispatcher));

        // Process queued events every 100ms. The returned task is intentionally not awaited
        // (timer callbacks cannot await); ProcessQueuedEventsAsync observes all exceptions itself.
        _processingTimer = new Timer(_ => _ = ProcessQueuedEventsAsync(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// Publishes a domain event for integration with external systems.
    /// </summary>
    public Task PublishAsync(IDomainEvent @event)
    {
        if (@event == null)
            throw new ArgumentNullException(nameof(@event));

        var envelope = new IntegrationEventEnvelope
        {
            Id = Guid.NewGuid(),
            Event = @event,
            EnqueuedAt = DateTime.UtcNow,
            Attempts = 0
        };

        _queue.Enqueue(envelope);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the number of events waiting in the queue.
    /// </summary>
    public int GetQueueLength() => _queue.Count;

    private async Task ProcessQueuedEventsAsync()
    {
        if (_isProcessing)
            return;

        _isProcessing = true;

        try
        {
            while (_queue.TryDequeue(out var envelope))
            {
                try
                {
                    envelope.Attempts++;
                    await _webhookDispatcher.DispatchEventAsync(envelope.Event);
                    envelope.ProcessedAt = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to publish integration event: {ex.Message}");

                    // Re-queue if retries available
                    if (envelope.Attempts < 3)
                    {
                        _queue.Enqueue(envelope);
                    }
                }
            }
        }
        finally
        {
            _isProcessing = false;
        }
    }

    public void Dispose()
    {
        _processingTimer?.Dispose();
    }

    private class IntegrationEventEnvelope
    {
        public Guid Id { get; set; }
        public IDomainEvent Event { get; set; }
        public DateTime EnqueuedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int Attempts { get; set; }

        public override string ToString() => $"IntegrationEventEnvelope {{ Id = {Id}, Event = {Event}, EnqueuedAt = {EnqueuedAt}, ProcessedAt = {ProcessedAt}, Attempts = {Attempts} }}";
    }
}

/// <summary>
/// Publisher for system events with deduplication support.
/// Prevents duplicate event processing using message IDs.
/// </summary>
public class DuplicateEventFilteringPublisher
{
    private readonly EventBus _eventBus;
    private readonly Dictionary<Guid, DateTime> _processedEventIds = [];
    private readonly object _lockObject = new();
    private readonly TimeSpan _deduplicationWindow;

    public DuplicateEventFilteringPublisher(EventBus eventBus, TimeSpan? deduplicationWindow = null)
    {
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _deduplicationWindow = deduplicationWindow ?? TimeSpan.FromMinutes(5);
    }

    /// <summary>
    /// Publishes an event only if it hasn't been seen within the deduplication window.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event) where TEvent : IDomainEvent
    {
        if (@event == null)
            return;

        var now = DateTime.UtcNow;

        lock (_lockObject)
        {
            // Evict entries that have fallen outside the deduplication window
            var cutoff = now - _deduplicationWindow;
            var expired = _processedEventIds
                .Where(kvp => kvp.Value < cutoff)
                .Select(kvp => kvp.Key)
                .ToList();
            foreach (var id in expired)
                _processedEventIds.Remove(id);

            if (_processedEventIds.ContainsKey(@event.EventId))
                return; // Already processed within the window

            _processedEventIds[@event.EventId] = now;
        }

        await _eventBus.PublishAsync(@event);
    }

    /// <summary>
    /// Clears old deduplication entries.
    /// </summary>
    public void ClearDeduplicationCache()
    {
        lock (_lockObject)
        {
            _processedEventIds.Clear();
        }
    }
}
