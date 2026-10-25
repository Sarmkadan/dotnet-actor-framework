// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Constants;
using DotNetActorFramework.Enums;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Configuration;

/// <summary>
/// Configuration options for the actor system.
/// </summary>
public class ActorSystemOptions
{
    /// <summary>
    /// Gets or sets the name that identifies this actor system. Must not be null, empty, or whitespace.
    /// </summary>
    public string SystemName { get; set; } = "DefaultActorSystem";

    /// <summary>
    /// Gets or sets the maximum number of messages a newly created mailbox can hold before the overflow policy applies.
    /// Defaults to 1000.
    /// </summary>
    public int DefaultMailboxCapacity { get; set; } = ActorConstants.DefaultMailboxCapacity;

    /// <summary>
    /// Gets or sets the ordering strategy used for newly created mailboxes. Defaults to <see cref="MailboxType.FIFO"/>.
    /// </summary>
    public MailboxType DefaultMailboxType { get; set; } = MailboxType.FIFO;

    /// <summary>
    /// Gets or sets the action taken when a mailbox is full. Defaults to <see cref="MailboxOverflowPolicy.DropNewest"/>.
    /// </summary>
    public MailboxOverflowPolicy DefaultMailboxOverflowPolicy { get; set; } = MailboxOverflowPolicy.DropNewest;

    /// <summary>
    /// Gets or sets the default timeout, in seconds, for actor operations. Must be greater than zero. Defaults to 30.
    /// </summary>
    public int DefaultTimeoutSeconds { get; set; } = ActorConstants.DefaultTimeoutSeconds;

    /// <summary>
    /// Gets or sets the maximum number of times a failed message is retried. Cannot be negative. Defaults to 3.
    /// </summary>
    public int MaxMessageRetries { get; set; } = ActorConstants.MaxMessageRetries;

    /// <summary>
    /// Gets or sets the maximum depth of the actor supervision hierarchy. Must be greater than zero. Defaults to 10.
    /// </summary>
    public int MaxActorDepth { get; set; } = ActorConstants.DefaultMaxActorDepth;

    /// <summary>
    /// Gets or sets the strategy applied when an actor fails. Defaults to <see cref="SupervisionStrategy.Restart"/>.
    /// </summary>
    public SupervisionStrategy DefaultSupervisionStrategy { get; set; } = SupervisionStrategy.Restart;

