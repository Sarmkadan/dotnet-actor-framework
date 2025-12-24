// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;

namespace DotNetActorFramework.Models;

/// <summary>
/// Represents the hierarchical path to an actor in the actor system.
/// Paths are immutable and uniquely identify an actor within a system.
/// </summary>
public class ActorPath : IEquatable<ActorPath>
{
    private static readonly Regex PathValidationRegex = new(@"^(/[a-zA-Z0-9_-]+)+$", RegexOptions.Compiled);

    public string Path { get; }
    public string Name { get; }
    public ActorPath? Parent { get; }
    public IReadOnlyList<string> Segments { get; }

    public ActorPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        if (!PathValidationRegex.IsMatch(path))
            throw new ArgumentException($"Invalid actor path format: {path}", nameof(path));

        Path = path;
        Segments = ParseSegments(path);
        Name = Segments.Last();
        Parent = Segments.Count > 1 ? new ActorPath("/" + string.Join("/", Segments.Take(Segments.Count - 1))) : null;
    }

    public static ActorPath Parse(string path) => new ActorPath(path);

    /// <summary>
    /// Creates a child path from this path.
    /// </summary>
    public ActorPath GetChild(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
            throw new ArgumentException("Child name cannot be null or empty.", nameof(childName));

        var childPath = Path.EndsWith('/') ? $"{Path}{childName}" : $"{Path}/{childName}";
        return new ActorPath(childPath);
    }

    /// <summary>
    /// Checks if this path is a descendant of another path.
    /// </summary>
    public bool IsDescendantOf(ActorPath other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        return Path.StartsWith(other.Path, StringComparison.Ordinal) &&
               Segments.Count > other.Segments.Count;
    }

    /// <summary>
    /// Gets the depth of this path in the actor hierarchy.
    /// </summary>
    public int GetDepth() => Segments.Count;

    private static IReadOnlyList<string> ParseSegments(string path)
    {
        return path
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .ToList()
            .AsReadOnly();
    }

    public override string ToString() => Path;

    public override bool Equals(object? obj) => Equals(obj as ActorPath);

    public bool Equals(ActorPath? other)
    {
        if (other is null) return false;
        return Path.Equals(other.Path, StringComparison.Ordinal);
    }

    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);

    public static bool operator ==(ActorPath? left, ActorPath? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ActorPath? left, ActorPath? right) => !(left == right);
}
