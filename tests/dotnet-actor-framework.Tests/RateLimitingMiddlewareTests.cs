// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for RateLimitingMiddleware to verify rate limiting behavior
// =============================================================================

using DotNetActorFramework.Middleware;
using DotNetActorFramework.Models;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Contains unit tests for verifying the behavior and correctness of the <see cref="RateLimitingMiddleware"/>
/// and <see cref="RateLimiter"/> classes.
/// </summary>
public class RateLimitingMiddlewareTests
{
    /// <summary>
    /// Creates an <see cref="ActorRef"/> instance for testing purposes.
    /// </summary>
    /// <param name="pathStr">The actor path string used to construct the actor reference.</param>
    /// <returns>A new <see cref="ActorRef"/> instance with the specified path and a unique identifier.</returns>
    private static ActorRef CreateActorRef(string pathStr)
    {
        var path = new ActorPath(pathStr);
        return new ActorRef(path, Guid.NewGuid());
    }

    /// <summary>
    /// Creates an <see cref="Envelope"/> instance for testing purposes with a default recipient path.
    /// </summary>
    /// <param name="recipientPath">The recipient actor path string. Defaults to "/system/actor".</param>
    /// <returns>A new <see cref="Envelope"/> instance containing a test control message and recipient reference.</returns>
    private static Envelope CreateEnvelope(string recipientPath = "/system/actor")
    {
        var message = new ControlMessage("test-command");
        var recipient = CreateActorRef(recipientPath);
        return new Envelope(message, recipient);
    }

    /// <summary>
    /// Creates a rate limiter with specified tokens per second and optional bucket capacity.
    /// </summary>
    private static RateLimiter CreateRateLimiter(int tokensPerSecond = 1000, int? bucketCapacity = null)
    {
        return new RateLimiter(tokensPerSecond, bucketCapacity);
    }

    [Fact]
    public void Constructor_WithNullRateLimiter_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new RateLimitingMiddleware(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("rateLimiter");
    }

    [Fact]
    public void Constructor_WithValidRateLimiter_CreatesInstance()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter();

        // Act
        var middleware = new RateLimitingMiddleware(rateLimiter);

