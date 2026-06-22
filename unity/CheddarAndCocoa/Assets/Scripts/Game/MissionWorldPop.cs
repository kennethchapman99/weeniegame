using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Short-lived world text for score, rescue, tug, and miss moments.</summary>
    public sealed class MissionWorldPop : MonoBehaviour
    {
        private TextMesh _label;
        private float _t;

        public string Label => _label != null ? _label.text : string.Empty;
        public float StrategicScale { get; private set; } = 1f;

        public void Begin(TextMesh label) => _label = label;

        private void Update()
        {
            _t += Time.deltaTime;
            var art = ArenaArtCatalog.WorldPop;
            transform.position += Vector3.up * (Time.deltaTime * art.RiseSpeed);
            float popScale = 1f + Mathf.Sin(Mathf.Clamp01(_t / art.LifeSeconds) * Mathf.PI) * art.PopScaleAmount;
            StrategicScale = Camera.main != null
                ? Mathf.Clamp(Camera.main.orthographicSize / 7.5f, 1f, 3.2f)
                : 1f;
            transform.localScale = Vector3.one * (popScale * StrategicScale);

            if (_label != null)
            {
                _label.transform.rotation = Quaternion.identity;
                Color c = _label.color;
                c.a = Mathf.Lerp(1f, 0f, _t / art.LifeSeconds);
                _label.color = c;
            }

            if (_t >= art.LifeSeconds) Destroy(gameObject);
        }
    }
}
