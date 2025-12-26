// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetActorFramework.Utilities;

/// <summary>
/// Extension methods for concurrent collections to simplify common operations.
/// These reduce boilerplate when working with thread-safe collections in the framework.
/// </summary>
public static class ConcurrentCollectionExtensions
{
    /// <summary>
    /// Gets all values from a ConcurrentDictionary.
    /// </summary>
    public static IEnumerable<TValue> GetAllValues<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        if (dictionary == null) yield break;
        foreach (var value in dictionary.Values)
            yield return value;
    }

    /// <summary>
    /// Gets all keys from a ConcurrentDictionary.
    /// </summary>
    public static IEnumerable<TKey> GetAllKeys<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        if (dictionary == null) yield break;
        foreach (var key in dictionary.Keys)
            yield return key;
    }

    /// <summary>
    /// Tries to get a value, returning a default value if not found.
    /// </summary>
    public static TValue? GetValueOrDefault<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue? defaultValue = default)
        where TKey : notnull
    {
        if (dictionary == null) return defaultValue;
        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets the count of items in a ConcurrentDictionary (thread-safe snapshot).
    /// </summary>
    public static int GetCount<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        return dictionary?.Count ?? 0;
    }

    /// <summary>
    /// Clears all items from a ConcurrentDictionary.
    /// </summary>
    public static void ClearAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        if (dictionary == null) return;
        foreach (var key in dictionary.Keys)
            dictionary.TryRemove(key, out _);
    }

    /// <summary>
    /// Removes all entries matching a predicate.
    /// </summary>
    public static int RemoveWhere<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        Func<TKey, TValue, bool> predicate)
        where TKey : notnull
    {
        if (dictionary == null) return 0;
        var count = 0;
        foreach (var kvp in dictionary)
        {
            if (predicate(kvp.Key, kvp.Value) && dictionary.TryRemove(kvp.Key, out _))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Enqueues multiple items to a ConcurrentQueue.
    /// </summary>
    public static void EnqueueRange<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)
    {
        if (queue == null || items == null) return;
        foreach (var item in items)
            queue.Enqueue(item);
    }

    /// <summary>
    /// Dequeues all available items from a ConcurrentQueue.
    /// </summary>
    public static List<T> DequeueAll<T>(this ConcurrentQueue<T> queue)
    {
        if (queue == null) return [];
        var items = new List<T>();
        while (queue.TryDequeue(out var item))
            items.Add(item);
        return items;
    }

    /// <summary>
    /// Gets the current count of items in a ConcurrentQueue.
    /// Note: This is a snapshot count and may change between calls.
    /// </summary>
    public static int GetCount<T>(this ConcurrentQueue<T> queue)
    {
        return queue?.Count ?? 0;
    }
}
