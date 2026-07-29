// =============================================================================
// Author: Automated Generation
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ActorRegistryTests
{
    private static ActorRef CreateActorRef(string pathString)
    {
        // Assuming ActorPath has a public constructor that accepts a string.
        var path = new ActorPath(pathString);
        return new ActorRef(path, Guid.NewGuid());
    }

    [Fact]
    public void Register_AddsActorAndUpdatesIndices()
    {
        var registry = new ActorRegistry();
        var actor = CreateActorRef("root/actor1");

        registry.Register(actor);

        registry.GetByPath(actor.Path).Should().BeSameAs(actor);
        registry.GetById(actor.Id).Should().BeSameAs(actor);
        registry.Contains(actor.Path).Should().BeTrue();
        registry.GetCount().Should().Be(1);
    }

    [Fact]
    public void Register_NullActor_ThrowsArgumentNullException()
    {
        var registry = new ActorRegistry();

        Action act = () => registry.Register(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Register_DuplicatePath_ThrowsInvalidOperationException()
    {
        var registry = new ActorRegistry();
        var actor1 = CreateActorRef("root/dup");
        var actor2 = CreateActorRef("root/dup"); // same path, different Id

        registry.Register(actor1);
        Action act = () => registry.Register(actor2);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Unregister_RemovesActor()
    {
        var registry = new ActorRegistry();
        var actor = CreateActorRef("root/toRemove");

        registry.Register(actor);
        registry.GetCount().Should().Be(1);

        registry.Unregister(actor);

        registry.GetByPath(actor.Path).Should().BeNull();
        registry.GetById(actor.Id).Should().BeNull();
        registry.Contains(actor.Path).Should().BeFalse();
        registry.GetCount().Should().Be(0);
    }

    [Fact]
    public void GetChildren_ReturnsDirectChildren()
    {
        var registry = new ActorRegistry();

        var parent = CreateActorRef("root");
        var child1 = CreateActorRef("root/child1");
        var child2 = CreateActorRef("root/child2");
        var unrelated = CreateActorRef("other/actor");

        registry.Register(parent);
        registry.Register(child1);
        registry.Register(child2);
        registry.Register(unrelated);

        var children = registry.GetChildren(parent.Path);
        children.Should().HaveCount(2);
        children.Select(a => a.Path).Should().Contain(new[] { child1.Path, child2.Path });
    }

    [Fact]
    public void GetDescendants_ReturnsAllDescendants()
    {
        var registry = new ActorRegistry();

        var root = CreateActorRef("root");
        var child = CreateActorRef("root/child");
        var grandChild = CreateActorRef("root/child/grand");

        registry.Register(root);
        registry.Register(child);
        registry.Register(grandChild);

        var descendants = registry.GetDescendants(root.Path);
        descendants.Should().HaveCount(2);
        descendants.Select(a => a.Path).Should().Contain(new[] { child.Path, grandChild.Path });
    }

    [Fact]
    public void GetAll_ReturnsAllRegisteredActors()
    {
        var registry = new ActorRegistry();

        var actors = new[]
        {
            CreateActorRef("a1"),
            CreateActorRef("a2"),
            CreateActorRef("a3")
        };

        foreach (var a in actors) registry.Register(a);

        var all = registry.GetAll();
        all.Should().HaveCount(3);
        all.Should().Contain(actors);
    }

    [Fact]
    public void Clear_RemovesAllRegistrations()
    {
        var registry = new ActorRegistry();

        registry.Register(CreateActorRef("x"));
        registry.Register(CreateActorRef("y"));
        registry.GetCount().Should().BeGreaterThan(0);

        registry.Clear();

        registry.GetCount().Should().Be(0);
        registry.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void GetByPath_Null_ThrowsArgumentNullException()
    {
        var registry = new ActorRegistry();

        Action act = () => registry.GetByPath(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetChildren_Null_ThrowsArgumentNullException()
    {
        var registry = new ActorRegistry();

        Action act = () => registry.GetChildren(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetDescendants_Null_ThrowsArgumentNullException()
    {
        var registry = new ActorRegistry();

        Action act = () => registry.GetDescendants(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Contains_Null_ThrowsArgumentNullException()
    {
        var registry = new ActorRegistry();

        Action act = () => registry.Contains(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
