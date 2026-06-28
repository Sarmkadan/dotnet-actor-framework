// 10-integration-example.cs
// Shows how to wire the actor framework into ASP.NET Core DI.

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using System;

public class IntegrationExample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // 1. Configure the actor framework via DI
        services.AddActorFramework(options =>
        {
            options.DefaultMailboxCapacity = 1000;
            options.EnableMessagePersistence = true;
            options.SnapshotIntervalSeconds = 30;
        });

        // 2. You can also use pre-defined configurations
        // services.AddActorFrameworkReliable();
        
        Console.WriteLine("Actor Framework services registered in DI container.");
    }
}
