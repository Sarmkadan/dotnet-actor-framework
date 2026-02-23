// ... (rest of the file remains unchanged)

## WebhookConfig

The `WebhookConfig` represents a configuration for a webhook endpoint. It defines how and when to dispatch events to an external service.

### Usage Example

```csharp
var webhookConfig = new WebhookConfig
{
    Url = "https://example.com/webhooks",
    EventType = "order.placed",
    IsActive = true,
    MaxRetries = 3,
    RetryDelay = TimeSpan.FromSeconds(5)
};

var webhookDispatcher = new WebhookDispatcher();
webhookDispatcher.RegisterWebhook(webhookConfig);
```

// ... (rest of the file remains unchanged)
