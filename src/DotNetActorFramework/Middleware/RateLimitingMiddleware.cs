// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that enforces per-actor rate limiting on message delivery.
/// Uses a token bucket algorithm: each actor has its own bucket that refills at a
/// fixed rate and holds up to a configurable maximum number of tokens.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Configuration:</strong> supply a <see cref="RateLimiter"/> constructed with
/// the desired <c>tokensPerSecond</c> and optional <c>bucketCapacity</c>
/// (defaults to <c>tokensPerSecond × 10</c> to absorb short bursts).
/// </para>
/// <para>
/// <strong>When the limit is exceeded:</strong> the message is silently dropped —
/// <see cref="InvokeAsync"/> returns <c>false</c> and <paramref name="next"/> is
/// <em>not</em> called. No error is raised and the sender is not notified.
/// Consider pairing this middleware with <see cref="LoggingMiddleware"/> (lower
/// <c>Order</c> value) so that dropped messages are still visible in logs.
/// </para>
/// </remarks>
public class RateLimitingMiddleware : IActorMiddleware
{
    public string Name => "RateLimitingMiddleware";
    public int Order => 50; // Run after logging but before main processing

    private readonly RateLimiter _rateLimiter;

    /// <summary>
    /// Initializes a new instance of <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="rateLimiter">The rate limiter that manages per-actor token buckets.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rateLimiter"/> is <c>null</c>.</exception>
    public RateLimitingMiddleware(RateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    /// <summary>
    /// Attempts to consume a rate-limit token for the envelope's recipient.
    /// Calls <paramref name="next"/> only when a token is available; otherwise drops the message.
    /// </summary>
    /// <param name="envelope">The envelope to process.</param>
    /// <param name="next">The next stage of the pipeline.</param>
    /// <returns><c>true</c> when the message was forwarded; <c>false</c> when the rate limit was exceeded and the message was dropped.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
    public async Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        // Check if the actor has rate limit tokens available
        if (!_rateLimiter.TryConsumeToken(envelope.Recipient.Path))
        {
            // Rate limit exceeded - drop the message
            return false;
        }

        await next(envelope);
        return true;
    }
}

/// <summary>
/// Token bucket rate limiter for controlling per-actor message processing rates.
/// </summary>
/// <remarks>
/// Each actor path gets its own <see cref="TokenBucket"/> that is created lazily on first use.
/// Buckets are refilled every 100 ms (one-tenth of a second) to smooth out burst distribution.
/// </remarks>
public class RateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly int _tokensPerSecond;
    private readonly int _bucketCapacity;
    private readonly Timer _refillTimer;

    /// <summary>
    /// Initializes a new <see cref="RateLimiter"/>.
    /// </summary>
    /// <param name="tokensPerSecond">
    /// Maximum number of messages to allow per actor per second.
    /// Must be a positive integer. Defaults to <c>1000</c>.
    /// </param>
    /// <param name="bucketCapacity">
    /// Maximum burst size (token bucket capacity). When <c>null</c>, defaults to
    /// <c>tokensPerSecond × 10</c>, allowing up to 10 seconds worth of burst.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="tokensPerSecond"/> is not positive.</exception>
    public RateLimiter(int tokensPerSecond = 1000, int? bucketCapacity = null)
    {
        if (tokensPerSecond <= 0)
            throw new ArgumentException("Tokens per second must be positive.", nameof(tokensPerSecond));

        _tokensPerSecond = tokensPerSecond;
        _bucketCapacity = bucketCapacity ?? tokensPerSecond * 10;
        _buckets = new ConcurrentDictionary<string, TokenBucket>();

        // Refill tokens every 100ms for smoother distribution
        _refillTimer = new Timer(_ => RefillBuckets(), null, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }

    /// <summary>
    /// Tries to consume a token from the actor's rate limit bucket.
    /// </summary>
    public bool TryConsumeToken(ActorPath path)
    {
        if (path == null) return false;
        var pathStr = path.ToString();
        var bucket = _buckets.GetOrAdd(pathStr, _ => new TokenBucket(_bucketCapacity));
        return bucket.TryConsumeToken();
    }

    /// <summary>
    /// Gets the current rate limit status for an actor.
    /// </summary>
    public RateLimitStatus GetStatus(ActorPath path)
    {
        if (path == null) return new RateLimitStatus();
        var pathStr = path.ToString();
        if (_buckets.TryGetValue(pathStr, out var bucket))
        {
            return new RateLimitStatus
            {
                CurrentTokens = bucket.CurrentTokens,
                Capacity = bucket.Capacity,
                IsLimited = bucket.CurrentTokens < 1
            };
        }
        return new RateLimitStatus { Capacity = _bucketCapacity };
    }

    private void RefillBuckets()
    {
        var tokensToAdd = (double)_tokensPerSecond / 10; // Refill 100ms worth
        foreach (var bucket in _buckets.Values)
        {
            bucket.AddTokens(tokensToAdd);
        }
    }

    public void Dispose()
    {
        _refillTimer?.Dispose();
    }
}

/// <summary>
/// Token bucket for a single actor's rate limiting.
/// </summary>
public class TokenBucket
{
    private double _tokens;
    private readonly int _capacity;
    private readonly object _lock = new();

    public TokenBucket(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        _capacity = capacity;
        _tokens = capacity; // Start with full capacity
    }

    public int CurrentTokens
    {
        get
        {
            lock (_lock)
            {
                return (int)_tokens;
            }
        }
    }

    public int Capacity => _capacity;

    public bool TryConsumeToken()
    {
        lock (_lock)
        {
            if (_tokens >= 1)
            {
                _tokens--;
                return true;
            }
            return false;
        }
    }

    public void AddTokens(double amount)
    {
        lock (_lock)
        {
            _tokens = Math.Min(_capacity, _tokens + amount);
        }
    }
}

/// <summary>
/// Rate limit status for an actor.
/// </summary>
public class RateLimitStatus
{
    public int CurrentTokens { get; set; }
    public int Capacity { get; set; }
    public bool IsLimited { get; set; }
}
