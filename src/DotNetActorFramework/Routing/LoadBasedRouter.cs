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
    private readonly ILogger<LoadBasedRouter>? _logger;

    // Per-capability monotonic counters; overflow wraps safely via modulo.
    private readonly ConcurrentDictionary<string, int> _roundRobinCounters =
        new(StringComparer.OrdinalIgnoreCase);

    // Thread-local RNG avoids lock contention on a single shared Random instance
    // on the hot routing path (System.Random is not thread-safe pre-.NET 6, and
    // even Random.Shared serializes callers internally under heavy fan-out).
    [ThreadStatic]
    private static Random? _threadLocalRandom;

    private static Random ThreadRandom => _threadLocalRandom ??= new Random(Environment.CurrentManagedThreadId ^ Environment.TickCount);

    /// <summary>
    /// Initializes a new instance of <see cref="LoadBasedRouter"/>.
    /// </summary>
    /// <param name="discovery">Discovery service used to resolve the actor pool.</param>
    /// <param name="mailbox">Mailbox service used to query per-actor queue depths.</param>
    /// <param name="dispatcher">Dispatcher used to deliver routed envelopes.</param>
    /// <param name="logger">Optional logger for routing diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required parameter is <c>null</c>.</exception>
    public LoadBasedRouter(
        ActorDiscoveryService discovery,
        MailboxService mailbox,
        MessageDispatcher dispatcher,
        ILogger<LoadBasedRouter>? logger = null)
    {
        _discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        _mailbox = mailbox ?? throw new ArgumentNullException(nameof(mailbox));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _logger = logger;
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

        var actors = GetActors(capability);
        if (actors.Count == 0)
        {
            _logger?.LogWarning(
                "No live actors are registered for capability {Capability}",
                capability);
            return false;
        }

        var target = SelectLeastLoadedFromPool(actors);
        if (target is null)
            return false;

        var queueDepth = _mailbox.GetMailboxSize(target.Id);
        var routed = new Envelope(envelope.Message, target, envelope.Sender);
        var dispatched = await _dispatcher.DispatchAsync(routed);

        if (dispatched)
        {
            _logger?.LogDebug(
                "Routed envelope for capability {Capability} to actor {ActorId} using strategy {Strategy} with current queue depth {QueueDepth}",
                capability,
                target.Id,
                "LeastLoaded",
                queueDepth);
        }
        else
        {
            _logger?.LogWarning(
                "Routing failed because the target mailbox is full for capability {Capability}, actor {ActorId}, strategy {Strategy}, current queue depth {QueueDepth}",
                capability,
                target.Id,
                "LeastLoaded",
                queueDepth);
        }

        return dispatched;
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

        var actors = GetActors(capability);
        if (actors.Count == 0)
        {
            _logger?.LogWarning(
                "No live actors are registered for capability {Capability}",
                capability);
            return false;
        }

        var target = SelectRoundRobinFromPool(capability, actors);
        var queueDepth = _mailbox.GetMailboxSize(target.Id);
        var routed = new Envelope(envelope.Message, target, envelope.Sender);
        var dispatched = await _dispatcher.DispatchAsync(routed);

        if (dispatched)
        {
            _logger?.LogDebug(
                "Routed envelope for capability {Capability} to actor {ActorId} using strategy {Strategy} with current queue depth {QueueDepth}",
                capability,
                target.Id,
                "RoundRobin",
                queueDepth);
        }
        else
        {
            _logger?.LogWarning(
                "Routing failed because the target mailbox is full for capability {Capability}, actor {ActorId}, strategy {Strategy}, current queue depth {QueueDepth}",
                capability,
                target.Id,
                "RoundRobin",
                queueDepth);
        }

        return dispatched;
    }

    /// <summary>
    /// Selects the live actor with the fewest queued messages within the pool
    /// registered under <paramref name="capability"/> by scanning every routee's
    /// mailbox depth.
    /// </summary>
    /// <remarks>
    /// This performs an O(n) scan across the whole routee pool and is intended for
    /// diagnostics and small, static pools (see <see cref="GetLoadSnapshot"/> callers).
    /// The hot routing path (<see cref="RouteAsync"/>) uses <see cref="SampleLeastLoaded"/>
    /// instead, which samples a constant number of routees per decision so routing cost
    /// does not grow with pool size.
    /// </remarks>
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
    /// Selects a lightly-loaded live actor within the pool registered under
    /// <paramref name="capability"/> using the power-of-two-choices heuristic: two
    /// routees are sampled uniformly at random and the one with the smaller mailbox
    /// depth is returned.
    /// </summary>
    /// <remarks>
    /// Reading a mailbox depth (<see cref="MailboxService.GetMailboxSize"/>) is a
    /// lock-free volatile read of the underlying queue's count, and this method only
    /// ever samples two routees regardless of pool size, so routing cost stays
    /// constant instead of scanning the entire pool (as <see cref="GetLeastLoaded"/>
    /// does) on every message. This trades perfect min-load selection for O(1) work
    /// per routing decision, which is the standard power-of-two-choices trade-off and
    /// yields load imbalance bounded by O(log log n) rather than the O(n) contention
    /// risk of a full scan under high fan-in.
    /// </remarks>
    /// <param name="capability">The capability identifier that selects the actor pool.</param>
    /// <returns>
    /// The less-loaded of two randomly sampled <see cref="ActorRef"/> instances, the
    /// sole routee when the pool has exactly one, or <c>null</c> when no live actors
    /// are registered for the capability.
    /// </returns>
    public ActorRef? SampleLeastLoaded(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return null;

        var actors = _discovery.Discover(capability);
        var count = actors.Count;
        if (count == 0)
            return null;
        if (count == 1)
            return actors[0];

        var random = ThreadRandom;
        var firstIndex = random.Next(count);
        var secondIndex = random.Next(count - 1);
        if (secondIndex >= firstIndex)
            secondIndex++;

        var first = actors[firstIndex];
        var second = actors[secondIndex];

        var firstLoad = _mailbox.GetMailboxSize(first.Id);
        var secondLoad = _mailbox.GetMailboxSize(second.Id);

        return secondLoad < firstLoad ? second : first;
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

    /// <summary>
    /// Gets the actor pool for the specified capability.
    /// </summary>
    /// <param name="capability">The capability identifier.</param>
    /// <returns>The actor pool for the capability.</returns>
    private IReadOnlyList<ActorRef> GetActors(string capability)
    {
        return _discovery.Discover(capability);
    }

    /// <summary>
    /// Selects the least-loaded actor from the specified pool using the power-of-two-choices heuristic.
    /// </summary>
    /// <param name="pool">The actor pool to select from.</param>
    /// <returns>The selected actor, or null if the pool is empty.</returns>
    private ActorRef? SelectLeastLoadedFromPool(IReadOnlyList<ActorRef> pool)
    {
        var count = pool.Count;
        if (count == 0)
            return null;
        if (count == 1)
            return pool[0];

        var random = ThreadRandom;
        var firstIndex = random.Next(count);
        var secondIndex = random.Next(count - 1);
        if (secondIndex >= firstIndex)
            secondIndex++;

        var first = pool[firstIndex];
        var second = pool[secondIndex];

        var firstLoad = _mailbox.GetMailboxSize(first.Id);
        var secondLoad = _mailbox.GetMailboxSize(second.Id);

        return secondLoad < firstLoad ? second : first;
    }

    /// <summary>
    /// Selects the next actor in a round-robin sequence from the specified pool.
    /// </summary>
    /// <param name="capability">The capability identifier used for the round-robin counter.</param>
    /// <param name="pool">The actor pool to select from.</param>
    /// <returns>The selected actor.</returns>
    private ActorRef SelectRoundRobinFromPool(string capability, IReadOnlyList<ActorRef> pool)
    {
        // Atomically advance the counter; cap at int.MaxValue - 1 to avoid negative overflow.
        var index = _roundRobinCounters.AddOrUpdate(
            capability,
            _ => 0,
            (_, prev) => prev >= int.MaxValue - 1 ? 0 : prev + 1);

        return pool[index % pool.Count];
    }
}
