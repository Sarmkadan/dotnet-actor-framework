// ... (rest of the file remains unchanged)

## HttpActorClient

The `HttpActorClient` is a client for communicating with actors over HTTP. It enables REST-based actor invocation and message sending.

### Usage Example

```csharp
var client = new HttpActorClient("https://example.com/actors");
var response = await client.SendMessageAsync("my-actor", new MyMessage { Foo = "bar" });
Console.WriteLine(response.StatusCode);

var actorState = await client.GetActorStateAsync<MyActorState>("my-actor");
Console.WriteLine(actorState.State);

var healthStatus = await client.GetActorHealthAsync("my-actor");
Console.WriteLine(healthStatus.IsHealthy);

var systemHealth = await client.GetSystemHealthAsync();
Console.WriteLine(systemHealth.TotalActors);
```

### Properties and Methods

- `HttpActorClient(string baseUrl)`: Initializes a new instance of the `HttpActorClient` class.
- `async Task<HttpResponseMessage> SendMessageAsync(string actorPath, Message message)`: Sends a message to an actor via HTTP POST.
- `async Task<T?> GetActorStateAsync<T>(string actorPath)`: Gets an actor's state via HTTP GET.
- `async Task<ActorHealthStatus?> GetActorHealthAsync(string actorPath)`: Gets an actor's health status via HTTP GET.
- `async Task<SystemHealthStatus?> GetSystemHealthAsync()`: Gets the system health status via HTTP GET.
- `void Dispose()`: Disposes the client.
```