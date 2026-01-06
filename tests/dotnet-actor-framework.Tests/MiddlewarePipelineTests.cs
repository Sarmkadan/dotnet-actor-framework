// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Middleware;
using DotNetActorFramework.Models;
using FluentAssertions;
using Moq;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Contains unit tests for verifying the behavior and correctness of the <see cref="MiddlewarePipeline"/> class
/// and related middleware functionality in the DotNetActorFramework.
/// </summary>
public class MiddlewarePipelineTests
{
    /// <summary>
    /// Creates an <see cref="ActorRef"/> instance for testing purposes.
    /// </summary>
    /// <param name="pathStr">The actor path string used to construct the actor reference.</param>
    /// <returns>A new <see cref="ActorRef"/> instance with the specified path and a unique identifier.</returns>
    private static ActorRef CreateActorRef(string pathStr)
    {
        var path = new ActorPath(pathStr);
        return new ActorRef(path, Guid.NewGuid());
    }

    /// <summary>
    /// Creates an <see cref="Envelope"/> instance for testing purposes with a default recipient path.
    /// </summary>
    /// <param name="recipientPath">The recipient actor path string. Defaults to "/system/actor".</param>
    /// <returns>A new <see cref="Envelope"/> instance containing a test control message and recipient reference.</returns>
    private static Envelope CreateEnvelope(string recipientPath = "/system/actor")
    {
        var message = new ControlMessage("test-command");
        var recipient = CreateActorRef(recipientPath);
        return new Envelope(message, recipient);
    }

    [Fact]
    public void Register_WithNullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline();

        // Act
        var act = () => pipeline.Register(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("middleware");
    }

    [Fact]
    public void Register_WithMockedMiddleware_MiddlewareAppearsInGetMiddleware()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline();
        var mockMiddleware = new Mock<IActorMiddleware>();
        mockMiddleware.Setup(m => m.Name).Returns("AuditMiddleware");
        mockMiddleware.Setup(m => m.Order).Returns(10);

        // Act
        pipeline.Register(mockMiddleware.Object);

        // Assert
        pipeline.GetMiddleware().Should().ContainSingle()
            .Which.Name.Should().Be("AuditMiddleware");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMiddleware_InvokesFinalHandlerAndReturnsTrue()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline();
        var envelope = CreateEnvelope();
        var finalHandlerInvoked = false;

        // Act
        var result = await pipeline.ExecuteAsync(envelope, e =>
        {
            finalHandlerInvoked = true;
            return Task.CompletedTask;
        });

        // Assert
        result.Should().BeTrue();
        finalHandlerInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenRegisteredMiddlewareThrows_ReturnsFalse()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline();
        var mockMiddleware = new Mock<IActorMiddleware>();
        mockMiddleware.Setup(m => m.Order).Returns(0);
        mockMiddleware
            .Setup(m => m.InvokeAsync(It.IsAny<Envelope>(), It.IsAny<Func<Envelope, Task>>()))
            .ThrowsAsync(new InvalidOperationException("Simulated middleware failure"));
        pipeline.Register(mockMiddleware.Object);

        var envelope = CreateEnvelope();

        // Act
        var result = await pipeline.ExecuteAsync(envelope, _ => Task.CompletedTask);

        // Assert
        result.Should().BeFalse("pipeline must absorb middleware exceptions and signal failure");
        mockMiddleware.Verify(
            m => m.InvokeAsync(It.IsAny<Envelope>(), It.IsAny<Func<Envelope, Task>>()),
            Times.Once);
    }

    [Fact]
    public void ExecuteAsync_WithNullEnvelope_ThrowsArgumentNullException()
    {
        // Arrange
        var pipeline = new MiddlewarePipeline();

        // Act
        var act = async () => await pipeline.ExecuteAsync(null!, _ => Task.CompletedTask);

        // Assert
        act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("envelope");
    }
}

public class MessageTypeTests
{
    [Fact]
    public void ControlMessage_WithEmptyCommand_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new ControlMessage(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Command cannot be null or empty*");
    }

    [Fact]
    public void ControlMessage_WithValidCommand_StoresCommandAndDefaultsParameters()
    {
        // Arrange & Act
        var msg = new ControlMessage("start");

        // Assert
        msg.Command.Should().Be("start");
        msg.Parameters.Should().BeEmpty();
        msg.MessageId.Should().NotBeEmpty();
    }

    [Fact]
    public void FailureMessage_WithValidReasonAndException_StoresReasonAndStackTrace()
    {
        // Arrange – must throw the exception first so it has a populated StackTrace
        Exception exception;
        try { throw new InvalidOperationException("boom"); }
        catch (Exception ex) { exception = ex; }

        // Act
        var failure = new FailureMessage("Actor crashed", exception);

        // Assert
        failure.Reason.Should().Be("Actor crashed");
        failure.StackTrace.Should().NotBeNull("a thrown exception has a non-null StackTrace");
    }
}