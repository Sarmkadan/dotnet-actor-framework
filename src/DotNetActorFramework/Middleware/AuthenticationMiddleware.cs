// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware for authenticating message senders.
/// Validates that messages come from authorized sources before passing them to subsequent
/// middleware or the target actor.
/// </summary>
/// <remarks>
/// <para>
/// Authentication is delegated to the <see cref="IAuthenticationProvider"/> supplied
/// at construction time. Built-in providers include:
/// <list type="bullet">
///   <item><see cref="TokenAuthenticationProvider"/> – validates sender-to-token mappings.</item>
///   <item><see cref="WhitelistAuthenticationProvider"/> – allows only explicitly listed senders.</item>
///   <item><see cref="NoOpAuthenticationProvider"/> – allows all senders (testing/development).</item>
/// </list>
/// </para>
/// <para>
/// When authentication fails or the sender identity is absent, <see cref="InvokeAsync"/>
/// returns <c>false</c> and the pipeline is short-circuited — <c>next</c> is not called.
/// </para>
/// </remarks>
public class AuthenticationMiddleware : IActorMiddleware
{
    public string Name => "AuthenticationMiddleware";
    public int Order => 10; // Run early in the pipeline

    private readonly IAuthenticationProvider _authProvider;

    /// <summary>
    /// Initializes a new instance of <see cref="AuthenticationMiddleware"/>.
    /// </summary>
    /// <param name="authProvider">The provider used to authenticate sender identities.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="authProvider"/> is <c>null</c>.</exception>
    public AuthenticationMiddleware(IAuthenticationProvider authProvider)
    {
        _authProvider = authProvider ?? throw new ArgumentNullException(nameof(authProvider));
    }

    /// <summary>
    /// Authenticates the envelope's sender before forwarding to the next pipeline stage.
    /// Returns <c>false</c> without calling <paramref name="next"/> when the sender is
    /// absent or authentication fails.
    /// </summary>
    /// <param name="envelope">The envelope to authenticate.</param>
    /// <param name="next">The next stage of the pipeline, invoked only on success.</param>
    /// <returns><c>true</c> when the sender is authenticated; <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
    public async Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        // Check if sender is authenticated
        var senderId = envelope.Sender?.Path.Name;
        if (string.IsNullOrEmpty(senderId))
            return false; // No sender info

        var isAuthenticated = await _authProvider.AuthenticateAsync(senderId);
        if (!isAuthenticated)
            return false; // Authentication failed

        await next(envelope);
        return true;
    }
}

/// <summary>
/// Interface for authentication providers.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Authenticates a sender ID.
    /// </summary>
    Task<bool> AuthenticateAsync(string senderId);

    /// <summary>
    /// Validates an authentication token.
    /// </summary>
    Task<bool> ValidateTokenAsync(string token);
}

/// <summary>
/// Simple token-based authentication provider.
/// </summary>
public class TokenAuthenticationProvider : IAuthenticationProvider
{
    private readonly HashSet<string> _validTokens;
    private readonly Dictionary<string, string> _senderTokens = [];
    private readonly object _lockObject = new();

    public TokenAuthenticationProvider(params string[] validTokens)
    {
        if (validTokens == null || validTokens.Length == 0)
            throw new ArgumentException("At least one valid token must be provided.", nameof(validTokens));

        _validTokens = new HashSet<string>(validTokens);
    }

    /// <summary>
    /// Registers a sender with an authentication token.
    /// </summary>
    public void RegisterSender(string senderId, string token)
    {
        if (string.IsNullOrWhiteSpace(senderId) || string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Sender ID and token cannot be empty.");

        lock (_lockObject)
        {
            _senderTokens[senderId] = token;
        }
    }

    public Task<bool> AuthenticateAsync(string senderId)
    {
        if (string.IsNullOrWhiteSpace(senderId))
            return Task.FromResult(false);

        lock (_lockObject)
        {
            if (_senderTokens.TryGetValue(senderId, out var token))
                return Task.FromResult(_validTokens.Contains(token));
        }

        return Task.FromResult(false);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return Task.FromResult(false);

        return Task.FromResult(_validTokens.Contains(token));
    }
}

/// <summary>
/// Whitelist-based authentication provider.
/// Only allows messages from whitelisted senders.
/// </summary>
public class WhitelistAuthenticationProvider : IAuthenticationProvider
{
    private readonly HashSet<string> _whitelist;
    private readonly object _lockObject = new();

    public WhitelistAuthenticationProvider(params string[] allowedSenders)
    {
        if (allowedSenders == null || allowedSenders.Length == 0)
            throw new ArgumentException("At least one allowed sender must be provided.", nameof(allowedSenders));

        _whitelist = new HashSet<string>(allowedSenders);
    }

    /// <summary>
    /// Adds a sender to the whitelist.
    /// </summary>
    public void AddSender(string senderId)
    {
        if (string.IsNullOrWhiteSpace(senderId))
            throw new ArgumentException("Sender ID cannot be empty.", nameof(senderId));

        lock (_lockObject)
        {
            _whitelist.Add(senderId);
        }
    }

    /// <summary>
    /// Removes a sender from the whitelist.
    /// </summary>
    public void RemoveSender(string senderId)
    {
        lock (_lockObject)
        {
            _whitelist.Remove(senderId);
        }
    }

    public Task<bool> AuthenticateAsync(string senderId)
    {
        if (string.IsNullOrWhiteSpace(senderId))
            return Task.FromResult(false);

        lock (_lockObject)
        {
            return Task.FromResult(_whitelist.Contains(senderId));
        }
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        // Not used by whitelist provider
        return Task.FromResult(false);
    }
}

/// <summary>
/// No-op authentication provider that allows all messages.
/// Useful for testing and development environments.
/// </summary>
public class NoOpAuthenticationProvider : IAuthenticationProvider
{
    public Task<bool> AuthenticateAsync(string senderId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        return Task.FromResult(true);
    }
}