    /// <summary>
    /// Gets or sets a value indicating whether messages are persisted. Defaults to true.
    /// </summary>
    public bool EnableMessagePersistence { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether runtime metrics are collected. Defaults to true.
    /// </summary>
    public bool EnableMetricsCollection { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether actor state is periodically snapshotted. Defaults to true.
    /// </summary>
    public bool EnableActorStateSnapshotting { get; set; } = true;

    /// <summary>
    /// Gets or sets the interval, in seconds, between actor state snapshots. Must be greater than zero. Defaults to 300.
    /// </summary>
    public int SnapshotIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Gets or sets the storage backend used for persistence. Defaults to <see cref="PersistenceBackend.InMemory"/>.
    /// </summary>
    public PersistenceBackend DefaultPersistenceBackend { get; set; } = PersistenceBackend.InMemory;

    /// <summary>
    /// Gets or sets the connection string for the database backend, if one is used. Null by default.
    /// </summary>
    public string? DatabaseConnectionString { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether clustering is enabled. Defaults to false.
    /// </summary>
    public bool EnableClusterMode { get; set; } = false;

    /// <summary>
    /// Gets or sets the address of this node in cluster mode, in <c>host:port</c> form. Defaults to <c>127.0.0.1:8080</c>.
    /// </summary>
    public string ClusterAddress { get; set; } = "127.0.0.1:8080";

    /// <summary>
    /// Gets or sets the maximum number of nodes allowed in the cluster. Defaults to 10.
    /// </summary>
    public int MaxClusterNodes { get; set; } = 10;

    /// <summary>
    /// Gets or sets the error rate, as a fraction from 0 to 1, above which an actor is considered unhealthy.
    /// Must be less than <see cref="CriticalErrorRateThreshold"/>. Defaults to 0.25.
    /// </summary>
    public double UnhealthyErrorRateThreshold { get; set; } = ActorConstants.UnhealthyErrorRateThreshold;

    /// <summary>
    /// Gets or sets the error rate, as a fraction from 0 to 1, at which an actor is considered critical.
    /// Must be greater than <see cref="UnhealthyErrorRateThreshold"/>. Defaults to 0.75.
    /// </summary>
    public double CriticalErrorRateThreshold { get; set; } = ActorConstants.CriticalErrorRateThreshold;

    /// <summary>
    /// Gets or sets the delay, in milliseconds, before the first retry under backoff. Must be greater than zero.
    /// Defaults to 100.
    /// </summary>
    public int InitialBackoffDelayMs { get; set; } = ActorConstants.InitialBackoffDelayMs;

    /// <summary>
    /// Gets or sets the upper bound, in milliseconds, on the backoff delay. Must be greater than
    /// <see cref="InitialBackoffDelayMs"/>. Defaults to 30000.
    /// </summary>
    public int MaxBackoffDelayMs { get; set; } = ActorConstants.MaxBackoffDelayMs;

    /// <summary>
    /// Gets or sets the factor by which the backoff delay grows after each attempt. Must be greater than 1.
    /// Defaults to 1.5.
    /// </summary>
    public double BackoffMultiplier { get; set; } = ActorConstants.BackoffMultiplier;

    /// <summary>
    /// Gets or sets a value indicating whether verbose diagnostic logging is enabled. Defaults to false.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the mailbox load percentage (0-100) above which a warning is logged. Defaults to 80.
    /// </summary>
    public int HighWatermarkWarningThreshold { get; set; } = 80;

    /// <summary>
    /// Validates the configuration options. Every violated rule is collected before anything is thrown,
    /// so a single exception reports all problems at once.
    /// </summary>
    /// <exception cref="OptionsValidationException">
    /// Thrown when one or more settings are invalid. <see cref="OptionsValidationException.Failures"/>
    /// lists each violation.
    /// </exception>
    public void Validate()
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(SystemName))
            failures.Add("System name cannot be null or empty.");

        failures.AddRange(ToMailboxOptions().GetValidationFailures().Select(failure => failure.Message));

        if (DefaultTimeoutSeconds <= 0)
            failures.Add("Default timeout must be greater than zero.");

        if (MaxMessageRetries < 0)
            failures.Add("Max message retries cannot be negative.");

        if (MaxActorDepth <= 0)
            failures.Add("Max actor depth must be greater than zero.");

        if (SnapshotIntervalSeconds <= 0)
            failures.Add("Snapshot interval must be greater than zero.");

        if (UnhealthyErrorRateThreshold < 0 || UnhealthyErrorRateThreshold > 1)
            failures.Add("Unhealthy error rate threshold must be between 0 and 1.");

        if (CriticalErrorRateThreshold < 0 || CriticalErrorRateThreshold > 1)
            failures.Add("Critical error rate threshold must be between 0 and 1.");

        if (UnhealthyErrorRateThreshold >= CriticalErrorRateThreshold)
            failures.Add("Unhealthy threshold must be less than critical threshold.");

        if (InitialBackoffDelayMs <= 0)
            failures.Add("Initial backoff delay must be greater than zero.");

        if (MaxBackoffDelayMs <= InitialBackoffDelayMs)
            failures.Add("Max backoff delay must be greater than initial backoff delay.");

        if (BackoffMultiplier <= 1.0)
            failures.Add("Backoff multiplier must be greater than 1.");

        if (failures.Count > 0)
            throw new OptionsValidationException(nameof(ActorSystemOptions), failures);
    }

    /// <summary>
    /// Creates the per-mailbox settings described by these options.
    /// </summary>
    /// <returns>A new <see cref="MailboxOptions"/> populated from the default mailbox settings.</returns>
    public MailboxOptions ToMailboxOptions() => new()
    {
        Capacity = DefaultMailboxCapacity,
        Type = DefaultMailboxType,
        OverflowPolicy = DefaultMailboxOverflowPolicy,
        HighWatermarkWarningThreshold = HighWatermarkWarningThreshold
    };

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

    /// <summary>
    /// Returns a string representation of the key options, intended for diagnostics.
    /// </summary>
    /// <returns>A string containing the system name, mailbox, timeout, retry, and depth settings.</returns>
    public override string ToString()
    {
        return $"ActorSystemOptions {{ SystemName = {SystemName}, DefaultMailboxCapacity = {DefaultMailboxCapacity}, DefaultMailboxType = {DefaultMailboxType}, DefaultTimeoutSeconds = {DefaultTimeoutSeconds}, MaxMessageRetries = {MaxMessageRetries}, MaxActorDepth = {MaxActorDepth} }}";
    }
}
