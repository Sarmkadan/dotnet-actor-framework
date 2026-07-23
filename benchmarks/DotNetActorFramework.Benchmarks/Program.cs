using BenchmarkDotNet.Running;
using DotNetActorFramework.Benchmarks;

BenchmarkSwitcher.FromTypes(
[
    typeof(ActorSystemBenchmarks),
    typeof(LoadBasedRouterBenchmarks)
]).Run(args);
