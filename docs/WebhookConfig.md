# WebhookConfig

Represents a configuration for an outgoing webhook in the actor framework. This type encapsulates the settings required to register, manage, and dispatch HTTP callbacks to a specified URL when a particular event occurs. It supports retry logic, activation/deactivation, and lifecycle management for webhook subscriptions.

## API

### `public Guid Id`
A unique identifier for the webhook configuration. This value is generated upon creation and persists for the lifetime of the configuration.

### `public string Url`
The target HTTP endpoint to which the webhook payload will be sent. Must be a valid, absolute URI. Throws `ArgumentException` if set to an invalid or relative URI.

### `public string EventType`
A string identifier for the type of event this webhook is subscribed to. Used to filter and route events during dispatch. Throws `ArgumentException` if set to `null` or whitespace.

### `public bool IsActive`
Indicates whether the webhook is currently active. Inactive webhooks are not dispatched. Defaults to `true` upon creation.

### `public DateTime CreatedAt`
The UTC timestamp when the webhook configuration was created. This value is set automatically and is immutable.

### `public int? MaxRetries`
The maximum number of retry attempts for failed webhook deliveries. If `null`, no retries are attempted. Must be non-negative if set. Throws `ArgumentOutOfRangeException` if set to a negative value.

### `public TimeSpan? RetryDelay`
The delay between retry attempts for failed webhook deliveries. If `null`, a default delay is used. Must be positive if set. Throws `ArgumentOutOfRangeException` if set to a non-positive value.

### `public WebhookDispatcher Dispatcher`
The dispatcher responsible for executing the webhook delivery. This property is set internally during registration and should not be modified directly.

### `public void RegisterWebhook()`
Registers the webhook with the dispatcher, enabling event delivery. Throws `InvalidOperationException` if the webhook is already registered or if required properties (`Url`, `EventType`) are not set.

### `public bool UnregisterWebhook()`
Unregisters the webhook, preventing further event delivery. Returns `true` if the webhook was successfully unregistered, `false` if it was not registered. Safe to call multiple times.

### `public async Task DispatchEventAsync(object eventData, CancellationToken cancellationToken = default)`
Asynchronously dispatches the provided event data to the configured `Url`. Implements retry logic based on `MaxRetries` and `RetryDelay`. Throws:
- `InvalidOperationException` if the webhook is not registered or inactive.
- `HttpRequestException` if the HTTP request fails after all retry attempts.
- `OperationCanceledException` if the `cancellationToken` is triggered.

### `public IReadOnlyList<WebhookConfig> GetWebhooks()`
Returns a read-only list of all registered webhook configurations. The returned list is a snapshot and does not reflect subsequent changes.

### `public void Dispose()`
Releases resources associated with the webhook, including unregistering it if active. Safe to call multiple times. Implements `IDisposable` for deterministic cleanup.

## Usage

### Example 1: Registering and Dispatching a Webhook
