// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

// Example 4: Stateful Actor
// Demonstrates actors that maintain and manage state

public class BankAccountActor : Actor
{
    private decimal _balance = 0m;
    private List<string> _transactions = new();

    public BankAccountActor(ActorPath path) : base(path) { }

    public override async Task OnInitializeAsync()
    {
        Console.WriteLine($"[{Path}] Bank account initialized.");
        await Task.CompletedTask;
    }

    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm)
        {
            switch (cm.Command)
            {
                case "deposit":
                    {
                        var amount = (decimal)cm.Parameters["amount"];
                        _balance += amount;
                        _transactions.Add($"Deposit: +{amount}");
                        Console.WriteLine($"[{Path}] Deposited {amount}. Balance: {_balance}");
                    }
                    break;

                case "withdraw":
                    {
                        var amount = (decimal)cm.Parameters["amount"];
                        if (amount <= _balance)
                        {
                            _balance -= amount;
                            _transactions.Add($"Withdraw: -{amount}");
                            Console.WriteLine($"[{Path}] Withdrew {amount}. Balance: {_balance}");
                        }
                        else
                        {
                            Console.WriteLine($"[{Path}] Insufficient funds for withdrawal of {amount}");
                        }
                    }
                    break;

                case "get-balance":
                    Console.WriteLine($"[{Path}] Current balance: {_balance}");
                    break;

                case "get-statement":
                    Console.WriteLine($"[{Path}] Account Statement:");
                    foreach (var tx in _transactions)
                    {
                        Console.WriteLine($"  {tx}");
                    }
                    Console.WriteLine($"  Final Balance: {_balance}");
                    break;

                default:
                    throw new InvalidOperationException($"Unknown command: {cm.Command}");
            }
        }
        await Task.CompletedTask;
    }

    public override async Task OnStopAsync()
    {
        Console.WriteLine($"[{Path}] Account closed. Final balance: {_balance}");
        await Task.CompletedTask;
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== DotNet Actor Framework - Stateful Actor Example ===\n");

        var services = new ServiceCollection();
        services.AddActorFramework(options =>
        {
            options.SystemName = "BankingSystem";
        });

        var sp = services.BuildServiceProvider();
        var config = ActivatorUtilities.CreateInstance<ActorSystemConfiguration>(sp);
        var system = await config.InitializeAsync().ConfigureAwait(false);

        Console.WriteLine("Banking system initialized.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create account
            var accountPath = new ActorPath("/user/account-001");
            var accountRef = await config.CreateActorAsync(accountPath).ConfigureAwait(false);

            Console.WriteLine("Account created.\n");

            // Perform transactions
            var operations = new[]
            {
                new ControlMessage("deposit", new Dictionary<string, object> { { "amount", 1000m } }),
                new ControlMessage("withdraw", new Dictionary<string, object> { { "amount", 250m } }),
                new ControlMessage("deposit", new Dictionary<string, object> { { "amount", 500m } }),
                new ControlMessage("withdraw", new Dictionary<string, object> { { "amount", 100m } }),
                new ControlMessage("get-statement"),
            };

            foreach (var op in operations)
            {
                await dispatcher.SendAsync(accountRef, op).ConfigureAwait(false);
            }

            await Task.Delay(500).ConfigureAwait(false);

            var health = config.GetHealthSummary();
            Console.WriteLine($"\nSystem Health: {health.GetHealthPercentage()}%");
        }
        finally
        {
            await system.ShutdownAsync().ConfigureAwait(false);
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
