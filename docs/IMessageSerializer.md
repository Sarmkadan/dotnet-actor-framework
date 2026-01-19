# IMessageSerializer

Defines a contract for converting messages to and from their binary representation. Implementations are responsible for encoding domain messages, envelopes, or generic payloads into byte arrays suitable for transport or storage, and for reversing that process.

## API

### `byte[] Serialize(object message)`
Serializes the supplied `message` instance into a byte array.  
- **Parameters** – `message`: The object to serialize; must not be `null`.  
- **Return value** – A byte array containing the serialized form of `message`.  
- **Exceptions** –  
  - `ArgumentNullException` if `message` is `null`.  
  - `SerializationException` if the message cannot be serialized (e.g., unsupported type or internal error).

### `Message? Deserialize(byte[] data)`
Attempts to deserialize a byte array into a `Message` instance.  
- **Parameters** – `data`: The byte array containing the serialized message; must not be `null`.  
- **Return value** – The deserialized `Message`, or `null` if the data does not represent a valid `Message`.  
- **Exceptions** –  
  - `ArgumentNullException` if `data` is `null`.  
  - `SerializationException` if the data is malformed or cannot be mapped to a `Message`.

### `byte[] Serialize<T>(T message)`
Generic overload that serializes a strongly‑typed message of type `T`.  
- **Parameters** – `message`: The instance of type `T` to serialize; must not be `null`.  
- **Return value** – A byte array representing the serialized `message`.  
- **Exceptions** –  
  - `ArgumentNullException` if `message` is `null`.  
  - `SerializationException` if serialization fails for the given type.

### `T? Deserialize<T>(byte[] data)`
Generic overload that attempts to deserialize a byte array into an instance of type `T`.  
- **Parameters** – `data`: The byte array containing the serialized value; must not be `null`.  
- **Return value** – The deserialized object of type `T`, or `null` if the data does not correspond to a valid `T`.  
- **Exceptions** –  
  - `ArgumentNullException` if `data` is `null`.  
  - `SerializationException` if the data cannot be deserialized to type `T`.

### `byte[] Serialize(Envelope envelope)`
Serializes an `Envelope` instance into a byte array.  
- **Parameters** – `envelope`: The envelope to serialize; must not be `null`.  
- **Return value** – A byte array containing the serialized envelope.  
- **Exceptions** –  
  - `ArgumentNullException` if `envelope` is `null`.  
  - `SerializationException` if the envelope cannot be serialized.

### `Envelope? Deserialize(byte[] data)`
Attempts to deserialize a byte array into an `Envelope` instance.  
- **Parameters** – `data`: The byte array containing the serialized envelope; must not be `null`.  
- **Return value** – The deserialized `Envelope`, or `null` if the data does not represent a valid envelope.  
- **Exceptions** –  
  - `ArgumentNullException` if `data` is `null`.  
  - `SerializationException` if the data is malformed or cannot be mapped to an `Envelope`.

## Usage

```csharp
// Example 1: Serializing and deserializing a custom message
IMessageSerializer serializer = new JsonMessageSerializer(); // hypothetical implementation

var msg = new GreetingMessage { Text = "Hello, Actor!" };
byte[] payload = serializer.Serialize(msg);

// Later, on the receiving end:
var received = serializer.Deserialize<GreetingMessage>(payload);
Console.WriteLine(received?.Text);
```

```csharp
// Example 2: Working with envelopes directly
IMessageSerializer serializer = new ProtobufMessageSerializer();

Envelope env = new Envelope
{
    MessageId = Guid.NewGuid(),
    CorrelationId = parentId,
    Payload = serializer.Serialize(new Command { Action = "Start" })
};

byte[] envelopeBytes = serializer.Serialize(env);

// On the consumer side:
Envelope? receivedEnv = serializer.Deserialize(envelopeBytes);
if (receivedEnv != null)
{
    var command = serializer.Deserialize<Command>(receivedEnv.Payload);
    // process command...
}
```

## Notes

- All `Serialize` methods throw `ArgumentNullException` when the input message or envelope is `null`. Implementations should validate arguments before attempting serialization.  
- Deserialization methods return `null` when the supplied byte array does not contain a recognizable representation of the expected type; this allows callers to distinguish between a successful but empty payload and a failure to parse.  
- Implementations are not required to be thread‑safe unless explicitly documented. Stateless serializers (e.g., those that rely only on immutable configuration) can be safely shared across threads, whereas stateful instances should be confined to a single thread or protected with external synchronization.  
- The generic methods (`Serialize<T>` and `Deserialize<T>`) rely on the runtime type of `T`; using them with open generic types or types lacking a parameterless constructor may result in `SerializationException`.  
- Byte arrays returned by `Serialize` are owned by the caller; mutating the returned array after serialization does not affect the internal state of the serializer.  
- Error messages thrown by `SerializationException` should include sufficient detail (e.g., the offending type or the position in the byte stream) to aid debugging, but they must not expose sensitive information.
