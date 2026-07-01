using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Small generated pointer that rides near a dog and names the current useful objective. It is
    /// deliberately hidden when the target is close so it helps without becoming constant clutter.
    /// </summary>
    public sealed class ObjectiveArrowFeedback : MonoBehaviour
    {
        private const string CueObjectName = "ObjectiveArrowCue";

        private static bool _debugTextVisible;

        private SpriteRenderer _cue;
        private TextMesh _label;
        private Transform _target;
        private string _copy = string.Empty;
        private float _hideDistance = 0.9f;
        private Vector3 _baseScale;
        private Vector3 _cueBaseScale = Vector3.one * 0.28f;
        private float _distanceToTarget;

        public string Label => _target != null ? _copy : string.Empty;
        public bool IsVisible => (_label != null && _label.gameObject.activeSelf) ||
                                 (_cue != null && _cue.gameObject.activeSelf);
        public bool TextVisible => _label != null && _label.gameObject.activeSelf;
        public float DistanceToTarget => _target != null ? _distanceToTarget : 0f;
        public bool UsesGeneratedCueArt => _cue != null && _cue.sprite != null;
        public string CueSpriteName => UsesGeneratedCueArt ? _cue.sprite.name : string.Empty;
        public string GuidanceLabel => _target == null
            ? string.Empty
            : _distanceToTarget <= _hideDistance ? "ON TARGET" : $"{_copy} {Mathf.CeilToInt(_distanceToTarget)}m";

        public static void SetDebugTextVisible(bool visible)
        {
            _debugTextVisible = visible;
            var arrows = FindObjectsByType<ObjectiveArrowFeedback>(FindObjectsSortMode.None);
            foreach (var arrow in arrows)
            {
                arrow.ApplyTextVisibility();
            }
        }

        public void Init(Color color)
        {
            Sprite cueSprite = FinalGameplayArt.Load(FinalGameplayArt.CueObjectiveArrow);
            if (cueSprite != null)
            {
                var cueGo = new GameObject(CueObjectName);
                cueGo.transform.SetParent(transform);
                cueGo.transform.localRotation = Quaternion.identity;
                cueGo.transform.localScale = _cueBaseScale;
                _cue = cueGo.AddComponent<SpriteRenderer>();
                _cue.sprite = cueSprite;
                _cue.sortingOrder = 22;
                _cue.color = Color.white;
            }

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
            if (_label.TryGetComponent<MeshRenderer>(out var labelRenderer)) labelRenderer.sortingOrder = 23;
            ApplyTextVisibility();
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
            if (_cue != null) _cue.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (_label == null) return;
            if (_target == null)
            {
                _label.gameObject.SetActive(false);
                if (_cue != null) _cue.gameObject.SetActive(false);
                return;
            }

            var delta = _target.position - transform.position;
            _distanceToTarget = delta.magnitude;
            if (_distanceToTarget <= _hideDistance)
            {
                _label.gameObject.SetActive(false);
                if (_cue != null) _cue.gameObject.SetActive(false);
                return;
            }

            var dir = ((Vector2)delta).normalized;
            var cuePosition = new Vector3(dir.x * 1.15f, dir.y * 1.15f + 0.72f, -0.18f);
            if (_cue != null)
            {
                _cue.gameObject.SetActive(true);
                _cue.transform.localPosition = cuePosition;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                _cue.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            }

            _label.transform.localPosition = cuePosition + new Vector3(0f, -0.62f, -0.02f);
            _label.transform.rotation = Quaternion.identity;
            float zoomScale = Camera.main != null ? Mathf.Clamp(Camera.main.orthographicSize / 7.5f, 1f, 3.2f) : 1f;
            _label.transform.localScale = _baseScale * zoomScale;
            if (_cue != null) _cue.transform.localScale = _cueBaseScale * Mathf.Min(zoomScale, 1.8f);
            _label.text = $"{_copy}  {Mathf.CeilToInt(_distanceToTarget)}m";
            ApplyTextVisibility();
        }

        private void ApplyTextVisibility()
        {
            if (_label == null) return;
            _label.gameObject.SetActive(_debugTextVisible && _target != null && _distanceToTarget > _hideDistance);
        }
    }
}
