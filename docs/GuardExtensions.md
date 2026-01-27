# GuardExtensions

Provides static extension methods for validating method arguments and throwing exceptions when preconditions are violated. Designed to enforce invariants early in public APIs with minimal boilerplate.

## API

### `NotNull<T>(T value)`
Validates that the input is not `null`.
- **Parameters**: `value` – the value to check.
- **Return value**: The input `value` if not `null`.
- **Throws**: `ArgumentNullException` if `value` is `null`.

### `NotNullOrEmpty(string value)`
Validates that the input string is not `null` and not empty.
- **Parameters**: `value` – the string to check.
- **Return value**: The input `value` if valid.
- **Throws**: `ArgumentNullException` if `value` is `null`; `ArgumentException` if `value` is empty.

### `NotNullOrWhiteSpace(string value)`
Validates that the input string is not `null`, not empty, and does not consist only of whitespace.
- **Parameters**: `value` – the string to check.
- **Return value**: The input `value` if valid.
- **Throws**: `ArgumentNullException` if `value` is `null`; `ArgumentException` if `value` is empty or whitespace-only.

### `NotEmpty(Guid value)`
Validates that the input `Guid` is not empty (i.e., not `Guid.Empty`).
- **Parameters**: `value` – the `Guid` to check.
- **Return value**: The input `value` if valid.
- **Throws**: `ArgumentException` with message “Guid cannot be empty.” if `value` is `Guid.Empty`.

### `MustBePositive(int value)`
Validates that the input integer is strictly greater than zero.
- **Parameters**: `value` – the integer to check.
- **Return value**: The input `value` if valid.
- **Throws**: `ArgumentOutOfRangeException` if `value` ≤ 0.

### `MustBeNonNegative(int value)`
Validates that the input integer is greater than or equal to zero.
- **Parameters**: `value` – the integer to check.
- **Return value**: The input `value` if valid.
- **Throws**: `ArgumentOutOfRangeException` if `value` < 0.

### `NotEmpty<T>(IEnumerable<T> sequence)`
Validates that the input sequence is not `null` and contains at least one element.
- **Parameters**: `sequence` – the sequence to check.
- **Return value**: The input `sequence` if valid.
- **Throws**: `ArgumentNullException` if `sequence` is `null`; `ArgumentException` if `sequence` is empty.

### `MustBeTrue(bool condition)`
Validates that the input boolean is `true`.
- **Parameters**: `condition` – the boolean to check.
- **Throws**: `ArgumentException` if `condition` is `false`.

### `MustBeFalse(bool condition)`
Validates that the input boolean is `false`.
- **Parameters**: `condition` – the boolean to check.
- **Throws**: `ArgumentException` if `condition` is `true`.

## Usage
