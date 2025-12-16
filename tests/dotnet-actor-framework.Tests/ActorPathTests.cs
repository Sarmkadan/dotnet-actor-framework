// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ActorPathTests
{
    [Fact]
    public void Constructor_WithValidNestedPath_SetsNameSegmentsAndDepth()
    {
        // Arrange & Act
        var path = new ActorPath("/system/workers/processor");

        // Assert
        path.Name.Should().Be("processor");
        path.Segments.Should().HaveCount(3);
        path.GetDepth().Should().Be(3);
        path.Parent!.Path.Should().Be("/system/workers");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithNullOrWhitespacePath_ThrowsArgumentException(string input)
    {
        // Arrange & Act
        var act = () => new ActorPath(input);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("no-leading-slash")]
    [InlineData("/invalid path with spaces")]
    [InlineData("/path//double-slash")]
    public void Constructor_WithInvalidPathFormat_ThrowsWithDescriptiveMessage(string input)
    {
        // Arrange & Act
        var act = () => new ActorPath(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Invalid actor path*");
    }

    [Fact]
    public void GetChild_WithValidChildName_BuildsCorrectHierarchy()
    {
        // Arrange
        var parent = new ActorPath("/system/workers");

        // Act
        var child = parent.GetChild("processor");

        // Assert
        child.Path.Should().Be("/system/workers/processor");
        child.Name.Should().Be("processor");
        child.GetDepth().Should().Be(3);
        child.IsDescendantOf(parent).Should().BeTrue();
    }

    [Fact]
    public void IsDescendantOf_WhenPathNested_ReturnsTrue_AndSiblingReturnsFalse()
    {
        // Arrange
        var root = new ActorPath("/system");
        var nested = new ActorPath("/system/workers/processor");
        var sibling = new ActorPath("/monitoring");

        // Act & Assert
        nested.IsDescendantOf(root).Should().BeTrue();
        sibling.IsDescendantOf(root).Should().BeFalse();
        root.IsDescendantOf(nested).Should().BeFalse();
    }

    [Fact]
    public void Equality_WithIdenticalPathStrings_PathsAreEqual()
    {
        // Arrange
        var path1 = new ActorPath("/system/workers");
        var path2 = new ActorPath("/system/workers");
        var different = new ActorPath("/system/monitors");

        // Assert
        path1.Should().Be(path2);
        (path1 == path2).Should().BeTrue();
        (path1 == different).Should().BeFalse();
        path1.GetHashCode().Should().Be(path2.GetHashCode());
    }

    [Fact]
    public void IsChildOf_ExtensionMethod_ReturnsTrueForAnyDescendant()
    {
        // Arrange – IsChildOf uses StartsWith semantics, so any descendant qualifies
        var parent = new ActorPath("/system");
        var directChild = new ActorPath("/system/workers");
        var deepDescendant = new ActorPath("/system/workers/processor");
        var unrelated = new ActorPath("/monitoring");

        // Act & Assert
        directChild.IsChildOf(parent).Should().BeTrue();
        deepDescendant.IsChildOf(parent).Should().BeTrue("IsChildOf checks ancestry, not just direct parent");
        parent.IsChildOf(directChild).Should().BeFalse("parent is not a child of its child");
        unrelated.IsChildOf(parent).Should().BeFalse();
    }
}
