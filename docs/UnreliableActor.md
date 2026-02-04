# UnreliableActor
The `UnreliableActor` class is a type of actor in the dotnet-actor-framework that provides a basic implementation for receiving and processing messages in an unreliable manner. It is designed to handle messages without guaranteeing delivery or processing, making it suitable for scenarios where message loss or duplication is acceptable.

## API
* `public UnreliableActor(ActorPath path)`: Initializes a new instance of the `UnreliableActor` class with the specified `ActorPath`. The `path` parameter is used to identify the actor in the actor system.
* `public override async Task ReceiveAsync`: Handles incoming messages. This method is called when a message is received by the actor. It does not return any value and does not throw any exceptions explicitly, but may throw exceptions if the message processing fails.
* `public override async Task OnStopAsync`: Called when the actor is stopping. This method is used to perform any necessary cleanup or shutdown operations. It does not return any value and does not throw any exceptions explicitly, but may throw exceptions if the shutdown operations fail.
* Note: The `SupervisorActor` constructor and methods are not part of the `UnreliableActor` class and are not included in this documentation.

## Usage
The following examples demonstrate how to use the `UnreliableActor` class:
```csharp
// Example 1: Creating an UnreliableActor instance
var actorPath = new ActorPath("MyUnreliableActor");
var unreliableActor = new UnreliableActor(actorPath);

// Example 2: Using the ReceiveAsync method to handle messages
public class MyUnreliableActor : UnreliableActor
{
    public MyUnreliableActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync()
    {
        // Handle incoming messages here
        var message = await ReceiveMessageAsync();
        Console.WriteLine($"Received message: {message}");
    }
}
```

## Notes
The `UnreliableActor` class is designed for use cases where message delivery and processing are not critical. It does not provide any guarantees about message delivery or processing, and messages may be lost or duplicated. Additionally, the `ReceiveAsync` and `OnStopAsync` methods are asynchronous and may be called concurrently, so implementations should be thread-safe. The `UnreliableActor` class inherits from a base class and may be subject to the constraints and behaviors of that base class.
