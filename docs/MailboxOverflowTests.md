# MailboxOverflowTests

Unit tests for verifying the behavior of actor mailbox overflow handling in the dotnet-actor-framework. These tests validate that the mailbox correctly manages capacity limits, prevents message loss under burst traffic, maintains thread safety during concurrent access, and accurately reflects its full state.

## API

### `EnqueueAsync_WithBurstTraffic_DoesNotCauseMessageLoss`
Ensures that rapid successive calls to `EnqueueAsync` do not result in message loss when the mailbox reaches or exceeds its configured capacity.

- **Parameters**: None
- **Return value**: `Task` (completes when all enqueued messages are processed)
- **Throws**: Does not throw under normal conditions; fails the test if message loss is detected

### `EnqueueAsync_WithConcurrentAccess_DoesNotCauseRaceConditions`
Validates that concurrent calls to `EnqueueAsync` from multiple threads do not lead to race conditions or corrupt the mailbox state.

- **Parameters**: None
- **Return value**: `Task` (completes when all concurrent operations finish)
- **Throws**: Fails the test if race conditions are detected (e.g., duplicate messages, lost messages, or corrupted state)

### `Mailbox_IsFull_AccuratelyReflectsCapacity`
Confirms that the mailbox's `IsFull` property correctly reflects its current capacity state at all times, including edge cases near full capacity.

- **Parameters**: None
- **Return value**: `void` (synchronous test assertion)
- **Throws**: Fails the test if `IsFull` does not match the expected state based on enqueued message count

### `MailboxService_Constructor_ValidatesCapacity`
Verifies that the `MailboxService` constructor properly validates the provided capacity parameter, rejecting invalid values (e.g., zero or negative capacity).

- **Parameters**: None (constructor parameters are validated internally)
- **Return value**: `void` (synchronous test assertion)
- **Throws**: Fails the test if invalid capacity values are accepted or valid values are rejected

## Usage

### Example 1: Testing burst traffic handling
