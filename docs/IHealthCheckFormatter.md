# IHealthCheckFormatter

`IHealthCheckFormatter` defines the contract for formatting health-check results into human-readable or machine-processable string representations. It provides multiple overloads for rendering individual health reports, collections of reports, and aggregate status summaries, along with a factory mechanism that allows registration and retrieval of named formatter implementations.

## API

### `string Format(HealthReport report)`

Formats a single `HealthReport` instance into its string representation according to the formatter’s rules.

- **Parameters:**
  - `report` — The health report to format. Must not be null.
- **Returns:** A non-null string containing the formatted output.
- **Throws:** `ArgumentNullException` if `report` is null. May throw `FormatException` if the report contains data that cannot be represented in the target format.

### `string Format(IEnumerable<HealthReport> reports)`

Formats a sequence of `HealthReport` instances into a single combined string representation.

- **Parameters:**
  - `reports` — An enumerable collection of health reports. Must not be null and may be empty.
- **Returns:** A non-null string containing the formatted output for all reports. An empty collection typically produces an empty or semantically neutral representation.
- **Throws:** `ArgumentNullException` if `reports` is null.

### `string Format(HealthStatus status, string message)`

Formats a health status and an accompanying message into a string representation, without requiring a full `HealthReport` object.

- **Parameters:**
  - `status` — The health status value to represent.
  - `message` — A descriptive message associated with the status. May be null or empty.
- **Returns:** A non-null string containing the formatted output.
- **Throws:** No exceptions are specified for valid inputs; implementations may throw `ArgumentException` for invalid status values.

### `HealthCheckFormatterFactory`

A static property exposing the singleton factory instance used to manage formatter registrations. The factory holds named mappings from format identifiers to concrete `IHealthCheckFormatter` instances.

- **Type:** `HealthCheckFormatterFactory`
- **Access:** Read-only static property. Always returns the same factory instance.

### `void Register(string name, IHealthCheckFormatter formatter)`

Registers a formatter instance under a unique name within the global factory. Subsequent calls to `GetFormatter` with the same name will return this instance.

- **Parameters:**
  - `name` — A non-null, non-empty string identifying the format (e.g., `"json"`, `"plaintext"`).
  - `formatter` — The formatter instance to register. Must not be null.
- **Throws:** `ArgumentNullException` if `name` or `formatter` is null. `ArgumentException` if `name` is empty or consists only of whitespace. `InvalidOperationException` if a formatter with the same name is already registered.

### `IHealthCheckFormatter? GetFormatter(string name)`

Retrieves a previously registered formatter by its name.

- **Parameters:**
  - `name` — The name under which the formatter was registered. Must not be null.
- **Returns:** The registered `IHealthCheckFormatter` instance, or `null` if no formatter is registered under the given name.
- **Throws:** `ArgumentNullException` if `name` is null.

### `string Format(HealthCheckResult result)`

Formats a single `HealthCheckResult` into its string representation. This overload operates at the granularity of an individual check outcome rather than a full report.

- **Parameters:**
  - `result` — The health-check result to format. Must not be null.
- **Returns:** A non-null string containing the formatted output.
- **Throws:** `ArgumentNullException` if `result` is null.

## Usage

### Example 1: Registering a custom formatter and formatting a single report

```csharp
// Define a simple plain-text formatter
public class PlainTextFormatter : IHealthCheckFormatter
{
    public string Format(HealthReport report) =>
        $"[{report.Status}] {report.Name}: {report.Description}";

    public string Format(IEnumerable<HealthReport> reports) =>
        string.Join(Environment.NewLine, reports.Select(Format));

    public string Format(HealthStatus status, string message) =>
        $"[{status}] {message}";

    public string Format(HealthCheckResult result) =>
        $"[{result.Status}] {result.Name}: {result.Description}";
}

// Register the formatter globally
IHealthCheckFormatter.Register("plaintext", new PlainTextFormatter());

// Later, retrieve and use it
var formatter = IHealthCheckFormatter.GetFormatter("plaintext");
if (formatter is not null)
{
    var report = new HealthReport("Database", HealthStatus.Healthy, "Connection OK");
    Console.WriteLine(formatter.Format(report));
}
```

### Example 2: Formatting multiple reports with a pre-registered JSON formatter

```csharp
// Assume a JSON formatter is already registered under "json"
var jsonFormatter = IHealthCheckFormatter.GetFormatter("json");

if (jsonFormatter is not null)
{
    var reports = new List<HealthReport>
    {
        new("Database", HealthStatus.Healthy, "All replicas online"),
        new("Cache", HealthStatus.Degraded, "High latency detected"),
        new("Queue", HealthStatus.Unhealthy, "Connection refused")
    };

    string jsonOutput = jsonFormatter.Format(reports);
    File.WriteAllText("/var/log/healthcheck.json", jsonOutput);
}
```

## Notes

- **Null handling:** All `Format` overloads and `Register`/`GetFormatter` throw `ArgumentNullException` for required reference parameters. Callers must guard against null inputs before invoking these members.
- **Empty collections:** When `Format(IEnumerable<HealthReport>)` receives an empty sequence, the output is implementation-defined — it may return an empty string, an empty JSON array, or a placeholder indicating no reports were provided. Consult the specific formatter’s documentation.
- **Thread safety:** The static `HealthCheckFormatterFactory` and its `Register`/`GetFormatter` methods are safe for concurrent use. Registrations are typically performed at application startup; runtime lookups via `GetFormatter` are lock-free and safe to call from multiple threads simultaneously. Individual `IHealthCheckFormatter` implementations must document their own thread safety; the interface imposes no constraints.
- **Registration conflicts:** Attempting to register a formatter under an already-occupied name throws `InvalidOperationException`. To replace an existing registration, the previous entry must be explicitly removed through the factory before re-registering.
- **Formatter lifetime:** Once registered, a formatter instance remains available for the lifetime of the process unless explicitly removed. The factory holds a strong reference, preventing garbage collection of registered formatters.
