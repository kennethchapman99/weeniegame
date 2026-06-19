using System.Collections;
using CheddarAndCocoa.Dogs;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Additive art pass for the playable backyard slice. It layers real/draft sprites, shadows, and
    /// art-driven VFX over the existing generated gameplay objects without changing collision or rules.
    /// </summary>
    public sealed class BackyardRescueArtEnhancer : MonoBehaviour
    {
        private GameManager _game;
        private ArtSpriteOverlay _squirrelOverlay;
        private ArtSpriteOverlay _predatorOverlay;
        private ArtSpriteOverlay _ropeOverlay;
        private string _lastScoreLabel = string.Empty;
        private GameManager.FeedbackKind _lastFeedback;
        private float _nextAmbientAt;

        public bool Enhanced { get; private set; }
        public int OverlayCount { get; private set; }
        public int VfxSpawnCount { get; private set; }
        public string LastEnhancementSummary { get; private set; } = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallSceneHook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "ArenaScene") return;
            var go = new GameObject("BackyardRescueArtEnhancer");
            go.AddComponent<BackyardRescueArtEnhancer>();
        }

        private IEnumerator Start()
        {
            for (int i = 0; i < 60 && _game == null; i++)
            {
                _game = FindFirstObjectByType<GameManager>();
                if (_game == null) yield return null;
            }

            if (_game == null)
            {
                LastEnhancementSummary = "No GameManager found";
                yield break;
            }

            EnhanceNow();
        }

        public void EnhanceNow()
        {
            if (_game == null) _game = FindFirstObjectByType<GameManager>();
            if (_game == null || Enhanced) return;

            OverlayCount = 0;
            EnhanceDogShadows();
            _squirrelOverlay = AddOverlay(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.Squirrel, new Vector3(0f, 0.05f, -0.35f), new Vector3(0.035f, 0.035f, 1f), 34, new Color(1f, 1f, 1f, 0.95f));
            _predatorOverlay = AddOverlay(_game.PredatorObject, RuntimeArtSpriteFactory.RuntimeSpriteId.EagleThreat, new Vector3(0f, 0.1f, -0.35f), new Vector3(0.05f, 0.05f, 1f), 36, new Color(1f, 1f, 1f, 0.78f));
            _ropeOverlay = AddOverlay(_game.RopeObject, RuntimeArtSpriteFactory.RuntimeSpriteId.RopeToy, new Vector3(0f, 0f, -0.32f), new Vector3(0.035f, 0.035f, 1f), 32, new Color(1f, 1f, 1f, 0.92f));
            AddBackyardSetDressing();

            Enhanced = true;
            LastEnhancementSummary = $"Art overlays active: {OverlayCount}";
            _lastFeedback = _game.LastFeedback;
            _lastScoreLabel = _game.LastScoreEventLabel;
        }

        private void Update()
        {
            if (_game == null || !Enhanced) return;

            if (_game.LastFeedback != _lastFeedback)
            {
                ReactToFeedback(_game.LastFeedback);
                _lastFeedback = _game.LastFeedback;
            }

            if (_game.LastScoreEventLabel != _lastScoreLabel)
            {
                ReactToScore(_game.LastScoreEventLabel);
                _lastScoreLabel = _game.LastScoreEventLabel;
            }

            if (Time.time >= _nextAmbientAt)
            {
                _nextAmbientAt = Time.time + 2.5f;
                if (_game.ActiveMissionVariant == GameManager.MissionVariant.BackyardRescue)
                    SpawnAmbientLeafPop();
            }
        }

        private ArtSpriteOverlay AddOverlay(GameObject target, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color tint)
        {
            if (target == null) return null;
            Sprite sprite = RuntimeArtSpriteFactory.Get(spriteId);
            if (sprite == null) return null;

            var overlay = target.GetComponent<ArtSpriteOverlay>() ?? target.AddComponent<ArtSpriteOverlay>();
            overlay.Init(sprite, localPosition, localScale, sortingOrder, tint, true);
            OverlayCount++;
            return overlay;
        }

        private void EnhanceDogShadows()
        {
            foreach (var feedback in FindObjectsByType<DogReadabilityFeedback>(FindObjectsSortMode.None))
            {
                if (feedback == null) continue;
                var shadow = feedback.transform.Find("ActualDogShadow");
                if (shadow != null) continue;

                var go = new GameObject("ActualDogShadow");
                go.transform.SetParent(feedback.transform);
                go.transform.localPosition = new Vector3(0f, -0.55f, 0.08f);
                go.transform.localScale = new Vector3(1.35f, 0.22f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = SpriteShapeCache.WhiteSquare;
                sr.color = new Color(0f, 0f, 0f, 0.18f);
                sr.sortingOrder = 3;
                OverlayCount++;
            }
        }

        private void AddBackyardSetDressing()
        {
            AddWorldArt("ActualArtBushLeft", RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardBush, new Vector3(-26f, -12f, 0.2f), new Vector3(0.06f, 0.06f, 1f), 2, new Color(1f, 1f, 1f, 0.82f));
            AddWorldArt("ActualArtBushRight", RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardBush, new Vector3(25f, 12f, 0.2f), new Vector3(0.06f, 0.06f, 1f), 2, new Color(1f, 1f, 1f, 0.82f));
            AddWorldArt("ActualArtFenceAccent", RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardFence, new Vector3(0f, 16f, 0.25f), new Vector3(0.08f, 0.08f, 1f), 1, new Color(1f, 1f, 1f, 0.75f));
            AddWorldArt("ActualArtRockAccent", RuntimeArtSpriteFactory.RuntimeSpriteId.BackyardRock, new Vector3(-7f, -14f, 0.2f), new Vector3(0.055f, 0.055f, 1f), 2, new Color(1f, 1f, 1f, 0.8f));
        }

        private void AddWorldArt(string name, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId, Vector3 position, Vector3 scale, int sortingOrder, Color tint)
        {
            Sprite sprite = RuntimeArtSpriteFactory.Get(spriteId);
            if (sprite == null) return;
            if (GameObject.Find(name) != null) return;

            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = scale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = tint;
            OverlayCount++;
        }

        private void ReactToFeedback(GameManager.FeedbackKind feedback)
        {
            switch (feedback)
            {
                case GameManager.FeedbackKind.SquirrelStealing:
                    _squirrelOverlay?.Pulse(0.5f, 0.12f);
                    SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.045f, new Color(1f, 0.88f, 0.35f, 0.9f));
                    break;
                case GameManager.FeedbackKind.SquirrelScared:
                    _squirrelOverlay?.Pulse(0.35f, 0.16f);
                    SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop, 0.045f, new Color(0.7f, 1f, 0.65f, 0.9f));
                    break;
                case GameManager.FeedbackKind.SquirrelStoleFood:
                    _squirrelOverlay?.Pulse(0.6f, 0.18f);
                    SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.05f, new Color(1f, 0.35f, 0.25f, 0.9f));
                    break;
                case GameManager.FeedbackKind.PredatorHuddle:
                case GameManager.FeedbackKind.PredatorAttack:
                    _predatorOverlay?.Pulse(0.7f, 0.12f);
                    SpawnAt(_game.PredatorObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.06f, new Color(1f, 0.2f, 0.2f, 0.82f));
                    break;
                case GameManager.FeedbackKind.UnitedBark:
                    SpawnTeamBarkBurst();
                    break;
                case GameManager.FeedbackKind.TugTogether:
                    _ropeOverlay?.Pulse(0.45f, 0.14f);
                    SpawnAt(_game.RopeObject, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop, 0.045f, new Color(1f, 0.92f, 0.35f, 0.9f));
                    break;
                case GameManager.FeedbackKind.LevelClear:
                    SpawnAt(_game.RopeObject, RuntimeArtSpriteFactory.RuntimeSpriteId.SuccessPop, 0.08f, new Color(0.65f, 1f, 0.55f, 0.9f));
                    SpawnTeamBarkBurst();
                    break;
                case GameManager.FeedbackKind.GameOver:
                    SpawnAt(_game.PredatorObject != null && _game.PredatorObject.activeInHierarchy ? _game.PredatorObject : _game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.WarningAlert, 0.07f, new Color(1f, 0.35f, 0.3f, 0.9f));
                    break;
            }
        }

        private void ReactToScore(string label)
        {
            if (string.IsNullOrEmpty(label)) return;
            if (label.Contains("WEENIE") || label.Contains("SNACK") || label.Contains("SOCK"))
                SpawnAt(_game.SquirrelObject, RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle, 0.04f, new Color(1f, 0.95f, 0.45f, 0.85f));
        }

        private void SpawnTeamBarkBurst()
        {
            Vector3 pos = Vector3.zero;
            int count = 0;
            foreach (var dog in FindObjectsByType<DogController>(FindObjectsSortMode.None))
            {
                pos += dog.transform.position;
                count++;
            }
            if (count > 0) pos /= count;
            pos += Vector3.up * 0.65f;
            BackyardArtVfxPulse.Spawn(pos, RuntimeArtSpriteFactory.RuntimeSpriteId.BarkBurst, new Vector3(0.075f, 0.075f, 1f), 60, new Color(1f, 1f, 1f, 0.9f), 0.55f, 120f);
            VfxSpawnCount++;
        }

        private void SpawnAt(GameObject target, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId, float scale, Color tint)
        {
            if (target == null) return;
            BackyardArtVfxPulse.Spawn(target.transform.position + Vector3.up * 0.8f, spriteId, new Vector3(scale, scale, 1f), 58, tint, 0.55f, 60f);
            VfxSpawnCount++;
        }

        private void SpawnAmbientLeafPop()
        {
            Vector3 pos = new Vector3(Mathf.Sin(Time.time * 0.71f) * 22f, Mathf.Cos(Time.time * 0.53f) * 12f, 0f);
            BackyardArtVfxPulse.Spawn(pos, RuntimeArtSpriteFactory.RuntimeSpriteId.PickupSparkle, new Vector3(0.025f, 0.025f, 1f), 6, new Color(0.75f, 1f, 0.55f, 0.35f), 0.8f, 30f);
            VfxSpawnCount++;
        }
    }
}
