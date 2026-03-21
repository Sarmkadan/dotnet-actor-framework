// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Constants;

/// <summary>
/// Constants for system control messages.
/// </summary>
public static class MessageConstants
{
    /// <summary>
    /// Command to initialize an actor.
    /// </summary>
    public const string InitializeCommand = "initialize";

    /// <summary>
    /// Command to start an actor.
    /// </summary>
    public const string StartCommand = "start";

    /// <summary>
    /// Command to stop an actor.
    /// </summary>
    public const string StopCommand = "stop";

    /// <summary>
    /// Command to restart an actor.
    /// </summary>
    public const string RestartCommand = "restart";

    /// <summary>
    /// Command to suspend an actor.
    /// </summary>
    public const string SuspendCommand = "suspend";

    /// <summary>
    /// Command to resume an actor.
    /// </summary>
    public const string ResumeCommand = "resume";

    /// <summary>
    /// Command to get actor health metrics.
    /// </summary>
    public const string HealthCheckCommand = "health-check";

    /// <summary>
    /// Command to get actor metrics.
    /// </summary>
    public const string GetMetricsCommand = "get-metrics";

    /// <summary>
    /// Parameter key for mailbox address.
    /// </summary>
    public const string MailboxAddressParam = "mailbox_address";

    /// <summary>
    /// Parameter key for supervision strategy.
    /// </summary>
    public const string SupervisionStrategyParam = "supervision_strategy";

    /// <summary>
    /// Parameter key for failure reason.
    /// </summary>
    public const string FailureReasonParam = "failure_reason";

    /// <summary>
    /// Parameter key for error details.
    /// </summary>
    public const string ErrorDetailsParam = "error_details";

    /// <summary>
    /// Default timeout for message delivery in seconds.
    /// </summary>
    public const int DefaultMessageTimeoutSeconds = 10;

    /// <summary>
    /// Default message batch size for processing.
    /// </summary>
    public const int DefaultMessageBatchSize = 100;
}
