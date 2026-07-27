using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Small deterministic motion on decorative render children. Gameplay roots, colliders, and
    /// objective anchors never move; this only keeps scenery from reading as a frozen backdrop.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SceneryAmbientMotion : MonoBehaviour
    {
        public enum Profile
        {
            LightWash,
            Breeze,
            GuidanceGlow
        }

        private SpriteRenderer _renderer;
        private Vector3 _baseLocalPosition;
        private Vector3 _baseLocalScale;
        private Quaternion _baseLocalRotation;
        private Color _baseColor;
        private float _phase;

        public Profile MotionProfile { get; private set; }
        public float CurrentMotionAmount { get; private set; }
        public bool PreservesGameplayAnchor => GetComponent<Collider2D>() == null;

        public static int SeedFor(string value)
        {
            unchecked
            {
                int hash = 17;
                if (value == null) return hash;
                for (int i = 0; i < value.Length; i++) hash = hash * 31 + value[i];
                return hash;
            }
        }

        public void Configure(Profile profile, int deterministicSeed)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _baseLocalPosition = transform.localPosition;
            _baseLocalScale = transform.localScale;
            _baseLocalRotation = transform.localRotation;
            _baseColor = _renderer.color;
            MotionProfile = profile;
            _phase = Mathf.Abs(deterministicSeed % 997) / 997f * Mathf.PI * 2f;
        }

        private void Awake()
        {
            if (_renderer == null) Configure(Profile.LightWash, SeedFor(gameObject.name));
        }

        private void Update()
        {
            if (_renderer == null) return;
            float speed = MotionProfile == Profile.Breeze ? 1.35f :
                MotionProfile == Profile.GuidanceGlow ? 2.1f : 0.55f;
            float wave = Mathf.Sin(Time.time * speed + _phase);
            CurrentMotionAmount = wave;

            _renderer.color = _baseColor;

            switch (MotionProfile)
            {
                case Profile.Breeze:
                    transform.localPosition = _baseLocalPosition;
                    transform.localRotation = _baseLocalRotation;
                    transform.localPosition = _baseLocalPosition + Vector3.up * (wave * 0.025f);
                    transform.localRotation = _baseLocalRotation * Quaternion.Euler(0f, 0f, wave * 1.4f);
                    break;
                case Profile.GuidanceGlow:
                    transform.localScale = _baseLocalScale;
                    float pulse = 1f + (wave + 1f) * 0.012f;
                    transform.localScale = new Vector3(
                        _baseLocalScale.x * pulse, _baseLocalScale.y * pulse, _baseLocalScale.z);
                    _renderer.color = LightShift(_baseColor, 0.035f * wave);
                    break;
                default:
                    _renderer.color = LightShift(_baseColor, 0.018f * wave);
                    break;
            }
        }

        private void OnDisable()
        {
            if (_renderer == null) return;
            if (MotionProfile == Profile.Breeze)
            {
                transform.localPosition = _baseLocalPosition;
                transform.localRotation = _baseLocalRotation;
            }
            else if (MotionProfile == Profile.GuidanceGlow)
            {
                transform.localScale = _baseLocalScale;
            }
            _renderer.color = _baseColor;
            CurrentMotionAmount = 0f;
        }

        private static Color LightShift(Color color, float amount) => new(
            Mathf.Clamp01(color.r + amount),
            Mathf.Clamp01(color.g + amount * 0.92f),
            Mathf.Clamp01(color.b + amount * 0.78f),
            color.a);
    }
}
