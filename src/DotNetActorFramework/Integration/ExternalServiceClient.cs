// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Net.Http.Json;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Generic HTTP client for integrating with external services.
/// Provides convenient methods for making REST API calls with error handling and retries.
/// </summary>
public class ExternalServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly int _maxRetries;
    private readonly TimeSpan _retryDelay;

    public ExternalServiceClient(string baseUrl, int maxRetries = 3, TimeSpan? retryDelay = null)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new ArgumentException("Base URL cannot be empty.", nameof(baseUrl));

        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient();
        _maxRetries = maxRetries > 0 ? maxRetries : 1;
        _retryDelay = retryDelay ?? TimeSpan.FromMilliseconds(500);
    }

    /// <summary>
    /// Makes a GET request to the external service.
    /// </summary>
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));

        var url = CombineUrl(endpoint);
        return await RetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            return content.FromJson<T>();
        });
    }

    /// <summary>
    /// Makes a POST request with JSON body.
    /// </summary>
    public async Task<T?> PostAsync<T>(string endpoint, object body)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));

        var url = CombineUrl(endpoint);
        var json = body.ToJson();
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        return await RetryAsync(async () =>
        {
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent.FromJson<T>();
        });
    }

    /// <summary>
    /// Makes a PUT request with JSON body.
    /// </summary>
    public async Task<T?> PutAsync<T>(string endpoint, object body)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));

        var url = CombineUrl(endpoint);
        var json = body.ToJson();
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        return await RetryAsync(async () =>
        {
            var response = await _httpClient.PutAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return responseContent.FromJson<T>();
        });
    }

    /// <summary>
    /// Makes a DELETE request.
    /// </summary>
    public async Task<bool> DeleteAsync(string endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint cannot be empty.", nameof(endpoint));

        var url = CombineUrl(endpoint);
        return await RetryAsync(async () =>
        {
            var response = await _httpClient.DeleteAsync(url);
            response.EnsureSuccessStatusCode();
            return true;
        });
    }

    /// <summary>
    /// Makes a request with automatic retry on failure.
    /// </summary>
    private async Task<T> RetryAsync<T>(Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (HttpRequestException) when (attempt < _maxRetries)
            {
                await Task.Delay(_retryDelay);
            }
        }

        return await operation(); // Final attempt without catch
    }

    private string CombineUrl(string endpoint)
    {
        if (endpoint.StartsWith("/"))
            return _baseUrl + endpoint;
        return $"{_baseUrl}/{endpoint}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
