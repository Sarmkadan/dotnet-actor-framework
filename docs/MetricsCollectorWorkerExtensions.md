# MetricsCollectorWorkerExtensions
The `MetricsCollectorWorkerExtensions` class provides a set of extension methods for working with metrics collection in the context of the dotnet-actor-framework. It offers functionality to clone the latest metrics snapshot, calculate health percentages, format metrics data, and serialize metrics to JSON. These extensions enable developers to easily integrate metrics collection and analysis into their actor-based systems.

## API
* `public static MetricsSnapshot CloneLatestSnapshot`: Creates a clone of the latest metrics snapshot. This method does not take any parameters and returns a `MetricsSnapshot` object. It does not throw any exceptions.
* `public static double GetHealthPercentage`: Calculates the health percentage based on the collected metrics. This method does not take any parameters and returns a `double` value representing the health percentage. It does not throw any exceptions.
* `public static string GetFormattedMetrics`: Formats the collected metrics into a human-readable string. This method does not take any parameters and returns a `string` containing the formatted metrics. It does not throw any exceptions.
* `public static string ToJson`: Serializes the collected metrics to a JSON string. This method does not take any parameters and returns a `string` containing the JSON representation of the metrics. It does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `MetricsCollectorWorkerExtensions` class:
```csharp
// Example 1: Cloning the latest metrics snapshot
MetricsSnapshot latestSnapshot = MetricsCollectorWorkerExtensions.CloneLatestSnapshot();
Console.WriteLine("Latest Metrics Snapshot: " + latestSnapshot);

// Example 2: Calculating health percentage and formatting metrics
double healthPercentage = MetricsCollectorWorkerExtensions.GetHealthPercentage();
string formattedMetrics = MetricsCollectorWorkerExtensions.GetFormattedMetrics();
Console.WriteLine("Health Percentage: " + healthPercentage);
Console.WriteLine("Formatted Metrics: " + formattedMetrics);
```

## Notes
When using the `MetricsCollectorWorkerExtensions` class, consider the following edge cases and thread-safety remarks:
* The `CloneLatestSnapshot` method creates a deep copy of the latest metrics snapshot, ensuring that modifications to the cloned snapshot do not affect the original data.
* The `GetHealthPercentage` and `GetFormattedMetrics` methods rely on the accuracy of the collected metrics data. If the data is incomplete or corrupted, the calculated health percentage and formatted metrics may not reflect the actual system state.
* The `ToJson` method serializes the metrics data to a JSON string, which can be useful for logging, monitoring, or external analysis. However, be aware that large metrics datasets may result in substantial JSON strings, potentially impacting performance.
* The `MetricsCollectorWorkerExtensions` class is designed to be thread-safe, allowing concurrent access to its methods without compromising data integrity. Nevertheless, it is essential to ensure that the underlying metrics collection infrastructure is also thread-safe to avoid data corruption or inconsistencies.
