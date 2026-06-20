using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public sealed class BackyardArtVfxPulse : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Vector3 _baseScale;
        private float _age;
        private float _duration;
        private float _spinSpeed;

        public static BackyardArtVfxPulse Spawn(Vector3 position, RuntimeArtSpriteFactory.RuntimeSpriteId spriteId, Vector3 scale, int sortingOrder, Color tint, float duration, float spinSpeed = 0f)
        {
            Sprite sprite = RuntimeArtSpriteFactory.Get(spriteId);
            if (sprite == null) return null;

            var go = new GameObject($"ArtVfx_{spriteId}");
            go.transform.position = position;
            go.transform.localScale = scale;
            var pulse = go.AddComponent<BackyardArtVfxPulse>();
            pulse.Init(sprite, sortingOrder, tint, duration, spinSpeed);
            return pulse;
        }

        private void Init(Sprite sprite, int sortingOrder, Color tint, float duration, float spinSpeed)
        {
            _baseScale = transform.localScale;
            _duration = Mathf.Max(0.05f, duration);
            _spinSpeed = spinSpeed;
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = tint;
            _renderer.sortingOrder = sortingOrder;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / _duration);
            float pop = Mathf.Lerp(0.65f, 1.25f, Mathf.Sin(t * Mathf.PI));
            transform.localScale = _baseScale * pop;
            transform.Rotate(0f, 0f, _spinSpeed * Time.deltaTime);
            if (_renderer != null)
            {
                var c = _renderer.color;
                c.a = Mathf.Lerp(c.a, 0f, t * t);
                _renderer.color = c;
            }
            if (_age >= _duration) Destroy(gameObject);
        }
    }
}
