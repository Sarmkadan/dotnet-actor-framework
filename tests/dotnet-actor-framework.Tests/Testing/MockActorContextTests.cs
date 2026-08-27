// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// Unit tests for MockActorContext and TestProbe
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Testing;
using FluentAssertions;
using Xunit;

namespace DotNetActorFramework.Tests.Testing;

/// <summary>
/// Unit tests for the <see cref="MockActorContext"/> class.
/// </summary>
public class MockActorContextTests
{
    private readonly ActorPath _testPath = ActorPath.Parse("/test/actor");
    private readonly MockActorContext _mockContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="MockActorContextTests"/> class.
    /// Sets up the test actor path and mock context.
    /// </summary>
    public MockActorContextTests()
    {
        _mockContext = new MockActorContext(_testPath);
    }

    /// <summary>
    /// Verifies that the MockActorContext constructor initializes with the given actor path.
    /// </summary>
    [Fact]
    public void Constructor_ShouldInitializeWithGivenActorPath()
    {
        // Act
        var context = new MockActorContext(_testPath);

        // Assert
        context.ActorPath.Should().Be(_testPath);
        context.ActorId.Should().NotBe(Guid.Empty);
    }

    /// <summary>
    /// Verifies that the MockActorContext constructor throws an ArgumentNullException when given a null actor path.
    /// </summary>
    [Fact]
    public void Constructor_ShouldThrowOnNullActorPath()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MockActorContext(null!));
    }

    /// <summary>
    /// Verifies that recording a received message adds it to the received messages collection.
    /// </summary>
    [Fact]
    public void RecordReceivedMessage_ShouldAddMessageToReceivedMessages()
    {
        // Arrange
        var message = new ControlMessage("test");

        // Act
        _mockContext.RecordReceivedMessage(message);

        // Assert
        var receivedMessages = _mockContext.GetReceivedMessages();
        receivedMessages.Should().HaveCount(1);
        receivedMessages[0].Should().BeSameAs(message);
    }

    /// <summary>
    /// Verifies that recording a null received message does not add anything to the received messages collection.
    /// </summary>
    [Fact]
    public void RecordReceivedMessage_ShouldNotAddNullMessage()
    {
        // Act
        _mockContext.RecordReceivedMessage(null);

        // Assert
        var receivedMessages = _mockContext.GetReceivedMessages();
        receivedMessages.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that recording a sent message adds it to the sent messages collection.
    /// </summary>
    [Fact]
    public void RecordSentMessage_ShouldAddMessageToSentMessages()
    {
        // Arrange
        var message = new ControlMessage("test");

        // Act
        _mockContext.RecordSentMessage(message);

        // Assert
        var sentMessages = _mockContext.GetSentMessages();
        sentMessages.Should().HaveCount(1);
        sentMessages[0].Should().BeSameAs(message);
    }

    /// <summary>
    /// Verifies that recording a null sent message does not add anything to the sent messages collection.
    /// </summary>
    [Fact]
    public void RecordSentMessage_ShouldNotAddNullMessage()
    {
        // Act
        _mockContext.RecordSentMessage(null);

        // Assert
        var sentMessages = _mockContext.GetSentMessages();
        sentMessages.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that getting received messages returns an empty list when no messages have been recorded.
    /// </summary>
    [Fact]
    public void GetReceivedMessages_ShouldReturnEmptyListInitially()
    {
        // Act
        var messages = _mockContext.GetReceivedMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that getting sent messages returns an empty list when no messages have been recorded.
    /// </summary>
    [Fact]
    public void GetSentMessages_ShouldReturnEmptyListInitially()
    {
        // Act
        var messages = _mockContext.GetSentMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that getting received messages returns all recorded messages in the order they were added.
    /// </summary>
    [Fact]
    public void GetReceivedMessages_ShouldReturnAllReceivedMessages()
    {
        // Arrange
        var message1 = new ControlMessage("test1");
        var message2 = new ControlMessage("test2");
        var message3 = new ResponseMessage("response");

        _mockContext.RecordReceivedMessage(message1);
        _mockContext.RecordReceivedMessage(message2);
        _mockContext.RecordReceivedMessage(message3);

        // Act
        var receivedMessages = _mockContext.GetReceivedMessages();

        // Assert
        receivedMessages.Should().HaveCount(3);
        receivedMessages.Should().ContainInOrder(message1, message2, message3);
    }

    /// <summary>
    /// Verifies that getting sent messages returns all recorded messages in the order they were added.
    /// </summary>
    [Fact]
    public void GetSentMessages_ShouldReturnAllSentMessages()
    {
        // Arrange
        var message1 = new ControlMessage("test1");
        var message2 = new ControlMessage("test2");
        var message3 = new ResponseMessage("response");

        _mockContext.RecordSentMessage(message1);
        _mockContext.RecordSentMessage(message2);
        _mockContext.RecordSentMessage(message3);

        // Act
        var sentMessages = _mockContext.GetSentMessages();

        // Assert
        sentMessages.Should().HaveCount(3);
        sentMessages.Should().ContainInOrder(message1, message2, message3);
    }

    /// <summary>
    /// Verifies that getting received messages of a specific type returns only messages of that type.
    /// </summary>
    [Fact]
    public void GetReceivedMessagesOfType_ShouldFilterByMessageTypeName()
    {
        // Arrange
        var controlMsg = new ControlMessage("test");
        var responseMsg = new ResponseMessage("response");
        var failureMsg = new FailureMessage("error");
        var typedMsg = new Message<string>("payload");

        _mockContext.RecordReceivedMessage(controlMsg);
        _mockContext.RecordReceivedMessage(responseMsg);
        _mockContext.RecordReceivedMessage(failureMsg);
        _mockContext.RecordReceivedMessage(typedMsg);

        // Act
        var controlMessages = _mockContext.GetReceivedMessagesOfType("ControlMessage");
        var responseMessages = _mockContext.GetReceivedMessagesOfType("ResponseMessage");
        var failureMessages = _mockContext.GetReceivedMessagesOfType("FailureMessage");
        var typedMessages = _mockContext.GetReceivedMessagesOfType("Message`1");

        // Assert
        controlMessages.Should().HaveCount(1);
        controlMessages[0].Should().BeSameAs(controlMsg);

        responseMessages.Should().HaveCount(1);
        responseMessages[0].Should().BeSameAs(responseMsg);

        failureMessages.Should().HaveCount(1);
        failureMessages[0].Should().BeSameAs(failureMsg);

        typedMessages.Should().HaveCount(1);
        typedMessages[0].Should().BeSameAs(typedMsg);
    }

    /// <summary>
    /// Verifies that getting received messages of a non-existent type returns an empty collection.
    /// </summary>
    [Fact]
    public void GetReceivedMessagesOfType_ShouldReturnEmptyForNonExistentType()
    {
        // Arrange
        var message = new ControlMessage("test");
        _mockContext.RecordReceivedMessage(message);

        // Act
        var messages = _mockContext.GetReceivedMessagesOfType("NonExistentMessage");

        // Assert
        messages.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that getting the message count returns zero when no messages have been recorded.
    /// </summary>
    [Fact]
    public void GetMessageCount_ShouldReturnZeroInitially()
    {
        // Act
        var count = _mockContext.GetMessageCount();

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that getting the message count returns the number of received messages.
    /// </summary>
    [Fact]
    public void GetMessageCount_ShouldReturnReceivedMessageCount()
    {
        // Arrange
        var message1 = new ControlMessage("test1");
        var message2 = new ControlMessage("test2");
        _mockContext.RecordReceivedMessage(message1);
        _mockContext.RecordReceivedMessage(message2);

        // Act
        var count = _mockContext.GetMessageCount();

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that getting the sent message count returns zero when no messages have been sent.
    /// </summary>
    [Fact]
    public void GetSentMessageCount_ShouldReturnZeroInitially()
    {
        // Act
        var count = _mockContext.GetSentMessageCount();

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that getting the sent message count returns the number of sent messages.
    /// </summary>
    [Fact]
    public void GetSentMessageCount_ShouldReturnSentMessageCount()
    {
        // Arrange
        var message1 = new ControlMessage("test1");
        var message2 = new ControlMessage("test2");
        _mockContext.RecordSentMessage(message1);
        _mockContext.RecordSentMessage(message2);

        // Act
        var count = _mockContext.GetSentMessageCount();

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that clearing the context removes all received and sent messages.
    /// </summary>
    [Fact]
    public void Clear_ShouldRemoveAllMessages()
    {
        // Arrange
        var receivedMsg = new ControlMessage("received");
        var sentMsg = new ControlMessage("sent");
        _mockContext.RecordReceivedMessage(receivedMsg);
        _mockContext.RecordSentMessage(sentMsg);

        // Verify setup
        _mockContext.GetMessageCount().Should().Be(1);
        _mockContext.GetSentMessageCount().Should().Be(1);

        // Act
        _mockContext.Clear();

        // Assert
        _mockContext.GetMessageCount().Should().Be(0);
        _mockContext.GetSentMessageCount().Should().Be(0);
        _mockContext.GetReceivedMessages().Should().BeEmpty();
        _mockContext.GetSentMessages().Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that checking for receipt of a message type returns true when that message type has been received.
    /// </summary>
    [Fact]
    public void DidReceiveMessageType_ShouldReturnTrueForExistingMessageType()
    {
        // Arrange
        var controlMsg = new ControlMessage("test");
        var responseMsg = new ResponseMessage("response");
        _mockContext.RecordReceivedMessage(controlMsg);
        _mockContext.RecordReceivedMessage(responseMsg);

        // Act & Assert
        _mockContext.DidReceiveMessageType("ControlMessage").Should().BeTrue();
        _mockContext.DidReceiveMessageType("ResponseMessage").Should().BeTrue();
    }

    /// <summary>
    /// Verifies that checking for receipt of a message type returns false when that message type has not been received.
    /// </summary>
    [Fact]
    public void DidReceiveMessageType_ShouldReturnFalseForNonExistentMessageType()
    {
        // Arrange
        var message = new ControlMessage("test");
        _mockContext.RecordReceivedMessage(message);

        // Act & Assert
        _mockContext.DidReceiveMessageType("NonExistentMessage").Should().BeFalse();
    }

    /// <summary>
    /// Verifies that checking for receipt of a specific message count returns true when the count matches.
    /// </summary>
    [Fact]
    public void DidReceiveMessageCount_ShouldReturnTrueForMatchingCount()
    {
        // Arrange
        var message1 = new ControlMessage("test1");
        var message2 = new ControlMessage("test2");
        var message3 = new ControlMessage("test3");
        _mockContext.RecordReceivedMessage(message1);
        _mockContext.RecordReceivedMessage(message2);
        _mockContext.RecordReceivedMessage(message3);

        // Act & Assert
        _mockContext.DidReceiveMessageCount(3).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that checking for receipt of a specific message count returns false when the count does not match.
    /// </summary>
    [Fact]
    public void DidReceiveMessageCount_ShouldReturnFalseForNonMatchingCount()
    {
        // Arrange
        var message1 = new ControlMessage("test1");
        var message2 = new ControlMessage("test2");
        _mockContext.RecordReceivedMessage(message1);
        _mockContext.RecordReceivedMessage(message2);

        // Act & Assert
        _mockContext.DidReceiveMessageCount(3).Should().BeFalse();
        _mockContext.DidReceiveMessageCount(1).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that the MockActorContext handles concurrent access to its message recording methods in a thread-safe manner.
    /// </summary>
    [Fact]
    public void ThreadSafety_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var tasks = new List<Task>();
        var messageCount = 1000;

        // Act - concurrent recording
        for (int i = 0; i < messageCount; i++)
        {
            var message = new ControlMessage($"test{i}");
            tasks.Add(Task.Run(() => _mockContext.RecordReceivedMessage(message)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        _mockContext.GetMessageCount().Should().Be(messageCount);
    }
}