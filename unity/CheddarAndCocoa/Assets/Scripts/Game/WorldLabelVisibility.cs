using CheddarAndCocoa.Dogs;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Keeps world labels as short-range prompts in production while letting the playtest overlay
    /// restore them as a full debug map.
    /// </summary>
    public sealed class WorldLabelVisibility : MonoBehaviour
    {
        public const float DefaultPromptRange = 3.2f;

        private static readonly Transform[] EmptyTargets = new Transform[0];
        private static Transform[] _promptTargets = EmptyTargets;
        private static bool _debugVisible;

        private TextMesh _label;
        private MeshRenderer _labelRenderer;
        private SpriteRenderer _skinRenderer;
        private float _promptRange = DefaultPromptRange;
        private bool _visible;

        public bool IsVisible => _visible;
        public float PromptRange => _promptRange;
        public static bool DebugVisible => _debugVisible;

        public static WorldLabelVisibility Attach(TextMesh label, float promptRange = DefaultPromptRange)
        {
            if (label == null) return null;

            var visibility = label.GetComponent<WorldLabelVisibility>();
            if (visibility == null) visibility = label.gameObject.AddComponent<WorldLabelVisibility>();
            visibility.Init(label, promptRange);
            return visibility;
        }

        public static void SetPromptTargets(DogController[] dogs)
        {
            if (dogs == null || dogs.Length == 0)
            {
                _promptTargets = EmptyTargets;
                RefreshAll();
                return;
            }

            var targets = new Transform[dogs.Length];
            for (int i = 0; i < dogs.Length; i++)
            {
                targets[i] = dogs[i] != null ? dogs[i].transform : null;
            }

            _promptTargets = targets;
            RefreshAll();
        }

        public static void SetDebugVisible(bool visible)
        {
            _debugVisible = visible;
            RefreshAll();
        }

        public void Init(TextMesh label, float promptRange)
        {
            _label = label;
            _promptRange = Mathf.Max(0.2f, promptRange);
            CacheRenderers();
            ApplyVisibility();
        }

        private void Awake()
        {
            if (_label == null) _label = GetComponent<TextMesh>();
            CacheRenderers();
            ApplyVisibility();
        }

        private void OnEnable()
        {
            CacheRenderers();
            ApplyVisibility();
        }

        private void LateUpdate()
        {
            CacheRenderers();
            ApplyVisibility();
        }

        private static void RefreshAll()
        {
            var labels = FindObjectsByType<WorldLabelVisibility>(FindObjectsSortMode.None);
            foreach (var label in labels)
            {
                label.ApplyVisibility();
            }
        }

        private void CacheRenderers()
        {
            if (_label == null) return;
            if (_labelRenderer == null) _labelRenderer = _label.GetComponent<MeshRenderer>();

            if (_skinRenderer == null)
            {
                Transform skin = _label.transform.Find(WorldLabelSkin.BackgroundName);
                if (skin != null) _skinRenderer = skin.GetComponent<SpriteRenderer>();
            }
        }

        private void ApplyVisibility()
        {
            bool visible = _debugVisible || AnyPromptTargetNear();
            _visible = visible;

            if (_labelRenderer != null) _labelRenderer.enabled = visible;
            if (_skinRenderer != null) _skinRenderer.enabled = visible;
        }

        private bool AnyPromptTargetNear()
        {
            if (_promptTargets == null || _promptTargets.Length == 0) return false;

            Vector3 position = transform.position;
            float rangeSqr = _promptRange * _promptRange;
            for (int i = 0; i < _promptTargets.Length; i++)
            {
                Transform target = _promptTargets[i];
                if (target == null) continue;

                Vector3 delta = target.position - position;
                if (delta.sqrMagnitude <= rangeSqr) return true;
            }

            return false;
        }
    }
}
