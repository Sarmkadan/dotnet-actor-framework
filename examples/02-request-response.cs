// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

/// <summary>
/// Demonstrates bidirectional communication between actors using the request-response pattern.
/// </summary>
/// <example>
/// This example shows how to create a calculator actor that responds to requests from a requestor actor.
/// </example>
public class CalculatorActor : Actor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CalculatorActor"/> class.
    /// </summary>
    /// <param name="path">The actor's path.</param>
    public CalculatorActor(ActorPath path) : base(path) { }

    /// <summary>
    /// Handles incoming messages.
    /// </summary>
    /// <param name="message">The incoming message.</param>
    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm)
        {
            try
            {
                /// <summary>
                /// Calculates the result of the specified command.
                /// </summary>
                /// <param name="cm">The control message containing the command and parameters.</param>
                /// <returns>The calculated result.</returns>
                var result = cm.Command switch
                {
                    "add" => (int)cm.Parameters["a"] + (int)cm.Parameters["b"],
                    "multiply" => (int)cm.Parameters["a"] * (int)cm.Parameters["b"],
                    "divide" => cm.Parameters["b"].Equals(0)
                        ? throw new DivideByZeroException()
                        : (int)cm.Parameters["a"] / (int)cm.Parameters["b"],
                    _ => throw new InvalidOperationException($"Unknown command: {cm.Command}")
                };

                Console.WriteLine($"[{Path}] Calculated: {cm.Command} = {result}");
                Metrics.RecordSuccess();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Path}] Error: {ex.Message}");
                Metrics.RecordError(ex);
                throw;
            }
        }
        await Task.CompletedTask;
    }
}

/// <summary>
/// A requestor actor that sends requests to the calculator actor.
/// </summary>
public class RequestorActor : Actor
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestorActor"/> class.
    /// </summary>
    /// <param name="path">The actor's path.</param>
    /// <param name="dispatcher">The message dispatcher.</param>
    public RequestorActor(ActorPath path, MessageDispatcher dispatcher) : base(path)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Handles incoming messages.
    /// </summary>
    /// <param name="message">The incoming message.</param>
    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm && cm.Command == "start")
        {
            /// <summary>
            /// Sends requests to the calculator actor.
            /// </summary>
            /// <param name="cm">The control message containing the start command.</param>
            var calculations = new[]
            {
                new ControlMessage("add", new Dictionary<string, object> { { "a", 5 }, { "b", 3 } }),
                new ControlMessage("multiply", new Dictionary<string, object> { { "a", 4 }, { "b", 7 } }),
                new ControlMessage("divide", new Dictionary<string, object> { { "a", 20 }, { "b", 4 } }),
            };

            foreach (var calc in calculations)
            {
                var path = new ActorPath("/user/calculator");
                var calculatorRef = Ref.System.GetActor(path);
                if (calculatorRef != null)
                {
                    await _dispatcher.SendAsync(calculatorRef, calc);
                }
            }

            Console.WriteLine($"[{Path}] Sent calculation requests.");
        }
        await Task.CompletedTask;
    }

    private readonly MessageDispatcher _dispatcher;
}

class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Request-Response Pattern ===\n");

        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "CalculatorSystem";
        });

        var sp = services.BuildServiceProvider();
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
        var system = await config.InitializeAsync();

        Console.WriteLine("Actor system initialized.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create calculator actor
            var calcPath = new ActorPath("/user/calculator");
            var calcRef = await config.CreateActorAsync(calcPath);

            // Create requestor actor
            var reqPath = new ActorPath("/user/requestor");
            var reqRef = await config.CreateActorAsync(reqPath);

            Console.WriteLine("Actors created.\n");

            // Send start request
            var startMsg = new ControlMessage("start");
            await dispatcher.SendAsync(reqRef, startMsg);

            await Task.Delay(1000);

            // Show metrics
            var stats = await config.GetStatisticsAsync();
            Console.WriteLine($"\nStatistics:");
            Console.WriteLine($"  Messages Processed: {stats.DispatcherStats?.TotalProcessed}");
            Console.WriteLine($"  Success Rate: {stats.DispatcherStats?.SuccessRate}%");
            Console.WriteLine($"  Average Latency: {stats.DispatcherStats?.AverageLatency}ms");
        }
        finally
        {
            await system.ShutdownAsync();
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
