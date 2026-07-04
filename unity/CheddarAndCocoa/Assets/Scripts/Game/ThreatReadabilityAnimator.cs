using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Runtime sprite-strip animator for squirrel, eagle, and coyote mission actors.</summary>
    public sealed class ThreatReadabilityAnimator : MonoBehaviour
    {
        private const string AuthoredMotionName = "ThreatAuthoredMotion";
        private const string BlendRendererName = "ThreatMotionBlend";
        private static readonly Vector3 AuthoredLocalOffset = new Vector3(0f, -0.08f, -0.18f);

        private ThreatMotionArt.Actor _defaultActor;
        private ThreatMotionArt.Actor _actor;
        private ThreatMotionArt.Clip _clip;
        private SpriteRenderer[] _fallbackRenderers;
        private SpriteRenderer _authored;
        private SpriteRenderer _blend;
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
        public float BlendAlpha { get; private set; }
        public string BlendSpriteName =>
            _blend != null && _blend.enabled && _blend.sprite != null ? _blend.sprite.name : string.Empty;

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

        // Sub-frame crossfade: the next strip frame fades in over the current one so four-frame
        // strips stop snapping. Ease-in keeps each authored frame crisp for most of its slot.
        public static float CrossfadeAlpha(float subFrameFraction)
        {
            float t = Mathf.Clamp01(subFrameFraction);
            return t * t;
        }

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

            // The blend renderer inherits the procedural pose from the authored child so both
            // frames move as one body during the crossfade.
            var blendGo = new GameObject(BlendRendererName);
            blendGo.transform.SetParent(go.transform);
            blendGo.transform.localPosition = Vector3.zero;
            blendGo.transform.localScale = Vector3.one;
            _blend = blendGo.AddComponent<SpriteRenderer>();
            _blend.sortingOrder = 30;
            _blend.enabled = false;
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

            if (!_active || _authored == null) return;

            float elapsed = Time.time - _clipStartedAt;
            float phase = ThreatMotionArt.CyclePhaseAtTime(_actor, _clip, elapsed);
            var pose = ThreatMotionPose.Evaluate(_actor, _clip, phase, _authored.flipX);
            float rotation = pose.RotationDegrees;
            float depth = 1f;
            if (_actor == ThreatMotionArt.Actor.Eagle)
            {
                float targetBank = EagleBankDegrees(_lastDirection, _authored.flipX);
                _bankDegrees = Mathf.Lerp(_bankDegrees, targetBank,
                    1f - Mathf.Exp(-6f * Time.deltaTime));
                rotation += _bankDegrees;
                depth = EagleDepthScale(pose.Offset.y);
            }
            else
            {
                _bankDegrees = 0f;
            }

            var authoredTransform = _authored.transform;
            authoredTransform.localPosition = AuthoredLocalOffset + (Vector3)pose.Offset;
            authoredTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
            authoredTransform.localScale = new Vector3(
                _authoredBaseScale.x * pose.Scale.x * depth,
                _authoredBaseScale.y * pose.Scale.y * depth,
                _authoredBaseScale.z);
        }

        private void ApplyFrame(bool force)
        {
            if (!_active || _authored == null) return;

            float elapsed = Time.time - _clipStartedAt;
            int frame = ThreatMotionArt.FrameAtTime(_actor, _clip, elapsed);
            if (force || frame != CurrentFrameIndex)
            {
                Sprite sprite = ThreatMotionArt.Load(_actor, _clip, frame);
                if (sprite == null)
                {
                    SetAuthoredActive(false);
                    return;
                }

                _authored.sprite = sprite;
                CurrentFrameIndex = frame;
            }

            _authored.flipX = _lastDirection.x < -0.01f;
            UpdateCrossfade(elapsed, frame);
        }

        private void UpdateCrossfade(float elapsed, int frame)
        {
            if (_blend == null) return;

            int count = ThreatMotionArt.FrameCount(_actor, _clip);
            Sprite next = count > 1 ? ThreatMotionArt.Load(_actor, _clip, (frame + 1) % count) : null;
            if (next == null)
            {
                BlendAlpha = 0f;
                _blend.enabled = false;
                return;
            }

            BlendAlpha = CrossfadeAlpha(ThreatMotionArt.SubFrameFractionAtTime(_actor, _clip, elapsed));
            _blend.sprite = next;
            _blend.flipX = _authored.flipX;
            _blend.color = new Color(1f, 1f, 1f, BlendAlpha);
            _blend.enabled = BlendAlpha > 0.001f;
        }

        private void SetAuthoredActive(bool active)
        {
            _active = active;
            if (_authored != null) _authored.enabled = active;
            if (!active && _blend != null)
            {
                _blend.enabled = false;
                BlendAlpha = 0f;
            }
            foreach (var renderer in _fallbackRenderers)
            {
                if (renderer != null) renderer.enabled = !active;
            }
            if (!active) CurrentFrameIndex = -1;
        }
    }
}
