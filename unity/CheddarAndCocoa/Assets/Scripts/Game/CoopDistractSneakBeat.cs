using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopDistractSneakPuzzle"/> from dog positions: the distractor counts as
    /// "distracting" while it stands in the enemy's front/attention zone, and the sneaker counts as
    /// "sneaking" while it is in the sneak lane. The primitive then handles the watchfulness/annoyance
    /// squeeze and checkpoint banking.
    ///
    /// Continuous-proximity driver (sibling of <see cref="CoopHoldReleaseBeat"/>). Drive from
    /// <see cref="Update"/> in play, or <see cref="Tick"/> for deterministic tests.
    /// </summary>
    public sealed class CoopDistractSneakBeat : MonoBehaviour
    {
        private Transform _distractor;
        private Transform _sneaker;
        private Vector2 _distractZone;
        private Vector2 _sneakLane;
        private float _distractRange = 2.5f;
        private float _sneakRange = 2.5f;

        private readonly CoopDistractSneakPuzzle _puzzle = new CoopDistractSneakPuzzle();

        public CoopDistractSneakPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }
        public Vector2 DistractZone => _distractZone;
        public Vector2 SneakLane => _sneakLane;

        public void Configure(Transform distractor, Transform sneaker, Vector2 distractZone, Vector2 sneakLane,
            int segments, float segmentTime, float distractRange = 2.5f, float sneakRange = 2.5f)
        {
            _distractor = distractor;
            _sneaker = sneaker;
            _distractZone = distractZone;
            _sneakLane = sneakLane;
            _distractRange = distractRange <= 0f ? 2.5f : distractRange;
            _sneakRange = sneakRange <= 0f ? 2.5f : sneakRange;
            _puzzle.Configure(segments, segmentTime);
            Active = true;
        }

        public void StopBeat() => Active = false;

        public bool IsDistracting =>
            _distractor != null && Vector2.Distance(_distractor.position, _distractZone) <= _distractRange;

        public bool IsSneaking =>
            _sneaker != null && Vector2.Distance(_sneaker.position, _sneakLane) <= _sneakRange;

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active || _distractor == null || _sneaker == null) return;

            _puzzle.Advance(dt, IsDistracting, IsSneaking);
            if (_puzzle.Solved) Active = false;
        }
    }
}
