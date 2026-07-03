using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Shows a mission-specific scenery cue (threat lane, scent trail, leash route, pee path) only
    /// while one of its missions is actually being played. Couch test #2: these cues rendered in
    /// every mission at once and read as nonsense background scenery over the painted yard plate.
    /// </summary>
    public sealed class MissionScopedScenery : MonoBehaviour
    {
        private GameManager _game;
        private SpriteRenderer[] _renderers;
        private GameManager.MissionVariant[] _variants;
        private bool _applied;
        private bool _visible;

        public bool VisibleForActiveMission => _visible;

        public static MissionScopedScenery Attach(GameObject target, params GameManager.MissionVariant[] variants)
        {
            var scoped = target.AddComponent<MissionScopedScenery>();
            scoped._variants = variants ?? System.Array.Empty<GameManager.MissionVariant>();
            scoped._renderers = target.GetComponentsInChildren<SpriteRenderer>(true);
            return scoped;
        }

        private void Update()
        {
            if (_game == null)
            {
                _game = FindFirstObjectByType<GameManager>();
                if (_game == null) return;
            }

            bool visible = !_game.MissionSelectVisible
                && System.Array.IndexOf(_variants, _game.ActiveMissionVariant) >= 0;
            if (_applied && visible == _visible) return;

            _visible = visible;
            _applied = true;
            foreach (var renderer in _renderers)
            {
                if (renderer != null) renderer.enabled = visible;
            }
        }
    }
}
