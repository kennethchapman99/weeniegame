using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopRescueTimingPuzzle"/> from rescue inputs plus a proximity gate: the
    /// held dog's wiggle opens a weakness window, and the free dog's pull only registers when it is
    /// actually next to the held dog (so a "rescue" from across the yard does nothing). The primitive
    /// then judges the timing — a pull inside the window frees a little, a mistimed one misses.
    ///
    /// Input-event driver (the rescue counterpart to the proximity beats). Call
    /// <see cref="HeldWiggle"/> / <see cref="FreePull"/> from the dogs' inputs and let
    /// <see cref="Update"/> (or <see cref="Tick"/>) close the window over time.
    /// </summary>
    public sealed class CoopRescueTimingBeat : MonoBehaviour
    {
        private Transform _heldDog;
        private Transform _freeDog;
        private float _rescueRange = 2f;

        private readonly CoopRescueTimingPuzzle _puzzle = new CoopRescueTimingPuzzle();

        public CoopRescueTimingPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }

        public void Configure(Transform heldDog, Transform freeDog, int pullsNeeded, float windowDuration,
            float rescueRange = 2f)
        {
            _heldDog = heldDog;
            _freeDog = freeDog;
            _rescueRange = rescueRange <= 0f ? 2f : rescueRange;
            _puzzle.Configure(pullsNeeded, windowDuration);
            Active = true;
        }

        public void StopBeat() => Active = false;

        public bool FreeDogInRange =>
            _heldDog != null && _freeDog != null &&
            Vector2.Distance(_heldDog.position, _freeDog.position) <= _rescueRange;

        /// <summary>The held dog wiggles to crack the grip open.</summary>
        public void HeldWiggle()
        {
            if (Active) _puzzle.Wiggle();
        }

        /// <summary>The free dog pulls — only counts when it is close enough to actually grab on.</summary>
        public void FreePull()
        {
            if (!Active || !FreeDogInRange) return;
            _puzzle.Pull();
            if (_puzzle.Freed) Active = false;
        }

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (Active) _puzzle.Advance(dt);
        }
    }
}
