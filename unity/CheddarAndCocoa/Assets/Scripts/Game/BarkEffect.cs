using UnityEngine;
using CheddarAndCocoa.Dogs;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Visible bark feedback (not just a console log): when its dog barks, it spawns a ring sprite at
    /// the dog that expands and fades over a fraction of a second. Pairs with the sprite squash-pop in
    /// <see cref="DogController"/> and the floating "WOOF!" text in the HUD. Cosmetic only.
    /// </summary>
    public sealed class BarkEffect : MonoBehaviour
    {
        private DogController _dog;
        private Sprite _ring;
        private Color _tint = Color.white;
        private System.Action<DogId> _handler;

        /// <summary>Subscribe to the dog's bark and use <paramref name="ring"/> for the pulse sprite.</summary>
        public void Init(DogController dog, Sprite ring, Color tint)
        {
            _dog = dog;
            _ring = ring;
            _tint = tint;
            _handler = _ => Spawn();
            _dog.OnBark += _handler;
        }

        private void OnDestroy()
        {
            if (_dog != null && _handler != null) _dog.OnBark -= _handler;
        }

        private void Spawn()
        {
            var go = new GameObject("BarkRing");
            go.transform.position = _dog.transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _ring;
            sr.color = _tint;
            sr.sortingOrder = 20;
            go.AddComponent<Ring>().Begin(sr);
        }

        /// <summary>Drives one ring's expand + fade, then self-destructs.</summary>
        private sealed class Ring : MonoBehaviour
        {
            private const float Life = 0.45f;
            private SpriteRenderer _sr;
            private float _t;

            public void Begin(SpriteRenderer sr) => _sr = sr;

            private void Update()
            {
                _t += Time.deltaTime;
                float k = _t / Life;
                float scale = Mathf.Lerp(0.4f, 2.4f, k);
                transform.localScale = new Vector3(scale, scale, 1f);

                Color c = _sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, k);
                _sr.color = c;

                if (_t >= Life) Destroy(gameObject);
            }
        }
    }
}
