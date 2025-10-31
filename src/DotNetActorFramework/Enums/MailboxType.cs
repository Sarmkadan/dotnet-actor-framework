// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Enums;

/// <summary>
/// Defines the available types of mailboxes for actors.
/// </summary>
public enum MailboxType
{
    /// <summary>
    /// First-In, First-Out (FIFO) mailbox. Messages are processed in the order they are received.
    /// </summary>
    FIFO,

    /// <summary>
    /// Priority mailbox. Messages are processed based on their assigned priority,
    /// with higher priority messages being processed before lower priority ones.
    /// </summary>
    Priority
}