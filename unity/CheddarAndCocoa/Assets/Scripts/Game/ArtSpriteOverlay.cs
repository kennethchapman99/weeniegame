using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Adds a real-art sprite overlay while preserving the generated collision/readability object below it.
    /// </summary>
    public sealed class ArtSpriteOverlay : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private SpriteRenderer _shadow;
        private Vector3 _baseScale;
        private float _pulseUntil;
        private float _pulseStrength;
        private string _runtimeSpriteName;

        public bool HasRuntimeSprite => _renderer != null && _renderer.sprite != null;
        public string RuntimeSpriteName => _runtimeSpriteName;

        public void Init(Sprite sprite, Vector3 localPosition, Vector3 localScale, int sortingOrder, Color tint, bool shadow = true)
        {
            _baseScale = localScale;
            if (_renderer == null)
            {
                var go = new GameObject("ActualArtOverlay");
                go.transform.SetParent(transform);
                go.transform.localPosition = localPosition;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = localScale;
                _renderer = go.AddComponent<SpriteRenderer>();
            }

            _renderer.sprite = sprite;
            _renderer.color = tint;
            _renderer.sortingOrder = sortingOrder;
            _runtimeSpriteName = sprite != null ? sprite.name : string.Empty;

            if (shadow && _shadow == null)
            {
                var shadowGo = new GameObject("ActualArtShadow");
                shadowGo.transform.SetParent(transform);
                shadowGo.transform.localPosition = localPosition + new Vector3(0f, -0.26f, 0.08f);
                shadowGo.transform.localRotation = Quaternion.identity;
                shadowGo.transform.localScale = new Vector3(localScale.x * 0.92f, localScale.y * 0.18f, 1f);
                _shadow = shadowGo.AddComponent<SpriteRenderer>();
                _shadow.sprite = SpriteShapeCache.WhiteSquare;
                _shadow.color = new Color(0f, 0f, 0f, 0.22f);
                _shadow.sortingOrder = sortingOrder - 2;
            }
        }

        public void Pulse(float seconds, float strength)
        {
            _pulseUntil = Mathf.Max(_pulseUntil, Time.time + seconds);
            _pulseStrength = Mathf.Max(_pulseStrength, strength);
        }

        private void Update()
        {
            if (_renderer == null) return;

            float pulse = Time.time < _pulseUntil ? 1f + Mathf.Sin(Time.time * 28f) * _pulseStrength : 1f;
            _renderer.transform.localScale = _baseScale * pulse;

            if (_shadow != null)
            {
                _shadow.transform.localScale = new Vector3(_baseScale.x * 0.92f, _baseScale.y * 0.18f, 1f);
            }
        }
    }
}
