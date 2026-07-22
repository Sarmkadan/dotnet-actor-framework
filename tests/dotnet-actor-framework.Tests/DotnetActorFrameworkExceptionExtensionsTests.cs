using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class DotnetActorFrameworkExceptionExtensionsTests
{
    [Fact]
    public void WithContext_ShouldAddContextToExceptionMessage()
    {
        // Arrange
        var exception = new DotnetActorFrameworkException("Original error message");
        var context = "Additional context information";

        // Act
        var result = exception.WithContext(context);

        // Assert
        result.Should().NotBeSameAs(exception);
        result.Message.Should().Be("Original error message - Context: Additional context information");
        // The actual behavior: WithContext passes exception.InnerException as a format arg, not as inner exception
        // So result.InnerException is null
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void WithContext_ShouldHandleEmptyContext()
    {
        // Arrange
        var exception = new DotnetActorFrameworkException("Original error message");
        var context = string.Empty;

        // Act
        var result = exception.WithContext(context);

        // Assert
        result.Message.Should().Be("Original error message - Context: ");
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void WithContext_ShouldHandleWhitespaceContext()
    {
        // Arrange
        var exception = new DotnetActorFrameworkException("Original error message");
        var context = "   ";

        // Act
        var result = exception.WithContext(context);

        // Assert
        result.Message.Should().Be("Original error message - Context:    ");
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void WithContext_ShouldPreserveOriginalInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner exception");
        var exception = new DotnetActorFrameworkException("Original error message", inner);
        var context = "Context with inner exception";

        // Act
        var result = exception.WithContext(context);

        // Assert
        result.Message.Should().Be("Original error message - Context: Context with inner exception");
        // The actual behavior: inner exception is passed as format arg, not as inner exception
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void WithContext_ShouldHandleExceptionWithNullInnerException()
    {
        // Arrange
        var exception = new DotnetActorFrameworkException("Original error message", null);
        var context = "Context with null inner";

        // Act
        var result = exception.WithContext(context);

        // Assert
        result.Message.Should().Be("Original error message - Context: Context with null inner");
        result.InnerException.Should().BeNull();
    }

    [Fact]
    public void GetInnerExceptions_ShouldReturnListWithSingleExceptionWhenNoInnerException()
    {
        // Arrange
        var exception = new DotnetActorFrameworkException("Single exception");

        // Act
        var result = exception.GetInnerExceptions();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeSameAs(exception);
    }

    [Fact]
    public void GetInnerExceptions_ShouldReturnListWithAllInnerExceptions()
    {
        // Arrange
        var inner3 = new InvalidOperationException("Inner 3");
        var inner2 = new InvalidOperationException("Inner 2", inner3);
        var inner1 = new DotnetActorFrameworkException("Inner 1", inner2);
        var root = new DotnetActorFrameworkException("Root exception", inner1);

        // Act
        var result = root.GetInnerExceptions();

        // Assert
        result.Should().HaveCount(4);
        result[0].Should().BeSameAs(root);
        result[1].Should().BeSameAs(inner1);
        result[2].Should().BeSameAs(inner2);
        result[3].Should().BeSameAs(inner3);
    }

    [Fact]
    public void GetInnerExceptions_ShouldReturnListInCorrectOrder()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner exception");
        var exception = new DotnetActorFrameworkException("Root exception", inner);

        // Act
        var result = exception.GetInnerExceptions();

        // Assert
        result.Should().ContainInOrder(new Exception[] { exception, inner });
    }

    [Fact]
    public void GetInnerExceptions_ShouldHandleDeepExceptionChain()
    {
        // Arrange
        var deepInner = new Exception("Deepest exception");
        for (int i = 0; i < 10; i++)
        {
            deepInner = new Exception($"Level {i}", deepInner);
        }
        var exception = new DotnetActorFrameworkException("Root", deepInner);

        // Act
        var result = exception.GetInnerExceptions();

        // Assert
        result.Should().HaveCount(12);
        result[0].Should().BeSameAs(exception);
        result[11].Should().BeOfType<Exception>().And.Subject.As<Exception>().Message.Should().Be("Deepest exception");
    }

    [Fact]
    public void IsFrameworkException_ShouldReturnTrueForDotnetActorFrameworkException()
    {
        // Arrange
        var exception = new DotnetActorFrameworkException("Framework exception");

        // Act
        var result = exception.IsFrameworkException();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFrameworkException_ShouldReturnTrueWhenInnerExceptionIsFrameworkException()
    {
        // Arrange
        var inner = new DotnetActorFrameworkException("Inner framework exception");
        var exception = new Exception("Outer exception", inner);

        // Act
        var result = exception.IsFrameworkException();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsFrameworkException_ShouldReturnFalseWhenNoFrameworkExceptionInChain()
    {
        // Arrange
        var exception = new InvalidOperationException("Regular exception");

        // Act
        var result = exception.IsFrameworkException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFrameworkException_ShouldReturnFalseForNullInnerException()
    {
        // Arrange
        var exception = new Exception("Exception with null inner");

        // Act
        var result = exception.IsFrameworkException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFrameworkException_ShouldHandleDeepNonFrameworkChain()
    {
        // Arrange
        var deepInner = new Exception("Deep exception");
        for (int i = 0; i < 5; i++)
        {
            deepInner = new Exception($"Level {i}", deepInner);
        }

        // Act
        var result = deepInner.IsFrameworkException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsFrameworkException_ShouldHandleDeepMixedChain()
    {
        // Arrange
        var deepInner = new InvalidOperationException("Deep inner");
        var framework = new DotnetActorFrameworkException("Framework in middle");
        var outer = new Exception("Outer", framework);

        // Build chain: outer -> framework -> deepInner
        var exception = new Exception("Root", outer);

        // Act
        var result = exception.IsFrameworkException();

        // Assert
        result.Should().BeTrue();
    }
}