using System.Collections.Generic;

namespace CheddarAndCocoa.Game
{
    public sealed class PlaytestEventLog
    {
        private const int MaxEntries = 200;
        private readonly List<string> _entries = new();
        private int _nextSequence = 1;

        public IReadOnlyList<string> Entries => _entries;
        public int Count => _entries.Count;
        public string LastEvent => _entries.Count == 0 ? "No playtest events yet." : _entries[_entries.Count - 1];

        public void Clear()
        {
            _entries.Clear();
            _nextSequence = 1;
        }

        public void Add(string kind, string detail)
        {
            string line = $"{_nextSequence:000} {kind}: {detail}";
            _nextSequence++;
            _entries.Add(line);
            if (_entries.Count > MaxEntries) _entries.RemoveAt(0);
        }
    }
}
