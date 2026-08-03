using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using DotNetActorFramework.Integration;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Benchmarks
{
    [MemoryDiagnoser]
    public class HttpActorClientBenchmarks
    {
        private HttpActorClient _client = null!;
        private MockHttpMessageHandler _mockHandler = null!;
        private const string BaseUrl = "http://localhost:5000";

        [Params(10, 100, 1000)]
        public int MessageSize { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _mockHandler = new MockHttpMessageHandler();
            var httpClient = new HttpClient(_mockHandler) { BaseAddress = new Uri(BaseUrl) };
            _client = new HttpActorClient(BaseUrl);
            // Replace the internal HttpClient with our mock using reflection
            var httpClientField = typeof(HttpActorClient).GetField("_httpClient", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (httpClientField != null)
            {
                httpClientField.SetValue(_client, httpClient);
            }
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _client.Dispose();
            _mockHandler.Dispose();
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public async Task Benchmark_SendMessageAsync(int messageCount)
        {
            // Arrange
            var message = new Message<TestMessagePayload>(new TestMessagePayload { Id = 1, Data = new string('x', MessageSize) });

            // Act
            for (int i = 0; i < messageCount; i++)
            {
                await _client.SendMessageAsync("actor/123", message);
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public async Task Benchmark_GetActorStateAsync(int requestCount)
        {
            // Act
            for (int i = 0; i < requestCount; i++)
            {
                await _client.GetActorStateAsync<TestState>("actor/123");
            }
        }

        [Benchmark]
        [Arguments(10)]
        [Arguments(100)]
        [Arguments(1000)]
        public async Task Benchmark_GetActorHealthAsync(int requestCount)
        {
            // Act
            for (int i = 0; i < requestCount; i++)
            {
                await _client.GetActorHealthAsync("actor/123");
            }
        }

        private class TestMessagePayload
        {
            public int Id { get; set; }
            public string Data { get; set; } = string.Empty;
        }

        private class TestState
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };

                // Simulate different responses based on request
                if (request.RequestUri?.ToString().Contains("health") == true)
                {
                    response.Content = new StringContent("{\"isHealthy\":true}", Encoding.UTF8, "application/json");
                }
                else if (request.RequestUri?.ToString().Contains("actor/") == true && request.Method == HttpMethod.Get)
                {
                    response.Content = new StringContent("{\"id\":1,\"name\":\"test\"}", Encoding.UTF8, "application/json");
                }
                else if (request.Method == HttpMethod.Post && request.RequestUri?.ToString().Contains("actor/") == true)
                {
                    // Simulate successful message send
                    response.Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json");
                }

                return Task.FromResult(response);
            }
        }
    }
}