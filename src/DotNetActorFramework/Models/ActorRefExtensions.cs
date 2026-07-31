namespace DotNetActorFramework.Models;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public static class ActorRefExtensions
{
    /// <summary>
    /// Sends a collection of messages to the actor sequentially.
    /// </summary>
    public static async Task TellAll(this ActorRef actorRef, IEnumerable<object> messages)
    {
        foreach (var message in messages)
        {
            await actorRef.SendAsync(message);
        }
    }

    /// <summary>
    /// Sends a message and waits for a response with a specified timeout and cancellation token.
    /// Returns null if the timeout is reached or an exception occurs during the ask operation.
    /// </summary>
    public static async Task<object?> AskWithTimeout(this ActorRef actorRef, object message, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await actorRef.AskAsync(message, timeout);
            if (result is Task<object?> task)
            {
                return await task;
            }
            return result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to send a message to the actor. Returns false if the actor is not alive or sending fails.
    /// </summary>
    public static async Task<bool> TryTell(this ActorRef actorRef, object message)
    {
        try
        {
            if (!actorRef.IsAlive) return false;
            await actorRef.SendAsync(message);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
