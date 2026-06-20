namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Split-information / Smell-and-act beat (doctrine #3 and
    /// #6) — the co-op upgrade for Scent Search. The lock is information asymmetry rather than timing:
    /// among several look-alike targets only one is the real buried item, and the dogs have split
    /// abilities —
    ///
    ///   - the <b>reader</b> (the dog by the scent/high ground) can <see cref="Reveal"/> which target
    ///     is real, but cannot dig it;
    ///   - the <b>digger</b> is the only one who can <see cref="ActOn"/> a target, but cannot tell the
    ///     real one apart on its own.
    ///
    /// So the digger must wait for and act on the reader's call: acting before a reveal is a blind
    /// guess that fails (<see cref="BlindActs"/>), and digging the wrong spot is a harmless decoy
    /// (<see cref="WrongDigs"/>). Each find re-buries the next item elsewhere, so the team has to relay
    /// again. Deterministic (seeded target sequence) so it tests cleanly; the digger reads the reader's
    /// call via <see cref="RevealedTarget"/>.
    /// </summary>
    public sealed class CoopScentRelayPuzzle
    {
        private int _targetCount = 4;
        private int _findsNeeded = 3;
        private int[] _sequence = System.Array.Empty<int>();

        /// <summary>Index of the target that is currently the real one (hidden from the digger until revealed).</summary>
        public int CorrectTarget { get; private set; }

        /// <summary>Whether the reader has revealed the current real target.</summary>
        public bool Known { get; private set; }

        public int Finds { get; private set; }
        public int WrongDigs { get; private set; }
        public int BlindActs { get; private set; }

        public bool Solved => Finds >= _findsNeeded;
        public int FindsNeeded => _findsNeeded;
        public int TargetCount => _targetCount;

        /// <summary>What the reader is signaling to the digger: the real target, or -1 if not revealed.</summary>
        public int RevealedTarget => Known && !Solved ? CorrectTarget : -1;

        public void Configure(int targetCount, int findsNeeded, int seed)
        {
            _targetCount = targetCount < 2 ? 2 : targetCount;
            _findsNeeded = findsNeeded < 1 ? 1 : findsNeeded;
            _sequence = new int[_findsNeeded];
            uint s = unchecked((uint)seed * 2654435761u + 1u);
            for (int i = 0; i < _findsNeeded; i++)
            {
                s = unchecked(s * 1664525u + 1013904223u);
                _sequence[i] = (int)(s % (uint)_targetCount);
            }
            Reset();
        }

        /// <summary>The reader reveals the current real target to the digger.</summary>
        public void Reveal()
        {
            if (Solved) return;
            Known = true;
        }

        /// <summary>The digger digs a target. Needs the reader's reveal first; only the real one finds.</summary>
        public void ActOn(int target)
        {
            if (Solved) return;

            if (!Known)
            {
                BlindActs++; // digging blind — can't tell the real one apart without the reader
                return;
            }

            if (target == CorrectTarget)
            {
                Finds++;
                Known = false;
                if (!Solved) CorrectTarget = _sequence[Finds];
            }
            else
            {
                WrongDigs++; // a decoy; the reader still knows, so the team can try the right spot
            }
        }

        public void Reset()
        {
            Finds = 0;
            WrongDigs = 0;
            BlindActs = 0;
            Known = false;
            CorrectTarget = _sequence.Length > 0 ? _sequence[0] : 0;
        }
    }
}
