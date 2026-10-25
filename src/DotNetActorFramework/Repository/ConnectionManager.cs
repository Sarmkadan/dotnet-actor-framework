// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Data;
using System.Data.Common;

namespace DotNetActorFramework.Repository;

/// <summary>
/// Manages database connections and provides connection pooling capabilities.
/// </summary>
public class ConnectionManager : IDisposable
{
    private string? _connectionString;
    private readonly Dictionary<string, PooledConnection> _connectionPool = [];
    private readonly object _lockObject = new();
    private bool _disposed;
    private readonly string _providerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManager"/> class for testing/reflection.
    /// </summary>
    protected ConnectionManager()
    {
        _providerName = "System.Data.SqlClient";
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
    /// </summary>
    /// <param name="providerName">The ADO.NET provider invariant name.</param>
    public ConnectionManager(string providerName = "System.Data.SqlClient")
    {
        _providerName = providerName ?? throw new ArgumentNullException(nameof(providerName));
    }

    /// <summary>
    /// Gets or sets the connection string for database connections.
    /// </summary>
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

    /// <summary>
    /// Gets the current pool size.
    /// </summary>
    public int PoolSize => _connectionPool.Count;

    /// <summary>
    /// Gets a value indicating whether the connection manager is initialized with a connection string.
    /// </summary>
    public bool IsConnected => !string.IsNullOrWhiteSpace(_connectionString);

    /// <summary>
    /// Initializes the connection manager with a connection string.
    /// </summary>
    /// <param name="connectionString">The database connection string.</param>
    public void Initialize(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string cannot be null or empty.", nameof(connectionString));

        ConnectionString = connectionString;
    }

    /// <summary>
    /// Gets or creates a connection from the pool.
    /// </summary>
    /// <param name="key">The connection key (defaults to "default").</param>
    /// <returns>A <see cref="PooledConnection"/> instance.</returns>
    public PooledConnection GetConnection(string key = "default")
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Connection key cannot be null or empty.", nameof(key));

        if (!IsConnected)
            throw new InvalidOperationException("Connection manager is not initialized.");

        lock (_lockObject)
        {
            if (_connectionPool.TryGetValue(key, out var pooledConnection))
            {
                pooledConnection.UpdateLastUsed();
                return pooledConnection;
            }

            var newConnection = new PooledConnection(key, _connectionString!);
            _connectionPool[key] = newConnection;
            return newConnection;
        }
    }

    /// <summary>
    /// Releases a connection back to the pool.
    /// </summary>
    /// <param name="key">The connection key.</param>
    public void ReleaseConnection(string key = "default")
    {
        ArgumentNullException.ThrowIfNull(key);
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Connection key cannot be null or empty.", nameof(key));

        lock (_lockObject)
        {
            if (_connectionPool.TryGetValue(key, out var pooledConnection))
            {
                pooledConnection.UpdateLastUsed();
            }
        }
    }

    /// <summary>
    /// Validates the current connection by opening and closing it.
    /// </summary>
    public Task<bool> ValidateConnectionAsync()
    {
        if (!IsConnected)
            return Task.FromResult(false);

        try
        {
            // Do not dispose here: the connection stays in the pool and disposing it
            // would make every subsequent GetConnection call return a dead instance.
            var connection = GetConnection();
            connection.Open();
            connection.Close();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
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
                CreatedAt = DateTime.UtcNow,
                ActiveConnections = _connectionPool.Count
            };
        }
    }

    /// <summary>
    /// Clears the connection pool and disposes all connections.
    /// </summary>
    private void ClearPool()
    {
        lock (_lockObject)
        {
            foreach (var connection in _connectionPool.Values)
            {
                if (connection is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _connectionPool.Clear();
        }
    }

    /// <summary>
    /// Releases all pooled connections and marks the connection manager as disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        ClearPool();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Returns a concise, informative representation of the connection manager.
    /// </summary>
    public override string ToString()
    {
        var stats = GetStatistics();
        return $"ConnectionManager {{ IsConnected = {stats.IsConnected}, PoolSize = {stats.PoolSize}, ActiveConnections = {stats.ActiveConnections}, ConnectionString = {stats.ConnectionString}, CreatedAt = {stats.CreatedAt} }}";
    }
}

/// <summary>
/// Represents a pooled connection.
/// </summary>
public class PooledConnection : IDisposable
{
    private bool _disposed;
    private string? _connectionString;

    /// <summary>
    /// Gets the key that identifies this connection in the pool.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the connection string used by this connection, or an empty string if none is set.
    /// </summary>
    public string ConnectionString => _connectionString ?? string.Empty;

    /// <summary>
    /// Gets the UTC time at which this connection was created.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Gets the UTC time at which this connection was last used.
    /// </summary>
    public DateTime LastUsedAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this connection is currently open.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PooledConnection"/> class.
    /// </summary>
    /// <param name="key">The connection key used to identify this connection in the pool.</param>
    /// <param name="connectionString">The database connection string.</param>
    public PooledConnection(string key, string connectionString)
    {
        Key = key ?? throw new ArgumentNullException(nameof(key));
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        CreatedAt = DateTime.UtcNow;
        LastUsedAt = DateTime.UtcNow;
        IsOpen = false;
    }

    /// <summary>
    /// Marks the connection as open and updates its last-used time.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The connection has been disposed.</exception>
    public void Open()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(PooledConnection));
        IsOpen = true;
        LastUsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the connection as closed.
    /// </summary>
    public void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Updates the last-used time of the connection to the current UTC time.
    /// </summary>
    public void UpdateLastUsed() => LastUsedAt = DateTime.UtcNow;

    /// <summary>
    /// Gets the amount of time elapsed since the connection was last used.
    /// </summary>
    /// <returns>The idle time as a <see cref="TimeSpan"/>.</returns>
    public TimeSpan GetIdleTime() => DateTime.UtcNow - LastUsedAt;

    /// <summary>
    /// Closes the connection and marks it as disposed. Subsequent calls have no effect.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        IsOpen = false;
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Statistics about connections.
/// </summary>
public class ConnectionStatistics
{
    /// <summary>
    /// Gets or sets a value indicating whether the connection manager is initialized with a connection string.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Gets or sets the number of connections in the pool.
    /// </summary>
    public int PoolSize { get; set; }

    /// <summary>
    /// Gets or sets the number of active connections.
    /// </summary>
    public int ActiveConnections { get; set; }

    /// <summary>
    /// Gets or sets the connection string used by the connection manager.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the statistics were captured.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
