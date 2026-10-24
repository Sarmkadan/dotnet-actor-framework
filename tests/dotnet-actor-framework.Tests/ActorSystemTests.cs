using DotNetActorFramework.Configuration;
using DotNetActorFramework.Constants;
using DotNetActorFramework.Exceptions;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ActorSystemTests
{
    [Fact]
    public void Constructor_ValidName_SetsProperties()
    {
        // Arrange and Act
        var system = new ActorSystem("TestSystem");

        // Assert
        Assert.NotNull(system);
        Assert.NotEmpty(system.Name);
        Assert.NotEqual(Guid.Empty, system.Id);
        Assert.True(system.IsRunning);
    }

    [Fact]
    public void Constructor_InvalidName_ThrowsArgumentException()
    {
        // Act and Assert
        Assert.Throws<ArgumentException>(() => new ActorSystem(string.Empty));
    }

    [Fact]
    public void Constructor_ValidOptions_CreatesSystem()
    {
        // Arrange
        var options = ActorSystemOptions.CreateDefault();

        // Act
        var system = new ActorSystem("TestSystem", null, options);

        // Assert
        Assert.True(system.IsRunning);
    }

    [Fact]
    public void Constructor_InvalidOptions_ThrowsOptionsValidationListingAllViolations()
    {
        // Arrange
        var options = new ActorSystemOptions
        {
            DefaultTimeoutSeconds = 0,
            MaxActorDepth = 0
        };

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() => new ActorSystem("TestSystem", null, options));

        // Assert
        Assert.Equal(2, exception.Failures.Count);
        Assert.Contains("Default timeout must be greater than zero.", exception.Message);
        Assert.Contains("Max actor depth must be greater than zero.", exception.Message);
    }

    [Fact]
    public void SetDeadLetterHandler_ValidHandler_SetsHandler()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        Action<Envelope> handler = _ => { };

        // Act
        system.SetDeadLetterHandler(handler);

        // Assert
        var retrievedHandler = system.GetDeadLetterHandler();
        Assert.NotNull(retrievedHandler);
    }

    [Fact]
    public void DeadLetters_NewSystem_ShouldBeEmptyWithDefaultCapacity()
    {
        // Arrange and Act
        var system = new ActorSystem("TestSystem");

        // Assert
        system.DeadLetters.Should().NotBeNull();
        system.DeadLetters.Count.Should().Be(0);
        system.DeadLetters.Capacity.Should().Be(MessageConstants.MaxDeadLetterQueueSize);
    }

    [Fact]
    public async Task DispatchAsync_UnknownRecipient_ShouldRouteEnvelopeToDeadLetters()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        var dispatcher = new MessageDispatcher(new MailboxService(new ActorSystemOptions()), new ActorRegistry(), system);
        var recipient = new ActorRef(new ActorPath("/missing"), Guid.NewGuid());
        var envelope = new Envelope(new ControlMessage("ping"), recipient);

        // Act
        var act = () => dispatcher.DispatchAsync(envelope);

        // Assert
        await act.Should().ThrowAsync<MessageDispatchException>();
        system.DeadLetters.GetSnapshot().Should().ContainSingle().Which.Should().BeSameAs(envelope);
    }

    [Fact]
    public async Task DispatchAsync_UnknownRecipient_ShouldNotifyDeadLetterSubscribers()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        var dispatcher = new MessageDispatcher(new MailboxService(new ActorSystemOptions()), new ActorRegistry(), system);
        var received = new List<Envelope>();
        using var _ = system.DeadLetters.Subscribe(received.Add);
        var recipient = new ActorRef(new ActorPath("/missing"), Guid.NewGuid());
        var envelope = new Envelope(new ControlMessage("ping"), recipient);

        // Act
        await Assert.ThrowsAsync<MessageDispatchException>(() => dispatcher.DispatchAsync(envelope));

        // Assert
        received.Should().ContainSingle().Which.Should().BeSameAs(envelope);
    }

    [Fact]
    public void InvokeDeadLetterHandler_WithHandlerThatThrows_ShouldStillRetainEnvelope()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        system.SetDeadLetterHandler(_ => throw new InvalidOperationException("handler failure"));
        var envelope = new Envelope(new ControlMessage("ping"), new ActorRef(new ActorPath("/gone"), Guid.NewGuid()));

        // Act
        var act = () => system.InvokeDeadLetterHandler(envelope);

        // Assert
        act.Should().Throw<InvalidOperationException>();
        system.DeadLetters.GetSnapshot().Should().ContainSingle().Which.Should().BeSameAs(envelope);
    }

    [Fact]
    public void CreateActorAsync_ValidPath_CreatesActor()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        var path = new ActorPath("/TestActor");

        // Act
        var actorRef = system.CreateActorAsync(path).Result;

        // Assert
        Assert.NotNull(actorRef);
    }

    [Fact]
    public void GetActorRef_ValidPath_ReturnsActorRef()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        var path = new ActorPath("/TestActor");
        system.CreateActorAsync(path).Wait();

        // Act
        var actorRef = system.GetActorRef(path);

        // Assert
        Assert.NotNull(actorRef);
    }

    [Fact]
    public void GetActorRef_InvalidPath_ReturnsNull()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        var path = new ActorPath("/NonExistentActor");

        // Act
        var actorRef = system.GetActorRef(path);

        // Assert
        Assert.Null(actorRef);
    }
}
