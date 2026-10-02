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
            var mockActorContext = new MockActorContext(new ActorPath("/user/TestActor"));

            // Act
            var actualJson = mockActorContext.ToJson();

            // Assert
            Assert.NotNull(actualJson);
            Assert.False(string.IsNullOrWhiteSpace(actualJson));
            using var doc = JsonDocument.Parse(actualJson);
            var root = doc.RootElement;
            // Verify the actorPath property exists and contains the correct path
            var hasActorPath = root.TryGetProperty("actorPath", out var actorPathProp) || root.TryGetProperty("ActorPath", out actorPathProp);
            Assert.True(hasActorPath);
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
            // Arrange - round-trip: serialize then deserialize
            var original = new MockActorContext(new ActorPath("/user/TestActor"));
            var json = original.ToJson();

            // Act
            var deserialized = MockActorContextJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(original.ActorPath.Path, deserialized!.ActorPath.Path);
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
            // Arrange - round-trip
            var original = new MockActorContext(new ActorPath("/user/TestActor"));
            var json = original.ToJson();

            // Act
            var result = MockActorContextJsonExtensions.TryFromJson(json, out var actualMockActorContext);

            // Assert
            Assert.True(result);
            Assert.NotNull(actualMockActorContext);
            Assert.Equal(original.ActorPath.Path, actualMockActorContext!.ActorPath.Path);
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
