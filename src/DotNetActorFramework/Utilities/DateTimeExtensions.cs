// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Extension methods for DateTime operations commonly used in actor framework.
/// Simplifies timestamp comparisons, duration calculations, and time-based logic.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Gets the elapsed time since this datetime.
    /// </summary>
    public static TimeSpan GetElapsed(this DateTime dateTime)
    {
        return DateTime.UtcNow - dateTime;
    }

    /// <summary>
    /// Gets the elapsed milliseconds since this datetime.
    /// </summary>
    public static long GetElapsedMilliseconds(this DateTime dateTime)
    {
        return (long)dateTime.GetElapsed().TotalMilliseconds;
    }

    /// <summary>
    /// Determines if the specified duration has elapsed since this datetime.
    /// </summary>
    public static bool HasElapsed(this DateTime dateTime, TimeSpan duration)
    {
        return dateTime.GetElapsed() >= duration;
    }

    /// <summary>
    /// Determines if this datetime is in the past (before now).
    /// </summary>
    public static bool IsPast(this DateTime dateTime)
    {
        return dateTime < DateTime.UtcNow;
    }

    /// <summary>
    /// Determines if this datetime is in the future (after now).
    /// </summary>
    public static bool IsFuture(this DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a human-readable description of how long ago this datetime was.
    /// Examples: "2 seconds ago", "5 minutes ago", "3 hours ago"
    /// </summary>
    public static string GetTimeAgoDescription(this DateTime dateTime)
    {
        var elapsed = DateTime.UtcNow - dateTime;

        return elapsed.TotalSeconds < 60
            ? $"{(int)elapsed.TotalSeconds} seconds ago"
            : elapsed.TotalMinutes < 60
                ? $"{(int)elapsed.TotalMinutes} minutes ago"
                : elapsed.TotalHours < 24
                    ? $"{(int)elapsed.TotalHours} hours ago"
                    : $"{(int)elapsed.TotalDays} days ago";
    }

    /// <summary>
    /// Rounds a datetime to the nearest second, discarding milliseconds.
    /// </summary>
    public static DateTime RoundToSecond(this DateTime dateTime)
    {
        return dateTime.AddMilliseconds(-dateTime.Millisecond);
    }

    /// <summary>
    /// Determines if the time is within the specified window.
    /// </summary>
    public static bool IsWithinWindow(this DateTime dateTime, DateTime start, DateTime end)
    {
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Gets a formatted timestamp suitable for logging.
    /// Format: ISO 8601 with milliseconds
    /// </summary>
    public static string GetLogTimestamp(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
    }
}
