using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Runtime sprite-strip animator for squirrel, eagle, and coyote mission actors.</summary>
    public sealed class ThreatReadabilityAnimator : MonoBehaviour
    {
        private const string AuthoredMotionName = "ThreatAuthoredMotion";
        private static readonly Vector3 AuthoredLocalOffset = new Vector3(0f, -0.08f, -0.18f);
        // Small vertical glide so the airborne eagle reads as flying between wing frames without
        // reintroducing the old whole-body scale pulse.
        private const float EagleBobAmplitude = 0.07f;
        private const float EagleBobSpeed = 2.6f;

        private ThreatMotionArt.Actor _defaultActor;
        private ThreatMotionArt.Actor _actor;
        private ThreatMotionArt.Clip _clip;
        private SpriteRenderer[] _fallbackRenderers;
        private SpriteRenderer _authored;
        private Vector3 _authoredBaseScale = Vector3.one;
        private Vector3 _lastPosition;
        private Vector2 _lastDirection = Vector2.right;
        private float _clipStartedAt;
        private bool _active;
        private float _bankDegrees;

        public bool UsesAuthoredMotion => _active && _authored != null && _authored.sprite != null;
        public string CurrentActorLabel => _actor.ToString();
        public string CurrentClipLabel => _active ? _clip.ToString() : string.Empty;
        public int CurrentFrameIndex { get; private set; } = -1;
        public string RuntimeSpriteName => UsesAuthoredMotion ? _authored.sprite.name : string.Empty;
        public float CurrentBankDegrees => _bankDegrees;

        // Couch feedback: the eagle read as a flat cutout. Depth comes from two cheap cues — the
        // body banks into its vertical travel and the glide bob breathes the sprite scale, so
        // climbing reads as tilting away/smaller and diving as leaning in/bigger. Pure functions
        // so PlayMode tests can pin the math without driving frames.
        public static float EagleBankDegrees(Vector2 travelDirection, bool mirrored)
        {
            float bank = Mathf.Clamp(travelDirection.y * 30f, -18f, 18f);
            return mirrored ? -bank : bank;
        }

        public static float EagleDepthScale(float bobOffset) => 1f - bobOffset * 0.9f;

        public void Init(ThreatMotionArt.Actor defaultActor, SpriteRenderer[] fallbackRenderers,
            float authoredScale = 1f)
        {
            _defaultActor = defaultActor;
            _fallbackRenderers = fallbackRenderers ?? System.Array.Empty<SpriteRenderer>();
            _lastPosition = transform.position;

            var go = new GameObject(AuthoredMotionName);
            go.transform.SetParent(transform);
            go.transform.localPosition = AuthoredLocalOffset;
            // Uniform scale under a uniformly scaled actor root: the frames must never render
            // squashed the way the old BodyScale-on-root setup drew them.
            _authoredBaseScale = Vector3.one * Mathf.Max(0.01f, authoredScale);
            go.transform.localScale = _authoredBaseScale;
            _authored = go.AddComponent<SpriteRenderer>();
            _authored.sortingOrder = 29;
            _authored.enabled = false;
        }

        public void SetLabelState(string label)
        {
            if (!ThreatMotionArt.TryInfer(label, _defaultActor, out var actor, out var clip))
            {
                SetAuthoredActive(false);
                return;
            }

            if (ThreatMotionArt.Load(actor, clip, 0) == null)
            {
                SetAuthoredActive(false);
                return;
            }

            if (!_active || actor != _actor || clip != _clip)
            {
                _clipStartedAt = Time.time;
                CurrentFrameIndex = -1;
            }

            _actor = actor;
            _clip = clip;
            SetAuthoredActive(true);
            ApplyFrame(force: true);
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector2 delta = position - _lastPosition;
            if (delta.sqrMagnitude > 0.0001f) _lastDirection = delta.normalized;
            _lastPosition = position;
            ApplyFrame(force: false);

            if (_active && _authored != null)
            {
                if (_actor == ThreatMotionArt.Actor.Eagle)
                {
                    float bob = Mathf.Sin(Time.time * EagleBobSpeed) * EagleBobAmplitude;
                    _authored.transform.localPosition = AuthoredLocalOffset + Vector3.up * bob;
                    float targetBank = EagleBankDegrees(_lastDirection, _authored.flipX);
                    _bankDegrees = Mathf.Lerp(_bankDegrees, targetBank,
                        1f - Mathf.Exp(-6f * Time.deltaTime));
                    _authored.transform.localRotation = Quaternion.Euler(0f, 0f, _bankDegrees);
                    _authored.transform.localScale = _authoredBaseScale * EagleDepthScale(bob);
                }
                else
                {
                    _authored.transform.localPosition = AuthoredLocalOffset;
                    _authored.transform.localRotation = Quaternion.identity;
                    _authored.transform.localScale = _authoredBaseScale;
                    _bankDegrees = 0f;
                }
            }
        }

        private void ApplyFrame(bool force)
        {
            if (!_active || _authored == null) return;

            int frame = ThreatMotionArt.FrameAtTime(_actor, _clip, Time.time - _clipStartedAt);
            if (!force && frame == CurrentFrameIndex) return;

            Sprite sprite = ThreatMotionArt.Load(_actor, _clip, frame);
            if (sprite == null)
            {
                SetAuthoredActive(false);
                return;
            }

            _authored.sprite = sprite;
            _authored.flipX = _lastDirection.x < -0.01f;
            CurrentFrameIndex = frame;
        }

        private void SetAuthoredActive(bool active)
        {
            _active = active;
            if (_authored != null) _authored.enabled = active;
            foreach (var renderer in _fallbackRenderers)
            {
                if (renderer != null) renderer.enabled = !active;
            }
            if (!active) CurrentFrameIndex = -1;
        }
    }
}
