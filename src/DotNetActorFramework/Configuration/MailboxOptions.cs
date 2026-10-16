// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Constants;
using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// The settings that define a single actor mailbox. Validate the settings before creating a mailbox
/// so that misconfiguration fails at startup rather than on the first message.
/// </summary>
public class MailboxOptions
{
    /// <summary>The minimum allowed high-watermark warning threshold percentage.</summary>
    private const int MinThresholdPercent = 0;

    /// <summary>The maximum allowed high-watermark warning threshold percentage.</summary>
    private const int MaxThresholdPercent = 100;

    /// <summary>
    /// Gets or sets the maximum number of messages a mailbox can hold. Must be greater than zero.
    /// </summary>
    public int Capacity { get; set; } = ActorConstants.DefaultMailboxCapacity;

    /// <summary>
    /// Gets or sets the type of mailbox to create.
    /// </summary>
    public MailboxType Type { get; set; } = MailboxType.FIFO;

    /// <summary>
    /// Gets or sets the policy applied when the mailbox is full.
    /// </summary>
    public MailboxOverflowPolicy OverflowPolicy { get; set; } = MailboxOverflowPolicy.DropNewest;

    /// <summary>
    /// Gets or sets the load percentage (0-100) above which a high-watermark warning is logged.
    /// </summary>
    public int HighWatermarkWarningThreshold { get; set; } = 80;

    /// <summary>
    /// Validates the mailbox settings.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <see cref="Capacity"/> is not positive, <see cref="Type"/> or <see cref="OverflowPolicy"/>
    /// is not a defined enum value, or <see cref="HighWatermarkWarningThreshold"/> is outside 0-100.
    /// </exception>
    public void Validate()
    {
        if (Capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(Capacity), Capacity, "Mailbox capacity must be greater than zero.");

        if (!Enum.IsDefined(Type))
            throw new ArgumentOutOfRangeException(nameof(Type), Type, $"Unknown mailbox type: {Type}.");

        if (!Enum.IsDefined(OverflowPolicy))
            throw new ArgumentOutOfRangeException(nameof(OverflowPolicy), OverflowPolicy, $"Unknown mailbox overflow policy: {OverflowPolicy}.");

        if (HighWatermarkWarningThreshold < MinThresholdPercent || HighWatermarkWarningThreshold > MaxThresholdPercent)
            throw new ArgumentOutOfRangeException(
                nameof(HighWatermarkWarningThreshold),
                HighWatermarkWarningThreshold,
                "High watermark threshold must be between 0 and 100.");
    }
}
