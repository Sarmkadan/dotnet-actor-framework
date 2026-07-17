# ActorSystemDiagnosticsJsonExtensions

The `ActorSystemDiagnosticsJsonExtensions` class provides a set of static extension and helper methods designed to facilitate the serialization and deserialization of various diagnostic data structures within the .NET Actor Framework. It enables the conversion of complex diagnostic objects, such as performance snapshots, memory statistics, GC statistics, actor path analyses, and actor load information, into and from JSON string representations. This functionality is essential for logging, monitoring, and transmitting diagnostic state across system boundaries or persisting it for later analysis.

## API

The following members are available on the `ActorSystemDiagnosticsJsonExtensions` class. Note that method overloads are distinguished by their specific target types or operation modes.

### `ToJson` (Overloads)
Converts a specific diagnostic object into its JSON string representation.
*   **Purpose**: Serializes the input object (e.g., `ActorSystemDiagnostics`, `PerformanceSnapshot`, `MemoryStatistics`, `GcStatistics`, `ActorPathAnalysis`, or `ActorLoadInfo`) into a formatted JSON string.
*   **Parameters**: Takes a single parameter representing the object to serialize (the specific type depends on the overload).
*   **Return Value**: Returns a `string` containing the JSON representation of the object. If the input object is null, behavior depends on the specific implementation, but typically returns a null or empty representation depending on the serializer configuration.
*   **Exceptions**: May throw serialization exceptions if the object graph contains circular references, unsupported types, or if the underlying JSON serializer encounters an error.

### `FromJson`
Deserializes a JSON string into an `ActorSystemDiagnostics` object.
*   **Purpose**: Parses a JSON string to reconstruct an `ActorSystemDiagnostics` instance.
*   **Parameters**: Takes a `string` containing the JSON data.
*   **Return Value**: Returns an `ActorSystemDiagnostics?` instance. Returns `null` if the input string is null, empty, or invalid.
*   **Exceptions**: May throw format exceptions if the JSON structure does not match the expected schema for `ActorSystemDiagnostics`.

### `TryFromJson` (Overloads)
Attempts to deserialize a JSON string into a specific diagnostic type without throwing exceptions on failure.
*   **Purpose**: Safely parses a JSON string into a target type (e.g., `ActorSystemDiagnostics`, `PerformanceSnapshot`, `MemoryStatistics`, `GcStatistics`, `ActorPathAnalysis`, or `ActorLoadInfo`).
*   **Parameters**: 
    *   `json`: The `string` containing the JSON data.
    *   `result`: An `out` parameter that receives the deserialized object if successful, or `null` if failed.
*   **Return Value**: Returns a `bool`. `true` if deserialization was successful; `false` if the JSON was invalid, null, or did not match the expected schema.
*   **Exceptions**: This method is designed not to throw exceptions for parsing failures; it returns `false` instead.

### `FromJsonToPerformanceSnapshot`
Deserializes a JSON string specifically into a `PerformanceSnapshot` object.
*   **Purpose**: Converts a JSON string into a `PerformanceSnapshot` instance.
*   **Parameters**: Takes a `string` containing the JSON data.
*   **Return Value**: Returns a `PerformanceSnapshot?`. Returns `null` if deserialization fails or input is invalid.
*   **Exceptions**: May throw exceptions if the JSON is malformed or incompatible with the `PerformanceSnapshot` structure.

### `FromJsonToMemoryStatistics`
Deserializes a JSON string specifically into a `MemoryStatistics` object.
*   **Purpose**: Converts a JSON string into a `MemoryStatistics` instance.
*   **Parameters**: Takes a `string` containing the JSON data.
*   **Return Value**: Returns a `MemoryStatistics?`. Returns `null` if deserialization fails or input is invalid.
*   **Exceptions**: May throw exceptions if the JSON is malformed or incompatible with the `MemoryStatistics` structure.

