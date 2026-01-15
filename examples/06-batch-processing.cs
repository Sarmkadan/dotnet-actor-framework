using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Example 6: Batch Processing
// Demonstrates efficient message batching for throughput optimization

/// <summary>
/// Actor that processes messages in batches to improve throughput.
/// </summary>
public class BatchProcessorActor : Actor
{
    private readonly List<Message> _batch = new();
    private readonly int _batchSize = 10;
    private Timer? _flushTimer;
    private int _totalProcessed = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatchProcessorActor"/> class.
    /// </summary>
    /// <param name="path">The actor path.</param>
    public BatchProcessorActor(ActorPath path) : base(path) { }

    /// <summary>
    /// Called when the actor is initialized. Sets up a timer to flush the batch every 2 seconds.
    /// </summary>
    /// <returns>A completed task.</returns>
    public override async Task OnInitializeAsync()
    {
        // Flush batch every 2 seconds even if not full
        _flushTimer = new Timer(async _ => await FlushBatchAsync(),
            null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

        Console.WriteLine($"[{Path}] Batch processor started (batch size: {_batchSize})");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles incoming messages. Adds control messages with command "add-item" to the batch and flushes when the batch size is reached.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <returns>A completed task.</returns>
    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "add-item")
        {
            _batch.Add(message);

            if (_batch.Count >= _batchSize)
            {
                await FlushBatchAsync();
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
        await Task.Delay(100);

        _totalProcessed += _batch.Count;
        Console.WriteLine($"[{Path}] Batch complete. Total processed: {_totalProcessed}");

        _batch.Clear();
    }

    /// <summary>
    /// Called when the actor is stopping. Disposes the timer, flushes any remaining items, and logs the total processed.
    /// </summary>
    /// <returns>A completed task.</returns>
    public override async Task OnStopAsync()
    {
        _flushTimer?.Dispose();
        await FlushBatchAsync();
        Console.WriteLine($"[{Path}] Batch processor stopped. Total items: {_totalProcessed}");
    }
}

/// <summary>
/// Entry point for the batch processing example.
/// </summary>
class Program
{
    /// <summary>
    /// Starts the actor system, creates a batch processor actor, sends items, and displays health status.
    /// </summary>
    /// <returns>A completed task.</returns>
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
        var system = await config.InitializeAsync();

        Console.WriteLine("System initialized.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create batch processor
            var batchPath = new ActorPath("/user/batch-processor");
            var batchRef = await config.CreateActorAsync(batchPath);

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

                await dispatcher.SendAsync(batchRef, msg);

                // Simulate item arrival intervals
                if (i % 10 == 9)
                    await Task.Delay(500);
            }

            // Wait for last batch to flush
            await Task.Delay(3000);

            var health = config.GetHealthSummary();
            Console.WriteLine($"\nFinal Status:");
            Console.WriteLine($"  Health: {health.GetHealthPercentage()}%");
        }
        finally
        {
            await system.ShutdownAsync();
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
