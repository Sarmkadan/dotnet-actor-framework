# LoadBasedRouter

`LoadBasedRouter` delivers an `Envelope` to one live actor from a pool of actors registered under a capability. It is defined in `src/DotNetActorFramework/Routing/LoadBasedRouter.cs` (namespace `DotNetActorFramework.Routing`) and implements `IRouter`.

The router offers two selection strategies, plus read-only helpers for inspecting load:

- **Least-loaded (power-of-two-choices)** — the default `IRouter.RouteAsync` path. Samples two routees at random and sends to the one with the shorter mailbox.
- **Round-robin** — `RouteRoundRobinAsync`. Cycles through the pool in order, ignoring load.
- **Full-scan least-loaded** — `GetLeastLoaded`. Checks every routee and returns the global minimum. Diagnostic use only; it is O(n) in pool size.

## Dependencies

| Dependency | Role |
|------------|------|
| `ActorDiscoveryService` | Resolves the live actor pool for a capability via `Discover(capability)`. |
| `MailboxService` | Reads per-actor queue depth via `GetMailboxSize(actorId)`. |
| `MessageDispatcher` | Delivers the routed envelope via `DispatchAsync(envelope)`. |
| `ILogger<LoadBasedRouter>` | Optional. Routing diagnostics only. |

## Routing algorithms

### Least-loaded (`RouteAsync`)

1. Look up the live pool with `Discover(capability)`. Capability matching is case-insensitive.
2. If the pool is empty, log a warning and return `false`.
3. Pick two distinct indices uniformly at random. If the pool has one routee, that routee is chosen directly.
4. Read both mailbox depths. The routee with the strictly smaller depth wins. On a tie the first sample is kept.
5. Create a new `Envelope` addressed to the chosen routee, keeping the original `Message` and `Sender`, and call `DispatchAsync`.

Cost per decision is constant (two depth reads), regardless of pool size. The trade-off is that the result is not guaranteed to be the global minimum. Power-of-two-choices keeps the maximum imbalance small in practice, and it avoids the O(n) scan that `GetLeastLoaded` performs on every message.

Queue depth is read before dispatch, so it is a snapshot. Concurrent senders can read the same depth and pick the same routee. This is accepted by design.

### Round-robin (`RouteRoundRobinAsync`)

1. Look up the live pool with `Discover(capability)`. If it is empty, log a warning and return `false`.
2. Advance a per-capability counter and select `pool[counter % pool.Count]`.
3. Dispatch the new `Envelope` to that routee.

Each capability has its own counter, keyed case-insensitively. The counter starts at `0` and resets to `0` once it reaches `int.MaxValue - 1`. Advancing is atomic (`ConcurrentDictionary.AddOrUpdate`), so concurrent callers never receive the same slot twice in a row. The router does not consider mailbox depth at all.

If the pool's membership changes between calls, the index is taken modulo the new size. The sequence therefore continues from the same counter value but over a different routee list. Routees added or removed mid-stream can shift which actor receives the next message.

Use round-robin when messages have similar processing cost. Use least-loaded when routee speed or backlog varies.

### Thread safety

- The round-robin counter is a `ConcurrentDictionary<string, int>`.
- Random selection uses a `[ThreadStatic]` `System.Random`, one per thread. This avoids a shared-RNG lock on the hot path.
- The router holds no other mutable state, so a single instance can be shared as a singleton.

## Public API

### Constructor

```csharp
public LoadBasedRouter(
    ActorDiscoveryService discovery,
    MailboxService mailbox,
    MessageDispatcher dispatcher,
    ILogger<LoadBasedRouter>? logger = null)
```

- `discovery`, `mailbox`, `dispatcher` — required. Passing `null` throws `ArgumentNullException`.
- `logger` — optional.

### `Task<bool> RouteAsync(string capability, Envelope envelope, CancellationToken cancellationToken = default)`

Implements `IRouter.RouteAsync`. Routes using the least-loaded strategy.

- **Returns** `true` if the envelope was accepted for delivery. Returns `false` if no live actors are registered for the capability, or if the target mailbox is full.
- **Throws** `ArgumentException` if `capability` is null, empty or whitespace. `ArgumentNullException` if `envelope` is null. `OperationCanceledException` if `cancellationToken` is already cancelled when the call starts.
- Exceptions raised by `MessageDispatcher.DispatchAsync` are not caught. These include `ActorNotFoundException` and `MailboxException`.
- The token is checked once, before dispatch. Cancellation does not interrupt an in-flight dispatch.

