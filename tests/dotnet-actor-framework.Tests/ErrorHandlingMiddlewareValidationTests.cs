using DotNetActorFramework.Middleware;
using DotNetActorFramework.Models;
using Xunit;
using FluentAssertions;

namespace DotNetActorFramework.Tests
{
    public class ErrorHandlingMiddlewareValidationTests
    {
        [Fact]
        public void Validate_HappyPath_ForEachMajorPublicMethod_ShouldReturnNoErrors()
        {
            // Arrange
            var middleware = new ErrorHandlingMiddleware(new SuppressErrorStrategy());

            // Act
            var errors = ErrorHandlingMiddlewareValidation.Validate(middleware);

            // Assert
            errors.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_HappyPath_ForEachMajorPublicMethod_ShouldReturnTrue()
        {
            // Arrange
            var middleware = new ErrorHandlingMiddleware(new SuppressErrorStrategy());

            // Act
            var isValid = ErrorHandlingMiddlewareValidation.IsValid(middleware);

            // Assert
            isValid.Should().BeTrue();
        }

        [Fact]
        public void EnsureValid_HappyPath_ForEachMajorPublicMethod_ShouldNotThrow()
        {
            // Arrange
            var middleware = new ErrorHandlingMiddleware(new SuppressErrorStrategy());

            // Act
            Action act = () => ErrorHandlingMiddlewareValidation.EnsureValid(middleware);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Validate_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            ErrorHandlingMiddleware? middleware = null;

            // Act
            Action act = () => ErrorHandlingMiddlewareValidation.Validate(middleware);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsValid_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            ErrorHandlingMiddleware? middleware = null;

            // Act
            Action act = () => ErrorHandlingMiddlewareValidation.IsValid(middleware);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void EnsureValid_NullInput_ShouldThrowArgumentNullException()
        {
            // Arrange
            ErrorHandlingMiddleware? middleware = null;

            // Act
            Action act = () => ErrorHandlingMiddlewareValidation.EnsureValid(middleware);

            // Assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}