# DateTimeExtensionsValidation

A static utility class providing validation helpers for `DateTime` and `DateTimeOffset` values, ensuring temporal values meet expected constraints before use in business logic or API operations. The class centralizes validation rules for common date/time scenarios such as elapsed time checks, window comparisons, and timestamp formatting, returning human-readable error messages when constraints are violated.

## API

### `public static IReadOnlyList<string> Validate(DateTime value)`

Validates the given `DateTime` value against all supported temporal constraints. Returns a list of error messages describing any violations; an empty list indicates the value is valid. Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---

### `public static bool IsValid(DateTime value)`

Determines whether the given `DateTime` value passes all validation rules. Returns `true` if valid; otherwise `false`. Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to check.
- **Returns**
  - `bool`: `true` if valid; otherwise `false`.

---

### `public static void EnsureValid(DateTime value)`

Throws an `ArgumentException` if the given `DateTime` value fails any validation rule. Otherwise, returns silently. Intended for use in guard clauses to enforce temporal constraints early.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Throws**
  - `ArgumentException`: When `value` violates any temporal constraint. The exception message contains a human-readable description of the first encountered error.

---

### `public static IReadOnlyList<string> ValidateForGetElapsed(DateTime value)`

Validates a `DateTime` value specifically for use with `GetElapsed`. Returns a list of error messages if the value is unsuitable for elapsed-time calculations (e.g., future dates or invalid ticks). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---

### `public static IReadOnlyList<string> ValidateForGetElapsedMilliseconds(DateTime value)`

Validates a `DateTime` value specifically for use with `GetElapsedMilliseconds`. Returns a list of error messages if the value is unsuitable for millisecond-precision elapsed-time calculations. Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForHasElapsed(DateTime value)`

Validates a `DateTime` value specifically for use with `HasElapsed`. Returns a list of error messages if the value is unsuitable for elapsed-state checks (e.g., future dates or invalid ticks). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForIsPast(DateTime value)`

Validates a `DateTime` value specifically for use with `IsPast`. Returns a list of error messages if the value is unsuitable for past-state checks (e.g., future dates or invalid ticks). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForIsFuture(DateTime value)`

Validates a `DateTime` value specifically for use with `IsFuture`. Returns a list of error messages if the value is unsuitable for future-state checks (e.g., past dates or invalid ticks). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForGetTimeAgoDescription(DateTime value)`

Validates a `DateTime` value specifically for use with `GetTimeAgoDescription`. Returns a list of error messages if the value is unsuitable for relative-time descriptions (e.g., future dates or invalid ticks). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForRoundToSecond(DateTime value)`

Validates a `DateTime` value specifically for use with `RoundToSecond`. Returns a list of error messages if the value is unsuitable for second-level rounding (e.g., invalid ticks or out-of-range values). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForIsWithinWindow(DateTime value, DateTime start, DateTime end)`

Validates a `DateTime` value against a specified time window. Returns a list of error messages if the value is outside the window or otherwise unsuitable for window checks. Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
  - `start` (`DateTime`): The start of the time window.
  - `end` (`DateTime`): The end of the time window.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

---
### `public static IReadOnlyList<string> ValidateForGetLogTimestamp(DateTime value)`

Validates a `DateTime` value specifically for use with `GetLogTimestamp`. Returns a list of error messages if the value is unsuitable for log-timestamp formatting (e.g., invalid ticks or out-of-range values). Does not throw exceptions.

- **Parameters**
  - `value` (`DateTime`): The date/time value to validate.
- **Returns**
  - `IReadOnlyList<string>`: Zero or more error messages describing validation failures.

## Usage
