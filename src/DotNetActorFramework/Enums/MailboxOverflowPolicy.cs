// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Enums;

/// <summary>
/// Defines the overflow policy for bounded mailboxes when the mailbox is full.
/// </summary>
public enum MailboxOverflowPolicy
{
    /// <summary>
    /// Drops the newest message when the mailbox is full.
    /// This is the default behavior for high-throughput systems where backpressure is not desired.
    /// </summary>
    DropNewest,

    /// <summary>
    /// Drops the oldest message when the mailbox is full.
    /// This preserves recent messages at the cost of older ones.
    /// </summary>
    DropOldest,

    /// <summary>
    /// Fails the enqueue operation by throwing a <see cref="MailboxFullException"/>.
    /// This applies backpressure to the sender, forcing it to handle the overload.
    /// </summary>
    Fail,

    /// <summary>
    /// Waits asynchronously until space becomes available in the mailbox.
    /// This applies backpressure by blocking the sender until the consumer can process messages.
    /// </summary>
    Wait
}