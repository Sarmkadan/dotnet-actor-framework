// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Interface for remote actor invocation across distributed systems.
/// Enables calling actors in other processes or machines.
/// </summary>
public interface IRemoteActorInvoker
{
    /// <summary>
    /// Invokes a remote actor and waits for a response.
    /// </summary>
    Task<T?> InvokeAsync<T>(string remoteActorPath, Message message, TimeSpan? timeout = null);

    /// <summary>
    /// Sends a message to a remote actor without waiting for response.
    /// </summary>
    Task SendAsync(string remoteActorPath, Message message);

    /// <summary>
    /// Checks if a remote actor is reachable.
    /// </summary>
    Task<bool> PingAsync(string remoteActorPath);
}

/// <summary>
/// HTTP-based remote actor invoker.
/// Uses HTTP to communicate with actors in remote systems.
/// </summary>
public class HttpRemoteActorInvoker : IRemoteActorInvoker
{
    private readonly HttpActorClient _client;
    private readonly Dictionary<string, string> _remoteActorUrls = [];
    private readonly object _lockObject = new();

    public HttpRemoteActorInvoker(string baseUrl)
    {
        _client = new HttpActorClient(baseUrl);
    }

    /// <summary>
    /// Registers a remote actor endpoint.
    /// </summary>
    public void RegisterRemoteActor(string actorPath, string httpUrl)
    {
        if (string.IsNullOrWhiteSpace(actorPath) || string.IsNullOrWhiteSpace(httpUrl))
            throw new ArgumentException("Actor path and URL must not be empty.");

        lock (_lockObject)
        {
            _remoteActorUrls[actorPath] = httpUrl;
        }
    }

    public async Task<T?> InvokeAsync<T>(string remoteActorPath, Message message, TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(remoteActorPath) || message == null)
            return default;

        try
        {
            var response = await _client.SendMessageAsync(remoteActorPath, message);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content.FromJson<T>();
            }
            return default;
        }
        catch
        {
            return default;
        }
    }

    public async Task SendAsync(string remoteActorPath, Message message)
    {
        if (string.IsNullOrWhiteSpace(remoteActorPath) || message == null)
            return;

        try
        {
            await _client.SendMessageAsync(remoteActorPath, message);
        }
        catch
        {
            // Silently fail for fire-and-forget
        }
    }

    public async Task<bool> PingAsync(string remoteActorPath)
    {
        if (string.IsNullOrWhiteSpace(remoteActorPath))
            return false;

        try
        {
            var health = await _client.GetActorHealthAsync(remoteActorPath);
            return health != null && health.IsHealthy;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}

/// <summary>
/// Remote call result with timing information.
/// </summary>
public class RemoteCallResult<T>
{
    public T? Result { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public long ElapsedMilliseconds { get; set; }
}

/// <summary>
/// Circuit breaker for remote actor calls.
/// Prevents cascading failures by failing fast when remote system is down.
/// </summary>
public class RemoteActorCircuitBreaker
{
    private readonly Dictionary<string, CircuitBreakerState> _states = [];
    private readonly int _failureThreshold;
    private readonly TimeSpan _timeout;
    private readonly object _lockObject = new();

    public RemoteActorCircuitBreaker(int failureThreshold = 5, TimeSpan? timeout = null)
    {
        if (failureThreshold <= 0)
            throw new ArgumentException("Failure threshold must be positive.", nameof(failureThreshold));

        _failureThreshold = failureThreshold;
        _timeout = timeout ?? TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Records a successful call.
    /// </summary>
    public void RecordSuccess(string remoteActorPath)
    {
        lock (_lockObject)
        {
            if (_states.TryGetValue(remoteActorPath, out var state))
            {
                state.FailureCount = 0;
                state.LastSuccessAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Records a failed call.
    /// </summary>
    public void RecordFailure(string remoteActorPath)
    {
        lock (_lockObject)
        {
            if (!_states.TryGetValue(remoteActorPath, out var state))
            {
                state = new CircuitBreakerState();
                _states[remoteActorPath] = state;
            }

            state.FailureCount++;
            if (state.FailureCount >= _failureThreshold)
            {
                state.IsOpen = true;
                state.OpenedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Checks if calls to the remote actor should be allowed.
    /// </summary>
    public bool CanCall(string remoteActorPath)
    {
        lock (_lockObject)
        {
            if (!_states.TryGetValue(remoteActorPath, out var state))
                return true;

            if (!state.IsOpen)
                return true;

            // Try to reset if timeout expired
            if (DateTime.UtcNow - state.OpenedAt > _timeout)
            {
                state.IsOpen = false;
                state.FailureCount = 0;
                return true;
            }

            return false;
        }
    }

    private class CircuitBreakerState
    {
        public bool IsOpen { get; set; }
        public int FailureCount { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? LastSuccessAt { get; set; }
    }
}
