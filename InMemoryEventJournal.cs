public class InMemoryEventJournal : IEventJournal
    {
        private readonly ConcurrentDictionary<Guid, ImmutableList<SequenceNr>> _persistenceIdToSequence = new();
        private readonly object _lock = new();

        public void Append(Guid persistenceId, SequenceNr sequenceNr)
        {
            lock (_lock);
            if (_persistenceIdToSequence.TryGetValue(persistenceId, out var sequences))
            {
                sequences = sequences.Add(sequenceNr);
            }
            else
            {
                _persistenceIdToSequence[persistenceId] = new ImmutableList<SequenceNr> { sequenceNr };
            }
        }

        public void TruncateBefore(Guid persistenceId, SequenceNr sequenceNr)
        {
            lock (_lock);
            if (_persistenceIdToSequence.TryGetValue(persistenceId, out var sequences))
            {
                sequences = sequences.TakeWhile(s => s <= sequenceNr);
                _persistenceIdToSequence[persistenceId] = sequences;
            }
        }
    }