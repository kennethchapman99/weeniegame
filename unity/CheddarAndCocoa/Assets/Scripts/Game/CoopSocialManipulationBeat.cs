using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Drives a <see cref="CoopSocialManipulationPuzzle"/> from positions: each stimulus has a station
    /// (the door, the leash, the shoe…) and an owner dog; a stimulus is active while its owner stands
    /// at its station. The active set is recomputed each step, so the team produces the required
    /// message only by both dogs covering their stations at once.
    /// </summary>
    public sealed class CoopSocialManipulationBeat : MonoBehaviour
    {
        [System.Serializable]
        public struct StimulusStation
        {
            public SocialStimulus Flag;
            public ChainActor Owner;
            public Vector2 Position;
        }

        private StimulusStation[] _stations = System.Array.Empty<StimulusStation>();
        private Transform _cheddar;
        private Transform _cocoa;
        private float _range = 2f;

        private readonly CoopSocialManipulationPuzzle _puzzle = new CoopSocialManipulationPuzzle();

        public CoopSocialManipulationPuzzle Puzzle => _puzzle;
        public bool Active { get; private set; }

        public void Configure(StimulusStation[] stations, SocialStimulus required,
            float comprehendNeeded, float confusionMax, Transform cheddar, Transform cocoa, float range = 2f)
        {
            _stations = stations ?? System.Array.Empty<StimulusStation>();
            _cheddar = cheddar;
            _cocoa = cocoa;
            _range = range <= 0f ? 2f : range;
            _puzzle.Configure(required, comprehendNeeded, confusionMax);
            Active = true;
        }

        public void StopBeat() => Active = false;

        public SocialStimulus CurrentActiveSet()
        {
            SocialStimulus active = SocialStimulus.None;
            foreach (var s in _stations)
            {
                bool cheddarThere = _cheddar != null && Vector2.Distance(_cheddar.position, s.Position) <= _range;
                bool cocoaThere = _cocoa != null && Vector2.Distance(_cocoa.position, s.Position) <= _range;
                bool covered = s.Owner switch
                {
                    ChainActor.Cheddar => cheddarThere,
                    ChainActor.Cocoa => cocoaThere,
                    _ => cheddarThere || cocoaThere,
                };
                if (covered) active |= s.Flag;
            }
            return active;
        }

        private void Update()
        {
            if (Active) Tick(Time.deltaTime);
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            _puzzle.SetActiveSet(CurrentActiveSet());
            _puzzle.Advance(dt);
            if (_puzzle.Solved) Active = false;
        }
    }
}
