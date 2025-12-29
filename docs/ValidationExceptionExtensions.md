# ValidationExceptionExtensions

Provides extension methods for creating and combining `ValidationException` instances with contextual information about actor framework validation failures.

## API

### `public static InvalidActorPathException WithContext(this ValidationException exception, string context)`

Adds contextual information to an existing `InvalidActorPathException` about where in the actor path the validation failed.

- **Parameters**
  - `exception`: The `InvalidActorPathException` to enrich with context.
  - `context`: A string describing the location or nature of the invalid path segment.
- **Return Value**: A new `InvalidActorPathException` with the original validation details and the additional context.
- **Throws**: `ArgumentNullException` if `exception` or `context` is `null`.

---

### `public static InvalidMessageException WithExpectedFormat(this ValidationException exception, string expectedFormat)`

Adds the expected message format to an existing `InvalidMessageException` to clarify what the correct message structure should be.

- **Parameters**
  - `exception`: The `InvalidMessageException` to enrich with the expected format.
  - `expectedFormat`: A string describing the correct format for the message.
- **Return Value**: A new `InvalidMessageException` with the original validation details and the expected format.
- **Throws**: `ArgumentNullException` if `exception` or `expectedFormat` is `null`.

---
### `public static InvalidActorReferenceException WithActorType(this ValidationException exception, string actorType)`

Adds the expected actor type to an existing `InvalidActorReferenceException` to clarify which actor type was expected in the reference.

- **Parameters**
  - `exception`: The `InvalidActorReferenceException` to enrich with the actor type.
  - `actorType`: A string describing the expected actor type.
- **Return Value**: A new `InvalidActorReferenceException` with the original validation details and the expected actor type.
- **Throws**: `ArgumentNullException` if `exception` or `actorType` is `null`.

---
### `public static ValidationException CombineWith(this ValidationException exception, ValidationException other)`

Combines two `ValidationException` instances into a single exception that aggregates their error messages.

- **Parameters**
  - `exception`: The first `ValidationException` to combine.
  - `other`: The second `ValidationException` to combine.
- **Return Value**: A new `ValidationException` containing the combined error messages from both exceptions.
- **Throws**: `ArgumentNullException` if `exception` or `other` is `null`.

---
### `public static bool IsValidationType(this Exception exception)`

Determines whether the given exception is one of the known validation exception types used by the actor framework.

- **Parameters**
  - `exception`: The `Exception` to check.
- **Return Value**: `true` if the exception is a `ValidationException`, `InvalidActorPathException`, `InvalidMessageException`, or `InvalidActorReferenceException`; otherwise, `false`.
- **Throws**: `ArgumentNullException` if `exception` is `null`.

## Usage

### Enriching a validation exception with context
