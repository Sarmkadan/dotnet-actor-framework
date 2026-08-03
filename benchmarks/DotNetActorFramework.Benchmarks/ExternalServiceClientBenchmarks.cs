using BenchmarkDotNet.Attributes;
using DotNetActorFramework.Integration;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DotNetActorFramework.Benchmarks;

[MemoryDiagnoser]
public class ExternalServiceClientBenchmarks
{
    private ExternalServiceClient _client;
    private const string BaseUrl = "http://localhost:5000";

    [Params(1, 3)]
    public int MaxRetries;

    [GlobalSetup]
    public void Setup()
    {
        _client = new ExternalServiceClient(BaseUrl, MaxRetries, TimeSpan.FromMilliseconds(10));
    }

    [Benchmark]
    public async Task GetAsyncBenchmark()
    {
        try
        {
            // This is expected to fail or timeout due to no real server
            await _client.GetAsync<object>("test");
        }
        catch
        {
            // Expected
        }
    }

    [Benchmark]
    public async Task PostAsyncBenchmark()
    {
        try
        {
            var body = new { Name = "Test", Value = 123 };
            await _client.PostAsync<object>("test", body);
        }
        catch
        {
            // Expected
        }
    }

    [Benchmark]
    public async Task PutAsyncBenchmark()
    {
        try
        {
            var body = new { Name = "Test", Value = 123 };
            await _client.PutAsync<object>("test", body);
        }
        catch
        {
            // Expected
        }
    }

    [Benchmark]
    public async Task DeleteAsyncBenchmark()
    {
        try
        {
            await _client.DeleteAsync("test");
        }
        catch
        {
            // Expected
        }
    }
}
