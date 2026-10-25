// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetActorFramework.Models;
using DotNetActorFramework.Serialization;

namespace DotNetActorFramework.Persistence;

/// <summary>
/// Interface for persisting actor state.
/// Implementations can store state in databases, files, or other backends.
/// </summary>
public interface IActorStatePersistence
{
    /// <summary>
    /// Saves the state of an actor.
    /// </summary>
    Task SaveAsync(Guid actorId, ActorPath actorPath, object state);

    /// <summary>
    /// Loads the state of an actor.
    /// </summary>
    Task<object?> LoadAsync(Guid actorId, ActorPath actorPath);

    /// <summary>
    /// Deletes the state of an actor.
    /// </summary>
    Task DeleteAsync(Guid actorId, ActorPath actorPath);

    /// <summary>
    /// Checks if state exists for an actor.
    /// </summary>
    Task<bool> ExistsAsync(Guid actorId, ActorPath actorPath);
}

/// <summary>
/// In-memory actor state persistence.
/// Useful for testing and temporary state storage.
/// </summary>
public class InMemoryActorStatePersistence : IActorStatePersistence
{
    private readonly Dictionary<string, object> _states = [];
    private readonly object _lockObject = new();

    /// <summary>
    /// Saves the state of an actor in memory. A null state is replaced with an empty object.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <param name="state">The state to store.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorPath"/> is null.</exception>
    public Task SaveAsync(Guid actorId, ActorPath actorPath, object state)
    {
        if (actorPath == null) throw new ArgumentNullException(nameof(actorPath));

        var key = $"{actorId}:{actorPath}";
        lock (_lockObject)
        {
            _states[key] = state ?? new object();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads the in-memory state of an actor.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>The stored state, or null if no state exists or <paramref name="actorPath"/> is null.</returns>
    public Task<object?> LoadAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorPath == null) return Task.FromResult((object?)null);

        var key = $"{actorId}:{actorPath}";
        lock (_lockObject)
        {
            _states.TryGetValue(key, out var state);
            return Task.FromResult(state);
        }
    }

    /// <summary>
    /// Deletes the in-memory state of an actor. Does nothing if no state exists.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A completed task.</returns>
    public Task DeleteAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorPath == null) return Task.CompletedTask;

        var key = $"{actorId}:{actorPath}";
        lock (_lockObject)
        {
            _states.Remove(key);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether in-memory state exists for an actor.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>True if state exists; otherwise false (including when <paramref name="actorPath"/> is null).</returns>
    public Task<bool> ExistsAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorPath == null) return Task.FromResult(false);

        var key = $"{actorId}:{actorPath}";
        lock (_lockObject)
        {
            return Task.FromResult(_states.ContainsKey(key));
        }
    }
}

/// <summary>
/// File-based actor state persistence.
/// Stores state as JSON files in the filesystem.
/// </summary>
public class FileActorStatePersistence : IActorStatePersistence
{
    private readonly string _basePath;
    private readonly IStateSerializer _serializer;

    /// <summary>
    /// Initializes a new instance that stores actor state under the given base directory.
    /// The directory is created if it does not exist.
    /// </summary>
    /// <param name="basePath">The root directory where state files are stored.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="basePath"/> is null, empty, or whitespace.</exception>
    public FileActorStatePersistence(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be empty.", nameof(basePath));

        _basePath = basePath;
        _serializer = new JsonStateSerializer();

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

    /// <summary>
    /// Serializes the actor state as JSON and writes it to a file under the base directory.
    /// Does nothing if <paramref name="actorPath"/> or <paramref name="state"/> is null.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor; used to build the subdirectory.</param>
    /// <param name="state">The state to serialize and store.</param>
    /// <returns>A task that completes when the file has been written.</returns>
    public async Task SaveAsync(Guid actorId, ActorPath actorPath, object state)
    {
        if (actorPath == null || state == null) return;

        var fileName = GetFileName(actorId, actorPath);
        var directoryPath = Path.GetDirectoryName(fileName);

        if (!Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath!);

        var serialized = _serializer.Serialize(state);
        await File.WriteAllBytesAsync(fileName, serialized);
    }

    /// <summary>
    /// Reads the raw state file for an actor.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>
    /// The file contents as a <c>byte[]</c>, or null if the file does not exist, <paramref name="actorPath"/> is null,
    /// or reading fails.
    /// </returns>
    public async Task<object?> LoadAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorPath == null) return null;

        var fileName = GetFileName(actorId, actorPath);
        if (!File.Exists(fileName)) return null;

        try
        {
            var data = await File.ReadAllBytesAsync(fileName);
            // Note: returning dynamic object as we don't know the type
            return data;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deletes the state file for an actor. Errors during deletion are ignored.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>A task that completes when the operation finishes.</returns>
    public async Task DeleteAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorPath == null) return;

        var fileName = GetFileName(actorId, actorPath);
        if (File.Exists(fileName))
        {
            try { File.Delete(fileName); }
            catch { /* Ignore */ }
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a state file exists for an actor.
    /// </summary>
    /// <param name="actorId">The unique identifier of the actor.</param>
    /// <param name="actorPath">The path of the actor.</param>
    /// <returns>True if the state file exists; otherwise false (including when <paramref name="actorPath"/> is null).</returns>
    public Task<bool> ExistsAsync(Guid actorId, ActorPath actorPath)
    {
        if (actorPath == null) return Task.FromResult(false);

        var fileName = GetFileName(actorId, actorPath);
        return Task.FromResult(File.Exists(fileName));
    }

    private string GetFileName(Guid actorId, ActorPath actorPath)
    {
        var relativePath = actorPath.ToString().Replace("/", Path.DirectorySeparatorChar.ToString());
        var fileName = $"{actorId:N}.json";
        return Path.Combine(_basePath, relativePath, fileName);
    }
}

/// <summary>
/// Actor state snapshot for checkpoint/recovery scenarios.
/// </summary>
public class ActorStateSnapshot
{
    /// <summary>
    /// Gets or sets the unique identifier of the actor.
    /// </summary>
    public Guid ActorId { get; set; }

    /// <summary>
    /// Gets or sets the string form of the actor's path.
    /// </summary>
    public string ActorPath { get; set; } = null!;

    /// <summary>
    /// Gets or sets the serialized state bytes.
    /// </summary>
    public byte[] StateData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the time at which the snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the version number of the snapshot.
    /// </summary>
    public long Version { get; set; }

    /// <summary>
    /// Gets a value indicating whether the snapshot has a non-empty actor path and non-empty state data.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(ActorPath) && StateData != null && StateData.Length > 0;
}
