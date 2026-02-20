// ... (rest of the file remains unchanged)

## SupervisionServiceExtensions

The `SupervisionServiceExtensions` provides a set of extension methods for monitoring actor health, failure thresholds, and performance statistics. It enables tracking of actor failures, retrieving detailed statistics, and identifying problematic actors for supervision or logging purposes.

### Usage Example
```csharp
public class HealthMonitorActor : Actor
{
    public override async Task ReceiveAsync(Message message)
    {
        var actorId = Guid.Parse("a1b2c3d4-e5f6-7890-g1h2-i3j4k5l6m7n8");
        
        if (SupervisionServiceExtensions.HasExceededFailureThreshold(actorId))
        {
            var stats = SupervisionServiceExtensions.GetActorStatistics(actorId);
            var worstActor = SupervisionServiceExtensions.GetWorstPerformingActor();
            
            Log.Warning($"Actor {actorId} has {stats.FailureCount} failures (last at {stats.LastFailureTime}) " +
                        $"and a {stats.TimeSinceLastFailure.TotalSeconds} second gap since last failure. " +
                        $"Worst performer: {worstActor}");
        }
    }
}
```

// ... (rest of the file remains unchanged)

## ActorRefExtensions

The `ActorRefExtensions` provides a set of extension methods for interacting with actor references. 
These extensions facilitate sending messages to actors, checking actor instance identity, 
and retrieving age and detailed string representations of actors.

### Usage Example
```csharp
public class MyActor : Actor
{
    public async Task HandleMessage(Message message)
    {
        var otherActorRef = new ActorRef(Guid.NewGuid(), "OtherActor");
        
        var response = await ActorRefExtensions.AskWithTimeoutAsync(otherActorRef, new MyMessage(), TimeSpan.FromSeconds(5));
        
        if (response != null)
        {
            Log.Information($"Received response from {otherActorRef}: {response}");
        }
        
        var isSameInstance = ActorRefExtensions.IsSameInstance(otherActorRef, otherActorRef);
        Log.Information($"Is same instance: {isSameInstance}");
        
        var age = ActorRefExtensions.GetAge(otherActorRef);
        Log.Information($"Actor age: {age}");
        
        var detailedString = ActorRefExtensions.ToDetailedString(otherActorRef);
        Log.Information($"Actor detailed string: {detailedString}");
    }
}
```

## ActorPathTestsExtensions

`ActorPathTestsExtensions` contains helper methods used in unit‑tests to build and assert `ActorPath` hierarchies.  
It simplifies creation of deep hierarchies, sibling paths, and provides fluent assertions for segment counts, parent/child relationships, and sibling checks.

### Usage Example
```csharp
using DotNetActorFramework.Models;
using DotNetActorFramework.Tests;

public class ActorPathTestsDemo
{
    public void Execute()
    {
        // Build a deep hierarchy path for testing
        ActorPath deep = ActorPathTestsExtensions.CreateDeepHierarchy();

        // Verify the path contains the expected segments
        ActorPathTestsExtensions.ShouldHaveSegments(
            deep,
            new[] { "root", "level1", "level2", "leaf" });

        // Create a sibling path of the same parent
        ActorPath sibling = ActorPathTestsExtensions.CreateSibling(deep);

        // Assert that the two paths are siblings
        ActorPathTestsExtensions.ShouldBeSiblings(deep, sibling);

        // Retrieve the parent of the deep path and assert the child relationship
        ActorPath parent = ActorPathTestsExtensions.ShouldHaveParent(deep);
        ActorPathTestsExtensions.ShouldBeDirectChildOf(deep, parent);

        // Compute the relative path from sibling to deep
        string[]? relative = ActorPathTestsExtensions.GetRelativePath(sibling, deep);
        // `relative` contains the segment differences, or null if the paths share the same hierarchy
    }
}
```

// ... (rest of the file remains unchanged)
