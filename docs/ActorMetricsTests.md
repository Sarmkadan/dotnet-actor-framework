# ActorMetricsTests

`ActorMetricsTests` contains a suite of unit tests that verify the behavior of the `ActorMetrics` class, which tracks message counts, error rates, processing times, duplicate detection, and health status for actors in the framework.

## API

### public void RecordMessageReceived_CalledMultipleTimes_IncrementsMessageCount  
**Purpose:** Confirms that each call to `RecordMessageReceived` increments the internal message counter by one.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if the assertion that the message count equals the expected value fails (typically an `AssertionException` from the test framework).

### public void GetErrorRate_WithFiftyPercentErrors_ReturnsFiftyPercent  
**Purpose:** Validates that `GetErrorRate` returns 0.5 when exactly half of the recorded messages are marked as errors.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if the returned error rate differs from 0.5 beyond an acceptable tolerance.

### public void GetErrorRate_WithNoMessages_ReturnsZeroWithoutDivisionError  
**Purpose:** Ensures that `GetErrorRate` returns 0 when no messages have been recorded, avoiding a divide‑by‑zero error.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if a non‑zero value is returned or if an exception is thrown during the call.

### public void RecordProcessingTime_WithThreeTimings_AveragesCorrectly  
**Purpose:** Checks that after recording three processing durations, the average reported by `GetAverageProcessingTime` equals the arithmetic mean of the three values.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if the computed average does not match the expected mean or if an unexpected exception occurs.

### public void IsUnhealthy_WhenErrorRateExceedsThreshold_ReturnsTrue  
**Purpose:** Verifies that `IsUnhealthy` returns `true` when the error rate surpasses the configured unhealthy threshold.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if the method returns `false` when it should return `true`, or if it throws unexpectedly.

### public void GetSummary_ReflectsCurrentMetricState  
**Purpose:** Asserts that the string returned by `GetSummary` contains the current values for message count, error rate, average processing time, and health status.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if any expected metric is missing or incorrectly formatted in the summary string.

### public void IsDuplicate_ForUnregisteredMessageId_ReturnsFalse  
**Purpose:** Confirms that `IsDuplicate` returns `false` for a message identifier that has not been previously registered.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if the method returns `true` for an unregistered ID or if an exception is thrown.

### public void IsDuplicate_AfterRegisterMessage_ReturnsTrueForSameId  
**Purpose:** Ensures that after a message ID is registered via `RegisterMessageId`, a subsequent call to `IsDuplicate` with the same ID returns `true`.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if the method returns `false` for a registered ID or if an unexpected exception occurs.

### public void Clear_AfterRegisteringMultipleIds_RemovesAllRecords  
**Purpose:** Validates that invoking `Clear` removes all registered message IDs and resets all metric counters to their initial state.  
**Parameters:** None.  
**Return Value:** `void`.  
**When it throws:** Throws an exception if any IDs remain registered, any counters are non‑zero, or if an exception is raised during the clear operation.

## Usage

```csharp
using Xunit;
using DotNetActorFramework.Metrics;

public class ActorMetricsTestsExamples
{
    [Fact]
    public void RecordMessageReceived_CalledMultipleTimes_IncrementsMessageCount()
    {
        // Arrange
        var metrics = new ActorMetrics();

        // Act
        metrics.RecordMessageReceived();
        metrics.RecordMessageReceived();
        metrics.RecordMessageReceived();

        // Assert
        Assert.Equal(3, metrics.GetMessageCount());
    }
}
```

```csharp
using Xunit;
using DotNetActorFramework.Metrics;

public class ActorMetricsTestsExamples
{
    [Fact]
    public void GetErrorRate_WithFiftyPercentErrors_ReturnsFiftyPercent()
    {
        // Arrange
        var metrics = new ActorMetrics();
        metrics.RecordMessageReceived(); // success
        metrics.RecordMessageReceived(); // error
        metrics.RecordMessageReceived(); // success
        metrics.RecordMessageReceived(); // error
        metrics.MarkLastMessageAsError(); // assume helper to flag error

        // Act
        var rate = metrics.GetErrorRate();

        // Assert
        Assert.Equal(0.5, rate, 2); // tolerance of 2 decimal places
    }
}
```

## Notes

- Each test method is independent; they do not rely on shared state unless explicitly set up within the method.
- The class is intended for execution by a test runner (e.g., xUnit, NUnit). Direct invocation outside of a test context will exercise the assertions but will not produce a test result.
- No thread‑safety guarantees are provided or implied; the tests assume single‑threaded execution. If `ActorMetrics` is used concurrently, external synchronization is required.
- Edge cases such as empty collections, boundary values for thresholds, and duplicate registration are covered by the respective test methods.
- Any modification to `ActorMetrics` that alters its public contract should be verified by updating or extending these tests accordingly.
