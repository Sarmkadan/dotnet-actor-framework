using DotNetActorFramework.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests;

public class ConfigurationExceptionExtensionsTests
{
    [Fact]
    public void IsActorSystemConfigurationException_ShouldReturnTrue_WhenExceptionIsActorSystemConfigurationException()
    {
        // Arrange
        var exception = new ActorSystemConfigurationException("Test actor system configuration error");

        // Act
        var result = exception.IsActorSystemConfigurationException();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsActorSystemConfigurationException_ShouldReturnFalse_WhenExceptionIsMailboxConfigurationException()
    {
        // Arrange
        var exception = new MailboxConfigurationException("Test mailbox configuration error");

        // Act
        var result = exception.IsActorSystemConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActorSystemConfigurationException_ShouldReturnFalse_WhenExceptionIsPersistenceConfigurationException()
    {
        // Arrange
        var exception = new PersistenceConfigurationException("Test persistence configuration error");

        // Act
        var result = exception.IsActorSystemConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsActorSystemConfigurationException_ShouldReturnFalse_WhenExceptionIsGenericConfigurationException()
    {
        // Arrange
        var exception = new ConfigurationException("Generic configuration error");

        // Act
        var result = exception.IsActorSystemConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsMailboxConfigurationException_ShouldReturnTrue_WhenExceptionIsMailboxConfigurationException()
    {
        // Arrange
        var exception = new MailboxConfigurationException("Test mailbox configuration error");

        // Act
        var result = exception.IsMailboxConfigurationException();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsMailboxConfigurationException_ShouldReturnFalse_WhenExceptionIsActorSystemConfigurationException()
    {
        // Arrange
        var exception = new ActorSystemConfigurationException("Test actor system configuration error");

        // Act
        var result = exception.IsMailboxConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsMailboxConfigurationException_ShouldReturnFalse_WhenExceptionIsPersistenceConfigurationException()
    {
        // Arrange
        var exception = new PersistenceConfigurationException("Test persistence configuration error");

        // Act
        var result = exception.IsMailboxConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsMailboxConfigurationException_ShouldReturnFalse_WhenExceptionIsGenericConfigurationException()
    {
        // Arrange
        var exception = new ConfigurationException("Generic configuration error");

        // Act
        var result = exception.IsMailboxConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPersistenceConfigurationException_ShouldReturnTrue_WhenExceptionIsPersistenceConfigurationException()
    {
        // Arrange
        var exception = new PersistenceConfigurationException("Test persistence configuration error");

        // Act
        var result = exception.IsPersistenceConfigurationException();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPersistenceConfigurationException_ShouldReturnFalse_WhenExceptionIsActorSystemConfigurationException()
    {
        // Arrange
        var exception = new ActorSystemConfigurationException("Test actor system configuration error");

        // Act
        var result = exception.IsPersistenceConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPersistenceConfigurationException_ShouldReturnFalse_WhenExceptionIsMailboxConfigurationException()
    {
        // Arrange
        var exception = new MailboxConfigurationException("Test mailbox configuration error");

        // Act
        var result = exception.IsPersistenceConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPersistenceConfigurationException_ShouldReturnFalse_WhenExceptionIsGenericConfigurationException()
    {
        // Arrange
        var exception = new ConfigurationException("Generic configuration error");

        // Act
        var result = exception.IsPersistenceConfigurationException();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetConfigurationType_ShouldReturnActorSystem_WhenExceptionIsActorSystemConfigurationException()
    {
        // Arrange
        var exception = new ActorSystemConfigurationException("Actor system error");

        // Act
        var result = exception.GetConfigurationType();

        // Assert
        result.Should().Be("Actor System");
    }

    [Fact]
    public void GetConfigurationType_ShouldReturnMailbox_WhenExceptionIsMailboxConfigurationException()
    {
        // Arrange
        var exception = new MailboxConfigurationException("Mailbox error");

        // Act
        var result = exception.GetConfigurationType();

        // Assert
        result.Should().Be("Mailbox");
    }

    [Fact]
    public void GetConfigurationType_ShouldReturnPersistence_WhenExceptionIsPersistenceConfigurationException()
    {
        // Arrange
        var exception = new PersistenceConfigurationException("Persistence error");

        // Act
        var result = exception.GetConfigurationType();

        // Assert
        result.Should().Be("Persistence");
    }

    [Fact]
    public void GetConfigurationType_ShouldReturnUnknown_WhenExceptionIsGenericConfigurationException()
    {
        // Arrange
        var exception = new ConfigurationException("Generic error");

        // Act
        var result = exception.GetConfigurationType();

        // Assert
        result.Should().Be("Unknown");
    }

    [Fact]
    public void IsActorSystemConfigurationException_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        ConfigurationException exception = null!;

        // Act
        Action act = () => exception.IsActorSystemConfigurationException();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsMailboxConfigurationException_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        ConfigurationException exception = null!;

        // Act
        Action act = () => exception.IsMailboxConfigurationException();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsPersistenceConfigurationException_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        ConfigurationException exception = null!;

        // Act
        Action act = () => exception.IsPersistenceConfigurationException();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetConfigurationType_ThrowsArgumentNullException_WhenExceptionIsNull()
    {
        // Arrange
        ConfigurationException exception = null!;

        // Act
        Action act = () => exception.GetConfigurationType();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
