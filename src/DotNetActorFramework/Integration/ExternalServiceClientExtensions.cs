using System;
using System.Threading.Tasks;

namespace DotNetActorFramework.Integration;

public static class ExternalServiceClientExtensions
{
    public static async Task<bool> TryDeleteAsync(this ExternalServiceClient client, string endpoint)
    {
        try
        {
            return await client.DeleteAsync(endpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting from {endpoint}: {ex.Message}");
            return false;
        }
    }

    public static async Task<T?> TryGetAsync<T>(this ExternalServiceClient client, string endpoint) where T : class
    {
        try
        {
            return await client.GetAsync<T>(endpoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting from {endpoint}: {ex.Message}");
            return null;
        }
    }

    public static async Task<T?> TryPostAsync<T>(this ExternalServiceClient client, string endpoint, object body) where T : class
    {
        try
        {
            return await client.PostAsync<T>(endpoint, body);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error posting to {endpoint}: {ex.Message}");
            return null;
        }
    }
}
