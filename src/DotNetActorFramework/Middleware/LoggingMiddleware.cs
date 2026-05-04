// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using DotNetActorFramework.Models;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Middleware;

/// <summary>
/// Middleware that logs all message processing activity.
/// Provides visibility into message flow and helps with debugging and monitoring.
/// </summary>
public class LoggingMiddleware : IActorMiddleware
{
    public string Name => "LoggingMiddleware";
    public int Order => 0; // Runs first to capture full lifecycle

    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly LogLevel _logLevel;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger, LogLevel logLevel = LogLevel.Information)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logLevel = logLevel;
    }

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
                message.Type,
                message.Id.ToString("N")[..8],
                envelope.RecipientPath);

            await next(envelope);

            var elapsed = DateTime.UtcNow - startTime;
            _logger.Log(
                _logLevel,
                "Completed {MessageType} for {ActorPath} in {ElapsedMs}ms",
                message.Type,
                envelope.RecipientPath,
                elapsed.TotalMilliseconds);

            return true;
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startTime;
            _logger.LogError(
                ex,
                "Error processing {MessageType} (Id={MessageId}) for {ActorPath} after {ElapsedMs}ms",
                message.Type,
                message.Id.ToString("N")[..8],
                envelope.RecipientPath,
                elapsed.TotalMilliseconds);

            return false;
        }
    }
}
