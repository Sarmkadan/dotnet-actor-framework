# ClusterActorRegistry

The `ClusterActorRegistry` maintains a mapping between cluster node addresses and the actors hosted on those nodes. It provides registration, lookup, and removal operations that enable the actor framework to track which actors belong to which nodes in a distributed cluster.

## API

### ClusterActorRegistry()
Creates an empty registry with no nodes or actors registered.

### RegisterActor(ActorRef actor)
Registers the specified actor in the registry, associating it with the node address contained in the actor's reference.  
- **Parameters**  
  - `actor`: The `ActorRef` to register.  
- **Return value**  
  - `void`.  
- **Exceptions**  
  - `ArgumentNullException` if `actor` is `null`.  
  - `InvalidOperationException` if an actor with the same identifier is already registered.

### UnregisterActor(ActorRef actor)
Attempts to remove the specified actor from the registry.  
- **Parameters**  
  - `actor`: The `ActorRef` to unregister.  
- **Return value**  
  - `true` if the actor was found and removed; `false` if the actor was not present.  
- **Exceptions**  
  - `ArgumentNullException` if `actor` is `null`.

### RemoveNode(string nodeAddress)
Removes all actors associated with the given node address from the registry.  
- **Parameters**  
  - `nodeAddress`: The address of the node to remove.  
- **Return value**  
  - `true` if at least one actor was removed; `false` if the node had no actors registered.  
- **Exceptions**  
  - `ArgumentNullException` if `nodeAddress` is `null`.  
  - `ArgumentException` if `nodeAddress` is empty or consists only of white‑space.

### GetActorsByNode(string nodeAddress)
Enumerates all actors currently registered on the specified node.  
- **Parameters**  
  - `nodeAddress`: The node address to query.  
- **Return value**  
  - An `IEnumerable<ActorRef>` containing the actors for the node; an empty enumeration if none are registered.  
- **Exceptions**  
  - `ArgumentNullException` if `nodeAddress` is `null`.  
  - `ArgumentException` if `nodeAddress` is empty or consists only of white‑space.

### GetAllNodeAddresses()
Returns the set of distinct node addresses that have at least one actor registered.  
- **Parameters**  
  - None.  
- **Return value**  
  - An `IEnumerable<string>` containing each node address.  
- **Exceptions**  
  - None.

### GetNodeCount()
Returns the number of distinct nodes currently represented in the registry.  
- **Parameters**  
  - None.  
- **Return value**  
  - An `int` indicating the count of unique node addresses.  
- **Exceptions**  
  - None.

## Usage

```csharp
var registry = new ClusterActorRegistry();

// Register an actor obtained from the actor system
ActorRef myActor = system.ActorOf<MyActor>("myActor");
registry.RegisterActor(myActor);

// Retrieve all actors running on a specific node
string node = myActor.Path.Address.ToString();
foreach (var actor in registry.GetActorsByNode(node))
{
    Console.WriteLine($"Actor {actor.Path.Name} is on node {node}");
}
```

```csharp
// Remove a node and check how many nodes remain
string nodeToRemove = "akka.tcp://cluster@10.0.0.5:4053";
bool removed = registry.RemoveNode(nodeToRemove);
Console.WriteLine($"Node removal succeeded: {removed}");

int remainingNodes = registry.GetNodeCount();
Console.WriteLine($"Remaining nodes: {remainingNodes}");
```

## Notes

- Registering the same `ActorRef` twice will throw an `InvalidOperationException`; duplicate registrations are not allowed.  
- `UnregisterActor` returns `false` when the actor is not present; it does not throw in that case.  
- Removing a node that has no actors registered returns `false` and leaves the registry unchanged.  
- The registry does **not** perform internal synchronization; concurrent access from multiple threads requires external locking or other synchronization mechanisms to avoid race conditions.  
- All methods that accept string arguments validate for `null` and empty/white‑space values and throw the appropriate exceptions as described.  
- The enumerables returned by `GetActorsByNode`, `GetAllNodeAddresses`, and similar methods reflect the state of the registry at the moment of enumeration; subsequent modifications to the registry are not reflected in an ongoing enumeration.
