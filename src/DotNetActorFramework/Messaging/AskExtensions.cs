// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Services;

namespace DotNetActorFramework.Messaging;

/// <summary>
/// Adds the request/response ("ask") pattern to <see cref="MessageDispatcher"/>, built on top of
/// the existing <see cref="ResponseMessage"/> / <see cref="FailureMessage"/> reply types and
/// <see cref="Message.CorrelationId"/> plumbing.
/// </summary>
/// <remarks>
/// A caller sends <typeparamref name="TRequest"/> and gets back a <see cref="Task{TResult}"/> that
/// completes when a reply correlated to that request arrives, faults with
/// <see cref="AskFailedException"/> if the reply is a <see cref="FailureMessage"/>, and faults with
/// <see cref="AskTimeoutException"/> if no reply arrives before <c>timeout</c> elapses.
/// </remarks>
public static class AskExtensions
{
    /// <summary>
    /// Sends <paramref name="message"/> to <paramref name="recipient"/> without a sender identity
    /// and asynchronously waits for a correlated <see cref="ResponseMessage"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message. Must derive from <see cref="Message"/>.</typeparam>
    /// <typeparam name="TResponse">The expected type of the response payload. Must be a reference type.</typeparam>
    /// <param name="dispatcher">The dispatcher used to deliver <paramref name="message"/>.</param>
    /// <param name="recipient">The actor being asked.</param>
    /// <param name="message">The request message. Its <see cref="Message.MessageId"/> is used as the correlation key.</param>
    /// <param name="timeout">The maximum time to wait for a reply before faulting with <see cref="AskTimeoutException"/>.</param>
    /// <param name="cancellationToken">A token that cancels the wait independently of <paramref name="timeout"/>.</param>
    /// <returns>The typed response payload once a matching <see cref="ResponseMessage"/> arrives.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcher"/>, <paramref name="recipient"/>, or <paramref name="message"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="timeout"/> is not greater than zero.</exception>
    /// <exception cref="AskTimeoutException">Thrown when no reply arrives before <paramref name="timeout"/> elapses.</exception>
    /// <exception cref="AskFailedException">Thrown when the recipient replies with a <see cref="FailureMessage"/>.</exception>
    /// <exception cref="InvalidCastException">Thrown when the reply's payload is not assignable to <typeparamref name="TResponse"/>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled before a reply arrives.</exception>
    public static Task<TResponse> AskAsync<TRequest, TResponse>(
        this MessageDispatcher dispatcher,
        ActorRef recipient,
        TRequest message,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TRequest : Message
        where TResponse : class
        => AskAsync<TRequest, TResponse>(dispatcher, null, recipient, message, timeout, cancellationToken);

    /// <summary>
    /// Sends <paramref name="message"/> from <paramref name="sender"/> to <paramref name="recipient"/>
    /// and asynchronously waits for a correlated <see cref="ResponseMessage"/>.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request message. Must derive from <see cref="Message"/>.</typeparam>
    /// <typeparam name="TResponse">The expected type of the response payload. Must be a reference type.</typeparam>
    /// <param name="dispatcher">The dispatcher used to deliver <paramref name="message"/>.</param>
    /// <param name="sender">The asking actor's reference, or <c>null</c> to send without a sender identity.</param>
    /// <param name="recipient">The actor being asked.</param>
    /// <param name="message">The request message. Its <see cref="Message.MessageId"/> is used as the correlation key.</param>
    /// <param name="timeout">The maximum time to wait for a reply before faulting with <see cref="AskTimeoutException"/>.</param>
    /// <param name="cancellationToken">A token that cancels the wait independently of <paramref name="timeout"/>.</param>
    /// <returns>The typed response payload once a matching <see cref="ResponseMessage"/> arrives.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="dispatcher"/>, <paramref name="recipient"/>, or <paramref name="message"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="timeout"/> is not greater than zero.</exception>
    /// <exception cref="AskTimeoutException">Thrown when no reply arrives before <paramref name="timeout"/> elapses.</exception>
    /// <exception cref="AskFailedException">Thrown when the recipient replies with a <see cref="FailureMessage"/>.</exception>
    /// <exception cref="InvalidCastException">Thrown when the reply's payload is not assignable to <typeparamref name="TResponse"/>.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled before a reply arrives.</exception>
    public static async Task<TResponse> AskAsync<TRequest, TResponse>(
        this MessageDispatcher dispatcher,
        ActorRef? sender,
        ActorRef recipient,
        TRequest message,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
        where TRequest : Message
        where TResponse : class
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(recipient);
        ArgumentNullException.ThrowIfNull(message);
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentException("Timeout must be greater than zero.", nameof(timeout));

        var correlationId = message.MessageId;
        var completionSource = new TaskCompletionSource<Message>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!AskRegistry.TryRegisterPending(correlationId, completionSource))
            throw new InvalidOperationException($"An ask for message id '{correlationId}' is already pending.");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            if (sender is null)
                await dispatcher.SendAsync(recipient, message).ConfigureAwait(false);
            else
                await dispatcher.SendAsync(sender, recipient, message).ConfigureAwait(false);

            var reply = await completionSource.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);

            return reply switch
            {
                FailureMessage failure => throw new AskFailedException(failure.Reason, failure.ExceptionType),
                ResponseMessage<TResponse> typedResponse => typedResponse.Payload,
                ResponseMessage { IsSuccess: true, Response: TResponse payload } => payload,
                ResponseMessage { IsSuccess: false } failedResponse =>
                    throw new AskFailedException(failedResponse.ErrorMessage ?? "Ask failed without an error message.", exceptionType: null),
                ResponseMessage untyped =>
                    throw new InvalidCastException(
                        $"Response to '{typeof(TRequest).Name}' carried a payload of type '{untyped.Response?.GetType().Name ?? "null"}', which is not assignable to '{typeof(TResponse).Name}'."),
                _ => throw new InvalidCastException($"Unexpected reply type '{reply.GetType().Name}' for ask of '{typeof(TRequest).Name}'.")
            };
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AskTimeoutException(recipient.Path.ToString(), typeof(TRequest), timeout);
        }
        finally
        {
            AskRegistry.RemovePending(correlationId);
        }
    }
}
