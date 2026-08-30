using System;
using System.Linq;
using System.Threading.Tasks;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;
using Xunit;

namespace DotNetActorFramework.Tests;

public sealed class MessageBatcherTests
{
    [Fact]
    public void AddMessage_ReturnsNullUntilBatchSizeReached_ThenReturnsFullBatch()
    {
        using var batcher = new MessageBatcher(batchSize: 3, batchTimeout: TimeSpan.FromMinutes(1));
        var messages = CreateMessages("one", "two", "three");

        Assert.Null(batcher.AddMessage("orders", messages[0]));
        Assert.Null(batcher.AddMessage("orders", messages[1]));

        var batch = batcher.AddMessage("orders", messages[2]);

        Assert.NotNull(batch);
        Assert.Equal(messages, batch);
        Assert.Null(batcher.FlushBatch("orders"));
    }

    [Fact]
    public void FlushBatch_ReturnsPendingMessagesAndEmptiesBatch()
    {
        using var batcher = new MessageBatcher(batchSize: 10, batchTimeout: TimeSpan.FromMinutes(1));
        var messages = CreateMessages("one", "two");
        batcher.AddMessage("orders", messages[0]);
        batcher.AddMessage("orders", messages[1]);

        var flushed = batcher.FlushBatch("orders");

        Assert.NotNull(flushed);
        Assert.Equal(messages, flushed);
        Assert.Null(batcher.FlushBatch("orders"));
    }

    [Fact]
    public void FlushAll_ReturnsAllPendingBatchesKeyedCorrectly()
    {
        using var batcher = new MessageBatcher(batchSize: 10, batchTimeout: TimeSpan.FromMinutes(1));
        var order = new ControlMessage("order");
        var notification = new ControlMessage("notification");
        batcher.AddMessage("orders", order);
        batcher.AddMessage("notifications", notification);

        var flushed = batcher.FlushAll();

        Assert.Equal(2, flushed.Count);
        Assert.Equal(new[] { order }, flushed["orders"]);
        Assert.Equal(new[] { notification }, flushed["notifications"]);
        Assert.Empty(batcher.FlushAll());
    }

    [Fact]
    public void AddMessage_DifferentBatchKeysAreIsolated()
    {
        using var batcher = new MessageBatcher(batchSize: 2, batchTimeout: TimeSpan.FromMinutes(1));
        var firstOrder = new ControlMessage("first-order");
        var notification = new ControlMessage("notification");
        var secondOrder = new ControlMessage("second-order");

        Assert.Null(batcher.AddMessage("orders", firstOrder));
        Assert.Null(batcher.AddMessage("notifications", notification));

        var orders = batcher.AddMessage("orders", secondOrder);

        Assert.NotNull(orders);
        Assert.Equal(new[] { firstOrder, secondOrder }, orders);
        Assert.Equal(new[] { notification }, batcher.FlushBatch("notifications"));
    }

    [Fact]
    public async Task BatchExpired_FiresAfterTimeoutWithBatchKeyAndMessages()
    {
        var batchTimeout = TimeSpan.FromMilliseconds(50);
        using var batcher = new MessageBatcher(batchSize: 10, batchTimeout: batchTimeout);
        var message = new ControlMessage("pending");
        var expired = new TaskCompletionSource<(string Key, IReadOnlyList<Message> Messages)>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        batcher.BatchExpired += (key, messages) => expired.TrySetResult((key, messages));

        batcher.AddMessage("orders", message);

        var result = await expired.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("orders", result.Key);
        Assert.Equal(new[] { message }, result.Messages);
        Assert.Null(batcher.FlushBatch("orders"));
    }

    [Fact]
    public async Task MessageDeduplicator_RegisterAndWindowExpiry_UpdateDuplicateStatus()
    {
        var deduplicator = new MessageDeduplicator(
            maxCapacity: 10,
            deduplicationWindow: TimeSpan.FromMilliseconds(50));
        var messageId = Guid.NewGuid();

        Assert.False(deduplicator.IsDuplicate(messageId));
        deduplicator.RegisterMessage(messageId);
        Assert.True(deduplicator.IsDuplicate(messageId));

        await Task.Delay(TimeSpan.FromMilliseconds(200));

        Assert.False(deduplicator.IsDuplicate(messageId));
    }

    [Fact]
    public void MessageDeduplicator_WhenCapacityReached_EvictsOldestMessage()
    {
        var deduplicator = new MessageDeduplicator(maxCapacity: 2, deduplicationWindow: TimeSpan.FromMinutes(1));
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var third = Guid.NewGuid();
        deduplicator.RegisterMessage(first);
        deduplicator.RegisterMessage(second);

        deduplicator.RegisterMessage(third);

        Assert.False(deduplicator.IsDuplicate(first));
        Assert.True(deduplicator.IsDuplicate(second));
        Assert.True(deduplicator.IsDuplicate(third));
    }

    [Fact]
    public async Task MessageThrottler_TryProcess_AllowsConfiguredRateThenRefuses()
    {
        const int messagesPerSecond = 4;
        var throttler = new MessageThrottler(messagesPerSecond);

        for (var i = 0; i < messagesPerSecond; i++)
        {
            Assert.True(throttler.TryProcess());
            if (i < messagesPerSecond - 1)
                await Task.Delay(TimeSpan.FromMilliseconds(300));
        }

        Assert.False(throttler.TryProcess());
    }

    private static ControlMessage[] CreateMessages(params string[] commands) =>
        commands.Select(command => new ControlMessage(command)).ToArray();
}
