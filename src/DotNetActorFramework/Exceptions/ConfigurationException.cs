// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetActorFramework.Exceptions;

/// <summary>
/// Base exception for configuration-related errors in the actor framework.
/// </summary>
public class ConfigurationException : DotnetActorFrameworkException
{
    public ConfigurationException()
    {
    }

    public ConfigurationException(string? message) : base(message)
    {
    }

    public ConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when actor system configuration is invalid or missing required values.
/// </summary>
public class ActorSystemConfigurationException : ConfigurationException
{
    public ActorSystemConfigurationException(string? message) : base(message)
    {
    }

    public ActorSystemConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when mailbox configuration is invalid.
/// </summary>
public class MailboxConfigurationException : ConfigurationException
{
    public MailboxConfigurationException(string? message) : base(message)
    {
    }

    public MailboxConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}

/// <summary>
/// Thrown when persistence configuration is invalid.
/// </summary>
public class PersistenceConfigurationException : ConfigurationException
{
    public PersistenceConfigurationException(string? message) : base(message)
    {
    }

    public PersistenceConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}