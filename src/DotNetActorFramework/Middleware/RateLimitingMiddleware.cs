// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that enforces rate limiting on message processing.
/// Prevents actors from being overwhelmed by controlling the rate at which messages are delivered.
/// Uses token bucket algorithm for fair distribution of processing capacity.
/// </summary>
public class RateLimitingMiddleware : IActorMiddleware
{
    public string Name => "RateLimitingMiddleware";
    public int Order => 50; // Run after logging but before main processing

    private readonly RateLimiter _rateLimiter;

    public RateLimitingMiddleware(RateLimiter rateLimiter)
    {
        _rateLimiter = rateLimiter ?? throw new ArgumentNullException(nameof(rateLimiter));
    }

    public async Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        // Check if the actor has rate limit tokens available
        if (!_rateLimiter.TryConsumeToken(envelope.RecipientPath))
        {
            // Rate limit exceeded - drop the message
            return false;
        }

        await next(envelope);
        return true;
    }
}

/// <summary>
/// Token bucket rate limiter for controlling message processing rates.
/// </summary>
public class RateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets;
    private readonly int _tokensPerSecond;
    private readonly int _bucketCapacity;
    private readonly Timer _refillTimer;

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
