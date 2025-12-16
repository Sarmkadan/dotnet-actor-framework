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

    public FileActorStatePersistence(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
            throw new ArgumentException("Base path cannot be empty.", nameof(basePath));

        _basePath = basePath;
        _serializer = new JsonStateSerializer();

        if (!Directory.Exists(_basePath))
            Directory.CreateDirectory(_basePath);
    }

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
    public Guid ActorId { get; set; }
    public string ActorPath { get; set; }
    public byte[] StateData { get; set; }
    public DateTime CreatedAt { get; set; }
    public long Version { get; set; }

    public bool IsValid => !string.IsNullOrEmpty(ActorPath) && StateData != null && StateData.Length > 0;
}
