using System;

namespace DotNetActorFramework.Exceptions
{
    public static class ConfigurationExceptionExtensions
    {
        public static bool IsActorSystemConfigurationException(this ConfigurationException exception) =>
            exception is ActorSystemConfigurationException;

        public static bool IsMailboxConfigurationException(this ConfigurationException exception) =>
            exception is MailboxConfigurationException;

        public static bool IsPersistenceConfigurationException(this ConfigurationException exception) =>
            exception is PersistenceConfigurationException;

        public static string GetConfigurationType(this ConfigurationException exception)
        {
            if (exception is ActorSystemConfigurationException) return "Actor System";
            if (exception is MailboxConfigurationException) return "Mailbox";
            if (exception is PersistenceConfigurationException) return "Persistence";
            return "Unknown";
        }
    }
}
