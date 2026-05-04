// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Extension methods for ActorPath manipulation and querying.
/// These utilities simplify common path operations like validation, parsing, and hierarchy traversal.
/// </summary>
public static class ActorPathExtensions
{
    /// <summary>
    /// Gets the parent path of the current actor path.
    /// Returns null if the current path is already at the root level.
    /// </summary>
    public static ActorPath? GetParent(this ActorPath path)
    {
        if (path == null) return null;
        var pathStr = path.ToString();
        var lastSlashIndex = pathStr.LastIndexOf('/');
        if (lastSlashIndex <= 0) return null;
        var parentPathStr = pathStr[..lastSlashIndex];
        return new ActorPath(parentPathStr);
    }

    /// <summary>
    /// Gets the name component of the actor path (last segment).
    /// </summary>
    public static string GetName(this ActorPath path)
    {
        if (path == null) return string.Empty;
        var pathStr = path.ToString();
        var lastSlashIndex = pathStr.LastIndexOf('/');
        return lastSlashIndex >= 0 ? pathStr[(lastSlashIndex + 1)..] : pathStr;
    }

    /// <summary>
    /// Gets the depth of the path in the hierarchy.
    /// Root path has depth 0, children of root have depth 1, etc.
    /// </summary>
    public static int GetDepth(this ActorPath path)
    {
        if (path == null) return 0;
        return path.ToString().Count(c => c == '/');
    }

    /// <summary>
    /// Determines if this path is a child of the specified parent path.
    /// </summary>
    public static bool IsChildOf(this ActorPath path, ActorPath parent)
    {
        if (path == null || parent == null) return false;
        var pathStr = path.ToString();
        var parentStr = parent.ToString();
        return pathStr.StartsWith(parentStr + "/", StringComparison.Ordinal);
    }

    /// <summary>
    /// Gets the relative path from a parent actor to this actor.
    /// Useful for understanding hierarchical relationships.
    /// </summary>
    public static string? GetRelativePath(this ActorPath path, ActorPath parent)
    {
        if (path == null || parent == null) return null;
        var pathStr = path.ToString();
        var parentStr = parent.ToString();
        if (!pathStr.StartsWith(parentStr + "/", StringComparison.Ordinal))
            return null;
        return pathStr[(parentStr.Length + 1)..];
    }

    /// <summary>
    /// Validates that the path contains only alphanumeric characters, hyphens, underscores, and forward slashes.
    /// </summary>
    public static bool IsValidPath(this ActorPath path)
    {
        if (path == null) return false;
        var pathStr = path.ToString();
        return System.Text.RegularExpressions.Regex.IsMatch(
            pathStr,
            @"^[a-zA-Z0-9/_-]+$",
            System.Text.RegularExpressions.RegexOptions.Compiled);
    }

    /// <summary>
    /// Builds a child path by appending a name segment to this path.
    /// </summary>
    public static ActorPath CreateChild(this ActorPath path, string childName)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (string.IsNullOrWhiteSpace(childName)) throw new ArgumentException("Child name cannot be empty.", nameof(childName));
        var combined = $"{path}/{childName}";
        return new ActorPath(combined);
    }

    /// <summary>
    /// Gets all ancestor paths from this path to the root.
    /// Useful for traversing the supervision hierarchy.
    /// </summary>
    public static IEnumerable<ActorPath> GetAncestors(this ActorPath path)
    {
        var current = path;
        while (current != null)
        {
            yield return current;
            current = current.GetParent();
        }
    }
}
