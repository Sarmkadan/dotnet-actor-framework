using System;
using DotNetActorFramework.Caching;
using DotNetActorFramework.Models;
using Xunit;

namespace DotNetActorFramework.Tests.Caching;

/// <summary>
/// Tests that verify the cache is correctly invalidated when an actor is stopped or restarted.
/// </summary>
public class ActorCacheServiceLifecycleTests
{
    [Fact]
    public void Cache_ShouldBeInvalidated_WhenActorIsStopped()
    {
        // Arrange: create a cache and store a reference
        var cache = new ActorCacheService();
        var path = new ActorPath("/test/actor");
        var actorRef = new ActorRef(path, Guid.NewGuid());

        cache.Set(path, actorRef);
        Assert.NotNull(cache.Get(path));

        // Act: simulate actor termination by invoking the Invalidate hook
        // (in a real system a supervisor would call this when the actor stops)
        cache.Invalidate(path);

        // Assert: the cache no longer contains the stale reference
        Assert.Null(cache.Get(path));
        Assert.False(cache.Contains(path));
    }
}
