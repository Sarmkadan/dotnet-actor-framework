// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Constants;

/// <summary>
/// Constants related to actor behavior and configuration.
/// </summary>
public static class ActorConstants
{
    /// <summary>
    /// Default timeout for actor operations in seconds.
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    /// <summary>
    /// Maximum number of message retries.
    /// </summary>
    public const int MaxMessageRetries = 3;

    /// <summary>
    /// Default mailbox size capacity.
    /// </summary>
    public const int DefaultMailboxCapacity = 1000;

    /// <summary>
    /// Default max actor depth in supervision hierarchy.
    /// </summary>
    public const int DefaultMaxActorDepth = 10;

    /// <summary>
    /// Root actor path.
    /// </summary>
    public const string RootActorPath = "/root";

    /// <summary>
    /// System actor path prefix.
    /// </summary>
    public const string SystemActorPrefix = "/system";

    /// <summary>
    /// User actor path prefix.
    /// </summary>
    public const string UserActorPrefix = "/user";

    /// <summary>
    /// Initial backoff delay in milliseconds for failed messages.
    /// </summary>
    public const int InitialBackoffDelayMs = 100;

    /// <summary>
    /// Maximum backoff delay in milliseconds.
    /// </summary>
    public const int MaxBackoffDelayMs = 30000;

    /// <summary>
    /// Backoff multiplier for exponential backoff.
    /// </summary>
    public const double BackoffMultiplier = 1.5;

    /// <summary>
    /// Error rate threshold (as percentage) above which an actor is considered unhealthy.
    /// </summary>
    public const double UnhealthyErrorRateThreshold = 0.25; // 25%

    /// <summary>
    /// Maximum allowed error rate before actor termination (as percentage).
    /// </summary>
    public const double CriticalErrorRateThreshold = 0.75; // 75%
}
