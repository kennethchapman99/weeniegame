using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopChaosMachinePuzzle"/> from junction positions: each stage has a world
    /// junction and an owner dog, and the cascade keeps rolling only while that junction's helper is
    /// standing in range when the cascade reaches it. Pull the lever with <see cref="Trigger"/>; the
    /// machine then runs itself (and stalls visibly at a junction whose helper isn't there).
    /// </summary>
    public sealed class CoopChaosMachineBeat : MonoBehaviour
    {
        private ChainActor[] _owners = System.Array.Empty<ChainActor>();
        private Vector2[] _junctions = System.Array.Empty<Vector2>();
        private Transform _cheddar;
        private Transform _cocoa;
        private float _assistRange = 2f;

        private readonly CoopChaosMachinePuzzle _puzzle = new CoopChaosMachinePuzzle();

        public CoopChaosMachinePuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }

        public void Configure(ChainActor[] owners, Vector2[] junctions, Transform cheddar, Transform cocoa,
            float windowPerStage, float assistRange = 2f)
        {
            _owners = owners ?? System.Array.Empty<ChainActor>();
            _junctions = junctions ?? System.Array.Empty<Vector2>();
            _cheddar = cheddar;
            _cocoa = cocoa;
            _assistRange = assistRange <= 0f ? 2f : assistRange;
            _puzzle.Configure(_junctions.Length, windowPerStage);
            Active = true;
        }

        public void StopBeat() => Active = false;
        public void Trigger() { if (Active) _puzzle.Trigger(); }

        /// <summary>World position of the junction the cascade is currently at (or stalled at).</summary>
        public Vector2 ActiveJunction =>
            _junctions.Length == 0 ? Vector2.zero
            : _junctions[Mathf.Clamp(_puzzle.Stage, 0, _junctions.Length - 1)];

        private bool HelperAssistingCurrentStage()
        {
            int s = Mathf.Clamp(_puzzle.Stage, 0, _junctions.Length - 1);
            Vector2 junction = _junctions[s];
            bool cheddarThere = _cheddar != null && Vector2.Distance(_cheddar.position, junction) <= _assistRange;
            bool cocoaThere = _cocoa != null && Vector2.Distance(_cocoa.position, junction) <= _assistRange;
            return _owners[s] switch
            {
                ChainActor.Cheddar => cheddarThere,
                ChainActor.Cocoa => cocoaThere,
                _ => cheddarThere || cocoaThere,
            };
        }

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active || _junctions.Length == 0) return;
            if (_puzzle.Running)
                _puzzle.Advance(dt, HelperAssistingCurrentStage());
            if (_puzzle.Solved) Active = false;
        }
    }
}
