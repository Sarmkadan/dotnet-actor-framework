// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Exception thrown when attempting to enqueue a message into a full bounded mailbox
/// with the Fail overflow policy.
/// </summary>
public class MailboxFullException : DotnetActorFrameworkException
{
    /// <summary>
    /// Gets the ID of the actor whose mailbox is full.
    /// </summary>
    public Guid ActorId { get; }

    /// <summary>
    /// Gets the capacity of the mailbox.
    /// </summary>
    public int Capacity { get; }

    /// <summary>
    /// Gets the number of messages currently in the mailbox.
    /// </summary>
    public int CurrentSize { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxFullException"/> class.
    /// </summary>
    /// <param name="actorId">The ID of the actor whose mailbox is full.</param>
    /// <param name="capacity">The capacity of the mailbox.</param>
    /// <param name="currentSize">The number of messages currently in the mailbox.</param>
    public MailboxFullException(Guid actorId, int capacity, int currentSize)
        : base($"Mailbox is full for actor {actorId}. Capacity: {capacity}, Current: {currentSize}")
    {
        ActorId = actorId;
        Capacity = capacity;
        CurrentSize = currentSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxFullException"/> class with a custom message.
    /// </summary>
    /// <param name="actorId">The ID of the actor whose mailbox is full.</param>
    /// <param name="capacity">The capacity of the mailbox.</param>
    /// <param name="currentSize">The number of messages currently in the mailbox.</param>
    /// <param name="message">The custom error message.</param>
    public MailboxFullException(Guid actorId, int capacity, int currentSize, string? message)
        : base(message ?? $"Mailbox is full for actor {actorId}. Capacity: {capacity}, Current: {currentSize}")
    {
        ActorId = actorId;
        Capacity = capacity;
        CurrentSize = currentSize;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MailboxFullException"/> class with a custom message and inner exception.
    /// </summary>
    /// <param name="actorId">The ID of the actor whose mailbox is full.</param>
    /// <param name="capacity">The capacity of the mailbox.</param>
    /// <param name="currentSize">The number of messages currently in the mailbox.</param>
    /// <param name="message">The custom error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public MailboxFullException(Guid actorId, int capacity, int currentSize, string? message, Exception? innerException)
        : base(message ?? $"Mailbox is full for actor {actorId}. Capacity: {capacity}, Current: {currentSize}", innerException)
    {
        ActorId = actorId;
        Capacity = capacity;
        CurrentSize = currentSize;
    }
}