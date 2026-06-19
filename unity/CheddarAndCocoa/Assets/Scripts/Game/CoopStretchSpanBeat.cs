using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopStretchSpanPuzzle"/> from the two dogs' transforms: every step it feeds
    /// the live separation (distance between the dogs) and midpoint x into the span, so the blanket/
    /// long-dog bridge is taut only when the dogs hold the right spacing. The mission calls
    /// <see cref="CatchItem"/> when a falling item reaches catch height (or a gap must be bridged).
    /// </summary>
    public sealed class CoopStretchSpanBeat : MonoBehaviour
    {
        private Transform _dogA;
        private Transform _dogB;

        private readonly CoopStretchSpanPuzzle _puzzle = new CoopStretchSpanPuzzle();

        public CoopStretchSpanPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }

        public void Configure(Transform dogA, Transform dogB,
            float minSeparation, float maxSeparation, float catchTolerance, int catchesNeeded, int maxRips)
        {
            _dogA = dogA;
            _dogB = dogB;
            _puzzle.Configure(minSeparation, maxSeparation, catchTolerance, catchesNeeded, maxRips);
            Active = true;
        }

        public void StopBeat() => Active = false;

        /// <summary>The world x where the span is currently centered (average of the two dogs).</summary>
        public float SpanMidpointX => _puzzle.MidpointX;

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active || _dogA == null || _dogB == null) return;

            float sep = Vector2.Distance(_dogA.position, _dogB.position);
            float midX = (_dogA.position.x + _dogB.position.x) * 0.5f;
            _puzzle.UpdateSpan(sep, midX);
            if (_puzzle.Solved) Active = false;
        }

        /// <summary>A falling item reaches catch height at <paramref name="itemX"/>; try to catch it on the span.</summary>
        public void CatchItem(float itemX)
        {
            if (Active) _puzzle.TryCatch(itemX);
            if (_puzzle.Solved) Active = false;
        }
    }
}
