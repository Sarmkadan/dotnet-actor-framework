namespace DotNetActorFramework.Tests;

using System;
using DotNetActorFramework.Exceptions;
using Xunit;

public class ActorExceptionTests
{
    [Fact]
    public void Constructor_Message_SetMessage()
    {
        // Arrange and Act
        var exception = new ActorException("Test message");

        // Assert
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void Constructor_MessageAndInnerException_SetMessageAndInnerException()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new ActorException("Test message", innerException);

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Create_Message_FormattedMessage()
    {
        // Act
        var exception = ActorException.Create("Test {0}", "message");

        // Assert
        Assert.Equal("Test message", exception.Message);
    }

    [Fact]
    public void Create_MessageAndInnerException_FormattedMessageAndInnerException()
    {
        // Arrange
        var innerException = new Exception("Inner exception");

        // Act
        var exception = ActorException.Create(innerException, "Test {0}", "message");

        // Assert
        Assert.Equal("Test message", exception.Message);
        Assert.Equal(innerException, exception.InnerException);
    }

    [Fact]
    public void Constructor_NullMessage_DefaultMessage()
    {
        // Act
        var exception = new ActorException(null);

        // Assert
        Assert.NotNull(exception.Message);
    }
}
