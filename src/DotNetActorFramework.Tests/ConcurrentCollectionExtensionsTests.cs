// Copyright (c) 2024
// SPDX-License-Identifier: MIT

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Tests;

public class ConcurrentCollectionExtensionsTests
{
    [Fact]
    public void GetAllValues_HappyPath_ReturnsAllValues()
    {
        var dict = new ConcurrentDictionary<int, string>();
        dict[1] = "one";
        dict[2] = "two";

        var values = dict.GetAllValues().ToList();

        Assert.Equal(2, values.Count);
        Assert.Contains("one", values);
        Assert.Contains("two", values);
    }

    [Fact]
    public void GetAllValues_NullDictionary_ThrowsArgumentNullException()
    {
        ConcurrentDictionary<int, string>? dict = null;
        Assert.Throws<ArgumentNullException>(() => dict!.GetAllValues().ToList());
    }

    [Fact]
    public void GetAllKeys_HappyPath_ReturnsAllKeys()
    {
        var dict = new ConcurrentDictionary<string, int>();
        dict["a"] = 10;
        dict["b"] = 20;

        var keys = dict.GetAllKeys().ToList();

        Assert.Equal(2, keys.Count);
        Assert.Contains("a", keys);
        Assert.Contains("b", keys);
    }

    [Fact]
    public void GetAllKeys_NullDictionary_ThrowsArgumentNullException()
    {
        ConcurrentDictionary<string, int>? dict = null;
        Assert.Throws<ArgumentNullException>(() => dict!.GetAllKeys().ToList());
    }

    [Fact]
    public void GetValueOrDefault_KeyExists_ReturnsValue()
    {
        var dict = new ConcurrentDictionary<int, string>();
        dict[42] = "answer";

        var result = dict.GetValueOrDefault(42, "fallback");

        Assert.Equal("answer", result);
    }

    [Fact]
    public void GetValueOrDefault_KeyMissing_ReturnsDefault()
    {
        var dict = new ConcurrentDictionary<int, string>();
        var result = dict.GetValueOrDefault(99, "fallback");

        Assert.Equal("fallback", result);
    }

    [Fact]
    public void GetValueOrDefault_NullDictionary_ThrowsArgumentNullException()
    {
        ConcurrentDictionary<int, string>? dict = null;
        Assert.Throws<ArgumentNullException>(() => dict!.GetValueOrDefault(1));
    }

    [Fact]
    public void GetValueOrDefault_NullKey_ThrowsArgumentNullException()
    {
        var dict = new ConcurrentDictionary<string, int>();
        Assert.Throws<ArgumentNullException>(() => dict.GetValueOrDefault(null!));
    }

    [Fact]
    public void GetCount_Dictionary_HappyPath()
    {
        var dict = new ConcurrentDictionary<int, string>();
        dict[1] = "a";
        dict[2] = "b";

        Assert.Equal(2, dict.GetCount());
    }

    [Fact]
    public void GetCount_NullDictionary_ThrowsArgumentNullException()
    {
        ConcurrentDictionary<int, string>? dict = null;
        Assert.Throws<ArgumentNullException>(() => dict!.GetCount());
    }

    [Fact]
    public void ClearAll_RemovesAllEntries()
    {
        var dict = new ConcurrentDictionary<int, string>();
        dict[1] = "x";
        dict[2] = "y";

        dict.ClearAll();

        Assert.Empty(dict);
    }

    [Fact]
    public void ClearAll_NullDictionary_ThrowsArgumentNullException()
    {
        ConcurrentDictionary<int, string>? dict = null;
        Assert.Throws<ArgumentNullException>(() => dict!.ClearAll());
    }

    [Fact]
    public void RemoveWhere_RemovesMatchingEntries()
    {
        var dict = new ConcurrentDictionary<int, string>();
        dict[1] = "keep";
        dict[2] = "remove";
        dict[3] = "remove";

        int removed = dict.RemoveWhere((k, v) => v == "remove");

        Assert.Equal(2, removed);
        Assert.Single(dict);
        Assert.Equal("keep", dict[1]);
    }

    [Fact]
    public void RemoveWhere_NullDictionary_ThrowsArgumentNullException()
    {
        ConcurrentDictionary<int, string>? dict = null;
        Assert.Throws<ArgumentNullException>(() => dict!.RemoveWhere((k, v) => true));
    }

    [Fact]
    public void RemoveWhere_NullPredicate_ThrowsArgumentNullException()
    {
        var dict = new ConcurrentDictionary<int, string>();
        Assert.Throws<ArgumentNullException>(() => dict.RemoveWhere(null!));
    }

    [Fact]
    public void EnqueueRange_HappyPath_EnqueuesAllItems()
    {
        var queue = new ConcurrentQueue<int>();
        var items = new[] { 1, 2, 3 };

        queue.EnqueueRange(items);

        Assert.Equal(3, queue.Count);
        Assert.True(queue.TryPeek(out var peeked));
        Assert.Equal(1, peeked);
    }

    [Fact]
    public void EnqueueRange_NullQueue_ThrowsArgumentNullException()
    {
        ConcurrentQueue<int>? queue = null;
        var items = new[] { 1 };
        Assert.Throws<ArgumentNullException>(() => queue!.EnqueueRange(items));
    }

    [Fact]
    public void EnqueueRange_NullItems_ThrowsArgumentNullException()
    {
        var queue = new ConcurrentQueue<int>();
        IEnumerable<int>? items = null;
        Assert.Throws<ArgumentNullException>(() => queue.EnqueueRange(items!));
    }

    [Fact]
    public void DequeueAll_HappyPath_ReturnsAllItems()
    {
        var queue = new ConcurrentQueue<string>();
        queue.Enqueue("a");
        queue.Enqueue("b");

        var result = queue.DequeueAll();

        Assert.Equal(2, result.Count);
        Assert.Contains("a", result);
        Assert.Contains("b", result);
        Assert.Empty(queue);
    }

    [Fact]
    public void DequeueAll_NullQueue_ThrowsArgumentNullException()
    {
        ConcurrentQueue<string>? queue = null;
        Assert.Throws<ArgumentNullException>(() => queue!.DequeueAll());
    }

    [Fact]
    public void GetCount_Queue_HappyPath()
    {
        var queue = new ConcurrentQueue<int>();
        queue.Enqueue(10);
        queue.Enqueue(20);

        Assert.Equal(2, queue.GetCount());
    }

    [Fact]
    public void GetCount_Queue_NullQueue_ThrowsArgumentNullException()
    {
        ConcurrentQueue<int>? queue = null;
        Assert.Throws<ArgumentNullException>(() => queue!.GetCount());
    }
}
