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

public class MockActorContextTests
{
    private readonly ActorPath _testPath = ActorPath.Parse("/test/actor");
    private readonly MockActorContext _mockContext;

    public MockActorContextTests()
    {
        _mockContext = new MockActorContext(_testPath);
    }

    [Fact]
    public void Constructor_ShouldInitializeWithGivenActorPath()
    {
        // Act
        var context = new MockActorContext(_testPath);

        // Assert
        context.ActorPath.Should().Be(_testPath);
        context.ActorId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Constructor_ShouldThrowOnNullActorPath()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new MockActorContext(null!));
    }

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

    [Fact]
    public void RecordReceivedMessage_ShouldNotAddNullMessage()
    {
        // Act
        _mockContext.RecordReceivedMessage(null);

        // Assert
        var receivedMessages = _mockContext.GetReceivedMessages();
        receivedMessages.Should().BeEmpty();
    }

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

    [Fact]
    public void RecordSentMessage_ShouldNotAddNullMessage()
    {
        // Act
        _mockContext.RecordSentMessage(null);

        // Assert
        var sentMessages = _mockContext.GetSentMessages();
        sentMessages.Should().BeEmpty();
    }

    [Fact]
    public void GetReceivedMessages_ShouldReturnEmptyListInitially()
    {
        // Act
        var messages = _mockContext.GetReceivedMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void GetSentMessages_ShouldReturnEmptyListInitially()
    {
        // Act
        var messages = _mockContext.GetSentMessages();

        // Assert
        messages.Should().BeEmpty();
    }

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

    [Fact]
    public void GetMessageCount_ShouldReturnZeroInitially()
    {
        // Act
        var count = _mockContext.GetMessageCount();

        // Assert
        count.Should().Be(0);
    }

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

    [Fact]
    public void GetSentMessageCount_ShouldReturnZeroInitially()
    {
        // Act
        var count = _mockContext.GetSentMessageCount();

        // Assert
        count.Should().Be(0);
    }

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

    [Fact]
    public void DidReceiveMessageType_ShouldReturnFalseForNonExistentMessageType()
    {
        // Arrange
        var message = new ControlMessage("test");
        _mockContext.RecordReceivedMessage(message);

        // Act & Assert
        _mockContext.DidReceiveMessageType("NonExistentMessage").Should().BeFalse();
    }

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

public class TestProbeTests
{
    private readonly TestProbe _testProbe;

    public TestProbeTests()
    {
        _testProbe = new TestProbe();
    }

    [Fact]
    public void ProbeId_ShouldBeUnique()
    {
        // Arrange
        var probe1 = new TestProbe();
        var probe2 = new TestProbe();

        // Assert
        probe1.ProbeId.Should().NotBe(probe2.ProbeId);
        probe1.ProbeId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ReceiveMessage_ShouldAddMessageToQueue()
    {
        // Arrange
        var envelope = new Envelope(new ControlMessage("test"), new ActorRef(ActorPath.Parse("/test"), Guid.NewGuid()));

        // Act
        _testProbe.ReceiveMessage(envelope);

        // Assert
        var messages = _testProbe.GetAllMessages();
        messages.Should().HaveCount(1);
        messages[0].Should().BeSameAs(envelope);
    }

    [Fact]
    public void ReceiveMessage_ShouldNotAddNullEnvelope()
    {
        // Act
        _testProbe.ReceiveMessage(null);

        // Assert
        _testProbe.GetAllMessages().Should().BeEmpty();
    }

    [Fact]
    public void GetAllMessages_ShouldReturnEmptyListInitially()
    {
        // Act
        var messages = _testProbe.GetAllMessages();

        // Assert
        messages.Should().BeEmpty();
    }

    [Fact]
    public void GetAllMessages_ShouldReturnAllMessagesWithoutConsuming()
    {
        // Arrange
        var envelope1 = new Envelope(new ControlMessage("test1"), new ActorRef(ActorPath.Parse("/test1"), Guid.NewGuid()));
        var envelope2 = new Envelope(new ControlMessage("test2"), new ActorRef(ActorPath.Parse("/test2"), Guid.NewGuid()));

        _testProbe.ReceiveMessage(envelope1);
        _testProbe.ReceiveMessage(envelope2);

        // Act
        var messages = _testProbe.GetAllMessages();

        // Assert
        messages.Should().HaveCount(2);
        messages.Should().ContainInOrder(envelope1, envelope2);
    }

    [Fact]
    public async Task ExpectMessageAsync_ShouldReturnMessageWhenAvailable()
    {
        // Arrange
        var envelope = new Envelope(new ControlMessage("test"), new ActorRef(ActorPath.Parse("/test"), Guid.NewGuid()));

        // Start receiving in background
        var receiveTask = Task.Run(async () =>
        {
            await Task.Delay(100); // Small delay to ensure probe is waiting
            _testProbe.ReceiveMessage(envelope);
        });

        // Act
        var result = await _testProbe.ExpectMessageAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(envelope);
        await receiveTask;
    }

    [Fact]
    public async Task ExpectMessageAsync_ShouldReturnNullOnTimeout()
    {
        // Act
        var result = await _testProbe.ExpectMessageAsync(TimeSpan.FromMilliseconds(100));

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExpectMessageAsync_ShouldUseDefaultTimeoutWhenNotSpecified()
    {
        // Arrange
        var envelope = new Envelope(new ControlMessage("test"), new ActorRef(ActorPath.Parse("/test"), Guid.NewGuid()));

        // Start receiving in background
        var receiveTask = Task.Run(async () =>
        {
            await Task.Delay(100);
            _testProbe.ReceiveMessage(envelope);
        });

        // Act
        var result = await _testProbe.ExpectMessageAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(envelope);
        await receiveTask;
    }

    [Fact]
    public void ClearMessages_ShouldRemoveAllMessages()
    {
        // Arrange
        var envelope1 = new Envelope(new ControlMessage("test1"), new ActorRef(ActorPath.Parse("/test1"), Guid.NewGuid()));
        var envelope2 = new Envelope(new ControlMessage("test2"), new ActorRef(ActorPath.Parse("/test2"), Guid.NewGuid()));

        _testProbe.ReceiveMessage(envelope1);
        _testProbe.ReceiveMessage(envelope2);

        // Verify setup
        _testProbe.GetAllMessages().Should().HaveCount(2);

        // Act
        _testProbe.ClearMessages();

        // Assert
        _testProbe.GetAllMessages().Should().BeEmpty();
    }

    [Fact]
    public void ThreadSafety_ShouldHandleConcurrentMessageReception()
    {
        // Arrange
        var tasks = new List<Task>();
        var messageCount = 100;

        // Act - concurrent message reception
        for (int i = 0; i < messageCount; i++)
        {
            var envelope = new Envelope(
                new ControlMessage($"test{i}"),
                new ActorRef(ActorPath.Parse($"/test{i}"), Guid.NewGuid())
            );
            tasks.Add(Task.Run(() => _testProbe.ReceiveMessage(envelope)));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        _testProbe.GetAllMessages().Should().HaveCount(messageCount);
    }
}