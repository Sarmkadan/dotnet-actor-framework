// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that logs all message processing activity using the standard
/// <see cref="Microsoft.Extensions.Logging.ILogger"/> infrastructure.
/// Logs message type, actor path, and elapsed processing time at configurable levels.
/// Exceptions are always logged at <see cref="LogLevel.Error"/> regardless of the configured level.
/// </summary>
/// <remarks>
/// Runs at <c>Order = 0</c> (first by default) so that all messages — including those
/// blocked by later middleware — are visible in the log.
/// </remarks>
public class LoggingMiddleware : IActorMiddleware
{
    /// <summary>
    /// Name of the middleware for identification and logging.
    /// </summary>
    public string Name => "LoggingMiddleware";

    /// <summary>
    /// Order in which this middleware executes relative to others (lower = earlier).
    /// Always <c>0</c> so logging wraps the pipeline from the outermost position.
    /// </summary>
    public int Order => 0;

    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly LogLevel _logLevel;

    /// <summary>
    /// Initializes a new instance of <see cref="LoggingMiddleware"/>.
    /// </summary>
    /// <param name="logger">The logger used to write processing events.</param>
    /// <param name="logLevel">The level at which successful processing events are logged. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is <c>null</c>.</exception>
    public LoggingMiddleware(ILogger<LoggingMiddleware> logger, LogLevel logLevel = LogLevel.Information)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logLevel = logLevel;
    }

    /// <summary>
    /// Logs the start and completion (or failure) of message processing, then continues the pipeline.
    /// </summary>
    /// <param name="envelope">The envelope containing the message and addressing information.</param>
    /// <param name="next">Delegate that passes control to the next middleware or the final actor handler.</param>
    /// <returns>
    /// <c>true</c> when processing completed successfully; <c>false</c> when an exception was thrown
    /// (after it has been logged at <see cref="LogLevel.Error"/>).
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="envelope"/> is <c>null</c>.</exception>
    public async Task<bool> InvokeAsync(Envelope envelope, Func<Envelope, Task> next)
    {
        if (envelope == null)
            throw new ArgumentNullException(nameof(envelope));

        var message = envelope.Message;
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.Log(
                _logLevel,
                "Processing {MessageType} (Id={MessageId}) for actor {ActorPath}",
                message.GetType().Name,
                message.MessageId.ToString("N")[..8],
                envelope.Recipient.Path);

            await next(envelope);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.Log(
                _logLevel,
                "Completed {MessageType} for {ActorPath} in {ElapsedMs}ms",
                message.GetType().Name,
                envelope.Recipient.Path,
                elapsed.TotalMilliseconds);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "Error processing {MessageType} (Id={MessageId}) for {ActorPath} after {ElapsedMs}ms",
                message.GetType().Name,
                message.MessageId.ToString("N")[..8],
                envelope.Recipient.Path,
                elapsed.TotalMilliseconds);

            return false;
        }
    }
}
