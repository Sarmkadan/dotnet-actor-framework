using System;

namespace DotNetActorFramework.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="ConfigurationException"/> to identify and categorize specific configuration exception types.
    /// </summary>
    public static class ConfigurationExceptionExtensions
    {
        /// <summary>
        /// Determines whether the specified exception is an <see cref="ActorSystemConfigurationException"/>.
        /// </summary>
        /// <param name="exception">The exception to check. Cannot be null.</param>
        /// <returns><see langword="true"/> if the exception is an <see cref="ActorSystemConfigurationException"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static bool IsActorSystemConfigurationException(this ConfigurationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return exception is ActorSystemConfigurationException;
        }

        /// <summary>
        /// Determines whether the specified exception is a <see cref="MailboxConfigurationException"/>.
        /// </summary>
        /// <param name="exception">The exception to check. Cannot be null.</param>
        /// <returns><see langword="true"/> if the exception is a <see cref="MailboxConfigurationException"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static bool IsMailboxConfigurationException(this ConfigurationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return exception is MailboxConfigurationException;
        }

        /// <summary>
        /// Determines whether the specified exception is a <see cref="PersistenceConfigurationException"/>.
        /// </summary>
        /// <param name="exception">The exception to check. Cannot be null.</param>
        /// <returns><see langword="true"/> if the exception is a <see cref="PersistenceConfigurationException"/>; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static bool IsPersistenceConfigurationException(this ConfigurationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);
            return exception is PersistenceConfigurationException;
        }

        /// <summary>
        /// Gets a human-readable string representing the type of configuration exception.
        /// </summary>
        /// <param name="exception">The exception to categorize. Cannot be null.</param>
        /// <returns>A string representing the configuration type ("Actor System", "Mailbox", "Persistence", or "Unknown").</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
        public static string GetConfigurationType(this ConfigurationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception switch
            {
                ActorSystemConfigurationException => "Actor System",
                MailboxConfigurationException => "Mailbox",
                PersistenceConfigurationException => "Persistence",
                _ => "Unknown"
            };
        }
    }
}
