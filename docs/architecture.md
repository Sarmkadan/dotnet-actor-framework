# Architecture

This document describes the actual structure of the codebase - what exists in
`src/DotNetActorFramework`, how the pieces talk to each other, and why some
decisions were made the way they were. If something is not in here, it is
probably not in the code either.

## Overview

DotNetActorFramework is a single-project actor library (`src/DotNetActorFramework`)
plus tests (`tests/dotnet-actor-framework.Tests`), BenchmarkDotNet benchmarks
(`benchmarks/`) and runnable examples (`examples/`). `Program.cs` in the library
project doubles as a demo entry point showing the intended wiring.

The core idea: actors are addressable units (`ActorPath` like
`/user/orders/processor`) that receive messages through per-actor mailboxes.
Delivery, supervision, persistence and cross-cutting concerns are separate
services composed via `Microsoft.Extensions.DependencyInjection`.

```
caller
  └─ MessageDispatcher.SendAsync(recipient, message)
       └─ wraps in Envelope, retries with backoff
            └─ MailboxService.EnqueueAsync(actorId, envelope)   (FIFO or Priority mailbox)
                 └─ consumer pulls: DequeueAsync / MessageDispatcher.GetNextMessageAsync
                      └─ Actor.ReceiveAsync -> OnReceiveAsync (your code)
                           └─ on exception: SupervisionService.HandleFailureAsync
```

## Module breakdown

| Directory | What lives there |
|---|---|
| `Models/` | `Actor`, `ActorRef`, `ActorPath`, `ActorSystem`, `Envelope`, `Message` (+ `ControlMessage` etc.), `ActorMetrics` |
| `Services/` | `ActorRegistry`, `MailboxService` (with `Mailbox` / `PriorityMailbox`), `MessageDispatcher`, `SupervisionService`, `ActorDiscoveryService`, `ClusterActorRegistry` |
| `Middleware/` | `IActorMiddleware`, `MiddlewarePipeline`, built-ins: logging, authentication, rate limiting, metrics collection, error handling |
| `Configuration/` | `ActorSystemOptions` (validated presets), `DependencyInjectionSetup` (`AddActorFramework` and friends), `ActorSystemConfiguration` (init coordinator), `ActorSystemBuilder` (fluent, DI-free alternative) |
| `Persistence/` | `PersistenceService` facade over `ISnapshotStore` + `IEventJournal` abstractions; only the in-memory implementations exist today |
| `Repository/` | `ConnectionManager`, `ActorStateRepository`, `MessagePersistenceRepository`, `ActorMetricsRepository` |
| `Events/` | `EventBus` - in-process pub/sub for `IDomainEvent` |
| `Integration/` | `HttpActorClient`, `HttpRemoteActorInvoker` (+ circuit breaker), `WebhookDispatcher`, `IntegrationEventPublisher`, `ExternalServiceClient` |
| `Routing/` | `LoadBasedRouter` and routing extensions |
| `BackgroundWorkers/` | `BackgroundWorkerService`, `MetricsCollectorWorker` |
| `Api/` | `ActorManagementApi`, `SystemMetricsApi` - programmatic management/metrics surface |
| `Diagnostics/`, `Caching/`, `Cli/`, `Testing/`, `Utilities/` | diagnostics snapshots, `ActorCacheService`, CLI handler, `MockActorContext` for tests, misc helpers (`MessageBatcher`, guard/serialization extensions) |

## Core components

### Actor

`Models/Actor.cs`. A concrete class (not abstract) with virtual hooks:
`OnInitializeAsync`, `OnReceiveAsync`, `OnErrorAsync`, `OnStopAsync`. Lifecycle
is a small state machine: `Created -> Initializing -> Started -> Stopping ->
Terminated`, with `Error` reachable from processing failures. State transitions
are guarded (`InitializeAsync` throws if the actor is not `Created`).

Each actor owns a private `Dictionary<string, object>` state bag behind a lock,
an `ActorMetrics` instance, and an optional `Supervisor` reference.

**Decision: concrete base class with virtual hooks instead of an abstract
`ReceiveAsync`.** This lets the framework instantiate plain `Actor` objects
(e.g. `ActorSystem.CreateActorAsync` does exactly that) and keeps the demo/test
surface simple. Trade-off: nothing forces a subclass to actually handle
messages, and `ActorSystem.CreateActorAsync` can only create base `Actor`
instances - custom actor types must be constructed by the caller.

### ActorSystem

`Models/ActorSystem.cs`. Root coordinator: creates/terminates actors, indexes
them by path (`Dictionary<ActorPath, Guid>` + `Dictionary<Guid, Actor>` behind
a single lock), aggregates health (`GetHealthSummary`) and shuts everything
down gracefully (`ShutdownAsync` terminates each actor, tolerating individual
failures).

**Decision: plain `Dictionary` + `lock` instead of `ConcurrentDictionary`.**
Creation/termination are rare compared to message sends, and the compound
invariants (path index and id index must move together, double-check after
async `InitializeAsync`) are easier to keep correct under one lock than with
two lock-free maps. Message throughput never touches these locks - hot-path
lookups happen in `MailboxService`, which *is* a `ConcurrentDictionary`.

