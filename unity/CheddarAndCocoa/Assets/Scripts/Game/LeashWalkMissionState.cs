namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Deterministic state for the Walkies on the Leash mission: the two dogs share a leash and must
    /// reach a sequence of checkpoints together without overstretching it. Tracks how many
    /// checkpoints have been reached and how many times the leash snapped taut (drifted too far
    /// apart). Spatial proximity lives on the GameManager; this holds the progress/penalty counts.
    /// </summary>
    public sealed class LeashWalkMissionState
    {
        public int RequiredCheckpoints { get; private set; }
        public int Reached { get; private set; }
        public int Snaps { get; private set; }

        public bool ReadyToClear() => RequiredCheckpoints > 0 && Reached >= RequiredCheckpoints;
        public bool TooManySnaps(int max) => Snaps >= max;
        public int CheckpointIndex => Reached; // the next checkpoint to reach

        public void Configure(int requiredCheckpoints)
        {
            RequiredCheckpoints = requiredCheckpoints < 1 ? 1 : requiredCheckpoints;
            Reached = 0;
            Snaps = 0;
        }

        public void ReachCheckpoint()
        {
            if (Reached < RequiredCheckpoints) Reached++;
        }

        public void Snap() => Snaps++;

        public void Reset()
        {
            Reached = 0;
            Snaps = 0;
        }
    }
}
