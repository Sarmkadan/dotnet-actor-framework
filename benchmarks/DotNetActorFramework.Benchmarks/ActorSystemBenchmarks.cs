using BenchmarkDotNet.Attributes;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Benchmarks;

/// <summary>
/// Benchmark class for ActorSystem performance.
/// </summary>
[MemoryDiagnoser]
public class ActorSystemBenchmarks
{
    /// <summary>
    /// The ActorSystem instance used for benchmarking.
    /// </summary>
    private ActorSystem? _system;

    /// <summary>
    /// The ActorPath used for benchmarking.
    /// </summary>
    private ActorPath _path;

    /// <summary>
    /// Sets up the ActorSystem and ActorPath for benchmarking.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _system = new ActorSystem("BenchmarkSystem");
        _path = new ActorPath("/test-actor");
    }

    /// <summary>
    /// Creates a new ActorRef asynchronously and measures the time it takes.
    /// </summary>
    /// <returns>The created ActorRef.</returns>
    [Benchmark]
    public async Task<ActorRef> CreateActorAsync()
    {
        return await _system!.CreateActorAsync(new ActorPath("/new-actor-" + Guid.NewGuid()));
    }

    /// <summary>
    /// Gets the ActorRef for the specified ActorPath and measures the time it takes.
    /// </summary>
    [Benchmark]
    public void GetActorRef()
    {
        _system!.GetActorRef(_path);
    }

    /// <summary>
    /// Gets the SystemHealthSummary for the ActorSystem and measures the time it takes.
    /// </summary>
    /// <returns>The SystemHealthSummary.</returns>
    [Benchmark]
    public SystemHealthSummary GetHealthSummary()
    {
        return _system!.GetHealthSummary();
    }
}
