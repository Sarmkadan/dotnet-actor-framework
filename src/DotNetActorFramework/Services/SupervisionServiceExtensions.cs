// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetActorFramework.Models;
using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Services;

/// <summary>
/// Extension methods for SupervisionService providing additional supervision utilities.
/// </summary>
public static class SupervisionServiceExtensions
{
    /// <summary>
    /// Gets the supervision context for the specified actor.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <param name="actorId">The actor ID</param>
    /// <returns>The supervision context or null if not found</returns>
    public static SupervisionContext? GetContext(this SupervisionService service, Guid actorId)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var contexts = service.GetType()
            .GetField("_supervisionContexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(service) as System.Collections.Generic.Dictionary<Guid, SupervisionContext>;

        return contexts?.GetValueOrDefault(actorId);
    }

    /// <summary>
    /// Checks if an actor has exceeded its maximum failure threshold.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <param name="actorId">The actor ID</param>
    /// <param name="maxFailures">Maximum allowed failures before escalation</param>
    /// <returns>True if actor has exceeded failure threshold</returns>
    public static bool HasExceededFailureThreshold(this SupervisionService service, Guid actorId, int maxFailures = 5)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var context = service.GetContext(actorId);
        return context != null && context.FailureCount >= maxFailures;
    }

    /// <summary>
    /// Gets statistics for a specific actor.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <param name="actorId">The actor ID</param>
    /// <returns>Supervision statistics for the actor or null if not supervised</returns>
    public static ActorSupervisionStatistics? GetActorStatistics(this SupervisionService service, Guid actorId)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

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
    public static System.Collections.Generic.Dictionary<Guid, ActorSupervisionStatistics> GetAllActorStatistics(this SupervisionService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var contexts = service.GetType()
            .GetField("_supervisionContexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(service) as System.Collections.Generic.Dictionary<Guid, SupervisionContext>;

        if (contexts == null)
            return new System.Collections.Generic.Dictionary<Guid, ActorSupervisionStatistics>();

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
    public static System.Collections.Generic.IEnumerable<Guid> GetRecentlyFailedActors(
        this SupervisionService service,
        TimeSpan timeWindow)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var contexts = service.GetType()
            .GetField("_supervisionContexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(service) as System.Collections.Generic.Dictionary<Guid, SupervisionContext>;

        if (contexts == null)
            return System.Linq.Enumerable.Empty<Guid>();

        var cutoffTime = DateTime.UtcNow - timeWindow;
        return contexts
            .Where(kvp => kvp.Value.LastFailureTime >= cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();
    }

    /// <summary>
    /// Gets the actor ID with the highest failure count.
    /// </summary>
    /// <param name="service">The supervision service</param>
    /// <returns>The actor ID with most failures, or Guid.Empty if no actors supervised</returns>
    public static Guid GetWorstPerformingActor(this SupervisionService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));

        var contexts = service.GetType()
            .GetField("_supervisionContexts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(service) as System.Collections.Generic.Dictionary<Guid, SupervisionContext>;

        if (contexts == null || contexts.Count == 0)
            return Guid.Empty;

        return contexts
            .OrderByDescending(kvp => kvp.Value.FailureCount)
            .First().Key;
    }
}

/// <summary>
/// Statistics for a specific actor's supervision.
/// </summary>
public class ActorSupervisionStatistics
{
    public Guid ActorId { get; set; }
    public int FailureCount { get; set; }
    public int RestartCount { get; set; }
    public DateTime LastFailureTime { get; set; }
    public TimeSpan TimeSinceLastFailure { get; set; }
}