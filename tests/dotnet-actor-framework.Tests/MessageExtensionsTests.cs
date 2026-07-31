using System;
using System.Collections.Generic;
using DotNetActorFramework.Models;
using Xunit;

namespace DotNetActorFramework.Tests
{
    public class MessageExtensionsTests
    {
        [Fact]
        public void WithHeader_AddsHeaderAndReturnsNewMessage()
        {
            // Arrange
            var original = new ControlMessage("test");

            // Act
            var withHeader = original.WithHeader("UserId", 42);

            // Assert
            Assert.NotSame(original, withHeader);
            Assert.Equal(42, withHeader.GetHeaderOrDefault<int>("UserId"));
            // Original should have no headers
            Assert.Equal(default(int), original.GetHeaderOrDefault<int>("UserId"));
        }

        [Fact]
        public void GetHeaderOrDefault_ReturnsDefaultWhenMissing()
        {
            var msg = new ControlMessage("cmd");
            var result = msg.GetHeaderOrDefault<string>("NonExisting", "fallback");
            Assert.Equal("fallback", result);
        }

        [Fact]
        public void IsExpired_ReturnsTrueWhenOlderThanTtl()
        {
            // Create a message with a created timestamp 2 hours ago
            var past = DateTime.UtcNow - TimeSpan.FromHours(2);
            var msg = new ControlMessage("old") with { CreatedAt = past };

            var ttl = TimeSpan.FromHours(1);
            Assert.True(msg.IsExpired(ttl));
        }

        [Fact]
        public void IsExpired_ReturnsFalseWhenWithinTtl()
        {
            var msg = new ControlMessage("fresh");
            var ttl = TimeSpan.FromHours(1);
            Assert.False(msg.IsExpired(ttl));
        }
    }
}
