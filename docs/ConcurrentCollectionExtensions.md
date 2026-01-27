# ConcurrentCollectionExtensions

Provides a set of static extension methods for `ConcurrentDictionary<TKey, TValue>` and `ConcurrentQueue<T>` that simplify common bulk operations, safe value retrieval, and conditional removal. These methods reduce boilerplate when working with concurrent collections in actor-based or multi-threaded environments.

## API

### ConcurrentDictionary Extensions

#### `GetAllValues<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`

Returns an `IEnumerable<TValue>` containing all values currently stored in the dictionary. The enumeration represents a point-in-time snapshot of the value collection.

**Parameters:**
- `dictionary` — The `ConcurrentDictionary<TKey, TValue>` instance to enumerate.

**Returns:**
- `IEnumerable<TValue>` — All values present at the time of the call.

**Throws:**
- `ArgumentNullException` — if `dictionary` is `null`.

---

#### `GetAllKeys<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`

Returns an `IEnumerable<TKey>` containing all keys currently stored in the dictionary. The enumeration represents a point-in-time snapshot of the key collection.

**Parameters:**
- `dictionary` — The `ConcurrentDictionary<TKey, TValue>` instance to enumerate.

**Returns:**
- `IEnumerable<TKey>` — All keys present at the time of the call.

**Throws:**
- `ArgumentNullException` — if `dictionary` is `null`.

---

#### `GetValueOrDefault<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)`

Attempts to retrieve the value associated with the specified key. Returns the value if found; otherwise returns the default value for `TValue` (e.g., `null` for reference types, `0` for numeric value types).

**Parameters:**
- `dictionary` — The `ConcurrentDictionary<TKey, TValue>` instance to search.
- `key` — The key whose associated value is to be retrieved.

**Returns:**
- `TValue?` — The value associated with the key, or `default(TValue)` if the key is not present.

**Throws:**
- `ArgumentNullException` — if `dictionary` is `null`.

---

#### `GetCount<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`

Gets the number of key/value pairs contained in the dictionary. This is a point-in-time count and may change immediately after the call in concurrent scenarios.

**Parameters:**
- `dictionary` — The `ConcurrentDictionary<TKey, TValue>` instance.

**Returns:**
- `int` — The number of entries in the dictionary.

**Throws:**
- `ArgumentNullException` — if `dictionary` is `null`.

---

#### `ClearAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)`

Removes all keys and values from the dictionary. After this call, the dictionary will be empty.

**Parameters:**
- `dictionary` — The `ConcurrentDictionary<TKey, TValue>` instance to clear.

**Throws:**
- `ArgumentNullException` — if `dictionary` is `null`.

---

#### `RemoveWhere<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, Func<TKey, TValue, bool> predicate)`

Removes all key/value pairs that satisfy the specified predicate. The predicate is evaluated against each entry atomically during removal.

**Parameters:**
- `dictionary` — The `ConcurrentDictionary<TKey, TValue>` instance to modify.
- `predicate` — A function that receives each key and value and returns `true` if the entry should be removed.

**Returns:**
- `int` — The number of entries removed.

**Throws:**
- `ArgumentNullException` — if `dictionary` or `predicate` is `null`.

---

### ConcurrentQueue Extensions

#### `EnqueueRange<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)`

Enqueues multiple items into the queue in a single operation. Items are added in the order they are yielded by the enumerable.

**Parameters:**
- `queue` — The `ConcurrentQueue<T>` instance to enqueue into.
- `items` — The collection of items to add.

**Throws:**
- `ArgumentNullException` — if `queue` or `items` is `null`.

---

#### `DequeueAll<T>(this ConcurrentQueue<T> queue)`

Dequeues all items currently in the queue and returns them as a `List<T>`. The queue will be empty after this call. Items are returned in FIFO order.

**Parameters:**
- `queue` — The `ConcurrentQueue<T>` instance to drain.

**Returns:**
- `List<T>` — All items that were in the queue, in the order they were dequeued.

**Throws:**
- `ArgumentNullException` — if `queue` is `null`.

---

#### `GetCount<T>(this ConcurrentQueue<T> queue)`

Gets the number of items currently in the queue. This is a point-in-time count and may change immediately after the call in concurrent scenarios.

**Parameters:**
- `queue` — The `ConcurrentQueue<T>` instance.

**Returns:**
- `int` — The approximate number of items in the queue.

**Throws:**
- `ArgumentNullException` — if `queue` is `null`.

## Usage

### Example 1: Bulk Maintenance on a ConcurrentDictionary

```csharp
ConcurrentDictionary<string, ActorState> actors = new();

// Populate dictionary
actors.TryAdd("actor-1", new ActorState { Health = 100, IsActive = true });
actors.TryAdd("actor-2", new ActorState { Health = 0, IsActive = false });
actors.TryAdd("actor-3", new ActorState { Health = 50, IsActive = true });

// Remove all inactive or dead actors in one pass
int removed = actors.RemoveWhere((id, state) => !state.IsActive || state.Health <= 0);
Console.WriteLine($"Removed {removed} actors");

// Enumerate remaining active actors
foreach (var state in actors.GetAllValues())
{
    Console.WriteLine($"Active actor health: {state.Health}");
}

// Clear everything on shutdown
actors.ClearAll();
```

### Example 2: Batch Processing with a ConcurrentQueue

```csharp
ConcurrentQueue<WorkItem> workQueue = new();

// Producer enqueues a batch of work
var newItems = new List<WorkItem>
{
    new("task-1", Priority.High),
    new("task-2", Priority.Low),
    new("task-3", Priority.Medium)
};
workQueue.EnqueueRange(newItems);

Console.WriteLine($"Queue size after enqueue: {workQueue.GetCount()}");

// Consumer drains all available work for batch processing
List<WorkItem> batch = workQueue.DequeueAll();
Console.WriteLine($"Dequeued {batch.Count} items for processing");

foreach (var item in batch)
{
    ProcessWorkItem(item);
}
```

## Notes

- **Thread safety:** All methods delegate to the underlying concurrent collection's own thread-safe mechanisms. `GetAllValues`, `GetAllKeys`, `GetCount`, and `DequeueAll` return point-in-time snapshots; the actual state of the collection may change immediately after the call completes. No locks are held across the entire enumeration or drain operation.
- **Empty collections:** `GetAllValues` and `GetAllKeys` return empty enumerables when the dictionary contains no entries. `DequeueAll` returns an empty `List<T>` when the queue is empty. `GetCount` returns `0` in both cases.
- **`GetValueOrDefault`:** Does not distinguish between a key that is absent and a key whose value happens to equal `default(TValue)`. Use `TryGetValue` directly if that distinction matters.
- **`RemoveWhere`:** The predicate is invoked under the dictionary's internal locking per entry. Avoid long-running or blocking operations inside the predicate to prevent contention.
- **`EnqueueRange`:** Items are enqueued individually in sequence. If the `IEnumerable<T>` is lazily evaluated, it is enumerated once at call time. A `null` item within the sequence is permitted unless `T` is a non-nullable value type.
- **Null arguments:** Every method throws `ArgumentNullException` when the target collection is `null`. Methods accepting additional arguments (`predicate`, `items`) also throw if those arguments are `null`.
