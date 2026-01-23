# ActorCacheService
The `ActorCacheService` is a caching mechanism designed to store and manage actor references, providing an efficient way to retrieve and update cached actor information. It offers methods to set, get, and remove cached actors, as well as to cache messages and check their cache status.

## API
* `public ActorCacheService`: The constructor for the `ActorCacheService` class, used to create a new instance of the service.
* `public void Set`: Sets a cached actor reference in the cache. Parameters and return values are not specified in the provided information.
* `public ActorRef? Get`: Retrieves a cached actor reference from the cache. Returns the cached `ActorRef` if found, otherwise `null`.
* `public bool Contains`: Checks if a specific actor reference is contained within the cache. Returns `true` if the actor is cached, `false` otherwise.
* `public bool Remove`: Removes a cached actor reference from the cache. Returns `true` if the removal was successful, `false` otherwise.
* `public void Clear`: Clears all cached actor references from the cache.
* `public int RemoveExpired`: Removes expired cached actor references from the cache. Returns the number of removed entries.
* `public ActorRef ActorRef`: Gets the actor reference associated with the cache entry.
* `public DateTime CachedAt`: Gets the timestamp when the actor reference was cached.
* `public DateTime LastAccessedAt`: Gets the timestamp when the cached actor reference was last accessed.
* `public CachedActorRef`: Represents a cached actor reference, including its associated actor and timestamps.
* `public MessageCacheService`: A related service for caching messages.
* `public void Cache`: Caches a message. Parameters and return values are not specified in the provided information.
* `public bool IsCached`: Checks if a message is cached. Returns `true` if the message is cached, `false` otherwise.
* `public void Clear`: Clears all cached messages.
* `public Message Message`: Gets the cached message.
* `public DateTime CachedAt`: Gets the timestamp when the message was cached.
* `public CachedMessage`: Represents a cached message, including its content and timestamp.

## Usage
```csharp
// Example 1: Basic caching of an actor reference
var cacheService = new ActorCacheService();
var actorRef = new ActorRef("exampleActor");
cacheService.Set(actorRef);
var cachedActorRef = cacheService.Get();
if (cachedActorRef != null)
{
    Console.WriteLine("Cached actor reference: " + cachedActorRef);
}

// Example 2: Caching and retrieving a message
var messageCacheService = new MessageCacheService();
var exampleMessage = new Message("Hello, world!");
messageCacheService.Cache(exampleMessage);
if (messageCacheService.IsCached(exampleMessage))
{
    var cachedMessage = messageCacheService.Message;
    Console.WriteLine("Cached message: " + cachedMessage);
}
```

## Notes
The `ActorCacheService` and `MessageCacheService` appear to be designed for use in a multi-threaded environment, given the presence of methods like `RemoveExpired` and the lack of explicit thread-safety warnings. However, the actual thread-safety of these classes depends on their implementation details, which are not provided. It is also worth noting that the `RemoveExpired` method may throw exceptions if the cache is modified concurrently while it is executing. Additionally, the `Clear` methods may have performance implications if the cache is very large, as they remove all cached entries without filtering.
