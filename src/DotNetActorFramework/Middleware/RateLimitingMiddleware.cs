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
/// <see cref="InvokeAsync"/> returns <c>false</c> and <c>next</c> is
/// <em>not</em> called. No error is raised and the sender is not notified.
/// Consider pairing this middleware with <see cref="LoggingMiddleware"/> (lower
/// <c>Order</c> value) so that dropped messages are still visible in logs.
/// </para>
/// </remarks>
public class RateLimitingMiddleware : IActorMiddleware
{
    /// <summary>
    /// Gets the unique name identifying this middleware in the pipeline.
    /// </summary>
    public string Name => "RateLimitingMiddleware";

    /// <summary>
    /// Gets the execution order of this middleware within the pipeline. Lower values run first.
    /// </summary>
    public int Order => 50; // Run after logging but before main processing

    private readonly RateLimiter _rateLimiter;

    /// <summary>
    /// Initializes a new instance of <see cref="RateLimitingMiddleware"/>.
    /// </summary>
    /// <param name="rateLimiter">The rate limiter that manages per-actor token buckets.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="rateLimiter"/> is <c>null</c>.</exception>
    public RateLimitingMiddleware(RateLimiter rateLimiter)
    {
        ArgumentNullException.ThrowIfNull(rateLimiter);

        _rateLimiter = rateLimiter;
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
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(next);

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
public class RateLimiter : IDisposable
{
    /// <summary>The default maximum number of messages allowed per actor per second.</summary>
    private const int DefaultTokensPerSecond = 1000;

    /// <summary>The number of seconds of traffic the default bucket capacity absorbs as a burst.</summary>
    private const int DefaultBurstSeconds = 10;

    /// <summary>The interval between bucket refills, in milliseconds.</summary>
    private const int RefillIntervalMilliseconds = 100;

    /// <summary>The number of milliseconds in one second.</summary>
    private const int MillisecondsPerSecond = 1000;

    /// <summary>The number of refills performed each second.</summary>
    private const int RefillsPerSecond = MillisecondsPerSecond / RefillIntervalMilliseconds;

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
    public RateLimiter(int tokensPerSecond = DefaultTokensPerSecond, int? bucketCapacity = null)
    {
        if (tokensPerSecond <= 0)
            throw new ArgumentException("Tokens per second must be positive.", nameof(tokensPerSecond));

        _tokensPerSecond = tokensPerSecond;
        _bucketCapacity = bucketCapacity ?? tokensPerSecond * DefaultBurstSeconds;
        _buckets = new ConcurrentDictionary<string, TokenBucket>();

        // Refill tokens every RefillIntervalMilliseconds for smoother distribution
        _refillTimer = new Timer(
            _ => RefillBuckets(),
            null,
            TimeSpan.FromMilliseconds(RefillIntervalMilliseconds),
            TimeSpan.FromMilliseconds(RefillIntervalMilliseconds));
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
        var tokensToAdd = (double)_tokensPerSecond / RefillsPerSecond; // Refill one interval's worth
        foreach (var bucket in _buckets.Values)
        {
            bucket.AddTokens(tokensToAdd);
        }
    }

    /// <summary>
    /// Stops the background refill timer and releases its resources.
    /// </summary>
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

    /// <summary>
    /// Initializes a new <see cref="TokenBucket"/> that starts full.
    /// </summary>
    /// <param name="capacity">Maximum number of tokens the bucket can hold. Must be positive.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="capacity"/> is not positive.</exception>
    public TokenBucket(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity must be positive.", nameof(capacity));

        _capacity = capacity;
        _tokens = capacity; // Start with full capacity
    }

    /// <summary>
    /// Gets the number of whole tokens currently available in the bucket.
    /// </summary>
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

    /// <summary>
    /// Gets the maximum number of tokens the bucket can hold.
    /// </summary>
    public int Capacity => _capacity;

    /// <summary>
    /// Attempts to take one token from the bucket.
    /// </summary>
    /// <returns><c>true</c> when a token was consumed; <c>false</c> when fewer than one token is available.</returns>
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

    /// <summary>
    /// Adds tokens to the bucket, capped at <see cref="Capacity"/>.
    /// </summary>
    /// <param name="amount">The number of tokens to add.</param>
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
    /// <summary>
    /// Gets or sets the number of whole tokens currently available.
    /// </summary>
    public int CurrentTokens { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of tokens the actor's bucket can hold.
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the actor currently has no tokens available.
    /// </summary>
    public bool IsLimited { get; set; }
}