### ActorRegistry

`Services/ActorRegistry.cs`. Registry used by the DI-composed stack
(`MessageDispatcher`, `SupervisionService`): path index, id index, and a
parent -> children hierarchy index, all behind one lock. This is deliberately
separate from `ActorSystem`'s internal bookkeeping - see "Known limitations".

### MailboxService, Mailbox, PriorityMailbox

`Services/MailboxService.cs`. `ConcurrentDictionary<Guid, IMailbox>` keyed by
actor id. `CreateMailbox` picks the implementation from
`ActorSystemOptions.DefaultMailboxType` (or per-mailbox override):

- `Mailbox` - FIFO, `ConcurrentQueue` + `SemaphoreSlim` for capacity.
- `PriorityMailbox` - one queue per `MessagePriority`, high drains first.

Capacity is enforced at enqueue; a full mailbox throws `MailboxException`,
which is what triggers the dispatcher's retry path.

**Decision: bounded mailboxes by default.** Unbounded queues turn a slow actor
into an OOM. The trade-off is that senders see backpressure as exceptions and
retries rather than an await-until-there-is-room API.

### MessageDispatcher

`Services/MessageDispatcher.cs`. Wraps a `Message` in an `Envelope`
(message + recipient + optional sender + retry count), checks recipient
existence against `ActorRegistry`, then enqueues with retry + exponential
backoff (`ActorConstants.MaxMessageRetries`, `InitialBackoffDelayMs`,
`BackoffMultiplier`, capped at `MaxBackoffDelayMs`). Messages that exhaust
retries or target unknown actors go to an in-memory dead-letter queue, capped
at 10,000 entries (oldest dropped first). Also provides `BroadcastAsync`,
`PublishControlAsync` and `GetStatistics()` (delivered/failed/dead-letter
counts).

**Decision: at-most-once delivery with a bounded, in-memory DLQ.** The DLQ is
a debugging aid, not a durability mechanism - it does not survive process
restart. Durable delivery is the job of the persistence layer, when enabled.

**Decision: pull-based consumption.** The dispatcher enqueues; it does not run
a scheduler thread that pushes envelopes into `Actor.ReceiveAsync`. Consumers
call `MessageDispatcher.GetNextMessageAsync(actorId)` (or
`MailboxService.DequeueAsync`) and invoke the actor themselves - the examples
and tests show this loop. Keeps the framework free of thread-pool policy, at
the cost of making the host responsible for the processing loop.

### SupervisionService

`Services/SupervisionService.cs`. `HandleFailureAsync(actor, exception,
strategy)` implements the `SupervisionStrategy` enum: `Restart`, `Stop`,
`Resume`, `Escalate`, `Backoff`. Restart counts are tracked per actor in a
`SupervisionContext`; more than 5 restarts escalates instead. Recovery is
signalled by sending `ControlMessage`s (restart/stop commands from
`MessageConstants`) through the normal dispatcher rather than by mutating the
actor directly.

**Decision: strategies as an enum + service, not pluggable strategy objects.**
Five strategies cover the practical cases and keep failure handling in one
readable switch. The extension point, if ever needed, is wrapping or replacing
the service in DI - not subclassing a strategy hierarchy.

### Middleware

`Middleware/IActorMiddleware.cs` defines
`Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)` plus an
`Order` default interface property; `MiddlewarePipeline` sorts by `Order`
(stable for ties) and executes around a final handler. Returning `false`
without calling `next` short-circuits (used by rate limiting / auth rejects).
Built-ins: `LoggingMiddleware`, `AuthenticationMiddleware`,
`RateLimitingMiddleware`, `MetricsCollectionMiddleware`,
`ErrorHandlingMiddleware`.

The pipeline is assembled by `ActorSystemBuilder.BuildMiddlewarePipeline()`
and exercised in `MiddlewarePipelineTests`; it is **not** automatically invoked
inside `MessageDispatcher.DispatchAsync` - the host composes it around its
processing loop. See "Known limitations".

### Persistence

Two layers, on purpose:

- `Persistence/Abstractions`: `ISnapshotStore`, `IEventJournal` - the
  swap-out point for storage backends. `PersistenceService` is a facade over
  both (snapshots + append/read/delete of `ActorEvent`s by sequence number,
  i.e. event-sourcing-shaped).
- `Repository/`: `ActorStateRepository`, `MessagePersistenceRepository`,
  `ActorMetricsRepository`, coordinated by `ConnectionManager` when a
  `DatabaseConnectionString` is configured.

`DependencyInjectionSetup.RegisterPersistenceServices` selects the backend from
`ActorSystemOptions.DefaultPersistenceBackend`. Only
`PersistenceBackend.InMemory` (`InMemorySnapshotStore`, `InMemoryEventJournal`)
is implemented; `File`, `LiteDb` and `PostgreSql` currently throw
`NotImplementedException` at registration time - a deliberate fail-fast so a
misconfigured system dies at startup, not on first snapshot.

### Configuration and composition

Two composition styles coexist:

