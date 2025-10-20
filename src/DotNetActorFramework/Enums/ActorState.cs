// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Enums;

/// <summary>
/// Represents the lifecycle state of an actor.
/// </summary>
public enum ActorState
{
    /// <summary>
    /// Actor has been created but not yet initialized.
    /// </summary>
    Created = 0,

    /// <summary>
    /// Actor is in the process of initialization.
    /// </summary>
    Initializing = 1,

    /// <summary>
    /// Actor is running and processing messages.
    /// </summary>
    Started = 2,

    /// <summary>
    /// Actor is in the process of stopping.
    /// </summary>
    Stopping = 3,

    /// <summary>
    /// Actor has been terminated and is no longer available.
    /// </summary>
    Terminated = 4,

    /// <summary>
    /// Actor encountered an error and is in an error state.
    /// </summary>
    Error = 5,

    /// <summary>
    /// Actor is suspended and not processing messages.
    /// </summary>
    Suspended = 6
}
