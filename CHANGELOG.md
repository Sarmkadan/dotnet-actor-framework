# Changelog

All notable changes to the DotNet Actor Framework project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2026-05-04

### Added
- Clustering support for multi-node deployments
- Remote actor reference resolution with transparent message routing
- Gossip protocol for node discovery and health monitoring
- Event sourcing pattern support with message replay capabilities
- Message batching utilities in `MessageBatcher` class
- Comprehensive monitoring dashboard endpoints
- Kubernetes deployment examples and manifests
- StatefulSet configuration for distributed deployments
- Health check middleware integration
- Circuit breaker pattern support via custom middleware

### Changed
- Refactored `ActorRegistry` for improved hierarchical lookups
- Optimized mailbox implementation with better capacity management
- Improved supervision service with more granular retry configuration
- Enhanced metrics collection with P95/P99 latency percentiles
- Updated dependencies to latest stable versions

### Fixed
- Fixed race condition in actor state persistence
- Corrected supervision strategy escalation logic
- Resolved memory leak in metrics collection during high throughput
- Fixed actor path parsing for edge cases with special characters

### Performance
- 30% improvement in message routing throughput
- Reduced memory overhead per actor from 3KB to 2KB
- Lock-free implementation for mailbox operations

## [1.1.0] - 2026-03-15

### Added
- Message persistence with event log storage
- Actor state snapshot functionality
- Database abstraction layer supporting PostgreSQL, SQL Server, MySQL
- Middleware pipeline architecture for extensibility
- Built-in middleware: Logging, Metrics, Rate Limiting, Authentication
- Comprehensive metrics collection and aggregation
- Health summary reporting with error rates
- Actor lifecycle hooks: `OnInitializeAsync()`, `OnStopAsync()`, `OnErrorAsync()`
- Configuration presets: Default, HighPerformance, Reliable, Cluster
- Docker and Docker Compose support
- Complete API documentation

### Changed
- Redesigned `Message` base class with correlation IDs
- Improved `ActorSystemConfiguration` with more options
- Enhanced error handling with specific exception types
- Refactored `MessageDispatcher` for better extensibility

### Fixed
- Fixed memory usage in concurrent message processing
- Corrected actor termination order in supervision
- Resolved issue with actor path resolution in hierarchies

### Performance
- 50% improvement in message dispatch latency
- More efficient actor registry lookup algorithm

## [1.0.0] - 2026-01-10

### Added
- Core actor model implementation with mailbox-based message processing
- Actor lifecycle management (Created, Initializing, Started, Suspended, Stopping, Terminated, Error)
- Supervision strategies: Restart, Stop, Resume, Escalate, Backoff
- Actor registry with hierarchical path structure
- Message dispatcher with async/await support
- Built-in message types: ControlMessage, ResponseMessage, FailureMessage
- Actor metrics collection and reporting
- Dependency injection integration with Microsoft.Extensions.DependencyInjection
- Actor path abstraction with parent-child relationships
- Actor references with immutable identity
- Async message processing with CancellationToken support
- Exception handling and error reporting
- Comprehensive exception hierarchy (ActorException, ActorNotFoundException, etc.)
- Unit test utilities with `MockActorContext`
- Serialization support for messages with JSON format
- Rate limiting and authentication middleware hooks
- CLI support for actor system management
- Background worker infrastructure
- System health monitoring and diagnostics
- Caching layer for frequently accessed actors
- Event bus for pub/sub messaging
- Documentation and examples

### Performance Characteristics
- Lock-free message queue using ConcurrentQueue
- O(1) actor path-based lookup
- Minimal memory overhead: ~2KB per actor
- Support for 10,000+ concurrent actors on modest hardware
- Throughput: 10k-100k messages/second depending on configuration

## [0.5.0] - 2025-11-20

### Added
- Initial preview release with core functionality
- Basic actor model with sequential message processing
- Supervision with configurable strategies
- Message routing and delivery
- Actor lifecycle management

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
- Machine learning integration examples
- Real-time analytics example
