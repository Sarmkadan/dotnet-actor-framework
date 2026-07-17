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
    /// <returns><see langword="true"/> if the actor is terminated; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> is <see langword="null"/>.</exception>
    public static bool IsTerminated(this Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.TerminatedAt.HasValue;
    }

    /// <summary>
    /// Checks if the actor is active.
    /// </summary>
    /// <param name="actor">The actor to check.</param>
    /// <returns><see langword="true"/> if the actor is active; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> is <see langword="null"/>.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="actor"/> is <see langword="null"/>.</exception>
    public static ActorMetricsSummary GetMetricsSummary(this Actor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return actor.GetMetricsSummary();
    }
}