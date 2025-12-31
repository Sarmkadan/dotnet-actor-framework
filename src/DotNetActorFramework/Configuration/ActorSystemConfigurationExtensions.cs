// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;
using System.Text;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// Extension methods for <see cref="ActorSystemConfiguration"/> providing convenient
/// utility operations for actor system management and diagnostics.
/// </summary>
public static class ActorSystemConfigurationExtensions
{
    /// <summary>
    /// Creates an actor at the specified hierarchical path.
    /// </summary>
    /// <param name="configuration">The actor system configuration.</param>
    /// <param name="path">The hierarchical path for the new actor (e.g., "/user/workers/processor").</param>
    /// <param name="supervisor">Optional supervisor actor reference.</param>
    /// <returns>A task that completes with the actor reference when the actor is created.</returns>
    /// <exception cref="ArgumentNullException">Thrown if path is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if actor system is not initialized.</exception>
    public static async Task<ActorRef> CreateActorAsync(
        this ActorSystemConfiguration configuration,
        string path,
        ActorRef? supervisor = null)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty.", nameof(path));

        var actorPath = ActorPath.Parse(path);
        return await configuration.CreateActorAsync(actorPath, supervisor);
    }

    /// <summary>
    /// Sends a message to an actor identified by its path.
    /// </summary>
    /// <param name="configuration">The actor system configuration.</param>
    /// <param name="recipientPath">The path of the recipient actor.</param>
    /// <param name="message">The message to send.</param>
    /// <returns>A task that completes when the message has been dispatched.</returns>
    /// <exception cref="ArgumentNullException">Thrown if recipientPath or message is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if actor system is not initialized.</exception>
    public static async Task SendMessageAsync(
        this ActorSystemConfiguration configuration,
        string recipientPath,
        Message message)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(recipientPath))
            throw new ArgumentException("Recipient path is null or empty.", nameof(recipientPath));

        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var actorPath = ActorPath.Parse(recipientPath);
        var recipient = configuration.GetActorSystem().GetActorRef(actorPath);

        if (recipient == null)
            throw new InvalidOperationException($"Actor not found at path: {recipientPath}");

        await configuration.SendMessageAsync(configuration.GetActorSystem().GetAllActors().First(), recipient, message);
    }

    /// <summary>
    /// Gets a formatted health report string for the actor system.
    /// </summary>
    /// <param name="configuration">The actor system configuration.</param>
    /// <param name="includeDetailedStats">Whether to include detailed statistics in the report.</param>
    /// <returns>A formatted health report string.</returns>
    /// <exception cref="InvalidOperationException">Thrown if actor system is not initialized.</exception>
    public static string GetHealthReport(
        this ActorSystemConfiguration configuration,
        bool includeDetailedStats = false)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var healthSummary = configuration.GetHealthSummary();
        var stats = configuration.GetStatistics();

        var report = new StringBuilder();
        report.AppendLine("=== Actor System Health Report ===");
        report.AppendLine($"System: {stats.Options?.SystemName ?? "Unknown"}");
        report.AppendLine($"Status: {(healthSummary.GetHealthPercentage() >= 90 ? "HEALTHY" : "DEGRADED")}");
        report.AppendLine($"Collected At: {stats.CollectedAt:yyyy-MM-dd HH:mm:ss}");
        report.AppendLine();

        report.AppendLine("=== Health Summary ===");
        report.AppendLine($"Total Actors: {healthSummary.TotalActors}");
        report.AppendLine($"Healthy Actors: {healthSummary.HealthyActors}");
        report.AppendLine($"Unhealthy Actors: {healthSummary.UnhealthyActors}");
        report.AppendLine($"Health Percentage: {healthSummary.GetHealthPercentage():F2}%");
        report.AppendLine();

        if (includeDetailedStats)
        {
            report.AppendLine("=== Detailed Statistics ===");
            report.AppendLine($"Mailbox Messages: {stats.MailboxStats?.TotalMessages ?? 0}");
            report.AppendLine($"Total Mailboxes: {stats.MailboxStats?.TotalMailboxes ?? 0}");
            report.AppendLine($"Active Dispatchers: {stats.DispatcherStats?.TotalProcessed ?? 0}");
            report.AppendLine($"Failed Deliveries: {stats.DispatcherStats?.TotalFailed ?? 0}");
            report.AppendLine($"Supervision Events: {stats.SupervisionStats?.TotalRestarts ?? 0}");
            report.AppendLine($"Persisted Messages: {stats.PersistenceStats?.TotalMessages ?? 0}");
            report.AppendLine($"Delivered Messages: {stats.PersistenceStats?.DeliveredMessages ?? 0}");
            report.AppendLine($"Connections: {(stats.ConnectionStats?.IsConnected ?? false ? "Connected" : "Disconnected")}");
        }

        return report.ToString();
    }

    /// <summary>
    /// Checks if the actor system is currently healthy based on health metrics.
    /// </summary>
    /// <param name="configuration">The actor system configuration.</param>
    /// <param name="healthThreshold">Minimum health percentage required (default: 90%).</param>
    /// <returns>True if the system is healthy; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if actor system is not initialized.</exception>
    public static bool IsHealthy(
        this ActorSystemConfiguration configuration,
        double healthThreshold = 90.0)
    {
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        var healthSummary = configuration.GetHealthSummary();
        return healthSummary.GetHealthPercentage() >= healthThreshold;
    }
}