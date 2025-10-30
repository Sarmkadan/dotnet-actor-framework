// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Example 6: Batch Processing
// Demonstrates efficient message batching for throughput optimization

public class BatchProcessorActor : Actor
{
    private readonly List<Message> _batch = new();
    private readonly int _batchSize = 10;
    private Timer? _flushTimer;
    private int _totalProcessed = 0;

    public BatchProcessorActor(ActorPath path) : base(path) { }

    public override async Task OnInitializeAsync()
    {
        // Flush batch every 2 seconds even if not full
        _flushTimer = new Timer(async _ => await FlushBatchAsync(),
            null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

        Console.WriteLine($"[{Path}] Batch processor started (batch size: {_batchSize})");
        await Task.CompletedTask;
    }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "add-item")
        {
            _batch.Add(message);

            if (_batch.Count >= _batchSize)
            {
                await FlushBatchAsync().ConfigureAwait(false);
            }
        }
        await Task.CompletedTask;
    }

    private async Task FlushBatchAsync()
    {
        if (_batch.Count == 0)
            return;

        Console.WriteLine($"[{Path}] Processing batch of {_batch.Count} items...");

        // Simulate batch processing
        await Task.Delay(100).ConfigureAwait(false);

        _totalProcessed += _batch.Count;
        Console.WriteLine($"[{Path}] Batch complete. Total processed: {_totalProcessed}");

        _batch.Clear();
    }

    public override async Task OnStopAsync()
    {
        _flushTimer?.Dispose();
        await FlushBatchAsync().ConfigureAwait(false);
        Console.WriteLine($"[{Path}] Batch processor stopped. Total items: {_totalProcessed}");
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Batch Processing ===\n");

        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "BatchProcessingSystem";
        });

        var sp = services.BuildServiceProvider();
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
        var system = await config.InitializeAsync().ConfigureAwait(false);

        Console.WriteLine("System initialized.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create batch processor
            var batchPath = new ActorPath("/user/batch-processor");
            var batchRef = await config.CreateActorAsync(batchPath).ConfigureAwait(false);

            Console.WriteLine("Batch processor created.\n");

            // Send items continuously
            Console.WriteLine("Sending items to batch processor...\n");

            for (int i = 0; i < 45; i++)
            {
                var msg = new ControlMessage("add-item", new Dictionary<string, object>
                {
                    { "item-id", i },
                    { "data", $"Item {i}" }
                });

                await dispatcher.SendAsync(batchRef, msg).ConfigureAwait(false);

                // Simulate item arrival intervals
                if (i % 10 == 9)
                    await Task.Delay(500).ConfigureAwait(false);
            }

            // Wait for last batch to flush
            await Task.Delay(3000).ConfigureAwait(false);

            var health = config.GetHealthSummary();
            Console.WriteLine($"\nFinal Status:");
            Console.WriteLine($"  Health: {health.GetHealthPercentage()}%");
        }
        finally
        {
            await system.ShutdownAsync().ConfigureAwait(false);
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
