// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Messaging;

/// <summary>
/// Process-wide registry of pending <c>Ask</c> operations, keyed by the
/// <see cref="Message.MessageId"/> of the outstanding request. Bridges the
/// asynchronous request/response pattern implemented by <see cref="AskExtensions"/>
/// with the normal fire-and-forget message dispatch pipeline: when a
/// <see cref="ResponseMessage"/> or <see cref="FailureMessage"/> is dispatched, the
/// pipeline consults this registry via <see cref="TryComplete"/> to see whether it
/// answers a pending ask rather than a regular mailbox delivery.
/// </summary>
/// <remarks>
/// Entries are removed as soon as they are consumed - either by a matching reply
/// (<see cref="TryComplete"/>) or by the asking side giving up
/// (<see cref="RemovePending"/>, called on timeout or cancellation). A reply that
/// arrives after its entry has already been removed is simply ignored: there is
/// nothing left in the registry to complete, so it is dropped silently instead of
/// throwing against a <see cref="System.Threading.Tasks.TaskCompletionSource{TResult}"/>
/// nobody is waiting on anymore.
/// </remarks>
internal static class AskRegistry
{
    private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<Message>> Pending = new();

    /// <summary>
    /// Registers a pending ask so that a future reply correlated by
    /// <paramref name="correlationId"/> can be routed back to <paramref name="completionSource"/>.
    /// </summary>
    /// <param name="correlationId">The <see cref="Message.MessageId"/> of the outstanding request.</param>
    /// <param name="completionSource">The completion source to fulfil when a matching reply arrives.</param>
    /// <returns><c>true</c> if the ask was registered; <c>false</c> if an ask with the same id is already pending.</returns>
    public static bool TryRegisterPending(Guid correlationId, TaskCompletionSource<Message> completionSource)
    {
        ArgumentNullException.ThrowIfNull(completionSource);

        return Pending.TryAdd(correlationId, completionSource);
    }

    /// <summary>
    /// Attempts to complete a pending ask with an incoming reply, matching on
    /// <see cref="Message.CorrelationId"/>.
    /// </summary>
    /// <param name="reply">The <see cref="ResponseMessage"/> or <see cref="FailureMessage"/> being dispatched.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="reply"/> correlated to a still-pending ask and was delivered to it;
    /// <c>false</c> if no ask is waiting for it (including one that already timed out), in which case
    /// the reply should be dropped rather than delivered anywhere else.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reply"/> is <c>null</c>.</exception>
    public static bool TryComplete(Message reply)
    {
        ArgumentNullException.ThrowIfNull(reply);

        if (reply.CorrelationId == Guid.Empty)
            return false;

        return Pending.TryRemove(reply.CorrelationId, out var completionSource) && completionSource.TrySetResult(reply);
    }

    /// <summary>
    /// Removes a pending ask without completing it, used when the asking side gives up
    /// (timeout or cancellation) so the registry never accumulates dead entries.
    /// </summary>
    /// <param name="correlationId">The <see cref="Message.MessageId"/> of the request being abandoned.</param>
    public static void RemovePending(Guid correlationId) => Pending.TryRemove(correlationId, out _);
}
