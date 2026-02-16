// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

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
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get values from.</param>
    /// <returns>An enumerable of all values in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static IEnumerable<TValue> GetAllValues<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        foreach (var value in dictionary.Values)
        {
            yield return value;
        }
    }

    /// <summary>
    /// Gets all keys from a ConcurrentDictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get keys from.</param>
    /// <returns>An enumerable of all keys in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static IEnumerable<TKey> GetAllKeys<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        foreach (var key in dictionary.Keys)
        {
            yield return key;
        }
    }

    /// <summary>
    /// Tries to get a value, returning a default value if not found.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is not found.</param>
    /// <returns>The value associated with the key if found; otherwise, the default value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static TValue? GetValueOrDefault<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        TKey key,
        TValue? defaultValue = default)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(key);

        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Gets the count of items in a ConcurrentDictionary (thread-safe snapshot).
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to get the count from.</param>
    /// <returns>The number of items in the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static int GetCount<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        return dictionary.Count;
    }

    /// <summary>
    /// Clears all items from a ConcurrentDictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to clear.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dictionary"/> is <see langword="null"/>.</exception>
    public static void ClearAll<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        foreach (var key in dictionary.Keys.ToList())
        {
            dictionary.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Removes all entries matching a predicate.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="dictionary">The dictionary to modify.</param>
    /// <param name="predicate">The predicate to match entries against.</param>
    /// <returns>The number of entries removed.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="dictionary"/> is <see langword="null"/> or
    /// <paramref name="predicate"/> is <see langword="null"/>.
    /// </exception>
    public static int RemoveWhere<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> dictionary,
        Func<TKey, TValue, bool> predicate)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentNullException.ThrowIfNull(predicate);

        var count = 0;
        foreach (var kvp in dictionary.ToList())
        {
            if (predicate(kvp.Key, kvp.Value) && dictionary.TryRemove(kvp.Key, out _))
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Enqueues multiple items to a ConcurrentQueue.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    /// <param name="queue">The queue to enqueue items to.</param>
    /// <param name="items">The items to enqueue.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="queue"/> is <see langword="null"/> or
    /// <paramref name="items"/> is <see langword="null"/>.
    /// </exception>
    public static void EnqueueRange<T>(this ConcurrentQueue<T> queue, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(queue);
        ArgumentNullException.ThrowIfNull(items);

        foreach (var item in items)
        {
            queue.Enqueue(item);
        }
    }

    /// <summary>
    /// Dequeues all available items from a ConcurrentQueue.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    /// <param name="queue">The queue to dequeue items from.</param>
    /// <returns>A list containing all dequeued items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="queue"/> is <see langword="null"/>.</exception>
    public static List<T> DequeueAll<T>(this ConcurrentQueue<T> queue)
    {
        ArgumentNullException.ThrowIfNull(queue);

        var items = new List<T>();
        while (queue.TryDequeue(out var item))
        {
            items.Add(item);
        }
        return items;
    }

    /// <summary>
    /// Gets the current count of items in a ConcurrentQueue.
    /// Note: This is a snapshot count and may change between calls.
    /// </summary>
    /// <typeparam name="T">The type of items in the queue.</typeparam>
    /// <param name="queue">The queue to get the count from.</param>
    /// <returns>The current count of items in the queue.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="queue"/> is <see langword="null"/>.</exception>
    public static int GetCount<T>(this ConcurrentQueue<T> queue)
    {
        ArgumentNullException.ThrowIfNull(queue);
        return queue.Count;
    }
}