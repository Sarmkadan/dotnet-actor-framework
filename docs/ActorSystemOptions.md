# ActorSystemOptions

Configuration container for initializing an `ActorSystem`. Instances of this type are passed to the actor framework bootstrap APIs to define system‑wide behavior such as mailbox settings, supervision, persistence, clustering, and observability.

## API

| Member | Type | Purpose | Parameters | Return Value | Throws |
|--------|------|---------|------------|--------------|--------|
| `SystemName` | `string` | Logical name of the actor system; used for logging, clustering identification, and persistence keys. | none | The assigned system name string. | `ArgumentNullException` if set to `null`; `ArgumentException` if empty or whitespace. |
| `DefaultMailboxCapacity` | `int` | Maximum number of messages a mailbox can hold before back‑pressure is applied. | none | The capacity value (must be > 0). | `ArgumentOutOfRangeException` if ≤ 0. |
| `DefaultMailboxType` | `MailboxType` | Enum selecting the default mailbox implementation (e.g., bounded, unbounded, priority). | none | The selected mailbox type. | None. |
| `DefaultTimeoutSeconds` | `int` | Default timeout (in seconds) for ask‑pattern operations and remote calls when no explicit timeout is supplied. | none | Timeout value in seconds (must be ≥ 0). | `ArgumentOutOfRangeException` if < 0. |
| `MaxMessageRetries` | `int` | Number of times a failed message delivery is retried before the message is dead‑lettered. | none | Retry count (must be ≥ 0). | `ArgumentOutOfRangeException` if < 0. |
| `MaxActorDepth` | `int` | Maximum allowed depth of actor supervision hierarchies; prevents runaway recursive supervision. | none | Depth limit (must be > 0). | `ArgumentOutOfRangeException` if ≤ 0. |
| `DefaultSupervisionStrategy` | `SupervisionStrategy` | Supervision directive applied to child actors when no explicit strategy is defined. | none | The supervision strategy enum value. | None. |
| `EnableMessagePersistence` | `bool` | When `true`, enables durable storage of incoming messages for replay and recovery. | none | Boolean flag. | None. |
| `EnableMetricsCollection` | `bool` | When `true`, activates collection of runtime metrics (throughput, latency, mailbox sizes). | none | Boolean flag. | None. |
| `EnableActorStateSnapshotting` | `bool` | When `true`, periodic snapshots of actor state are taken to reduce recovery time. | none | Boolean flag. | None. |
| `SnapshotIntervalSeconds` | `int` | Interval (in seconds) between automatic state snapshots when snapshotting is enabled. | none | Interval value (must be > 0). | `ArgumentOutOfRangeException` if ≤ 0. |
| `DefaultPersistenceBackend` | `PersistenceBackend` | Enum selecting the default storage provider (e.g., SQLite, PostgreSQL, Azure Blob). | none | The selected backend. | None. |
| `DatabaseConnectionString` | `string?` | Connection string used by the chosen persistence backend; may be `null` for in‑memory or file‑based stores. | none | The connection string or `null`. | None. |
| `EnableClusterMode` | `bool` | When `true`, the actor system joins a cluster and participates in distributed routing and failover. | none | Boolean flag. | None. |
| `ClusterAddress` | `string` | Network endpoint (host:port) where this node listens for cluster gossip and replication messages. | none | The address string. | `ArgumentNullException` if set to `null`; `ArgumentException` if malformed. |
| `MaxClusterNodes` | `int` | Upper bound on the number of nodes allowed to join the cluster; excess join attempts are rejected. | none | Node limit (must be > 0). | `ArgumentOutOfRangeException` if ≤ 0. |
| `UnhealthyErrorRateThreshold` | `double` | Ratio of failed messages to total messages (0.0‑1.0) that marks a node as unhealthy for cluster health checks. | none | Threshold value (must be between 0 and 1). | `ArgumentOutOfRangeException` if outside [0,1]. |
| `CriticalErrorRateThreshold` | `double` | Ratio of failed messages to total messages (0.0‑1.0) that triggers automatic node removal from the cluster. | none | Threshold value (must be between 0 and 1 and ≥ `UnhealthyErrorRateThreshold`). | `ArgumentOutOfRangeException` if outside [0,1] or less than unhealthy threshold. |
| `InitialBackoffDelayMs` | `int` | Initial delay (in milliseconds) before retrying a failed operation (e.g., message send, cluster join). | none | Delay value (must be ≥ 0). | `ArgumentOutOfRangeException` if < 0. |
| `MaxBackoffDelayMs` | `int` | Upper limit for exponential backoff delay (in milliseconds). | none | Delay value (must be ≥ `InitialBackoffDelayMs`). | `ArgumentOutOfRangeException` if < 0 or less than initial delay. |

