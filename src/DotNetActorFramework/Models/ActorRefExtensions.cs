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
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorRef"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="message"/> is null.</exception>
        public static async Task<object?> AskAsync(this ActorRef actorRef, object message)
        {
            ArgumentNullException.ThrowIfNull(actorRef);
            ArgumentNullException.ThrowIfNull(message);

            return await actorRef.AskAsync(message, TimeSpan.FromSeconds(5));
        }


        /// <summary>
        /// Determines whether this actor reference points to the same actor instance as another reference.
        /// </summary>
        /// <param name="actorRef">The actor reference</param>
        /// <param name="other">The other actor reference to compare with</param>
        /// <returns>True if both references point to the same actor instance</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorRef"/> is null.</exception>
        public static bool IsSameInstance(this ActorRef actorRef, ActorRef? other)
        {
            ArgumentNullException.ThrowIfNull(actorRef);

            return actorRef.Id == other?.Id;
        }

        /// <summary>
        /// Gets the actor's age (time since creation).
        /// </summary>
        /// <param name="actorRef">The actor reference</param>
        /// <returns>TimeSpan representing how long the actor has been alive</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorRef"/> is null.</exception>
        public static TimeSpan GetAge(this ActorRef actorRef)
        {
            ArgumentNullException.ThrowIfNull(actorRef);

            // Calculate age from creation timestamp
            // Using DateTime.UtcNow - CreatedAt is the standard way to calculate age
            // and avoids potential issues with time zone differences
            return DateTime.UtcNow - actorRef.CreatedAt;
        }

        /// <summary>
        /// Creates a string representation of the actor reference that includes its ID and path.
        /// </summary>
        /// <param name="actorRef">The actor reference</param>
        /// <returns>Formatted string with actor information</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="actorRef"/> is null.</exception>
        public static string ToDetailedString(this ActorRef actorRef)
        {
            ArgumentNullException.ThrowIfNull(actorRef);

            return $"ActorRef {{ Id = {actorRef.Id}, Path = {actorRef.Path}, IsAlive = {actorRef.IsAlive} }}";
        }
    }
}