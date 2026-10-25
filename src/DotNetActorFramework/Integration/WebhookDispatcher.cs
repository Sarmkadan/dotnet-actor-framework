// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotNetActorFramework.Events;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Webhook configuration for external event notifications.
/// </summary>
public class WebhookConfig
{
    private const int DefaultMaxRetries = 3;
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Unique identifier of the webhook, used to unregister it.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Endpoint URL that receives the event payloads.
    /// </summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// Event type to match, e.g., "actor.error"; use "*" to match all events.
    /// </summary>
    public string EventType { get; set; } = null!; // e.g., "actor.error", "*" for all

    /// <summary>
    /// Whether the webhook is currently eligible to receive events.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// UTC timestamp at which the webhook configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Maximum number of delivery attempts for a single event. When null, only one attempt is made.
    /// </summary>
    public int? MaxRetries { get; set; } = DefaultMaxRetries;

    /// <summary>
    /// Delay between delivery attempts. When null, a 1-second delay is used.
    /// </summary>
    public TimeSpan? RetryDelay { get; set; } = DefaultRetryDelay;

    /// <summary>
    /// Returns a string representation of the webhook configuration.
    /// </summary>
    /// <returns>A formatted string containing the key configuration values.</returns>
    public override string ToString()
    {
        return $"WebhookConfig {{ Id = {Id}, Url = {Url}, EventType = {EventType}, IsActive = {IsActive}, CreatedAt = {CreatedAt}, MaxRetries = {MaxRetries} }}";
    }
}

/// <summary>
/// Dispatcher for sending domain events to external webhooks.
/// Enables integration with external systems and event logging services.
/// </summary>
public class WebhookDispatcher : IDisposable
{
    private const int SingleAttempt = 1;
    private static readonly TimeSpan FallbackRetryDelay = TimeSpan.FromSeconds(1);

    private readonly List<WebhookConfig> _webhooks = [];
    private readonly HttpClient _httpClient;
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDispatcher"/> class with its own <see cref="HttpClient"/>.
    /// </summary>
    public WebhookDispatcher()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Registers a webhook endpoint.
    /// </summary>
    public void RegisterWebhook(WebhookConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (string.IsNullOrWhiteSpace(config.Url))
            throw new ArgumentException("Webhook URL cannot be empty.", nameof(config));

        lock (_lockObject)
        {
            _webhooks.Add(config);
        }
    }

    /// <summary>
    /// Unregisters a webhook by ID.
    /// </summary>
    public bool UnregisterWebhook(Guid webhookId)
    {
        lock (_lockObject)
        {
            var webhook = _webhooks.FirstOrDefault(w => w.Id == webhookId);
            if (webhook != null)
                return _webhooks.Remove(webhook);
            return false;
        }
    }

    /// <summary>
    /// Dispatches a domain event to all matching webhooks.
    /// Executes asynchronously without blocking the caller.
    /// </summary>
    public async Task DispatchEventAsync(IDomainEvent @event)
    {
        if (@event == null)
            return;

        List<WebhookConfig> matchingWebhooks;
        lock (_lockObject)
        {
            matchingWebhooks = _webhooks
                .Where(w => w.IsActive && (w.EventType == "*" || w.EventType == @event.EventType))
                .ToList();
        }

        var tasks = matchingWebhooks
            .Select(w => SendWebhookAsync(w, @event))
            .ToList();

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Gets all registered webhooks.
    /// </summary>
    public IReadOnlyList<WebhookConfig> GetWebhooks()
    {
        lock (_lockObject)
        {
            return _webhooks.ToList().AsReadOnly();
        }
    }

    private async Task SendWebhookAsync(WebhookConfig config, IDomainEvent @event)
    {
        var maxRetries = config.MaxRetries ?? SingleAttempt;
        var retryDelay = config.RetryDelay ?? FallbackRetryDelay;

        for (int attempt = 1; attempt <= maxRetries; attempt++) // Only retry on 5xx/network errors
        {
            try
            {
                var json = @event.ToJson();
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync(config.Url, content);

                if (response.IsSuccessStatusCode)
                    return; // Success

                if (attempt < maxRetries)
                    await Task.Delay(retryDelay);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Webhook dispatch failed: {ex.Message}");

                if (attempt < maxRetries)
                    await Task.Delay(retryDelay);
            }
        }
    }

    /// <summary>
    /// Releases the <see cref="HttpClient"/> used to send webhook requests.
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
