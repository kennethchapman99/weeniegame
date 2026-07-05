using System;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Turns the GameManager's deterministic feedback stream into short cosmetic sprite pops.</summary>
    public sealed class FinalJuiceEffect : MonoBehaviour
    {
        public const string EffectNamePrefix = "FinalJuiceEffect_";

        private GameManager _game;
        private int _observedSequence;
        private int _lastSpawnFrame = -1;
        private int _sameFrameSpawnIndex;

        public int SpawnCount { get; private set; }
        public string LastSpawnedSpriteName { get; private set; } = string.Empty;
        public GameObject LastSpawnedObject { get; private set; }

        public void Init(GameManager game)
        {
            _game = game;
            _observedSequence = game != null ? game.JuiceFeedbackSequence : 0;
            if (_game != null) _game.OnJuiceFeedback += HandleFeedback;
        }

        private void OnDestroy()
        {
            if (_game != null) _game.OnJuiceFeedback -= HandleFeedback;
        }

        private void Update() => RefreshNow();

        public void RefreshNow()
        {
            if (_game == null || _game.JuiceFeedbackSequence == _observedSequence) return;
            _observedSequence = _game.JuiceFeedbackSequence;
            Spawn(_game.LastJuiceFeedback, _game.LastJuiceLabel);
        }

        private void HandleFeedback(GameManager.JuiceFeedbackKind kind, string label)
        {
            _observedSequence = _game != null ? _game.JuiceFeedbackSequence : _observedSequence;
            Spawn(kind, label);
        }

        private void Spawn(GameManager.JuiceFeedbackKind kind, string label)
        {
            string path = SelectSpritePath(kind, label,
                _game.LastScoreDelta, _game.IsGameOver);
            if (string.IsNullOrEmpty(path)) return;

            Sprite sprite = FinalGameplayArt.Load(path);
            if (sprite == null) return;

            // A controller's own fail-gag SetJuice call and GameManager.EndRound's generic follow-up
            // (e.g. "SAD FLOP REPLAY!") land in the same Update, one frame apart from each other in
            // wall-clock terms but on the identical Time.frameCount. Destroying-on-replace there would
            // kill the controller's pop before it ever rendered a frame, so only replace across frames;
            // same-frame pops stack with a vertical offset instead.
            int frame = Time.frameCount;
            if (frame != _lastSpawnFrame)
            {
                if (LastSpawnedObject != null) Destroy(LastSpawnedObject);
                _sameFrameSpawnIndex = 0;
            }
            else
            {
                _sameFrameSpawnIndex++;
            }
            _lastSpawnFrame = frame;

            var effect = new GameObject($"{EffectNamePrefix}{_observedSequence}");
            effect.transform.position = FeedbackPosition(_game) +
                new Vector3(0.9f, 1.55f + _sameFrameSpawnIndex * 0.55f, 0f);
            float width = EffectWorldWidth(path);
            float scale = sprite.bounds.size.x > 0.001f ? width / sprite.bounds.size.x : 1f;
            effect.transform.localScale = Vector3.one * scale;

            var renderer = effect.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 24;
            effect.AddComponent<PopLifetime>().Begin(renderer, effect.transform.localScale);

            SpawnCount++;
            LastSpawnedSpriteName = sprite.name;
            LastSpawnedObject = effect;
        }

        public static string SelectSpritePath(GameManager.JuiceFeedbackKind kind, string label,
            int scoreDelta, bool gameOver)
        {
            label ??= string.Empty;
            switch (kind)
            {
                case GameManager.JuiceFeedbackKind.ScoreDelta:
                    return scoreDelta >= 0 ? FinalGameplayArt.PickupSparkle : FinalGameplayArt.FailPuff;
                case GameManager.JuiceFeedbackKind.SuccessPop:
                    return label.IndexOf("RESCUE", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           label.IndexOf("PREDATOR", StringComparison.OrdinalIgnoreCase) >= 0
                        ? FinalGameplayArt.RescueBurst
                        : FinalGameplayArt.SuccessPop;
                case GameManager.JuiceFeedbackKind.WarningMiss:
                    return gameOver || label.IndexOf("FLOP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                           label.IndexOf("FAILED", StringComparison.OrdinalIgnoreCase) >= 0
                        ? FinalGameplayArt.FailPuff
                        : FinalGameplayArt.WarningAlert;
                default:
                    return null;
            }
        }

        private static Vector3 FeedbackPosition(GameManager game)
        {
            if (game.DogFeedback == null || game.DogFeedback.Length == 0) return Vector3.zero;
            Vector3 total = Vector3.zero;
            int count = 0;
            foreach (var dog in game.DogFeedback)
            {
                if (dog == null) continue;
                total += dog.transform.position;
                count++;
            }
            return count > 0 ? total / count : Vector3.zero;
        }

        public static float EffectWorldWidth(string path)
        {
            if (path == FinalGameplayArt.RescueBurst) return 3.6f;
            if (path == FinalGameplayArt.WarningAlert) return 2.35f;
            if (path == FinalGameplayArt.FailPuff) return 2.7f;
            if (path == FinalGameplayArt.SuccessPop) return 1.25f;
            return 2.3f;
        }

        private sealed class PopLifetime : MonoBehaviour
        {
            private const float LifeSeconds = 0.8f;
            private SpriteRenderer _renderer;
            private Vector3 _baseScale;
            private float _age;

            public void Begin(SpriteRenderer renderer, Vector3 baseScale)
            {
                _renderer = renderer;
                _baseScale = baseScale;
                transform.localScale = baseScale * 0.55f;
            }

            private void Update()
            {
                _age += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(_age / LifeSeconds);
                float punch = progress < 0.24f
                    ? Mathf.Lerp(0.55f, 1.15f, progress / 0.24f)
                    : Mathf.Lerp(1.15f, 1f, (progress - 0.24f) / 0.76f);
                transform.localScale = _baseScale * punch;
                transform.position += Vector3.up * (Time.unscaledDeltaTime * 0.35f);
                if (_renderer != null)
                {
                    Color color = _renderer.color;
                    color.a = Mathf.Lerp(1f, 0f, Mathf.InverseLerp(0.58f, 1f, progress));
                    _renderer.color = color;
                }
                if (_age >= LifeSeconds) Destroy(gameObject);
            }
        }
    }
}
