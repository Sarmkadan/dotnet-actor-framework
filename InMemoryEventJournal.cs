// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence.Abstractions;

namespace DotNetActorFramework.Persistence;

/// <summary>
/// In‑memory implementation of <see cref="IEventJournal"/> used primarily for testing
/// and lightweight scenarios. It guarantees per‑persistence‑id ordering and is thread‑safe.
/// </summary>
public sealed class InMemoryEventJournal : IEventJournal
{
    private const int MaxEventsPerPersistenceId = 10_000; // guard against unbounded growth

    private readonly ConcurrentDictionary<Guid, ImmutableList<SequenceNr>> _store = new();
    private readonly object _lock = new();

    /// <summary>
    /// Appends a <paramref name="sequenceNr"/> for the given <paramref name="persistenceId"/>.
    /// </summary>
    /// <param name="persistenceId">The identifier of the actor (or aggregate) whose event stream is being appended to.</param>
    /// <param name="sequenceNr">The sequence number of the event to append.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the number of stored events for a single <paramref name="persistenceId"/> would exceed
    /// <see cref="MaxEventsPerPersistenceId"/>.
    /// </exception>
    public void Append(Guid persistenceId, SequenceNr sequenceNr)
    {
        // Guard clauses
        ArgumentException.ThrowIfNullOrEmpty(persistenceId.ToString());

        lock (_lock)
        {
            if (_store.TryGetValue(persistenceId, out var list))
            {
                if (list.Count >= MaxEventsPerPersistenceId)
                    throw new InvalidOperationException($"Maximum number of events ({MaxEventsPerPersistenceId}) for persistence id {persistenceId} exceeded.");

                // Preserve insertion order
                _store[persistenceId] = list.Add(sequenceNr);
            }
            else
            {
                _store[persistenceId] = ImmutableList.Create(sequenceNr);
            }
        }
    }

    /// <summary>
    /// Reads all stored sequence numbers for <paramref name="persistenceId"/> starting at
    /// <paramref name="fromSequenceNr"/> (inclusive). If <paramref name="fromSequenceNr"/> is
    /// greater than the highest stored sequence number, an empty collection is returned.
    /// </summary>
    /// <param name="persistenceId">The identifier whose events should be read.</param>
    /// <param name="fromSequenceNr">
    /// The sequence number from which to start reading (inclusive). Must be non‑negative.
    /// </param>
    /// <returns>
    /// An ordered, read‑only list of <see cref="SequenceNr"/> values. The list is empty if the
    /// <paramref name="persistenceId"/> is unknown or if <paramref name="fromSequenceNr"/> is beyond
    /// the last stored event.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fromSequenceNr"/> is negative.
    /// </exception>
    public IReadOnlyList<SequenceNr> Read(Guid persistenceId, long fromSequenceNr = 0)
    {
        if (fromSequenceNr < 0)
            throw new ArgumentOutOfRangeException(nameof(fromSequenceNr), "Offset must be non‑negative.");

        if (!_store.TryGetValue(persistenceId, out var list) || list.IsEmpty)
            return Array.Empty<SequenceNr>();

        // The underlying list is already ordered by insertion; filter by the offset.
        var result = list
            .Where(sn => sn.Value >= fromSequenceNr)
            .ToImmutableList();

        return result;
    }

    /// <summary>
    /// Truncates the event stream for <paramref name="persistenceId"/> by removing all
    /// sequence numbers less than or equal to <paramref name="sequenceNr"/>.
    /// </summary>
    /// <param name="persistenceId">The identifier whose events should be truncated.</param>
    /// <param name="sequenceNr">
    /// The sequence number up to (and including) which events will be removed.
    /// </param>
    public void TruncateBefore(Guid persistenceId, SequenceNr sequenceNr)
    {
        lock (_lock)
        {
            if (_store.TryGetValue(persistenceId, out var list))
            {
                var truncated = list.TakeWhile(sn => sn.Value > sequenceNr.Value).ToImmutableList();
                if (truncated.IsEmpty)
                    _store.TryRemove(persistenceId, out _);
                else
                    _store[persistenceId] = truncated;
            }
        }
    }
}
