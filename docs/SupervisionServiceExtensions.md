# SupervisionServiceExtensions

Provides extension methods for working with actor supervision contexts, statistics, and failure tracking in the dotnet-actor-framework. These methods enable inspection of actor failure states, restart counts, and performance metrics to support custom supervision strategies.

## API

### `GetContext`
Gets the current supervision context for an actor.

- **Returns**: The current `SupervisionContext` if one exists; otherwise `null`.
- **Throws**: Does not throw exceptions.

### `HasExceededFailureThreshold`
Determines whether an actor has exceeded its configured failure threshold.

- **Returns**: `true` if the actor's failure count exceeds the threshold; otherwise `false`.
- **Throws**: Does not throw exceptions.

### `GetActorStatistics`
Gets the supervision statistics for a specific actor.

- **Returns**: An `ActorSupervisionStatistics` object containing failure and restart counts, or `null` if the actor has no recorded statistics.
- **Throws**: Does not throw exceptions.

### `GetAllActorStatistics`
Gets supervision statistics for all actors.

- **Returns**: A dictionary mapping actor IDs to their respective `ActorSupervisionStatistics`.
- **Throws**: Does not throw exceptions.

### `GetRecentlyFailedActors`
Gets the IDs of actors that have failed recently.

- **Returns**: An enumerable of actor IDs, ordered by most recent failure time.
- **Throws**: Does not throw exceptions.

### `GetWorstPerformingActor`
Identifies the actor with the highest failure count.

- **Returns**: The actor ID of the worst-performing actor, or `Guid.Empty` if no actors have failed.
- **Throws**: Does not throw exceptions.

### `ActorId` (property)
Gets the unique identifier of the actor.

- **Type**: `Guid`
- **Throws**: Does not throw exceptions.

### `FailureCount` (property)
Gets the total number of failures recorded for the actor.

- **Type**: `int`
- **Throws**: Does not throw exceptions.

### `RestartCount` (property)
Gets the total number of restarts performed for the actor.

- **Type**: `int`
- **Throws**: Does not throw exceptions.

### `LastFailureTime` (property)
Gets the timestamp of the most recent failure.

- **Type**: `DateTime`
- **Throws**: Does not throw exceptions.

### `TimeSinceLastFailure` (property)
Gets the time elapsed since the last failure.

- **Type**: `TimeSpan`
- **Throws**: Does not throw exceptions.

## Usage

### Example 1: Checking if an actor should be restarted
