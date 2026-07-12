namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the Feast-and-Fend beat: one dog is busy consuming a
    /// prize in stages (grab it, then shake it down bite by bite) and is defenseless while doing
    /// so; the partner must intercept incoming threats before their strike window expires. The
    /// co-op lock is the busy-hands split — the eater cannot defend and the defender cannot eat:
    ///
    ///   - a prize lands (<see cref="ChickState.Grounded"/>) and must be claimed before the
    ///     opposition airlifts it back (<see cref="AirliftUnclaimed"/>);
    ///   - the eater claims it (<see cref="Grab"/>) and works it down with repeated
    ///     <see cref="Shake"/>s while threats dive (<see cref="StartDive"/>);
    ///   - the defender must <see cref="Repel"/> an active dive before its window runs out, or
    ///     the strike lands (<see cref="Pecks"/>) and the prize escapes.
    ///
    /// Pure logic: a mission drives landings, grabs, shakes, dives, and time via
    /// <see cref="Advance"/>; tests drive all of it deterministically.
    /// </summary>
    public sealed class CoopFeastGuardPuzzle
    {
        public enum ChickState { None, Grounded, Held }

        private int _chicksNeeded = 4;
        private int _shakesNeeded = 3;
        private int _maxPecks = 3;
        private float _diveWindowSeconds = 1.6f;

        public ChickState Chick { get; private set; } = ChickState.None;
        public int ChicksEaten { get; private set; }
        public int Shakes { get; private set; }
        public int Repels { get; private set; }
        public int Pecks { get; private set; }
        public int Airlifts { get; private set; }
        public bool DiveActive { get; private set; }
        public float DiveTimeLeft { get; private set; }

        public int ChicksNeeded => _chicksNeeded;
        public int ShakesNeeded => _shakesNeeded;
        public int MaxPecks => _maxPecks;
        public bool Solved => ChicksEaten >= _chicksNeeded;
        public bool Overrun => Pecks >= _maxPecks;

        /// <summary>Rank-relevant mistakes: landed pecks plus prizes lost to hesitation.</summary>
        public int Mistakes => Pecks + Airlifts;

        public void Configure(int chicksNeeded, int shakesNeeded, int maxPecks, float diveWindowSeconds)
        {
            _chicksNeeded = chicksNeeded < 1 ? 1 : chicksNeeded;
            _shakesNeeded = shakesNeeded < 1 ? 1 : shakesNeeded;
            _maxPecks = maxPecks < 1 ? 1 : maxPecks;
            _diveWindowSeconds = diveWindowSeconds <= 0f ? 1f : diveWindowSeconds;
            Reset();
        }

        /// <summary>A prize touches down and is up for grabs. Only one is live at a time.</summary>
        public bool ChickLanded()
        {
            if (Solved || Chick != ChickState.None) return false;
            Chick = ChickState.Grounded;
            return true;
        }

        /// <summary>The eater claims the grounded prize and starts working it down.</summary>
        public bool Grab()
        {
            if (Chick != ChickState.Grounded) return false;
            Chick = ChickState.Held;
            Shakes = 0;
            return true;
        }

        /// <summary>
        /// One shake of the held prize. The shake that reaches the goal swallows it — the prize is
        /// consumed and any mid-air dive breaks off with nothing left to defend.
        /// </summary>
        public bool Shake()
        {
            if (Chick != ChickState.Held) return false;
            Shakes++;
            if (Shakes >= _shakesNeeded)
            {
                ChicksEaten++;
                Chick = ChickState.None;
                Shakes = 0;
                DiveActive = false;
                DiveTimeLeft = 0f;
            }
            return true;
        }

        /// <summary>A threat commits to a dive at the busy eater. Only one dive runs at a time.</summary>
        public bool StartDive()
        {
            if (Chick != ChickState.Held || DiveActive || Solved || Overrun) return false;
            DiveActive = true;
            DiveTimeLeft = _diveWindowSeconds;
            return true;
        }

        /// <summary>The defender drives the diving threat off before it connects.</summary>
        public bool Repel()
        {
            if (!DiveActive) return false;
            DiveActive = false;
            DiveTimeLeft = 0f;
            Repels++;
            return true;
        }

        /// <summary>The opposition reclaims a grounded prize nobody grabbed.</summary>
        public bool AirliftUnclaimed()
        {
            if (Chick != ChickState.Grounded) return false;
            Chick = ChickState.None;
            Airlifts++;
            return true;
        }

        /// <summary>Run the dive clock. An expired dive lands: a peck, and the held prize escapes.</summary>
        public void Advance(float deltaTime)
        {
            if (!DiveActive) return;
            DiveTimeLeft -= deltaTime;
            if (DiveTimeLeft > 0f) return;
            DiveActive = false;
            DiveTimeLeft = 0f;
            Pecks++;
            Chick = ChickState.None; // the shaken-loose prize flutters back to the nest
            Shakes = 0;
        }

        public void Reset()
        {
            Chick = ChickState.None;
            ChicksEaten = 0;
            Shakes = 0;
            Repels = 0;
            Pecks = 0;
            Airlifts = 0;
            DiveActive = false;
            DiveTimeLeft = 0f;
        }
    }
}
