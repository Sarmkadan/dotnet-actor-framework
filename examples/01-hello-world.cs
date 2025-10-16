// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Example 1: Hello World
// Demonstrates basic actor creation and message sending

public class HelloActor : Actor
{
    public HelloActor(ActorPath path) : base(path) { }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "greet")
        {
            var name = cm.Parameters?.GetValueOrDefault("name", "World");
            Console.WriteLine($"[{Path}] Hello, {name}!");
        }
        await Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Hello World Example ===\n");

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "HelloWorldSystem";
        });

        var serviceProvider = services.BuildServiceProvider();

        // Initialize the actor system
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(serviceProvider);
        var actorSystem = await config.InitializeAsync();

        Console.WriteLine($"Actor system '{config.Options.SystemName}' initialized.\n");

        try
        {
            // Get the message dispatcher
            var dispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();

            // Create an actor
            var actorPath = new ActorPath("/user/hello");
            var helloRef = await config.CreateActorAsync(actorPath);

            Console.WriteLine($"Created actor: {actorPath}\n");

            // Send greeting messages
            var greetings = new[] { "Alice", "Bob", "Charlie" };

            foreach (var name in greetings)
            {
                var message = new ControlMessage("greet", new Dictionary<string, object>
                {
                    { "name", name }
                });

                await dispatcher.SendAsync(helloRef, message);
            }

            // Give actors time to process
            await Task.Delay(500);

            // Display system health
            var health = config.GetHealthSummary();
            Console.WriteLine($"\nSystem Health:");
            Console.WriteLine($"  Total Actors: {health.TotalActors}");
            Console.WriteLine($"  Running: {health.RunningActors}");
            Console.WriteLine($"  Health: {health.GetHealthPercentage()}%");
        }
        finally
        {
            // Graceful shutdown
            Console.WriteLine("\nShutting down actor system...");
            await actorSystem.ShutdownAsync();
            Console.WriteLine("Done!");
        }
    }
}
