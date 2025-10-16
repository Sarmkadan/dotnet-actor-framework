// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Repository;

/// <summary>
/// Manages database connections and provides connection pooling capabilities.
/// </summary>
public class ConnectionManager : IDisposable
{
    private string? _connectionString;
    private readonly Dictionary<string, object> _connectionPool = [];
    private readonly object _lockObject = new();
    private bool _disposed;

    public string? ConnectionString
    {
        get => _connectionString;
        set
        {
            if (value != _connectionString)
            {
                _connectionString = value;
                ClearPool();
            }
        }
    }

    public int PoolSize => _connectionPool.Count;
    public bool IsConnected => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>
    /// Initializes the connection manager with a connection string.
    /// </summary>
    public void Initialize(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        ConnectionString = connectionString;
    }

    /// <summary>
    /// Gets or creates a connection from the pool.
    /// </summary>
    public object? GetConnection(string key = "default")
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Connection key cannot be null or empty.", nameof(key));

        if (!IsConnected)
            throw new InvalidOperationException("Connection manager is not initialized.");

        lock (_lockObject)
        {
            if (_connectionPool.TryGetValue(key, out var connection))
            {
                return connection;
            }

            // Create a new mock connection for demonstration
            var newConnection = new PooledConnection(key, _connectionString!);
            _connectionPool[key] = newConnection;
            return newConnection;
        }
    }

    /// <summary>
    /// Releases a connection back to the pool.
    /// </summary>
    public void ReleaseConnection(string key = "default")
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Connection key cannot be null or empty.", nameof(key));

        lock (_lockObject)
        {
            // In a real implementation, the connection would be validated and reused
            // For now, we keep it in the pool for reuse
        }
    }

    /// <summary>
    /// Validates the current connection.
    /// </summary>
    public async Task<bool> ValidateConnectionAsync()
    {
        if (!IsConnected)
            return false;

        try
        {
            var connection = GetConnection();
            // Simulate validation
            await Task.Delay(10);
            return connection != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets connection statistics.
    /// </summary>
    public ConnectionStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            return new ConnectionStatistics
            {
                IsConnected = IsConnected,
                PoolSize = _connectionPool.Count,
                ConnectionString = _connectionString,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Clears the connection pool.
    /// </summary>
    private void ClearPool()
    {
        lock (_lockObject)
        {
            foreach (var conn in _connectionPool.Values.OfType<IDisposable>())
            {
                conn.Dispose();
            }

            _connectionPool.Clear();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        ClearPool();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Represents a pooled connection.
/// </summary>
public class PooledConnection : IDisposable
{
    public string Key { get; }
    public string ConnectionString { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastUsedAt { get; private set; }
    public bool IsOpen { get; private set; }

    public PooledConnection(string key, string connectionString)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
        IsOpen = true;
    }

    public void UpdateLastUsed() => LastUsedAt = DateTime.UtcNow;

    public TimeSpan GetIdleTime() => DateTime.UtcNow - LastUsedAt;

    public void Dispose()
    {
        IsOpen = false;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Statistics about connections.
/// </summary>
public class ConnectionStatistics
{
    public bool IsConnected { get; set; }
    public int PoolSize { get; set; }
    public string? ConnectionString { get; set; }
    public DateTime CreatedAt { get; set; }
}
