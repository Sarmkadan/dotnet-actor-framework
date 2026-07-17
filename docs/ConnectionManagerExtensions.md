# ConnectionManagerExtensions

The `ConnectionManagerExtensions` static class provides a set of extension methods for inspecting the state of an `IConnectionManager` instance. These methods allow callers to query connection keys, aggregate statistics, and identify idle connections without directly accessing the internal pool. They are designed for monitoring, diagnostics, and health-check scenarios.

## API

### `GetConnectionKeys`

`public static IEnumerable<string> GetConnectionKeys`

Returns an enumeration of all connection keys currently registered in the connection manager. The keys are returned in an arbitrary order. This method does not throw.

### `GetTotalConnections`

`public static int GetTotalConnections`

Returns the total number of connections currently managed by the connection manager, including both active and idle connections. This method does not throw.

### `GetConnectionStatistics`

`public static IReadOnlyList<(string Key, PooledConnection Connection, TimeSpan IdleTime)> GetConnectionStatistics`

Returns a read-only list of tuples, each containing a connection key, the associated `PooledConnection` object, and the duration the connection has been idle. The list is a snapshot of the current state. This method does not throw.

### `GetOldestIdleConnection`

`public static (string Key, PooledConnection Connection, TimeSpan IdleTime)? GetOldestIdleConnection`

Returns the connection with the longest idle time, or `null` if no connections are idle. The returned tuple contains the key, the connection, and its idle duration. This method does not throw.

### `GetTotalIdleTime`

`public static TimeSpan GetTotalIdleTime`

Returns the sum of idle times across all connections currently in the pool. If no connections exist, returns `TimeSpan.Zero`. This method does not throw.

### `GetAverageIdleTime`

`public static TimeSpan GetAverageIdleTime`

Returns the average idle time across all connections. If no connections exist, returns `TimeSpan.Zero`. This method does not throw.

## Usage

The following examples assume an `IConnectionManager` instance named `manager`.

**Example 1 – Monitoring idle connections**

```csharp
var oldest = manager.GetOldestIdleConnection();
if (oldest.HasValue)
{
    Console.WriteLine($"Oldest idle connection: {oldest.Value.Key}, idle for {oldest.Value.IdleTime.TotalSeconds:F1}s");
}
else
{
    Console.WriteLine("No idle connections.");
}

var stats = manager.GetConnectionStatistics();
foreach (var (key, conn, idle) in stats)
{
    Console.WriteLine($"Key: {key}, Idle: {idle.TotalMilliseconds}ms");
}
```

**Example 2 – Aggregate health check**

```csharp
int total = manager.GetTotalConnections();
TimeSpan totalIdle = manager.GetTotalIdleTime();
TimeSpan avgIdle = manager.GetAverageIdleTime();

Console.WriteLine($"Total connections: {total}");
Console.WriteLine($"Total idle time: {totalIdle.TotalSeconds:F2}s");
Console.WriteLine($"Average idle time: {avgIdle.TotalSeconds:F2}s");

var keys = manager.GetConnectionKeys().ToList();
Console.WriteLine($"Connection keys: {string.Join(", ", keys)}");
```

## Notes

- All methods are extension methods on `IConnectionManager`. They will throw an `ArgumentNullException` if the `manager` argument is `null`.
- The returned collections and tuples are snapshots taken at the moment of the call. They may become stale immediately after the method returns due to concurrent pool activity.
- Thread safety depends on the underlying `IConnectionManager` implementation. The methods themselves do not introduce additional synchronization; they rely on the pool’s internal locking mechanisms. In a multi-threaded environment, repeated calls may yield inconsistent results if the pool is being modified concurrently.
- `GetConnectionStatistics` returns a new list each time it is called. Callers should not cache the reference indefinitely.
- `GetOldestIdleConnection` returns `null` when the pool is empty or when all connections are active (idle time is zero). It does not throw.
- `GetTotalIdleTime` and `GetAverageIdleTime` return `TimeSpan.Zero` when there are no connections. They do not throw.
