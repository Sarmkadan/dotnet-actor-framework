// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Services;

/// <summary>
/// The central registry for managing actor registrations, lookups, and hierarchy indexing within the system.
/// It provides thread-safe mechanisms to register, retrieve, and terminate actors based on their path or ID.
/// </summary>
public class ActorRegistry
{
    private readonly Dictionary<ActorPath, ActorRef> _pathIndex = [];
    private readonly Dictionary<Guid, ActorRef> _idIndex = [];
    private readonly Dictionary<ActorPath, List<ActorPath>> _hierarchyIndex = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Registers an actor in the registry and updates the hierarchy index.
    /// </summary>
    /// <param name="actorRef">The <see cref="ActorRef"/> to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorRef"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if an actor is already registered at the same path.</exception>
    public void Register(ActorRef actorRef)
    {
        if (actorRef == null)
            throw new ArgumentNullException(nameof(actorRef));

        lock (_lockObject)
        {
            if (_pathIndex.ContainsKey(actorRef.Path))
                throw new InvalidOperationException($"Actor already registered at path: {actorRef.Path}");

            _pathIndex[actorRef.Path] = actorRef;
            _idIndex[actorRef.Id] = actorRef;

            // Update hierarchy index
            if (actorRef.Path.Parent != null)
            {
                if (!_hierarchyIndex.ContainsKey(actorRef.Path.Parent))
                {
                    _hierarchyIndex[actorRef.Path.Parent] = [];
                }
                _hierarchyIndex[actorRef.Path.Parent].Add(actorRef.Path);
            }
        }
    }

    /// <summary>
    /// Unregisters an actor from the registry.
    /// </summary>
    public void Unregister(ActorRef actorRef)
    {
        if (actorRef == null)
            throw new ArgumentNullException(nameof(actorRef));

        lock (_lockObject)
        {
            _pathIndex.Remove(actorRef.Path);
            _idIndex.Remove(actorRef.Id);

            if (actorRef.Path.Parent != null && _hierarchyIndex.TryGetValue(actorRef.Path.Parent, out var children))
            {
                children.Remove(actorRef.Path);
            }
        }
    }

    /// <summary>
    /// Gets an actor reference by its path.
    /// </summary>
    public ActorRef? GetByPath(ActorPath path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        lock (_lockObject)
        {
            return _pathIndex.TryGetValue(path, out var actorRef) ? actorRef : null;
        }
    }

    /// <summary>
    /// Gets an actor reference by its ID.
    /// </summary>
    public ActorRef? GetById(Guid id)
    {
        lock (_lockObject)
        {
            return _idIndex.TryGetValue(id, out var actorRef) ? actorRef : null;
        }
    }

    /// <summary>
    /// Gets all child actors for a given parent path.
    /// </summary>
    public IReadOnlyList<ActorRef> GetChildren(ActorPath parentPath)
    {
        if (parentPath == null)
            throw new ArgumentNullException(nameof(parentPath));

        lock (_lockObject)
        {
            if (_hierarchyIndex.TryGetValue(parentPath, out var children))
            {
                return children
                    .Select(path => _pathIndex.TryGetValue(path, out var actorRef) ? actorRef : null)
                    .Where(actorRef => actorRef != null)
                    .Cast<ActorRef>()
                    .ToList()
                    .AsReadOnly();
            }

            return [];
        }
    }

    /// <summary>
    /// Gets all descendant actors for a given parent path.
    /// </summary>
    public IReadOnlyList<ActorRef> GetDescendants(ActorPath parentPath)
    {
        if (parentPath == null)
            throw new ArgumentNullException(nameof(parentPath));

        lock (_lockObject)
        {
            var descendants = new List<ActorRef>();
            CollectDescendants(parentPath, descendants);
            return descendants.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all registered actors.
    /// </summary>
    public IReadOnlyList<ActorRef> GetAll()
    {
        lock (_lockObject)
        {
            return _pathIndex.Values.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Checks if an actor is registered.
    /// </summary>
    public bool Contains(ActorPath path)
    {
        if (path == null)
            throw new ArgumentNullException(nameof(path));

        lock (_lockObject)
        {
            return _pathIndex.ContainsKey(path);
        }
    }

    /// <summary>
    /// Gets the total number of registered actors.
    /// </summary>
    public int GetCount()
    {
        lock (_lockObject)
        {
            return _pathIndex.Count;
        }
    }

    /// <summary>
    /// Clears all registrations.
    /// </summary>
    public void Clear()
    {
        lock (_lockObject)
        {
            _pathIndex.Clear();
            _idIndex.Clear();
            _hierarchyIndex.Clear();
        }
    }

    private void CollectDescendants(ActorPath parentPath, List<ActorRef> descendants)
    {
        if (_hierarchyIndex.TryGetValue(parentPath, out var children))
        {
            foreach (var childPath in children)
            {
                if (_pathIndex.TryGetValue(childPath, out var actorRef))
                {
                    descendants.Add(actorRef);
                    CollectDescendants(childPath, descendants);
                }
            }
        }
    }
}
