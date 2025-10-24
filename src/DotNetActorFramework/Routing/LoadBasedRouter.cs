// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

namespace DotNetActorFramework.Routing;

/// <summary>
/// Routes incoming envelopes to one actor within a capability-scoped pool using
/// either least-mailbox-load selection or cyclic round-robin distribution.
/// </summary>
/// <remarks>
/// Least-load routing inspects live mailbox queue depths at dispatch time via
/// <see cref="MailboxService"/> and selects the actor with the fewest queued
/// messages, providing natural back-pressure-aware load balancing.
/// Round-robin routing ignores current load and guarantees even distribution
/// over time, which is preferable when messages have comparable processing cost.
/// </remarks>
public sealed class LoadBasedRouter
{
    private readonly ActorDiscoveryService _discovery;
    private readonly MailboxService _mailbox;
    private readonly MessageDispatcher _dispatcher;

    // Per-capability monotonic counters; overflow wraps safely via modulo.
    private readonly ConcurrentDictionary<string, int> _roundRobinCounters =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of <see cref="LoadBasedRouter"/>.
    /// </summary>
    /// <param name="discovery">Discovery service used to resolve the actor pool.</param>
    /// <param name="mailbox">Mailbox service used to query per-actor queue depths.</param>
    /// <param name="dispatcher">Dispatcher used to deliver routed envelopes.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <c>null</c>.</exception>
    public LoadBasedRouter(
        ActorDiscoveryService discovery,
        MailboxService mailbox,
        MessageDispatcher dispatcher)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _mailbox = mailbox ?? throw new ArgumentNullException(nameof(mailbox));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <summary>
    /// Dispatches <paramref name="envelope"/> to the least-loaded live actor registered
    /// under <paramref name="capability"/>.
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
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task<bool> RouteAsync(
        string capability,
        Envelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capability))
            throw new ArgumentException("Capability must not be empty.", nameof(capability));
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));

        cancellationToken.ThrowIfCancellationRequested();

        var target = GetLeastLoaded(capability);
        if (target is null)
            return false;

        var routed = new Envelope(envelope.Message, target, envelope.Sender);
        return await _dispatcher.DispatchAsync(routed);
    }

    /// <summary>
    /// Dispatches <paramref name="envelope"/> to the next actor in a round-robin sequence
    /// across all live actors registered under <paramref name="capability"/>.
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
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is cancelled.</exception>
    public async Task<bool> RouteRoundRobinAsync(
        string capability,
        Envelope envelope,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(capability))
            throw new ArgumentException("Capability must not be empty.", nameof(capability));
        if (envelope == null) throw new ArgumentNullException(nameof(envelope));

        cancellationToken.ThrowIfCancellationRequested();

        var actors = _discovery.Discover(capability);
        if (actors.Count == 0)
            return false;

        // Atomically advance the counter; cap at int.MaxValue - 1 to avoid negative overflow.
        var index = _roundRobinCounters.AddOrUpdate(
            capability,
            _ => 0,
            (_, prev) => prev >= int.MaxValue - 1 ? 0 : prev + 1);

        var target = actors[index % actors.Count];

        var routed = new Envelope(envelope.Message, target, envelope.Sender);
        return await _dispatcher.DispatchAsync(routed);
    }

    /// <summary>
    /// Selects the live actor with the fewest queued messages within the pool
    /// registered under <paramref name="capability"/>.
    /// </summary>
    /// <param name="capability">The capability identifier that selects the actor pool.</param>
    /// <returns>
    /// The <see cref="ActorRef"/> with the minimum mailbox depth, or <c>null</c> when
    /// no live actors are registered for the capability.
    /// </returns>
    public ActorRef? GetLeastLoaded(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return null;

        var actors = _discovery.Discover(capability);
        if (actors.Count == 0)
            return null;

        ActorRef? best = null;
        var minLoad = int.MaxValue;

        foreach (var actor in actors)
        {
            var load = _mailbox.GetMailboxSize(actor.Id);
            if (load < minLoad)
            {
                minLoad = load;
                best = actor;
            }
        }

        return best;
    }

    /// <summary>
    /// Returns a snapshot of the current mailbox depth for every live actor
    /// registered under <paramref name="capability"/>, keyed by actor path name.
    /// </summary>
    /// <param name="capability">The capability identifier that selects the actor pool.</param>
    /// <returns>
    /// A dictionary mapping actor path name to current mailbox depth; empty when no
    /// live actors are registered for the capability.
    /// </returns>
    public IReadOnlyDictionary<string, int> GetLoadSnapshot(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return new Dictionary<string, int>();

        var actors = _discovery.Discover(capability);
        var snapshot = new Dictionary<string, int>(actors.Count);

        foreach (var actor in actors)
            snapshot[actor.Path.Name] = _mailbox.GetMailboxSize(actor.Id);

        return snapshot;
    }
}