### `FromJsonToGcStatistics`
Deserializes a JSON string specifically into a `GcStatistics` object.
*   **Purpose**: Converts a JSON string into a `GcStatistics` instance.
*   **Parameters**: Takes a `string` containing the JSON data.
*   **Return Value**: Returns a `GcStatistics?`. Returns `null` if deserialization fails or input is invalid.
*   **Exceptions**: May throw exceptions if the JSON is malformed or incompatible with the `GcStatistics` structure.

### `FromJsonToActorPathAnalysis`
Deserializes a JSON string specifically into an `ActorPathAnalysis` object.
*   **Purpose**: Converts a JSON string into an `ActorPathAnalysis` instance.
*   **Parameters**: Takes a `string` containing the JSON data.
*   **Return Value**: Returns an `ActorPathAnalysis?`. Returns `null` if deserialization fails or input is invalid.
*   **Exceptions**: May throw exceptions if the JSON is malformed or incompatible with the `ActorPathAnalysis` structure.

### `FromJsonToActorLoadInfo`
Deserializes a JSON string specifically into an `ActorLoadInfo` object.
*   **Purpose**: Converts a JSON string into an `ActorLoadInfo` instance.
*   **Parameters**: Takes a `string` containing the JSON data.
*   **Return Value**: Returns an `ActorLoadInfo?`. Returns `null` if deserialization fails or input is invalid.
*   **Exceptions**: May throw exceptions if the JSON is malformed or incompatible with the `ActorLoadInfo` structure.

## Usage

### Example 1: Serializing and Deserializing System Diagnostics
This example demonstrates how to capture current system diagnostics, convert them to JSON for logging or transmission, and subsequently restore the object state.

```csharp
using DotNetActorFramework.Diagnostics;

// Assume 'diagnostics' is a populated instance of ActorSystemDiagnostics
ActorSystemDiagnostics diagnostics = ActorSystem.GetCurrentDiagnostics();

// Serialize to JSON
string jsonPayload = ActorSystemDiagnosticsJsonExtensions.ToJson(diagnostics);

// Later, deserialize back to an object
ActorSystemDiagnostics? restoredDiagnostics = ActorSystemDiagnosticsJsonExtensions.FromJson(jsonPayload);

if (restoredDiagnostics != null)
{
    Console.WriteLine($"System uptime restored: {restoredDiagnostics.Uptime}");
}
```

### Example 2: Safe Parsing of Performance Snapshots
This example illustrates the use of the `TryFromJson` pattern to safely handle potentially malformed JSON data when retrieving performance metrics from an external source, avoiding runtime exceptions.

```csharp
using DotNetActorFramework.Diagnostics;

string incomingJson = GetExternalMetricData(); // Method retrieving JSON from network/file

if (ActorSystemDiagnosticsJsonExtensions.TryFromJson(incomingJson, out PerformanceSnapshot? snapshot))
{
    // Successfully parsed
    Console.WriteLine($"CPU Usage: {snapshot.CpuUsagePercent}%");
}
else
{
    // Handling invalid data gracefully
    Console.WriteLine("Failed to parse performance snapshot data.");
}
```

## Notes

*   **Null Handling**: All `FromJson` and `FromJsonTo*` methods return nullable types (`T?`). Consumers must check for `null` before accessing properties, as invalid JSON or empty strings will result in a `null` return value rather than an exception in the `TryFromJson` variants.
*   **Thread Safety**: As this class consists entirely of static methods operating on immutable string inputs and creating new object instances, it is inherently thread-safe. Multiple threads can safely call `ToJson` or `FromJson` concurrently without external synchronization.
*   **Schema Compatibility**: Deserialization methods assume the JSON structure strictly matches the current version of the diagnostic data contracts. If the JSON originates from a different version of the framework where properties have been renamed or removed, deserialization may fail or result in objects with default values for missing fields.
*   **Performance**: Serialization and deserialization involve reflection and string manipulation. For high-frequency diagnostic polling loops, consider caching serialized results or using the `TryFromJson` methods to minimize exception overhead in failure scenarios.
