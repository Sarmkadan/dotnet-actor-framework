// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Extension methods for ActorPath manipulation and querying.
/// These utilities simplify common path operations like hierarchy traversal and relative path calculation.
/// </summary>
public static class ActorPathExtensions
{
    /// <summary>
    /// Gets the relative path from a parent actor to this actor.
    /// Returns null if the path is not a descendant of the specified parent.
    /// </summary>
    /// <param name="path">The actor path to calculate the relative path from.</param>
    /// <param name="parent">The parent actor path to use as the base.</param>
    /// <returns>The relative path from parent to path, or null if path is not a descendant of parent.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path or parent is null.</exception>
    public static string? GetRelativePath(this ActorPath path, ActorPath parent)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(parent);

        var pathStr = path.ToString();
        var parentStr = parent.ToString();

        if (!pathStr.StartsWith(parentStr + "/", StringComparison.Ordinal))
            return null;

        return pathStr[(parentStr.Length + 1)..];
    }

    /// <summary>
    /// Gets all ancestor paths from this path to the root, including the current path itself.
    /// Useful for traversing the supervision hierarchy or building path-based context.
    /// </summary>
    /// <param name="path">The actor path to get ancestors from.</param>
    /// <returns>An enumerable of ancestor paths starting from the current path up to the root.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    public static IEnumerable<ActorPath> GetAncestors(this ActorPath path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var current = path;
        while (current != null)
        {
            yield return current;
            current = current.Parent;
        }
    }

    /// <summary>
    /// Determines if this path is a child of the specified parent path.
    /// A path is considered a child if it starts with the parent path followed by a '/'.
    /// </summary>
    /// <param name="path">The actor path to check.</param>
    /// <param name="parent">The potential parent path.</param>
    /// <returns>True if path is a child of parent; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path or parent is null.</exception>
    public static bool IsChildOf(this ActorPath path, ActorPath parent)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(parent);

        var pathStr = path.ToString();
        var parentStr = parent.ToString();
        return pathStr.StartsWith(parentStr + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Determines if this path is a descendant of the specified ancestor path.
    /// </summary>
    /// <param name="path">The actor path to check.</param>
    /// <param name="ancestor">The potential ancestor path.</param>
    /// <returns>True if path is a descendant of ancestor; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path or ancestor is null.</exception>
    public static bool IsDescendantOf(this ActorPath path, ActorPath ancestor)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(ancestor);

        return path.IsDescendantOf(ancestor);
    }
}