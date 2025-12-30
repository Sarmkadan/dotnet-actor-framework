# ConfigurationExceptionExtensions

The `ConfigurationExceptionExtensions` class provides extension methods for `ConfigurationException` instances in the `dotnet-actor-framework` project. These methods allow for type-safe inspection of configuration-related exceptions, enabling developers to determine the specific category of configuration failure (e.g., actor system, mailbox, or persistence) without relying on string parsing or reflection.

## API

### `IsActorSystemConfigurationException`
