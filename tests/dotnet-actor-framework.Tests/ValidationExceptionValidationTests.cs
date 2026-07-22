// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for ValidationExceptionValidation class
// =============================================================================

using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ValidationExceptionValidationTests
{
    [Fact]
    public void Validate_InvalidActorPathException_WithValidData_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new InvalidActorPathException("valid/path", "Valid message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithNullPath_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorPathException(null!, "Valid message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorPathException.InvalidPath cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithEmptyPath_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorPathException(string.Empty, "Valid message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorPathException.InvalidPath cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithWhitespacePath_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorPathException("   ", "Valid message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorPathException.InvalidPath cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithPathTooLong_ShouldReturnValidationProblem()
    {
        // Arrange
        var longPath = new string('a', 1025); // Exceeds 1024 character limit
        var exception = new InvalidActorPathException(longPath, "Valid message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorPathException.InvalidPath exceeds maximum length of 1024 characters.");
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithNullMessage_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new InvalidActorPathException("valid/path", null);

        // Act
        var result = exception.Validate();

        // Assert - InvalidActorPathException constructor sets default message when null
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithEmptyMessage_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorPathException("valid/path", string.Empty);

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorPathException.Message cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidMessageException_WithValidData_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new InvalidMessageException("Valid message content");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidMessageException_WithNullMessage_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new InvalidMessageException(null);

        // Act
        var result = exception.Validate();

        // Assert - InvalidMessageException constructor sets default message, so it's valid
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidMessageException_WithEmptyMessage_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidMessageException(string.Empty);

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidMessageException.Message cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidMessageException_WithMessageTooLong_ShouldReturnValidationProblem()
    {
        // Arrange
        var longMessage = new string('a', 10485761); // Exceeds 10MB limit
        var exception = new InvalidMessageException(longMessage);

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidMessageException.Message exceeds maximum length of 10485760 characters (10MB).");
    }

    [Fact]
    public void Validate_InvalidActorReferenceException_WithValidData_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new InvalidActorReferenceException("Valid reference message");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidActorReferenceException_WithNullMessage_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new InvalidActorReferenceException(null);

        // Act
        var result = exception.Validate();

        // Assert - InvalidActorReferenceException constructor sets default message, so it's valid
        result.Should().BeEmpty();
    }

    [Fact]
    public void Validate_InvalidActorReferenceException_WithEmptyMessage_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorReferenceException(string.Empty);

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorReferenceException.Message cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_GenericValidationException_ShouldReturnEmptyList()
    {
        // Arrange
        var exception = new ValidationException("Some validation error");

        // Act
        var result = exception.Validate();

        // Assert - generic ValidationException has no specific validation rules
        result.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_WithValidException_ShouldReturnTrue()
    {
        // Arrange
        var exception = new InvalidActorPathException("valid/path", "Valid message");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithInvalidException_ShouldReturnFalse()
    {
        // Arrange
        var exception = new InvalidActorPathException(null!, "Valid message");

        // Act
        var result = exception.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_WithValidException_ShouldNotThrow()
    {
        // Arrange
        var exception = new InvalidActorPathException("valid/path", "Valid message");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_WithInvalidException_ShouldThrowArgumentException()
    {
        // Arrange
        var exception = new InvalidActorPathException(null!, "Valid message");

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ValidationException is not valid. Problems:*InvalidActorPathException.InvalidPath cannot be null, empty, or whitespace.*");
    }

    [Fact]
    public void EnsureValid_WithInvalidException_ShouldIncludeAllProblemsInExceptionMessage()
    {
        // Arrange
        var exception = new InvalidActorPathException(null!, null!);

        // Act
        Action act = () => exception.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("ValidationException is not valid. Problems:*InvalidActorPathException.InvalidPath cannot be null, empty, or whitespace. (Parameter 'value')*");
    }

    [Fact]
    public void Validate_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ValidationException? exception = null;

        // Act
        Action act = () => exception!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsValid_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ValidationException? exception = null;

        // Act
        Action act = () => exception!.IsValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureValid_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Arrange
        ValidationException? exception = null;

        // Act
        Action act = () => exception!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Validate_InvalidActorPathException_WithWhitespaceMessage_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorPathException("valid/path", "   ");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorPathException.Message cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidMessageException_WithWhitespaceMessage_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidMessageException("   ");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidMessageException.Message cannot be null, empty, or whitespace.");
    }

    [Fact]
    public void Validate_InvalidActorReferenceException_WithWhitespaceMessage_ShouldReturnValidationProblem()
    {
        // Arrange
        var exception = new InvalidActorReferenceException("   ");

        // Act
        var result = exception.Validate();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().Be("InvalidActorReferenceException.Message cannot be null, empty, or whitespace.");
    }
}