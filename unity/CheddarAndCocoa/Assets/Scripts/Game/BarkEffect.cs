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
            var art = ArenaArtCatalog.BarkFeedback;
            var go = new GameObject(art.RingName);
            go.transform.position = _dog.transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _ring;
            sr.color = _tint;
            sr.sortingOrder = 20;
            go.AddComponent<Ring>().Begin(sr);

            var burst = new GameObject(art.BurstName);
            burst.transform.position = _dog.transform.position + Vector3.up * 0.9f;
            ArenaDraftArt.AddSpriteBadge(burst.transform, ArenaDraftArt.VfxBarkBadgeName,
                ArenaDraftArt.SpriteId.Vfx, Vector3.zero, new Vector3(0.045f, 0.045f, 1f), 19,
                new Color(1f, 1f, 1f, 0.32f));
            var text = burst.AddComponent<TextMesh>();
            text.text = art.BurstText;
            text.fontSize = art.BurstFontSize;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = _tint;
            burst.AddComponent<FloatingText>().Begin(text);
        }

        /// <summary>Drives one ring's expand + fade, then self-destructs.</summary>
        private sealed class Ring : MonoBehaviour
        {
            private SpriteRenderer _sr;
            private float _t;

            public void Begin(SpriteRenderer sr) => _sr = sr;

            private void Update()
            {
                _t += Time.deltaTime;
                var art = ArenaArtCatalog.BarkFeedback;
                float k = _t / art.RingLifeSeconds;
                float scale = Mathf.Lerp(art.RingStartScale, art.RingEndScale, k);
                transform.localScale = new Vector3(scale, scale, 1f);

                Color c = _sr.color;
                c.a = Mathf.Lerp(0.8f, 0f, k);
                _sr.color = c;

                if (_t >= art.RingLifeSeconds) Destroy(gameObject);
            }
        }

        /// <summary>Comic bark text paired with the ring; self-contained so no UI system is needed.</summary>
        private sealed class FloatingText : MonoBehaviour
        {
            private TextMesh _text;
            private float _t;

            public void Begin(TextMesh text) => _text = text;

            private void Update()
            {
                _t += Time.deltaTime;
                transform.position += Vector3.up * (Time.deltaTime * 0.5f);
                if (_text != null)
                {
                    Color c = _text.color;
                    c.a = Mathf.Lerp(1f, 0f, _t / ArenaArtCatalog.BarkFeedback.BurstLifeSeconds);
                    _text.color = c;
                }

                if (_t >= ArenaArtCatalog.BarkFeedback.BurstLifeSeconds) Destroy(gameObject);
            }
        }
    }
}
