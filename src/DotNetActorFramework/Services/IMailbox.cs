using DotNetActorFramework.Models;

namespace DotNetActorFramework.Services;

/// <summary>
/// Defines the contract for an actor's mailbox.
/// </summary>
public interface IMailbox : IDisposable
{
    /// <summary>
    /// Enqueues a message into the mailbox.
    /// </summary>
    Task<bool> EnqueueAsync(Envelope envelope);
    
    /// <summary>
    /// Dequeues a message from the mailbox.
    /// </summary>
    Task<Envelope?> DequeueAsync();
    
    /// <summary>
    /// Gets the number of messages in the mailbox.
    /// </summary>
    int GetSize();
    
    /// <summary>
    /// Gets a value indicating whether the mailbox is full.
    /// </summary>
    bool IsFull { get; }
    
    /// <summary>
    /// Gets the capacity of the mailbox.
    /// </summary>
    int Capacity { get; }
    
    /// <summary>
    /// Gets the load factor of the mailbox.
    /// </summary>
    double GetLoadFactor();
}
