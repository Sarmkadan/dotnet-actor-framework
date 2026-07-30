using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Tests
{
    public class GuardExtensionsJsonExtensionsTests
    {
        [Fact]
        public void ToJson_Happy_PATH()
        {
            // Arrange
            var guardPattern = GuardExtensions.NotNullOrEmpty;
            var json = GuardExtensionsJsonExtensions.ToJson(guardPattern);
            // Assert
            Assert.NotNull(json);
            Assert.Equal("NotNullOrEmpty", json);
        }

        [Fact]
        public void FromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "NotNullOrEmpty";
            var guardPattern = GuardExtensionsJsonExtensions.FromJson(json);
            // Assert
            Assert.NotNull(guardPattern);
            Assert.Equal(GuardExtensions.NotNullOrEmpty, guardPattern);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "NotNullOrEmpty";
            var guardPattern = GuardExtensionsJsonExtensions.TryFromJson(json, out var result);
            // Assert
            Assert.True(guardPattern);
            Assert.NotNull(result);
            Assert.Equal(GuardExtensions.NotNullOrEmpty, result);
        }

        [Fact]
        public void ToJson_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => GuardExtensionsJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_NULL_INPUT()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => GuardExtensionsJsonExtensions.FromJson(null));
        }

        [Fact]
        public void TryFromJson_NULL_INPUT()
        {
            // Arrange
            var json = "";
            var result = GuardExtensionsJsonExtensions.TryFromJson(json, out var guardPattern);
            // Assert
            Assert.False(result);
            Assert.Null(guardPattern);
        }
    }
}