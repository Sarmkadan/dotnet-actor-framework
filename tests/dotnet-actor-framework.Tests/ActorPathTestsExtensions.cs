// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Provides extension methods for testing assertions and utilities related to <see cref="ActorPath"/> instances.
/// </summary>
public static class ActorPathTestsExtensions
{
    /// <summary>
    /// Creates a deep hierarchy of actor paths for testing purposes.
    /// </summary>
    /// <param name="rootPath">The root path to start from.</param>
    /// <param name="segments">The segments to append to create the hierarchy.</param>
    /// <returns>An <see cref="ActorPath"/> representing the full hierarchy.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rootPath"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="rootPath"/> is empty or contains only whitespace.
    /// -or- <paramref name="segments"/> contains a null or empty string.</exception>
    public static ActorPath CreateDeepHierarchy(this string rootPath, params string[] segments)
    {
        ArgumentException.ThrowIfNullOrEmpty(rootPath);

        if (segments is not { Length: > 0 })
        {
            return new ActorPath(rootPath);
        }

        var currentPath = rootPath;
        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                throw new ArgumentException("Segment cannot be null or empty", nameof(segments));
            }

            currentPath = $"{currentPath}/{segment}";
        }

        return new ActorPath(currentPath);
    }

    /// <summary>
    /// Asserts that a path has the expected segments.
    /// </summary>
    /// <param name="path">The actor path to test.</param>
    /// <param name="expectedSegments">The expected segment values.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    public static void ShouldHaveSegments(this ActorPath path, params string[] expectedSegments)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(expectedSegments);

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
    /// <param name="childPath">The child path to test.</param>
    /// <param name="parentPath">The expected parent path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="childPath"/> or <paramref name="parentPath"/> is <see langword="null"/>.</exception>
    public static void ShouldBeDirectChildOf(this ActorPath childPath, ActorPath parentPath)
    {
        ArgumentNullException.ThrowIfNull(childPath);
        ArgumentNullException.ThrowIfNull(parentPath);

        childPath.IsChildOf(parentPath).Should().BeTrue("Should be a child of the parent path");
        childPath.Path.Should().StartWith($"{parentPath.Path}/", "Child path should start with parent path");

        // Verify it's a direct child by checking depth difference
        childPath.GetDepth().Should().Be(parentPath.GetDepth() + 1);
    }

    /// <summary>
    /// Asserts that a path has the expected parent path.
    /// </summary>
    /// <param name="path">The actor path to test.</param>
    /// <param name="expectedParentPath">The expected parent path string.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> has no parent.</exception>
    public static void ShouldHaveParent(this ActorPath path, string expectedParentPath)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrEmpty(expectedParentPath);

        path.Should().NotBeNull();
        path.Parent.Should().NotBeNull("Path should have a parent");
        path.Parent!.Path.Should().Be(expectedParentPath);
    }

    /// <summary>
    /// Creates a sibling path by replacing the last segment of the current path.
    /// </summary>
    /// <param name="path">The original path.</param>
    /// <param name="newSegment">The new segment name for the sibling.</param>
    /// <returns>A new <see cref="ActorPath"/> representing the sibling.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="path"/> has no segments or <paramref name="newSegment"/> is null or empty.</exception>
    public static ActorPath CreateSibling(this ActorPath path, string newSegment)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentException.ThrowIfNullOrEmpty(newSegment);

        path.Segments.Should().NotBeEmpty();

        var parentPath = path.Parent?.Path ?? "/";
            return new ActorPath(string.Concat(parentPath, "/", newSegment));
    }

    /// <summary>
    /// Gets the relative path from one actor path to another.
    /// </summary>
    /// <param name="fromPath">The starting path.</param>
    /// <param name="toPath">The target path.</param>
    /// <returns>The relative path segments, or null if paths are unrelated.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="fromPath"/> or <paramref name="toPath"/> is <see langword="null"/>.</exception>
    public static string[]? GetRelativePath(this ActorPath fromPath, ActorPath toPath)
    {
        ArgumentNullException.ThrowIfNull(fromPath);
        ArgumentNullException.ThrowIfNull(toPath);

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
    /// <param name="path1">First path.</param>
    /// <param name="path2">Second path.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path1"/> or <paramref name="path2"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Either path has no parent.</exception>
    public static void ShouldBeSiblings(this ActorPath path1, ActorPath path2)
    {
        ArgumentNullException.ThrowIfNull(path1);
        ArgumentNullException.ThrowIfNull(path2);

        path1.Parent.Should().NotBeNull("Both paths should have parents");
        path2.Parent.Should().NotBeNull();

        path1.Parent!.Path.Should().Be(path2.Parent!.Path, "Sibling paths should have the same parent");
    }
}