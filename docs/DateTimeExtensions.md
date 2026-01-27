# DateTimeExtensions

Extension methods for `System.DateTime` that provide common time-related operations such as elapsed time calculation, rounding, and timestamp formatting.

## API

### `GetElapsed(DateTime start, DateTime end)`
Calculates the elapsed time between two `DateTime` instances.

- **Parameters:**
  - `start` – The starting `DateTime`.
  - `end` – The ending `DateTime`.
- **Returns:** A `TimeSpan` representing the elapsed time between `start` and `end`.
- **Throws:** `ArgumentOutOfRangeException` if `end` is earlier than `start`.

### `GetElapsedMilliseconds(DateTime start, DateTime end)`
Calculates the elapsed time in milliseconds between two `DateTime` instances.

- **Parameters:**
  - `start` – The starting `DateTime`.
  - `end` – The ending `DateTime`.
- **Returns:** A `long` representing the elapsed time in milliseconds.
- **Throws:** `ArgumentOutOfRangeException` if `end` is earlier than `start`.

### `HasElapsed(DateTime target, TimeSpan tolerance)`
Determines whether the specified `DateTime` has elapsed relative to the current time, within a given tolerance.

- **Parameters:**
  - `target` – The `DateTime` to check.
  - `tolerance` – The allowed time difference before considering the target elapsed.
- **Returns:** `true` if `target` has elapsed within the tolerance; otherwise, `false`.

### `IsPast(DateTime target)`
Determines whether the specified `DateTime` is in the past relative to the current time.

- **Parameters:**
  - `target` – The `DateTime` to check.
- **Returns:** `true` if `target` is earlier than `DateTime.UtcNow`; otherwise, `false`.

### `IsFuture(DateTime target)`
Determines whether the specified `DateTime` is in the future relative to the current time.

- **Parameters:**
  - `target` – The `DateTime` to check.
- **Returns:** `true` if `target` is later than `DateTime.UtcNow`; otherwise, `false`.

### `GetTimeAgoDescription(DateTime target)`
Generates a human-readable string describing how long ago the specified `DateTime` occurred.

- **Parameters:**
  - `target` – The `DateTime` to describe.
- **Returns:** A `string` such as `"2 minutes ago"` or `"in 3 hours"`.
- **Throws:** `ArgumentOutOfRangeException` if `target` is invalid (e.g., `DateTime.MinValue` or `DateTime.MaxValue`).

### `RoundToSecond(DateTime dateTime)`
Rounds a `DateTime` to the nearest second by truncating sub-second precision.

- **Parameters:**
  - `dateTime` – The `DateTime` to round.
- **Returns:** A new `DateTime` with milliseconds and ticks set to zero.

### `IsWithinWindow(DateTime target, TimeSpan window)`
Determines whether the specified `DateTime` falls within a time window around the current time.

- **Parameters:**
  - `target` – The `DateTime` to check.
  - `window` – The time span defining the window around the current time.
- **Returns:** `true` if `target` is within `[DateTime.UtcNow - window, DateTime.UtcNow + window]`; otherwise, `false`.

### `GetLogTimestamp(DateTime dateTime)`
Formats a `DateTime` as a standardized log timestamp string in ISO 8601 format with UTC indication.

- **Parameters:**
  - `dateTime` – The `DateTime` to format.
- **Returns:** A `string` in the format `"yyyy-MM-ddTHH:mm:ss.fffZ"`.
- **Throws:** `ArgumentOutOfRangeException` if `dateTime` is outside the valid range for `DateTime`.

## Usage
