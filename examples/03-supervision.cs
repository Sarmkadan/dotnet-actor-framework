// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Models;

/// <summary>
/// Demonstrates fault tolerance with supervision strategies.
/// </summary>
public class UnreliableActor : Actor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnreliableActor"/> class.
    /// </summary>
    /// <param name="path">The actor path.</param>
    public UnreliableActor(ActorPath path) : base(path) { }

    /// <summary>
    /// Handles incoming messages.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "risky-operation")
        {
            /// <summary>
            /// The attempt count.
            /// </summary>
            int _attemptCount = 0;

            _attemptCount++;

            if (_attemptCount < 3)
            {
                Console.WriteLine($"[{Path}] Attempt {_attemptCount}: About to fail...");
                throw new InvalidOperationException($"Simulated failure on attempt {_attemptCount}");
            }

            Console.WriteLine($"[{Path}] Attempt {_attemptCount}: Success!");
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Called when the actor is stopped.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnStopAsync()
    {
        Console.WriteLine($"[{Path}] Actor stopped. Total attempts: {_attemptCount}");
        await Task.CompletedTask;
    }
}

/// <summary>
/// Demonstrates supervision and error recovery.
/// </summary>
public class SupervisorActor : Actor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SupervisorActor"/> class.
    /// </summary>
    /// <param name="path">The actor path.</param>
    /// <param name="dispatcher">The message dispatcher.</param>
    public SupervisorActor(ActorPath path, MessageDispatcher dispatcher) : base(path)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Called when the actor is initialized.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public override async Task OnInitializeAsync()
    {
        // Create worker actors
        for (int i = 0; i < 3; i++)
        {
            var workerPath = new ActorPath($"{Path}/worker-{i}");
            var workerRef = await ActorSystem.CreateActorAsync(workerPath, Ref);
            _workers.Add(workerRef);
        }

        Console.WriteLine($"[{Path}] Created {_workers.Count} worker actors.");
    }

    /// <summary>
    /// Handles incoming messages.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "distribute-work")
        {
            Console.WriteLine($"[{Path}] Distributing work to {_workers.Count} workers...");

            foreach (var worker in _workers)
            {
                var workMsg = new ControlMessage("risky-operation");
                await _dispatcher.SendAsync(worker, workMsg);
            }
        }
        await Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Supervision Example ===\n");

        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "SupervisionSystem";
            options.DefaultSupervisionStrategy = SupervisionStrategy.Restart;
            options.BackoffInitialDelayMs = 100;
            options.BackoffMaxDelayMs = 5000;
        });

        var sp = services.BuildServiceProvider();
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
        var system = await config.InitializeAsync();

        Console.WriteLine("Actor system initialized with Restart supervision strategy.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create supervisor
            var supervisorPath = new ActorPath("/user/supervisor");
            var supervisorRef = await config.CreateActorAsync(supervisorPath);

            Console.WriteLine("Supervisor created with worker actors.\n");

            // Distribute work
            var workMsg = new ControlMessage("distribute-work");
            await dispatcher.SendAsync(supervisorRef, workMsg);

            // Wait for recovery process
            await Task.Delay(3000);

            // Check health
            var health = config.GetHealthSummary();
            Console.WriteLine($"\nSystem Status After Supervision:");
            Console.WriteLine($"  Running Actors: {health.RunningActors}");
            Console.WriteLine($"  Failed Actors: {health.ErroredActors}");
            Console.WriteLine($"  Health: {health.GetHealthPercentage()}%");
            Console.WriteLine($"  Error Rate: {health.GetErrorRate()}%");
        }
        finally
        {
            await system.ShutdownAsync();
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
