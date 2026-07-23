using System;
using DotNetActorFramework.Caching;
using DotNetActorFramework.Models;
using Xunit;

namespace DotNetActorFramework.Tests.Caching;

public class ActorCacheServiceTests
{
    [Fact]
    public void Invalidate_ShouldRemoveEntryFromCache()
    {
        // Arrange
        var cache = new ActorCacheService();
        var path = new ActorPath("/test/actor");
        var actorRef = new ActorRef(path, Guid.NewGuid());

        cache.Set(path, actorRef);
        Assert.NotNull(cache.Get(path));

        // Act
        cache.Invalidate(path);

        // Assert
        Assert.Null(cache.Get(path));
    }
}