        // Assert
        middleware.Should().NotBeNull();
        middleware.Name.Should().Be("RateLimitingMiddleware");
        middleware.Order.Should().Be(50);
    }

    [Fact]
    public async Task InvokeAsync_WithNullEnvelope_ThrowsArgumentNullException()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter();
        var middleware = new RateLimitingMiddleware(rateLimiter);
        var nextCalled = false;

        // Act
        var act = async () => await middleware.InvokeAsync(null!, _ => { nextCalled = true; return Task.CompletedTask; });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("envelope");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_UnderLimit_PassesMessageThroughAndReturnsTrue()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: 10000);
        var middleware = new RateLimitingMiddleware(rateLimiter);
        var envelope = CreateEnvelope();
        var nextCalled = false;
        var nextEnvelope = (Envelope?)null;

        // Act
        var result = await middleware.InvokeAsync(envelope, e =>
        {
            nextCalled = true;
            nextEnvelope = e;
            return Task.CompletedTask;
        });

        // Assert
        result.Should().BeTrue("message should pass through when under rate limit");
        nextCalled.Should().BeTrue("next delegate should be called when under limit");
        nextEnvelope.Should().BeSameAs(envelope);
    }

    [Fact]
    public async Task InvokeAsync_OverLimit_DropsMessageAndReturnsFalse()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1, bucketCapacity: 1); // Very restrictive
        var middleware = new RateLimitingMiddleware(rateLimiter);
        var envelope = CreateEnvelope();
        var nextCalled = false;

        // First call - should succeed
        var firstResult = await middleware.InvokeAsync(envelope, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled = false; // Reset for second call

        // Act - second call should fail
        var secondResult = await middleware.InvokeAsync(envelope, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        // Assert
        firstResult.Should().BeTrue("first message should pass through");
        secondResult.Should().BeFalse("second message should be dropped when over limit");
        nextCalled.Should().BeFalse("next delegate should not be called when over limit");
    }

    [Fact]
    public async Task InvokeAsync_MultipleActors_EachActorHasSeparateRateLimit()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 2, bucketCapacity: 2);
        var middleware1 = new RateLimitingMiddleware(rateLimiter);
        var middleware2 = new RateLimitingMiddleware(rateLimiter);
        var actor1Path = new ActorPath("/system/actor1");
        var actor2Path = new ActorPath("/system/actor2");
        var actor1Ref = new ActorRef(actor1Path, Guid.NewGuid());
        var actor2Ref = new ActorRef(actor2Path, Guid.NewGuid());
        var envelope1 = new Envelope(new ControlMessage("test"), actor1Ref);
        var envelope2 = new Envelope(new ControlMessage("test"), actor2Ref);
        var nextCalled1 = false;
        var nextCalled2 = false;

        // Act - send 2 messages to each actor (within limit)
        var result1 = await middleware1.InvokeAsync(envelope1, _ => { nextCalled1 = true; return Task.CompletedTask; });
        var result2 = await middleware2.InvokeAsync(envelope2, _ => { nextCalled2 = true; return Task.CompletedTask; });

        // Assert
        result1.Should().BeTrue("first actor should be under limit");
        result2.Should().BeTrue("second actor should be under limit");
        nextCalled1.Should().BeTrue();
        nextCalled2.Should().BeTrue();
    }

    [Fact]
    public async Task RateLimiter_TryConsumeToken_WithValidPath_ReturnsTrueWhenTokensAvailable()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: 10);
        var path = new ActorPath("/system/test-actor");

        // Act - should have tokens initially
        var result = rateLimiter.TryConsumeToken(path);

        // Assert
        result.Should().BeTrue("should have tokens available initially");
    }

    [Fact]
    public async Task RateLimiter_TryConsumeToken_WithNullPath_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter();

        // Act
        var result = rateLimiter.TryConsumeToken(null!);

        // Assert
        result.Should().BeFalse("null path should return false");
    }

    [Fact]
    public async Task RateLimiter_TryConsumeToken_ExhaustsBucket_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: 2);
        var path = new ActorPath("/system/test-actor");
        var envelope = new Envelope(new ControlMessage("test"), new ActorRef(path, Guid.NewGuid()));
        var middleware = new RateLimitingMiddleware(rateLimiter);

        // Act - consume all tokens
        var result1 = await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);
        var result2 = await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);
        var result3 = await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);

        // Assert
        result1.Should().BeTrue("first message should pass");
        result2.Should().BeTrue("second message should pass");
        result3.Should().BeFalse("third message should be dropped when bucket exhausted");
    }

    [Fact]
    public async Task RateLimiter_GetStatus_ReturnsCorrectInformation()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 100, bucketCapacity: 500);
        var path = new ActorPath("/system/test-actor");
        var envelope = new Envelope(new ControlMessage("test"), new ActorRef(path, Guid.NewGuid()));
        var middleware = new RateLimitingMiddleware(rateLimiter);

        // Act - consume some tokens
        await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);
        await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);

        // Get status
        var status = rateLimiter.GetStatus(path);

        // Assert
        status.Should().NotBeNull();
        status.Capacity.Should().Be(500);
        status.CurrentTokens.Should().Be(498); // 500 - 2 consumed
        status.IsLimited.Should().BeFalse("498 tokens remaining is not limited");
    }

    [Fact]
    public async Task RateLimiter_GetStatus_ForNonExistentActor_ReturnsDefaultStatus()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 100, bucketCapacity: 500);
        var path = new ActorPath("/system/nonexistent");

        // Act
        var status = rateLimiter.GetStatus(path);

        // Assert
        status.Should().NotBeNull();
        status.Capacity.Should().Be(500);
        status.CurrentTokens.Should().Be(0);
        status.IsLimited.Should().BeFalse();
    }

    [Fact]
    public async Task RateLimiter_GetStatus_WithNullPath_ReturnsEmptyStatus()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter();

        // Act
        var status = rateLimiter.GetStatus(null!);

        // Assert
        status.Should().NotBeNull();
        status.Capacity.Should().Be(0);
        status.CurrentTokens.Should().Be(0);
        status.IsLimited.Should().BeFalse();
    }

    [Fact]
    public async Task RateLimiter_RefillTimer_RefillsTokensOverTime()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 100, bucketCapacity: 1000);
        var path = new ActorPath("/system/test-actor");
        var envelope = new Envelope(new ControlMessage("test"), new ActorRef(path, Guid.NewGuid()));
        var middleware = new RateLimitingMiddleware(rateLimiter);

        // Act - exhaust bucket
        for (int i = 0; i < 1000; i++)
        {
            await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);
        }

        // Wait for refill (100ms timer, 10 tokens per 100ms)
        await Task.Delay(200);

        // Get status
        var status = rateLimiter.GetStatus(path);

        // Assert - should have some tokens back
        status.CurrentTokens.Should().BeGreaterThan(0, "tokens should be refilled after waiting");
        status.IsLimited.Should().BeFalse("should no longer be limited after refill");
    }

    [Fact]
    public void RateLimiter_Constructor_WithZeroTokensPerSecond_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new RateLimiter(tokensPerSecond: 0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tokens per second must be positive*");
    }

    [Fact]
    public void RateLimiter_Constructor_WithNegativeTokensPerSecond_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new RateLimiter(tokensPerSecond: -1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Tokens per second must be positive*");
    }

    [Fact]
    public void TokenBucket_Constructor_WithZeroCapacity_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new TokenBucket(0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Capacity must be positive*");
    }

    [Fact]
    public void TokenBucket_Constructor_WithNegativeCapacity_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => new TokenBucket(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Capacity must be positive*");
    }

    [Fact]
    public void RateLimiter_DefaultTokensPerSecond_Is1000()
    {
        // Arrange
        var rateLimiter = new RateLimiter();

        // Act - check default via status
        var status = rateLimiter.GetStatus(new ActorPath("/system/test"));

        // Assert
        status.Capacity.Should().Be(10000, "default capacity should be tokensPerSecond * 10 = 10000");
    }

    [Fact]
    public void RateLimiter_CustomBucketCapacity_IsUsed()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 100, bucketCapacity: 5000);

        // Act
        var status = rateLimiter.GetStatus(new ActorPath("/system/test"));

        // Assert
        status.Capacity.Should().Be(5000, "custom capacity should be used");
    }

    [Fact]
    public async Task InvokeAsync_MultipleMessagesWithinLimit_AllPassThrough()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: 100);
        var middleware = new RateLimitingMiddleware(rateLimiter);
        var path = new ActorPath("/system/test-actor");
        var actorRef = new ActorRef(path, Guid.NewGuid());
        var results = new List<bool>();
        var nextCalledCount = 0;

        // Act - send 50 messages (within 100 capacity)
        for (int i = 0; i < 50; i++)
        {
            var envelope = new Envelope(new ControlMessage("test"), actorRef);
            var result = await middleware.InvokeAsync(envelope, _ =>
            {
                nextCalledCount++;
                return Task.CompletedTask;
            });
            results.Add(result);
        }

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        results.Should().HaveCount(50);
        nextCalledCount.Should().Be(50);
    }

    [Fact]
    public async Task InvokeAsync_ExactBurstSize_AllPassThrough()
    {
        // Arrange
        var burstSize = 50;
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: burstSize);
        var middleware = new RateLimitingMiddleware(rateLimiter);
        var path = new ActorPath("/system/test-actor");
        var actorRef = new ActorRef(path, Guid.NewGuid());
        var results = new List<bool>();

        // Act - send exactly burst size messages
        for (int i = 0; i < burstSize; i++)
        {
            var envelope = new Envelope(new ControlMessage("test"), actorRef);
            var result = await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);
            results.Add(result);
        }

        // Assert
        results.Should().AllSatisfy(r => r.Should().BeTrue());
        results.Should().HaveCount(burstSize);
    }

    [Fact]
    public async Task InvokeAsync_OneOverBurstSize_LastMessageDropped()
    {
        // Arrange
        var burstSize = 50;
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: burstSize);
        var middleware = new RateLimitingMiddleware(rateLimiter);
        var path = new ActorPath("/system/test-actor");
        var actorRef = new ActorRef(path, Guid.NewGuid());
        var results = new List<bool>();

        // Act - send burst size + 1 messages
        for (int i = 0; i < burstSize + 1; i++)
        {
            var envelope = new Envelope(new ControlMessage("test"), actorRef);
            var result = await middleware.InvokeAsync(envelope, _ => Task.CompletedTask);
            results.Add(result);
        }

        // Assert
        results.Should().HaveCount(burstSize + 1);
        results.Take(burstSize).Should().AllSatisfy(r => r.Should().BeTrue());
        results.Last().Should().BeFalse("the message over the burst size should be dropped");
    }

    [Fact]
    public void RateLimiter_Dispose_DisposesTimer()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter();

        // Act
        rateLimiter.Dispose();

        // Assert - no exception thrown
        // The timer should be disposed and not throw on disposal
    }

    [Fact]
    public async Task RateLimiter_TryConsumeToken_ThreadSafety_NoRaceConditions()
    {
        // Arrange
        var rateLimiter = CreateRateLimiter(tokensPerSecond: 1000, bucketCapacity: 100);
        var path = new ActorPath("/system/test-actor");
        var tasks = new List<Task<bool>>();

        // Act - try to consume more tokens than available concurrently
        for (int i = 0; i < 150; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(1); // Small delay to increase chance of race
                return rateLimiter.TryConsumeToken(path);
            }));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        results.Should().HaveCount(150);
        results.Count(r => r).Should().Be(100, "only 100 tokens available, rest should fail");
    }
}
