// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Constants;
using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// Configuration options for the actor system.
/// </summary>
public class ActorSystemOptions
{
    public string SystemName { get; set; } = "DefaultActorSystem";

    public int DefaultMailboxCapacity { get; set; } = ActorConstants.DefaultMailboxCapacity;

    public MailboxType DefaultMailboxType { get; set; } = MailboxType.FIFO;

public MailboxOverflowPolicy DefaultMailboxOverflowPolicy { get; set; } = MailboxOverflowPolicy.DropNewest;

    public int DefaultTimeoutSeconds { get; set; } = ActorConstants.DefaultTimeoutSeconds;

    public int MaxMessageRetries { get; set; } = ActorConstants.MaxMessageRetries;

    public int MaxActorDepth { get; set; } = ActorConstants.DefaultMaxActorDepth;

    public SupervisionStrategy DefaultSupervisionStrategy { get; set; } = SupervisionStrategy.Restart;

    public bool EnableMessagePersistence { get; set; } = true;

    public bool EnableMetricsCollection { get; set; } = true;

    public bool EnableActorStateSnapshotting { get; set; } = true;

    public int SnapshotIntervalSeconds { get; set; } = 300;

    public PersistenceBackend DefaultPersistenceBackend { get; set; } = PersistenceBackend.InMemory;

    public string? DatabaseConnectionString { get; set; }

    public bool EnableClusterMode { get; set; } = false;

    public string ClusterAddress { get; set; } = "127.0.0.1:8080";

    public int MaxClusterNodes { get; set; } = 10;

    public double UnhealthyErrorRateThreshold { get; set; } = ActorConstants.UnhealthyErrorRateThreshold;

    public double CriticalErrorRateThreshold { get; set; } = ActorConstants.CriticalErrorRateThreshold;

    public int InitialBackoffDelayMs { get; set; } = ActorConstants.InitialBackoffDelayMs;

    public int MaxBackoffDelayMs { get; set; } = ActorConstants.MaxBackoffDelayMs;

    public double BackoffMultiplier { get; set; } = ActorConstants.BackoffMultiplier;

    public bool EnableDetailedLogging { get; set; } = false;

public int HighWatermarkWarningThreshold { get; set; } = 80;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SystemName))
            throw new InvalidOperationException("System name cannot be null or empty.");

        if (DefaultMailboxCapacity <= 0)
            throw new InvalidOperationException("Default mailbox capacity must be greater than zero.");

        if (DefaultTimeoutSeconds <= 0)
            throw new InvalidOperationException("Default timeout must be greater than zero.");

        if (MaxMessageRetries < 0)
            throw new InvalidOperationException("Max message retries cannot be negative.");

        if (MaxActorDepth <= 0)
            throw new InvalidOperationException("Max actor depth must be greater than zero.");

        if (SnapshotIntervalSeconds <= 0)
            throw new InvalidOperationException("Snapshot interval must be greater than zero.");

        if (UnhealthyErrorRateThreshold < 0 || UnhealthyErrorRateThreshold > 1)
            throw new InvalidOperationException("Unhealthy error rate threshold must be between 0 and 1.");

        if (CriticalErrorRateThreshold < 0 || CriticalErrorRateThreshold > 1)
            throw new InvalidOperationException("Critical error rate threshold must be between 0 and 1.");

        if (UnhealthyErrorRateThreshold >= CriticalErrorRateThreshold)
            throw new InvalidOperationException("Unhealthy threshold must be less than critical threshold.");

        if (InitialBackoffDelayMs <= 0)
            throw new InvalidOperationException("Initial backoff delay must be greater than zero.");

        if (MaxBackoffDelayMs <= InitialBackoffDelayMs)
            throw new InvalidOperationException("Max backoff delay must be greater than initial backoff delay.");

        if (BackoffMultiplier <= 1.0)
            throw new InvalidOperationException("Backoff multiplier must be greater than 1.");
    }

    // Overflow policy is always valid, no validation needed

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static ActorSystemOptions CreateDefault() => new();

    /// <summary>
    /// Creates a high-performance configuration optimized for throughput.
    /// </summary>
    public static ActorSystemOptions CreateHighPerformance() => new()
    {
        DefaultMailboxCapacity = 5000,
        EnableMessagePersistence = false,
        EnableMetricsCollection = false,
        SnapshotIntervalSeconds = 600
    };

    /// <summary>
    /// Creates a reliable configuration optimized for durability.
    /// </summary>
    public static ActorSystemOptions CreateReliable() => new()
    {
        DefaultMailboxCapacity = 500,
        EnableMessagePersistence = true,
        EnableActorStateSnapshotting = true,
        SnapshotIntervalSeconds = 60,
        MaxMessageRetries = 5,
        DefaultSupervisionStrategy = SupervisionStrategy.Backoff
    };

    /// <summary>
    /// Creates a cluster-optimized configuration.
    /// </summary>
    public static ActorSystemOptions CreateCluster(string clusterAddress = "127.0.0.1:8080") => new()
    {
        EnableClusterMode = true,
        ClusterAddress = clusterAddress,
        EnableMessagePersistence = true,
        EnableMetricsCollection = true,
        DefaultSupervisionStrategy = SupervisionStrategy.Escalate
    };
}
