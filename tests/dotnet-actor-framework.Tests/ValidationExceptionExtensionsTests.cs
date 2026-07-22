using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ValidationExceptionExtensionsTests
{
    [Fact]
    public void WithContext_ShouldReturnExceptionWithFormattedMessage()
    {
        // Arrange
        var exception = new InvalidActorPathException("test/path");

        // Act
        var result = exception.WithContext("Some context");

        // Assert
        result.Message.Should().Contain("Invalid actor path: test/path. Context: Some context");
        result.InvalidPath.Should().Be("test/path");
    }

    [Fact]
    public void WithContext_WhenContextIsNull_ShouldReturnExceptionWithDefaultMessage()
    {
        // Arrange
        var exception = new InvalidActorPathException("test/path");

        // Act
        var result = exception.WithContext(null);

        // Assert
        result.Message.Should().Be("Invalid actor path: test/path");
        result.InvalidPath.Should().Be("test/path");
    }

    [Fact]
    public void WithExpectedFormat_ShouldReturnExceptionWithFormattedMessage()
    {
        // Arrange
        var exception = new InvalidMessageException("Invalid message");

        // Act
        var result = exception.WithExpectedFormat("Expected format");

        // Assert
        result.Message.Should().Be("Message validation failed: Invalid message. Expected format: Expected format");
    }

    [Fact]
    public void WithExpectedFormat_WhenFormatIsNull_ShouldReturnExceptionWithDefaultMessage()
    {
        // Arrange
        var exception = new InvalidMessageException("Invalid message");

        // Act
        var result = exception.WithExpectedFormat(null);

        // Assert
        result.Message.Should().Be("Message validation failed: Invalid message");
    }

    [Fact]
    public void WithActorType_ShouldReturnExceptionWithFormattedMessage()
    {
        // Arrange
        var exception = new InvalidActorReferenceException("Original message");

        // Act
        var result = exception.WithActorType("MyActorType");

        // Assert
        result.Message.Should().Be("Actor reference is invalid for MyActorType: Original message");
    }

    [Fact]
    public void WithActorType_WhenActorTypeIsNull_ShouldReturnExceptionWithDefaultMessage()
    {
        // Arrange
        var exception = new InvalidActorReferenceException("Original message");

        // Act
        var result = exception.WithActorType(null);

        // Assert
        result.Message.Should().Be("Actor reference is invalid: Original message");
    }

    [Fact]
    public void CombineWith_ShouldReturnCombinedMessage()
    {
        // Arrange
        var exception = new ValidationException("Original error");

        // Act
        var result = exception.CombineWith("Error 1", "Error 2");

        // Assert
        result.Message.Should().Contain("Original error");
        result.Message.Should().Contain("- Error 1");
        result.Message.Should().Contain("- Error 2");
        result.InnerException.Should().Be(exception);
    }

    [Fact]
    public void CombineWith_WhenErrorsAreEmpty_ShouldReturnSameMessage()
    {
        // Arrange
        var exception = new ValidationException("Original error");

        // Act
        var result = exception.CombineWith();

        // Assert
        result.Message.Should().Be("Original error");
        result.InnerException.Should().Be(exception);
    }

    [Fact]
    public void IsValidationType_ShouldReturnTrueIfTypeMatches()
    {
        // Arrange
        var exception = new InvalidActorPathException("test/path");

        // Act
        var result = exception.IsValidationType(typeof(InvalidActorPathException));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidationType_ShouldReturnFalseIfTypeDoesNotMatch()
    {
        // Arrange
        var exception = new InvalidMessageException("Message");

        // Act
        var result = exception.IsValidationType(typeof(InvalidActorPathException));

        // Assert
        result.Should().BeFalse();
    }
}
