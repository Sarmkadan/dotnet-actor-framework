namespace DotNetActorFramework.Models;

/// <summary>
/// Provides extension methods for <see cref="Actor"/>.
/// </summary>
public static class ActorExtensions
{
    /// <summary>
    /// Checks if the actor is terminated.
    /// </summary>
    /// <param name="actor">The actor to check.</param>
    /// <returns><c>true</c> if the actor is terminated; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actor"/> is <c>null</c>.</exception>
    public static bool IsTerminated(this Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.TerminatedAt.HasValue;
    }

    /// <summary>
    /// Checks if the actor is active.
    /// </summary>
    /// <param name="actor">The actor to check.</param>
    /// <returns><c>true</c> if the actor is active; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actor"/> is <c>null</c>.</exception>
    public static bool IsActive(this Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return !actor.TerminatedAt.HasValue;
    }

    /// <summary>
    /// Gets a summary of the actor's metrics.
    /// </summary>
    /// <param name="actor">The actor to get metrics for.</param>
    /// <returns>A summary of the actor's metrics.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actor"/> is <c>null</c>.</exception>
    public static ActorMetricsSummary GetMetricsSummary(this Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.GetMetricsSummary();
    }
}
