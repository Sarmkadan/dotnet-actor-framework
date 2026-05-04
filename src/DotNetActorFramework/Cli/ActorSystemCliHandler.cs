// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Api;
using DotNetActorFramework.Serialization;

namespace DotNetActorFramework.Cli;

/// <summary>
/// CLI handler for interacting with the actor system.
/// Provides command-line interface for managing actors and querying metrics.
/// </summary>
public class ActorSystemCliHandler
{
    private readonly ActorManagementApi _managementApi;
    private readonly SystemMetricsApi _metricsApi;
    private readonly MessageFormatterFactory _messageFormatterFactory;
    private readonly HealthCheckFormatterFactory _healthFormatterFactory;

    public ActorSystemCliHandler(ActorSystem actorSystem, ActorManagementApi managementApi, SystemMetricsApi metricsApi)
    {
        _managementApi = managementApi ?? throw new ArgumentNullException(nameof(managementApi));
        _metricsApi = metricsApi ?? throw new ArgumentNullException(nameof(metricsApi));
        _messageFormatterFactory = new MessageFormatterFactory();
        _healthFormatterFactory = new HealthCheckFormatterFactory();
    }

    /// <summary>
    /// Handles CLI commands.
    /// </summary>
    public async Task<string> HandleCommandAsync(string command, string[] args)
    {
        if (string.IsNullOrWhiteSpace(command))
            return GetHelpText();

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var action = parts.FirstOrDefault()?.ToLowerInvariant();

        return action switch
        {
            "help" => GetHelpText(),
            "list" => HandleList(args),
            "get" => HandleGet(args),
            "health" => HandleHealth(args),
            "metrics" => HandleMetrics(args),
            "terminate" => await HandleTerminate(args),
            "status" => HandleStatus(),
            "top-messages" => HandleTopMessages(args),
            "error-actors" => HandleErrorActors(args),
            _ => $"Unknown command: {action}. Type 'help' for available commands."
        };
    }

    private string HandleList(string[] args)
    {
        var limit = args.Length > 0 && int.TryParse(args[0], out var l) ? l : 100;
        var result = _managementApi.ListActors(limit);
        return $"Total Actors: {result.Total}\n" +
               string.Join("\n", result.Actors.Select(a => $"  - {a.Path} ({(a.IsAlive ? "alive" : "dead")})"));
    }

    private string HandleGet(string[] args)
    {
        if (args.Length == 0)
            return "Usage: get <actor-path>";

        var actorPath = args[0];
        var actor = _managementApi.GetActor(actorPath);

        if (actor == null)
            return $"Actor not found: {actorPath}";

        return $"Actor: {actor.Path}\nID: {actor.Id}\nAlive: {actor.IsAlive}\nCreated: {actor.CreatedAt:O}";
    }

    private string HandleHealth(string[] args)
    {
        var health = _metricsApi.GetSystemHealth();
        var format = args.Length > 0 ? args[0] : "text";
        var formatter = _healthFormatterFactory.GetFormatter(format);

        if (formatter == null)
            return "Invalid format. Use: json, text, or csv";

        var summary = new SystemHealthSummary
        {
            SystemId = health.SystemId,
            SystemName = health.SystemName,
            TotalActors = health.TotalActors,
            HealthyActors = health.HealthyActors,
            UnhealthyActors = health.UnhealthyActors,
            ErrorActors = health.ErrorActors,
            TotalMessages = health.TotalMessages,
            TotalErrors = health.TotalErrors,
            CreatedAt = DateTime.UtcNow
        };

        return formatter.Format(summary);
    }

    private string HandleMetrics(string[] args)
    {
        if (args.Length == 0)
            return GetMetricsHelp();

        var subcommand = args[0].ToLowerInvariant();
        return subcommand switch
        {
            "summary" => GetMetricsSummary(),
            "types" => GetMessageTypeMetrics(),
            "actors" => GetActorMetrics(),
            _ => GetMetricsHelp()
        };
    }

    private string GetMetricsSummary()
    {
        var health = _metricsApi.GetSystemHealth();
        return $"Messages: {health.TotalMessages}\n" +
               $"Errors: {health.TotalErrors} ({health.ErrorRate:F2}%)\n" +
               $"Avg Latency: {health.AverageLatencyMs:F2}ms";
    }

    private string GetMessageTypeMetrics()
    {
        var topTypes = _metricsApi.GetTopMessageTypes(5);
        return "Top Message Types:\n" +
               string.Join("\n", topTypes.Select(m => $"  {m.MessageType}: {m.ProcessedCount} msgs ({m.ErrorRate:F2}% errors)"));
    }

    private string GetActorMetrics()
    {
        var slowest = _metricsApi.GetSlowesttActors(5);
        return "Slowest Actors:\n" +
               string.Join("\n", slowest.Select(a => $"  {a.ActorPath}: {a.AverageLatencyMs:F2}ms avg"));
    }

    private async Task<string> HandleTerminate(string[] args)
    {
        if (args.Length == 0)
            return "Usage: terminate <actor-path>";

        var result = await _managementApi.TerminateActorAsync(args[0]);
        return result.Message;
    }

    private string HandleStatus()
    {
        var health = _metricsApi.GetSystemHealth();
        return $"System: {health.SystemName}\n" +
               $"Actors: {health.HealthyActors}/{health.TotalActors} healthy\n" +
               $"Status: {(health.IsHealthy ? "HEALTHY" : "UNHEALTHY")}";
    }

    private string HandleTopMessages(string[] args)
    {
        var limit = args.Length > 0 && int.TryParse(args[0], out var l) ? l : 10;
        var metrics = _metricsApi.GetTopMessageTypes(limit);
        return "Top Message Types:\n" +
               string.Join("\n", metrics.Select((m, i) => $"{i + 1}. {m.MessageType}: {m.ProcessedCount}"));
    }

    private string HandleErrorActors(string[] args)
    {
        var result = _managementApi.GetErrorActors();
        if (result.Actors.Count == 0)
            return "No actors in error state.";

        return "Error Actors:\n" + string.Join("\n", result.Actors.Select(a => $"  - {a.Path}"));
    }

    private static string GetMetricsHelp()
    {
        return "metrics <subcommand>\n" +
               "  summary   - Show metrics summary\n" +
               "  types     - Show top message types\n" +
               "  actors    - Show slowest actors";
    }

    private static string GetHelpText()
    {
        return @"Actor System CLI
Commands:
  help              - Show this help
  list [limit]      - List all actors
  get <path>        - Get actor info
  health [format]   - System health (json|text|csv)
  metrics <cmd>     - Show metrics
  terminate <path>  - Terminate an actor
  status            - Quick system status
  top-messages      - Top message types
  error-actors      - Show actors in error state";
    }
}
