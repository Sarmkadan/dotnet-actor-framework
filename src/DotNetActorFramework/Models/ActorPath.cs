// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DotNetActorFramework.Models;

/// <summary>
/// Represents the hierarchical path to an actor in the actor system.
/// Paths are immutable and uniquely identify an actor within a system.
/// </summary>
public class ActorPath : IEquatable<ActorPath>
{
    private static readonly Regex PathValidationRegex = new(@"^(/[a-zA-Z0-9_-]+)+$", RegexOptions.Compiled);

    /// <summary>
    /// Gets the full path string, for example <c>/root/child</c>.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Gets the name of the actor, which is the last segment of the path.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the path of the parent actor, or <c>null</c> if this path has a single segment.
    /// </summary>
    public ActorPath? Parent { get; }

    /// <summary>
    /// Gets the individual path segments, in order from the root to this actor.
    /// </summary>
    public IReadOnlyList<string> Segments { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorPath"/> class from a path string.
    /// </summary>
    /// <param name="path">The absolute actor path, for example <c>/root/child</c>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or has an invalid format.</exception>
    [JsonConstructor]
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

    /// <summary>
    /// Parses a path string into an <see cref="ActorPath"/>.
    /// </summary>
    /// <param name="path">The absolute actor path to parse.</param>
    /// <returns>A new <see cref="ActorPath"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null, empty, or has an invalid format.</exception>
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

    /// <summary>
    /// Returns the full path string.
    /// </summary>
    /// <returns>The path as a string.</returns>
    public override string ToString() => Path;

    /// <summary>
    /// Determines whether the specified object is an <see cref="ActorPath"/> with the same path.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns><c>true</c> if <paramref name="obj"/> is an equal <see cref="ActorPath"/>; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => Equals(obj as ActorPath);

    /// <summary>
    /// Determines whether the specified <see cref="ActorPath"/> has the same path as this instance.
    /// </summary>
    /// <param name="other">The path to compare with this instance.</param>
    /// <returns><c>true</c> if the paths are equal (ordinal comparison); otherwise, <c>false</c>.</returns>
    public bool Equals(ActorPath? other)
    {
        if (other is null) return false;
        return Path.Equals(other.Path, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns a hash code for this path, computed with ordinal comparison.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode() => Path.GetHashCode(StringComparison.Ordinal);

    /// <summary>
    /// Determines whether two <see cref="ActorPath"/> instances have the same path.
    /// </summary>
    /// <param name="left">The first path to compare.</param>
    /// <param name="right">The second path to compare.</param>
    /// <returns><c>true</c> if the paths are equal or both are <c>null</c>; otherwise, <c>false</c>.</returns>
    public static bool operator ==(ActorPath? left, ActorPath? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two <see cref="ActorPath"/> instances have different paths.
    /// </summary>
    /// <param name="left">The first path to compare.</param>
    /// <param name="right">The second path to compare.</param>
    /// <returns><c>true</c> if the paths differ; otherwise, <c>false</c>.</returns>
    public static bool operator !=(ActorPath? left, ActorPath? right) => !(left == right);
}
