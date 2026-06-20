using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopScentRelayPuzzle"/> from positions: the reader dog reveals the real
    /// target by interacting at the scent source, and the digger dog digs whichever target station it
    /// is standing at. The primitive enforces the relay — a dig with no reveal is blind, only the
    /// revealed target finds.
    ///
    /// The mission renders the reader's call by highlighting <see cref="CoopScentRelayPuzzle.RevealedTarget"/>
    /// near the reader; this driver just maps the digger's position to a target index.
    /// </summary>
    public sealed class CoopScentRelayBeat : MonoBehaviour
    {
        private Transform _reader;
        private Transform _digger;
        private Vector2 _scentSource;
        private Vector2[] _targets = System.Array.Empty<Vector2>();
        private float _readRange = 2f;
        private float _digRange = 1.5f;

        private readonly CoopScentRelayPuzzle _puzzle = new CoopScentRelayPuzzle();

        public CoopScentRelayPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }

        public void Configure(Transform reader, Transform digger, Vector2 scentSource, Vector2[] targets,
            int findsNeeded, int seed, float readRange = 2f, float digRange = 1.5f)
        {
            _reader = reader;
            _digger = digger;
            _scentSource = scentSource;
            _targets = targets ?? System.Array.Empty<Vector2>();
            _readRange = readRange <= 0f ? 2f : readRange;
            _digRange = digRange <= 0f ? 1.5f : digRange;
            _puzzle.Configure(_targets.Length, findsNeeded, seed);
            Active = true;
        }

        public void StopBeat() => Active = false;

        public bool ReaderAtScent =>
            _reader != null && Vector2.Distance(_reader.position, _scentSource) <= _readRange;

        /// <summary>World position the reader is currently signaling, or null if nothing revealed.</summary>
        public Vector2? RevealedTargetPosition
        {
            get
            {
                int t = _puzzle.RevealedTarget;
                return t >= 0 && t < _targets.Length ? _targets[t] : (Vector2?)null;
            }
        }

        /// <summary>The reader reads the scent (only counts at the scent source).</summary>
        public void ReaderReveal()
        {
            if (Active && ReaderAtScent) _puzzle.Reveal();
        }

        /// <summary>The digger digs the nearest target station it is standing on (if any).</summary>
        public void DiggerDig()
        {
            if (!Active || _digger == null) return;

            int nearest = NearestTargetInRange(_digger.position);
            if (nearest >= 0)
            {
                _puzzle.ActOn(nearest);
                if (_puzzle.Solved) Active = false;
            }
        }

        private int NearestTargetInRange(Vector2 pos)
        {
            int best = -1;
            float bestDist = float.PositiveInfinity;
            for (int i = 0; i < _targets.Length; i++)
            {
                float d = Vector2.Distance(pos, _targets[i]);
                if (d <= _digRange && d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }
    }
}
