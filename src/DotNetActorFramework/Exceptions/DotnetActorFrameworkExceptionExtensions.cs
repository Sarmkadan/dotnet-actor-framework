using System;
using System.Collections.Generic;

namespace DotNetActorFramework.Exceptions
{
    public static class DotnetActorFrameworkExceptionExtensions
    {
        /// <summary>
        /// Adds contextual information to an existing framework exception by creating a new exception with the original message and context.
        /// </summary>
        public static DotnetActorFrameworkException WithContext(this DotnetActorFrameworkException exception, string contextMessage)
        {
            var newMessage = $"{exception.Message} - Context: {contextMessage}";
            return DotnetActorFrameworkException.Create(newMessage, exception.InnerException);
        }

        /// <summary>
        /// Collects all nested inner exceptions into a flat list for inspection or logging.
        /// </summary>
        public static List<Exception> GetInnerExceptions(this DotnetActorFrameworkException exception)
        {
            var exceptions = new List<Exception>();
            var current = exception.InnerException;
            while (current != null)
            {
                exceptions.Add(current);
                current = current.InnerException;
            }
            return exceptions;
        }

        /// <summary>
        /// Checks if the provided exception or any of its inner exceptions is a DotnetActorFrameworkException.
        /// </summary>
        public static bool IsFrameworkException(Exception exception)
        {
            while (exception != null)
            {
                if (exception is DotnetActorFrameworkException)
                    return true;
                exception = exception.InnerException;
            }
            return false;
        }
    }
}
