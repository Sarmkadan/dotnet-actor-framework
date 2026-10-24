// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using Microsoft.Extensions.Logging;

namespace DotNetActorFramework.Routing;

/// <summary>
/// Routes incoming envelopes to live actors within a capability-scoped pool in strict
/// cyclic order, ignoring current mailbox load.
/// </summary>
/// <remarks>
/// Each capability keeps its own monotonic counter, so over any run of
/// <c>n</c> consecutive messages every routee in a pool of size <c>n</c> receives exactly
/// one. Use this router when messages have comparable processing cost; use
/// <see cref="LoadBasedRouter"/> when routee speed or backlog varies.
/// </remarks>
public sealed class RoundRobinRouter : IRouter
{
    private readonly ActorDiscoveryService _discovery;
    private readonly MessageDispatcher _dispatcher;
    private readonly ILogger<RoundRobinRouter>? _logger;

    // Per-capability counters. Advancing via AddOrUpdate is atomic, so concurrent
    // callers never receive the same slot twice in a row.
    private readonly ConcurrentDictionary<string, int> _counters =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of <see cref="RoundRobinRouter"/>.
    /// </summary>
    /// <param name="discovery">Discovery service used to resolve the actor pool.</param>
    /// <param name="dispatcher">Dispatcher used to deliver routed envelopes.</param>
    /// <param name="logger">Optional logger for routing diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required parameter is <c>null</c>.</exception>
    public RoundRobinRouter(
        ActorDiscoveryService discovery,
        MessageDispatcher dispatcher,
        ILogger<RoundRobinRouter>? logger = null)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger;
    }

    /// <summary>
    /// Dispatches <paramref name="envelope"/> to the next live actor in round-robin order
    /// among those registered under <paramref name="capability"/>.
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
    public async Task<bool> RouteAsync(
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
        {
            _logger?.LogWarning(
                "No live actors are registered for capability {Capability}",
                capability);
            return false;
        }

        var target = SelectNext(capability, actors);
        var routed = new Envelope(envelope.Message, target, envelope.Sender);
        var dispatched = await _dispatcher.DispatchAsync(routed);

        if (dispatched)
        {
            _logger?.LogDebug(
                "Routed envelope for capability {Capability} to actor {ActorId} using round-robin",
                capability,
                target.Id);
        }
        else
        {
            _logger?.LogWarning(
                "Routing failed because the target mailbox is full for capability {Capability}, actor {ActorId}",
                capability,
                target.Id);
        }

        return dispatched;
    }

    /// <summary>
    /// Advances the counter for <paramref name="capability"/> and returns the routee at the
    /// resulting position in <paramref name="pool"/>.
    /// </summary>
    /// <param name="capability">The capability identifier used as the counter key.</param>
    /// <param name="pool">The non-empty actor pool to select from.</param>
    /// <returns>The selected actor.</returns>
    private ActorRef SelectNext(string capability, IReadOnlyList<ActorRef> pool)
    {
        // Wrap to zero before overflow so the counter never goes negative.
        var index = _counters.AddOrUpdate(
            capability,
            _ => 0,
            (_, prev) => prev >= int.MaxValue - 1 ? 0 : prev + 1);

        return pool[index % pool.Count];
    }
}
