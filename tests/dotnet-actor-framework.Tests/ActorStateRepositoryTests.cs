using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence.Abstractions;
using DotNetActorFramework.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using DotNetActorFramework.Repository;
using Xunit;

namespace DotNetActorFramework.Tests
{
    public class ActorStateRepositoryTests
    {
        private readonly Mock<ISnapshotStore> _snapshotStoreMock;
        private readonly Mock<IEventJournal> _eventJournalMock;
        private readonly PersistenceService _persistenceService;
        private readonly Mock<ILogger<ActorStateRepository>> _mockLogger;
        private readonly ActorStateRepository _repository;

        public ActorStateRepositoryTests()
        {
            _snapshotStoreMock = new Mock<ISnapshotStore>();
            _eventJournalMock = new Mock<IEventJournal>();
            _persistenceService = new PersistenceService(_snapshotStoreMock.Object, _eventJournalMock.Object);
            _mockLogger = new Mock<ILogger<ActorStateRepository>>();
            _repository = new ActorStateRepository(_persistenceService, _mockLogger.Object);
        }

        [Fact]
        public void Properties_ReturnDefaultValues()
        {
            // Assert
            Assert.Equal(Guid.Empty, _repository.ActorId);
            Assert.Equal(ActorPath.Parse("/default"), _repository.ActorPath);
            Assert.IsType<Dictionary<string, object>>(_repository.State);
            Assert.Equal(DateTime.MinValue, _repository.SavedAt);
            Assert.Equal(0L, _repository.SequenceNr);
            Assert.Equal(0, _repository.Version);
        }

        [Fact]
        public async Task SaveStateAsync_NullActorId_ThrowsArgumentException()
        {
            // Arrange
            var actorId = Guid.Empty;
            var actorPath = ActorPath.Parse("/test");
            var state = new Dictionary<string, object> { { "key", "value" } };
            long sequenceNr = 1;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.SaveStateAsync(actorId, actorPath, state, sequenceNr));
        }

        [Fact]
        public async Task SaveStateAsync_Success_ReturnsTrue()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            var state = new Dictionary<string, object> { { "key", "value" } };
            long sequenceNr = 1;
            _snapshotStoreMock.Setup(x => x.SaveSnapshotAsync(
                    It.Is<ActorSnapshot>(s =>
                        s.ActorId == actorId &&
                        s.ActorPath == actorPath.ToString() &&
                        s.SequenceNr == sequenceNr)))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _repository.SaveStateAsync(actorId, actorPath, state, sequenceNr);

            // Assert
            Assert.True(result);
            _snapshotStoreMock.Verify();
        }

        [Fact]
        public async Task SaveStateAsync_Failure_ReturnsFalse()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            var state = new Dictionary<string, object> { { "key", "value" } };
            long sequenceNr = 1;
            _snapshotStoreMock.Setup(x => x.SaveSnapshotAsync(
                    It.Is<ActorSnapshot>(s =>
                        s.ActorId == actorId &&
                        s.ActorPath == actorPath.ToString() &&
                        s.SequenceNr == sequenceNr)))
                .ThrowsAsync(new Exception("Some error"))
                .Verifiable();

            // Act
            var result = await _repository.SaveStateAsync(actorId, actorPath, state, sequenceNr);

            // Assert
            Assert.False(result);
            _snapshotStoreMock.Verify();
        }

        [Fact]
        public async Task LoadStateAsync_NullActorId_ThrowsArgumentException()
        {
            // Arrange
            var actorId = Guid.Empty;
            var actorPath = ActorPath.Parse("/test");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.LoadStateAsync(actorId, actorPath));
        }

        [Fact]
        public async Task LoadStateAsync_StateExists_ReturnsDeserializedState()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            var stateDict = new Dictionary<string, object> { { "key", "value" } };
            var serializedState = System.Text.Json.JsonSerializer.Serialize(stateDict);
            var snapshot = new ActorSnapshot(
                actorId,
                actorPath.ToString(),
                serializedState,
                1,
                DateTime.UtcNow);

            _snapshotStoreMock.Setup(x => x.LoadLatestSnapshotAsync(
                    actorId, actorPath.ToString()))
                .ReturnsAsync(snapshot)
                .Verifiable();

            // Act
            var result = await _repository.LoadStateAsync(actorId, actorPath);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(stateDict, result);
            _snapshotStoreMock.Verify();
        }

        [Fact]
        public async Task LoadStateAsync_NoStateExists_ReturnsNull()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            _snapshotStoreMock.Setup(x => x.LoadLatestSnapshotAsync(
                    actorId, actorPath.ToString()))
                .ReturnsAsync((ActorSnapshot)null)
                .Verifiable();

            // Act
            var result = await _repository.LoadStateAsync(actorId, actorPath);

            // Assert
            Assert.Null(result);
            _snapshotStoreMock.Verify();
        }

        [Fact]
        public async Task LoadStateAsync_Exception_ReturnsNull()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            _snapshotStoreMock.Setup(x => x.LoadLatestSnapshotAsync(
                    actorId, actorPath.ToString()))
                .ThrowsAsync(new Exception("Some error"))
                .Verifiable();

            // Act
            var result = await _repository.LoadStateAsync(actorId, actorPath);

            // Assert
            Assert.Null(result);
            _snapshotStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteStateAsync_NullActorId_ThrowsArgumentException()
        {
            // Arrange
            var actorId = Guid.Empty;
            var actorPath = ActorPath.Parse("/test");

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _repository.DeleteStateAsync(actorId, actorPath));
        }

        [Fact]
        public async Task DeleteStateAsync_Success_ReturnsTrue()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            _snapshotStoreMock.Setup(x => x.DeleteAllSnapshotsAsync(
                    actorId, actorPath.ToString()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _repository.DeleteStateAsync(actorId, actorPath);

            // Assert
            Assert.True(result);
            _snapshotStoreMock.Verify();
        }

        [Fact]
        public async Task DeleteStateAsync_Failure_ReturnsFalse()
        {
            // Arrange
            var actorId = Guid.NewGuid();
            var actorPath = ActorPath.Parse("/test");
            _snapshotStoreMock.Setup(x => x.DeleteAllSnapshotsAsync(
                    actorId, actorPath.ToString()))
                .ThrowsAsync(new Exception("Some error"))
                .Verifiable();

            // Act
            var result = await _repository.DeleteStateAsync(actorId, actorPath);

            // Assert
            Assert.False(result);
            _snapshotStoreMock.Verify();
        }
    }
}