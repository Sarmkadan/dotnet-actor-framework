// 08-basic-usage.cs
// A minimal example showing the basic setup and first actor creation.

using System;
using System.Threading.Tasks;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

public class BasicUsage
{
    public static async Task RunAsync()
    {
        // 1. Initialize the system
        var builder = new ActorSystemBuilder("BasicSystem");
        var system = builder.Build();

        // 2. Define an actor path
        var actorPath = new ActorPath("/user/basic-actor");

        // 3. Create the actor (using the system's factory)
        // In a real scenario, you'd define a class inheriting from ActorBase
        Console.WriteLine($"Initializing actor system: {system.Name}");
        Console.WriteLine($"Actor path defined: {actorPath}");
        
        // This is a simplified representation of creating an actor
        // Actor creation would involve registering the actor type
        Console.WriteLine("Basic usage setup complete.");
        
        await Task.CompletedTask;
    }
}