## Usage

### Basic local actor system

```csharp
using DotNetActorFramework;

var options = new ActorSystemOptions
{
    SystemName = "MyLocalSystem",
    DefaultMailboxCapacity = 1000,
    DefaultMailboxType = MailboxType.Unbounded,
    DefaultTimeoutSeconds = 5,
    MaxMessageRetries = 3,
    MaxActorDepth = 10,
    DefaultSupervisionStrategy = SupervisionStrategy.Restart,
    EnableMessagePersistence = false,
    EnableMetricsCollection = true,
    EnableActorStateSnapshotting = false,
    SnapshotIntervalSeconds = 60,
    DefaultPersistenceBackend = PersistenceBackend.None,
    DatabaseConnectionString = null,
    EnableClusterMode = false,
    ClusterAddress = "", // ignored when cluster mode is disabled
    MaxClusterNodes = 0,
    UnhealthyErrorRateThreshold = 0.1,
    CriticalErrorRateThreshold = 0.3,
    InitialBackoffDelayMs = 100,
    MaxBackoffDelayMs = 5000
};

var system = ActorSystem.Create(options);
```

### Clustered system with persistence and snapshotting

```csharp
using DotNetActorFramework;

var options = new ActorSystemOptions
{
    SystemName = "ClusteredProdSystem",
    DefaultMailboxCapacity = 5000,
    DefaultMailboxType = MailboxType.Bounded,
    DefaultTimeoutSeconds = 10,
    MaxMessageRetries = 5,
    MaxActorDepth = 15,
    DefaultSupervisionStrategy = SupervisionStrategy.Escalate,
    EnableMessagePersistence = true,
    EnableMetricsCollection = true,
    EnableActorStateSnapshotting = true,
    SnapshotIntervalSeconds = 300,
    DefaultPersistenceBackend = PersistenceBackend.PostgreSql,
    DatabaseConnectionString = "Host=dbhost;Port=5432;Database=actordb;Username=actor;Password=secret",
    EnableClusterMode = true,
    ClusterAddress = "actor-node-01.internal:4053",
    MaxClusterNodes = 20,
    UnhealthyErrorRateThreshold = 0.05,
    CriticalErrorRateThreshold = 0.2,
    InitialBackoffDelayMs = 250,
    MaxBackoffDelayMs = 15000
};

var system = ActorSystem.Create(options);
```

## Notes

- All mutable properties are intended to be set **before** the `ActorSystem` is constructed; modifying them after creation has no effect on the running system and may lead to undefined behavior.
- The type is **not thread‑safe** for concurrent writes; configuration should be performed by a single thread or protected by external synchronization.
- Validation occurs at the point of assignment; invalid values throw the exceptions listed in the API table.
- When `EnableClusterMode` is `false`, `ClusterAddress`, `MaxClusterNodes`, `UnhealthyErrorRateThreshold`, and `CriticalErrorRateThreshold` are ignored but must still conform to their validation rules if set.
- Setting `EnableMessagePersistence` to `true` requires a non‑null `DatabaseConnectionString` for backends that need a connection (e.g., PostgreSQL, SQLite). For in‑memory backends the connection string may be `null`.
- `SnapshotIntervalSeconds` is only consulted when `EnableActorStateSnapshotting` is `true`; otherwise the value is ignored but still validated.
- The `SupervisionStrategy` enum values dictate how exceptions in child actors are propagated; choosing `Escalate` will cause failures to bubble up to the parent’s supervisor, potentially terminating the entire hierarchy if not handled elsewhere.
