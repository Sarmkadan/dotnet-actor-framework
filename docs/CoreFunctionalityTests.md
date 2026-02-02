# CoreFunctionalityTests

Unit test class verifying core functionality of the actor framework, including actor registry operations and mailbox service behavior under various conditions.

## API

### `ActorRegistry_RegisterAndGet_ShouldReturnCorrectActor()`
Verifies that an actor can be registered in the registry and retrieved by its key, ensuring the correct actor instance is returned.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: May throw if registration fails or retrieval returns an incorrect actor instance.

---

### `ActorRegistry_Clear_ShouldRemoveAllActors()`
Confirms that clearing the actor registry removes all registered actors, leaving the registry empty.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: May throw if the registry is not properly cleared or contains residual entries.

---
### `MailboxService_CreateAndEnqueue_ShouldHoldMessage()`
Ensures that creating a mailbox and enqueuing a message retains the message until explicitly dequeued.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if the message is not retained or is lost during enqueue.

---
### `MailboxService_EnqueueAndDequeue_ShouldReturnSameMessage()`
Validates that a message enqueued in the mailbox can be dequeued and matches the original message exactly.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if the dequeued message differs from the enqueued message or the operation fails.

---
### `MailboxService_EnqueueToFullMailbox_ShouldFail()`
Checks that attempting to enqueue a message into a full mailbox results in a failure, preventing overflow.

- **Parameters**: None
- **Return value**: `Task`
- **Throws**: May throw if the enqueue operation succeeds despite the mailbox being full or if the failure is not properly signaled.

## Usage
