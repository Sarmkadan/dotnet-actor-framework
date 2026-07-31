namespace DotNetActorFramework.Tests;

using System;
using System.Collections.Generic;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using Xunit;

public class ActorDiscoveryServiceTests
{
    private readonly ActorDiscoveryService _service = new();

    [Fact]
    public void Register_ValidActor_AddsEntry()
    {
        var path = new ActorPath("/my/actor");
        var id = Guid.NewGuid();
        var actorRef = new ActorRef(path, id);
        
        _service.Register(actorRef, new[] { "cap1" }, new[] { "tag1" });
        
        var all = _service.GetAll();
        Assert.Single(all);
        Assert.Equal(actorRef, all[0].ActorRef);
    }

    [Fact]
    public void Unregister_ExistingActor_ReturnsTrueAndRemoves()
    {
        var path = new ActorPath("/my/actor");
        var id = Guid.NewGuid();
        var actorRef = new ActorRef(path, id);
        
        _service.Register(actorRef, new[] { "cap1" });
        var removed = _service.Unregister(actorRef);
        
        Assert.True(removed);
        Assert.Empty(_service.GetAll());
    }

    [Fact]
    public void Discover_RegisteredCapability_ReturnsActor()
    {
        var path = new ActorPath("/my/actor");
        var id = Guid.NewGuid();
        var actorRef = new ActorRef(path, id);
        
        _service.Register(actorRef, new[] { "cap1" });
        
        var discovered = _service.Discover("cap1");
        Assert.Single(discovered);
        Assert.Equal(actorRef, discovered[0]);
    }

    [Fact]
    public void DiscoverByTag_RegisteredTag_ReturnsActor()
    {
        var path = new ActorPath("/my/actor");
        var id = Guid.NewGuid();
        var actorRef = new ActorRef(path, id);
        
        _service.Register(actorRef, new[] { "cap1" }, new[] { "tag1" });
        
        var discovered = _service.DiscoverByTag("tag1");
        Assert.Single(discovered);
        Assert.Equal(actorRef, discovered[0]);
    }

    [Fact]
    public void Register_NullActorRef_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _service.Register(null!, new[] { "cap1" }));
    }

    [Fact]
    public void Register_NullCapabilities_ThrowsArgumentNullException()
    {
        var path = new ActorPath("/my/actor");
        var id = Guid.NewGuid();
        var actorRef = new ActorRef(path, id);
        
        Assert.Throws<ArgumentNullException>(() => _service.Register(actorRef, null!));
    }
}
