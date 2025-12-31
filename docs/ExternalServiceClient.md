# ExternalServiceClient
The `ExternalServiceClient` class is designed to facilitate communication with external services, providing a simple and intuitive interface for sending HTTP requests and retrieving responses. It allows users to perform common HTTP operations such as GET, POST, PUT, and DELETE, with support for asynchronous execution and disposable resources.

## API
* `public ExternalServiceClient`: The constructor for the `ExternalServiceClient` class, used to create a new instance.
* `public async Task<T?> GetAsync<T>`: Sends a GET request to the external service and returns the response deserialized to the specified type `T`. The method returns `null` if the response is empty or an error occurs. It throws an exception if the request fails or the response cannot be deserialized.
* `public async Task<T?> PostAsync<T>`: Sends a POST request to the external service with the provided data and returns the response deserialized to the specified type `T`. The method returns `null` if the response is empty or an error occurs. It throws an exception if the request fails or the response cannot be deserialized.
* `public async Task<T?> PutAsync<T>`: Sends a PUT request to the external service with the provided data and returns the response deserialized to the specified type `T`. The method returns `null` if the response is empty or an error occurs. It throws an exception if the request fails or the response cannot be deserialized.
* `public async Task<bool> DeleteAsync`: Sends a DELETE request to the external service and returns a boolean indicating whether the operation was successful. It throws an exception if the request fails.
* `public void Dispose`: Releases any unmanaged resources held by the `ExternalServiceClient` instance.

## Usage
The following examples demonstrate how to use the `ExternalServiceClient` class to send HTTP requests to an external service:
```csharp
// Example 1: Sending a GET request
var client = new ExternalServiceClient();
var response = await client.GetAsync<MyResponse>("https://example.com/api/data");
if (response != null)
{
    Console.WriteLine(response.Data);
}

// Example 2: Sending a POST request
var client = new ExternalServiceClient();
var requestData = new MyRequestData { Name = "John Doe", Age = 30 };
var response = await client.PostAsync<MyResponse>("https://example.com/api/create", requestData);
if (response != null)
{
    Console.WriteLine(response.Id);
}
```

## Notes
When using the `ExternalServiceClient` class, consider the following edge cases and thread-safety remarks:
* The `ExternalServiceClient` instance is not thread-safe, and concurrent access to its methods may result in unexpected behavior. It is recommended to create a new instance for each thread or use a thread-safe wrapper.
* The `Dispose` method should be called when the `ExternalServiceClient` instance is no longer needed to release any unmanaged resources.
* The `GetAsync`, `PostAsync`, and `PutAsync` methods return `null` if the response is empty or an error occurs. It is essential to check for `null` before attempting to access the response data.
* The `DeleteAsync` method returns a boolean indicating whether the operation was successful. It is crucial to check the return value to determine the outcome of the request.
