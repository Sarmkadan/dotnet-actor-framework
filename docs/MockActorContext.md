# MockActorContext

`MockActorContext` is a test‑double implementation of the actor context used in unit tests of the dotnet‑actor‑framework. It records inbound and outbound messages, provides query helpers for assertions, and offers asynchronous expectation utilities to verify message exchanges without exercising a real actor system.

## API

### ActorPath
- **Type**: `ActorPath` (get‑only property)  
- **Purpose**: Returns the logical path of the actor associated with this context.  
- **Parameters**: None.  
- **Return Value**: The `ActorPath` assigned when the context was created.  
- **Exceptions**: None.

### ActorId
- **Type**: `Guid` (get‑only property)  
- **Purpose**: Returns the unique identifier of the actor represented by this context.  
- **Parameters**: None.  
- **Return Value**: The `Guid` assigned at construction.  
- **Exceptions**: None.

### MockActorContext(MockActorContext other)
- **Type**: Constructor  
- **Purpose**: Creates a new `MockActorContext` that copies the state (recorded messages, actor id, path, probe id) from an existing instance. Useful for branching test scenarios.  
- **Parameters**:  
  - `other`: The source `MockActorContext` to copy from. Must not be `null`.  
- **Return Value**: A new `MockActorContext` instance.  
- **Exceptions**:  
  - `ArgumentNullException` if `other` is `null`.

### RecordReceivedMessage(Message message)
- **Type**: `void`  
- **Purpose**: Records that the actor has received a message. Called internally by the mock when `ReceiveMessage` is invoked.  
- **Parameters**:  
  - `message`: The `Message` that was received. Must not be `null`.  
- **Return Value**: None.  
- **Exceptions**:  
  - `ArgumentNullException` if `message` is `null`.

### RecordSentMessage(Message message)
- **Type**: `void`  
- **Purpose**: Records that the actor has sent a message.  
- **Parameters**:  
  - `message`: The `Message` that was sent. Must not be `null`.  
- **Return Value**: None.  
- **Exceptions**:  
  - `ArgumentNullException` if `message` is `null`.

### GetReceivedMessages()
- **Type**: `IReadOnlyList<Message>`  
- **Purpose**: Provides read‑only access to the collection of all messages recorded as received.  
- **Parameters**: None.  
- **Return Value**: An immutable list of `Message` objects in the order they were recorded.  
- **Exceptions**: None.

### GetSentMessages()
- **Type**: `IReadOnlyList<Message>`  
- **Purpose**: Provides read‑only access to the collection of all messages recorded as sent.  
- **Parameters**: None.  
- **Return Value**: An immutable list of `Message` objects in the order they were recorded.  
- **Exceptions**: None.

### GetReceivedMessagesOfType(Type messageType)
- **Type**: `IReadOnlyList<Message>`  
- **Purpose**: Returns a filtered list of received messages that match the specified type (including derived types).  
- **Parameters**:  
  - `messageType`: The `Type` to filter by. Must not be `null`.  
- **Return Value**: An immutable list containing only those received messages whose runtime type is assignable from `messageType`.  
- **Exceptions**:  
  - `ArgumentNullException` if `messageType` is `null`.

### GetMessageCount()
- **Type**: `int`  
- **Purpose**: Returns the total number of messages recorded as received.  
- **Parameters**: None.  
- **Return Value**: The count of received messages.  
- **Exceptions**: None.

### GetSentMessageCount()
- **Type**: `int`  
- **Purpose**: Returns the total number of messages recorded as sent.  
- **Parameters**: None.  
- **Return Value**: The count of sent messages.  
- **Exceptions**: None.

### Clear()
- **Type**: `void`  
- **Purpose**: Removes all recorded received and sent messages, resetting the context to a clean state while preserving `ActorId`, `ActorPath`, and `ProbeId`.  
- **Parameters**: None.  
- **Return Value**: None.  
- **Exceptions**: None.

### DidReceiveMessageType(Type messageType)
- **Type**: `bool`  
- **Purpose**: Indicates whether at least one received message of the specified type (or a derived type) has been recorded.  
- **Parameters**:  
  - `messageType`: The `Type` to check for. Must not be `null`.  
- **Return Value**: `true` if a matching message exists; otherwise `false`.  
- **Exceptions**:  
  - `ArgumentNullException` if `messageType` is `null`.

### DidReceiveMessageCount(int expectedCount)
- **Type**: `bool`  
- **Purpose**: Indicates whether the number of received messages exactly equals `expectedCount`.  
- **Parameters**:  
  - `expectedCount`: The expected number of received messages. Must be non‑negative.  
- **Return Value**: `true` if the received message count matches `expectedCount`; otherwise `false`.  
- **Exceptions**:  
  - `ArgumentOutOfRangeException` if `expectedCount` is less than zero.

### ProbeId
- **Type**: `Guid` (get‑only property)  
- **Purpose**: Returns an identifier used to distinguish this mock context from others in a test scenario (e.g., when multiple probes are active).  
- **Parameters**: None.  
- **Return Value**: The `Guid` assigned at construction.  
- **Exceptions**: None.

### ReceiveMessage(Envelope envelope)
- **Type**: `void`  
- **Purpose**: Simulates the actor receiving an envelope; internally records the message as received and makes it available for synchronous inspection.  
- **Parameters**:  
  - `envelope`: The `Envelope` containing the message and sender information. Must not be `null`.  
- **Return Value**: None.  
- **Exceptions**:  
  - `ArgumentNullException` if `envelope` is `null`.

### ExpectMessageAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
- **Type**: `async Task<Envelope?>`  
- **Purpose**: Asynchronously waits for a message to be received, up to the specified timeout. Returns the envelope when a message arrives, or `null` if the timeout elapses.  
- **Parameters**:  
  - `timeout`: The maximum time to wait. Must be non‑negative.  
  - `cancellationToken`: Optional token to cancel the wait.  
- **Return Value**: A `Task` that completes with the received `Envelope` or `null` on timeout/cancellation.  
- **Exceptions**:  
  - `ArgumentOutOfRangeException` if `timeout` is negative.  
  - `OperationCanceledException` if the waiting task is canceled via `cancellationToken`.

### GetAllMessages()
- **Type**: `IReadOnlyList<Envelope>`  
- **Purpose**: Provides read‑only access to the combined collection of all envelopes (both received and sent) recorded by this context.  
- **Parameters**: None.  
- **Return Value**: An immutable list of `Envelope` objects in the order they were recorded (received first, then sent).  
- **Exceptions**: None.

### ClearMessages()
- **Type**: `void`  
- **Purpose**: Removes all recorded envelopes (both received and sent) while leaving `ActorId`, `ActorPath`, and `ProbeId` unchanged. Functionally equivalent to `Clear()` but emphasized for envelope‑centric workflows.  
- **Parameters**: None.  
- **Return Value**: None.  
- **Exceptions**: None.

## Usage

### Example 1: Verifying message exchanges in a synchronous test
```csharp
using System;
using System.Threading;
using DotnetActorFramework.Testing;

public class GreetingActorTests
{
    [Fact]
    public void Actor_Replies_With_Greeting()
    {
        // Arrange
        var context = new MockActorContext(ActorId: Guid.NewGuid(),
                                           ActorPath: ActorPath.Parse("/user/greeter"));
        var probe   = new MockActorContext(ActorId: Guid.NewGuid(),
                                           ActorPath: ActorPath.Parse("/user/probe"));

        // Simulate sending a greeting request to the actor under test
        var request = new Envelope(
            Sender: probe.ActorPath,
            Message: new GreetRequest("Alice"));

        context.ReceiveMessage(request);

        // Act – the actor processes the request and sends a reply
        // (Assume the actor implementation uses context.Sender.Tell(...))
        // For demonstration we manually record the reply:
        var reply = new Envelope(
            Sender: context.ActorPath,
            Message: new GreetResponse("Hello, Alice!"));
        context.RecordSentMessage(reply);

        // Assert
        Assert.True(context.DidReceiveMessageType(typeof(GreetRequest)));
        Assert.True(context.DidReceiveMessageCount(1));
        Assert.Single(context.GetSentMessagesOfType(typeof(GreetResponse)));
        Assert.Equal("Hello, Alice!", ((GreetResponse)context.GetSentMessages()[0].Message).Text);
    }
}
```

### Example 2: Asynchronously expecting a message with a timeout
```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetActorFramework.Testing;

public class PingPongActorTests
{
    [Fact]
    public async Task Actor_Responds_Within_Timeout()
    {
        // Arrange
        var context = new MockActorContext(ActorId: Guid.NewGuid(),
                                           ActorPath: ActorPath.Parse("/user/pinger"));
        var pongProbe = new MockActorContext(ActorId: Guid.NewGuid(),
                                             ActorPath: ActorPath.Parse("/user/pong"));

        // Simulate receiving a Ping message
        var ping = new Envelope(
            Sender: pongProbe.ActorPath,
            Message: new Ping());

        // Act – start waiting for a Pong response
        var waitTask = context.ExpectMessageAsync(TimeSpan.FromSeconds(2));

        // Simulate the actor processing the Ping and sending a Pong after a short delay
        _ = Task.Run(async () =>
        {
            await Task.Delay(300);
            var pong = new Envelope(
                Sender: context.ActorPath,
                Message: new Pong());
            context.RecordSentMessage(pong);
            context.ReceiveMessage(pong); // if the actor also processes its own outbound as inbound for test
        });

        // Assert
        Envelope? received = await waitTask;
        Assert.NotNull(received);
        Assert.IsType<Pong>(received!.Message);
    }
}
```

## Notes

- The type is **not thread‑safe**. Concurrent calls to `RecordReceivedMessage`, `RecordSentMessage`, `ReceiveMessage`, or any of the query methods from multiple threads may result in inconsistent state. External synchronization (e.g., locking) is required when the mock is accessed from different threads in a test.
- `Clear()` and `ClearMessages()` only affect the recorded message collections; they do **not** reset `ActorId`, `ActorPath`, or `ProbeId`. If a fresh identity is needed, a new instance must be constructed.
- Methods that accept a `Type` parameter (`GetReceivedMessagesOfType`, `DidReceiveMessageType`) treat inheritance relationships as matches; a message of a derived type will be returned when querying for its base type.
- `DidReceiveMessageCount(int)` expects a non‑negative value; supplying a negative number throws `ArgumentOutOfRangeException`.
- `ExpectMessageAsync` returns `null` when the timeout elapses or the supplied `CancellationToken` is triggered. It does **not** throw on timeout; callers must check the result for `null`.
- The `MockActorContext` constructor that takes another `MockActorContext` performs a shallow copy of the internal lists; modifications to the copied instance’s message collections do not affect the source, and vice‑versa. However, if the stored `Message` or `Envelope` objects are mutable and shared, changes to those objects will be visible in both instances. For strict isolation, ensure message objects are immutable or deep‑copied before sharing.
