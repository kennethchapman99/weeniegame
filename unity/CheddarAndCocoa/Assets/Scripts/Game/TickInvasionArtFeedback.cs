using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Mission-local nonverbal read for infestation pressure. The controller owns the state; this
    /// component only turns that state into a pulsing swarm or a single urgent Super Tick sprite.
    /// </summary>
    public sealed class TickInvasionArtFeedback : MonoBehaviour
    {
        private SpriteRenderer _renderer;
        private Sprite _swarm;
        private Sprite _superTick;
        private Vector3 _baseScale;
        private float _pressure;
        private bool _super;

        public bool HasAuthoredArt => _swarm != null && _superTick != null;
        public bool Visible => _renderer != null && _renderer.enabled;
        public string CurrentSpriteName => _renderer != null && _renderer.sprite != null ? _renderer.sprite.name : string.Empty;
        public float Pressure => _pressure;

        public void Init()
        {
            _swarm = FinalGameplayArt.Load(FinalGameplayArt.TickInvasionSwarm);
            _superTick = FinalGameplayArt.Load(FinalGameplayArt.TickInvasionSuperTick);

            var child = new GameObject("TickInfestationArt");
            child.transform.SetParent(transform);
            child.transform.localPosition = new Vector3(0f, 0.48f, -0.42f);
            child.transform.localRotation = Quaternion.identity;
            _baseScale = Vector3.one * 0.24f;
            child.transform.localScale = _baseScale;
            _renderer = child.AddComponent<SpriteRenderer>();
            _renderer.sortingOrder = 46;
            _renderer.enabled = false;
        }

        public void SetState(float pressure, bool superTick, bool wet)
        {
            _pressure = Mathf.Clamp01(pressure);
            _super = superTick;
            if (_renderer == null) return;

            _renderer.sprite = _super ? _superTick : _swarm;
            _renderer.enabled = _renderer.sprite != null && (_super || _pressure >= 0.08f);
            if (!_renderer.enabled) return;

            float alpha = _super ? 1f : Mathf.Lerp(0.32f, 0.9f, _pressure);
            _renderer.color = wet ? new Color(0.7f, 0.9f, 1f, alpha) : new Color(1f, 1f, 1f, alpha);
            _baseScale = Vector3.one * (_super ? 0.27f : Mathf.Lerp(0.17f, 0.25f, _pressure));
        }

        public void Hide()
        {
            _pressure = 0f;
            _super = false;
            if (_renderer != null) _renderer.enabled = false;
        }

        private void Update()
        {
            if (_renderer == null || !_renderer.enabled) return;
            float speed = _super ? 9f : Mathf.Lerp(4f, 7f, _pressure);
            float strength = _super ? 0.12f : Mathf.Lerp(0.025f, 0.08f, _pressure);
            float pulse = 1f + Mathf.Sin(Time.time * speed) * strength;
            _renderer.transform.localScale = _baseScale * pulse;
            _renderer.transform.localRotation = Quaternion.Euler(0f, 0f,
                _super ? Mathf.Sin(Time.time * 5f) * 8f : Mathf.Sin(Time.time * 2.4f) * 3f);
        }
    }
}
