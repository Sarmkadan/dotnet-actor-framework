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
