// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Example 5: Metrics and Monitoring
// Demonstrates built-in metrics collection and system health monitoring

public class ProcessorActor : Actor
{
    public ProcessorActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "process")
        {
            // Simulate processing delay
            var delay = (int)cm.Parameters["delay"];
            await Task.Delay(delay);

            Metrics.RecordSuccess();
            Console.WriteLine($"[{Path}] Processed message (delayed {delay}ms)");
        }
        await Task.CompletedTask;
    }
}

public class MonitorActor : Actor
{
    private readonly MessageDispatcher _dispatcher;
    private readonly ActorSystemConfiguration _config;
    private Timer? _monitoringTimer;

    public MonitorActor(ActorPath path, MessageDispatcher dispatcher,
        ActorSystemConfiguration config) : base(path)
    {
        _dispatcher = dispatcher;
        _config = config;
    }

    public override async Task OnInitializeAsync()
    {
        Console.WriteLine($"[{Path}] Monitor started. Reporting every 2 seconds.\n");

        _monitoringTimer = new Timer(_ => ReportMetrics(), null,
            TimeSpan.Zero, TimeSpan.FromSeconds(2));

        await Task.CompletedTask;
    }

    private void ReportMetrics()
    {
        var health = _config.GetHealthSummary();
        var stats = _config.GetStatistics();

        Console.WriteLine("╔════════════════════════════════════════════════════════╗");
        Console.WriteLine("║              SYSTEM METRICS REPORT                     ║");
        Console.WriteLine("╠════════════════════════════════════════════════════════╣");

        Console.WriteLine($"║ ACTOR SYSTEM STATUS                                   ║");
        Console.WriteLine($"║   Total Actors: {health.TotalActors,-44} ║");
        Console.WriteLine($"║   Running: {health.RunningActors,-47} ║");
        Console.WriteLine($"║   Terminated: {health.TerminatedActors,-44} ║");
        Console.WriteLine($"║   In Error: {health.ErroredActors,-46} ║");
        Console.WriteLine($"║   Health: {health.GetHealthPercentage()}%{" ",-43} ║");

        Console.WriteLine("╠════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ MESSAGE DISPATCHER                                    ║");
        if (stats.DispatcherStats != null)
        {
            Console.WriteLine($"║   Processed: {stats.DispatcherStats.TotalProcessed,-44} ║");
            Console.WriteLine($"║   Failed: {stats.DispatcherStats.TotalFailed,-48} ║");
            Console.WriteLine($"║   Success Rate: {stats.DispatcherStats.SuccessRate}%{" ",-36} ║");
            Console.WriteLine($"║   Avg Latency: {stats.DispatcherStats.AverageLatency}ms{" ",-35} ║");
            Console.WriteLine($"║   P95 Latency: {stats.DispatcherStats.P95Latency}ms{" ",-35} ║");
            Console.WriteLine($"║   P99 Latency: {stats.DispatcherStats.P99Latency}ms{" ",-35} ║");
        }

        Console.WriteLine("╠════════════════════════════════════════════════════════╣");
        Console.WriteLine($"║ MAILBOX STATUS                                        ║");
        if (stats.MailboxStats != null)
        {
            Console.WriteLine($"║   Total Enqueued: {stats.MailboxStats.TotalEnqueued,-40} ║");
            Console.WriteLine($"║   Current Queue Size: {stats.MailboxStats.CurrentQueueSize,-35} ║");
            Console.WriteLine($"║   Avg Queue Length: {stats.MailboxStats.AverageQueueLength,-37} ║");
            Console.WriteLine($"║   Peak Queue Length: {stats.MailboxStats.PeakQueueLength,-36} ║");
        }

        Console.WriteLine("╚════════════════════════════════════════════════════════╝\n");
    }

    public override async Task OnStopAsync()
    {
        _monitoringTimer?.Dispose();
        Console.WriteLine($"[{Path}] Monitor stopped.");
        await Task.CompletedTask;
    }

    public override async Task ReceiveAsync(Message message)
    {
        // Monitor only reports, doesn't process messages
        await Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Metrics & Monitoring ===\n");

        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "MonitoredSystem";
            options.EnableMetricsCollection = true;
        });

        var sp = services.BuildServiceProvider();
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
        var system = await config.InitializeAsync();

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create monitor actor
            var monitorPath = new ActorPath("/user/monitor");
            await config.CreateActorAsync(monitorPath);

            // Create processor actors
            for (int i = 0; i < 5; i++)
            {
                var procPath = new ActorPath($"/user/processor-{i}");
                await config.CreateActorAsync(procPath);
            }

            // Send messages to processors
            var random = new Random();
            for (int i = 0; i < 50; i++)
            {
                var procIndex = random.Next(0, 5);
                var procPath = new ActorPath($"/user/processor-{procIndex}");
                var procRef = config.GetActor(procPath);

                if (procRef != null)
                {
                    var delay = random.Next(10, 100);
                    var msg = new ControlMessage("process",
                        new Dictionary<string, object> { { "delay", delay } });

                    await dispatcher.SendAsync(procRef, msg);
                }

                await Task.Delay(100);
            }

            // Let monitoring run for a bit
            await Task.Delay(15000);
        }
        finally
        {
            await system.ShutdownAsync();
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
