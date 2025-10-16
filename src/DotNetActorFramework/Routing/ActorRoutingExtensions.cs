// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetActorFramework.Services;

namespace DotNetActorFramework.Routing;

/// <summary>
/// Extension methods for registering actor discovery and load-based routing
/// into a dependency injection container.
/// </summary>
public static class ActorRoutingExtensions
{
    /// <summary>
    /// Registers <see cref="ActorDiscoveryService"/> as a singleton, enabling
    /// capability-based and tag-based actor lookup without load-aware routing.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    /// <example>
    /// <code>
    /// services.AddActorFramework()
    ///         .AddActorDiscovery();
    /// </code>
    /// </example>
    public static IServiceCollection AddActorDiscovery(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton<ActorDiscoveryService>();
        return services;
    }

    /// <summary>
    /// Registers both <see cref="ActorDiscoveryService"/> and <see cref="LoadBasedRouter"/>
    /// as singletons, enabling capability-based discovery with load-aware and round-robin
    /// message routing.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Requires <c>AddActorFramework()</c> to have been called first so that
    /// <see cref="DotNetActorFramework.Services.MailboxService"/> and
    /// <see cref="DotNetActorFramework.Services.MessageDispatcher"/> are available.
    /// </remarks>
    /// <example>
    /// <code>
    /// services.AddActorFramework()
    ///         .AddActorDiscoveryWithRouting();
    ///
    /// // Later, register a worker actor under the "image-resize" capability:
    /// var discovery = provider.GetRequiredService&lt;ActorDiscoveryService&gt;();
    /// discovery.Register(workerRef, capabilities: ["image-resize"], tags: ["gpu"]);
    ///
    /// // Route a message to the least-loaded worker:
    /// var router = provider.GetRequiredService&lt;LoadBasedRouter&gt;();
    /// await router.RouteAsync("image-resize", envelope);
    /// </code>
    /// </example>
    public static IServiceCollection AddActorDiscoveryWithRouting(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.AddSingleton<ActorDiscoveryService>();
        services.AddSingleton<LoadBasedRouter>();
        return services;
    }
}
