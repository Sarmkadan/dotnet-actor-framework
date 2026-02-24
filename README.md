// ... (rest of the file remains unchanged)

## LoadBasedRouter

The `LoadBasedRouter` routes incoming messages to actors within a capability-scoped pool using either least-load selection or round-robin distribution. It provides natural back-pressure-aware load balancing by inspecting live mailbox queue depths at dispatch time, or can evenly distribute load over time when using round-robin routing.

### Usage Example

```csharp
// Setup: discover actors and create router
var discovery = new ActorDiscoveryService();
var mailbox = new MailboxService();
var dispatcher = new MessageDispatcher();
var router = new LoadBasedRouter(discovery, mailbox, dispatcher);

// Get least-loaded actor for a capability
var leastLoaded = router.GetLeastLoaded("file-processor");
Console.WriteLine($"Least loaded actor: {leastLoaded?.Path.Name}");

// Route message to least-loaded actor
var envelope = new Envelope(new ProcessFile("data.txt"), null);
bool routed = await router.RouteAsync("file-processor", envelope);
Console.WriteLine($"Message routed: {routed}");

// Route message using round-robin
bool roundRoued = await router.RouteRoundRobinAsync("file-processor", envelope);
Console.WriteLine($"Message round-robin routed: {roundRoued}");

// Get current load snapshot
var loadSnapshot = router.GetLoadSnapshot("file-processor");
foreach (var actorLoad in loadSnapshot)
{
    Console.WriteLine($"{actorLoad.Key}: {actorLoad.Value} messages in queue");
}
```

### Properties and Methods

- `LoadBasedRouter(ActorDiscoveryService discovery, MailboxService mailbox, MessageDispatcher dispatcher)`: Initializes a new instance of the `LoadBasedRouter` class.
- `async Task<bool> RouteAsync(string capability, Envelope envelope, CancellationToken cancellationToken)`: Dispatches an envelope to the least-loaded live actor registered under the specified capability.
- `async Task<bool> RouteRoundRobinAsync(string capability, Envelope envelope, CancellationToken cancellationToken)`: Dispatches an envelope to the next actor in a round-robin sequence across all live actors registered under the specified capability.
- `ActorRef? GetLeastLoaded(string capability)`: Selects the live actor with the fewest queued messages within the pool registered under the specified capability.
- `IReadOnlyDictionary<string, int> GetLoadSnapshot(string capability)`: Returns a snapshot of the current mailbox depth for every live actor registered under the specified capability.

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