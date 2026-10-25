// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Diagnostics;

/// <summary>
/// Diagnostics and profiling tools for the actor system.
/// Provides insights into system behavior and performance bottlenecks.
/// </summary>
public class ActorSystemDiagnostics
{
    private readonly ActorSystem _actorSystem;
    private readonly List<PerformanceSnapshot> _snapshots = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorSystemDiagnostics"/> class for the specified actor system.
    /// </summary>
    /// <param name="actorSystem">The actor system to diagnose.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorSystem"/> is <c>null</c>.</exception>
    public ActorSystemDiagnostics(ActorSystem actorSystem)
    {
        _actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));
    }

    /// <summary>
    /// Creates a performance snapshot of the current system state.
    /// </summary>
    public PerformanceSnapshot TakeSnapshot()
    {
        var health = _actorSystem.GetHealthSummary();
        var process = Process.GetCurrentProcess();

        var snapshot = new PerformanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TotalActors = health.TotalActors,
            HealthyActors = health.HealthyActors,
            ErrorActors = health.ErrorActors,
            TotalMessages = health.TotalMessages,
            TotalErrors = health.TotalErrors,
            MemoryUsageMb = process.WorkingSet64 / (1024 * 1024),
            CpuUsagePercent = GetCpuUsage()
        };

        lock (_lockObject)
        {
            _snapshots.Add(snapshot);

            // Keep only last 1000 snapshots
            if (_snapshots.Count > 1000)
                _snapshots.RemoveAt(0);
        }

        return snapshot;
    }

    /// <summary>
    /// Gets the latest performance snapshot.
    /// </summary>
    public PerformanceSnapshot? GetLatestSnapshot()
    {
        lock (_lockObject)
        {
            return _snapshots.LastOrDefault();
        }
    }

    /// <summary>
    /// Gets performance snapshots within a time window.
    /// </summary>
    public IReadOnlyList<PerformanceSnapshot> GetSnapshotsSince(TimeSpan timeWindow)
    {
        var cutoff = DateTime.UtcNow - timeWindow;
        lock (_lockObject)
        {
            return _snapshots.Where(s => s.Timestamp >= cutoff).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Gets memory usage statistics.
    /// </summary>
    public MemoryStatistics GetMemoryStatistics()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64 / (1024 * 1024);
        var privateMemory = process.PrivateMemorySize64 / (1024 * 1024);

        return new MemoryStatistics
        {
            WorkingSetMb = workingSet,
            PrivateMemoryMb = privateMemory,
            ManagedHeapMb = GC.GetTotalMemory(false) / (1024 * 1024),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Gets GC (Garbage Collection) statistics.
    /// </summary>
    public GcStatistics GetGcStatistics()
    {
        return new GcStatistics
        {
            Gen0Collections = GC.CollectionCount(0),
            Gen1Collections = GC.CollectionCount(1),
            Gen2Collections = GC.CollectionCount(2),
            TotalMemoryBytes = GC.GetTotalMemory(false),
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Analyzes actor paths to find potential issues.
    /// </summary>
    public ActorPathAnalysis AnalyzeActorHierarchy()
    {
        var allActors = _actorSystem.GetAllActors();
        var analysis = new ActorPathAnalysis();

        foreach (var actorRef in allActors)
        {
            var depth = actorRef.Path.ToString().Count(c => c == '/');
            if (depth > analysis.MaxDepth)
                analysis.MaxDepth = depth;

            var pathStr = actorRef.Path.ToString();
            var lastSlash = pathStr.LastIndexOf('/');
            var parentPath = lastSlash > 0 ? pathStr[..lastSlash] : "/";

            if (!analysis.ChildCountByParent.ContainsKey(parentPath))
                analysis.ChildCountByParent[parentPath] = 0;
            analysis.ChildCountByParent[parentPath]++;
        }

        analysis.TotalActors = allActors.Count;
        analysis.AverageChildrenPerParent = analysis.ChildCountByParent.Values.Count > 0
            ? analysis.ChildCountByParent.Values.Average()
            : 0;

        return analysis;
    }

    /// <summary>
    /// Finds actors with the most messages.
    /// </summary>
    public List<ActorLoadInfo> FindHeaviestActors(int count = 10)
    {
        var allActors = _actorSystem.GetAllActors();
        return allActors
            .Select(a => new ActorLoadInfo
            {
                Path = a.Path.ToString(),
                MessageCount = 0,
                ErrorCount = 0
            })
            .OrderByDescending(x => x.MessageCount)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Clears all collected snapshots.
    /// </summary>
    public void ClearSnapshots()
    {
        lock (_lockObject)
        {
            _snapshots.Clear();
        }
    }

    /// <summary>
    /// Exports the current diagnostics snapshot (including memory, GC, hierarchy and heaviest actors)
    /// as a JSON string. The JSON uses camelCase property names and is indented for readability.
    /// </summary>
    public string ExportSnapshotJson()
    {
        // Gather the various pieces of diagnostic data.
        var snapshot = TakeSnapshot();
        var memory = GetMemoryStatistics();
        var gc = GetGcStatistics();
        var hierarchy = AnalyzeActorHierarchy();
        var heaviest = FindHeaviestActors();

        var exportDto = new DiagnosticsExportDto
        {
            Snapshot = snapshot,
            Memory = memory,
            Gc = gc,
            Hierarchy = hierarchy,
            HeaviestActors = heaviest
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        return JsonSerializer.Serialize(exportDto, options);
    }

    private static double GetCpuUsage()
    {
        // PerformanceCounter is Windows-only; return a neutral sentinel value
        return 0;
    }
}

/// <summary>
/// DTO used for JSON export of diagnostics data.
/// </summary>
public class DiagnosticsExportDto
{
    /// <summary>
    /// Gets or sets the performance snapshot taken at export time.
    /// </summary>
    public PerformanceSnapshot Snapshot { get; set; } = null!;

    /// <summary>
    /// Gets or sets the memory statistics of the running process.
    /// </summary>
    public MemoryStatistics Memory { get; set; } = null!;

    /// <summary>
    /// Gets or sets the garbage collection statistics.
    /// </summary>
    public GcStatistics Gc { get; set; } = null!;

    /// <summary>
    /// Gets or sets the analysis of the actor hierarchy.
    /// </summary>
    public ActorPathAnalysis Hierarchy { get; set; } = null!;

    /// <summary>
    /// Gets or sets the actors with the highest load.
    /// </summary>
    public List<ActorLoadInfo> HeaviestActors { get; set; } = null!;
}

/// <summary>
/// Performance snapshot of the system at a point in time.
/// </summary>
public class PerformanceSnapshot
{
    /// <summary>
    /// Gets or sets the UTC time at which the snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the total number of actors in the system.
    /// </summary>
    public int TotalActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors that are healthy.
    /// </summary>
    public int HealthyActors { get; set; }

    /// <summary>
    /// Gets or sets the number of actors in an error state.
    /// </summary>
    public int ErrorActors { get; set; }

    /// <summary>
    /// Gets or sets the total number of messages processed by the system.
    /// </summary>
    public long TotalMessages { get; set; }

    /// <summary>
    /// Gets or sets the total number of errors recorded by the system.
    /// </summary>
    public long TotalErrors { get; set; }

    /// <summary>
    /// Gets or sets the process working set in megabytes.
    /// </summary>
    public long MemoryUsageMb { get; set; }

    /// <summary>
    /// Gets or sets the CPU usage percentage. Not collected on this platform, so it is currently always <c>0</c>.
    /// </summary>
    public double CpuUsagePercent { get; set; }
}

/// <summary>
/// Memory statistics of the running process.
/// </summary>
public class MemoryStatistics
{
    /// <summary>
    /// Gets or sets the process working set in megabytes.
    /// </summary>
    public long WorkingSetMb { get; set; }

    /// <summary>
    /// Gets or sets the private memory size of the process in megabytes.
    /// </summary>
    public long PrivateMemoryMb { get; set; }

    /// <summary>
    /// Gets or sets the managed heap size in megabytes.
    /// </summary>
    public long ManagedHeapMb { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the statistics were collected.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Garbage collection statistics.
/// </summary>
public class GcStatistics
{
    /// <summary>
    /// Gets or sets the number of generation 0 garbage collections.
    /// </summary>
    public int Gen0Collections { get; set; }

    /// <summary>
    /// Gets or sets the number of generation 1 garbage collections.
    /// </summary>
    public int Gen1Collections { get; set; }

    /// <summary>
    /// Gets or sets the number of generation 2 garbage collections.
    /// </summary>
    public int Gen2Collections { get; set; }

    /// <summary>
    /// Gets or sets the total bytes of memory currently allocated by the garbage collector.
    /// </summary>
    public long TotalMemoryBytes { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the statistics were collected.
    /// </summary>
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Analysis of the actor hierarchy.
/// </summary>
public class ActorPathAnalysis
{
    /// <summary>
    /// Gets or sets the total number of actors analyzed.
    /// </summary>
    public int TotalActors { get; set; }

    /// <summary>
    /// Gets or sets the maximum depth of any actor path in the hierarchy.
    /// </summary>
    public int MaxDepth { get; set; }

    /// <summary>
    /// Gets or sets the average number of child actors per parent path.
    /// </summary>
    public double AverageChildrenPerParent { get; set; }

    /// <summary>
    /// Gets or sets the number of child actors keyed by parent path.
    /// </summary>
    public Dictionary<string, int> ChildCountByParent { get; set; } = [];
}

/// <summary>
/// Load information for an actor.
/// </summary>
public class ActorLoadInfo
{
    /// <summary>
    /// Gets or sets the string representation of the actor's path.
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// Gets or sets the number of messages processed by the actor.
    /// </summary>
    public long MessageCount { get; set; }

    /// <summary>
    /// Gets or sets the number of errors raised by the actor.
    /// </summary>
    public long ErrorCount { get; set; }
}
