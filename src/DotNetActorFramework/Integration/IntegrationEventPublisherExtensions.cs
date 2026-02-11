// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics;
using DotNetActorFramework.Events;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Extension methods for <see cref="IntegrationEventPublisher"/> providing additional functionality
/// for monitoring, batching, and event management.
/// </summary>
public static class IntegrationEventPublisherExtensions
{
    /// <summary>
    /// Waits for all queued events to be processed.
    /// </summary>
    /// <param name="publisher">The event publisher instance</param>
    /// <param name="timeout">Maximum time to wait for processing to complete</param>
    /// <returns>True if all events were processed, false if timeout occurred</returns>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative or zero</exception>
    public static async Task<bool> WaitForProcessingAsync(this IntegrationEventPublisher publisher, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        if (timeout < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout cannot be negative");

        var stopwatch = Stopwatch.StartNew();
        int previousQueueLength;

        do
        {
            previousQueueLength = publisher.GetQueueLength();

            if (previousQueueLength == 0)
                return true;

            // Give the processing timer a chance to process events
            await Task.Delay(TimeSpan.FromMilliseconds(50));

            if (stopwatch.Elapsed >= timeout)
                return false;
        }
        while (true);
    }

    /// <summary>
    /// Publishes a batch of events as a single operation.
    /// </summary>
    /// <param name="publisher">The event publisher instance</param>
    /// <param name="events">Collection of events to publish</param>
    /// <returns>Task representing the batch publish operation</returns>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> or <paramref name="events"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException"><paramref name="events"/> contains a <see langword="null"/> event</exception>
    public static async Task PublishBatchAsync(this IntegrationEventPublisher publisher, IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        ArgumentNullException.ThrowIfNull(events);

        foreach (var @event in events)
        {
            ArgumentNullException.ThrowIfNull(@event);
            await publisher.PublishAsync(@event);
        }
    }

    /// <summary>
    /// Gets the approximate processing rate of events in events per second.
    /// </summary>
    /// <param name="publisher">The event publisher instance</param>
    /// <param name="sampleDuration">Duration to sample processing rate</param>
    /// <returns>Events per second, or 0 if no events processed</returns>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sampleDuration"/> is negative or zero</exception>
    public static async Task<double> GetProcessingRateAsync(this IntegrationEventPublisher publisher, TimeSpan sampleDuration)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        if (sampleDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(sampleDuration), "Sample duration must be positive");

        var initialQueueLength = publisher.GetQueueLength();
        var initialTime = DateTime.UtcNow;

        await Task.Delay(sampleDuration);

        var finalQueueLength = publisher.GetQueueLength();
        var finalTime = DateTime.UtcNow;

        var processedCount = initialQueueLength - finalQueueLength;
        var elapsedSeconds = (finalTime - initialTime).TotalSeconds;

        return elapsedSeconds > 0 ? processedCount / elapsedSeconds : 0;
    }

}

