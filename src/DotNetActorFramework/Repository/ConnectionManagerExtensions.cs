// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Data.Common;

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

        return connectionManager.IsConnected
            ? connectionManager.GetConnectionStatistics().Select(stat => stat.Key)
            : [];
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

        return connectionManager.IsConnected
            ? connectionManager.GetConnectionStatistics().Count
            : 0;
    }

    /// <summary>
    /// Gets all pooled connections with their statistics.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>A collection of connection information tuples containing key, connection, and statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when reflection fails to access internal fields.</exception>
    public static IReadOnlyList<(string Key, PooledConnection Connection, TimeSpan IdleTime)> GetConnectionStatistics(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return [];

        var lockObject = connectionManager.GetType()
            .GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(connectionManager) ?? new object();

        lock (lockObject)
        {
            var poolField = connectionManager.GetType()
                .GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return poolField?.GetValue(connectionManager) is Dictionary<string, PooledConnection> pool
                ? GetConnectionStatisticsInternal(pool)
                : [];
        }
    }

    /// <summary>
    /// Gets the oldest idle connection from the pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The oldest idle connection, or null if no connections are available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when reflection fails to access internal fields.</exception>
    public static (string Key, PooledConnection Connection, TimeSpan IdleTime)? GetOldestIdleConnection(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return null;

        var lockObject = connectionManager.GetType()
            .GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(connectionManager) ?? new object();

        lock (lockObject)
        {
            var poolField = connectionManager.GetType()
                .GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return poolField?.GetValue(connectionManager) is Dictionary<string, PooledConnection> pool
                ? GetOldestIdleConnectionInternal(pool)
                : null;
        }
    }

    /// <summary>
    /// Gets the total idle time of all connections in the pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The total idle time across all connections.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when reflection fails to access internal fields.</exception>
    public static TimeSpan GetTotalIdleTime(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return TimeSpan.Zero;

        var lockObject = connectionManager.GetType()
            .GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(connectionManager) ?? new object();

        lock (lockObject)
        {
            var poolField = connectionManager.GetType()
                .GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return poolField?.GetValue(connectionManager) is Dictionary<string, PooledConnection> pool
                ? GetTotalIdleTimeInternal(pool)
                : TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Gets the average idle time of connections in the pool.
    /// </summary>
    /// <param name="connectionManager">The connection manager instance.</param>
    /// <returns>The average idle time, or TimeSpan.Zero if no connections are available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionManager"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when reflection fails to access internal fields.</exception>
    public static TimeSpan GetAverageIdleTime(this ConnectionManager connectionManager)
    {
        ArgumentNullException.ThrowIfNull(connectionManager);

        if (!connectionManager.IsConnected)
            return TimeSpan.Zero;

        var lockObject = connectionManager.GetType()
            .GetField("_lockObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(connectionManager) ?? new object();

        lock (lockObject)
        {
            var poolField = connectionManager.GetType()
                .GetField("_connectionPool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            return poolField?.GetValue(connectionManager) is Dictionary<string, PooledConnection> pool && pool.Count > 0
                ? GetAverageIdleTimeInternal(pool)
                : TimeSpan.Zero;
        }
    }

    private static IReadOnlyList<(string Key, PooledConnection Connection, TimeSpan IdleTime)> GetConnectionStatisticsInternal(Dictionary<string, PooledConnection> pool)
    {
        var result = new List<(string Key, PooledConnection Connection, TimeSpan IdleTime)>(pool.Count);
        foreach (var kvp in pool)
        {
            result.Add((kvp.Key, kvp.Value, kvp.Value.GetIdleTime()));
        }
        return result.AsReadOnly();
    }

    private static (string Key, PooledConnection Connection, TimeSpan IdleTime)? GetOldestIdleConnectionInternal(Dictionary<string, PooledConnection> pool)
    {
        (string Key, PooledConnection Connection, TimeSpan IdleTime)? oldest = null;

        foreach (var kvp in pool)
        {
            var idleTime = kvp.Value.GetIdleTime();
            if (oldest == null || idleTime > oldest.Value.IdleTime)
            {
                oldest = (kvp.Key, kvp.Value, idleTime);
            }
        }

        return oldest;
    }

    private static TimeSpan GetTotalIdleTimeInternal(Dictionary<string, PooledConnection> pool)
    {
        var totalIdleTime = TimeSpan.Zero;
        foreach (var kvp in pool)
        {
            totalIdleTime += kvp.Value.GetIdleTime();
        }
        return totalIdleTime;
    }

    private static TimeSpan GetAverageIdleTimeInternal(Dictionary<string, PooledConnection> pool)
    {
        var totalIdleTime = GetTotalIdleTimeInternal(pool);
        return TimeSpan.FromTicks(totalIdleTime.Ticks / pool.Count);
    }
}
