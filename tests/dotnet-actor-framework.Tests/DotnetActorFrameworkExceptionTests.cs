using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class DotnetActorFrameworkExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldCreateExceptionWithMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new DotnetActorFrameworkException(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithNullMessage_ShouldCreateExceptionWithNullMessage()
    {
        // Arrange & Act
        var exception = new DotnetActorFrameworkException(null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Exception of type 'DotNetActorFramework.Exceptions.DotnetActorFrameworkException' was thrown.");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldCreateExceptionWithEmptyMessage()
    {
        // Arrange & Act
        var exception = new DotnetActorFrameworkException(string.Empty);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().BeEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldCreateExceptionWithBoth()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DotnetActorFrameworkException(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithNullMessageAndInnerException_ShouldCreateExceptionWithNullMessage()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new DotnetActorFrameworkException(null, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Exception of type 'DotNetActorFramework.Exceptions.DotnetActorFrameworkException' was thrown.");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Constructor_WithMessageAndNullInnerException_ShouldCreateExceptionWithMessage()
    {
        // Arrange
        var message = "Test error message";

        // Act
        var exception = new DotnetActorFrameworkException(message, null);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Create_WithFormatAndArgs_ShouldCreateExceptionWithFormattedMessage()
    {
        // Arrange
        var arg1 = "first";
        var arg2 = 42;
        var format = "Error occurred with {0} and {1}";

        // Act
        var exception = DotnetActorFrameworkException.Create(format, arg1, arg2);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Error occurred with first and 42");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Create_WithFormatAndNoArgs_ShouldCreateExceptionWithPlainMessage()
    {
        // Arrange
        var message = "Simple error message";

        // Act
        var exception = DotnetActorFrameworkException.Create(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullFormat_ShouldThrowArgumentNullException()
    {
        // Arrange & Act
        Action act = () => DotnetActorFrameworkException.Create(null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithEmptyFormat_ShouldCreateExceptionWithEmptyMessage()
    {
        // Arrange & Act
        var exception = DotnetActorFrameworkException.Create(string.Empty);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().BeEmpty();
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Create_WithFormatAndArgs_ShouldHandleComplexFormatting()
    {
        // Arrange
        var user = "admin";
        var action = "delete";
        var resource = "user123";
        var format = "User {0} attempted to {1} resource {2} without permission";

        // Act
        var exception = DotnetActorFrameworkException.Create(format, user, action, resource);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("User admin attempted to delete resource user123 without permission");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Create_WithFormatAndNullArgs_ShouldThrowArgumentNullException()
    {
        // Arrange
        var format = "Error with null arg: {0}";

        // Act
        Action act = () => DotnetActorFrameworkException.Create(format, null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithFormatAndMixedArgs_ShouldHandleMixedArguments()
    {
        // Arrange
        var user = "testuser";
        var count = 5;
        var active = true;
        var format = "User {0} has {1} active sessions (enabled: {2})";

        // Act
        var exception = DotnetActorFrameworkException.Create(format, user, count, active);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("User testuser has 5 active sessions (enabled: True)");
        exception.InnerException.Should().BeNull();
    }

    [Fact]
    public void Create_WithInnerExceptionAndFormat_ShouldCreateExceptionWithInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var format = "Wrapper error: {0}";
        var arg = "operation failed";

        // Act
        var exception = DotnetActorFrameworkException.Create(innerException, format, arg);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Wrapper error: operation failed");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Create_WithInnerExceptionAndNullFormat_ShouldThrowArgumentNullException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        Action act = () => DotnetActorFrameworkException.Create(innerException, null);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_WithInnerExceptionAndEmptyFormat_ShouldCreateExceptionWithEmptyMessage()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = DotnetActorFrameworkException.Create(innerException, string.Empty);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().BeEmpty();
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Create_WithInnerExceptionAndComplexFormat_ShouldHandleComplexFormatting()
    {
        // Arrange
        var innerException = new ArgumentNullException("param1");
        var format = "Failed to process request for user {0} on endpoint {1}";
        var user = "john_doe";
        var endpoint = "/api/users";

        // Act
        var exception = DotnetActorFrameworkException.Create(innerException, format, user, endpoint);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Failed to process request for user john_doe on endpoint /api/users");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    [Fact]
    public void Inheritance_ShouldBeException()
    {
        // Arrange & Act
        var exception = new DotnetActorFrameworkException("Test");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void Message_ShouldBeAccessible()
    {
        // Arrange
        var message = "Access denied";
        var exception = new DotnetActorFrameworkException(message);

        // Act & Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void InnerException_ShouldBeAccessible()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner");
        var exception = new DotnetActorFrameworkException("Outer", inner);

        // Act & Assert
        exception.InnerException.Should().BeSameAs(inner);
    }
}