1. **DI-first** (`DependencyInjectionSetup.AddActorFramework`): registers
   everything as singletons, with opinionated presets
   `AddActorFrameworkHighPerformance` (big mailboxes, persistence and metrics
   off), `AddActorFrameworkReliable` (small mailboxes, snapshotting every 60s,
   more retries), `AddActorFrameworkCluster` (adds `ClusterActorRegistry`).
   `ActorSystemConfiguration` is the init coordinator: validates options,
   optionally initializes/validates the DB connection, creates the
   `ActorSystem`, and exposes `CreateActorAsync`/health/statistics on top of
   the underlying services.
2. **Fluent builder** (`ActorSystemBuilder`): `WithLogging()`,
   `WithRateLimiting()`, `WithMetrics()`, `WithEventBus()`,
   `AddBackgroundWorker()` etc., for hosts that do not want a DI container.
   It builds the `ActorSystem`, the middleware pipeline and the background
   worker service as separate artifacts.

`ActorSystemOptions.Validate()` runs both at registration and inside
`ActorSystemConfiguration`, so invalid option combinations fail early
regardless of which path composed them.

### Everything else, briefly

- **EventBus** (`Events/`): typed in-process pub/sub over `IDomainEvent`,
  delegate-based subscribe/unsubscribe. Decoupled from the mailbox path - it
  is for domain notifications, not actor messaging.
- **Integration** (`Integration/`): `HttpRemoteActorInvoker` implements
  `IRemoteActorInvoker` over HTTP with a `RemoteActorCircuitBreaker`;
  `WebhookDispatcher` pushes events out; `HttpActorClient` is the inbound
  counterpart. This is point-to-point HTTP integration, not cluster transport.
- **ClusterActorRegistry** (`Services/`): tracks which node address hosts
  which actor. It is a directory only - there is no gossip, failure detection
  or transparent remote delivery behind it.
- **Routing** (`Routing/LoadBasedRouter.cs`): picks a recipient from a pool
  based on load; used with plain `SendAsync`.
- **BackgroundWorkers**: `BackgroundWorkerService` hosts `IBackgroundWorker`
  implementations; `MetricsCollectorWorker` periodically snapshots metrics
  into `ActorMetricsRepository`.
- **Api**: `ActorManagementApi` / `SystemMetricsApi` are programmatic facades
  (suitable to hang an HTTP layer on), not ASP.NET controllers.
- **Testing**: `MockActorContext` for unit-testing actors without a running
  system.

## Concurrency model

- One mailbox per actor; a single consumer draining a mailbox gives
  per-actor sequential processing and FIFO ordering (priority mailboxes trade
  FIFO across priorities for it within a priority level).
- No ordering guarantees between different actors.
- Shared structures: `MailboxService` uses `ConcurrentDictionary` +
  `ConcurrentQueue` (hot path, lock-free); `ActorSystem`/`ActorRegistry` use
  coarse locks (cold path, compound invariants); counters in
  `MessageDispatcher` and `ActorMetrics` are lock- or `Interlocked`-protected.

## Extension points

- `IActorMiddleware` + `Order` - cross-cutting message concerns.
- `ISnapshotStore` / `IEventJournal` - alternative persistence backends
  (add an enum member and a registration branch in
  `RegisterPersistenceServices`).
- `IRemoteActorInvoker` - alternative remote transport.
- `IBackgroundWorker` - periodic/system tasks hosted by
  `BackgroundWorkerService`.
- `IMailbox` - custom mailbox semantics (the `MailboxType` switch in
  `MailboxService.CreateMailbox` is where a new type plugs in).
- Subclass `Actor` and override the `On*Async` hooks for behavior.

## Known limitations (honest list)

1. **The middleware pipeline is not wired into the dispatch path.**
   `MessageDispatcher` enqueues envelopes directly; `MiddlewarePipeline` only
   runs if the host composes it (via `ActorSystemBuilder` or manually) around
   its own processing loop. Documented pipeline diagrams that show middleware
   between `SendAsync` and the mailbox describe an integration the host has to
   do, not something the dispatcher does for you.
2. **Two sources of actor truth.** `ActorSystem` keeps its own path/id maps,
   and `ActorRegistry` keeps another set (plus hierarchy index). They are
   consistent only if the same code path updates both
   (`ActorSystemConfiguration` does; direct use of `ActorSystem` alone does
   not populate the registry the dispatcher checks).
3. **No built-in processing loop.** Nothing automatically pumps mailboxes into
   `Actor.ReceiveAsync`; the host owns that loop.
4. **Persistence backends beyond in-memory are stubs** that throw
   `NotImplementedException` at registration.
5. **Dead letters are in-memory and capped** (10,000); they are lost on
   restart.
6. **Cluster support is a directory, not a runtime.** No transport, failure
   detection, or remote deployment; `HttpRemoteActorInvoker` is explicit HTTP,
   not location transparency.
7. **Supervision restart cap (5) is hardcoded** in `SupervisionService`, not
   an option.

These are acceptable for the current scope (in-process actor coordination with
observability); items 1-3 are the first candidates if the framework grows a
hosted runtime.
