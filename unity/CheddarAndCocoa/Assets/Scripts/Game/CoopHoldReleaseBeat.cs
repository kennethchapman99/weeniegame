using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopHoldReleasePuzzle"/> from real dog positions so a mission can drop in a
    /// Hold-and-Release beat without bespoke logic: the anchor dog holds a pressure point open by
    /// standing in the hold zone, and the crosser dog makes progress by being in the cross corridor
    /// while the anchor holds. Letting the anchor wander off mid-cross snaps it back.
    ///
    /// Deliberately self-contained (own component, own zones) so it can be attached at runtime
    /// alongside the existing arena without editing the core mission loop. Drive it from
    /// <see cref="Update"/> in play, or call <see cref="Tick"/> directly for deterministic tests.
    /// </summary>
    public sealed class CoopHoldReleaseBeat : MonoBehaviour
    {
        private Transform _anchor;
        private Transform _crosser;
        private Vector2 _holdZone;
        private Vector2 _crossZone;
        private float _holdRange = 2f;
        private float _crossRange = 2f;

        private readonly CoopHoldReleasePuzzle _puzzle = new CoopHoldReleasePuzzle();

        public CoopHoldReleasePuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }
        public Vector2 HoldZone => _holdZone;
        public Vector2 CrossZone => _crossZone;

        public void Configure(Transform anchor, Transform crosser, Vector2 holdZone, Vector2 crossZone,
            float crossNeeded, float holdWindow, float holdRange = 2f, float crossRange = 2f)
        {
            _anchor = anchor;
            _crosser = crosser;
            _holdZone = holdZone;
            _crossZone = crossZone;
            _holdRange = holdRange <= 0f ? 2f : holdRange;
            _crossRange = crossRange <= 0f ? 2f : crossRange;
            _puzzle.Configure(crossNeeded, holdWindow);
            Active = true;
        }

        public void StopBeat() => Active = false;

        public bool AnchorIsHolding =>
            _anchor != null && Vector2.Distance(_anchor.position, _holdZone) <= _holdRange;

        public bool CrosserIsEngaged =>
            _crosser != null && Vector2.Distance(_crosser.position, _crossZone) <= _crossRange;

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        /// <summary>Advance the beat one step from the current dog positions.</summary>
        public void Tick(float dt)
        {
            if (!Active || _anchor == null || _crosser == null) return;

            _puzzle.SetHeld(AnchorIsHolding);
            if (CrosserIsEngaged) _puzzle.Advance(dt);
            if (_puzzle.Solved) Active = false;
        }
    }
}
