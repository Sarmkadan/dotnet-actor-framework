using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetActorFramework.Models
{
    /// <summary>
    /// Extension methods for <see cref="ActorMetrics"/>.
    /// </summary>
    public static class ActorMetricsExtensions
    {
        /// <summary>
        /// Calculates the average number of messages processed per second since the actor was created.
        /// </summary>
        /// <param name="metrics">The <see cref="ActorMetrics"/> instance.</param>
        /// <returns>The messages per second as a <see cref="double"/>. Returns 0 if uptime is zero.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public static double GetMessagesPerSecond(this ActorMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            var uptimeSeconds = metrics.GetUptime().TotalSeconds;
            return uptimeSeconds > 0 ? metrics.MessageCount / uptimeSeconds : 0d;
        }

        /// <summary>
        /// Returns a culture‑invariant, single‑line representation of the most important metric values.
        /// </summary>
        /// <param name="metrics">The <see cref="ActorMetrics"/> instance.</param>
        /// <returns>A formatted string.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public static string ToInvariantString(this ActorMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            return string.Format(
                CultureInfo.InvariantCulture,
                "ActorId={0} Path={1} Msgs={2} Errors={3} SuccessRate={4:P2} AvgProcTimeMs={5:F2} Uptime={6}",
                metrics.ActorId,
                metrics.ActorPath,
                metrics.MessageCount,
                metrics.ErrorCount,
                metrics.GetSuccessRate(),
                metrics.AverageProcessingTimeMs,
                metrics.GetUptime());
        }

        /// <summary>
        /// Projects the metric values into a read‑only list of key/value pairs suitable for logging or diagnostics.
        /// </summary>
        /// <param name="metrics">The <see cref="ActorMetrics"/> instance.</param>
        /// <returns>An <see cref="IReadOnlyList{KeyValuePair{string, object}}"/> containing the metric data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public static IReadOnlyList<KeyValuePair<string, object>> ToKeyValuePairs(this ActorMetrics metrics)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            var list = new List<KeyValuePair<string, object>>
            {
                new(nameof(metrics.ActorId), metrics.ActorId),
                new(nameof(metrics.ActorPath), metrics.ActorPath?.ToString()),
                new(nameof(metrics.MessageCount), metrics.MessageCount),
                new(nameof(metrics.ErrorCount), metrics.ErrorCount),
                new(nameof(metrics.ProcessedCount), metrics.ProcessedCount),
                new(nameof(metrics.AverageProcessingTimeMs), metrics.AverageProcessingTimeMs),
                new(nameof(metrics.CreatedAt), metrics.CreatedAt),
                new(nameof(metrics.LastMessageTime), metrics.LastMessageTime),
                new(nameof(metrics.MailboxDepth), metrics.MailboxDepth),
                new("ErrorRate", metrics.GetErrorRate()),
                new("SuccessRate", metrics.GetSuccessRate()),
                new("Uptime", metrics.GetUptime())
            };
            return list.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the actor is considered healthy based on a configurable error‑rate threshold.
        /// </summary>
        /// <param name="metrics">The <see cref="ActorMetrics"/> instance.</param>
        /// <param name="errorRateThreshold">The maximum acceptable error rate (default 0.25).</param>
        /// <returns><c>true</c> if the error rate is less than or equal to the threshold; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public static bool IsHealthy(this ActorMetrics metrics, double errorRateThreshold = 0.25)
        {
            ArgumentNullException.ThrowIfNull(metrics);
            return metrics.GetErrorRate() <= errorRateThreshold;
        }
    }
}
