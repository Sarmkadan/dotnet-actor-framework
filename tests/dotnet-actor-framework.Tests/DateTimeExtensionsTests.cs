// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Utilities;
using FluentAssertions;
using Xunit;
using System;

namespace DotNetActorFramework.Tests;

public class DateTimeExtensionsTests
{
    [Fact]
    public void GetElapsed_PastDateTime_ReturnsApproximateDurationAndMilliseconds()
    {
        // Arrange
        var start = DateTime.UtcNow.AddSeconds(-2);

        // Act
        var elapsed = start.GetElapsed();
        var milliseconds = start.GetElapsedMilliseconds();

        // Assert
        elapsed.Should().BeCloseTo(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1));
        milliseconds.Should().BeInRange(2000, 3500);
    }

    [Fact]
    public void HasElapsed_DurationReachedOrNot_ReturnsExpected()
    {
        // Arrange
        var tenMinutesAgo = DateTime.UtcNow.AddMinutes(-10);
        var oneMinuteAgo = DateTime.UtcNow.AddMinutes(-1);
        var future = DateTime.UtcNow.AddMinutes(5);

        // Act & Assert
        tenMinutesAgo.HasElapsed(TimeSpan.FromMinutes(5)).Should().BeTrue();
        oneMinuteAgo.HasElapsed(TimeSpan.FromMinutes(5)).Should().BeFalse();
        oneMinuteAgo.HasElapsed(TimeSpan.Zero).Should().BeTrue();
        future.HasElapsed(TimeSpan.Zero).Should().BeFalse();
    }

    [Fact]
    public void HasElapsed_NegativeDuration_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddMinutes(-10);

        // Act
        Action act = () => dateTime.HasElapsed(TimeSpan.FromSeconds(-1));

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("duration");
    }

    [Fact]
    public void IsPast_AndIsFuture_ClassifyAgainstUtcNow()
    {
        // Act & Assert
        DateTime.UtcNow.AddHours(-1).IsPast().Should().BeTrue();
        DateTime.UtcNow.AddHours(-1).IsFuture().Should().BeFalse();
        DateTime.UtcNow.AddHours(1).IsFuture().Should().BeTrue();
        DateTime.UtcNow.AddHours(1).IsPast().Should().BeFalse();
        DateTime.MinValue.IsPast().Should().BeTrue();
        DateTime.MaxValue.IsFuture().Should().BeTrue();
    }

    [Theory]
    [InlineData(-30, "30 seconds ago")]
    [InlineData(-45, "45 seconds ago")]
    [InlineData(-300, "5 minutes ago")]
    [InlineData(-3540, "59 minutes ago")]
    [InlineData(-7200, "2 hours ago")]
    [InlineData(-82800, "23 hours ago")]
    [InlineData(-172800, "2 days ago")]
    public void GetTimeAgoDescription_PluralUnits_UsesUnitThresholds(int secondsAgo, string expected)
    {
        // Arrange
        var dateTime = DateTime.UtcNow.AddSeconds(secondsAgo);

        // Act
        var description = dateTime.GetTimeAgoDescription();

        // Assert
        description.Should().Be(expected);
    }

    [Fact]
    public void RoundToSecond_SubSecondTicks_TruncatesAndKeepsKind()
    {
        // Arrange
        var utc = new DateTime(2025, 1, 2, 3, 4, 5, 999, DateTimeKind.Utc);
        var local = new DateTime(2025, 1, 2, 3, 4, 5, 500, DateTimeKind.Local);

        // Act
        var truncatedUtc = utc.RoundToSecond();
        var truncatedLocal = local.RoundToSecond();

        // Assert
        truncatedUtc.Should().Be(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        truncatedUtc.Kind.Should().Be(DateTimeKind.Utc);
        truncatedLocal.Should().Be(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Local));
        truncatedLocal.Kind.Should().Be(DateTimeKind.Local);
    }

    [Fact]
    public void IsWithinWindow_InclusiveBounds_ReversedWindowThrows()
    {
        // Arrange
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddHours(1);

        // Act & Assert
        start.IsWithinWindow(start, end).Should().BeTrue();
        end.IsWithinWindow(start, end).Should().BeTrue();
        start.AddTicks(-1).IsWithinWindow(start, end).Should().BeFalse();
        end.AddTicks(1).IsWithinWindow(start, end).Should().BeFalse();
        start.IsWithinWindow(start, start).Should().BeTrue();

        Action act = () => start.IsWithinWindow(end, start);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("start");
    }

    [Fact]
    public void GetLogTimestamp_UtcAndLocalInputs_FormatsAsUtcIso8601WithMilliseconds()
    {
        // Arrange
        var utc = new DateTime(2024, 3, 15, 10, 30, 45, 123, DateTimeKind.Utc);
        var local = utc.ToLocalTime();

        // Act & Assert
        utc.GetLogTimestamp().Should().Be("2024-03-15T10:30:45.123Z");
        local.GetLogTimestamp().Should().Be("2024-03-15T10:30:45.123Z");
    }
}
