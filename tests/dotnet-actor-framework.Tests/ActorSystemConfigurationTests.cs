using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Repository;
using DotNetActorFramework.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ActorSystemConfigurationTests
{
    [Fact]
    public async Task InitializeAsync_HappyPath_ReturnsActorSystem()
    {
        // Arrange
        var options = new ActorSystemOptions();
        var registry = new Mock<ActorRegistry>().Object;
        var mailboxService = new Mock<MailboxService>().Object;
        var dispatcher = new Mock<MessageDispatcher>().Object;
        var supervisionService = new Mock<SupervisionService>().Object;
        var stateRepository = new Mock<ActorStateRepository>().Object;
        var messageRepository = new Mock<MessagePersistenceRepository>().Object;
        var metricsRepository = new Mock<ActorMetricsRepository>().Object;
        var connectionManager = new Mock<ConnectionManager>().Object;
        var logger = new Mock<ILogger<ActorSystemConfiguration>>().Object;

        var configuration = new ActorSystemConfiguration(
            options,
            registry,
            mailboxService,
            dispatcher,
            supervisionService,
            stateRepository,
            messageRepository,
            metricsRepository,
            connectionManager,
            logger);

        // Act
        var actorSystem = await configuration.InitializeAsync();

        // Assert
        Assert.NotNull(actorSystem);
    }

    [Fact]
    public async Task CreateActorAsync_HappyPath_ReturnsActorRef()
    {
        // Arrange
        var options = new ActorSystemOptions();
        var registry = new Mock<ActorRegistry>().Object;
        var mailboxService = new Mock<MailboxService>().Object;
        var dispatcher = new Mock<MessageDispatcher>().Object;
        var supervisionService = new Mock<SupervisionService>().Object;
        var stateRepository = new Mock<ActorStateRepository>().Object;
        var messageRepository = new Mock<MessagePersistenceRepository>().Object;
        var metricsRepository = new Mock<ActorMetricsRepository>().Object;
        var connectionManager = new Mock<ConnectionManager>().Object;
        var logger = new Mock<ILogger<ActorSystemConfiguration>>().Object;

        var configuration = new ActorSystemConfiguration(
            options,
            registry,
            mailboxService,
            dispatcher,
            supervisionService,
            stateRepository,
            messageRepository,
            metricsRepository,
            connectionManager,
            logger);

        await configuration.InitializeAsync();

        // Act
        var actorRef = await configuration.CreateActorAsync(new ActorPath("test"));

        // Assert
        Assert.NotNull(actorRef);
    }

    [Fact]
    public void GetActorSystem_HappyPath_ReturnsActorSystem()
    {
        // Arrange
        var options = new ActorSystemOptions();
        var registry = new Mock<ActorRegistry>().Object;
        var mailboxService = new Mock<MailboxService>().Object;
        var dispatcher = new Mock<MessageDispatcher>().Object;
        var supervisionService = new Mock<SupervisionService>().Object;
        var stateRepository = new Mock<ActorStateRepository>().Object;
        var messageRepository = new Mock<MessagePersistenceRepository>().Object;
        var metricsRepository = new Mock<ActorMetricsRepository>().Object;
        var connectionManager = new Mock<ConnectionManager>().Object;
        var logger = new Mock<ILogger<ActorSystemConfiguration>>().Object;

        var configuration = new ActorSystemConfiguration(
            options,
            registry,
            mailboxService,
            dispatcher,
            supervisionService,
            stateRepository,
            messageRepository,
            metricsRepository,
            connectionManager,
            logger);

        configuration.InitializeAsync().Wait();

        // Act
        var actorSystem = configuration.GetActorSystem();

        // Assert
        Assert.NotNull(actorSystem);
    }

    [Fact]
    public void GetActorSystem_NullActorSystem_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = new ActorSystemOptions();
        var registry = new Mock<ActorRegistry>().Object;
        var mailboxService = new Mock<MailboxService>().Object;
        var dispatcher = new Mock<MessageDispatcher>().Object;
        var supervisionService = new Mock<SupervisionService>().Object;
        var stateRepository = new Mock<ActorStateRepository>().Object;
        var messageRepository = new Mock<MessagePersistenceRepository>().Object;
        var metricsRepository = new Mock<ActorMetricsRepository>().Object;
        var connectionManager = new Mock<ConnectionManager>().Object;
        var logger = new Mock<ILogger<ActorSystemConfiguration>>().Object;

        var configuration = new ActorSystemConfiguration(
            options,
            registry,
            mailboxService,
            dispatcher,
            supervisionService,
            stateRepository,
            messageRepository,
            metricsRepository,
            connectionManager,
            logger);

        // Act and Assert
        Assert.Throws<InvalidOperationException>(() => configuration.GetActorSystem());
    }
}
