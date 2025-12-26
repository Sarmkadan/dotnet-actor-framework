// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;

namespace DotNetActorFramework.BackgroundWorkers;

/// <summary>
/// Extension methods for <see cref="MetricsCollectorWorker"/> providing additional functionality
/// for metrics collection, analysis, and reporting.
/// </summary>
public static class MetricsCollectorWorkerExtensions
{
    /// <summary>
    /// Creates a shallow copy of the latest metrics snapshot.
    /// </summary>
    /// <param name="worker">The metrics collector worker instance</param>
    /// <returns>A new MetricsSnapshot instance with the same values</returns>
    public static MetricsSnapshot CloneLatestSnapshot(this MetricsCollectorWorker worker)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        var snapshot = worker.GetLatestSnapshot();
        return new MetricsSnapshot
        {
            Timestamp = snapshot.Timestamp,
            TotalActors = snapshot.TotalActors,
            HealthyActors = snapshot.HealthyActors,
            ErrorActors = snapshot.ErrorActors,
            TotalMessages = snapshot.TotalMessages,
            TotalErrors = snapshot.TotalErrors,
            AverageLatencyMs = snapshot.AverageLatencyMs,
            ErrorRate = snapshot.ErrorRate
        };
    }

    /// <summary>
    /// Gets the system health status as a percentage (0-100).
    /// </summary>
    /// <param name="worker">The metrics collector worker instance</param>
    /// <returns>Health percentage (100 = fully healthy, 0 = completely unhealthy)</returns>
    public static double GetHealthPercentage(this MetricsCollectorWorker worker)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        var snapshot = worker.GetLatestSnapshot();

        // Calculate health based on multiple factors
        double actorHealth = snapshot.TotalActors > 0
            ? (double)snapshot.HealthyActors / snapshot.TotalActors * 50.0
            : 0;

        double errorHealth = Math.Max(0, 50.0 - (snapshot.ErrorRate * 2.0));

        return Math.Min(100, actorHealth + errorHealth);
    }

    /// <summary>
    /// Gets a formatted string representation of the current metrics.
    /// </summary>
    /// <param name="worker">The metrics collector worker instance</param>
    /// <returns>Formatted metrics string</returns>
    public static string GetFormattedMetrics(this MetricsCollectorWorker worker)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        var snapshot = worker.GetLatestSnapshot();

        return $@"Metrics Snapshot - {snapshot.Timestamp:yyyy-MM-dd HH:mm:ss}
========================================
Total Actors:     {snapshot.TotalActors} ({snapshot.HealthyActors} healthy, {snapshot.ErrorActors} errors)
Message Throughput: {snapshot.TotalMessages:N0} messages, {snapshot.TotalErrors:N0} errors
Performance:      Avg Latency: {snapshot.AverageLatencyMs:F2}ms | Error Rate: {snapshot.ErrorRate:P2}
System Health:     {worker.GetHealthPercentage():F1}% {(snapshot.IsHealthy ? "✓ HEALTHY" : "✗ DEGRADED")}
";
    }

    /// <summary>
    /// Gets a JSON representation of the latest metrics snapshot.
    /// </summary>
    /// <param name="worker">The metrics collector worker instance</param>
    /// <returns>JSON string containing all metrics</returns>
    public static string ToJson(this MetricsCollectorWorker worker)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        var snapshot = worker.GetLatestSnapshot();

        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(new
        {
            timestamp = snapshot.Timestamp,
            totalActors = snapshot.TotalActors,
            healthyActors = snapshot.HealthyActors,
            errorActors = snapshot.ErrorActors,
            totalMessages = snapshot.TotalMessages,
            totalErrors = snapshot.TotalErrors,
            averageLatencyMs = snapshot.AverageLatencyMs,
            errorRate = snapshot.ErrorRate,
            isHealthy = snapshot.IsHealthy,
            healthPercentage = worker.GetHealthPercentage()
        }, options);
    }
}