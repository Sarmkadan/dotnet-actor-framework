using System;
using System.Threading.Tasks;

namespace DotNetActorFramework.Models
{
    public static class ActorRefExtensions
    {
        /// <summary>
        /// Sends a message to this actor and waits for a response with a default timeout of 5 seconds.
        /// </summary>
        /// <param name="actorRef">The target actor reference</param>
        /// <param name="message">The message to send</param>
        /// <returns>The response from the actor, or null if no response</returns>
        public static async Task<object?> AskAsync(this ActorRef actorRef, object message)
        {
            return await actorRef.AskAsync(message, TimeSpan.FromSeconds(5));
        }

        /// <summary>
        /// Sends a message to this actor and waits for a response with the specified timeout.
        /// </summary>
        /// <param name="actorRef">The target actor reference</param>
        /// <param name="message">The message to send</param>
        /// <param name="timeout">Maximum time to wait for response</param>
        /// <returns>The response from the actor, or null if no response</returns>
        public static async Task<object?> AskWithTimeoutAsync(this ActorRef actorRef, object message, TimeSpan timeout)
        {
            return await actorRef.AskAsync(message, timeout);
        }

        /// <summary>
        /// Determines whether this actor reference points to the same actor instance as another reference.
        /// </summary>
        /// <param name="actorRef">The actor reference</param>
        /// <param name="other">The other actor reference to compare with</param>
        /// <returns>True if both references point to the same actor instance</returns>
        public static bool IsSameInstance(this ActorRef actorRef, ActorRef? other)
        {
            if (other == null)
            {
                return false;
            }

            return actorRef.Id == other.Id;
        }

        /// <summary>
        /// Gets the actor's age (time since creation).
        /// </summary>
        /// <param name="actorRef">The actor reference</param>
        /// <returns>TimeSpan representing how long the actor has been alive</returns>
        public static TimeSpan GetAge(this ActorRef actorRef)
        {
            return DateTime.UtcNow - actorRef.CreatedAt;
        }

        /// <summary>
        /// Creates a string representation of the actor reference that includes its ID and path.
        /// </summary>
        /// <param name="actorRef">The actor reference</param>
        /// <returns>Formatted string with actor information</returns>
        public static string ToDetailedString(this ActorRef actorRef)
        {
            return $"ActorRef {{ Id = {actorRef.Id}, Path = {actorRef.Path}, IsAlive = {actorRef.IsAlive} }}";
        }
    }
}