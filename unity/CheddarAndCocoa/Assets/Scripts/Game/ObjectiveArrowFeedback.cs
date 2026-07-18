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
        public const string ScentBreadcrumbRootName = "ObjectiveScentBreadcrumbs";
        public const int ScentBreadcrumbSlots = 3;

        private static bool _debugTextVisible;

        private SpriteRenderer _cue;
        private SpriteRenderer[] _scentBreadcrumbs;
        private TextMesh _label;
        private Transform _target;
        private string _copy = string.Empty;
        private float _hideDistance = 0.9f;
        private Vector3 _baseScale;
        private Vector3 _cueBaseScale = Vector3.one * 0.28f;
        private float _distanceToTarget;
        private Color _scentColor = Color.white;
        private bool _emphasized;

        public string Label => _target != null ? _copy : string.Empty;
        /// <summary>Set by the guidance-escalation ladder's Tier 1 nudge; brightens the arrow cue and breadcrumbs.</summary>
        public bool IsEmphasized => _emphasized;
        public bool IsVisible => (_label != null && _label.gameObject.activeSelf) ||
                                 (_cue != null && _cue.gameObject.activeSelf) ||
                                 VisibleScentBreadcrumbCount > 0;
        public bool TextVisible => _label != null && _label.gameObject.activeSelf;
        public float DistanceToTarget => _target != null ? _distanceToTarget : 0f;
        public bool UsesGeneratedCueArt => _cue != null && _cue.sprite != null;
        public string CueSpriteName => UsesGeneratedCueArt ? _cue.sprite.name : string.Empty;
        public int ScentBreadcrumbCount => _scentBreadcrumbs != null ? _scentBreadcrumbs.Length : 0;
        public int VisibleScentBreadcrumbCount
        {
            get
            {
                if (_scentBreadcrumbs == null) return 0;
                int visible = 0;
                foreach (var breadcrumb in _scentBreadcrumbs)
                    if (breadcrumb != null && breadcrumb.gameObject.activeSelf) visible++;
                return visible;
            }
        }
        public bool UsesGeneratedScentArt => _scentBreadcrumbs != null &&
                                             _scentBreadcrumbs.Length == ScentBreadcrumbSlots &&
                                             _scentBreadcrumbs[0] != null &&
                                             _scentBreadcrumbs[0].sprite != null;
        public string ScentBreadcrumbSpriteName => UsesGeneratedScentArt
            ? _scentBreadcrumbs[0].sprite.name
            : string.Empty;
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
            _scentColor = color;
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

            BuildScentBreadcrumbs();

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
            _emphasized = false;
            if (_label != null) _label.gameObject.SetActive(false);
            if (_cue != null) _cue.gameObject.SetActive(false);
            SetScentBreadcrumbsVisible(false);
        }

        /// <summary>Guidance-ladder Tier 1 nudge: brighten the cue and breadcrumbs while the team is stalled on this target.</summary>
        public void SetEmphasis(bool emphasized) => _emphasized = emphasized;

        private void LateUpdate()
        {
            if (_label == null) return;
            if (_target == null)
            {
                _label.gameObject.SetActive(false);
                if (_cue != null) _cue.gameObject.SetActive(false);
                SetScentBreadcrumbsVisible(false);
                return;
            }

            var delta = _target.position - transform.position;
            _distanceToTarget = delta.magnitude;
            if (_distanceToTarget <= _hideDistance)
            {
                _label.gameObject.SetActive(false);
                if (_cue != null) _cue.gameObject.SetActive(false);
                SetScentBreadcrumbsVisible(false);
                return;
            }

            var dir = ((Vector2)delta).normalized;
            UpdateScentBreadcrumbs(dir);
            var cuePosition = new Vector3(dir.x * 1.15f, dir.y * 1.15f + 0.72f, -0.18f);
            float emphasisPulse = _emphasized ? 0.5f + 0.5f * Mathf.Sin(Time.time * 6f) : 0f;
            if (_cue != null)
            {
                _cue.gameObject.SetActive(true);
                _cue.transform.localPosition = cuePosition;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                _cue.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                _cue.color = _emphasized
                    ? Color.Lerp(Color.white, new Color(1f, 0.82f, 0.2f), 0.4f + emphasisPulse * 0.4f)
                    : Color.white;
            }

            _label.transform.localPosition = cuePosition + new Vector3(0f, -0.62f, -0.02f);
            _label.transform.rotation = Quaternion.identity;
            float zoomScale = Camera.main != null ? Mathf.Clamp(Camera.main.orthographicSize / 7.5f, 1f, 3.2f) : 1f;
            _label.transform.localScale = _baseScale * zoomScale;
            if (_cue != null)
                _cue.transform.localScale = _cueBaseScale * Mathf.Min(zoomScale, 1.8f) * (1f + emphasisPulse * 0.18f);
            _label.text = $"{_copy}  {Mathf.CeilToInt(_distanceToTarget)}m";
            ApplyTextVisibility();
        }

        private void BuildScentBreadcrumbs()
        {
            Sprite pawSprite = FinalGameplayArt.Load(FinalGameplayArt.CueTargetPaw);
            if (pawSprite == null)
            {
                _scentBreadcrumbs = System.Array.Empty<SpriteRenderer>();
                return;
            }

            var root = new GameObject(ScentBreadcrumbRootName);
            root.transform.SetParent(transform);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            _scentBreadcrumbs = new SpriteRenderer[ScentBreadcrumbSlots];
            for (int i = 0; i < _scentBreadcrumbs.Length; i++)
            {
                var breadcrumb = new GameObject($"ScentPaw_{i + 1}");
                breadcrumb.transform.SetParent(root.transform);
                breadcrumb.transform.localPosition = Vector3.zero;
                breadcrumb.transform.localRotation = Quaternion.identity;
                breadcrumb.transform.localScale = Vector3.one * 0.24f;

                var renderer = breadcrumb.AddComponent<SpriteRenderer>();
                renderer.sprite = pawSprite;
                renderer.sortingOrder = 19;
                renderer.color = new Color(_scentColor.r, _scentColor.g, _scentColor.b, 0.28f);
                breadcrumb.SetActive(false);
                _scentBreadcrumbs[i] = renderer;
            }
        }

        private void UpdateScentBreadcrumbs(Vector2 direction)
        {
            if (_scentBreadcrumbs == null || _scentBreadcrumbs.Length == 0) return;

            // The breadcrumbs only reveal the first few metres. They give players a dog-authentic
            // directional hint without drawing a complete GPS line to an undiscovered objective.
            float usableDistance = _distanceToTarget - Mathf.Max(_hideDistance, 0.8f);
            float heading = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            for (int i = 0; i < _scentBreadcrumbs.Length; i++)
            {
                var breadcrumb = _scentBreadcrumbs[i];
                if (breadcrumb == null) continue;

                float stepDistance = 1.65f + i * 1.18f;
                bool visible = usableDistance >= stepDistance + 0.35f;
                breadcrumb.gameObject.SetActive(visible);
                if (!visible) continue;

                float side = i % 2 == 0 ? -0.13f : 0.13f;
                Vector2 perpendicular = new Vector2(-direction.y, direction.x);
                breadcrumb.transform.localPosition = (Vector3)(direction * stepDistance + perpendicular * side) +
                                                     new Vector3(0f, 0f, -0.12f);
                breadcrumb.transform.localRotation = Quaternion.Euler(0f, 0f, heading + (i % 2 == 0 ? -9f : 9f));

                float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 4.2f - i * 0.85f);
                float scale = 0.22f + wave * 0.035f;
                breadcrumb.transform.localScale = Vector3.one * scale;
                float alpha = (0.24f - i * 0.035f) + wave * 0.09f;
                if (_emphasized) alpha = Mathf.Min(1f, alpha * 2.1f);
                breadcrumb.color = new Color(_scentColor.r, _scentColor.g, _scentColor.b, alpha);
            }
        }

        private void SetScentBreadcrumbsVisible(bool visible)
        {
            if (_scentBreadcrumbs == null) return;
            foreach (var breadcrumb in _scentBreadcrumbs)
                if (breadcrumb != null) breadcrumb.gameObject.SetActive(visible);
        }

        private void ApplyTextVisibility()
        {
            if (_label == null) return;
            _label.gameObject.SetActive(_debugTextVisible && _target != null && _distanceToTarget > _hideDistance);
        }
    }
}
