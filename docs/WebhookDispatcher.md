# WebhookDispatcher

Sends domain events as JSON `POST` requests to external HTTP endpoints. Each endpoint is described by a `WebhookConfig` and is matched to events by event type. The dispatcher is the outbound half of the integration layer: `IntegrationEventPublisher` takes a `WebhookDispatcher` in its constructor, and inbound calls go through `HttpActorClient` instead.

`WebhookDispatcher` lives in the `DotNetActorFramework.Integration` namespace and implements `IDisposable`. It creates and owns its own `HttpClient`, so dispose it when the application shuts down.

## Behavior at a glance

- Matching is by `WebhookConfig.EventType`, which must equal `IDomainEvent.EventType` exactly (case-sensitive), or be `"*"` to match every event. Inactive webhooks (`IsActive == false`) are skipped.
- Every matching webhook is delivered to concurrently. `DispatchEventAsync` completes only after all deliveries have finished or exhausted their retries. It does not fire and forget.
- A delivery is a `POST` to `WebhookConfig.Url` with the event serialized by `ToJson()` as the body (`Content-Type: application/json`, UTF-8). A 2xx response counts as success. Any other status code, network error, or serialization error is treated as a failed attempt.
- Failures are never thrown to the caller. Each exception is written with `System.Diagnostics.Debug.WriteLine` and the attempt is counted.
- The dispatcher keeps no queue and no persistence. Retries happen in-process, and an event whose attempts all fail is dropped.

## API

### `public WebhookDispatcher()`

Creates a dispatcher with its own `HttpClient`, using default settings (no custom timeout, headers, or handler).

### `public void RegisterWebhook(WebhookConfig config)`

Adds a webhook to the dispatcher's list. The list is not checked for duplicates, so registering the same `WebhookConfig` twice delivers each event twice.

- **Parameters**
  - `config`: The webhook to register. Its `Url` must not be `null`, empty, or whitespace.
- **Throws**
  - `ArgumentNullException`: If `config` is `null`.
  - `ArgumentException`: If `config.Url` is `null`, empty, or whitespace (parameter name `config`).
- **Notes**
  - The URL is not checked to be absolute, and `EventType` is not validated.
  - The `WebhookConfig` instance is stored by reference. Changes to its properties, such as `IsActive`, take effect on the next dispatch.

### `public bool UnregisterWebhook(Guid webhookId)`

Removes the webhook whose `WebhookConfig.Id` matches `webhookId`.

- **Parameters**
  - `webhookId`: The `Id` of the webhook to remove.
- **Returns**
  - `true` if a webhook was removed; `false` if no webhook has that `Id`.
- **Notes**
  - Deliveries already in progress for the removed webhook continue until they finish or exhaust their retries.

### `public Task DispatchEventAsync(IDomainEvent @event)`

Delivers `@event` to every active webhook whose `EventType` is `"*"` or equals `@event.EventType`.

- **Parameters**
  - `@event`: The event to deliver. A `null` value is ignored.
- **Returns**
  - A `Task` that completes when every matching delivery has succeeded or run out of attempts. If no webhook matches, the task completes immediately.
- **Notes**
  - Matching webhooks are captured under a lock when the call starts. Webhooks registered or removed during the call do not affect it.
  - The method does not throw for delivery failures, so awaiting it does not reveal whether the endpoints accepted the event. Check the endpoint side or the debug output.
  - The XML summary of this method says it "executes asynchronously without blocking the caller". That describes the `async` signature. The caller that awaits the task does wait for all deliveries.

### `public IReadOnlyList<WebhookConfig> GetWebhooks()`

Returns a snapshot of all registered webhooks, active or not.

- **Returns**
  - A read-only list copied at call time. Later registrations or removals do not change it. The `WebhookConfig` objects inside it are the same instances the dispatcher uses.

### `public void Dispose()`

Disposes the internal `HttpClient`. It does not cancel in-flight deliveries, and dispatching after disposal fails each attempt, which is logged and retried like any other failure.

## Retry policy

Retry behavior is controlled by two `WebhookConfig` properties. Their defaults are set on `WebhookConfig`; the fallbacks below apply only when a property is set to `null`.

| `WebhookConfig` property | Default | If `null` | Meaning |
|---|---|---|---|
| `MaxRetries` (`int?`) | `3` | `1` | **Total** number of attempts per event, including the first. |
| `RetryDelay` (`TimeSpan?`) | `5 s` | `1 s` | Wait between consecutive attempts. No wait follows the last attempt. |

With the defaults, one event produces up to 3 POST requests with 5 seconds between them, so a permanently failing endpoint delays `DispatchEventAsync` by about 10 seconds.

`MaxRetries` is named as if it counted retries, but it counts attempts. `MaxRetries = 1` sends once and never retries. `MaxRetries = 0` or a negative value sends nothing.

The code comment says retries happen only for 5xx and network errors. The code does not do that: every non-2xx status (4xx included) is retried until attempts run out.

## Usage

The example uses a small event type that implements `IDomainEvent` from `DotNetActorFramework.Events`. A real application would usually define its own event types. Put the event type in its own file: a file with top-level statements cannot declare types before those statements.

`ActorFailedEvent.cs`:

```csharp
using DotNetActorFramework.Events;

public sealed record ActorFailedEvent(
    Guid EventId,
    DateTime OccurredAt,
    string EventType,
    string ActorPath,
    string Reason) : IDomainEvent;
```

`Program.cs`:

```csharp
using DotNetActorFramework.Integration;

using var dispatcher = new WebhookDispatcher();

var webhook = new WebhookConfig
{
    Url = "https://hooks.example.com/actor-errors",
    EventType = "actor.error",
    MaxRetries = 3,                              // total attempts
    RetryDelay = TimeSpan.FromSeconds(2)         // wait between attempts
};

dispatcher.RegisterWebhook(webhook);

// Awaits every matching delivery (or its final failure) before returning.
await dispatcher.DispatchEventAsync(new ActorFailedEvent(
    Guid.NewGuid(),
    DateTime.UtcNow,
    "actor.error",
    "/root/orders",
    "Mailbox overflow"));

// Stop sending to this endpoint.
dispatcher.UnregisterWebhook(webhook.Id);
```

To receive every event type, set `EventType = "*"`. To pause an endpoint without removing it, set `IsActive = false`.

## Thread safety

Registration, removal, snapshotting, and dispatch matching all lock on one internal object. Calling `DispatchEventAsync` from several threads at once is safe. The `WebhookConfig` instances are not synchronized, so changing their properties while a dispatch is in progress is a data race. Change them before registering, or unregister and register a new instance.
