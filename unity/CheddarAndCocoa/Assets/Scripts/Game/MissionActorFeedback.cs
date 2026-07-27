using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Shared actor acting layer: contextual label/badge plus restrained idle and state-change
    /// motion. It animates the visual root only; mission logic remains controller-owned.
    /// </summary>
    public sealed class MissionActorFeedback : MonoBehaviour
    {
        public enum ActingProfile
        {
            Breathe,
            Attend,
            Celebrate,
            Alarm,
            Defeat
        }

        private SpriteRenderer _renderer;
        private TextMesh _label;
        private MeshRenderer _labelRenderer;
        private ThreatReadabilityAnimator _threatMotion;
        private Vector3 _baseScale;
        private float _pulseAmount;
        private Quaternion _baseRotation;
        private float _swaySpeed;
        private float _swayAmplitudeDegrees;
        private float _swayPhase;
        private ActorSignalBadge _signalBadge;
        private ActingProfile _actingProfile;
        private float _stateChangedAt;
        private bool _preserveAuthoredBounds;

        public ActorSignalBadge SignalBadge => _signalBadge;
        public string Label => _label != null ? _label.text : string.Empty;
        public bool TextVisible => _labelRenderer != null && _labelRenderer.enabled;
        // Frame-swapped characters (squirrel/eagle/coyote) must not balloon in and out: the couch
        // read that scale-pulse as the whole animation instead of the wing/run frames underneath.
        // Lazy lookup: some missions attach the animator after this component is initialized.
        public bool ScalePulseSuppressedByAuthoredMotion
        {
            get
            {
                if (_threatMotion == null) _threatMotion = GetComponent<ThreatReadabilityAnimator>();
                return _threatMotion != null && _threatMotion.UsesAuthoredMotion;
            }
        }
        public bool HasContextualTextVisibility => _label != null && _label.GetComponent<WorldLabelVisibility>() != null;
        public string ActingProfileLabel => _actingProfile.ToString();
        public float StateChangeMotion01 =>
            Mathf.Clamp01(1f - (Time.time - _stateChangedAt) / 0.42f);

        public static ActingProfile InferActingProfile(string label)
        {
            string value = (label ?? string.Empty).ToUpperInvariant();
            if (ContainsAny(value, "CAUGHT", "FAILED", "GAVE UP", "BLOCKED", "DRIVEN BACK"))
                return ActingProfile.Defeat;
            if (ContainsAny(value, "SPOTTED", "WARNING", "ALERT", "THREAT", "MISREAD",
                    "CONFUSED", "PANIC", "DANGER", "ATTACK", "SNATCH"))
                return ActingProfile.Alarm;
            if (ContainsAny(value, "SUCCESS", "CLEAR", "SAVED", "GONE", "FOUND", "WALKIES",
                    "GETTING IT", "DISTRACTED", "REPAIRED", "FREED"))
                return ActingProfile.Celebrate;
            if (ContainsAny(value, "WATCHING", "GUARD", "PATROL", "READY", "WAITING", "TARGET"))
                return ActingProfile.Attend;
            return ActingProfile.Breathe;
        }

        private static bool ContainsAny(string value, params string[] candidates)
        {
            foreach (string candidate in candidates)
                if (value.Contains(candidate, System.StringComparison.Ordinal)) return true;
            return false;
        }

        public void Init(SpriteRenderer renderer, string label, float pulseAmount, Vector3 rotationPerSecond)
        {
            _renderer = renderer;
            _label = GetComponentInChildren<TextMesh>();
            if (_label != null)
            {
                if (!_label.TryGetComponent(out _labelRenderer)) _labelRenderer = null;
                if (_label.GetComponent<WorldLabelVisibility>() == null)
                    WorldLabelVisibility.Attach(_label);
            }
            _signalBadge = ActorSignalBadge.Attach(gameObject);
            _threatMotion = GetComponent<ThreatReadabilityAnimator>();
            _baseScale = transform.localScale;
            _preserveAuthoredBounds =
                Mathf.Max(Mathf.Abs(_baseScale.x), Mathf.Abs(_baseScale.y)) /
                Mathf.Max(0.001f, Mathf.Min(Mathf.Abs(_baseScale.x), Mathf.Abs(_baseScale.y))) > 1.5f;
            _pulseAmount = pulseAmount;
            _baseRotation = transform.localRotation;

            // The old behavior spun props continuously (squirrel 80deg/s, rope 45deg/s), which made
            // them read as unidentifiable rotating blobs. Reinterpret that authored "spin" intent as a
            // small, bounded life-wobble around the resting pose so the silhouette stays recognizable.
            float spin = rotationPerSecond.magnitude;
            if (spin > 0.01f)
            {
                _swaySpeed = Mathf.Clamp(spin * 0.03f, 1.2f, 3f);
                _swayAmplitudeDegrees = Mathf.Min(3.5f, spin * 0.06f);
                // Deterministic per-object phase so wobbles desync without runtime RNG.
                _swayPhase = (GetInstanceID() & 1023) / 1023f * Mathf.PI * 2f;
            }

            SetState(label, renderer != null ? renderer.color : Color.white, pulseAmount);
        }

        public void SetState(string label, Color color, float pulseAmount)
        {
            if (_label == null || _label.text != label) _stateChangedAt = Time.time;
            if (_label != null) _label.text = label;
            if (_renderer != null) _renderer.color = color;
            _pulseAmount = pulseAmount;
            _actingProfile = InferActingProfile(label);
            _signalBadge?.Apply(label, pulseAmount);
        }

        public void Pulse(float amount)
        {
            _pulseAmount = Mathf.Max(_pulseAmount, amount);
        }

        private void Update()
        {
            float time = Time.time;
            float actingScale = _actingProfile switch
            {
                ActingProfile.Celebrate => 1f + Mathf.Abs(Mathf.Sin(time * 7f)) * 0.015f,
                ActingProfile.Alarm => 1f + Mathf.Sin(time * 18f) * 0.008f,
                ActingProfile.Defeat => 0.985f,
                _ => 1f
            };
            float pulse = ScalePulseSuppressedByAuthoredMotion
                ? 1f
                : 1f + Mathf.Sin(time * 5f) *
                    (_preserveAuthoredBounds ? Mathf.Min(_pulseAmount, 0.025f) : _pulseAmount);
            float transitionPop = 1f;
            transform.localScale = _baseScale * pulse * actingScale * transitionPop;

            float angle = _swayAmplitudeDegrees > 0f
                ? Mathf.Sin(time * _swaySpeed + _swayPhase) * _swayAmplitudeDegrees
                : 0f;
            angle += _actingProfile switch
            {
                ActingProfile.Attend => 0f,
                ActingProfile.Celebrate => Mathf.Sin(time * 6f + _swayPhase) * 0.65f,
                ActingProfile.Alarm => Mathf.Sin(time * 22f + _swayPhase) * 0.8f,
                ActingProfile.Defeat => -0.8f,
                _ => 0f
            };
            if (_preserveAuthoredBounds) angle = 0f;
            transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }
}
