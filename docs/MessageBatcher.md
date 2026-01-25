# MessageBatcher

A utility class for batching, throttling, and deduplicating messages in actor-based systems. It aggregates messages into batches, enforces capacity limits, and provides throttling and deduplication mechanisms to optimize message processing while preventing redundant operations.

## API

### `public MessageBatcher`

Initializes a new instance of the `MessageBatcher` with default capacity and throttling settings. The batcher starts empty and tracks the creation timestamp.

### `public IEnumerable<Message>? AddMessage(Message message)`

Adds a single message to the current batch. If the batch reaches capacity after adding the message, the batch is flushed and returned. If throttling or deduplication rules prevent the message from being added, `null` is returned.

- **Parameters**:
  - `message`: The message to add to the batch.
- **Returns**:
  - An enumerable of messages representing the flushed batch, or `null` if the message was not added.
- **Throws**:
  - `ArgumentNullException`: If `message` is `null`.

### `public IEnumerable<Message>? FlushBatch()`

Forces the current batch to be flushed and returned, regardless of capacity. The batch is cleared after flushing.

- **Returns**:
  - An enumerable of messages representing the flushed batch, or `null` if the batch was empty.
- **Throws**:
  - No exceptions.

### `public Dictionary<string, IEnumerable<Message>> FlushAll()`

Flushes all pending batches, including any internal state used for throttling or deduplication. Returns a dictionary mapping message identifiers to their respective batches.

- **Returns**:
  - A dictionary where keys are message identifiers and values are batches of messages.
- **Throws**:
  - No exceptions.

### `public void Dispose()`

Releases all resources used by the batcher, including clearing internal buffers and resetting state for throttling and deduplication.

- **Throws**:
  - No exceptions.

### `public int Capacity`

Gets the maximum number of messages allowed in a single batch before automatic flushing.

- **Returns**:
  - The current capacity value.
- **Throws**:
  - No exceptions.

### `public DateTime CreatedAt`

Gets the timestamp when the batcher was initialized.

- **Returns**:
  - The creation timestamp.
- **Throws**:
  - No exceptions.

### `public MessageBatch`

Gets the current batch of messages awaiting processing.

- **Returns**:
  - The current batch as a `MessageBatch` instance.
- **Throws**:
  - No exceptions.

### `public void Add(Message message)`

Adds a message to the internal buffer for throttling and deduplication checks. This method does not immediately flush the batch.

- **Parameters**:
  - `message`: The message to register.
- **Throws**:
  - `ArgumentNullException`: If `message` is `null`.

### `public MessageThrottler`

Gets the throttler instance used to control the rate of message processing.

- **Returns**:
  - The `MessageThrottler` instance.
- **Throws**:
  - No exceptions.

### `public async Task ThrottleAsync()`

Asynchronously applies throttling logic to the current batch, delaying execution if necessary to adhere to rate limits.

- **Returns**:
  - A `Task` representing the asynchronous throttling operation.
- **Throws**:
  - No exceptions.

### `public bool TryProcess()`

Attempts to process the current batch, applying throttling and deduplication rules. Returns `true` if the batch was processed; otherwise, `false`.

- **Returns**:
  - `true` if the batch was processed; otherwise, `false`.
- **Throws**:
  - No exceptions.

### `public MessageDeduplicator`

Gets the deduplicator instance used to prevent processing duplicate messages.

- **Returns**:
  - The `MessageDeduplicator` instance.
- **Throws**:
  - No exceptions.

### `public bool IsDuplicate(Message message)`

Checks whether the given message is a duplicate based on the deduplicator's rules.

- **Parameters**:
  - `message`: The message to check for duplication.
- **Returns**:
  - `true` if the message is a duplicate; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `message` is `null`.

### `public void RegisterMessage(Message message)`

Registers a message with the deduplicator to track it for future duplicate checks.

- **Parameters**:
  - `message`: The message to register.
- **Throws**:
  - `ArgumentNullException`: If `message` is `null`.

### `public void Clear()`

Clears all internal buffers, including the current batch, throttling state, and deduplication history.

- **Throws**:
  - No exceptions.

## Usage

### Example 1: Basic Batching and Flushing
