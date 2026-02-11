using System;
using System.Threading.Tasks;

namespace DotNetActorFramework.Integration;

/// <summary>
/// Provides extension methods for <see cref="ExternalServiceClient"/> that offer safe, fire-and-forget style HTTP operations
/// with automatic exception handling and graceful degradation.
/// </summary>
public static class ExternalServiceClientExtensions
{
    /// <summary>
    /// Attempts to delete a resource at the specified endpoint.
    /// </summary>
    /// <param name="client">The HTTP client to use for the request.</param>
    /// <param name="endpoint">The endpoint to delete from, relative to the client's base URL.</param>
    /// <returns><see langword="true"/> if the deletion was successful; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or empty.</exception>
    public static async Task<bool> TryDeleteAsync(this ExternalServiceClient client, string endpoint)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        try
        {
            return await client.DeleteAsync(endpoint);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to retrieve a resource from the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The type of resource to retrieve.</typeparam>
    /// <param name="client">The HTTP client to use for the request.</param>
    /// <param name="endpoint">The endpoint to get from, relative to the client's base URL.</param>
    /// <returns>The deserialized resource if successful; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or empty.</exception>
    public static async Task<T?> TryGetAsync<T>(this ExternalServiceClient client, string endpoint) where T : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);

        try
        {
            return await client.GetAsync<T>(endpoint);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to post data to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The type of response to expect.</typeparam>
    /// <param name="client">The HTTP client to use for the request.</param>
    /// <param name="endpoint">The endpoint to post to, relative to the client's base URL.</param>
    /// <param name="body">The request body to send.</param>
    /// <returns>The deserialized response if successful; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    public static async Task<T?> TryPostAsync<T>(this ExternalServiceClient client, string endpoint, object body) where T : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            return await client.PostAsync<T>(endpoint, body);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to put data to the specified endpoint.
    /// </summary>
    /// <typeparam name="T">The type of response to expect.</typeparam>
    /// <param name="client">The HTTP client to use for the request.</param>
    /// <param name="endpoint">The endpoint to put to, relative to the client's base URL.</param>
    /// <param name="body">The request body to send.</param>
    /// <returns>The deserialized response if successful; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="endpoint"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="body"/> is <see langword="null"/>.</exception>
    public static async Task<T?> TryPutAsync<T>(this ExternalServiceClient client, string endpoint, object body) where T : class
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(endpoint);
        ArgumentNullException.ThrowIfNull(body);

        try
        {
            return await client.PutAsync<T>(endpoint, body);
        }
        catch
        {
            return null;
        }
    }
}
