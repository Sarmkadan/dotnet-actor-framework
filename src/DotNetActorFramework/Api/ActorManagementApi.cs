// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Services;
using DotNetActorFramework.Utilities;

namespace DotNetActorFramework.Api;

/// <summary>
/// API handler for actor management operations.
/// Provides methods for querying and controlling actors via a programmatic interface.
/// </summary>
public class ActorManagementApi
{
    private readonly ActorSystem _actorSystem;
    private readonly MailboxService? _mailboxService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorManagementApi"/> class.
    /// </summary>
    /// <param name="actorSystem">The actor system whose actors are queried and managed. Must not be <c>null</c>.</param>
    /// <param name="mailboxService">
    /// Optional mailbox service used to report live mailbox depth in <see cref="GetActorMetrics"/>.
    /// When <c>null</c>, the mailbox depth is not overridden.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorSystem"/> is <c>null</c>.</exception>
    public ActorManagementApi(ActorSystem actorSystem, MailboxService? mailboxService = null)
    {
        _actorSystem = actorSystem ?? throw new ArgumentNullException(nameof(actorSystem));
        _mailboxService = mailboxService;
    }

    /// <summary>
    /// Gets information about a specific actor.
    /// </summary>
    public ActorInfo? GetActor(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var actorPath = new ActorPath(path);
        var actorRef = _actorSystem.GetActorRef(actorPath);

        if (actorRef == null)
            return null;

        return new ActorInfo
        {
            Path = path,
            Id = actorRef.Id,
            IsAlive = actorRef.IsAlive,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Lists all actors in the system.
    /// </summary>
    public ActorListResponse ListActors(int? limit = null, int offset = 0)
    {
        var allActors = _actorSystem.GetAllActors();
        var total = allActors.Count;
        var take = limit ?? 100;

        var actors = allActors
            .Skip(offset)
            .Take(take)
            .Select(a => new ActorInfo
            {
                Path = a.Path.ToString(),
                Id = a.Id,
                IsAlive = a.IsAlive
            })
            .ToList();

        return new ActorListResponse
        {
            Actors = actors,
            Total = total,
            Limit = take,
            Offset = offset
        };
    }

    /// <summary>
    /// Lists actors by parent path.
    /// </summary>
    public ActorListResponse ListActorsByParent(string parentPath)
    {
        if (string.IsNullOrWhiteSpace(parentPath))
            return new ActorListResponse();

        var parentActorPath = new ActorPath(parentPath);
        var children = _actorSystem.GetActorsByParent(parentActorPath);

        var actors = children
            .Select(a => new ActorInfo
            {
                Path = a.Path.ToString(),
                Id = a.Id,
                IsAlive = a.IsAlive
            })
            .ToList();

        return new ActorListResponse
        {
            Actors = actors,
            Total = actors.Count
        };
    }

    /// <summary>
    /// Terminates an actor.
    /// </summary>
    public async Task<ApiResponse> TerminateActorAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return new ApiResponse { Success = false, Message = "Path cannot be empty." };

        try
        {
            var actorPath = new ActorPath(path);
            var actorRef = _actorSystem.GetActorRef(actorPath);

            if (actorRef == null)
                return new ApiResponse { Success = false, Message = "Actor not found." };

            await _actorSystem.TerminateActorAsync(actorRef);
            return new ApiResponse { Success = true, Message = "Actor terminated successfully." };
        }
        catch (Exception ex)
        {
            return new ApiResponse { Success = false, Message = $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Gets all actors in error state.
    /// </summary>
    public ActorListResponse GetErrorActors()
    {
        var errorActors = _actorSystem.GetErrorActors();

        var actors = errorActors
            .Select(a => new ActorInfo
            {
                Path = a.Path.ToString(),
                Id = a.Id,
                IsAlive = a.IsAlive
            })
            .ToList();

        return new ActorListResponse
        {
            Actors = actors,
            Total = actors.Count
        };
    }

    /// <summary>
    /// Gets the total number of actors.
    /// </summary>
    public int GetActorCount() => _actorSystem.GetActorCount();

    /// <summary>
    /// Gets the metrics snapshot for a specific actor, including live mailbox depth.
    /// </summary>
    /// <param name="path">The actor's path string (e.g. <c>/user/orders/processor</c>).</param>
    /// <returns>
    /// An <see cref="ActorMetricsSummary"/> with up-to-date <c>MailboxDepth</c> and
    /// <c>ProcessedCount</c>, or <c>null</c> if no actor is registered at <paramref name="path"/>.
    /// </returns>
    public ActorMetricsSummary? GetActorMetrics(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        var actorPath = new ActorPath(path);
        var summary = _actorSystem.GetActorMetricsSummary(actorPath);

        if (summary == null)
            return null;

        var actorRef = _actorSystem.GetActorRef(actorPath);
        if (actorRef != null && _mailboxService != null)
        {
            var depth = _mailboxService.GetMailboxSize(actorRef.Id);
            summary.MailboxDepth = depth;
        }

        return summary;
    }
}

/// <summary>
/// API response for management operations.
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets a human-readable message describing the outcome of the operation.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Gets or sets optional payload data associated with the response. <c>null</c> when no data is returned.
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// Information about a single actor.
/// </summary>
public class ActorInfo
{
    /// <summary>
    /// Gets or sets the actor's path (e.g. <c>/user/orders/processor</c>).
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// Gets or sets the unique identifier of the actor.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the actor is currently alive.
    /// </summary>
    public bool IsAlive { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp associated with the actor information.
    /// Set to the time of the query by <see cref="ActorManagementApi.GetActor"/>; not populated by the list methods.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response for listing actors.
/// </summary>
public class ActorListResponse
{
    /// <summary>
    /// Gets or sets the actors included in this response.
    /// </summary>
    public List<ActorInfo> Actors { get; set; } = [];

    /// <summary>
    /// Gets or sets the total number of matching actors. For <see cref="ActorManagementApi.ListActors"/>
    /// this is the number of all actors in the system, not just the returned page.
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of actors requested for the page. Set only by <see cref="ActorManagementApi.ListActors"/>.
    /// </summary>
    public int Limit { get; set; }

    /// <summary>
    /// Gets or sets the number of actors skipped before the page. Set only by <see cref="ActorManagementApi.ListActors"/>.
    /// </summary>
    public int Offset { get; set; }
}
