# HttpActorClient

The `HttpActorClient` is a client component in the `dotnet-actor-framework` that facilitates remote interaction with an actor system via HTTP. It provides methods to send messages, query actor and system state, and monitor health metrics. This client is designed for scenarios where actors are hosted in a remote process or service and accessed over HTTP.

## API

### `HttpActorClient`
**Purpose:**
Initializes a new instance of the `HttpActorClient` to interact with a specific actor in the remote system. The client maintains metadata about the actor and system, including health status and message statistics.

**Parameters:**
- None (instance properties are set during initialization or updated via method calls).

---

### `Task<HttpResponseMessage> SendMessageAsync`
**Purpose:**
Sends a message to the actor asynchronously and returns the HTTP response from the actor's endpoint.

**Parameters:**
- None (message content is configured internally or via the actor's endpoint).

**Returns:**
- `Task<HttpResponseMessage>`: The HTTP response from the actor, including status code and payload.

**Throws:**
- `HttpRequestException`: If the request fails due to network issues or invalid responses.
- `ObjectDisposedException`: If the client has been disposed.

---

### `Task<T?> GetActorStateAsync<T>`
**Purpose:**
Retrieves the current state of the actor as a strongly-typed object. The state is serialized and deserialized using the framework's conventions.

**Parameters:**
- None.

**Returns:**
- `Task<T?>`: The actor's state, or `null` if the state is not available or deserialization fails.

**Throws:**
- `HttpRequestException`: If the request fails or the response cannot be deserialized.
- `InvalidOperationException`: If the actor does not support state retrieval.

---

### `Task<ActorHealthStatus?> GetActorHealthAsync`
**Purpose:**
Queries the health status of the actor, returning structured health information if available.

**Parameters:**
- None.

**Returns:**
- `Task<ActorHealthStatus?>`: The actor's health status, or `null` if health reporting is disabled or unavailable.

**Throws:**
- `HttpRequestException`: If the request fails or the response is invalid.

---

### `Task<SystemHealthStatus?> GetSystemHealthAsync`
**Purpose:**
Queries the health status of the entire actor system, including aggregated metrics.

**Parameters:**
- None.

**Returns:**
- `Task<SystemHealthStatus?>`: The system's health status, or `null` if health reporting is disabled or unavailable.

**Throws:**
- `HttpRequestException`: If the request fails or the response is invalid.

---

### `void Dispose`
**Purpose:**
Releases resources held by the client, including HTTP connections and internal state. Call this method when the client is no longer needed to avoid resource leaks.

**Parameters:**
- None.

**Throws:**
- None.

---

### `string ActorPath`
**Purpose:**
Gets the logical path of the actor within the system, used for routing messages and queries.

**Returns:**
- `string`: The actor's path (e.g., `/system/actor`).

---

### `Guid ActorId`
**Purpose:**
Gets the unique identifier of the actor.

**Returns:**
- `Guid`: The actor's ID.

---

### `string State`
**Purpose:**
Gets the current state of the actor as a string representation (e.g., serialized JSON or a status descriptor).

**Returns:**
- `string`: The actor's state, or an empty string if unavailable.

---

### `long MessageCount`
**Purpose:**
Gets the total number of messages processed by the actor since initialization.

**Returns:**
- `long`: The message count.

---

### `long ErrorCount`
**Purpose:**
Gets the total number of errors encountered by the actor since initialization.

**Returns:**
- `long`: The error count.

---

### `double ErrorRate`
**Purpose:**
Gets the ratio of errors to total messages, expressed as a value between `0.0` (no errors) and `1.0` (all messages failed).

**Returns:**
- `double`: The error rate.

---

### `bool IsHealthy`
**Purpose:**
Indicates whether the actor is currently in a healthy state, based on error thresholds or custom health checks.

**Returns:**
- `bool`: `true` if healthy, `false` otherwise.

---

### `string SystemName`
**Purpose:**
Gets the name of the actor system to which the actor belongs.

**Returns:**
- `string`: The system name.

---

### `Guid SystemId`
**Purpose:**
Gets the unique identifier of the actor system.

**Returns:**
- `Guid`: The system ID.

---

### `int TotalActors`
**Purpose:**
Gets the total number of actors in the system, including both healthy and unhealthy actors.

**Returns:**
- `int`: The total actor count.

---

### `int HealthyActors`
**Purpose:**
Gets the number of actors in the system currently marked as healthy.

**Returns:**
- `int`: The healthy actor count.

---

### `int UnhealthyActors`
**Purpose:**
Gets the number of actors in the system currently marked as unhealthy.

**Returns:**
- `int`: The unhealthy actor count.

---

### `long TotalMessages`
**Purpose:**
Gets the total number of messages processed across all actors in the system.

**Returns:**
- `long`: The total message count.

---

### `long TotalErrors`
**Purpose:**
Gets the total number of errors encountered across all actors in the system.

**Returns:**
- `long`: The total error count.

## Usage

### Example 1: Sending a Message and Checking Actor Health
