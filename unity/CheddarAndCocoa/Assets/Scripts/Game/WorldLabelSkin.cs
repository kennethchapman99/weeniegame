using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Skins mission world labels and transient score pops with generated transparent sprites while
    /// preserving the TextMesh as the authoritative gameplay/readability string.
    /// </summary>
    public sealed class WorldLabelSkin : MonoBehaviour
    {
        public const string BackgroundName = "GeneratedWorldLabelSkin";

        private TextMesh _label;
        private SpriteRenderer _background;
        private bool _scorePop;
        private string _lastText = string.Empty;
        private string _lastPath = string.Empty;

        public bool HasGeneratedSkin => _background != null && _background.sprite != null;
        public string SkinSpriteName => HasGeneratedSkin ? _background.sprite.name : string.Empty;

        public static bool GeneratedWorldLabelSkinAvailable =>
            FinalGameplayArt.Has(FinalGameplayArt.WorldLabelBubble)
            && FinalGameplayArt.Has(FinalGameplayArt.WorldLabelCommand)
            && FinalGameplayArt.Has(FinalGameplayArt.WorldLabelWarning)
            && FinalGameplayArt.Has(FinalGameplayArt.WorldPopBurst);

        public static WorldLabelSkin Attach(TextMesh label, bool scorePop)
        {
            if (label == null) return null;

            var skin = label.GetComponent<WorldLabelSkin>();
            if (skin == null) skin = label.gameObject.AddComponent<WorldLabelSkin>();
            skin.Init(label, scorePop);
            return skin;
        }

        public void Init(TextMesh label, bool scorePop)
        {
            _label = label;
            _scorePop = scorePop;

            var mesh = _label.GetComponent<MeshRenderer>();
            if (mesh != null) mesh.sortingOrder = scorePop ? 46 : 32;

            EnsureBackground();
            Refresh(force: true);
        }

        private void LateUpdate()
        {
            Refresh(force: false);
        }

        private void EnsureBackground()
        {
            if (_background != null) return;

            Transform existing = transform.Find(BackgroundName);
            if (existing != null)
            {
                _background = existing.GetComponent<SpriteRenderer>();
                return;
            }

            var go = new GameObject(BackgroundName);
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, -0.03f, 0.08f);
            go.transform.localRotation = Quaternion.identity;
            _background = go.AddComponent<SpriteRenderer>();
            _background.sortingOrder = _scorePop ? 42 : 28;
            _background.color = Color.white;
        }

        private void Refresh(bool force)
        {
            if (_label == null || _background == null) return;

            string text = _label.text ?? string.Empty;
            string path = SelectSpritePath(text, _scorePop);
            if (force || text != _lastText || path != _lastPath)
            {
                var sprite = FinalGameplayArt.Load(path);
                if (sprite != null)
                {
                    _background.sprite = sprite;
                    _background.color = ColorFor(text, _scorePop);
                    _background.sortingOrder = _scorePop ? 42 : 28;
                    _lastPath = path;
                }

                _lastText = text;
            }

            ResizeForText(text);
        }

        public static string SelectSpritePath(string text, bool scorePop)
        {
            if (scorePop) return FinalGameplayArt.WorldPopBurst;

            string upper = (text ?? string.Empty).ToUpperInvariant();
            if (upper.Contains("FAIL") || upper.Contains("MISS") || upper.Contains("WARNING")
                || upper.Contains("PREDATOR") || upper.Contains("SHADOW") || upper.Contains("NEEDS"))
                return FinalGameplayArt.WorldLabelWarning;

            if (upper.Contains("BARK") || upper.Contains("HOLD") || upper.Contains("STAND")
                || upper.Contains("TUG") || upper.Contains("RESCUE") || upper.Contains("READY")
                || upper.Contains("LOCKED") || upper.Contains("CHECKPOINT"))
                return FinalGameplayArt.WorldLabelCommand;

            return FinalGameplayArt.WorldLabelBubble;
        }

        private static Color ColorFor(string text, bool scorePop)
        {
            if (scorePop) return Color.white;

            string upper = (text ?? string.Empty).ToUpperInvariant();
            if (upper.Contains("FAIL") || upper.Contains("MISS") || upper.Contains("WARNING")
                || upper.Contains("PREDATOR") || upper.Contains("SHADOW") || upper.Contains("NEEDS"))
                return new Color(1f, 0.94f, 0.9f, 0.94f);
            if (upper.Contains("BARK") || upper.Contains("HOLD") || upper.Contains("STAND")
                || upper.Contains("TUG") || upper.Contains("RESCUE") || upper.Contains("READY")
                || upper.Contains("LOCKED") || upper.Contains("CHECKPOINT"))
                return new Color(1f, 0.98f, 0.84f, 0.94f);
            return new Color(0.9f, 1f, 1f, 0.92f);
        }

        private void ResizeForText(string text)
        {
            if (_background.sprite == null) return;

            int maxLine = 1;
            int lineCount = 1;
            int current = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n')
                {
                    if (current > maxLine) maxLine = current;
                    current = 0;
                    lineCount++;
                }
                else
                {
                    current++;
                }
            }
            if (current > maxLine) maxLine = current;

            float width = Mathf.Clamp(maxLine * 0.075f + (_scorePop ? 0.72f : 0.46f), _scorePop ? 0.95f : 0.82f, _scorePop ? 2.7f : 4.0f);
            float height = Mathf.Clamp(lineCount * 0.31f + (_scorePop ? 0.42f : 0.2f), _scorePop ? 0.82f : 0.42f, _scorePop ? 1.7f : 1.8f);
            if (!_scorePop && SelectSpritePath(text, false) == FinalGameplayArt.WorldLabelWarning)
            {
                height = Mathf.Max(height, 0.86f);
            }

            Vector3 parentScale = transform.lossyScale;
            Vector3 spriteSize = _background.sprite.bounds.size;
            float parentX = Mathf.Max(0.001f, Mathf.Abs(parentScale.x));
            float parentY = Mathf.Max(0.001f, Mathf.Abs(parentScale.y));
            float spriteX = Mathf.Max(0.001f, spriteSize.x);
            float spriteY = Mathf.Max(0.001f, spriteSize.y);
            _background.transform.localScale = new Vector3(width / (spriteX * parentX), height / (spriteY * parentY), 1f);
        }
    }
}
