namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the doctrine's Bait-and-Switch / readable-deception beat
    /// (family #4, ladder rung #7): one dog (the <b>baiter</b>, usually Cheddar) feints at a decoy to
    /// pull an enemy (squirrel / coyote / vacuum) off the real prize, and ONLY while the enemy is fully
    /// committed to the decoy can the other dog (the <b>striker</b>, usually Cocoa) snatch the real
    /// target. The co-op lock is a commitment band that the baiter alone cannot hold open forever:
    ///
    ///   - bait too little and the enemy stays guarding the prize (<see cref="Committed"/> is false) so
    ///     a strike just <see cref="Whiffs"/> into the enemy's face;
    ///   - bait just enough and the enemy lunges at the decoy — the open window where the striker lands
    ///     a <see cref="Hits"/>;
    ///   - <i>hold</i> the feint at full pelt and the deception <see cref="Overbaited"/>: once the
    ///     enemy has been pinned on the decoy past the overbait tolerance it wises up (or Cheddar takes
    ///     his own bait and bolts), the commitment snaps back to zero, and it counts a
    ///     <see cref="Backfires"/>.
    ///
    /// So the baiter must <i>feather</i> the feint — commit the enemy and ease off rather than slamming
    /// it to the pin and holding — while the striker reads the opening. The window is the whole
    /// committed band (threshold..full), so it is robust to coarse frame steps; only sustained
    /// over-baiting backfires. Pure logic: a mission drives <see cref="Advance"/> from whether the
    /// baiter is currently feinting, and calls <see cref="Strike"/> when the striker reaches the real
    /// target; tests drive both deterministically. Commitment moves at one unit per second so callers
    /// can pick readable seconds-based timings.
    /// </summary>
    public sealed class CoopBaitSwitchPuzzle
    {
        private float _commitThreshold = 0.6f;
        private float _commitRate = 1f;
        private float _decayRate = 1f;
        private float _overbaitTolerance = 0.5f;
        private int _hitsNeeded = 3;
        private int _maxBackfires = 3;

        private float _overbaitTimer;

        /// <summary>How fully the enemy has lunged at the decoy (0..1). Rises while baiting, decays otherwise.</summary>
        public float Commitment { get; private set; }

        /// <summary>Real-target snatches landed inside the committed window.</summary>
        public int Hits { get; private set; }

        /// <summary>Strikes attempted while the enemy was still guarding (not committed). Recoverable, informative.</summary>
        public int Whiffs { get; private set; }

        /// <summary>Times the bait was overplayed and the enemy snapped back / Cheddar took his own bait.</summary>
        public int Backfires { get; private set; }

        /// <summary>The enemy is lunging at the decoy: the open window for a strike (threshold up to full).</summary>
        public bool Committed => Commitment >= _commitThreshold;

        /// <summary>The bait is pinned at full pelt: hold it here past the tolerance and the enemy wises up.</summary>
        public bool Overbaited => Commitment >= 1f;

        public bool Solved => Hits >= _hitsNeeded;
        public bool TooManyBackfires => Backfires >= _maxBackfires;

        public int HitsNeeded => _hitsNeeded;
        public float CommitThreshold => _commitThreshold;

        public void Configure(float commitThreshold, float commitRate, float decayRate,
            float overbaitTolerance, int hitsNeeded, int maxBackfires)
        {
            _commitThreshold = commitThreshold <= 0f ? 0.1f : (commitThreshold >= 1f ? 0.9f : commitThreshold);
            _commitRate = commitRate <= 0f ? 1f : commitRate;
            _decayRate = decayRate <= 0f ? 1f : decayRate;
            _overbaitTolerance = overbaitTolerance <= 0f ? 0.01f : overbaitTolerance;
            _hitsNeeded = hitsNeeded < 1 ? 1 : hitsNeeded;
            _maxBackfires = maxBackfires < 1 ? 1 : maxBackfires;
            Reset();
        }

        /// <summary>
        /// Advance the beat by <paramref name="dt"/> seconds given whether the baiter is currently
        /// feinting at the decoy. Holding commitment pinned at full past the overbait tolerance backfires
        /// once and snaps the enemy back to guarding the prize.
        /// </summary>
        public void Advance(float dt, bool baiting)
        {
            if (Solved || dt <= 0f) return;

            Commitment += baiting ? _commitRate * dt : -_decayRate * dt;
            if (Commitment < 0f) Commitment = 0f;
            else if (Commitment > 1f) Commitment = 1f;

            if (Commitment >= 1f)
            {
                _overbaitTimer += dt;
                if (_overbaitTimer >= _overbaitTolerance)
                {
                    // Overplayed: the enemy realizes the decoy is fake (or Cheddar bolts after it himself).
                    Backfires++;
                    Commitment = 0f;     // window slams shut; enemy back on the prize
                    _overbaitTimer = 0f;
                }
            }
            else
            {
                _overbaitTimer = 0f; // eased off the pin in time
            }
        }

        /// <summary>The striker swings at the real target. Lands only while the enemy is committed to the decoy.</summary>
        public void Strike()
        {
            if (Solved) return;

            if (Committed) Hits++;
            else Whiffs++; // enemy was still guarding the prize
        }

        public void Reset()
        {
            Commitment = 0f;
            Hits = 0;
            Whiffs = 0;
            Backfires = 0;
            _overbaitTimer = 0f;
        }
    }
}
