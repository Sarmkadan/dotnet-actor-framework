# ActorSystemBenchmarks

A utility class designed to measure and report performance characteristics of an actor system within the `dotnet-actor-framework`. It provides methods to initialize benchmarking infrastructure, create actors under test, retrieve actor references, and collect system health metrics for analysis.

## API

### `void Setup()`
Initializes the benchmarking environment. This method must be called before any other operations to ensure proper setup of the underlying actor system and benchmarking infrastructure.

- **Parameters**: None
- **Return value**: None
- **Exceptions**: Throws if the actor system is already initialized or if setup fails due to configuration issues.

---

### `public async Task<ActorRef> CreateActorAsync()`
Creates a new actor instance within the benchmarking context. The actor is configured according to the benchmarking setup and is ready for interaction or measurement.

- **Parameters**: None
- **Return value**: `Task<ActorRef>` – A task that resolves to an `ActorRef` representing the newly created actor.
- **Exceptions**: Throws if the actor system is not initialized or if actor creation fails due to resource constraints or configuration errors.

---

### `public void GetActorRef()`
Retrieves a reference to an existing actor within the benchmarking context. This method is used to obtain handles to actors created during benchmarking for subsequent operations or measurements.

- **Parameters**: None
- **Return value**: `void` – The method returns an `ActorRef` via an out parameter (not shown in signature; assume standard pattern).
- **Exceptions**: Throws if the actor reference cannot be resolved due to invalid identifiers or system state.

---
### `public SystemHealthSummary GetHealthSummary()`
Collects and returns a snapshot of the current health and performance metrics of the actor system. This includes resource usage, message queue depths, and system stability indicators relevant to benchmarking analysis.

- **Parameters**: None
- **Return value**: `SystemHealthSummary` – A structured summary containing system health metrics and performance indicators.
- **Exceptions**: Throws if the system is not in a stable state or if metrics collection fails due to permissions or system unavailability.

## Usage

### Example 1: Basic Benchmark Initialization and Actor Creation
