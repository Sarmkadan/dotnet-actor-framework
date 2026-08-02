using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

    /// <summary>
    /// Tests for the ValidationExceptionExtensions extension methods.
    /// </summary>
public class ValidationExceptionExtensionsTests
{
    /// <summary>
    /// Tests that WithContext returns an exception with a formatted message when context is provided.
    /// </summary>
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

    /// <summary>
    /// Tests that WithContext does not add context when null is provided.
    /// </summary>
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

    /// <summary>
    /// Tests that WithExpectedFormat appends the expected format to the message.
    /// </summary>
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

    /// <summary>
    /// Tests that WithExpectedFormat does not append anything when format is null.
    /// </summary>
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

    /// <summary>
    /// Tests that WithActorType prepends the actor type to the message.
    /// </summary>
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

    /// <summary>
    /// Tests that WithActorType does not prepend anything when actor type is null.
    /// </summary>
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

    /// <summary>
    /// Tests that CombineWith combines the original error with additional errors, each on a new line with a dash.
    /// </summary>
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

    /// <summary>
    /// Tests that CombineWith with no additional errors returns the original exception unchanged.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidationType returns true when the exception type matches the provided type.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidationType returns false when the exception type does not match.
    /// </summary>
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
