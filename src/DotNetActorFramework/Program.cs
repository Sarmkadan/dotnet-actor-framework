// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

namespace DotNetActorFramework;

/// <summary>
/// Main entry point demonstrating usage of the actor framework.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddLogging(config =>
        {
            config.SetMinimumLevel(LogLevel.Information);
        });

        // Add the actor framework
        services.AddActorFramework(options =>
        {
            options.SystemName = "DotNetActorSystem";
            options.DefaultMailboxCapacity = 1000;
            options.EnableMessagePersistence = true;
            options.EnableMetricsCollection = true;
            options.EnableDetailedLogging = false;
        });

        // Create service provider
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== DotNet Actor Framework Demo ===");

        try
        {
            // Initialize actor system
            var configuration = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(serviceProvider);
            var actorSystem = await configuration.InitializeAsync();

            logger.LogInformation($"Actor system started: {actorSystem.Name}");

            // Create sample actors
            var rootPath = new ActorPath("/user");
            var counterActorPath = rootPath.GetChild("counter");
            var loggerActorPath = rootPath.GetChild("logger");

            var counterRef = await configuration.CreateActorAsync(counterActorPath);
            var loggerRef = await configuration.CreateActorAsync(loggerActorPath);

            logger.LogInformation("Actors created successfully");

            // Demonstrate message sending
            var messageDispatcher = serviceProvider.GetRequiredService<MessageDispatcher>();

            // Send some messages
            for (int i = 0; i < 5; i++)
            {
                var message = new ControlMessage($"process-{i}");
                await messageDispatcher.SendAsync(counterRef, loggerRef, message);
                logger.LogInformation($"Message {i} sent from counter to logger");
            }

            // Get health summary
            var health = configuration.GetHealthSummary();
            logger.LogInformation(
                $"System Health - Total Actors: {health.TotalActors}, " +
                $"Healthy: {health.HealthyActors}, " +
                $"Errors: {health.ErrorActors}");

            // Get statistics
            var stats = configuration.GetStatistics();
            logger.LogInformation(
                $"Dispatcher Stats - Delivered: {stats.DispatcherStats?.TotalDelivered}, " +
                $"Failed: {stats.DispatcherStats?.TotalFailed}, " +
                $"Success Rate: {stats.DispatcherStats?.SuccessRate:F2}%");

            logger.LogInformation("Mailbox Stats - " +
                $"Total: {stats.MailboxStats?.TotalMailboxes}, " +
                $"Messages: {stats.MailboxStats?.TotalMessages}, " +
                $"Load: {stats.MailboxStats?.AverageLoadFactor:F2}");

            // Graceful shutdown
            await configuration.ShutdownAsync();
            logger.LogInformation("Actor system shutdown gracefully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred");
            Environment.Exit(1);
        }

        logger.LogInformation("Demo completed");
    }
}
