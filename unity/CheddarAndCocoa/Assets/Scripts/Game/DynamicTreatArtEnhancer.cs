using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Keeps respawned Backyard Rescue treats decorated with final weenie sprites.</summary>
    public sealed class DynamicTreatArtEnhancer : MonoBehaviour
    {
        public const string OverlayName = "FinalWeenieOverlay";
        private GameManager _game;
        private float _nextScan;

        public void Init(GameManager game)
        {
            _game = game;
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScan) return;
            _nextScan = Time.unscaledTime + 0.25f;
            Refresh();
        }

        private void Refresh()
        {
            bool visible = _game != null && _game.ActiveMissionVariant == GameManager.MissionVariant.BackyardRescue;
            foreach (var treat in FindObjectsByType<Treat>(FindObjectsSortMode.None))
            {
                var overlay = RuntimeArtSpriteFactory.AddOverlay(treat.transform, OverlayName, FinalGameplayArt.Weenie,
                    Vector3.zero, Vector3.one * 1.15f, 14);
                if (overlay != null) overlay.gameObject.SetActive(visible);
            }
        }
    }
}
