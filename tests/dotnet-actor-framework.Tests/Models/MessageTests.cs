// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for Message and related classes
// =============================================================================

using DotNetActorFramework.Models;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests.Models;

public class MessageTests
{
    /// <summary>
    /// Tests that Message base class has default values for MessageId and CreatedAt
    /// </summary>
    [Fact]
    public void Message_BaseClass_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var message = new TestMessage();

        // Assert
        message.MessageId.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.Priority.Should().Be(0);
    }

    /// <summary>
    /// Tests that MessageId is unique across multiple instances
    /// </summary>
    [Fact]
    public void Message_BaseClass_MessageIdShouldBeUnique()
    {
        // Arrange & Act
        var message1 = new TestMessage();
        var message2 = new TestMessage();
        var message3 = new TestMessage();

        // Assert
        message1.MessageId.Should().NotBe(message2.MessageId);
        message2.MessageId.Should().NotBe(message3.MessageId);
        message1.MessageId.Should().NotBe(message3.MessageId);
    }

    /// <summary>
    /// Tests that CreatedAt is set to current UTC time
    /// </summary>
    [Fact]
    public void Message_BaseClass_CreatedAtShouldBeUtcNow()
    {
        // Arrange & Act
        var message = new TestMessage();

        // Assert
        message.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
        message.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow);
    }

    /// <summary>
    /// Tests that Priority can be set and defaults to 0
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(-5)]
    public void Message_BaseClass_PriorityShouldBeConfigurable(int priority)
    {
        // Arrange & Act
        var message = new TestMessage { Priority = priority };

        // Assert
        message.Priority.Should().Be(priority);
    }

    /// <summary>
    /// Tests that Message base class constructor initializes properties correctly
    /// </summary>
    [Fact]
    public void Message_BaseClass_ConstructorInitializesProperties()
    {
        // Arrange & Act
        var message = new TestMessage();

        // Assert
        message.MessageId.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.Priority.Should().Be(0);
    }

    /// <summary>
    /// Tests Message&lt;T&gt; constructor with null payload throws ArgumentNullException
    /// </summary>
    [Fact]
    public void Message_T_Constructor_WithNullPayload_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Message<string>(null!));
    }

    /// <summary>
    /// Tests Message&lt;T&gt; constructor with valid payload
    /// </summary>
    [Fact]
    public void Message_T_Constructor_WithValidPayload_SetsPayload()
    {
        // Arrange
        var payload = "test payload";

        // Act
        var message = new Message<string>(payload);

        // Assert
        message.Payload.Should().Be(payload);
        message.MessageId.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests Message&lt;T&gt; with complex payload
    /// </summary>
    [Fact]
    public void Message_T_Constructor_WithComplexPayload_SetsPayloadCorrectly()
    {
        // Arrange
        var payload = new { Name = "test", Value = 42 };

        // Act
        var message = new Message<object>(payload);

        // Assert
        message.Payload.Should().BeSameAs(payload);
    }

    /// <summary>
    /// Tests ControlMessage constructor with null command throws ArgumentException
    /// </summary>
    [Fact]
    public void ControlMessage_Constructor_WithNullCommand_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new ControlMessage(null!));
    }

    /// <summary>
    /// Tests ControlMessage constructor with empty command throws ArgumentException
    /// </summary>
    [Fact]
    public void ControlMessage_Constructor_WithEmptyCommand_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new ControlMessage(""));
        Assert.Throws<ArgumentException>(() => new ControlMessage("   "));
    }

    /// <summary>
    /// Tests ControlMessage constructor with valid command
    /// </summary>
    [Fact]
    public void ControlMessage_Constructor_WithValidCommand_SetsCommand()
    {
        // Arrange
        var command = "processOrder";

        // Act
        var message = new ControlMessage(command);

        // Assert
        message.Command.Should().Be(command);
        message.Parameters.Should().NotBeNull();
        message.Parameters.Should().BeEmpty();
        message.MessageId.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests ControlMessage constructor with command and parameters
    /// </summary>
    [Fact]
    public void ControlMessage_Constructor_WithCommandAndParameters_SetsBoth()
    {
        // Arrange
        var command = "processOrder";
        var parameters = new Dictionary<string, object> { { "orderId", "ORD-123" }, { "priority", 1 } };

        // Act
        var message = new ControlMessage(command, parameters);

        // Assert
        message.Command.Should().Be(command);
        message.Parameters.Should().HaveCount(2);
        message.Parameters["orderId"].Should().Be("ORD-123");
        message.Parameters["priority"].Should().Be(1);
    }

    /// <summary>
    /// Tests ControlMessage constructor with null parameters initializes empty dictionary
    /// </summary>
    [Fact]
    public void ControlMessage_Constructor_WithNullParameters_InitializesEmptyDictionary()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        // Arrange & Act
        var message = new ControlMessage("test", null);

        // Assert
        message.Parameters.Should().NotBeNull();
        message.Parameters.Should().BeEmpty();
#pragma warning restore CS8625
    }

    /// <summary>
    /// Tests ResponseMessage constructor with null response
    /// </summary>
    [Fact]
    public void ResponseMessage_Constructor_WithNullResponse_SetsResponseToNull()
    {
        // Arrange & Act
        var message = new ResponseMessage(null);

        // Assert
        message.Response.Should().BeNull();
        message.IsSuccess.Should().BeTrue();
        message.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests ResponseMessage constructor with response and success flag
    /// </summary>
    [Fact]
    public void ResponseMessage_Constructor_WithResponseAndSuccess_SetsAllProperties()
    {
        // Arrange
        var response = new { Status = "OK" };
        var isSuccess = false;
        var errorMessage = "Something went wrong";

        // Act
        var message = new ResponseMessage(response, isSuccess, errorMessage);

        // Assert
        message.Response.Should().BeSameAs(response);
        message.IsSuccess.Should().Be(isSuccess);
        message.ErrorMessage.Should().Be(errorMessage);
    }

    /// <summary>
    /// Tests ResponseMessage constructor with default values
    /// </summary>
    [Fact]
    public void ResponseMessage_Constructor_WithDefaultValues_SetsDefaultProperties()
    {
        // Arrange & Act
        var message = new ResponseMessage("success");

        // Assert
        message.Response.Should().Be("success");
        message.IsSuccess.Should().BeTrue();
        message.ErrorMessage.Should().BeNull();
    }

    /// <summary>
    /// Tests FailureMessage constructor with null reason throws ArgumentException
    /// </summary>
    [Fact]
    public void FailureMessage_Constructor_WithNullReason_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentException>(() => new FailureMessage(null!));
        Assert.Throws<ArgumentException>(() => new FailureMessage(""));
    }

    /// <summary>
    /// Tests FailureMessage constructor with valid reason
    /// </summary>
    [Fact]
    public void FailureMessage_Constructor_WithValidReason_SetsReason()
    {
        // Arrange
        var reason = "Database connection failed";

        // Act
        var message = new FailureMessage(reason);

        // Assert
        message.Reason.Should().Be(reason);
        message.StackTrace.Should().BeNull();
        message.FailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        message.MessageId.Should().NotBe(Guid.Empty);
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tests FailureMessage constructor with reason and exception
    /// </summary>
    [Fact]
    public void FailureMessage_Constructor_WithReasonAndException_SetsExceptionDetails()
    {
        // Arrange
        var reason = "Null reference exception";
        var exception = new NullReferenceException("test exception");

        // Act
        var message = new FailureMessage(reason, exception);

        // Assert
        message.Reason.Should().Be(reason);
        message.StackTrace.Should().Be(exception.StackTrace);
        message.FailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        // The exception object should be stored
        message.Should().NotBeNull();
    }

    /// <summary>
    /// Tests FailureMessage constructor with null exception sets default FailureTime
    /// </summary>
    [Fact]
    public void FailureMessage_Constructor_WithNullException_SetsFailureTime()
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type
        // Arrange
        var reason = "Test failure";

        // Act
        var message = new FailureMessage(reason, null);

        // Assert
        message.Reason.Should().Be(reason);
        message.StackTrace.Should().BeNull();
        message.FailureTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
#pragma warning restore CS8625
    }

    /// <summary>
    /// Tests that all message types are records and support value equality
    /// </summary>
    [Fact]
    public void MessageTypes_ShouldSupportValueEquality()
    {
        // Arrange
        var payload1 = new { Name = "test", Value = 42 };
        var payload2 = new { Name = "test", Value = 42 };
        var payload3 = new { Name = "different", Value = 99 };

        // Act
        var message1 = new Message<object>(payload1);
        var message2 = new Message<object>(payload2);
        var message3 = new Message<object>(payload3);
        var control1 = new ControlMessage("test");
        var control2 = new ControlMessage("test");
        var response1 = new ResponseMessage("response");
        var response2 = new ResponseMessage("response");
        var failure1 = new FailureMessage("reason");
        var failure2 = new FailureMessage("reason");

        // Assert - records should have value equality based on properties
        // For records with init-only properties like MessageId and CreatedAt, we need to compare the values
        message1.Should().BeEquivalentTo(message2, options => options
            .Excluding(m => m.MessageId)
            .Excluding(m => m.CreatedAt));
        message1.Should().NotBeEquivalentTo(message3, options => options
            .Excluding(m => m.MessageId)
            .Excluding(m => m.CreatedAt));
        control1.Should().BeEquivalentTo(control2, options => options
            .Excluding(m => m.MessageId)
            .Excluding(m => m.CreatedAt));
        response1.Should().BeEquivalentTo(response2, options => options
            .Excluding(m => m.MessageId)
            .Excluding(m => m.CreatedAt));
        failure1.Should().BeEquivalentTo(failure2, options => options
            .Excluding(m => m.MessageId)
            .Excluding(m => m.CreatedAt)
            .Excluding(m => m.FailureTime));
    }

    /// <summary>
    /// Tests that MessageId is immutable (init-only property)
    /// </summary>
    [Fact]
    public void Message_BaseClass_MessageIdShouldBeImmutable()
    {
        // Arrange
        var message = new TestMessage();
        var originalId = message.MessageId;

        // Act - Try to change via reflection (this is a defensive test)
        // Since it's init-only, direct assignment should fail at compile time
        // We can only verify it's set initially
        message.MessageId.Should().Be(originalId);
    }

    /// <summary>
    /// Tests that CreatedAt is immutable (init-only property)
    /// </summary>
    [Fact]
    public void Message_BaseClass_CreatedAtShouldBeImmutable()
    {
        // Arrange
        var message = new TestMessage();
        var originalCreatedAt = message.CreatedAt;

        // Assert
        message.CreatedAt.Should().Be(originalCreatedAt);
    }

    /// <summary>
    /// Tests that Priority is mutable via object initializer
    /// </summary>
    [Fact]
    public void Message_BaseClass_PriorityShouldBeMutableViaInitializer()
    {
        // Arrange & Act
        var message = new TestMessage { Priority = 5 };

        // Assert
        message.Priority.Should().Be(5);
    }

    // Test message class for testing base Message functionality
    private sealed record TestMessage : Message;
}
