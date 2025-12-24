using BenchmarkDotNet.Attributes;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Benchmarks;

[MemoryDiagnoser]
public class ActorSystemBenchmarks
{
    private ActorSystem? _system;
    private ActorPath _path;

    [GlobalSetup]
    public void Setup()
    {
        _system = new ActorSystem("BenchmarkSystem");
        _path = new ActorPath("/test-actor");
    }

    [Benchmark]
    public async Task<ActorRef> CreateActorAsync()
    {
        return await _system!.CreateActorAsync(new ActorPath("/new-actor-" + Guid.NewGuid()));
    }

    [Benchmark]
    public void GetActorRef()
    {
        _system!.GetActorRef(_path);
    }

    [Benchmark]
    public SystemHealthSummary GetHealthSummary()
    {
        return _system!.GetHealthSummary();
    }
}
