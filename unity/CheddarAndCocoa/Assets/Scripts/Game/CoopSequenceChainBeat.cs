using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopSequenceChainPuzzle"/> from discrete dog interactions at placed
    /// stations — the contraption pattern. Each chain step has a world station; a dog "uses" the next
    /// step by interacting while standing near that station, and only the step's owner advances it.
    /// Time drives the dawdle/settle regression.
    ///
    /// This is the discrete-interaction counterpart to <see cref="CoopHoldReleaseBeat"/> (continuous
    /// proximity), giving missions a template for both styles without touching the core loop. Call
    /// <see cref="Interact"/> from a dog's interact input and <see cref="Tick"/> (or let
    /// <see cref="Update"/> run) for the settle timer.
    /// </summary>
    public sealed class CoopSequenceChainBeat : MonoBehaviour
    {
        private Vector2[] _stations = System.Array.Empty<Vector2>();
        private float _interactRange = 2f;
        private readonly CoopSequenceChainPuzzle _puzzle = new CoopSequenceChainPuzzle();

        public CoopSequenceChainPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }

        public void Configure(ChainActor[] owners, Vector2[] stations, float settleTime, float interactRange = 2f)
        {
            _puzzle.Configure(owners, settleTime);
            _stations = stations ?? System.Array.Empty<Vector2>();
            _interactRange = interactRange <= 0f ? 2f : interactRange;
            Active = true;
        }

        public void StopBeat() => Active = false;

        /// <summary>World position of the step the team should do next (zero when solved/empty).</summary>
        public Vector2 CurrentStation =>
            _puzzle.Solved || _stations.Length == 0 ? Vector2.zero
            : _stations[Mathf.Clamp(_puzzle.Step, 0, _stations.Length - 1)];

        /// <summary>A dog interacts; advances the chain only if it is at the next step's station and owns it.</summary>
        public void Interact(ChainActor actor, Vector2 dogPosition)
        {
            if (!Active || _puzzle.Solved) return;

            int step = _puzzle.Step;
            if (step < 0 || step >= _stations.Length) return;
            if (Vector2.Distance(dogPosition, _stations[step]) <= _interactRange)
                _puzzle.TryStep(actor);
        }

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            _puzzle.Advance(dt);
            if (_puzzle.Solved) Active = false;
        }
    }
}
