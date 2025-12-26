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
    public static async Task<bool> WaitForProcessingAsync(this IntegrationEventPublisher publisher, TimeSpan timeout)
    {
        if (publisher == null)
            throw new ArgumentNullException(nameof(publisher));

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
    public static async Task PublishBatchAsync(this IntegrationEventPublisher publisher, IEnumerable<IDomainEvent> events)
    {
        if (publisher == null)
            throw new ArgumentNullException(nameof(publisher));

        if (events == null)
            throw new ArgumentNullException(nameof(events));

        foreach (var @event in events)
        {
            await publisher.PublishAsync(@event);
        }
    }

    /// <summary>
    /// Gets the approximate processing rate of events in events per second.
    /// </summary>
    /// <param name="publisher">The event publisher instance</param>
    /// <param name="sampleDuration">Duration to sample processing rate</param>
    /// <returns>Events per second, or 0 if no events processed</returns>
    public static async Task<double> GetProcessingRateAsync(this IntegrationEventPublisher publisher, TimeSpan sampleDuration)
    {
        if (publisher == null)
            throw new ArgumentNullException(nameof(publisher));

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

