// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Models;

// Example 7: Parent-Child Hierarchy
// Demonstrates supervised actor hierarchies with delegated work

public class WorkerActor : Actor
{
    private int _workItems = 0;

    public WorkerActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "work")
        {
            _workItems++;
            var duration = (int)cm.Parameters["duration"];

            Console.WriteLine($"[{Path}] Processing work item #{_workItems} (duration: {duration}ms)");
            await Task.Delay(duration).ConfigureAwait(false);

            Console.WriteLine($"[{Path}] Work item #{_workItems} completed");
            Metrics.RecordSuccess();
        }
        await Task.CompletedTask;
    }

    public override async Task OnStopAsync()
    {
        Console.WriteLine($"[{Path}] Worker stopped after processing {_workItems} items");
        await Task.CompletedTask;
    }
}

public class SupervisorActor : Actor
{
    private readonly MessageDispatcher _dispatcher;
    private List<ActorRef> _workers = new();

    public SupervisorActor(ActorPath path, MessageDispatcher dispatcher) : base(path)
    {
        _dispatcher = dispatcher;
    }

    public override async Task OnInitializeAsync()
    {
        // Create worker pool
        for (int i = 0; i < 3; i++)
        {
            var workerPath = new ActorPath($"{Path}/worker-{i}");
            var workerRef = await ActorSystem.CreateActorAsync(workerPath, Ref).ConfigureAwait(false);
            _workers.Add(workerRef);
        }

        Console.WriteLine($"[{Path}] Supervisor created {_workers.Count} workers");
    }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "distribute")
        {
            var workCount = (int)cm.Parameters["count"];
            var random = new Random();

            Console.WriteLine($"[{Path}] Distributing {workCount} work items...\n");

            for (int i = 0; i < workCount; i++)
            {
                var workerIndex = i % _workers.Count;
                var duration = random.Next(50, 200);

                var workMsg = new ControlMessage("work", new Dictionary<string, object>
                {
                    { "duration", duration },
                    { "item-id", i }
                });

                await _dispatcher.SendAsync(_workers[workerIndex], workMsg).ConfigureAwait(false);
            }

            Console.WriteLine($"\n[{Path}] All work items distributed");
        }
        await Task.CompletedTask;
    }

    public override async Task OnStopAsync()
    {
        Console.WriteLine($"[{Path}] Supervisor shutting down with {_workers.Count} workers");
        await Task.CompletedTask;
    }
}

public class RootActor : Actor
{
    private readonly MessageDispatcher _dispatcher;
    private ActorRef? _supervisor;

    public RootActor(ActorPath path, MessageDispatcher dispatcher) : base(path)
    {
        _dispatcher = dispatcher;
    }

    public override async Task OnInitializeAsync()
    {
        // Create the supervisor
        var supervisorPath = new ActorPath($"{Path}/supervisor");
        _supervisor = await ActorSystem.CreateActorAsync(supervisorPath, Ref).ConfigureAwait(false);

        Console.WriteLine($"[{Path}] Root actor created supervisor\n");
    }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "start-work")
        {
            if (_supervisor != null)
            {
                var distributeMsg = new ControlMessage("distribute",
                    new Dictionary<string, object> { { "count", 20 } });

                await _dispatcher.SendAsync(_supervisor, distributeMsg).ConfigureAwait(false);
            }
        }
        await Task.CompletedTask;
    }

    public override async Task OnStopAsync()
    {
        Console.WriteLine($"[{Path}] Root actor shutting down");
        await Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Parent-Child Hierarchy ===\n");

        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "HierarchySystem";
            options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
        });

        var sp = services.BuildServiceProvider();
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
        var system = await config.InitializeAsync().ConfigureAwait(false);

        Console.WriteLine("Actor system initialized.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create root actor
            var rootPath = new ActorPath("/user/root");
            var rootRef = await config.CreateActorAsync(rootPath).ConfigureAwait(false);

            Console.WriteLine("Root actor created with supervisor and workers.\n");

            // Trigger work distribution
            var startMsg = new ControlMessage("start-work");
            await dispatcher.SendAsync(rootRef, startMsg).ConfigureAwait(false);

            // Wait for work to complete
            await Task.Delay(5000).ConfigureAwait(false);

            // Display hierarchy
            Console.WriteLine("\nActor Hierarchy:");
            PrintActorHierarchy("/user", config);

            // Show stats
            var stats = await config.GetStatisticsAsync().ConfigureAwait(false);
            Console.WriteLine($"\nStatistics:");
            Console.WriteLine($"  Actors Created: {stats.ActorRegistryStats?.TotalCreated}");
            Console.WriteLine($"  Messages Processed: {stats.DispatcherStats?.TotalProcessed}");
            Console.WriteLine($"  Success Rate: {stats.DispatcherStats?.SuccessRate}%");
        }
        finally
        {
            await system.ShutdownAsync().ConfigureAwait(false);
            Console.WriteLine("\nShutdown complete.");
        }
    }

    static void PrintActorHierarchy(string pathStr, ActorSystemConfiguration config, int indent = 0)
    {
        var path = new ActorPath(pathStr);
        var actors = config.GetActorsByPath(path);

        foreach (var actor in actors)
        {
            var indentStr = new string(' ', indent * 2);
            Console.WriteLine($"{indentStr}├─ {actor.Path}");

            // Print children recursively
            PrintActorHierarchy(actor.Path.ToString(), config, indent + 1);
        }
    }
}
