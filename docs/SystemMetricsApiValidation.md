# SystemMetricsApiValidation

Provides validation utilities for system metrics API inputs, ensuring consistency and correctness of metric data before processing or storage.

## API

### `Validate(SystemMetricsApiRequest request)`

Validates the structure and content of a `SystemMetricsApiRequest` object.

- **Parameters**
  - `request`: The request object to validate.
- **Returns**
  - `IReadOnlyList<string>`: A list of validation error messages. Empty if the request is valid.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.

### `Validate(SystemMetricsApiResponse response)`

Validates the structure and content of a `SystemMetricsApiResponse` object.

- **Parameters**
  - `response`: The response object to validate.
- **Returns**
  - `IReadOnlyList<string>`: A list of validation error messages. Empty if the response is valid.
- **Throws**
  - `ArgumentNullException`: If `response` is `null`.

### `Validate(SystemMetricsApiBatch batch)`

Validates the structure and content of a `SystemMetricsApiBatch` object.

- **Parameters**
  - `batch`: The batch object to validate.
- **Returns**
  - `IReadOnlyList<string>`: A list of validation error messages. Empty if the batch is valid.
- **Throws**
  - `ArgumentNullException`: If `batch` is `null`.

### `IsValid(SystemMetricsApiRequest request)`

Checks whether a `SystemMetricsApiRequest` object is valid.

- **Parameters**
  - `request`: The request object to check.
- **Returns**
  - `bool`: `true` if the request is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.

### `IsValid(SystemMetricsApiResponse response)`

Checks whether a `SystemMetricsApiResponse` object is valid.

- **Parameters**
  - `response`: The response object to check.
- **Returns**
  - `bool`: `true` if the response is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `response` is `null`.

### `IsValid(SystemMetricsApiBatch batch)`

Checks whether a `SystemMetricsApiBatch` object is valid.

- **Parameters**
  - `batch`: The batch object to check.
- **Returns**
  - `bool`: `true` if the batch is valid; otherwise, `false`.
- **Throws**
  - `ArgumentNullException`: If `batch` is `null`.

### `EnsureValid(SystemMetricsApiRequest request)`

Validates a `SystemMetricsApiRequest` object and throws if invalid.

- **Parameters**
  - `request`: The request object to validate.
- **Throws**
  - `ArgumentNullException`: If `request` is `null`.
  - `SystemMetricsApiValidationException`: If the request is invalid, containing validation error messages.

### `EnsureValid(SystemMetricsApiResponse response)`

Validates a `SystemMetricsApiResponse` object and throws if invalid.

- **Parameters**
  - `response`: The response object to validate.
- **Throws**
  - `ArgumentNullException`: If `response` is `null`.
  - `SystemMetricsApiValidationException`: If the response is invalid, containing validation error messages.

### `EnsureValid(SystemMetricsApiBatch batch)`

Validates a `SystemMetricsApiBatch` object and throws if invalid.

- **Parameters**
  - `batch`: The batch object to validate.
- **Throws**
  - `ArgumentNullException`: If `batch` is `null`.
  - `SystemMetricsApiValidationException`: If the batch is invalid, containing validation error messages.

## Usage
