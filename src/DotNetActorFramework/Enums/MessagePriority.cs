// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Enums;

/// <summary>
/// Priority levels for message delivery.
/// Higher values are processed before lower values.
/// </summary>
public enum MessagePriority
{
    /// <summary>
    /// Lowest priority - processed last.
    /// </summary>
    Low = -1,

    /// <summary>
    /// Normal priority - default for most messages.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// High priority - processed before normal messages.
    /// </summary>
    High = 1,

    /// <summary>
    /// Critical priority - processed immediately.
    /// </summary>
    Critical = 2
}
