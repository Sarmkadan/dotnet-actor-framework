```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04 LTS (Resolute Raccoon)
AMD EPYC-Rome Processor 2.45GHz, 1 CPU, 16 logical and 16 physical cores
.NET SDK 10.0.300
  [Host]     : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3


```
| Method           | Mean        | Error     | StdDev     | Gen0   | Gen1   | Allocated |
|----------------- |------------:|----------:|-----------:|-------:|-------:|----------:|
| CreateActorAsync | 4,016.39 ns | 79.030 ns | 110.788 ns | 0.1335 | 0.0648 |    1136 B |
| GetActorRef      |    12.22 ns |  0.242 ns |   0.288 ns |      - |      - |         - |
| GetHealthSummary |    37.84 ns |  0.816 ns |   1.429 ns | 0.0095 |      - |      80 B |
