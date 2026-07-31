namespace DotNetActorFramework.Options;

/// <summary>
/// Configuration options for the <see cref="MetricsCollectorWorker"/>.
/// These values can be bound from an <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> source (e.g., appsettings.json).
/// </summary>
public class MetricsCollectorOptions
{
    /// <summary>
    /// How often the <see cref="MetricsCollectorWorker"/> should run.
    /// Default: 30 seconds.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// The error‑rate percentage threshold used to evaluate health.
    /// Default: 5.0 (percent).
    /// </summary>
    public double ErrorRateThreshold { get; set; } = 5.0;
}
