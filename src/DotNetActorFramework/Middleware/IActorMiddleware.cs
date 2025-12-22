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
/// <remarks>
/// Middleware components are sorted by <see cref="Order"/> before execution.
/// Lower values run earlier; the final message handler runs after all middleware.
/// </remarks>
public interface IActorMiddleware
{
    /// <summary>
    /// Name of the middleware for identification and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Processes a message as it flows through the pipeline.
    /// </summary>
    /// <param name="envelope">The envelope containing the message and addressing information.</param>
    /// <param name="next">
    /// Delegate that passes control to the next middleware or the final actor handler.
    /// <strong>Call <paramref name="next"/> to continue processing.</strong>
    /// Omit the call to short-circuit the pipeline — subsequent middleware and the actor
    /// will not see the message. Return <c>false</c> when short-circuiting to indicate
    /// the message was intentionally dropped (e.g., rate-limited or rejected).
    /// </param>
    /// <returns>
    /// <c>true</c> when the message should continue or was successfully handled;
    /// <c>false</c> when the message is dropped and no further processing should occur.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
    Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next);

    /// <summary>
    /// Order in which this middleware executes relative to others (lower = earlier).
    /// Defaults to <c>0</c>. Use negative values to run before all default middleware.
    /// </summary>
    int Order => 0;
}

/// <summary>
/// Middleware pipeline for processing actor messages.
/// Middleware components are sorted by <see cref="IActorMiddleware.Order"/> (ascending)
/// and executed in that order before each message is delivered to an actor.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Execution order:</strong> middleware with a lower <c>Order</c> value always
/// runs before middleware with a higher value. When two components share the same order,
/// the one registered first runs first.
/// </para>
/// <para>
/// <strong>Thread safety:</strong> <see cref="ExecuteAsync"/> is safe to call concurrently
/// from multiple threads. <see cref="Register"/> is <em>not</em> thread-safe and should
/// only be called during application startup, before any messages are dispatched.
/// </para>
/// </remarks>
public class MiddlewarePipeline
{
    private readonly List<IActorMiddleware> _middleware = [];

    /// <summary>
    /// Registers a middleware component. The internal list is re-sorted by
    /// <see cref="IActorMiddleware.Order"/> after each registration.
    /// </summary>
    /// <param name="middleware">The middleware component to register. Must not be <c>null</c>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is <c>null</c>.</exception>
    /// <remarks>Not thread-safe — call only during startup before dispatching messages.</remarks>
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
    /// <param name="envelope">The envelope to process. Must not be <c>null</c>.</param>
    /// <param name="finalHandler">
    /// The terminal handler invoked after all middleware have passed the envelope through.
    /// Typically delivers the message to the target actor.
    /// </param>
    /// <returns>
    /// <c>true</c> if all middleware and the final handler completed without exception;
    /// <c>false</c> if any middleware short-circuited the pipeline or an exception was thrown.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
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
        Func<Task>? executeMiddleware = null;
        executeMiddleware = async () =>
        {
            if (index < _middleware.Count)
            {
                var middleware = _middleware[index++];
                await middleware.InvokeAsync(envelope, async (_) =>
                {
                    await executeMiddleware!();
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
