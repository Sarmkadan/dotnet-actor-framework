// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Enums;

/// <summary>
/// Supervision strategies for handling actor failures.
/// </summary>
public enum SupervisionStrategy
{
    /// <summary>
    /// Escalate the failure to the parent supervisor.
    /// </summary>
    Escalate = 0,

    /// <summary>
    /// Restart the failed actor.
    /// </summary>
    Restart = 1,

    /// <summary>
    /// Stop the failed actor without restarting.
    /// </summary>
    Stop = 2,

    /// <summary>
    /// Resume operation after the failure, ignoring it.
    /// </summary>
    Resume = 3,

    /// <summary>
    /// Backoff and retry with exponential delay.
    /// </summary>
    Backoff = 4
}
