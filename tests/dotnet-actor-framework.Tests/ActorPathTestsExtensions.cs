// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public static class ActorPathTestsExtensions
{
    /// <summary>
    /// Creates a deep hierarchy of actor paths for testing purposes.
    /// </summary>
    /// <param name="rootPath">The root path to start from</param>
    /// <param name="segments">The segments to append to create the hierarchy</param>
    /// <returns>An ActorPath representing the full hierarchy</returns>
    public static ActorPath CreateDeepHierarchy(this string rootPath, params string[] segments)
    {
        if (string.IsNullOrEmpty(rootPath))
        {
            throw new ArgumentException("Root path cannot be null or empty", nameof(rootPath));
        }

        if (segments == null || segments.Length == 0)
        {
            return new ActorPath(rootPath);
        }

        var currentPath = rootPath;
        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
            {
                throw new ArgumentException("Segment cannot be null or empty");
            }
            currentPath = $"{currentPath}/{segment}";
        }

        return new ActorPath(currentPath);
    }

    /// <summary>
    /// Asserts that a path has the expected segments.
    /// </summary>
    /// <param name="path">The actor path to test</param>
    /// <param name="expectedSegments">The expected segment values</param>
    public static void ShouldHaveSegments(this ActorPath path, params string[] expectedSegments)
    {
        path.Should().NotBeNull();
        path.Segments.Should().HaveCount(expectedSegments.Length);

        for (var i = 0; i < expectedSegments.Length; i++)
        {
            path.Segments[i].Should().Be(expectedSegments[i]);
        }
    }

    /// <summary>
    /// Asserts that a path is a direct child (not just any descendant) of another path.
    /// </summary>
    /// <param name="childPath">The child path to test</param>
    /// <param name="parentPath">The expected parent path</param>
    public static void ShouldBeDirectChildOf(this ActorPath childPath, ActorPath parentPath)
    {
        childPath.Should().NotBeNull();
        parentPath.Should().NotBeNull();

        childPath.IsChildOf(parentPath).Should().BeTrue("Should be a child of the parent path");
        childPath.Path.Should().StartWith($"{parentPath.Path}/");

        // Verify it's a direct child by checking depth difference
        childPath.GetDepth().Should().Be(parentPath.GetDepth() + 1);
    }

    /// <summary>
    /// Asserts that a path has the expected parent path.
    /// </summary>
    /// <param name="path">The actor path to test</param>
    /// <param name="expectedParentPath">The expected parent path string</param>
    public static void ShouldHaveParent(this ActorPath path, string expectedParentPath)
    {
        path.Should().NotBeNull();
        path.Parent.Should().NotBeNull("Path should have a parent");
        path.Parent.Path.Should().Be(expectedParentPath);
    }

    /// <summary>
    /// Creates a sibling path by replacing the last segment of the current path.
    /// </summary>
    /// <param name="path">The original path</param>
    /// <param name="newSegment">The new segment name for the sibling</param>
    /// <returns>A new ActorPath representing the sibling</returns>
    public static ActorPath CreateSibling(this ActorPath path, string newSegment)
    {
        path.Should().NotBeNull();
        path.Segments.Should().NotBeEmpty();

        var parentPath = path.Parent?.Path ?? "/";
        if (string.IsNullOrEmpty(newSegment))
        {
            throw new ArgumentException("New segment cannot be null or empty", nameof(newSegment));
        }
        return new ActorPath($"{parentPath}/{newSegment}");
    }

    /// <summary>
    /// Gets the relative path from one actor path to another.
    /// </summary>
    /// <param name="fromPath">The starting path</param>
    /// <param name="toPath">The target path</param>
    /// <returns>The relative path segments, or null if paths are unrelated</returns>
    public static string[]? GetRelativePath(this ActorPath fromPath, ActorPath toPath)
    {
        fromPath.Should().NotBeNull();
        toPath.Should().NotBeNull();

        if (!fromPath.IsDescendantOf(toPath) && !toPath.IsDescendantOf(fromPath))
        {
            return null; // Unrelated paths
        }

        var fromSegments = fromPath.Segments;
        var toSegments = toPath.Segments;

        // Find common ancestor
        var commonLength = 0;
        while (commonLength < fromSegments.Count &&
               commonLength < toSegments.Count &&
               fromSegments[commonLength] == toSegments[commonLength])
        {
            commonLength++;
        }

        // Calculate relative path
        var relativeSegments = new string[toSegments.Count - commonLength];
        for (var i = 0; i < relativeSegments.Length; i++)
        {
            relativeSegments[i] = toSegments[commonLength + i];
        }

        return relativeSegments;
    }

    /// <summary>
    /// Asserts that two paths are siblings (share the same parent).
    /// </summary>
    /// <param name="path1">First path</param>
    /// <param name="path2">Second path</param>
    public static void ShouldBeSiblings(this ActorPath path1, ActorPath path2)
    {
        path1.Should().NotBeNull();
        path2.Should().NotBeNull();

        path1.Parent.Should().NotBeNull("Both paths should have parents");
        path2.Parent.Should().NotBeNull();

        path1.Parent.Path.Should().Be(path2.Parent.Path, "Sibling paths should have the same parent");
    }
}