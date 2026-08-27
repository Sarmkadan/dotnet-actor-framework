using BenchmarkDotNet.Attributes;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Benchmarks;

/// <summary>
/// Benchmark class for DotnetActorFrameworkException performance.
/// </summary>
[MemoryDiagnoser]
public class DotnetActorFrameworkExceptionBenchmarks
{
    private const string TestMessage = "Test exception message";
    private const string TestFormat = "Test message with param: {0}";
    private static readonly object[] TestArgs = { "paramValue" };
    private Exception _innerException = null!;

    [GlobalSetup]
    public void Setup()
    {
        _innerException = new InvalidOperationException("Inner exception");
    }

    [Benchmark]
    public DotnetActorFrameworkException DefaultConstructor()
    {
        return new DotnetActorFrameworkException();
    }

    [Benchmark]
    public DotnetActorFrameworkException ConstructorWithMessage()
    {
        return new DotnetActorFrameworkException(TestMessage);
    }

    [Benchmark]
    public DotnetActorFrameworkException ConstructorWithMessageAndInner()
    {
        return new DotnetActorFrameworkException(TestMessage, _innerException);
    }

    [Benchmark]
    public DotnetActorFrameworkException CreateMethod()
    {
        return DotnetActorFrameworkException.Create(TestFormat, TestArgs);
    }

    [Benchmark]
    public DotnetActorFrameworkException CreateMethodWithInner()
    {
        return DotnetActorFrameworkException.Create(_innerException, TestFormat, TestArgs);
    }
}