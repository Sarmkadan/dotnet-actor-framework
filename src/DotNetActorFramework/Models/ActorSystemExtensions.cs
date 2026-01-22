namespace DotNetActorFramework.Models;

/// <summary>
/// Provides extension methods for <see cref="ActorSystem"/>.
/// </summary>
public static class ActorSystemExtensions
{
    /// <summary>
    /// Checks if the actor system has been shut down.
    /// </summary>
    /// <param name="actorSystem">The actor system to check.</param>
    /// <returns><c>true</c> if the actor system has been shut down; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorSystem"/> is <c>null</c>.</exception>
    public static bool IsShutdown(this ActorSystem actorSystem)
    {
        ArgumentNullException.ThrowIfNull(actorSystem);
        return actorSystem.ShutdownAt.HasValue;
    }

    /// <summary>
    /// Checks if the actor system is active (i.e., not shut down and still running).
    /// </summary>
    /// <param name="actorSystem">The actor system to check.</param>
    /// <returns><c>true</c> if the actor system is active; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorSystem"/> is <c>null</c>.</exception>
    public static bool IsActive(this ActorSystem actorSystem)
    {
        ArgumentNullException.ThrowIfNull(actorSystem);
        return actorSystem.IsRunning && !actorSystem.IsShutdown();
    }

    /// <summary>
    /// Gets the uptime of the actor system.
    /// </summary>
    /// <param name="actorSystem">The actor system.</param>
    /// <returns>The uptime of the actor system.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorSystem"/> is <c>null</c>.</exception>
    public static TimeSpan GetUptime(this ActorSystem actorSystem)
    {
        ArgumentNullException.ThrowIfNull(actorSystem);
        return DateTime.UtcNow - actorSystem.CreatedAt;
    }
}
