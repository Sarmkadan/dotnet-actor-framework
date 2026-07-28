using DotNetActorFramework.Models;
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
    public void CreateActorAsync_ValidPath_CreatesActor()
    {
        // Arrange
        var system = new ActorSystem("TestSystem");
        var path = new ActorPath("TestActor");

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
        var path = new ActorPath("TestActor");
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
        var path = new ActorPath("NonExistentActor");

        // Act
        var actorRef = system.GetActorRef(path);

        // Assert
        Assert.Null(actorRef);
    }
}
