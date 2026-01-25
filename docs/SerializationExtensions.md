# SerializationExtensions

A set of utility extension methods for serializing and deserializing objects to and from JSON, with support for both string and byte array representations. These methods simplify common serialization tasks and provide additional validation and deep copy functionality.

## API

### `ToJson<T>(T? value)`
Serializes the given object to a JSON string using the default JSON serializer settings.

- **Parameters**
  - `value` – The object to serialize. May be `null`.
- **Returns**
  - A JSON string representation of `value`, or `null` if `value` is `null`.
- **Throws**
  - `JsonException` – If serialization fails.

### `ToJsonPretty<T>(T? value)`
Serializes the given object to a human-readable JSON string with indentation.

- **Parameters**
  - `value` – The object to serialize. May be `null`.
- **Returns**
  - A formatted JSON string, or `null` if `value` is `null`.
- **Throws**
  - `JsonException` – If serialization fails.

### `ToJsonBytes<T>(T? value)`
Serializes the given object to a UTF-8 encoded JSON byte array.

- **Parameters**
  - `value` – The object to serialize. May be `null`.
- **Returns**
  - A byte array containing the JSON representation, or `null` if `value` is `null`.
- **Throws**
  - `JsonException` – If serialization fails.

### `FromJson<T>(string? json)`
Deserializes a JSON string into an object of type `T`.

- **Parameters**
  - `json` – The JSON string to deserialize. May be `null`.
- **Returns**
  - An instance of type `T`, or `null` if `json` is `null` or empty.
- **Throws**
  - `JsonException` – If deserialization fails or if `json` is invalid.

### `FromJsonBytes<T>(byte[]? data)`
Deserializes a UTF-8 encoded JSON byte array into an object of type `T`.

- **Parameters**
  - `data` – The byte array to deserialize. May be `null`.
- **Returns**
  - An instance of type `T`, or `null` if `data` is `null` or empty.
- **Throws**
  - `JsonException` – If deserialization fails or if `data` is invalid.

### `TryFromJson<T>(string? json, out T? result)`
Attempts to deserialize a JSON string into an object of type `T`. Returns `true` if successful.

- **Parameters**
  - `json` – The JSON string to deserialize. May be `null`.
  - `result` – When this method returns, contains the deserialized object or `null`.
- **Returns**
  - `true` if deserialization succeeded; otherwise, `false`.
- **Throws**
  - None.

### `IsValidJson(string? json)`
Determines whether the given string is valid JSON.

- **Parameters**
  - `json` – The string to validate. May be `null`.
- **Returns**
  - `true` if `json` is valid JSON; otherwise, `false`.

### `DeepCopy<T>(T? value)`
Creates a deep copy of the given object by serializing it to JSON and deserializing it back.

- **Parameters**
  - `value` – The object to copy. May be `null`.
- **Returns**
  - A deep copy of `value`, or `null` if `value` is `null`.
- **Throws**
  - `JsonException` – If serialization or deserialization fails.

## Usage
