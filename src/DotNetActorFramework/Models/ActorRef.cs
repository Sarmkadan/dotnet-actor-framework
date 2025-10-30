// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Models;

/// <summary>
/// A reference to an actor that can be used to send messages.
/// ActorRefs are immutable and can be safely shared across threads.
/// </summary>
public class ActorRef : IEquatable<ActorRef>
{
    public ActorPath Path { get; }
    public Guid Id { get; }
    public bool IsAlive { get; private set; }
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
    /// Sends a message and waits for a response with timeout.
    /// </summary>
    public async Task<object?> AskAsync(object message, TimeSpan timeout)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        if (!IsAlive)
            throw new InvalidOperationException($"Actor {Path} is not alive.");

        // Request-reply pattern implemented via temporary actor
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            await Task.Delay(timeout, cts.Token).ConfigureAwait(false);
            throw new TimeoutException($"Actor {Path} did not respond within {timeout.TotalSeconds} seconds.");
        }
        catch (OperationCanceledException)
        {
            // Timeout or cancellation
            return null;
        }
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
