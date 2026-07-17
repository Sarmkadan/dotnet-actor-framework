# ActorSystemBuilderJsonExtensions

Provides System.Text.Json serialization and deserialization extensions for `ActorSystemBuilder` instances, enabling persistence and transmission of actor system configurations across application boundaries.

## API

### ToJson

```csharp
public static string ToJson(this ActorSystemBuilder value, bool indented = false)
```

Serializes an `ActorSystemBuilder` instance to a JSON string representation.

**Parameters:**
- `value` – The actor system builder to serialize. Must not be null.
- `indented` – When true, formats the JSON with indentation for human readability. When false (default), produces compact JSON.

**Return value:**
A JSON string containing the serialized state of the builder, including its system name, options, and middleware pipeline.

**Exceptions:**
- Throws `ArgumentNullException` if `value` is null.

**Remarks:**
The serialization captures the builder’s private fields via reflection to preserve its complete configuration state. Middleware instances are serialized by their type names; custom middleware must be registered on deserialization.

---

### FromJson

```csharp
public static ActorSystemBuilder? FromJson(string json)
```

Deserializes an `ActorSystemBuilder` from a JSON string.

**Parameters:**
- `json` – The JSON string to deserialize. Must not be null or empty.

**Return value:**
An `ActorSystemBuilder` instance configured with the deserialized state, or null if deserialization fails.

**Exceptions:**
- Throws `ArgumentException` if `json` is null or empty.

**Remarks:**
Returns null on JSON parse errors or missing required fields; no exception is thrown for malformed input. The deserializer reconstructs the builder with the captured system name and applies options and middleware via reflection.

---

### TryFromJson

```csharp
public static bool TryFromJson(string json, out ActorSystemBuilder? value)
```

Attempts to deserialize an `ActorSystemBuilder` from a JSON string, providing a safe alternative to `FromJson`.

**Parameters:**
- `json` – The JSON string to deserialize. Must not be null or empty.
- `value` – Receives the deserialized builder if successful, otherwise null.

**Return value:**
True if deserialization succeeds; otherwise false.

**Exceptions:**
- Throws `ArgumentException` if `json` is null or empty.

**Remarks:**
Catches `JsonException` internally and returns a boolean status, making it suitable for error handling without try/catch blocks.

---

### SystemName

```csharp
public string? SystemName { get; }
```

Gets the name of the actor system as configured during construction.

**Remarks:**
This property is exposed through the serialized state object and reflects the original system name provided to the builder’s constructor.

---

### Options

```csharp
public ActorSystemOptions? Options { get; }
```

Gets the actor system configuration options that control mailbox capacity, timeouts, and other runtime parameters.

**Remarks:**
Serialized as part of the builder state; null if no custom options were applied.

---

### Middleware

```csharp
public System.Collections.Generic.List<IActorMiddleware>? Middleware { get; }
```

Gets the list of middleware components registered in the actor system pipeline.

**Remarks:**
Serialized as part of the builder state; null if no middleware was added. Middleware instances are stored by type for deserialization.

## Usage

### Serialize a builder to JSON

```csharp
var builder = new ActorSystemBuilder("MyActorSystem")
    .WithLogging()
    .WithErrorHandling(ErrorHandlingStrategy.Resilience);

// Serialize with compact formatting
string compactJson = builder.ToJson();
Console.WriteLine(compactJson);

// Serialize with pretty-printing
string prettyJson = builder.ToJson(indented: true);
File.WriteAllText("system-config.json", prettyJson);
```

---

### Deserialize a builder from JSON

```csharp
string json = File.ReadAllText("system-config.json");

// Safe deserialization with error handling
if (ActorSystemBuilderJsonExtensions.TryFromJson(json, out var builder))
{
    var actorSystem = builder.Build();
    Console.WriteLine($"Loaded system: {builder.SystemName}");
}
else
{
    Console.Error.WriteLine("Failed to deserialize actor system configuration");
}

// Alternative: direct deserialization (returns null on failure)
var altBuilder = ActorSystemBuilderJsonExtensions.FromJson(json);
if (altBuilder != null)
{
    var actorSystem = altBuilder.Build();
}
```

## Notes

- **Thread safety:** The extension methods are thread-safe; concurrent calls to `ToJson`, `FromJson`, and `TryFromJson` do not share mutable state.

- **Middleware serialization:** Middleware components are serialized by their type names. When deserializing, the corresponding middleware types must be available in the assembly; otherwise, those entries are skipped.

- **Options handling:** Custom `ActorSystemOptions` are applied during deserialization via reflection. If the options object contains complex nested settings, ensure those types are also serializable.

- **Null handling:** `FromJson` returns null on any JSON parsing error, while `TryFromJson` provides a boolean result without throwing exceptions, making it the preferred method for production code.

- **CamelCase policy:** JSON serialization uses camelCase property naming by default, consistent with the framework’s web defaults.
