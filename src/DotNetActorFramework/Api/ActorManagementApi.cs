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
    public bool Success { get; set; }
    public string Message { get; set; }
    public object? Data { get; set; }
}

/// <summary>
/// Information about a single actor.
/// </summary>
public class ActorInfo
{
    public string Path { get; set; }
    public Guid Id { get; set; }
    public bool IsAlive { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Response for listing actors.
/// </summary>
public class ActorListResponse
{
    public List<ActorInfo> Actors { get; set; } = [];
    public int Total { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
}
