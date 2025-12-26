// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Services;

/// <summary>
/// Provides extension methods for <see cref="ActorRegistry"/> to simplify common actor management scenarios.
/// </summary>
public static class ActorRegistryExtensions
{
    /// <summary>
    /// Attempts to get an actor by path, returning null if not found.
    /// </summary>
    /// <param name="registry">The actor registry.</param>
    /// <param name="path">The actor path to lookup.</param>
    /// <returns>The actor reference if found, otherwise null.</returns>
    public static ActorRef? Get(this ActorRegistry registry, ActorPath path)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.GetByPath(path);
    }

    /// <summary>
    /// Attempts to get an actor by ID, returning null if not found.
    /// </summary>
    /// <param name="registry">The actor registry.</param>
    /// <param name="id">The actor ID to lookup.</param>
    /// <returns>The actor reference if found, otherwise null.</returns>
    public static ActorRef? Get(this ActorRegistry registry, Guid id)
    {
        ArgumentNullException.ThrowIfNull(registry);
        return registry.GetById(id);
    }

    /// <summary>
    /// Gets all actors that match a predicate.
    /// </summary>
    /// <param name="registry">The actor registry.</param>
    /// <param name="predicate">The predicate to match actors.</param>
    /// <returns>A read-only list of matching actors.</returns>
    public static IReadOnlyList<ActorRef> FindAll(this ActorRegistry registry, Func<ActorRef, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(predicate);

        return registry.GetAll().Where(predicate).ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the root actor (actor with no parent path).
    /// </summary>
    /// <param name="registry">The actor registry.</param>
    /// <returns>The root actor if one exists, otherwise null.</returns>
    public static ActorRef? GetRoot(this ActorRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        return registry.GetAll().FirstOrDefault(actor => actor.Path.Parent == null);
    }
}