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
    /// <param name="dateTime">The datetime to calculate elapsed time from.</param>
    /// <returns>The elapsed <see cref="TimeSpan"/> since the specified datetime.</returns>
    public static TimeSpan GetElapsed(this DateTime dateTime)
    {
        return DateTime.UtcNow - dateTime;
    }

    /// <summary>
    /// Gets the elapsed milliseconds since this datetime.
    /// </summary>
    /// <param name="dateTime">The datetime to calculate elapsed milliseconds from.</param>
    /// <returns>The elapsed time in milliseconds.</returns>
    public static long GetElapsedMilliseconds(this DateTime dateTime)
        => (long)dateTime.GetElapsed().TotalMilliseconds;

    /// <summary>
    /// Determines if the specified duration has elapsed since this datetime.
    /// </summary>
    /// <param name="dateTime">The datetime to check against.</param>
    /// <param name="duration">The duration to compare against.</param>
    /// <returns><see langword="true"/> if the duration has elapsed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is negative.</exception>
    public static bool HasElapsed(this DateTime dateTime, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
        }
        return dateTime.GetElapsed() >= duration;
    }

    /// <summary>
    /// Determines if this datetime is in the past (before now).
    /// </summary>
    /// <param name="dateTime">The datetime to check.</param>
    /// <returns><see langword="true"/> if the datetime is in the past; otherwise, <see langword="false"/>.</returns>
    public static bool IsPast(this DateTime dateTime)
        => dateTime < DateTime.UtcNow;

    /// <summary>
    /// Determines if this datetime is in the future (after now).
    /// </summary>
    /// <param name="dateTime">The datetime to check.</param>
    /// <returns><see langword="true"/> if the datetime is in the future; otherwise, <see langword="false"/>.</returns>
    public static bool IsFuture(this DateTime dateTime)
        => dateTime > DateTime.UtcNow;

    /// <summary>
    /// Gets a human-readable description of how long ago this datetime was.
    /// Examples: "2 seconds ago", "5 minutes ago", "3 hours ago", "2 days ago"
    /// </summary>
    /// <param name="dateTime">The datetime to describe.</param>
    /// <returns>A human-readable string representing the time elapsed.</returns>
    public static string GetTimeAgoDescription(this DateTime dateTime)
    {
        var elapsed = DateTime.UtcNow - dateTime;

        return elapsed.TotalSeconds switch
        {
            < 60 => $"{(int)elapsed.TotalSeconds} second{(elapsed.TotalSeconds == 1 ? "" : "s")} ago",
            < 3600 => $"{(int)elapsed.TotalMinutes} minute{(elapsed.TotalMinutes == 1 ? "" : "s")} ago",
            < 86400 => $"{(int)elapsed.TotalHours} hour{(elapsed.TotalHours == 1 ? "" : "s")} ago",
            _ => $"{(int)elapsed.TotalDays} day{(elapsed.TotalDays == 1 ? "" : "s")} ago"
        };
    }

    /// <summary>
    /// Truncates a datetime to whole seconds, discarding milliseconds and smaller tick components.
    /// </summary>
    /// <param name="dateTime">The datetime to truncate.</param>
    /// <returns>A new <see cref="DateTime"/> truncated to the second, preserving the original <see cref="DateTime.Kind"/>.</returns>
    public static DateTime RoundToSecond(this DateTime dateTime)
        => new(dateTime.Ticks - dateTime.Ticks % TimeSpan.TicksPerSecond, dateTime.Kind);

    /// <summary>
    /// Determines if the time is within the specified window.
    /// </summary>
    /// <param name="dateTime">The datetime to check.</param>
    /// <param name="start">The start of the window.</param>
    /// <param name="end">The end of the window.</param>
    /// <returns><see langword="true"/> if the datetime is within the window; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="start"/> is after <paramref name="end"/>.</exception>
    public static bool IsWithinWindow(this DateTime dateTime, DateTime start, DateTime end)
    {
        if (start > end)
        {
            throw new ArgumentOutOfRangeException(nameof(start), start, "Start time cannot be after end time.");
        }
        return dateTime >= start && dateTime <= end;
    }

    /// <summary>
    /// Gets a formatted timestamp suitable for logging.
    /// Format: ISO 8601 with milliseconds (UTC)
    /// </summary>
    /// <param name="dateTime">The datetime to format.</param>
    /// <returns>A formatted timestamp string.</returns>
    public static string GetLogTimestamp(this DateTime dateTime)
        => dateTime.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture);
}
