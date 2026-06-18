using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Small generated pointer that rides near a dog and names the current useful objective. It is
    /// deliberately hidden when the target is close so it helps without becoming constant clutter.
    /// </summary>
    public sealed class ObjectiveArrowFeedback : MonoBehaviour
    {
        private TextMesh _label;
        private Transform _target;
        private string _copy = string.Empty;
        private float _hideDistance = 0.9f;
        private Vector3 _baseScale;
        private float _distanceToTarget;

        public string Label => _target != null ? _copy : string.Empty;
        public bool IsVisible => _label != null && _label.gameObject.activeSelf;
        public float DistanceToTarget => _target != null ? _distanceToTarget : 0f;
        public string GuidanceLabel => _target == null
            ? string.Empty
            : _distanceToTarget <= _hideDistance ? "ON TARGET" : $"{_copy} {Mathf.CeilToInt(_distanceToTarget)}m";

        public void Init(Color color)
        {
            var slot = ArenaArtCatalog.ObjectiveArrowLabel;
            var labelGo = new GameObject(slot.Name);
            labelGo.transform.SetParent(transform);
            labelGo.transform.localScale = slot.LocalScale;
            _baseScale = slot.LocalScale;
            _label = labelGo.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = slot.FontSize;
            _label.color = color;
            Hide();
        }

        public void PointAt(Transform target, string copy, float hideDistance = 0.9f)
        {
            _target = target;
            _copy = copy;
            _hideDistance = hideDistance;
        }

        public void Hide()
        {
            _target = null;
            _copy = string.Empty;
            _distanceToTarget = 0f;
            if (_label != null) _label.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_label == null) return;
            if (_target == null)
            {
                _label.gameObject.SetActive(false);
                return;
            }

            var delta = _target.position - transform.position;
            _distanceToTarget = delta.magnitude;
            if (_distanceToTarget <= _hideDistance)
            {
                _label.gameObject.SetActive(false);
                return;
            }

            _label.gameObject.SetActive(true);
            var dir = ((Vector2)delta).normalized;
            _label.transform.localPosition = new Vector3(dir.x * 1.1f, dir.y * 1.1f + 0.75f, -0.15f);
            _label.transform.rotation = Quaternion.identity;
            float zoomScale = Camera.main != null ? Mathf.Clamp(Camera.main.orthographicSize / 7.5f, 1f, 3.2f) : 1f;
            _label.transform.localScale = _baseScale * zoomScale;
            _label.text = $"{ArrowGlyph(dir)} {_copy}  {Mathf.CeilToInt(_distanceToTarget)}m";
        }

        private static string ArrowGlyph(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) return dir.x >= 0f ? ">" : "<";
            return dir.y >= 0f ? "^" : "v";
        }
    }
}
