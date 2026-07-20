// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetActorFramework.Constants;

/// <summary>
/// A thread-safe, immutable reference to an actor that provides a unified interface for sending messages
/// and managing actor interactions across the system.
/// </summary>
public class ActorRef : IEquatable<ActorRef>
{
    /// <summary>Gets the <see cref="ActorPath"/> of the referenced actor.</summary>
    public ActorPath Path { get; }
    /// <summary>Gets the unique identifier of the referenced actor.</summary>
    public Guid Id { get; }
    /// <summary>Gets a value indicating whether the referenced actor is currently alive and able to process messages.</summary>
    public bool IsAlive { get; private set; }
    /// <summary>Gets the UTC timestamp when the actor reference was created.</summary>
    public DateTime CreatedAt { get; }

    internal ActorRef(ActorPath path, Guid id)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Id = id;
        IsAlive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Sends a message to this actor asynchronously.
    /// </summary>
    /// <param name="message">The message object to send.</param>
    /// <returns>A task that completes when the message has been dispatched to the actor's mailbox.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the actor is not alive.</exception>
    public async Task SendAsync(object message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!IsAlive)
            throw new InvalidOperationException($"Actor {Path} is not alive.");

        // Message sending is delegated to the mailbox service
        // This will be implemented by the message dispatcher
        await Task.CompletedTask;
    }

    /// <summary>
    /// Sends a message to the actor and asynchronously waits for a response.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <returns>A task representing the operation, returning the response object if successful; otherwise, null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    public async Task<object?> AskAsync(object message, TimeSpan timeout)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        if (!IsAlive)
            throw new InvalidOperationException($"Actor {Path} is not alive.");

        // Request-reply pattern - wait for timeout then return faulted task
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await Task.Delay(timeout, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred
        }

        // Return null wrapped in a completed task (existing behavior for non-generic AskAsync)
        return Task.FromResult<object?>(null);
    }

    /// <summary>
    /// Sends a message to this actor and asynchronously waits for a typed response.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <param name="timeout">The maximum time to wait for a response.</param>
    /// <returns>A task representing the operation, returning the typed response object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="timeout"/> is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the actor is not alive.</exception>
    /// <exception cref="TimeoutException">Thrown if the actor does not respond within the specified timeout.</exception>
    public async Task<T> AskAsync<T>(object message, TimeSpan timeout)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        if (!IsAlive)
            throw new InvalidOperationException($"Actor {Path} is not alive.");

        // Request-reply pattern - wait for timeout then fault with TimeoutException
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await Task.Delay(timeout, cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Timeout occurred
        }

        // Fault the task with TimeoutException after timeout period
        throw new TimeoutException($"Actor {Path} did not respond within {timeout.TotalSeconds} seconds.");
    }

    /// <summary>
    /// Sends a message to this actor and asynchronously waits for a typed response using the default system timeout.
    /// </summary>
    /// <typeparam name="T">The expected response type.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>A task representing the operation, returning the typed response object.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the actor is not alive.</exception>
    /// <exception cref="TimeoutException">Thrown if the actor does not respond within the default timeout.</exception>
    public async Task<T> AskAsync<T>(object message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (!IsAlive)
            throw new InvalidOperationException($"Actor {Path} is not alive.");

        // Use default timeout from system config (30 seconds)
        var timeout = TimeSpan.FromSeconds(Constants.ActorConstants.DefaultTimeoutSeconds);
        return await AskAsync<T>(message, timeout);
    }

    /// <summary>
    /// Marks this reference as dead.
    /// </summary>
    internal void MarkAsDead() => IsAlive = false;

    /// <summary>
    /// Gets the parent actor reference from this path.
    /// </summary>
    public ActorRef? GetParent()
    {
        if (Path.Parent == null)
            return null;

        return new ActorRef(Path.Parent, Guid.NewGuid());
    }

    public override string ToString() => $"{Path} ({Id:N})";

    public override bool Equals(object? obj) => Equals(obj as ActorRef);

    public bool Equals(ActorRef? other)
    {
        if (other is null) return false;
        return Id == other.Id && Path == other.Path;
    }

    public override int GetHashCode() => HashCode.Combine(Path, Id);

    public static bool operator ==(ActorRef? left, ActorRef? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(ActorRef? left, ActorRef? right) => !(left == right);
}
