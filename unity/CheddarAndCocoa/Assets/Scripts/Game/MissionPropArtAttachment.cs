using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Runtime evidence for a promoted mission prop sprite layered over a controller-owned marker.
    /// </summary>
    public sealed class MissionPropArtAttachment : MonoBehaviour
    {
        public const float DefaultAffordanceRange = 2.35f;

        private static readonly Transform[] EmptyTargets = new Transform[0];
        private static Transform[] _affordanceTargets = EmptyTargets;

        private ArtSpriteOverlay _overlay;
        private float _fallbackMaxAlpha = 1f;
        private Color _baseTint = Color.white;
        private float _affordanceRange = DefaultAffordanceRange;
        private bool _affordanceEnabled = true;
        private bool _affordanceActive;

        public string ResourcePath { get; private set; } = string.Empty;
        public bool IsAffordanceActive => _affordanceActive;
        public float AffordanceRange => _affordanceRange;
        public bool HasRuntimeSprite
        {
            get
            {
                ApplyFallbackAlphaCap();
                return _overlay != null && _overlay.HasRuntimeSprite;
            }
        }
        public string RuntimeSpriteName => _overlay != null ? _overlay.RuntimeSpriteName : string.Empty;

        public static void SetAffordanceTargets(DogController[] dogs)
        {
            if (dogs == null || dogs.Length == 0)
            {
                _affordanceTargets = EmptyTargets;
                return;
            }

            var targets = new Transform[dogs.Length];
            for (int i = 0; i < dogs.Length; i++)
            {
                targets[i] = dogs[i] != null ? dogs[i].transform : null;
            }
            _affordanceTargets = targets;
        }

        public bool Init(string resourcePath, Vector3 localPosition, Vector3 localScale,
            int sortingOrder, Color tint, bool shadow = true)
        {
            ResourcePath = resourcePath ?? string.Empty;
            Sprite sprite = FinalGameplayArt.Load(ResourcePath);
            if (sprite == null) return false;

            _overlay = GetComponent<ArtSpriteOverlay>();
            if (_overlay == null) _overlay = gameObject.AddComponent<ArtSpriteOverlay>();
            _baseTint = tint;
            _overlay.Init(sprite, localPosition, localScale, sortingOrder, tint, shadow);
            return true;
        }

        public bool SetResource(string resourcePath)
        {
            ResourcePath = resourcePath ?? string.Empty;
            Sprite sprite = FinalGameplayArt.Load(ResourcePath);
            if (sprite == null || _overlay == null) return false;
            _overlay.SetSprite(sprite);
            return true;
        }

        public void SetTint(Color tint)
        {
            _baseTint = tint;
            if (_overlay != null && !_affordanceActive) _overlay.SetTint(tint);
        }

        public void CapFallbackAlpha(float maxAlpha)
        {
            _fallbackMaxAlpha = Mathf.Clamp01(maxAlpha);
            ApplyFallbackAlphaCap();
        }

        public void Pulse(float seconds = 0.2f, float strength = 0.08f)
        {
            if (_overlay != null) _overlay.Pulse(seconds, strength);
        }

        public void SetProximityAffordance(bool enabled, float range = DefaultAffordanceRange)
        {
            _affordanceEnabled = enabled;
            _affordanceRange = Mathf.Max(0.2f, range);
            if (!enabled) ClearAffordance();
        }

        private void LateUpdate()
        {
            ApplyFallbackAlphaCap();
            UpdateProximityAffordance();
        }

        private void ApplyFallbackAlphaCap()
        {
            if (_fallbackMaxAlpha >= 0.999f || !TryGetComponent<SpriteRenderer>(out var renderer)) return;
            var color = renderer.color;
            if (color.a <= _fallbackMaxAlpha) return;
            color.a = _fallbackMaxAlpha;
            renderer.color = color;
        }

        private void UpdateProximityAffordance()
        {
            if (!_affordanceEnabled || _overlay == null || !gameObject.activeInHierarchy)
            {
                ClearAffordance();
                return;
            }

            bool near = AnyAffordanceTargetNear();
            if (!near)
            {
                ClearAffordance();
                return;
            }

            _affordanceActive = true;
            float pulse = 0.24f + (Mathf.Sin(Time.time * 6f) + 1f) * 0.08f;
            Color highlight = new Color(1f, 1f, 0.72f, _baseTint.a);
            _overlay.SetTint(Color.Lerp(_baseTint, highlight, pulse));
            _overlay.Pulse(0.08f, 0.035f);
        }

        private void ClearAffordance()
        {
            if (!_affordanceActive) return;
            _affordanceActive = false;
            if (_overlay != null) _overlay.SetTint(_baseTint);
        }

        private bool AnyAffordanceTargetNear()
        {
            if (_affordanceTargets == null || _affordanceTargets.Length == 0) return false;

            Vector3 position = transform.position;
            float rangeSqr = _affordanceRange * _affordanceRange;
            for (int i = 0; i < _affordanceTargets.Length; i++)
            {
                Transform target = _affordanceTargets[i];
                if (target == null) continue;
                Vector3 delta = target.position - position;
                if (delta.sqrMagnitude <= rangeSqr) return true;
            }

            return false;
        }
    }
}
