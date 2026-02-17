// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net;
using System.Text;
using System.Text.Json;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Integration;

/// <summary>
/// HTTP client for communicating with actors over HTTP.
/// Enables REST-based actor invocation and message sending.
/// </summary>
public class HttpActorClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpActorClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty.", nameof(baseUrl));

        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <summary>
    /// Sends a message to an actor via HTTP POST.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when actorPath is empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when message is null.</exception>
    /// <exception cref="HttpActorCommunicationException">Thrown when HTTP communication fails.</exception>
    public async Task<HttpResponseMessage> SendMessageAsync(string actorPath, Message message)
    {
        if (string.IsNullOrWhiteSpace(actorPath))
            throw new ArgumentException("Actor path cannot be empty.", nameof(actorPath));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var url = $"{_baseUrl}/actors/{actorPath}/messages";
        var json = message.ToJson();
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                throw new HttpActorCommunicationException(url, response.StatusCode, responseContent);
            }
            return response;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpActorCommunicationException(url, HttpStatusCode.ServiceUnavailable, null, ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HttpActorCommunicationException(url, HttpStatusCode.RequestTimeout, "Request timeout", ex);
        }
        catch (Exception ex)
        {
            throw new HttpActorCommunicationException(url, HttpStatusCode.InternalServerError, null, ex);
        }
    }

    /// <summary>
    /// Gets an actor's state via HTTP GET.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when actorPath is empty.</exception>
    /// <exception cref="HttpActorCommunicationException">Thrown when HTTP communication fails.</exception>
    public async Task<T?> GetActorStateAsync<T>(string actorPath)
    {
        if (string.IsNullOrWhiteSpace(actorPath))
            throw new ArgumentException("Actor path cannot be empty.", nameof(actorPath));

        var url = $"{_baseUrl}/actors/{actorPath}/state";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content.FromJson<T>();
            }
            return default;
        }
        catch (HttpRequestException ex)
        {
            throw new HttpActorCommunicationException(url, HttpStatusCode.ServiceUnavailable, null, ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new HttpActorCommunicationException(url, HttpStatusCode.RequestTimeout, "Request timeout", ex);
        }
        catch (Exception ex)
        {
            throw new HttpActorCommunicationException(url, HttpStatusCode.InternalServerError, null, ex);
        }
    }

    /// <summary>
    /// Gets actor health status via HTTP GET.
    /// </summary>
    public async Task<ActorHealthStatus?> GetActorHealthAsync(string actorPath)
    {
        if (string.IsNullOrWhiteSpace(actorPath))
            throw new ArgumentException("Actor path cannot be empty.", nameof(actorPath));

        var url = $"{_baseUrl}/actors/{actorPath}/health";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content.FromJson<ActorHealthStatus>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets system health status via HTTP GET.
    /// </summary>
    public async Task<SystemHealthStatus?> GetSystemHealthAsync()
    {
        var url = $"{_baseUrl}/health";

        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content.FromJson<SystemHealthStatus>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Health status of an actor.
/// </summary>
public class ActorHealthStatus
{
    public string ActorPath { get; set; }
    public Guid ActorId { get; set; }
    public string State { get; set; }
    public long MessageCount { get; set; }
    public long ErrorCount { get; set; }
    public double ErrorRate { get; set; }
    public bool IsHealthy { get; set; }
}

/// <summary>
/// Health status of the entire system.
/// </summary>
public class SystemHealthStatus
{
    public string SystemName { get; set; }
    public Guid SystemId { get; set; }
    public int TotalActors { get; set; }
    public int HealthyActors { get; set; }
    public int UnhealthyActors { get; set; }
    public long TotalMessages { get; set; }
    public long TotalErrors { get; set; }
    public double ErrorRate { get; set; }
    public bool IsHealthy { get; set; }
}
