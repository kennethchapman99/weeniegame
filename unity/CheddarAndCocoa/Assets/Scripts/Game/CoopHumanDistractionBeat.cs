using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopHumanDistractionPuzzle"/> from inputs + positions. Cheddar's burp is an
    /// input event (<see cref="CheddarBurp"/>); Cocoa's belly flop is a toggle
    /// (<see cref="SetCocoaFlop"/>). Whoever is NOT currently the distractor is the sneaker, and counts
    /// as sneaking while standing in the objective lane — so when Cocoa commits to the flop, Cheddar
    /// sneaks; otherwise Cheddar burps and Cocoa sneaks.
    ///
    /// Input + proximity driver (completes the toolkit's driver set). Drive via <see cref="Update"/> in
    /// play or <see cref="Tick"/> for tests.
    /// </summary>
    public sealed class CoopHumanDistractionBeat : MonoBehaviour
    {
        private Transform _cheddar;
        private Transform _cocoa;
        private Vector2 _objectiveLane;
        private float _sneakRange = 2.5f;

        private readonly CoopHumanDistractionPuzzle _puzzle = new CoopHumanDistractionPuzzle();

        public CoopHumanDistractionPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }
        public Vector2 ObjectiveLane => _objectiveLane;

        /// <summary>The dog currently sneaking is the one not distracting (Cheddar while Cocoa flops).</summary>
        public Transform Sneaker => _puzzle.BellyFlopped ? _cheddar : _cocoa;

        public void Configure(Transform cheddar, Transform cocoa, Vector2 objectiveLane,
            float sneakNeeded, float attentionThreshold, float attentionDecay,
            float burpSpike, float burpCooldown, float flopRise, float flopStamina,
            float sneakRange = 2.5f)
        {
            _cheddar = cheddar;
            _cocoa = cocoa;
            _objectiveLane = objectiveLane;
            _sneakRange = sneakRange <= 0f ? 2.5f : sneakRange;
            _puzzle.Configure(sneakNeeded, attentionThreshold, attentionDecay, burpSpike, burpCooldown, flopRise, flopStamina);
            Active = true;
        }

        public void StopBeat() => Active = false;

        public void CheddarBurp()
        {
            if (Active) _puzzle.Burp();
        }

        public void SetCocoaFlop(bool flopped)
        {
            if (Active) _puzzle.SetBellyFlop(flopped);
        }

        public bool SneakerInLane
        {
            get
            {
                var s = Sneaker;
                return s != null && Vector2.Distance(s.position, _objectiveLane) <= _sneakRange;
            }
        }

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            _puzzle.Advance(dt, SneakerInLane);
            if (_puzzle.Solved) Active = false;
        }
    }
}
