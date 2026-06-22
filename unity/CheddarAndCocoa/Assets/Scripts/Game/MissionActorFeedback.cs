using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Lightweight placeholder polish: readable world label + idle pulse/rotation.</summary>
    public sealed class MissionActorFeedback : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private TextMesh _label;
        private Vector3 _baseScale;
        private float _pulseAmount;
        private Quaternion _baseRotation;
        private float _swaySpeed;
        private float _swayAmplitudeDegrees;
        private float _swayPhase;

        public string Label => _label != null ? _label.text : string.Empty;

        public void Init(SpriteRenderer renderer, string label, float pulseAmount, Vector3 rotationPerSecond)
        {
            _renderer = renderer;
            _label = GetComponentInChildren<TextMesh>();
            _baseScale = transform.localScale;
            _pulseAmount = pulseAmount;
            _baseRotation = transform.localRotation;

            // The old behavior spun props continuously (squirrel 80deg/s, rope 45deg/s), which made
            // them read as unidentifiable rotating blobs. Reinterpret that authored "spin" intent as a
            // small, bounded life-wobble around the resting pose so the silhouette stays recognizable.
            float spin = rotationPerSecond.magnitude;
            if (spin > 0.01f)
            {
                _swaySpeed = Mathf.Clamp(spin * 0.03f, 1.2f, 3f);
                _swayAmplitudeDegrees = Mathf.Min(7f, spin * 0.12f);
                // Deterministic per-object phase so wobbles desync without runtime RNG.
                _swayPhase = (GetInstanceID() & 1023) / 1023f * Mathf.PI * 2f;
            }

            SetState(label, renderer != null ? renderer.color : Color.white, pulseAmount);
        }

        public void SetState(string label, Color color, float pulseAmount)
        {
            if (_label != null) _label.text = label;
            if (_renderer != null) _renderer.color = color;
            _pulseAmount = pulseAmount;
        }

        public void Pulse(float amount)
        {
            _pulseAmount = Mathf.Max(_pulseAmount, amount);
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * 5f) * _pulseAmount;
            transform.localScale = _baseScale * pulse;
            if (_swayAmplitudeDegrees > 0f)
            {
                float angle = Mathf.Sin(Time.time * _swaySpeed + _swayPhase) * _swayAmplitudeDegrees;
                transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, angle);
            }
            if (_label != null) _label.transform.rotation = Quaternion.identity;
        }
    }
}
