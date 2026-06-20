namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive for the doctrine's Rescue-as-puzzle beat (family #8) built on
    /// Dual-timing (family #7): a grabbed dog can't free itself by being barked at — instead the
    /// <b>held</b> dog wiggles/leans to briefly weaken the captor's grip, and only the <b>free</b> dog
    /// can land the rescue pull, and only while that weakness window is open.
    ///
    /// The co-op lock is split timing: the held dog alone controls when a window opens (only it can
    /// wiggle), the free dog alone can pull, and a pull outside the window just misses (a recoverable
    /// <see cref="MissedPulls"/>, not a permanent fail). Enough well-timed pulls free the dog.
    ///
    /// Pure logic so a mission drives wiggle/pull from inputs and the window from time, while tests
    /// drive it deterministically.
    /// </summary>
    public sealed class CoopRescueTimingPuzzle
    {
        private int _pullsNeeded = 3;
        private float _windowDuration = 1f;

        /// <summary>Well-timed rescue pulls landed inside an open window.</summary>
        public int Pulls { get; private set; }

        /// <summary>Pulls attempted with no open window (mistimed).</summary>
        public int MissedPulls { get; private set; }

        /// <summary>Seconds left on the current weakness window (0 = grip is tight again).</summary>
        public float WindowRemaining { get; private set; }

        public bool WindowOpen => WindowRemaining > 0f;
        public bool Freed => Pulls >= _pullsNeeded;
        public int PullsNeeded => _pullsNeeded;

        public void Configure(int pullsNeeded, float windowDuration)
        {
            _pullsNeeded = pullsNeeded < 1 ? 1 : pullsNeeded;
            _windowDuration = windowDuration <= 0f ? 1f : windowDuration;
            Reset();
        }

        /// <summary>The held dog wiggles, opening a brief weakness window for the free dog to use.</summary>
        public void Wiggle()
        {
            if (Freed) return;
            WindowRemaining = _windowDuration;
        }

        /// <summary>The free dog pulls. Counts only if a weakness window is currently open.</summary>
        public void Pull()
        {
            if (Freed) return;

            if (WindowOpen)
            {
                Pulls++;
                WindowRemaining = 0f; // the window is spent on a good pull
            }
            else
            {
                MissedPulls++;
            }
        }

        /// <summary>Advance time; the weakness window closes as the captor re-tightens its grip.</summary>
        public void Advance(float dt)
        {
            if (Freed || dt <= 0f || WindowRemaining <= 0f) return;
            WindowRemaining -= dt;
            if (WindowRemaining < 0f) WindowRemaining = 0f;
        }

        public void Reset()
        {
            Pulls = 0;
            MissedPulls = 0;
            WindowRemaining = 0f;
        }
    }
}