### `Task<bool> RouteRoundRobinAsync(string capability, Envelope envelope, CancellationToken cancellationToken = default)`

Routes using the round-robin strategy. Same parameters, return semantics and exceptions as `RouteAsync`.

Unlike `RouteAsync`, this method does not use queue depth to choose a routee (it reads depth only for logging, after the choice is made). It is not part of `IRouter`, so callers must hold a `LoadBasedRouter` reference to use it.

### `ActorRef? GetLeastLoaded(string capability)`

Scans every live routee in the pool and returns the one with the smallest mailbox depth.

- Returns `null` if `capability` is blank or no live actors are registered.
- Ties go to the first routee in discovery order.
- O(n) per call. Intended for diagnostics and small pools, not for the per-message path.

### `ActorRef? SampleLeastLoaded(string capability)`

Power-of-two-choices selection without dispatching anything. This is the same selection that `RouteAsync` uses.

- Returns the sole routee when the pool has exactly one member, and `null` when it has none or when `capability` is blank.
- Useful for testing selection behaviour or for callers that dispatch through their own path.

### `IReadOnlyDictionary<string, int> GetLoadSnapshot(string capability)`

Returns the current mailbox depth of every live routee, keyed by `ActorPath.Name`.

- Returns an empty dictionary if `capability` is blank or no live actors are registered.
- The snapshot is point-in-time. Depths can change immediately after it is returned.
- Two routees with the same `Path.Name` would overwrite each other in the result. Actor names should be unique within a pool.

## Usage

### With dependency injection

`AddActorDiscoveryWithRouting()` registers `ActorDiscoveryService` and `LoadBasedRouter` as singletons. It requires that `AddActorFramework()` has already registered `MailboxService` and `MessageDispatcher`.

```csharp
using DotNetActorFramework.Routing;
using DotNetActorFramework.Services;
using DotNetActorFramework.Models;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddActorFramework()
        .AddActorDiscoveryWithRouting();

var provider = services.BuildServiceProvider();

// Register live worker actors under a capability. Actors are created through
// the ActorSystem your application already runs.
var discovery = provider.GetRequiredService<ActorDiscoveryService>();
discovery.Register(workerA, ["image-resize"]);
discovery.Register(workerB, ["image-resize"]);

var router = provider.GetRequiredService<LoadBasedRouter>();

// Least-loaded delivery.
var envelope = new Envelope(new ControlMessage("resize"), workerA);
bool accepted = await router.RouteAsync("image-resize", envelope);

// Round-robin delivery over the same pool.
bool next = await router.RouteRoundRobinAsync("image-resize", envelope);

// Inspect current load.
IReadOnlyDictionary<string, int> depths = router.GetLoadSnapshot("image-resize");
```

The `Envelope` recipient is overwritten by the router. Its initial value is only required by the constructor.

### Manual construction

```csharp
var options = new ActorSystemOptions();
var mailbox = new MailboxService(options);
var discovery = new ActorDiscoveryService();
var registry = new ActorRegistry();
var system = new ActorSystem("worker-system");
var dispatcher = new MessageDispatcher(mailbox, registry, system);

var router = new LoadBasedRouter(discovery, mailbox, dispatcher);
```

### Handling the result

```csharp
if (!await router.RouteAsync("image-resize", envelope))
{
    // No live routees for the capability, or the chosen mailbox is full.
    // Decide whether to retry, queue elsewhere, or report back to the caller.
}
```

A `false` result does not say which of the two causes applied. The logs distinguish them: "No live actors are registered" versus "target mailbox is full".

## Logging

When `logger` is supplied:

| Level | Condition |
|-------|-----------|
| Warning | No live actors registered for the capability. |
| Debug | Envelope accepted. Includes capability, actor ID, strategy (`LeastLoaded` or `RoundRobin`) and queue depth. |
| Warning | Dispatch returned `false`. Includes capability, actor ID, strategy and queue depth. |

## Related

- `IRouter` — common interface, implemented by `LoadBasedRouter` and `RoundRobinRouter`.
- `RoundRobinRouter` — a standalone round-robin router that does not depend on `MailboxService`.
- `ActorDiscoveryService` — registers actors under capabilities and tags.
- `ActorRoutingExtensions` — DI registration helpers.
- `benchmarks/DotNetActorFramework.Benchmarks/LoadBasedRouterBenchmarks.cs` — compares `GetLeastLoaded` with `SampleLeastLoaded`.
