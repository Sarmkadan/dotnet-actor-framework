// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Interface for middleware components that process messages in the actor pipeline.
/// Middleware can inspect, modify, or intercept messages before they're processed by actors.
/// </summary>
public interface IActorMiddleware
{
    /// <summary>
    /// Name of the middleware for identification and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Processes a message as it flows through the pipeline.
    /// The next delegate should be called to continue the pipeline.
    /// Return false to stop message processing.
    /// </summary>
    Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next);

    /// <summary>
    /// Order in which this middleware executes relative to others (lower = earlier).
    /// </summary>
    int Order => 0;
}

/// <summary>
/// Middleware pipeline for processing actor messages.
/// Middleware components are executed in order before each message is delivered to an actor.
/// </summary>
public class MiddlewarePipeline
{
    private readonly List<IActorMiddleware> _middleware = [];

    /// <summary>
    /// Registers a middleware component.
    /// </summary>
    public void Register(IActorMiddleware middleware)
    {
        if (middleware == null)
            throw new ArgumentNullException(nameof(middleware));

        _middleware.Add(middleware);
        _middleware.Sort((a, b) => a.Order.CompareTo(b.Order));
    }

    /// <summary>
    /// Executes the middleware pipeline for an envelope.
    /// </summary>
    public async Task<bool> ExecuteAsync(Envelope envelope, Func<Envelope, Task> finalHandler)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        if (_middleware.Count == 0)
        {
            await finalHandler(envelope);
            return true;
        }

        var index = 0;
        Func<Task> executeMiddleware = async () =>
        {
            if (index < _middleware.Count)
            {
                var middleware = _middleware[index++];
                await middleware.InvokeAsync(envelope, async (_) =>
                {
                    await executeMiddleware();
                });
            }
            else
            {
                await finalHandler(envelope);
            }
        };

        try
        {
            await executeMiddleware();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all registered middleware.
    /// </summary>
    public IReadOnlyList<IActorMiddleware> GetMiddleware() => _middleware.AsReadOnly();
}
