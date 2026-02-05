// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Repository;

/// <summary>
/// Provides extension methods for <see cref="ConnectionManager"/> to enhance connection management functionality.
/// </summary>
public static class ConnectionManagerExtensions
{
    /// <summary>
    /// Gets all active connection keys from the connection pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>An enumerable of connection keys.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public static IEnumerable<string> GetConnectionKeys(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return [];

        lock (connectionManager.GetType().GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(connectionManager) ?? new object())
        {
            var poolField = connectionManager.GetType().GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolField?.GetValue(connectionManager) is Dictionary<string, object> pool)
            {
                return pool.Keys.ToList().AsReadOnly();
            }
            return [];
        }
    }

    /// <summary>
    /// Gets the total number of active connections across all pools.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The total number of active connections.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public static int GetTotalConnections(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return 0;

        lock (connectionManager.GetType().GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(connectionManager) ?? new object())
        {
            var poolField = connectionManager.GetType().GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return poolField?.GetValue(connectionManager) is Dictionary<string, object> pool ? pool.Count : 0;
        }
    }

    /// <summary>
    /// Gets all pooled connections with their statistics.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>A collection of connection information tuples containing key, connection, and statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public static IReadOnlyList<(string Key, object Connection, TimeSpan IdleTime)> GetConnectionStatistics(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return [];

        lock (connectionManager.GetType().GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(connectionManager) ?? new object())
        {
            var poolField = connectionManager.GetType().GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolField?.GetValue(connectionManager) is not Dictionary<string, object> pool)
                return [];

            var result = new List<(string Key, object Connection, TimeSpan IdleTime)>();

            foreach (var kvp in pool)
            {
                if (kvp.Value is PooledConnection pooledConnection)
                {
                    result.Add((kvp.Key, pooledConnection, pooledConnection.GetIdleTime()));
                }
            }

            return result.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the oldest idle connection from the pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The oldest idle connection, or null if no connections are available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public static (string Key, object Connection, TimeSpan IdleTime)? GetOldestIdleConnection(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return null;

        lock (connectionManager.GetType().GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(connectionManager) ?? new object())
        {
            var poolField = connectionManager.GetType().GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolField?.GetValue(connectionManager) is not Dictionary<string, object> pool)
                return null;

            (string Key, object Connection, TimeSpan IdleTime)? oldest = null;

            foreach (var kvp in pool)
            {
                if (kvp.Value is PooledConnection pooledConnection)
                {
                    var idleTime = pooledConnection.GetIdleTime();
                    if (oldest == null || idleTime > oldest.Value.IdleTime)
                    {
                        oldest = (kvp.Key, pooledConnection, idleTime);
                    }
                }
            }

            return oldest;
        }
    }

    /// <summary>
    /// Gets the total idle time of all connections in the pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The total idle time across all connections.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public static TimeSpan GetTotalIdleTime(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return TimeSpan.Zero;

        lock (connectionManager.GetType().GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(connectionManager) ?? new object())
        {
            var poolField = connectionManager.GetType().GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolField?.GetValue(connectionManager) is not Dictionary<string, object> pool)
                return TimeSpan.Zero;

            var totalIdleTime = TimeSpan.Zero;
            foreach (var kvp in pool)
            {
                if (kvp.Value is PooledConnection pooledConnection)
                {
                    totalIdleTime += pooledConnection.GetIdleTime();
                }
            }

            return totalIdleTime;
        }
    }

    /// <summary>
    /// Gets the average idle time of connections in the pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The average idle time, or TimeSpan.Zero if no connections are available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    public static TimeSpan GetAverageIdleTime(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return TimeSpan.Zero;

        lock (connectionManager.GetType().GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(connectionManager) ?? new object())
        {
            var poolField = connectionManager.GetType().GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (poolField?.GetValue(connectionManager) is not Dictionary<string, object> pool || pool.Count == 0)
                return TimeSpan.Zero;

            var totalIdleTime = TimeSpan.Zero;
            foreach (var kvp in pool)
            {
                if (kvp.Value is PooledConnection pooledConnection)
                {
                    totalIdleTime += pooledConnection.GetIdleTime();
                }
            }

            return TimeSpan.FromTicks(totalIdleTime.Ticks / pool.Count);
        }
    }
}