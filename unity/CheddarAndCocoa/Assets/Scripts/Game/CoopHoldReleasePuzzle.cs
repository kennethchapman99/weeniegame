namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Reusable co-op puzzle primitive implementing the doctrine's Hold-and-Release beat (family #1)
    /// fused with Timed-Double-Action (family #7): one dog (the <b>anchor</b>, usually Cocoa) holds a
    /// pressure point open while the other dog (the <b>crosser</b>, usually Cheddar) makes the
    /// crossing/steal/squeeze. The co-op lock:
    ///
    ///   - the crosser only makes progress <i>while the anchor is holding</i> (you can't hold and
    ///     cross at once);
    ///   - the anchor's hold has a limited patience window, so the crosser must finish in time;
    ///   - if the anchor lets go mid-cross, the held thing SNAPS BACK and the crossing resets — a
    ///     funny, recoverable failure, not a silent score subtraction.
    ///
    /// This is pure logic (no MonoBehaviour) so missions can drive it from real dog positions while
    /// PlayMode/unit tests drive it deterministically. Roles are soft: either dog can anchor or cross;
    /// a mission picks defaults for comedy/clarity.
    /// </summary>
    public sealed class CoopHoldReleasePuzzle
    {
        private float _crossNeeded = 1f;
        private float _holdWindow = 1f;

        /// <summary>Is the anchor currently holding the pressure point?</summary>
        public bool Held { get; private set; }

        /// <summary>Seconds of anchor patience left before the hold gives out (0..holdWindow).</summary>
        public float HoldRemaining { get; private set; }

        /// <summary>Crossing progress in seconds-of-held-crossing (0..crossNeeded).</summary>
        public float CrossProgress { get; private set; }

        public bool Solved { get; private set; }

        /// <summary>How many times the hold snapped back mid-cross (the funny failure).</summary>
        public int Snaps { get; private set; }

        public float CrossRatio => _crossNeeded <= 0f ? 1f : (CrossProgress / _crossNeeded);
        public float HoldRatio => _holdWindow <= 0f ? 0f : (HoldRemaining / _holdWindow);

        /// <summary>Crosser is actively crossing and the anchor is holding it open — the live beat.</summary>
        public bool CrossingActive => Held && !Solved && CrossProgress > 0f;

        /// <summary>
        /// Sets up a beat. Because a snap resets crossing progress, the cross must complete within a
        /// single hold — so for a solvable beat callers should keep <paramref name="holdWindow"/> at
        /// least <paramref name="crossNeeded"/> (a tighter window means the only way through is for
        /// the anchor to re-grab, which is intentionally an unsolvable/comic config).
        /// </summary>
        public void Configure(float crossNeeded, float holdWindow)
        {
            _crossNeeded = crossNeeded <= 0f ? 1f : crossNeeded;
            _holdWindow = holdWindow <= 0f ? 1f : holdWindow;
            Reset();
        }

        /// <summary>The anchor engages or releases the hold. Releasing mid-cross snaps it back.</summary>
        public void SetHeld(bool held)
        {
            if (Solved) return;

            if (held && !Held)
                HoldRemaining = _holdWindow; // grabbing on (re)charges the patience window

            bool wasHeld = Held;
            Held = held;

            if (wasHeld && !held)
                ReleaseMidCross();
        }

        /// <summary>
        /// Advance the beat by <paramref name="dt"/> seconds. While the anchor holds, the hold window
        /// drains and the crosser's progress accrues; if the window runs out first, it snaps.
        /// </summary>
        public void Advance(float dt)
        {
            if (Solved || !Held || dt <= 0f) return;

            HoldRemaining -= dt;
            if (HoldRemaining <= 0f)
            {
                Snap();
                return;
            }

            CrossProgress += dt;
            if (CrossProgress >= _crossNeeded)
            {
                CrossProgress = _crossNeeded;
                Solved = true;
            }
        }

        public void Reset()
        {
            Held = false;
            HoldRemaining = 0f;
            CrossProgress = 0f;
            Solved = false;
            Snaps = 0;
        }

        private void ReleaseMidCross()
        {
            if (!Solved && CrossProgress > 0f) Snap();
            else HoldRemaining = 0f;
        }

        private void Snap()
        {
            Snaps++;
            CrossProgress = 0f;
            HoldRemaining = 0f;
            Held = false;
        }
    }
}
