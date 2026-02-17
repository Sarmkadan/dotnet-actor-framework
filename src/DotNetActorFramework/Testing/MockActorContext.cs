// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Testing;

/// <summary>
/// Mock context for testing actors in isolation.
/// Captures messages and provides inspection capabilities for unit testing.
/// </summary>
public class MockActorContext
{
    private readonly List<Message> _receivedMessages = [];
    private readonly List<Message> _sentMessages = [];
    private readonly object _lockObject = new();

    public ActorPath ActorPath { get; }
    public Guid ActorId { get; }

    public MockActorContext(ActorPath actorPath)
    {
        ActorPath = actorPath ?? throw new ArgumentNullException(nameof(actorPath));
        ActorId = Guid.NewGuid();
    }

    /// <summary>
    /// Records a message received by the actor.
    /// </summary>
    public void RecordReceivedMessage(Message message)
    {
        if (message == null) return;
        lock (_lockObject)
        {
            _receivedMessages.Add(message);
        }
    }

    /// <summary>
    /// Records a message sent by the actor.
    /// </summary>
    public void RecordSentMessage(Message message)
    {
        if (message == null) return;
        lock (_lockObject)
        {
            _sentMessages.Add(message);
        }
    }

    /// <summary>
    /// Gets all messages received by this actor.
    /// </summary>
    public IReadOnlyList<Message> GetReceivedMessages()
    {
        lock (_lockObject)
        {
            return _receivedMessages.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all messages sent by this actor.
    /// </summary>
    public IReadOnlyList<Message> GetSentMessages()
    {
        lock (_lockObject)
        {
            return _sentMessages.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets messages received of a specific type.
    /// </summary>
    public IReadOnlyList<Message> GetReceivedMessagesOfType(string messageType)
    {
        lock (_lockObject)
        {
            return _receivedMessages
                .Where(m => m.GetType().Name == messageType)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the count of messages received.
    /// </summary>
    public int GetMessageCount()
    {
        lock (_lockObject)
        {
            return _receivedMessages.Count;
        }
    }

    /// <summary>
    /// Gets the count of messages sent.
    /// </summary>
    public int GetSentMessageCount()
    {
        lock (_lockObject)
        {
            return _sentMessages.Count;
        }
    }

    /// <summary>
    /// Clears all recorded messages.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _receivedMessages.Clear();
            _sentMessages.Clear();
        }
    }

    /// <summary>
    /// Asserts that a specific message type was received.
    /// Useful for testing.
    /// </summary>
    public bool DidReceiveMessageType(string messageType)
    {
        lock (_lockObject)
        {
            return _receivedMessages.Any(m => m.GetType().Name == messageType);
        }
    }

    /// <summary>
    /// Asserts that a specific message count was received.
    /// </summary>
    public bool DidReceiveMessageCount(int count)
    {
        lock (_lockObject)
        {
            return _receivedMessages.Count == count;
        }
    }
}

/// <summary>
/// Test probe for capturing actor interactions.
/// </summary>
public class TestProbe
{
    private readonly ConcurrentQueue<Envelope> _messages = [];
    private readonly TaskCompletionSource<Envelope> _nextMessage = new();

    public Guid ProbeId { get; } = Guid.NewGuid();

    /// <summary>
    /// Receives a message sent to the probe.
    /// </summary>
    public void ReceiveMessage(Envelope envelope)
    {
        if (envelope == null) return;
        _messages.Enqueue(envelope);
        _nextMessage.TrySetResult(envelope);
    }

    /// <summary>
    /// Waits for the next message with timeout.
    /// </summary>
    public async Task<Envelope?> ExpectMessageAsync(TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        var completed = await Task.WhenAny(
            _nextMessage.Task,
            Task.Delay(Timeout.Infinite, cts.Token));

        return completed == _nextMessage.Task ? await _nextMessage.Task : null;
    }

    /// <summary>
    /// Gets all received messages without consuming them.
    /// </summary>
    public IReadOnlyList<Envelope> GetAllMessages() => _messages.ToArray();

    /// <summary>
    /// Clears all captured messages.
    /// </summary>
    public void ClearMessages()
    {
        while (_messages.TryDequeue(out _)) { }
    }
}
