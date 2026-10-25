// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;

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
public class HttpRemoteActorInvoker : IRemoteActorInvoker, IDisposable
{
    private readonly HttpActorClient _client;
    private readonly Dictionary<string, string> _remoteActorUrls = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRemoteActorInvoker"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL of the remote actor system.</param>
    public HttpRemoteActorInvoker(string baseUrl)
    {
        _client = new HttpActorClient(baseUrl);
    }

    /// <summary>
    /// Registers a remote actor endpoint.
    /// </summary>
    public void RegisterRemoteActor(string actorPath, string httpUrl)
    {
        ArgumentException.ThrowIfNullOrEmpty(actorPath);
        ArgumentException.ThrowIfNullOrEmpty(httpUrl);

        lock (_lockObject)
        {
            _remoteActorUrls[actorPath] = httpUrl;
        }
    }

    /// <summary>
    /// Invokes a remote actor and waits for a response.
    /// Returns the default value if the call fails or the response is unsuccessful.
    /// </summary>
    /// <typeparam name="T">The type of the response payload.</typeparam>
    /// <param name="remoteActorPath">The path of the remote actor.</param>
    /// <param name="message">The message to send.</param>
    /// <param name="timeout">An optional timeout for the call.</param>
    /// <returns>The deserialized response, or the default value on failure.</returns>
    public async Task<T?> InvokeAsync<T>(string remoteActorPath, Message message, TimeSpan? timeout = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(remoteActorPath);
        ArgumentNullException.ThrowIfNull(message);

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

    /// <summary>
    /// Sends a message to a remote actor without waiting for response.
    /// Failures are suppressed.
    /// </summary>
    /// <param name="remoteActorPath">The path of the remote actor.</param>
    /// <param name="message">The message to send.</param>
    public async Task SendAsync(string remoteActorPath, Message message)
    {
        ArgumentException.ThrowIfNullOrEmpty(remoteActorPath);
        ArgumentNullException.ThrowIfNull(message);

        try
        {
            await _client.SendMessageAsync(remoteActorPath, message);
        }
        catch
        {
            // Silently fail for fire-and-forget
        }
    }

    /// <summary>
    /// Checks if a remote actor is reachable.
    /// Returns <c>false</c> if the health check fails or throws.
    /// </summary>
    /// <param name="remoteActorPath">The path of the remote actor.</param>
    /// <returns><c>true</c> if the remote actor reports healthy; otherwise <c>false</c>.</returns>
    public async Task<bool> PingAsync(string remoteActorPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(remoteActorPath);

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

    /// <summary>
    /// Releases the underlying HTTP client.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the value returned by the remote call.
    /// </summary>
    public T? Result { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the remote call succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the error message if the remote call failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the elapsed time of the remote call in milliseconds.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteActorCircuitBreaker"/> class.
    /// </summary>
    /// <param name="failureThreshold">Consecutive failures before the circuit opens. Must be positive.</param>
    /// <param name="timeout">How long the circuit stays open before calls resume. Defaults to one minute.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="failureThreshold"/> is not positive.</exception>
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
                state.IsOpen = false;
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
