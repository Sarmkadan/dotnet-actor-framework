# ConnectionManager

The `ConnectionManager` type manages the lifecycle of database connections within the dotnet‑actor‑framework, providing initialization, pooling, validation, and disposal semantics for a single logical connection identified by a key.

## API

| Member | Description | Parameters | Return Value | Exceptions |
|--------|-------------|------------|--------------|------------|
| `public void Initialize()` | Prepares the manager for use; reads configuration, creates the underlying pool, and sets initial state. | None | None | Throws `InvalidOperationException` if called after the manager has already been initialized or disposed. |
| `public object? GetConnection()` | Retrieves an available connection from the pool. Returns `null` when no connections are currently available and the pool cannot grow. | None | An object representing a live connection, or `null`. | Throws `ObjectDisposedException` if the manager has been disposed. |
| `public void ReleaseConnection()` | Returns a previously obtained connection to the pool for reuse. | None | None | Throws `InvalidOperationException` if called without a corresponding `GetConnection` call, or if the manager is disposed. |
| `public async Task<bool> ValidateConnectionAsync()` | Asynchronously checks whether the underlying connection is still usable (e.g., by issuing a lightweight ping). | None | `true` if the connection is valid; `false` otherwise. | Throws `ObjectDisposedException` if the manager is disposed; may propagate provider‑specific exceptions from the validation query. |
| `public ConnectionStatistics GetStatistics()` | Returns a snapshot of pool usage statistics (e.g., active, idle, and total connections). | None | A `ConnectionStatistics` instance. | Throws `ObjectDisposedException` if the manager is disposed. |
| `public void Dispose()` | Releases all resources held by the manager, closes pooled connections, and prevents further operations. | None | None | None (calling multiple times is safe). |
| `public string Key` | Gets the immutable identifier associated with this manager instance. | None | The key string supplied at construction. | None |
| `public string ConnectionString` | Gets the connection string used to create connections (non‑null view). | None | The connection string; guaranteed non‑null after `Initialize`. | Throws `InvalidOperationException` if accessed before `Initialize`. |
| `public DateTime CreatedAt` | Gets the timestamp when the manager was initialized. | None | The UTC date/time of initialization. | Throws `InvalidOperationException` if accessed before `Initialize`. |
| `public DateTime LastUsedAt` | Gets the UTC date/time when the pool last handed out a connection. | None | The last‑used timestamp; updates on each successful `GetConnection`. | Throws `ObjectDisposedException` if the manager is disposed. |
| `public bool IsOpen` | Indicates whether the manager is currently able to service connection requests. | None | `true` after `Initialize` and before `Dispose`; otherwise `false`. | None |
| `public PooledConnection PooledConnection` | Provides direct access to the internal pooled connection object used for advanced scenarios. | None | The pooled connection instance. | Throws `ObjectDisposedException` if the manager is disposed. |
| `public void UpdateLastUsed()` | Manually refreshes the `LastUsedAt` timestamp to the current UTC time. | None | None | Throws `ObjectDisposedException` if the manager is disposed. |
| `public TimeSpan GetIdleTime()` | Returns the amount of time that has elapsed since the last connection was used (`LastUsedAt`). | None | A `TimeSpan` representing idle duration. | Throws `ObjectDisposedException` if the manager is disposed. |
| `public bool IsConnected` | Indicates whether at least one connection in the pool is currently open and usable. | None | `true` if the pool contains an open connection; otherwise `false`. | None |
| `public int PoolSize` | Gets the configured maximum number of connections that the pool may hold. | None | The pool size limit. | Throws `InvalidOperationException` if accessed before `Initialize`. |
| `public string? ConnectionString` | Gets the connection string used to create connections (nullable view). | None | The connection string; may be `null` prior to `Initialize`. | None |
| `public DateTime CreatedAt` | Gets the timestamp when the manager was initialized (duplicate entry). | None | The UTC date/time of initialization. | Throws `InvalidOperationException` if accessed before `Initialize`. |

## Usage

```csharp
using System;
using System.Threading.Tasks;
using DotNetActorFramework.Data;

public class Service
{
    private readonly ConnectionManager _mgr;

    public Service(string key, string connectionString)
    {
        _mgr = new ConnectionManager(key, connectionString);
        _mgr.Initialize();
    }

    public async Task<bool> ExecuteWorkAsync()
    {
        var conn = _mgr.GetConnection();
        if (conn == null)
        {
            // Pool exhausted – handle back‑pressure or fallback.
            return false;
        }

        try
        {
            // Use the connection (omitted for brevity).
            bool isValid = await _mgr.ValidateConnectionAsync();
            if (!isValid)
                return false;

            // Perform work...
            return true;
        }
        finally
        {
            _mgr.ReleaseConnection();
            _mgr.UpdateLastUsed();
        }
    }

    public void Dispose()
    {
        _mgr.Dispose();
    }
}
```

```csharp
using System;
using DotNetActorFramework.Diagnostics;

public class MonitoringJob
{
    public void ReportPoolHealth(ConnectionManager mgr)
    {
        if (!mgr.IsOpen)
        {
            Console.WriteLine("Manager is not initialized.");
            return;
        }

        var stats = mgr.GetStatistics();
        Console.WriteLine($"Pool size: {mgr.PoolSize}");
        Console.WriteLine($"Active connections: {stats.Active}");
        Console.WriteLine($"Idle connections: {stats.Idle}");
        Console.WriteLine($"Last used: {mgr.LastUsedAt}");
        Console.WriteLine($"Idle time: {mgr.GetIdleTime()}");
        Console.WriteLine($"Is any connection open? {mgr.IsConnected}");
    }
}
```

## Notes

- `Initialize` must be invoked exactly once before any other member is accessed; calling it after disposal or a prior initialization results in an `InvalidOperationException`.
- `GetConnection` may return `null` when the pool is exhausted and cannot grow; callers should handle this case rather than assuming a non‑null return.
- Each successful call to `GetConnection` must be paired with a call to `ReleaseConnection`; failing to do so will leak connections and eventually exhaust the pool.
- `ValidateConnectionAsync` performs a lightweight validity check; it does not automatically reconnect. If validation fails, the caller should discard the connection and obtain a new one via `GetConnection`.
- `Dispose` is idempotent; subsequent calls after the first have no effect. After disposal, all instance members throw `ObjectDisposedException` except for the read‑only properties `Key`, `IsOpen`, `IsConnected`, and the duplicate `ConnectionString`/`CreatedAt` members, which return their last known values.
- The class exposes two `ConnectionString` and two `CreatedAt` members (one nullable, one not). Both refer to the same underlying value; the non‑nullable versions throw if accessed before `Initialize`, while the nullable versions return `null` in that state.
- All members are thread‑safe unless explicitly noted; concurrent calls to `GetConnection` and `ReleaseConnection` are synchronized internally. However, manual state‑changing calls such as `UpdateLastUsed` should not be relied upon for precise timing in highly contended scenarios without external synchronization.
