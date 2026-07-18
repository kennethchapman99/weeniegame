using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Guidance-ladder role-turn beacon (G1.3): a small paw badge in the owning dog's identity
    /// color, floating over the active objective target so "whose turn is it" reads at the object
    /// itself. One shared instance owned by GameManager and repositioned to whatever target is
    /// currently owned, rather than attached as a child of it - the owning target changes
    /// GameObject across a mission's steps (e.g. Great Escape's stations).
    ///
    /// GameManager owns this component directly on its own GameObject (same pattern as PanicMeter),
    /// so everything here operates on a dedicated CHILD icon object - never on this.transform or
    /// this.gameObject, which would move/rescale/disable GameManager itself.
    /// </summary>
    public sealed class RoleTurnBeacon : MonoBehaviour
    {
        private const float WorldYOffset = 1.35f;

        private SpriteRenderer _icon;
        private Transform _iconRoot;
        private Transform _target;

        public bool IsShowing => _icon != null && _icon.enabled && _target != null;
        public bool UsesGeneratedArt => _icon != null && _icon.sprite != null;
        public Color CurrentTint => _icon != null ? _icon.color : Color.white;

        public void Init()
        {
            if (_icon != null) return;

            var go = new GameObject("RoleTurnBeaconIcon");
            go.transform.SetParent(transform);
            _iconRoot = go.transform;
            _icon = go.AddComponent<SpriteRenderer>();
            _icon.sprite = FinalGameplayArt.Load(FinalGameplayArt.CueRoleBeacon);
            _icon.sortingOrder = 35;
            _icon.enabled = false;
        }

        public void Show(Transform target, Color tint)
        {
            if (_icon == null) Init();
            if (_icon.sprite == null || target == null)
            {
                Hide();
                return;
            }

            _target = target;
            _icon.color = tint;
            _icon.enabled = true;
        }

        public void Hide()
        {
            _target = null;
            if (_icon != null) _icon.enabled = false;
        }

        private void LateUpdate()
        {
            if (_target == null || _icon == null || !_icon.enabled) return;

            float bob = Mathf.Sin(Time.time * 5f) * 0.06f;
            _iconRoot.position = _target.position + new Vector3(0f, WorldYOffset + bob, -0.2f);
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            _iconRoot.localScale = Vector3.one * 0.45f * pulse;
        }
    }
}
