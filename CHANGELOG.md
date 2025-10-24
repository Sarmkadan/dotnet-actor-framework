# Changelog

All notable changes to the DotNet Actor Framework project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Distributed tracing integration (OpenTelemetry)
- gRPC support for remote actors
- WebSocket actor support
- Custom serialization plugins
- Actor hot-reloading
- Persistence plugins for additional databases
- Performance optimization for 1M+ actors
- Integration with .NET libraries (Polly, MediatR)
- Actor replay and debugging tools

## [1.0.0] - 2025-11-03

### Added
- Clustering support for multi-node deployments with gossip-based node discovery
- Remote actor reference resolution with transparent message routing
- Event sourcing pattern support with message replay capabilities
- Message batching utilities in `MessageBatcher` class
- Kubernetes deployment manifests and Docker Compose examples
- Health check middleware integration
- Circuit breaker pattern support via custom middleware
- Complete API documentation and cross-references

### Changed
- Refactored `ActorRegistry` for improved hierarchical lookups
- Optimized mailbox with lock-free `ConcurrentQueue` implementation
- Improved supervision service with more granular retry and backoff configuration
- Enhanced metrics collection with P95/P99 latency percentiles
- Stabilized public API surface for 1.0 release

### Fixed
- Fixed race condition in actor state persistence under concurrent writes
- Corrected supervision strategy escalation logic for nested hierarchies
- Resolved memory leak in metrics collection at high message throughput
- Fixed actor path parsing for paths containing special characters

### Performance
- 30% improvement in message routing throughput vs 0.9.0
- Reduced steady-state memory overhead per actor from 3 KB to 2 KB
- Lock-free mailbox enqueue path eliminates contention at high concurrency

## [0.9.0] - 2025-09-22

### Added
- Load-based message routing via `LoadBasedRouter`
- Actor routing extension methods (`ActorRoutingExtensions`)
- `ActorSystemDiagnostics` for runtime inspection
- CLI handler for actor system management commands
- NuGet packaging configuration and release pipeline

### Changed
- Hardened error handling across middleware pipeline
- Improved `SupervisionService` with configurable restart limits
- Refined `ActorSystemBuilder` fluent API for cleaner configuration

### Fixed
- Actor termination could leave orphaned mailbox entries
- Backoff timer not reset correctly after successful actor restart
- `MockActorContext` missing cancellation token propagation

## [0.8.0] - 2025-08-11

### Added
- HTTP actor client (`HttpActorClient`) for remote invocation over HTTP
- Webhook dispatcher (`WebhookDispatcher`) for outbound event notifications
- Integration event publisher (`IntegrationEventPublisher`)
- External service client abstraction (`ExternalServiceClient`)
- Remote actor invoker (`RemoteActorInvoker`) for cross-node calls
- Serialization extensions and `EnvelopeExtensions` helpers

### Changed
- `MessageSerializer` now supports pluggable format selection
- `HealthCheckFormatter` standardized to ASP.NET Core health check schema
- Connection manager refactored for pooled database access

### Fixed
- HTTP client timeout not propagated to actor message deadline
- Integration event publisher swallowing serialization exceptions

## [0.7.0] - 2025-07-07

### Added
- Actor state persistence with snapshotting (`ActorStatePersistence`)
- Message persistence repository with durable event log (`MessagePersistenceRepository`)
- Actor metrics repository for aggregated performance data (`ActorMetricsRepository`)
- Actor state repository for snapshot storage (`ActorStateRepository`)
- Database initialization script (`scripts/init-db.sql`)
- `ConnectionManager` for database connection lifecycle management

### Changed
- `ActorSystemOptions` extended with persistence and connection string settings
- Configuration presets `AddActorFrameworkReliable` wired to persistence layer
- `DependencyInjectionSetup` registers repository services conditionally

### Fixed
- Snapshot restore failed silently when actor path contained uppercase letters
- Message log could grow unbounded without compaction trigger

## [0.6.0] - 2025-06-02

### Added
- Background worker infrastructure (`BackgroundWorkerService`)
- Metrics collector background worker (`MetricsCollectorWorker`)
- Actor management REST API endpoints (`ActorManagementApi`)
- System metrics API (`SystemMetricsApi`)
- Actor discovery service (`ActorDiscoveryService`)
- In-memory actor cache service (`ActorCacheService`)

