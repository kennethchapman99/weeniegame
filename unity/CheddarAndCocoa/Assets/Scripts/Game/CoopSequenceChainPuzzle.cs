namespace CheddarAndCocoa.Game
{
    /// <summary>Which dog must perform a given chain step (soft asymmetry: Either accepts both).</summary>
    public enum ChainActor
    {
        Either,
        Cheddar,
        Cocoa
    }

    /// <summary>
    /// Reusable co-op puzzle primitive for the doctrine's Sequential cause/effect beat (family #4) with
    /// built-in Role reversal (family #5): an ordered "contraption" chain where each step is owned by a
    /// dog and can only happen after the previous step. Alternating owners create the co-op lock — e.g.
    /// Cheddar paws the latch, then Cocoa shoulders the gate, then Cheddar darts through — so neither
    /// dog can rush the whole chain alone.
    ///
    /// Failures are recoverable, not silent: doing a step with the wrong dog or out of order is a
    /// harmless <see cref="Fumbles"/> (nothing moves), and dawdling past the settle window lets the
    /// contraption ease back one step (<see cref="Settles"/>) so the team has to keep pace together.
    ///
    /// Pure logic so a mission drives <see cref="TryStep"/> from interacts and <see cref="Advance"/>
    /// from time, while tests drive both deterministically.
    /// </summary>
    public sealed class CoopSequenceChainPuzzle
    {
        private ChainActor[] _owners = System.Array.Empty<ChainActor>();
        private float _settleTime;

        /// <summary>Number of chain steps completed so far.</summary>
        public int Step { get; private set; }

        /// <summary>Wrong-dog / out-of-order attempts (recoverable, no progress).</summary>
        public int Fumbles { get; private set; }

        /// <summary>Times the chain eased back a step because the team dawdled past the settle window.</summary>
        public int Settles { get; private set; }

        private float _idle;

        public int StepCount => _owners.Length;
        public bool Solved => _owners.Length > 0 && Step >= _owners.Length;
        public ChainActor NextOwner => Solved || _owners.Length == 0 ? ChainActor.Either : _owners[Step];

        /// <summary>
        /// Set up the chain. <paramref name="settleTime"/> &lt;= 0 disables the dawdle regression.
        /// </summary>
        public void Configure(ChainActor[] owners, float settleTime)
        {
            _owners = owners ?? System.Array.Empty<ChainActor>();
            _settleTime = settleTime;
            Reset();
        }

        /// <summary>The given dog attempts the next step. Advances only if it owns the next step.</summary>
        public void TryStep(ChainActor actor)
        {
            if (Solved || _owners.Length == 0) return;

            ChainActor owner = _owners[Step];
            if (owner == ChainActor.Either || owner == actor)
            {
                Step++;
                _idle = 0f;
            }
            else
            {
                Fumbles++;
            }
        }

        /// <summary>Advance time; an in-progress chain eases back one step if left idle too long.</summary>
        public void Advance(float dt)
        {
            if (Solved || dt <= 0f || _settleTime <= 0f) return;

            _idle += dt;
            if (Step > 0 && _idle >= _settleTime)
            {
                Step--;
                Settles++;
                _idle = 0f;
            }
        }

        public void Reset()
        {
            Step = 0;
            Fumbles = 0;
            Settles = 0;
            _idle = 0f;
        }
    }
}
