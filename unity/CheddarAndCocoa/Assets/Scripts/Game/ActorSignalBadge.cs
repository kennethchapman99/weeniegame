using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Icon-only urgency signal above a mission actor, drawn with the authored world-label skins
    /// and no text. Unlike the contextual TextMesh prompts (near-range or debug overlay only),
    /// the badge stays visible at any distance while an urgent state is active, so couch players
    /// read "act now" across the yard without parsing a sentence.
    /// </summary>
    public sealed class ActorSignalBadge : MonoBehaviour
    {
        public const string BadgeName = "ActorSignalBadgeIcon";

        /// <summary>
        /// SetActorState's pulse amplitude doubles as the authored urgency channel: states that
        /// demand immediate player action pulse at 0.26+, ambient/aftermath states stay below.
        /// </summary>
        public const float UrgentPulseThreshold = 0.26f;

        private const float WorldHeight = 0.72f;
        private const float WorldYOffset = 1.15f;

        private SpriteRenderer _icon;
        private float _phase;

        public bool IsShowing => _icon != null && _icon.enabled && _icon.sprite != null;
        public string IconSpriteName => IsShowing ? _icon.sprite.name : string.Empty;

        public static ActorSignalBadge Attach(GameObject actor)
        {
            if (actor == null) return null;
            var badge = actor.GetComponent<ActorSignalBadge>();
            if (badge == null) badge = actor.AddComponent<ActorSignalBadge>();
            badge.EnsureIcon();
            return badge;
        }

        public void Apply(string label, float pulse)
        {
            EnsureIcon();
            if (pulse < UrgentPulseThreshold)
            {
                Hide();
                return;
            }

            string path = WorldLabelSkin.SelectSpritePath(label, scorePop: false);
            // Urgency must never read as a calm speech bubble; unclassified urgent states fall
            // back to the warning skin.
            if (path == FinalGameplayArt.WorldLabelBubble) path = FinalGameplayArt.WorldLabelWarning;

            Sprite sprite = FinalGameplayArt.Load(path);
            if (sprite == null)
            {
                Hide();
                return;
            }

            _icon.sprite = sprite;
            _icon.enabled = true;
        }

        public void Hide()
        {
            if (_icon != null) _icon.enabled = false;
        }

        private void EnsureIcon()
        {
            if (_icon != null) return;

            Transform existing = transform.Find(BadgeName);
            if (existing == null)
            {
                var go = new GameObject(BadgeName);
                go.transform.SetParent(transform);
                existing = go.transform;
            }

            _icon = existing.GetComponent<SpriteRenderer>();
            if (_icon == null) _icon = existing.gameObject.AddComponent<SpriteRenderer>();
            _icon.sortingOrder = 34;
            _icon.enabled = false;
            _phase = (GetInstanceID() & 511) / 511f * Mathf.PI * 2f;
        }

        private void LateUpdate()
        {
            if (!IsShowing) return;

            // Own the badge's world pose completely: the actor root pulses, sways, and carries
            // per-actor scale, none of which should distort a fixed-size overhead signal.
            float bob = Mathf.Sin(Time.time * 6f + _phase) * 0.08f;
            Transform icon = _icon.transform;
            icon.rotation = Quaternion.identity;
            icon.position = transform.position + new Vector3(0f, WorldYOffset + bob, -0.22f);

            Vector3 size = _icon.sprite.bounds.size;
            float throb = 1f + Mathf.Sin(Time.time * 6.5f + _phase) * 0.05f;
            float worldScale = WorldHeight / Mathf.Max(0.001f, size.y) * throb;
            Vector3 lossy = icon.parent != null ? icon.parent.lossyScale : Vector3.one;
            icon.localScale = new Vector3(
                worldScale / Mathf.Max(0.001f, Mathf.Abs(lossy.x)),
                worldScale / Mathf.Max(0.001f, Mathf.Abs(lossy.y)), 1f);
        }
    }
}
