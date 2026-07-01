using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Generated ring + label used only when a bark, tug, or rescue range is actionable.
    /// </summary>
    public sealed class InteractionRangeIndicator : MonoBehaviour
    {
        private GameObject _root;
        private GameObject _labelRoot;
        private SpriteRenderer _ring;
        private TextMesh _label;
        private Sprite _fallbackRingSprite;
        private Color _baseColor = Color.white;
        private float _radius = 1f;
        private string _labelText = string.Empty;
        private static bool _debugTextVisible;

        public bool IsVisible => _root != null && _root.activeSelf;
        public float Radius => _radius;
        public string Label => IsVisible ? _labelText : string.Empty;
        public bool TextVisible => _labelRoot != null && _labelRoot.activeSelf;
        public bool UsesGeneratedCueArt => _ring != null && _ring.sprite != null && _ring.sprite != _fallbackRingSprite;
        public string CueSpriteName => _ring != null && _ring.sprite != null ? _ring.sprite.name : string.Empty;

        public static void SetDebugTextVisible(bool visible)
        {
            _debugTextVisible = visible;
            foreach (var indicator in Object.FindObjectsByType<InteractionRangeIndicator>(FindObjectsSortMode.None))
            {
                indicator.ApplyTextVisibility();
            }
        }

        public void Init(Sprite ringSprite, Color color, string label)
        {
            _baseColor = color;
            _fallbackRingSprite = ringSprite;

            _root = new GameObject("InteractionRangeIndicator");
            _root.transform.SetParent(transform);
            _root.transform.localPosition = new Vector3(0f, 0f, 0.06f);
            _root.transform.localRotation = Quaternion.identity;

            _ring = _root.AddComponent<SpriteRenderer>();
            _ring.sprite = SelectCueSprite(label) ?? ringSprite;
            _ring.sortingOrder = 3;

            _labelRoot = new GameObject("InteractionRangeLabel");
            _labelRoot.transform.SetParent(transform);
            _labelRoot.transform.localPosition = new Vector3(0f, 1.45f, -0.02f);
            _labelRoot.transform.localScale = Vector3.one * 0.08f;
            _label = _labelRoot.AddComponent<TextMesh>();
            _label.anchor = TextAnchor.MiddleCenter;
            _label.alignment = TextAlignment.Center;
            _label.fontSize = 18;
            if (_label.TryGetComponent<MeshRenderer>(out var labelRenderer)) labelRenderer.sortingOrder = 24;

            Show(1f, label, color);
            Hide();
        }

        public void Show(float radius, string label, Color color)
        {
            if (_root == null) return;

            _radius = Mathf.Max(0.1f, radius);
            _baseColor = color;
            _labelText = label;
            _root.SetActive(true);
            _root.transform.localScale = Vector3.one * (_radius * 2f);
            if (_labelRoot != null) _labelRoot.transform.localPosition = new Vector3(0f, _radius + 0.45f, -0.02f);

            if (_ring != null) _ring.color = _baseColor;
            if (_ring != null) _ring.sprite = SelectCueSprite(label) ?? _fallbackRingSprite;
            if (_label != null)
            {
                _label.text = label;
                Color labelColor = color;
                labelColor.a = 1f;
                _label.color = labelColor;
            }
            ApplyTextVisibility();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            if (_labelRoot != null) _labelRoot.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!IsVisible) return;

            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.035f;
            _root.transform.localScale = Vector3.one * (_radius * 2f * pulse);
            if (_labelRoot != null) _labelRoot.transform.localPosition = new Vector3(0f, _radius + 0.45f, -0.02f);
            if (_label != null) _label.transform.rotation = Quaternion.identity;
            ApplyTextVisibility();
        }

        private void ApplyTextVisibility()
        {
            if (_labelRoot != null) _labelRoot.SetActive(_debugTextVisible && IsVisible);
        }

        private static Sprite SelectCueSprite(string label)
        {
            string upper = string.IsNullOrEmpty(label) ? string.Empty : label.ToUpperInvariant();
            if (upper.Contains("BOTH") || upper.Contains("TUG"))
                return FinalGameplayArt.Load(FinalGameplayArt.CueTugRange);
            if (upper.Contains("RESCUE"))
                return FinalGameplayArt.Load(FinalGameplayArt.CueRescueRange);
            if (upper.Contains("BARK"))
                return FinalGameplayArt.Load(FinalGameplayArt.CueBarkRange);
            return FinalGameplayArt.Load(FinalGameplayArt.CueTargetPaw);
        }
    }
}
