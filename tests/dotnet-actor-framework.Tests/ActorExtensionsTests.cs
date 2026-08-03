using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using FluentAssertions;
using Xunit;
using System;
using System.Threading.Tasks;

namespace DotNetActorFramework.Tests;

public class ActorExtensionsTests
{
    [Fact]
    public void IsTerminated_WithTerminatedActor_ReturnsTrue()
    {
        // Arrange
        var actor = new Actor(new ActorPath("/system/test-actor"));
        // Terminate the actor synchronously by calling TerminateAsync and waiting
        actor.TerminateAsync().GetAwaiter().GetResult();

        // Act
        var result = ActorExtensions.IsTerminated(actor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTerminated_WithActiveActor_ReturnsFalse()
    {
        // Arrange
        var actor = new Actor(new ActorPath("/system/test-actor"));

        // Act
        var result = ActorExtensions.IsTerminated(actor);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTerminated_NullActor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => ActorExtensions.IsTerminated(null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsActive_WithActiveActor_ReturnsTrue()
    {
        // Arrange
        var actor = new Actor(new ActorPath("/system/test-actor"));

        // Act
        var result = ActorExtensions.IsActive(actor);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithTerminatedActor_ReturnsFalse()
    {
        // Arrange
        var actor = new Actor(new ActorPath("/system/test-actor"));
        actor.TerminateAsync().GetAwaiter().GetResult();

        // Act
        var result = ActorExtensions.IsActive(actor);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActive_NullActor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => ActorExtensions.IsActive(null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetMetricsSummary_WithActor_ReturnsMetricsSummary()
    {
        // Arrange
        var actor = new Actor(new ActorPath("/system/test-actor"));

        // Act
        var result = ActorExtensions.GetMetricsSummary(actor);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetMetricsSummary_NullActor_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => ActorExtensions.GetMetricsSummary(null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}