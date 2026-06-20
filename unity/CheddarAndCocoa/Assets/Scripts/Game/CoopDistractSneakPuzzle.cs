namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the doctrine's Distract-and-Sneak beat (family #2): one dog
    /// (the <b>distractor</b>, usually Cheddar) pulls an enemy/human's attention while the other (the
    /// <b>sneaker</b>, usually Cocoa) slips past in segments. The co-op lock is a squeeze between two
    /// opposing pressures, so it is NOT just "hold a button":
    ///
    ///   - stop distracting too long and the enemy's <see cref="Watchfulness"/> drifts back toward the
    ///     sneak lane;
    ///   - distract too long and the enemy's <see cref="Annoyance"/> builds until it turns on the
    ///     distractor and catches the sneaker anyway;
    ///   - the sneaker only advances while it is sneaking AND the distractor is engaging AND the enemy
    ///     is not yet alert.
    ///
    /// Progress banks at <see cref="Segment"/> checkpoints — getting spotted only knocks the sneaker
    /// back to the last checkpoint (a funny, partial, recoverable failure), not all the way to the
    /// start. Pure logic so missions drive it from real positions and tests drive it deterministically.
    /// Meters move at one unit per second so callers can pick readable seconds-based timings.
    /// </summary>
    public sealed class CoopDistractSneakPuzzle
    {
        private int _segments = 3;
        private float _segmentTime = 1f;

        /// <summary>Checkpoints banked so far. The sneaker is safe at a checkpoint.</summary>
        public int Segment { get; private set; }

        /// <summary>Progress into the current (unbanked) segment, in seconds (0..segmentTime).</summary>
        public float SegmentProgress { get; private set; }

        /// <summary>How much the enemy is looking back toward the sneak lane (0..1). Rises when not distracted.</summary>
        public float Watchfulness { get; private set; }

        /// <summary>How fed up the enemy is with the distractor (0..1). Rises while distracted.</summary>
        public float Annoyance { get; private set; }

        /// <summary>Times the sneaker was caught mid-segment and knocked back to the last checkpoint.</summary>
        public int Spotted { get; private set; }

        public bool Solved => Segment >= _segments;

        /// <summary>The enemy is alert (about to catch an exposed sneaker) from either pressure.</summary>
        public bool EnemyAlert => Watchfulness >= 1f || Annoyance >= 1f;

        /// <summary>The sneaker is mid-segment (exposed) rather than safe at a checkpoint.</summary>
        public bool Exposed => SegmentProgress > 0f;

        public int RequiredSegments => _segments;

        public void Configure(int segments, float segmentTime)
        {
            _segments = segments < 1 ? 1 : segments;
            _segmentTime = segmentTime <= 0f ? 1f : segmentTime;
            Reset();
        }

        /// <summary>
        /// Advance the beat by <paramref name="dt"/> seconds given whether the distractor is currently
        /// engaging the enemy and whether the sneaker is moving through the lane.
        /// </summary>
        public void Advance(float dt, bool distracting, bool sneaking)
        {
            if (Solved || dt <= 0f) return;

            Watchfulness = Clamp01(Watchfulness + (distracting ? -dt : dt));
            Annoyance = Clamp01(Annoyance + (distracting ? dt : -dt));

            if (EnemyAlert && Exposed)
            {
                // Caught in the open: back to the last checkpoint, enemy momentarily resets.
                Spotted++;
                SegmentProgress = 0f;
                Watchfulness = 0f;
                Annoyance = 0f;
                return;
            }

            if (!EnemyAlert && sneaking && distracting)
            {
                SegmentProgress += dt;
                if (SegmentProgress >= _segmentTime)
                {
                    Segment++;
                    SegmentProgress = 0f; // banked at the checkpoint
                }
            }
        }

        public void Reset()
        {
            Segment = 0;
            SegmentProgress = 0f;
            Watchfulness = 0f;
            Annoyance = 0f;
            Spotted = 0;
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
