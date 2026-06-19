namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Cooperative chaos machine beat (doctrine #10): a
    /// Rube-Goldberg contraption (drag towel → knock basket → reveal toy → open route). Unlike the
    /// manual <see cref="CoopSequenceChainPuzzle"/>, the players set up, pull the lever
    /// (<see cref="Trigger"/>), and the cascade then RUNS ITSELF through the stages — but each junction
    /// has a brief assist window where the right dog has to be in position to keep it going. Miss one
    /// and the machine MISFIRES and stalls at that exact stage (<see cref="StalledStage"/>, so players
    /// see which step failed), and a re-trigger resumes from there once the dog is back in place.
    ///
    /// The co-op magic: the cascade is unattended, so both dogs must pre-position at their junctions
    /// before pulling the lever and trust the timing. Pure logic — a mission feeds whether the active
    /// junction's helper is in position via <see cref="Advance"/>; tests drive it deterministically.
    /// </summary>
    public sealed class CoopChaosMachinePuzzle
    {
        private int _stages = 4;
        private float _windowPerStage = 1f;

        /// <summary>How many junctions the cascade has cleared (its current position).</summary>
        public int Stage { get; private set; }

        /// <summary>Is the cascade currently live (lever pulled, not yet stalled or finished)?</summary>
        public bool Running { get; private set; }

        public float StageWindowRemaining { get; private set; }

        /// <summary>The stage the machine misfired at last (-1 when running / cleanly finished).</summary>
        public int StalledStage { get; private set; } = -1;

        public int Stalls { get; private set; }

        public bool Solved => Stage >= _stages;
        public int StageCount => _stages;

        public void Configure(int stages, float windowPerStage)
        {
            _stages = stages < 1 ? 1 : stages;
            _windowPerStage = windowPerStage <= 0f ? 1f : windowPerStage;
            Reset();
        }

        /// <summary>Pull the lever: start (or resume from a stall) the cascade at the current stage.</summary>
        public void Trigger()
        {
            if (Solved || Running) return;
            Running = true;
            StalledStage = -1;
            StageWindowRemaining = _windowPerStage;
        }

        /// <summary>
        /// Advance the live cascade by <paramref name="dt"/>. <paramref name="assisting"/> is whether
        /// the current junction's helper dog is in position this step.
        /// </summary>
        public void Advance(float dt, bool assisting)
        {
            if (!Running || Solved || dt <= 0f) return;

            StageWindowRemaining -= dt;

            if (assisting)
            {
                Stage++;
                if (Solved) { Running = false; return; }
                StageWindowRemaining = _windowPerStage; // cascade rolls on to the next junction
            }
            else if (StageWindowRemaining <= 0f)
            {
                Running = false;
                StalledStage = Stage; // misfire, visibly stuck here
                Stalls++;
            }
        }

        public void Reset()
        {
            Stage = 0;
            Running = false;
            StageWindowRemaining = 0f;
            StalledStage = -1;
            Stalls = 0;
        }
    }
}
