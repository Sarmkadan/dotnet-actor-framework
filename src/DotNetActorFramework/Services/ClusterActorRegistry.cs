// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;
using Microsoft.Extensions.Logging;
using DotNetActorFramework.Exceptions;

namespace DotNetActorFramework.Services;

/// <summary>
/// Manages actor references across different nodes in a cluster.
/// This registry keeps track of which actors are hosted on which cluster nodes.
/// </summary>
public class ClusterActorRegistry
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<ActorRef>> _nodeActors = new();
    private readonly ILogger<ClusterActorRegistry>? _logger;

    public ClusterActorRegistry(ILogger<ClusterActorRegistry>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registers an actor as being hosted on a specific cluster node.
    /// </summary>
    /// <param name="nodeAddress">The address of the cluster node (acting as NodeId).</param>
    /// <param name="actorRef">The actor reference to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when nodeAddress or actorRef is null.</exception>
    /// <exception cref="ClusterException">Thrown when cluster operations fail.</exception>
    public void RegisterActor(string nodeAddress, ActorRef actorRef)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(nodeAddress))
                throw new ArgumentNullException(nameof(nodeAddress));
            if (actorRef == null)
                throw new ArgumentNullException(nameof(actorRef));

            var actorsOnNode = _nodeActors.GetOrAdd(nodeAddress, _ => new ConcurrentBag<ActorRef>());
            actorsOnNode.Add(actorRef);
            _logger.LogInformation("RegisterActor called with {NodeAddress} and {ActorId}", nodeAddress, actorRef.Id);
            _logger?.LogDebug("Registered actor {ActorPath} on node {NodeAddress}", actorRef.Path, nodeAddress);
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to register actor {ActorId} on node {NodeAddress}", actorRef.Id, nodeAddress);
            throw new ClusterException(nodeAddress, $"Failed to register actor on node {nodeAddress}", ex);
        }
    }

    /// <summary>
    /// Unregisters a specific actor from a cluster node.
    /// Note: ConcurrentBag does not support efficient removal of specific items.
    /// For this reason, this method will rebuild the ConcurrentBag for the given node.
    /// </summary>
    /// <param name="nodeAddress">The address of the cluster node.</param>
    /// <param name="actorRef">The actor reference to unregister.</param>
    /// <returns>True if the actor was found and removed, false otherwise.</returns>
    public bool UnregisterActor(string nodeAddress, ActorRef actorRef)
    {
        _logger?.LogInformation("UnregisterActor called with node {NodeAddress} and actor {ActorId}", nodeAddress, actorRef?.Id);
        try
        {
            if (string.IsNullOrWhiteSpace(nodeAddress))
                throw new ArgumentNullException(nameof(nodeAddress));
            if (actorRef == null)
                throw new ArgumentNullException(nameof(actorRef));

            if (_nodeActors.TryGetValue(nodeAddress, out var actorsOnNode))
            {
                var newBag = new ConcurrentBag<ActorRef>(actorsOnNode.Where(ar => ar.Id != actorRef.Id));
                if (_nodeActors.TryUpdate(nodeAddress, newBag, actorsOnNode))
                {
                    _logger?.LogDebug("Unregistered actor {ActorPath} from node {NodeAddress}", actorRef.Path, nodeAddress);
                    return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to unregister actor {ActorId} from node {NodeAddress}", actorRef?.Id, nodeAddress);
            throw;
        }
    }

    /// <summary>
    /// Removes all actors associated with a disconnected or unreachable cluster node.
    /// This is the core pruning mechanism to prevent memory leaks.
    /// </summary>
    /// <param name="nodeAddress">The address of the cluster node to remove.</param>
    /// <returns>True if the node was found and removed, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when nodeAddress is null.</exception>
    /// <exception cref="ClusterException">Thrown when cluster operations fail.</exception>
    public bool RemoveNode(string nodeAddress)
    {
        _logger?.LogInformation("RemoveNode called with {NodeAddress}", nodeAddress);
        try
        {
            if (string.IsNullOrWhiteSpace(nodeAddress))
                throw new ArgumentNullException(nameof(nodeAddress));

            if (_nodeActors.TryRemove(nodeAddress, out var removedActors))
            {
                _logger?.LogInformation("Removed node {NodeAddress} and all {ActorCount} associated actors from the cluster registry.", nodeAddress, removedActors.Count);
                return true;
            }
            _logger?.LogWarning("Attempted to remove non-existent node {NodeAddress} from cluster registry.", nodeAddress);
            return false;
        }
        catch (ArgumentNullException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to remove node {NodeAddress}", nodeAddress);
            throw new ClusterException(nodeAddress, $"Failed to remove node {nodeAddress}", ex);
        }
    }

    /// <summary>
    /// Retrieves all actors registered on a specific cluster node.
    /// </summary>
    /// <param name="nodeAddress">The address of the cluster node.</param>
    /// <returns>An enumerable of ActorRef for the specified node, or an empty enumerable if the node is not found.</returns>
    public IEnumerable<ActorRef> GetActorsByNode(string nodeAddress)
    {
        _logger?.LogInformation("GetActorsByNode called with {NodeAddress}", nodeAddress);
        if (string.IsNullOrWhiteSpace(nodeAddress))
            return Enumerable.Empty<ActorRef>();

        if (_nodeActors.TryGetValue(nodeAddress, out var actorsOnNode))
        {
            return actorsOnNode;
        }
        return Enumerable.Empty<ActorRef>();
    }

    /// <summary>
    /// Gets all registered node addresses.
    /// </summary>
    public IEnumerable<string> GetAllNodeAddresses()
    {
        _logger?.LogInformation("GetAllNodeAddresses called");
        return _nodeActors.Keys;
    }

    /// <summary>
    /// Gets the total count of registered nodes.
    /// </summary>
    public int GetNodeCount()
    {
        _logger?.LogInformation("GetNodeCount called");
        return _nodeActors.Count;
    }
}
