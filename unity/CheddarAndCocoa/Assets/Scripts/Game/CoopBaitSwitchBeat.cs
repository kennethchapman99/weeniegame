using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopBaitSwitchPuzzle"/> from the baiter dog's position relative to a decoy
    /// spot: while the baiter is inside <see cref="BaitRange"/> of the decoy it is feinting (commitment
    /// rises); when it backs off, the enemy's commitment to the decoy decays. The mission calls
    /// <see cref="StrikeTarget"/> when the striker dog reaches the real prize, which lands only inside
    /// the committed window. Leaving bait range too late overbaits and snaps the enemy back, so the two
    /// dogs must talk: "he's locked — GO!"
    /// </summary>
    public sealed class CoopBaitSwitchBeat : MonoBehaviour
    {
        private Transform _baiter;
        private Transform _decoySpot;

        private readonly CoopBaitSwitchPuzzle _puzzle = new CoopBaitSwitchPuzzle();

        public CoopBaitSwitchPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }
        public float BaitRange { get; private set; } = 1.5f;

        public void Configure(Transform baiter, Transform decoySpot, float baitRange,
            float commitThreshold, float commitRate, float decayRate, float overbaitTolerance,
            int hitsNeeded, int maxBackfires)
        {
            _baiter = baiter;
            _decoySpot = decoySpot;
            BaitRange = baitRange <= 0f ? 1.5f : baitRange;
            _puzzle.Configure(commitThreshold, commitRate, decayRate, overbaitTolerance, hitsNeeded, maxBackfires);
            Active = true;
        }

        public void StopBeat() => Active = false;

        /// <summary>True while the baiter is close enough to the decoy to be actively feinting.</summary>
        public bool Baiting =>
            _baiter != null && _decoySpot != null &&
            Vector2.Distance(_baiter.position, _decoySpot.position) <= BaitRange;

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active || _baiter == null || _decoySpot == null) return;

            _puzzle.Advance(dt, Baiting);
            if (_puzzle.Solved) Active = false;
        }

        /// <summary>The striker reaches the real prize and swings; lands only while the enemy is committed.</summary>
        public void StrikeTarget()
        {
            if (Active) _puzzle.Strike();
            if (_puzzle.Solved) Active = false;
        }
    }
}
