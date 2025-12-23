// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Enums;

namespace DotNetActorFramework.Models;

/// <summary>
/// The root actor system that manages the lifecycle, registry, and message delivery for a hierarchy of actors.
/// It acts as the central coordinator for all actor operations within the domain.
/// </summary>
public class ActorSystem
{
    /// <summary>Gets the unique name of the actor system.</summary>
    public string Name { get; }
    /// <summary>Gets the unique identifier of the actor system instance.</summary>
    public Guid Id { get; }
    /// <summary>Gets the UTC timestamp when the system was initialized.</summary>
    public DateTime CreatedAt { get; }
    /// <summary>Gets the UTC timestamp when the system was shut down, if applicable.</summary>
    public DateTime? ShutdownAt { get; private set; }
    /// <summary>Gets a value indicating whether the actor system is currently running and accepting messages.</summary>
    public bool IsRunning { get; private set; }

    private readonly Dictionary<Guid, Actor> _actors = [];
    private readonly Dictionary<ActorPath, Guid> _pathIndex = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorSystem"/> class.
    /// </summary>
    /// <param name="name">The unique name for this actor system.</param>
    /// <exception cref="ArgumentException">Thrown if the provided name is null or whitespace.</exception>
    public ActorSystem(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("System name cannot be null or empty.", nameof(name));

        Name = name;
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        IsRunning = true;
    }

    /// <summary>
    /// Creates and registers a new actor within the system.
    /// </summary>
    /// <param name="path">The <see cref="ActorPath"/> defining the actor's location in the hierarchy.</param>
    /// <param name="supervisor">Optional <see cref="ActorRef"/> of the parent supervisor actor.</param>
    /// <returns>A task representing the asynchronous creation process, returning the <see cref="ActorRef"/> of the newly created actor.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the actor system is not running or an actor already exists at the specified path.</exception>
    public async Task<ActorRef> CreateActorAsync(ActorPath path, ActorRef? supervisor = null)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        if (!IsRunning)
            throw new InvalidOperationException("Actor system is not running.");

        lock (_lockObject)
        {
            if (_pathIndex.ContainsKey(path))
                throw new InvalidOperationException($"Actor already exists at path: {path}");
        }

        var actor = new Actor(path, supervisor);
        await actor.InitializeAsync();

        lock (_lockObject)
        {
            _actors[actor.Id] = actor;
            _pathIndex[path] = actor.Id;
        }

        return actor.Ref;
    }

    /// <summary>
    /// Gets an actor reference by its path.
    /// </summary>
    public ActorRef? GetActorRef(ActorPath path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        lock (_lockObject)
        {
            if (_pathIndex.TryGetValue(path, out var id) && _actors.TryGetValue(id, out var actor))
            {
                return actor.Ref;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all actor references for a given parent path.
    /// </summary>
    public IReadOnlyList<ActorRef> GetActorsByParent(ActorPath parentPath)
    {
        if (parentPath == null)
            throw new ArgumentNullException(nameof(parentPath));

        lock (_lockObject)
        {
            return _actors.Values
                .Where(a => a.Path.IsDescendantOf(parentPath))
                .Select(a => a.Ref)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all registered actors.
    /// </summary>
    public IReadOnlyList<ActorRef> GetAllActors()
    {
        lock (_lockObject)
        {
            return _actors.Values.Select(a => a.Ref).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Terminates an actor by its reference.
    /// </summary>
    public async Task TerminateActorAsync(ActorRef actorRef)
    {
        if (actorRef == null)
            throw new ArgumentNullException(nameof(actorRef));

        Actor? actor = null;
        lock (_lockObject)
        {
            if (_actors.TryGetValue(actorRef.Id, out var a))
            {
                actor = a;
                _actors.Remove(actorRef.Id);
                _pathIndex.Remove(a.Path);
            }
        }

        if (actor != null)
        {
            await actor.TerminateAsync();
        }
    }

    /// <summary>
    /// Gets the total number of actors in the system.
    /// </summary>
    public int GetActorCount()
    {
        lock (_lockObject)
        {
            return _actors.Count;
        }
    }

    /// <summary>
    /// Gets actors that are in an error state.
    /// </summary>
    public IReadOnlyList<ActorRef> GetErrorActors()
    {
        lock (_lockObject)
        {
            return _actors.Values
                .Where(a => a.State == ActorState.Error)
                .Select(a => a.Ref)
                .ToList()
                .AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the metrics summary for a specific actor.
    /// </summary>
    /// <param name="path">The actor's hierarchical path.</param>
    /// <returns>
    /// The actor's <see cref="ActorMetricsSummary"/>, or <c>null</c> if no actor is registered at <paramref name="path"/>.
    /// </returns>
    public ActorMetricsSummary? GetActorMetricsSummary(ActorPath path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        lock (_lockObject)
        {
            if (_pathIndex.TryGetValue(path, out var id) && _actors.TryGetValue(id, out var actor))
                return actor.Metrics.GetSummary();
        }

        return null;
    }

    /// <summary>
    /// Gets a health summary of all actors.
    /// </summary>
    public SystemHealthSummary GetHealthSummary()
    {
        lock (_lockObject)
        {
            var summary = new SystemHealthSummary
            {
                SystemId = Id,
                SystemName = Name,
                CreatedAt = CreatedAt,
                TotalActors = _actors.Count,
                HealthyActors = _actors.Values.Count(a => !a.Metrics.IsUnhealthy()),
                UnhealthyActors = _actors.Values.Count(a => a.Metrics.IsUnhealthy()),
                ErrorActors = _actors.Values.Count(a => a.State == ActorState.Error),
                TotalMessages = _actors.Values.Sum(a => a.Metrics.MessageCount),
                TotalErrors = _actors.Values.Sum(a => a.Metrics.ErrorCount)
            };

            return summary;
        }
    }

    /// <summary>
    /// Gracefully shuts down the actor system.
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (!IsRunning)
            return;

        IsRunning = false;

        List<Actor> actorsToShutdown;
        lock (_lockObject)
        {
            actorsToShutdown = _actors.Values.ToList();
        }

        foreach (var actor in actorsToShutdown)
        {
            try
            {
                await actor.TerminateAsync();
            }
            catch (Exception)
            {
                // Continue shutdown even if an actor fails
            }
        }

        ShutdownAt = DateTime.UtcNow;

        lock (_lockObject)
        {
            _actors.Clear();
            _pathIndex.Clear();
        }
    }

    public override string ToString() => $"ActorSystem({Name}, {Id:N})";
}

/// <summary>
/// Summary of the actor system health.
/// </summary>
public class SystemHealthSummary
{
    public Guid SystemId { get; set; }
    public string SystemName { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalActors { get; set; }
    public int HealthyActors { get; set; }
    public int UnhealthyActors { get; set; }
    public int ErrorActors { get; set; }
    public long TotalMessages { get; set; }
    public long TotalErrors { get; set; }

    public double GetHealthPercentage()
    {
        if (TotalActors == 0) return 100;
        return (double)HealthyActors / TotalActors * 100;
    }

    public double GetErrorRate()
    {
        if (TotalMessages == 0) return 0;
        return (double)TotalErrors / TotalMessages * 100;
    }

    public bool IsHealthy => UnhealthyActors == 0 && ErrorActors == 0;
}