### Changed
- `ActorRegistry` emits events through `EventBus` on actor lifecycle transitions
- `MessageDispatcher` exposes per-actor latency histogram
- Health summary now includes cache hit rate

### Fixed
- Background worker failed to restart after unhandled exception in processing loop
- Actor discovery service not deregistering terminated actors

## [0.5.0] - 2025-04-28

### Added
- Middleware pipeline architecture for cross-cutting concerns (`IActorMiddleware`)
- Built-in middleware: `LoggingMiddleware`, `MetricsCollectionMiddleware`
- Built-in middleware: `AuthenticationMiddleware`, `RateLimitingMiddleware`, `ErrorHandlingMiddleware`
- Event bus for pub/sub messaging between actors (`EventBus`)
- `ActorSystemConfiguration` with fluent initialization API
- Configuration presets: `AddActorFramework`, `AddActorFrameworkHighPerformance`

### Changed
- `MessageDispatcher` now executes configured middleware chain before delivery
- `ActorSystemOptions` extended with middleware toggle flags
- `DependencyInjectionSetup` registers all built-in middleware in correct order

### Fixed
- Rate limiting middleware counted messages from terminated actors
- Logging middleware logged duplicate entries when pipeline was re-entered

## [0.4.0] - 2025-03-24

### Added
- Comprehensive actor metrics model (`ActorMetrics`) with counters and latency tracking
- Guard extension utilities (`GuardExtensions`) for input validation
- Message extension helpers (`MessageExtensions`, `SerializationExtensions`)
- `DateTimeExtensions` for timestamp formatting
- `ConcurrentCollectionExtensions` for thread-safe collection helpers
- `MessageBatcher` utility for accumulating and flushing message batches
- Unit test utilities: `MockActorContext` for isolated actor testing

### Changed
- `ActorRef` is now a value type with identity-based equality
- `Envelope` carries priority alongside the wrapped message
- `MessagePriority` enum added for mailbox ordering hints

### Fixed
- `ActorPath` equality check was case-sensitive on segment comparison
- Concurrent actor creation could assign duplicate internal IDs

## [0.3.0] - 2025-03-03

### Added
- Supervision strategies: `Restart`, `Stop`, `Resume`, `Escalate`, `Backoff`
- `SupervisionService` with configurable per-actor strategy
- Actor lifecycle hooks: `OnInitializeAsync()`, `OnStopAsync()`, `OnErrorAsync()`
- `ActorState` enum: Created, Initializing, Started, Suspended, Stopping, Terminated, Error
- `SupervisionStrategy` enum with exponential backoff support
- `ActorException` hierarchy: `ActorNotFoundException`, `ActorTerminatedException`, `MailboxFullException`

### Changed
- `Actor.ReceiveAsync` made abstract; default implementation removed
- `ActorSystem.ShutdownAsync` waits for in-flight messages before stopping

### Fixed
- Actor restart did not clear error state before re-entering `OnInitializeAsync`
- Escalation incorrectly propagated to non-supervisor parents

## [0.2.0] - 2025-02-10

### Added
- `MailboxService` with FIFO per-actor message queues
- `MessageDispatcher` with async delivery and cancellation support
- `ActorRegistry` with hierarchical path-based lookup
- `ActorPath` abstraction with parent-child segment parsing
- `ActorPathExtensions` for path manipulation helpers
- Dependency injection setup via `Microsoft.Extensions.DependencyInjection`
- `ActorSystemOptions` for centralized configuration
- Built-in message types: `ControlMessage`, `ResponseMessage`, `FailureMessage`
- JSON serialization support via `MessageSerializer`
- Constants: `ActorConstants`, `MessageConstants`

### Changed
- `Actor` base class split from `ActorSystem`; actors are created through the registry
- `ActorSystem` manages lifecycle only; dispatch decoupled to `MessageDispatcher`

### Fixed
- Mailbox enqueue blocked indefinitely when actor was in Stopping state
- Actor lookup returned stale reference after termination

## [0.1.0] - 2025-01-20

### Added
- Initial release with core actor model implementation
- `Actor` base class with sequential, mailbox-based message processing
- `ActorSystem` for lifecycle coordination
- `ActorRef` for location-transparent actor references
- `Message` abstract record base with correlation ID and sender fields
- Basic actor creation and termination
- Async/await throughout with `CancellationToken` support
- MIT license
