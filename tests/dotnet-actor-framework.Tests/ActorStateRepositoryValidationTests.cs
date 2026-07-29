namespace DotNetActorFramework.Tests;

using System;
using System.Collections.Generic;
using DotNetActorFramework.Models;
using DotNetActorFramework.Repository;
using DotNetActorFramework.Persistence;
using DotNetActorFramework.Persistence.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ActorStateRepositoryValidationTests
{
    private readonly Mock<ISnapshotStore> _snapshotStoreMock = new();
    private readonly Mock<IEventJournal> _eventJournalMock = new();
    private readonly PersistenceService _persistenceService;
    private readonly ActorStateRepository _repository;

    public ActorStateRepositoryValidationTests()
    {
        _persistenceService = new PersistenceService(_snapshotStoreMock.Object, _eventJournalMock.Object);
        _repository = new ActorStateRepository(_persistenceService);
    }

    [Fact]
    public void Validate_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((ActorStateRepository)null!).Validate());
    }

    [Fact]
    public void Validate_DefaultRepository_ReturnsErrors()
    {
        // The default repository initializes with invalid defaults (Guid.Empty, etc.)
        var problems = _repository.Validate();
        Assert.NotEmpty(problems);
    }

    [Fact]
    public void IsValid_DefaultRepository_ReturnsFalse()
    {
        // Since it has errors, it should be false
        Assert.False(_repository.IsValid());
    }

    [Fact]
    public void IsValid_NullRepository_ReturnsFalse()
    {
        // Based on the code implementation: value?.Validate().Count == 0
        // If value is null, it seems it returns false instead of throwing.
        Assert.False(((ActorStateRepository)null!).IsValid());
    }

    [Fact]
    public void EnsureValid_DefaultRepository_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _repository.EnsureValid());
    }

    [Fact]
    public void EnsureValid_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ((ActorStateRepository)null!).EnsureValid());
    }
}
