using System;
using System.Collections.Generic;

namespace DotNetActorFramework.Exceptions
{
	/// <summary>
	/// Provides extension methods for <see cref="DotnetActorFrameworkException"/> to enhance exception handling and analysis.
	/// </summary>
	public static class DotnetActorFrameworkExceptionExtensions
	{
		/// <summary>
		/// Adds contextual information to an existing framework exception by creating a new exception with the original message and context.
		/// </summary>
		/// <param name="exception">The original exception. Cannot be null.</param>
		/// <param name="contextMessage">The contextual information to add.</param>
		/// <returns>A new <see cref="DotnetActorFrameworkException"/> with enhanced error information.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
		public static DotnetActorFrameworkException WithContext(this DotnetActorFrameworkException exception, string contextMessage)
		{
			ArgumentNullException.ThrowIfNull(exception);
			ArgumentNullException.ThrowIfNull(contextMessage);

			var newMessage = $"{exception.Message} - Context: {contextMessage}";
			return DotnetActorFrameworkException.Create(newMessage, exception.InnerException);
		}

		/// <summary>
		/// Collects all nested inner exceptions into a flat list for inspection or logging.
		/// Includes both the root exception and all inner exceptions in traversal order.
		/// </summary>
		/// <param name="exception">The exception to analyze. Cannot be null.</param>
		/// <returns>A list containing the exception and all its inner exceptions.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
		public static List<Exception> GetInnerExceptions(this DotnetActorFrameworkException exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			var exceptions = new List<Exception> { exception };
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
		/// <param name="exception">The exception to check. Cannot be null.</param>
		/// <returns><see langword="true"/> if the exception or any inner exception is a <see cref="DotnetActorFrameworkException"/>; otherwise, <see langword="false"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/></exception>
		public static bool IsFrameworkException(this Exception exception)
		{
			ArgumentNullException.ThrowIfNull(exception);

			return exception switch
			{
				DotnetActorFrameworkException => true,
				_ => exception.InnerException?.IsFrameworkException() ?? false
			};
		}
	}
}