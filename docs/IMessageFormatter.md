# IMessageFormatter

`IMessageFormatter` defines the contract for formatting messages in the actor framework, enabling consistent serialization and deserialization of message payloads.

## API

### `string Format(object? message, bool includeHeaders = true)`
Formats the provided message object into a string representation, optionally including message headers.

- **Parameters**
  - `message`: The message object to format. May be `null`.
  - `includeHeaders`: If `true`, includes message headers in the output. Defaults to `true`.
- **Return value**: A string representation of the message.
- **Exceptions**: Throws `ArgumentNullException` if `message` is `null` and `includeHeaders` is `true`.

---

### `bool IncludeHeaders { get; }`
Gets a value indicating whether headers are included in formatted messages.

- **Return value**: `true` if headers are included; otherwise, `false`.

---

### `MessageFormatterFactory { get; }`
Gets the factory used to create instances of `IMessageFormatter`.

- **Return value**: A `MessageFormatterFactory` delegate capable of creating formatters.

---

### `void Register(Type messageType, Func<IMessageFormatter> formatterFactory)`
Registers a formatter factory for a specific message type.

- **Parameters**
  - `messageType`: The type of message to associate with the formatter.
  - `formatterFactory`: A delegate that creates an `IMessageFormatter` for the given message type.
- **Exceptions**: Throws `ArgumentNullException` if `messageType` or `formatterFactory` is `null`.

---
### `IMessageFormatter? GetFormatter(Type messageType)`
Retrieves a formatter for the specified message type, if one is registered.

- **Parameters**
  - `messageType`: The type of message for which to retrieve a formatter.
- **Return value**: An `IMessageFormatter` instance if a formatter is registered for `messageType`; otherwise, `null`.
- **Exceptions**: Throws `ArgumentNullException` if `messageType` is `null`.

---
### `string Format(object? message, bool includeHeaders = true)`
Formats the provided message object into a string representation, optionally including message headers.

- **Parameters**
  - `message`: The message object to format. May be `null`.
  - `includeHeaders`: If `true`, includes message headers in the output. Defaults to `true`.
- **Return value**: A string representation of the message.
- **Exceptions**: Throws `ArgumentNullException` if `message` is `null` and `includeHeaders` is `true`.

## Usage

### Basic Formatting
