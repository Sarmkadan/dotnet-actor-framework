// 09-advanced-usage.cs
// Demonstrates advanced configuration including middleware and metrics.

using System;
using System.Threading.Tasks;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;

public class AdvancedUsage
{
    public static async Task RunAsync()
    {
        // 1. Initialize builder with advanced configurations
        var builder = new ActorSystemBuilder("AdvancedSystem");

        // 2. Configure middleware pipeline
        builder
            .WithLogging()
            .WithErrorHandling(ErrorHandlingStrategy.Restart)
            .WithRateLimiting(tokensPerSecond: 500)
            .WithMetrics()
            .WithCaching(maxCapacity: 5000);

        // 3. Build the system components
        var actorSystem = builder.Build();
        var pipeline = builder.BuildMiddlewarePipeline();
        var metrics = builder.GetMetricsCollector();

        Console.WriteLine($"System '{actorSystem.Name}' initialized with advanced middleware.");
        
        if (metrics != null)
        {
            Console.WriteLine("Metrics collection enabled.");
        }

        await Task.CompletedTask;
    }
}
