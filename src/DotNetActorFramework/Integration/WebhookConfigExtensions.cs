using System;

namespace DotNetActorFramework.Integration
{
    /// <summary>
    /// Extension methods that add convenient, reusable behaviour to <see cref="WebhookConfig"/>.
    /// </summary>
    public static class WebhookConfigExtensions
    {
        /// <summary>
        /// Activates the webhook and returns the same instance for fluent chaining.
        /// </summary>
        /// <param name="config">The webhook configuration to activate.</param>
        /// <returns>The same <see cref="WebhookConfig"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
        public static WebhookConfig Activate(this WebhookConfig config) =>
            config.WhenNotNull(c => c.IsActive = true);

        /// <summary>
        /// Deactivates the webhook and returns the same instance for fluent chaining.
        /// </summary>
        /// <param name="config">The webhook configuration to deactivate.</param>
        /// <returns>The same <see cref="WebhookConfig"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
        public static WebhookConfig Deactivate(this WebhookConfig config) =>
            config.WhenNotNull(c => c.IsActive = false);

        /// <summary>
        /// Configures the retry policy for the webhook.
        /// </summary>
        /// <param name="config">The webhook configuration to configure.</param>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        /// <param name="retryDelay">Delay between retry attempts.</param>
        /// <returns>The same <see cref="WebhookConfig"/> instance for fluent chaining.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxRetries"/> is negative. -or- <paramref name="retryDelay"/> is negative.</exception>
        public static WebhookConfig WithRetryPolicy(this WebhookConfig config, int maxRetries, TimeSpan retryDelay)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            if (maxRetries < 0)
                throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "MaxRetries cannot be negative.");

            if (retryDelay < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(retryDelay), retryDelay, "RetryDelay cannot be negative.");

            config.MaxRetries = maxRetries;
            config.RetryDelay = retryDelay;
            return config;
        }

        /// <summary>
        /// Returns the amount of time that has elapsed since the webhook was created.
        /// </summary>
        /// <param name="config">The webhook configuration to calculate age for.</param>
        /// <returns>The elapsed time since the webhook was created.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="config"/> is <see langword="null"/>.</exception>
        public static TimeSpan GetAge(this WebhookConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return DateTime.UtcNow - config.CreatedAt;
        }

        private static T WhenNotNull<T>(this T value, Action<T> action) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            action(value);
            return value;
        }
    }
}
