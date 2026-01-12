# AuthenticationMiddleware

A middleware component for the `dotnet-actor-framework` that handles authentication of incoming messages using token-based or whitelist-based authentication providers. It validates tokens or checks sender whitelists before allowing message processing to proceed.

## API

### `AuthenticationMiddleware`

The constructor for the middleware. Initializes a new instance with default or provided authentication providers.
