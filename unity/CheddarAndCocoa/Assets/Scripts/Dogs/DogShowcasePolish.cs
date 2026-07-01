using CheddarAndCocoa.Game;
using UnityEngine;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Dog-local presentation polish for couch-showcase readability. It only moves child renderers;
    /// the dog root, Rigidbody, collider, and input contract remain untouched.
    /// </summary>
    [RequireComponent(typeof(DogIdentity))]
    [RequireComponent(typeof(DogController))]
    public sealed class DogShowcasePolish : MonoBehaviour
    {
        public const string RootName = "ShowcasePersonalityRoot";

        private DogIdentity _identity;
        private DogController _dog;
        private Rigidbody2D _body;
        private Transform _root;
        private SpriteRenderer _groundGlow;
        private SpriteRenderer _sparkA;
        private SpriteRenderer _sparkB;
        private SpriteRenderer _sparkC;
        private SpriteRenderer _nameGlint;
        private Color _primary;
        private Color _secondary;
        private Vector3 _groundBaseScale;

        public bool Built { get; private set; }
        public int DecorativeRendererCount { get; private set; }
        public int GeneratedDogFxRendererCount { get; private set; }
        public bool UsesGeneratedDogFx => GeneratedDogFxRendererCount >= 5;
        public string PersonalitySignature { get; private set; } = string.Empty;

        public void Begin()
        {
            if (Built) return;

            _identity = GetComponent<DogIdentity>();
            _dog = GetComponent<DogController>();
            _body = GetComponent<Rigidbody2D>();

            bool cheddar = _identity.Id == DogId.Cheddar;
            _primary = cheddar ? new Color(1f, 0.62f, 0.12f, 0.46f) : new Color(0.12f, 0.95f, 0.88f, 0.42f);
            _secondary = cheddar ? new Color(1f, 0.95f, 0.25f, 0.64f) : new Color(0.45f, 0.72f, 1f, 0.58f);
            PersonalitySignature = cheddar ? "Cheddar chaos comet sparks" : "Cocoa calm queen glints";

            _root = new GameObject(RootName).transform;
            _root.SetParent(transform);
            _root.localPosition = Vector3.zero;
            _root.localRotation = Quaternion.identity;
            _root.localScale = Vector3.one;

            _groundGlow = AddRenderer("ShowcaseGroundGlow", new Vector3(0f, -0.58f, 0.06f),
                new Vector3(1.26f, 0.18f, 1f), new Color(_primary.r, _primary.g, _primary.b, 0.18f), 22);
            _sparkA = AddRenderer("ShowcaseSparkA", new Vector3(-0.48f, 0.36f, -0.24f),
                Vector3.one * 0.09f, _secondary, 35);
            _sparkB = AddRenderer("ShowcaseSparkB", new Vector3(0.48f, 0.22f, -0.24f),
                Vector3.one * 0.07f, _primary, 34);
            _sparkC = AddRenderer("ShowcaseSparkC", new Vector3(0.1f, 0.62f, -0.24f),
                Vector3.one * 0.055f, _secondary, 36);
            _nameGlint = AddRenderer("ShowcaseCollarGlint", new Vector3(0.34f, 0.02f, -0.25f),
                new Vector3(0.12f, 0.035f, 1f), _secondary, 37);
            _groundBaseScale = _groundGlow.transform.localScale;

            Built = true;
        }

        private SpriteRenderer AddRenderer(string name, Vector3 localPosition, Vector3 localScale, Color color,
            int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = SelectSprite(name) ?? SpriteShapeCache.WhiteSquare;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            DecorativeRendererCount++;
            if (renderer.sprite != SpriteShapeCache.WhiteSquare) GeneratedDogFxRendererCount++;
            return renderer;
        }

        private Sprite SelectSprite(string name)
        {
            if (name == "ShowcaseGroundGlow")
                return FinalGameplayArt.Load(FinalGameplayArt.DogFxGroundGlow);
            if (name == "ShowcaseCollarGlint")
                return FinalGameplayArt.Load(FinalGameplayArt.DogFxCollarGlint);
            bool cheddar = _identity != null && _identity.Id == DogId.Cheddar;
            return FinalGameplayArt.Load(cheddar ? FinalGameplayArt.DogFxChaosSpark : FinalGameplayArt.DogFxQueenGlint);
        }

        private void Update()
        {
            if (!Built) return;

            Vector2 velocity = _body != null ? _body.linearVelocity : Vector2.zero;
            float speed01 = Mathf.Clamp01(velocity.magnitude / 7f);
            float t = Time.time;
            bool cheddar = _identity != null && _identity.Id == DogId.Cheddar;
            float tempo = cheddar ? 5.8f : 3.4f;
            float barkPulse = _dog != null ? _dog.BarkVisualPulse : 1f;
            float zoom = _dog != null && _dog.Zoomies ? 1.5f : 1f;
            float pulse = Mathf.Sin(t * tempo) * 0.5f + 0.5f;

            if (_groundGlow != null)
            {
                _groundGlow.transform.localScale = _groundBaseScale * (1f + speed01 * 0.22f + pulse * 0.05f);
                SetAlpha(_groundGlow, 0.16f + speed01 * 0.16f + pulse * 0.04f);
            }

            AnimateSpark(_sparkA, t, 0.0f, 0.46f + speed01 * 0.18f, 0.22f + speed01 * 0.32f, zoom);
            AnimateSpark(_sparkB, t, 1.7f, 0.36f + speed01 * 0.22f, 0.12f + speed01 * 0.22f, zoom);
            AnimateSpark(_sparkC, t, 3.1f, 0.22f + speed01 * 0.12f, 0.48f + pulse * 0.12f, zoom);

            if (_nameGlint != null)
            {
                _nameGlint.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 7f) * 22f);
                _nameGlint.transform.localScale = new Vector3(0.12f * barkPulse, 0.035f * barkPulse, 1f);
                SetAlpha(_nameGlint, 0.36f + pulse * 0.28f);
            }
        }

        private void AnimateSpark(SpriteRenderer renderer, float time, float phase, float radius, float height, float zoom)
        {
            if (renderer == null) return;
            float wave = time * zoom + phase;
            renderer.transform.localPosition = new Vector3(
                Mathf.Sin(wave * 1.7f) * radius,
                0.18f + Mathf.Cos(wave * 1.2f) * height,
                -0.24f);
            renderer.transform.localRotation = Quaternion.Euler(0f, 0f, time * (90f + phase * 12f));
            float size = 0.055f + (Mathf.Sin(wave * 2.3f) * 0.5f + 0.5f) * 0.06f;
            renderer.transform.localScale = Vector3.one * size;
            SetAlpha(renderer, 0.22f + (Mathf.Sin(wave * 2.1f) * 0.5f + 0.5f) * 0.48f);
        }

        private static void SetAlpha(SpriteRenderer renderer, float alpha)
        {
            var color = renderer.color;
            color.a = Mathf.Clamp01(alpha);
            renderer.color = color;
        }
    }
}
