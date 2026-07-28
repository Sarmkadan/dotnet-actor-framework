using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetActorFramework.Models;
using DotNetActorFramework.Persistence;
using Xunit;

namespace DotNetActorFramework.Tests;

/// <summary>
/// Unit tests for <see cref="InMemoryEventJournal"/> covering ordering, empty reads,
/// concurrency safety and offset handling.
/// </summary>
public sealed class InMemoryEventJournalTests
{
    [Fact]
    public void Append_Read_ReturnsEventsInInsertionOrder()
    {
        // Arrange
        var journal = new InMemoryEventJournal();
        var pid = Guid.NewGuid();

        // Act
        for (int i = 0; i < 5; i++)
            journal.Append(pid, new SequenceNr(i));

        var read = journal.Read(pid);

        // Assert
        var expected = Enumerable.Range(0, 5).Select(i => new SequenceNr(i)).ToArray();
        Assert.Equal(expected.Length, read.Count);
        Assert.Equal(expected, read);
    }

    [Fact]
    public void Read_UnknownPersistenceId_ReturnsEmptySequence()
    {
        // Arrange
        var journal = new InMemoryEventJournal();
        var unknownPid = Guid.NewGuid();

        // Act
        var read = journal.Read(unknownPid);

        // Assert
        Assert.Empty(read);
    }

    [Fact]
    public void Concurrent_Appends_DoNotLoseOrInterleaveEvents()
    {
        // Arrange
        var journal = new InMemoryEventJournal();
        var pid = Guid.NewGuid();
        const int total = 10_000;

        // Act
        Parallel.For(0, total, i => journal.Append(pid, new SequenceNr(i)));

        var read = journal.Read(pid);

        // Assert
        Assert.Equal(total, read.Count);
        var ordered = read.OrderBy(sn => sn.Value).Select(sn => sn.Value).ToArray();
        var expected = Enumerable.Range(0, total).Select(i => (long)i).ToArray();
        Assert.Equal(expected, ordered);
    }

    [Theory]
    [InlineData(-1)]
    public void Read_NegativeOffset_ThrowsArgumentOutOfRangeException(long offset)
    {
        // Arrange
        var journal = new InMemoryEventJournal();
        var pid = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => journal.Read(pid, offset));
    }

    [Fact]
    public void Read_OffsetBeyondLast_ReturnsEmpty()
    {
        // Arrange
        var journal = new InMemoryEventJournal();
        var pid = Guid.NewGuid();
        journal.Append(pid, new SequenceNr(0));
        journal.Append(pid, new SequenceNr(1));

        // Act
        var read = journal.Read(pid, fromSequenceNr: 10);

        // Assert
        Assert.Empty(read);
    }
}
