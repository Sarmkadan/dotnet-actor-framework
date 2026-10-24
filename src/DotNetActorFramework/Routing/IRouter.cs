// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Routing;

/// <summary>
/// Common contract for routers that deliver an envelope to one live actor selected
/// from the pool registered under a capability.
/// </summary>
/// <remarks>
/// Implementations differ only in how the target routee is chosen (for example,
/// least-loaded or round-robin); callers can depend on this interface to switch
/// strategies without changing message-sending code.
/// </remarks>
public interface IRouter
{
    /// <summary>
    /// Routes <paramref name="envelope"/> to a routee selected from the pool registered
    /// under <paramref name="capability"/> and delivers it.
    /// </summary>
    /// <param name="capability">The capability identifier that selects the actor pool.</param>
    /// <param name="envelope">The envelope to route and deliver.</param>
    /// <param name="cancellationToken">Token observed before dispatching.</param>
    /// <returns>
    /// <c>true</c> if the envelope was accepted for delivery; <c>false</c> when no live
    /// actors are registered for the capability or the target mailbox is full.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="capability"/> is blank.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/>
    /// is cancelled.</exception>
    Task<bool> RouteAsync(string capability, Envelope envelope, CancellationToken cancellationToken = default);
}
