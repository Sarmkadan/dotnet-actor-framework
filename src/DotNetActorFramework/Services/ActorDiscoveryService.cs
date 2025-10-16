// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetActorFramework.Models;

namespace DotNetActorFramework.Services;

/// <summary>
/// Provides capability-based actor discovery, allowing actor pools to be located
/// dynamically without coupling callers to concrete actor paths or identifiers.
/// </summary>
public sealed class ActorDiscoveryService
{
    private readonly ConcurrentDictionary<Guid, ActorDiscoveryEntry> _entries = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, bool>> _capabilityIndex
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, bool>> _tagIndex
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers an actor with one or more capabilities and optional metadata tags.
    /// Subsequent calls with the same actor overwrite the previous registration.
    /// </summary>
    /// <param name="actorRef">The actor reference to register.</param>
    /// <param name="capabilities">Non-empty capability identifiers the actor handles (case-insensitive).</param>
    /// <param name="tags">Optional metadata tags used for supplementary grouping.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="actorRef"/> or <paramref name="capabilities"/> is <c>null</c>.
    /// </exception>
    public void Register(ActorRef actorRef, IEnumerable<string> capabilities, IEnumerable<string>? tags = null)
    {
        if (actorRef == null) throw new ArgumentNullException(nameof(actorRef));
        if (capabilities == null) throw new ArgumentNullException(nameof(capabilities));

        var capabilityList = capabilities.Where(c => !string.IsNullOrWhiteSpace(c)).ToArray();
        var tagList = tags?.Where(t => !string.IsNullOrWhiteSpace(t)).ToArray() ?? Array.Empty<string>();

        // Remove stale index entries from any previous registration of this actor.
        if (_entries.TryRemove(actorRef.Id, out var previous))
            RemoveFromIndices(actorRef.Id, previous.Capabilities, previous.Tags);

        _entries[actorRef.Id] = new ActorDiscoveryEntry(actorRef, capabilityList, tagList);

        foreach (var cap in capabilityList)
            _capabilityIndex.GetOrAdd(cap, _ => new ConcurrentDictionary<Guid, bool>())[actorRef.Id] = true;

        foreach (var tag in tagList)
            _tagIndex.GetOrAdd(tag, _ => new ConcurrentDictionary<Guid, bool>())[actorRef.Id] = true;
    }

    /// <summary>
    /// Removes an actor from all capability and tag indices.
    /// </summary>
    /// <param name="actorRef">The actor reference to remove.</param>
    /// <returns><c>true</c> if the actor was found and removed; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="actorRef"/> is <c>null</c>.</exception>
    public bool Unregister(ActorRef actorRef)
    {
        if (actorRef == null) throw new ArgumentNullException(nameof(actorRef));

        if (!_entries.TryRemove(actorRef.Id, out var entry))
            return false;

        RemoveFromIndices(actorRef.Id, entry.Capabilities, entry.Tags);
        return true;
    }

    /// <summary>
    /// Returns all live actors registered under the specified capability.
    /// Dead actor references are silently filtered out.
    /// </summary>
    /// <param name="capability">The capability identifier to search (case-insensitive).</param>
    /// <returns>A read-only list of live <see cref="ActorRef"/> instances; never <c>null</c>.</returns>
    public IReadOnlyList<ActorRef> Discover(string capability)
    {
        if (string.IsNullOrWhiteSpace(capability))
            return Array.Empty<ActorRef>();

        if (!_capabilityIndex.TryGetValue(capability, out var bucket))
            return Array.Empty<ActorRef>();

        return ResolveAlive(bucket);
    }

    /// <summary>
    /// Returns all live actors that carry the given metadata tag.
    /// </summary>
    /// <param name="tag">The tag to filter by (case-insensitive).</param>
    /// <returns>A read-only list of live <see cref="ActorRef"/> instances; never <c>null</c>.</returns>
    public IReadOnlyList<ActorRef> DiscoverByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return Array.Empty<ActorRef>();

        if (!_tagIndex.TryGetValue(tag, out var bucket))
            return Array.Empty<ActorRef>();

        return ResolveAlive(bucket);
    }

    /// <summary>
    /// Returns a point-in-time snapshot of all registered discovery entries,
    /// including entries for actors that may no longer be alive.
    /// </summary>
    public IReadOnlyList<ActorDiscoveryEntry> GetAll() => _entries.Values.ToList();

    private IReadOnlyList<ActorRef> ResolveAlive(ConcurrentDictionary<Guid, bool> bucket)
    {
        var result = new List<ActorRef>(bucket.Count);
        foreach (var id in bucket.Keys)
        {
            if (_entries.TryGetValue(id, out var e) && e.ActorRef.IsAlive)
                result.Add(e.ActorRef);
        }
        return result;
    }

    private void RemoveFromIndices(Guid id, string[] capabilities, string[] tags)
    {
        foreach (var cap in capabilities)
            if (_capabilityIndex.TryGetValue(cap, out var b)) b.TryRemove(id, out _);

        foreach (var tag in tags)
            if (_tagIndex.TryGetValue(tag, out var b)) b.TryRemove(id, out _);
    }
}

/// <summary>
/// Immutable record that captures an actor's discovery registration details.
/// </summary>
/// <param name="ActorRef">The registered actor reference.</param>
/// <param name="Capabilities">Capability identifiers under which this actor is discoverable.</param>
/// <param name="Tags">Metadata tags associated with this actor.</param>
public sealed record ActorDiscoveryEntry(
    ActorRef ActorRef,
    string[] Capabilities,
    string[] Tags)
{
    /// <summary>Gets the UTC timestamp when this entry was created.</summary>
    public DateTime RegisteredAt { get; } = DateTime.UtcNow;
}
