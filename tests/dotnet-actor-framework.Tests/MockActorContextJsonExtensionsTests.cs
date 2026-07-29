using Xunit;
using System.Text.Json;
using DotNetActorFramework.Models;
using DotNetActorFramework.Testing;

namespace DotNetActorFramework.Tests
{
    public class MockActorContextJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_Test()
        {
            // Arrange
            var mockActorContext = new MockActorContext(new ActorPath("TestActor"));
            var expectedJson = "{\"ActorPath\":\"TestActor\"}";

            // Act
            var actualJson = mockActorContext.ToJson();

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException_Test()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => new MockActorContext(null).ToJson());
        }

        [Fact]
        public void FromJson_HappyPath_Test()
        {
            // Arrange
            var json = "{\"ActorPath\":\"TestActor\"}";
            var expectedMockActorContext = new MockActorContext(new ActorPath("TestActor"));

            // Act
            var actualMockActorContext = MockActorContextJsonExtensions.FromJson(json);

            // Assert
            Assert.Equal(expectedMockActorContext, actualMockActorContext);
        }

        [Fact]
        public void FromJson_NullInput_ReturnsNull_Test()
        {
            // Act
            var actualMockActorContext = MockActorContextJsonExtensions.FromJson(null);

            // Assert
            Assert.Null(actualMockActorContext);
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsNull_Test()
        {
            // Act
            var actualMockActorContext = MockActorContextJsonExtensions.FromJson("");

            // Assert
            Assert.Null(actualMockActorContext);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrue_Test()
        {
            // Arrange
            var json = "{\"ActorPath\":\"TestActor\"}";
            var expectedMockActorContext = new MockActorContext(new ActorPath("TestActor"));

            // Act
            var result = MockActorContextJsonExtensions.TryFromJson(json, out var actualMockActorContext);

            // Assert
            Assert.True(result);
            Assert.Equal(expectedMockActorContext, actualMockActorContext);
        }

        [Fact]
        public void TryFromJson_NullInput_ReturnsFalse_Test()
        {
            // Act
            var result = MockActorContextJsonExtensions.TryFromJson(null, out _);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void TryFromJson_EmptyJson_ReturnsFalse_Test()
        {
            // Act
            var result = MockActorContextJsonExtensions.TryFromJson("", out _);

            // Assert
            Assert.False(result);
        }
    }
}
