// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetActorFramework.Services;

/// <summary>
/// Extension methods for SupervisionService providing additional supervision utilities.
/// </summary>
public static class SupervisionServiceExtensions
{
    /// <summary>
    /// Checks if an actor has exceeded its maximum failure threshold.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <param name="actorId">The actor ID</param>
    /// <param name="maxFailures">Maximum allowed failures before escalation. Defaults to 5.</param>
    /// <returns>True if actor has exceeded failure threshold</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    public static bool HasExceededFailureThreshold(this SupervisionService service, Guid actorId, int maxFailures = 5)
    {
        ArgumentNullException.ThrowIfNull(service);

        var context = service.GetContext(actorId);
        return context != null && context.FailureCount >= maxFailures;
    }

    /// <summary>
    /// Gets statistics for a specific actor.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <param name="actorId">The actor ID</param>
    /// <returns>Supervision statistics for the actor or null if not supervised</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    public static ActorSupervisionStatistics? GetActorStatistics(this SupervisionService service, Guid actorId)
    {
        ArgumentNullException.ThrowIfNull(service);

        var context = service.GetContext(actorId);
        if (context == null)
            return null;

        return new ActorSupervisionStatistics
        {
            ActorId = actorId,
            FailureCount = context.FailureCount,
            RestartCount = context.RestartCount,
            LastFailureTime = context.LastFailureTime,
            TimeSinceLastFailure = context.GetTimeSinceLastFailure()
        };
    }

    /// <summary>
    /// Gets statistics for all supervised actors.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <returns>Dictionary of actor statistics keyed by actor ID</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    public static Dictionary<Guid, ActorSupervisionStatistics> GetAllActorStatistics(this SupervisionService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var contexts = service.GetAllContexts();
        return contexts.ToDictionary(
            kvp => kvp.Key,
            kvp => new ActorSupervisionStatistics
            {
                ActorId = kvp.Key,
                FailureCount = kvp.Value.FailureCount,
                RestartCount = kvp.Value.RestartCount,
                LastFailureTime = kvp.Value.LastFailureTime,
                TimeSinceLastFailure = kvp.Value.GetTimeSinceLastFailure()
            }
        );
    }

    /// <summary>
    /// Gets actors that have failed recently (within the specified time window).
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <param name="timeWindow">Time window to check for recent failures</param>
    /// <returns>Collection of actor IDs that have failed recently</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeWindow"/> is negative.</exception>
    public static IEnumerable<Guid> GetRecentlyFailedActors(this SupervisionService service, TimeSpan timeWindow)
    {
        ArgumentNullException.ThrowIfNull(service);
        if (timeWindow < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeWindow), "Time window cannot be negative");

        var cutoffTime = DateTime.UtcNow - timeWindow;
        return service.GetAllContexts()
            .Where(kvp => kvp.Value.LastFailureTime >= cutoffTime)
            .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Gets the actor ID with the highest failure count.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <returns>The actor ID with most failures, or <see cref="Guid.Empty"/> if no actors supervised</returns>
    /// <exception cref="ArgumentNullException"><paramref name="service"/> is null.</exception>
    public static Guid GetWorstPerformingActor(this SupervisionService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        var contexts = service.GetAllContexts();
        return contexts.Count == 0
            ? Guid.Empty
            : contexts
                .OrderByDescending(kvp => kvp.Value.FailureCount)
                .First().Key;
    }
}

/// <summary>
/// Statistics for a specific actor's supervision.
/// </summary>
public sealed class ActorSupervisionStatistics
{
    /// <summary>
    /// Gets the actor ID.
    /// </summary>
    public Guid ActorId { get; init; }

    /// <summary>
    /// Gets the number of failures for this actor.
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Gets the number of restarts for this actor.
    /// </summary>
    public int RestartCount { get; init; }

    /// <summary>
    /// Gets the timestamp of the last failure.
    /// </summary>
    public DateTime LastFailureTime { get; init; }

    /// <summary>
    /// Gets the time elapsed since the last failure.
    /// </summary>
    public TimeSpan TimeSinceLastFailure { get; init; }
}