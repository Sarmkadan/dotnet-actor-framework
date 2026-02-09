# WorkerActor
The `WorkerActor` class is a fundamental component of the dotnet-actor-framework, designed to handle incoming messages and perform tasks asynchronously. It provides a basic implementation for receiving and processing messages, making it a crucial building block for more complex actor systems.

## API
The `WorkerActor` class has the following public members:
* `public WorkerActor(ActorPath path)`: Initializes a new instance of the `WorkerActor` class with the specified `ActorPath`. This constructor is used to create a new worker actor with a unique path.
* `public override async Task ReceiveAsync`: This method is called when the actor receives a message. It is responsible for handling the incoming message and performing the necessary actions. The method returns a `Task` that represents the asynchronous operation.
* `public override async Task OnStopAsync`: This method is called when the actor is stopping. It is used to perform any necessary cleanup or shutdown actions. The method returns a `Task` that represents the asynchronous operation.

## Usage
Here are two examples of using the `WorkerActor` class:
```csharp
// Example 1: Creating a new worker actor
var path = new ActorPath("worker");
var workerActor = new WorkerActor(path);

// Example 2: Using the worker actor to receive a message
var message = new MyMessage();
await workerActor.ReceiveAsync(message);
```
In the first example, a new `WorkerActor` instance is created with a unique `ActorPath`. In the second example, the `ReceiveAsync` method is called to handle an incoming message.

## Notes
When using the `WorkerActor` class, it is essential to consider the following edge cases and thread-safety remarks:
* The `ReceiveAsync` method is called asynchronously, and its execution may overlap with other messages being received. Therefore, it is crucial to ensure that the method is thread-safe and can handle concurrent access.
* The `OnStopAsync` method is called when the actor is stopping, and it should be used to perform any necessary cleanup or shutdown actions. However, it is essential to note that this method may be called multiple times, and its implementation should be idempotent.
* The `WorkerActor` class does not provide any built-in support for handling exceptions. It is the responsibility of the developer to handle any exceptions that may occur during the execution of the `ReceiveAsync` or `OnStopAsync` methods.
