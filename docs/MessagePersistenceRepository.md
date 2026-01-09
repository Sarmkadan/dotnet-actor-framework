# MessagePersistenceRepository

Provides persistence operations for actor messages, enabling storage, retrieval, and tracking of message delivery status within the actor framework.

## API

### `MessagePersistenceRepository`

Initializes a new instance of the persistence repository.

### `async Task<bool> PersistAsync()`

Persists the current message to the underlying store. The message is considered persisted once this operation completes successfully.

- **Returns**: `Task<bool>` indicating whether the operation succeeded.
- **Throws**: May throw if the underlying store is unavailable or encounters an error during persistence.

### `async Task<IReadOnlyList<PersistedMessage>> GetActorMessagesAsync()`

Retrieves all messages associated with the current actor from the store.

- **Returns**: `Task<IReadOnlyList<PersistedMessage>>` containing the list of persisted messages for the actor.
- **Throws**: May throw if the store is unavailable or encounters an error during retrieval.

### `async Task<IReadOnlyList<PersistedMessage>> GetUndeliveredMessagesAsync()`

Retrieves all messages that have not yet been marked as delivered for the current actor.

- **Returns**: `Task<IReadOnlyList<PersistedMessage>>` containing the list of undelivered messages.
- **Throws**: May throw if the store is unavailable or encounters an error during retrieval.

### `async Task<IReadOnlyList<PersistedMessage>> GetMessagesAsync()`

Retrieves all messages (regardless of delivery status) for the current actor.

- **Returns**: `Task<IReadOnlyList<PersistedMessage>>` containing the full list of messages.
- **Throws**: May throw if the store is unavailable or encounters an error during retrieval.

### `long GetMessageCount()`

Returns the total number of messages persisted for the current actor.

- **Returns**: `long` representing the total message count.

### `long GetCurrentSequenceNumber()`

Returns the highest sequence number assigned to any message in the store for the current actor.

- **Returns**: `long` representing the current sequence number.

### `PersistenceStatistics GetStatistics()`

Retrieves aggregated statistics about message persistence for the current actor, including total, delivered, and undelivered counts.

- **Returns**: `PersistenceStatistics` containing counts of total, delivered, and undelivered messages.

### `async Task<bool> MarkAsDeliveredAsync()`

Marks the current message as delivered in the store.

- **Returns**: `Task<bool>` indicating whether the operation succeeded.
- **Throws**: May throw if the store is unavailable or encounters an error during marking.

### `void Clear()`

Removes all persisted messages for the current actor from the store. Use with caution; this operation cannot be undone.

### Properties

#### `Guid EnvelopeId`
Gets the unique identifier of the message envelope.

#### `string MessageType`
Gets the type of the message.

#### `Guid? SenderId`
Gets the identifier of the sender actor, if available.

#### `Guid RecipientId`
Gets the identifier of the recipient actor.

#### `DateTime PersistedAt`
Gets the timestamp when the message was persisted.

#### `bool IsDelivered`
Gets a value indicating whether the message has been marked as delivered.

#### `long SequenceNumber`
Gets the sequence number assigned to the message.

#### `long TotalMessages` (from `PersistenceStatistics`)
Gets the total number of messages for the actor.

#### `long DeliveredMessages` (from `PersistenceStatistics`)
Gets the number of messages marked as delivered.

#### `long UndeliveredMessages` (from `PersistenceStatistics`)
Gets the number of messages not yet marked as delivered.

## Usage

### Persisting and retrieving a message
