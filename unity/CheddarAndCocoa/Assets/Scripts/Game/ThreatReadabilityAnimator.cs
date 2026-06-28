using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Runtime sprite-strip animator for squirrel, eagle, and coyote mission actors.</summary>
    public sealed class ThreatReadabilityAnimator : MonoBehaviour
    {
        private const string AuthoredMotionName = "ThreatAuthoredMotion";

        private ThreatMotionArt.Actor _defaultActor;
        private ThreatMotionArt.Actor _actor;
        private ThreatMotionArt.Clip _clip;
        private SpriteRenderer[] _fallbackRenderers;
        private SpriteRenderer _authored;
        private Vector3 _lastPosition;
        private Vector2 _lastDirection = Vector2.right;
        private float _clipStartedAt;
        private bool _active;

        public bool UsesAuthoredMotion => _active && _authored != null && _authored.sprite != null;
        public string CurrentActorLabel => _actor.ToString();
        public string CurrentClipLabel => _active ? _clip.ToString() : string.Empty;
        public int CurrentFrameIndex { get; private set; } = -1;
        public string RuntimeSpriteName => UsesAuthoredMotion ? _authored.sprite.name : string.Empty;

        public void Init(ThreatMotionArt.Actor defaultActor, SpriteRenderer[] fallbackRenderers)
        {
            _defaultActor = defaultActor;
            _fallbackRenderers = fallbackRenderers ?? System.Array.Empty<SpriteRenderer>();
            _lastPosition = transform.position;

            var go = new GameObject(AuthoredMotionName);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, -0.08f, -0.18f);
            go.transform.localScale = Vector3.one;
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
