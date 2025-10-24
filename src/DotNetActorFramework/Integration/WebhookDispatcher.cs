// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
using DotNetActorFramework.Events;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Webhook configuration for external event notifications.
/// </summary>
public class WebhookConfig
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; }
    public string EventType { get; set; } // e.g., "actor.error", "*" for all
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? MaxRetries { get; set; } = 3;
    public TimeSpan? RetryDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Dispatcher for sending domain events to external webhooks.
/// Enables integration with external systems and event logging services.
/// </summary>
public class WebhookDispatcher
{
    private readonly List<WebhookConfig> _webhooks = [];
    private readonly HttpClient _httpClient;
    private readonly object _lockObject = new();

    public WebhookDispatcher()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Registers a webhook endpoint.
    /// </summary>
    public void RegisterWebhook(WebhookConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

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
        var maxRetries = config.MaxRetries ?? 1;
        var retryDelay = config.RetryDelay ?? TimeSpan.FromSeconds(1);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var json = @event.ToJson();
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(config.Url, content);

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

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
