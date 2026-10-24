// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

/// <summary>
/// A bounded, thread-safe store for envelopes that could not be delivered.
/// When the queue is full, the oldest envelope is evicted to make room for the newest one.
/// Subscribers are notified synchronously for every envelope that is added.
/// </summary>
public sealed class DeadLetterQueue
{
    private readonly Queue<Envelope> _envelopes = new();
    private readonly List<Subscription> _subscriptions = [];
    private readonly object _lockObject = new();
    private long _evictedCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeadLetterQueue"/> class.
    /// </summary>
    /// <param name="capacity">The maximum number of envelopes retained at once. Must be greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="capacity"/> is less than one.</exception>
    public DeadLetterQueue(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than zero.");

        Capacity = capacity;
    }

    /// <summary>Gets the maximum number of envelopes retained at once.</summary>
    public int Capacity { get; }

    /// <summary>Gets the number of envelopes currently retained.</summary>
    public int Count
    {
        get
        {
            lock (_lockObject)
            {
                return _envelopes.Count;
            }
        }
    }

    /// <summary>Gets the total number of envelopes evicted because the queue was full.</summary>
    public long EvictedCount
    {
        get
        {
            lock (_lockObject)
            {
                return _evictedCount;
            }
        }
    }

    /// <summary>
    /// Adds an undeliverable envelope to the queue, evicting the oldest one if the queue is full,
    /// then notifies all subscribers.
    /// </summary>
    /// <param name="envelope">The envelope that could not be delivered.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="envelope"/> is null.</exception>
    public void Enqueue(Envelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        Subscription[] subscribers;
        lock (_lockObject)
        {
            if (_envelopes.Count == Capacity)
            {
                _envelopes.Dequeue();
                _evictedCount++;
            }

            _envelopes.Enqueue(envelope);
            subscribers = _subscriptions.ToArray();
        }

        // Notify outside the lock so subscribers may call back into the queue.
        foreach (var subscriber in subscribers)
        {
            subscriber.Notify(envelope);
        }
    }

    /// <summary>
    /// Gets a point-in-time copy of the retained envelopes, ordered oldest first.
    /// </summary>
    public IReadOnlyList<Envelope> GetSnapshot()
    {
        lock (_lockObject)
        {
            return _envelopes.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Removes all retained envelopes. Eviction statistics are preserved.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _envelopes.Clear();
        }
    }

    /// <summary>
    /// Subscribes to envelopes as they are added to the queue.
    /// The handler runs on the thread that enqueues the envelope, so it should return quickly.
    /// An exception thrown by a handler is swallowed so that it cannot disrupt message delivery
    /// or the other subscribers.
    /// </summary>
    /// <param name="handler">The callback to invoke for each new dead letter.</param>
    /// <returns>An <see cref="IDisposable"/> that removes the subscription when disposed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="handler"/> is null.</exception>
    public IDisposable Subscribe(Action<Envelope> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var subscription = new Subscription(this, handler);
        lock (_lockObject)
        {
            _subscriptions.Add(subscription);
        }

        return subscription;
    }

    private void Unsubscribe(Subscription subscription)
    {
        lock (_lockObject)
        {
            _subscriptions.Remove(subscription);
        }
    }

    private sealed class Subscription(DeadLetterQueue owner, Action<Envelope> handler) : IDisposable
    {
        private int _disposed;

        public void Notify(Envelope envelope)
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;

            try
            {
                handler(envelope);
            }
            catch (Exception)
            {
                // Faulty subscribers must not break delivery or starve other subscribers.
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
            {
                owner.Unsubscribe(this);
            }
        }
    }
}
