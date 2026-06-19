using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Treats are spawned/despawned during play, so art overlays need to follow new Treat instances.
    /// This keeps pickup visuals art-driven without changing Treat or GameManager spawning rules.
    /// </summary>
    public sealed class DynamicTreatArtEnhancer : MonoBehaviour
    {
        private readonly HashSet<int> _enhanced = new HashSet<int>();
        private float _nextScanAt;

        public int EnhancedTreatCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ArenaScene") return;
            var go = new GameObject("DynamicTreatArtEnhancer");
            go.AddComponent<DynamicTreatArtEnhancer>();
        }

        private void Update()
        {
            if (Time.time < _nextScanAt) return;
            _nextScanAt = Time.time + 0.35f;
            ScanTreats();
        }

        public void ScanTreats()
        {
            Sprite sprite = RuntimeArtSpriteFactory.Get(RuntimeArtSpriteFactory.RuntimeSpriteId.WeenieCollectible);
            if (sprite == null) return;

            foreach (var treat in FindObjectsByType<Treat>(FindObjectsSortMode.None))
            {
                if (treat == null) continue;
                int id = treat.GetInstanceID();
                if (_enhanced.Contains(id)) continue;

                var overlay = treat.GetComponent<ArtSpriteOverlay>() ?? treat.gameObject.AddComponent<ArtSpriteOverlay>();
                overlay.Init(sprite, new Vector3(0f, 0f, -0.22f), new Vector3(0.032f, 0.032f, 1f), 31, new Color(1f, 1f, 1f, 0.94f), true);
                _enhanced.Add(id);
                EnhancedTreatCount++;
            }
        }
    }
}
