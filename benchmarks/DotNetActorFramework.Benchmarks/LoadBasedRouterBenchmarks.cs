using BenchmarkDotNet.Attributes;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Routing;
using DotNetActorFramework.Services;

namespace DotNetActorFramework.Benchmarks;

/// <summary>
/// Compares the full-scan (<see cref="LoadBasedRouter.GetLeastLoaded"/>) and
/// power-of-two-choices (<see cref="LoadBasedRouter.SampleLeastLoaded"/>) routing
/// strategies at increasing routee-pool sizes.
/// </summary>
[MemoryDiagnoser]
public class LoadBasedRouterBenchmarks
{
    private const string Capability = "benchmark-capability";

    /// <summary>
    /// The number of routees registered under <see cref="Capability"/> for a given
    /// benchmark iteration.
    /// </summary>
    [Params(8, 64, 256)]
    public int RouteeCount { get; set; }

    private LoadBasedRouter? _router;
    private ActorSystem? _system;
    private MailboxService? _mailboxService;

    /// <summary>
    /// Builds a router with <see cref="RouteeCount"/> live routees, each carrying a
    /// randomized mailbox depth so neither strategy benchmarks a degenerate all-zero pool.
    /// </summary>
    [GlobalSetup]
    public async Task Setup()
    {
        var options = new ActorSystemOptions();
        _mailboxService = new MailboxService(options);
        var discovery = new ActorDiscoveryService();
        var registry = new ActorRegistry();
        _system = new ActorSystem("router-benchmark-system");
        var dispatcher = new MessageDispatcher(_mailboxService, registry, _system);

        var random = new Random(42);
        for (var i = 0; i < RouteeCount; i++)
        {
            var actorRef = await _system.CreateActorAsync(new ActorPath($"/routee-{i}"));
            discovery.Register(actorRef, [Capability]);

            var mailbox = _mailboxService.CreateMailbox(actorRef.Id, capacity: 1000);
            var depth = random.Next(0, 50);
            for (var m = 0; m < depth; m++)
                await mailbox.EnqueueAsync(new Envelope(new ControlMessage("seed"), actorRef));
        }

        _router = new LoadBasedRouter(discovery, _mailboxService, dispatcher);
    }

    /// <summary>
    /// Selects a routee via the O(n) full-scan strategy.
    /// </summary>
    [Benchmark(Baseline = true)]
    public ActorRef? FullScan() => _router!.GetLeastLoaded(Capability);

    /// <summary>
    /// Selects a routee via the constant-cost power-of-two-choices strategy.
    /// </summary>
    [Benchmark]
    public ActorRef? PowerOfTwoChoices() => _router!.SampleLeastLoaded(Capability);
}
