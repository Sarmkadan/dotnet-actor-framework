// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Configuration;
using DotNetActorFramework.Models;

/// <summary>
/// Example 4: Stateful Actor
/// Demonstrates actors that maintain and manage state
/// </summary>
public class BankAccountActor : Actor
{
    private decimal _balance = 0m;
    private List<string> _transactions = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="BankAccountActor"/> class.
    /// </summary>
    /// <param name="path">The actor path.</param>
    public BankAccountActor(ActorPath path) : base(path) { }

    /// <summary>
    /// Called when the actor is initialized.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task OnInitializeAsync()
    {
        Console.WriteLine($"[{Path}] Bank account initialized.");
        await Task.CompletedTask;
    }

    /// <summary>
    /// Handles incoming messages.
    /// </summary>
    /// <param name="message">The message to handle.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public override async Task ReceiveAsync(Message message)
    {
        if (message is ControlMessage cm)
        {
            switch (cm.Command)
            {
                case "deposit":
                    {
                        /// <summary>
                        /// Deposits the specified amount into the account.
                        /// </summary>
                        /// <param name="amount">The amount to deposit.</param>
                        var amount = (decimal)cm.Parameters["amount"];
                        _balance += amount;
                        _transactions.Add($"Deposit: +{amount}");
                        Console.WriteLine($"[{Path}] Deposited {amount}. Balance: {_balance}");
                    }
                    break;

                case "withdraw":
                    {
                        /// <summary>
                        /// Withdraws the specified amount from the account.
                        /// </summary>
                        /// <param name="amount">The amount to withdraw.</param>
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
                    /// <summary>
                    /// Retrieves the current balance of the account.
                    /// </summary>
                    Console.WriteLine($"[{Path}] Current balance: {_balance}");
                    break;

                case "get-statement":
                    /// <summary>
                    /// Retrieves the account statement.
                    /// </summary>
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

    /// <summary>
    /// Called when the actor is stopped.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
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
        var system = await config.InitializeAsync();

        Console.WriteLine("Banking system initialized.\n");

        try
        {
            var dispatcher = sp.GetRequiredService<MessageDispatcher>();

            // Create account
            var accountPath = new ActorPath("/user/account-001");
            var accountRef = await config.CreateActorAsync(accountPath);

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
                await dispatcher.SendAsync(accountRef, op);
            }

            await Task.Delay(500);

            var health = config.GetHealthSummary();
            Console.WriteLine($"\nSystem Health: {health.GetHealthPercentage()}%");
        }
        finally
        {
            await system.ShutdownAsync();
            Console.WriteLine("\nShutdown complete.");
        }
    }
}
