# MessageExtensions

Provides static utility methods for inspecting and formatting messages in the actor framework. These extensions enable callers to determine message age, expiration status, validity, and to produce a canonical log representation without coupling to the internal message implementation.

## API

### GetAge

```csharp
public static long GetAge(this IMessage message)
```

Returns the elapsed time, in milliseconds, since the message was created. The value is computed as the difference between the current system time and the message’s creation timestamp.

**Parameters**
- `message` — The message to inspect. Must not be `null`.

**Return Value**
A `long` representing age in milliseconds. May be zero for messages just created.

**Throws**
- `ArgumentNullException` if `message` is `null`.

---

### HasExpired

```csharp
public static bool HasExpired(this IMessage message)
```

Determines whether the message has exceeded its configured time-to-live. A message is considered expired when its age is greater than or equal to its TTL.

**Parameters**
- `message` — The message to check. Must not be `null`.

**Return Value**
`true` if the message has expired; otherwise `false`. Messages with no TTL set (or a TTL of zero, depending on configuration) typically return `false`.

**Throws**
- `ArgumentNullException` if `message` is `null`.

---

### IsValid

```csharp
public static bool IsValid(this IMessage message)
```

Evaluates whether the message is still viable for processing. A message is valid if it has not expired and meets any additional framework-defined integrity checks (e.g., non-null payload, correct addressing).

**Parameters**
- `message` — The message to validate. Must not be `null`.

**Return Value**
`true` if the message is valid for delivery or processing; otherwise `false`.

**Throws**
- `ArgumentNullException` if `message` is `null`.

---

### GetLogFormat

```csharp
public static string GetLogFormat(this IMessage message)
```

Produces a human-readable, structured string suitable for logging and diagnostics. The format typically includes the message identifier, type, sender, recipient, age, and expiration status.

**Parameters**
- `message` — The message to format. Must not be `null`.

**Return Value**
A `string` containing the formatted log representation. Never returns `null`; returns a placeholder string for messages that cannot be fully resolved.

**Throws**
- `ArgumentNullException` if `message` is `null`.

## Usage

### Example 1: Discarding Expired Messages Before Processing

```csharp
void Dispatch(IMessage message)
{
    if (!message.IsValid())
    {
        logger.Warn(message.GetLogFormat());
        return;
    }

    logger.Info($"Processing message aged {message.GetAge()} ms");
    actor.Tell(message);
}
```

### Example 2: Monitoring Message Age in a Diagnostic Loop

```csharp
foreach (var msg in mailbox.Snapshot())
{
    if (msg.HasExpired())
    {
        deadLetters.Forward(msg);
        logger.Debug($"Expired: {msg.GetLogFormat()}");
    }
    else if (msg.GetAge() > warningThresholdMs)
    {
        logger.Warn($"Stale message: {msg.GetLogFormat()}");
    }
}
```

## Notes

- All methods throw `ArgumentNullException` when passed a `null` message reference; callers must guard against this or rely on upstream null checks.
- `GetAge` relies on the system clock and is therefore affected by clock skew or adjustments. The returned value is monotonic only within a single process lifetime under normal conditions.
- `HasExpired` and `IsValid` may return `false` for messages that have not been assigned a TTL. The exact behavior for a zero or unset TTL is framework-configuration dependent.
- `GetLogFormat` is designed for diagnostic output, not for machine parsing. The format may change between framework versions.
- These methods are static extension methods and carry no mutable state. They are safe to call concurrently from multiple threads, provided the underlying `IMessage` implementation is itself thread-safe for reads.
