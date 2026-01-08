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
        public static WebhookConfig Activate(this WebhookConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.IsActive = true;
            return config;
        }

        /// <summary>
        /// Deactivates the webhook and returns the same instance for fluent chaining.
        /// </summary>
        public static WebhookConfig Deactivate(this WebhookConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.IsActive = false;
            return config;
        }

        /// <summary>
        /// Configures the retry policy for the webhook.
        /// </summary>
        /// <param name="maxRetries">Maximum number of retry attempts.</param>
        /// <param name="retryDelay">Delay between retry attempts.</param>
        /// <returns>The same <see cref="WebhookConfig"/> instance for fluent chaining.</returns>
        public static WebhookConfig WithRetryPolicy(this WebhookConfig config, int maxRetries, TimeSpan retryDelay)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (maxRetries < 0) throw new ArgumentOutOfRangeException(nameof(maxRetries), "MaxRetries cannot be negative.");
            if (retryDelay < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(retryDelay), "RetryDelay cannot be negative.");

            config.MaxRetries = maxRetries;
            config.RetryDelay = retryDelay;
            return config;
        }

        /// <summary>
        /// Returns the amount of time that has elapsed since the webhook was created.
        /// </summary>
        public static TimeSpan GetAge(this WebhookConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            return DateTime.UtcNow - config.CreatedAt;
        }
    }
}
