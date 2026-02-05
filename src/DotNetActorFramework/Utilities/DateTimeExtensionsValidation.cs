// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Validation extension methods for DateTimeExtensions that validate the semantic correctness
/// of DateTime values and their usage patterns in the actor framework.
/// </summary>
public static class DateTimeExtensionsValidation
{
    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with DateTimeExtensions methods.
    /// Checks for default DateTime values, dates in the future when past is expected, and other
    /// invalid patterns that would cause incorrect behavior.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> Validate(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Check for default DateTime (Unix epoch or MinValue)
        if (dateTime == default)
        {
            problems.Add("DateTime is default (Unix epoch or MinValue), which is invalid for actor framework operations");
        }

        // Check for DateTime.MinValue specifically
        if (dateTime == DateTime.MinValue)
        {
            problems.Add("DateTime is DateTime.MinValue, which indicates an uninitialized timestamp");
        }

        // Check for DateTime.MaxValue (practically impossible for real timestamps)
        if (dateTime == DateTime.MaxValue)
        {
            problems.Add("DateTime is DateTime.MaxValue, which is not a valid operational timestamp");
        }

        // Check if the date is in the future when it should logically be in the past
        // This catches cases where someone accidentally uses a future date
        if (dateTime > DateTime.UtcNow.AddYears(1))
        {
            problems.Add("DateTime is more than 1 year in the future, which is likely incorrect for operational timestamps");
        }

        // Check if the date is extremely far in the past (more than 100 years ago)
        if (dateTime < DateTime.UtcNow.AddYears(-100))
        {
            problems.Add("DateTime is more than 100 years in the past, which is likely incorrect for operational timestamps");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines if a DateTime value is semantically valid for use with DateTimeExtensions methods.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate</param>
    /// <returns>True if the DateTime is valid; otherwise false</returns>
    public static bool IsValid(this DateTime dateTime)
    {
        return dateTime.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a DateTime value is semantically valid for use with DateTimeExtensions methods.
    /// Throws an ArgumentException with detailed validation messages if the DateTime is invalid.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate</param>
    /// <exception cref="ArgumentException">Thrown when the DateTime is invalid with detailed validation messages</exception>
    public static void EnsureValid(this DateTime dateTime)
    {
        var problems = dateTime.Validate();

        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"DateTime validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the GetElapsed method.
    /// The GetElapsed method calculates time since the given DateTime, so future dates are semantically
    /// valid but will return negative TimeSpan values.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for GetElapsed usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForGetElapsed(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the GetElapsedMilliseconds method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for GetElapsedMilliseconds usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForGetElapsedMilliseconds(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value and duration are semantically valid for use with the HasElapsed method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate</param>
    /// <param name="duration">The duration to check against</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForHasElapsed(this DateTime dateTime, TimeSpan duration)
    {
        var problems = new List<string>();

        // Validate DateTime
        problems.AddRange(dateTime.Validate());

        // Validate duration is not negative (HasElapsed with negative duration always returns true)
        if (duration < TimeSpan.Zero)
        {
            problems.Add("Duration cannot be negative for HasElapsed check");
        }

        // Validate duration is not zero (HasElapsed with zero duration always returns true)
        if (duration == TimeSpan.Zero)
        {
            problems.Add("Duration cannot be zero for HasElapsed check - use direct comparison instead");
        }

        // Validate duration is not extremely large (more than 100 years)
        if (duration > TimeSpan.FromDays(365 * 100))
        {
            problems.Add("Duration is more than 100 years, which is likely incorrect for operational timeouts");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the IsPast method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for IsPast usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForIsPast(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the IsFuture method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for IsFuture usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForIsFuture(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the GetTimeAgoDescription method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for GetTimeAgoDescription usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForGetTimeAgoDescription(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the RoundToSecond method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for RoundToSecond usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForRoundToSecond(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value and window boundaries are semantically valid for use with the IsWithinWindow method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate</param>
    /// <param name="start">The start of the window</param>
    /// <param name="end">The end of the window</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForIsWithinWindow(this DateTime dateTime, DateTime start, DateTime end)
    {
        var problems = new List<string>();

        // Validate DateTime
        problems.AddRange(dateTime.Validate());

        // Validate start and end are not default
        if (start == default)
        {
            problems.Add("Window start DateTime is default (Unix epoch or MinValue)");
        }

        if (end == default)
        {
            problems.Add("Window end DateTime is default (Unix epoch or MinValue)");
        }

        // Validate start is before end
        if (start != default && end != default && start > end)
        {
            problems.Add("Window start cannot be after window end");
        }

        // Validate window is not extremely large (more than 100 years)
        if (start != default && end != default && (end - start).TotalDays > 365 * 100)
        {
            problems.Add("Time window is more than 100 years, which is likely incorrect for operational contexts");
        }

        // Validate window is not in the future (start in future means window hasn't opened yet)
        if (start != default && start > DateTime.UtcNow)
        {
            problems.Add("Window start is in the future, which may indicate incorrect configuration");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates that a DateTime value is semantically valid for use with the GetLogTimestamp method.
    /// </summary>
    /// <param name="dateTime">The DateTime value to validate for GetLogTimestamp usage</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable validation problems</returns>
    public static IReadOnlyList<string> ValidateForGetLogTimestamp(this DateTime dateTime)
    {
        var problems = new List<string>();

        // Basic DateTime validation
        problems.AddRange(dateTime.Validate());

        return problems.AsReadOnly();
    }
}