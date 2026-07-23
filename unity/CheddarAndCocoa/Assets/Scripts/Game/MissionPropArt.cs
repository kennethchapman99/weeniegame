using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public static class MissionPropArt
    {
        public static MissionPropArtAttachment Attach(GameObject target, string resourcePath,
            Vector3 localScale, int sortingOrder, Color? tint = null, Vector3? localPosition = null,
            bool shadow = true, bool debugOnlyFallback = false)
        {
            if (target == null || string.IsNullOrEmpty(resourcePath)) return null;
            var attachment = target.GetComponent<MissionPropArtAttachment>();
            if (attachment == null) attachment = target.AddComponent<MissionPropArtAttachment>();
            bool loaded = attachment.Init(resourcePath, localPosition ?? new Vector3(0f, 0.12f, -0.28f),
                localScale, sortingOrder, tint ?? Color.white, shadow, debugOnlyFallback);
            if (loaded)
            {
                float maxAlpha = shadow ? 0.14f : 0.1f;
                attachment.CapFallbackAlpha(maxAlpha);
                DimGeneratedFallback(target, maxAlpha);
            }
            return loaded ? attachment : null;
        }

        public static MissionPropArtAttachment AttachObject(GameObject target, string resourcePath,
            float scale = 0.015f, int sortingOrder = 18, bool shadow = true)
        {
            return Attach(target, resourcePath, Vector3.one * scale, sortingOrder, Color.white,
                new Vector3(0f, 0.12f, -0.28f), shadow);
        }

        /// <summary>
        /// Attaches an object at a stable authored world width while cancelling any non-uniform
        /// scale on its controller-owned marker. The marker stays responsible for gameplay shape;
        /// the promoted sprite keeps its intended proportions.
        /// </summary>
        public static MissionPropArtAttachment AttachObjectAtWorldWidth(GameObject target, string resourcePath,
            float worldWidth, int sortingOrder = 18, bool shadow = true)
        {
            if (!TryGetWorldWidthScale(target, resourcePath, worldWidth, out Vector3 localScale))
                return null;
            return Attach(target, resourcePath, localScale, sortingOrder, Color.white,
                new Vector3(0f, 0.12f, -0.28f), shadow, true);
        }

        public static MissionPropArtAttachment AttachPad(GameObject target, string resourcePath,
            float scale = 0.013f, int sortingOrder = 12)
        {
            return Attach(target, resourcePath, Vector3.one * scale, sortingOrder,
                new Color(1f, 1f, 1f, 0.92f), new Vector3(0f, 0.1f, -0.28f), false, true);
        }

        /// <summary>
        /// Attaches a pad/state cue at a stable authored world width, independent of source texture
        /// resolution and the marker root's generated scale.
        /// </summary>
        public static MissionPropArtAttachment AttachPadAtWorldWidth(GameObject target, string resourcePath,
            float worldWidth, int sortingOrder = 12)
        {
            if (!TryGetWorldWidthScale(target, resourcePath, worldWidth, out Vector3 localScale))
                return null;
            return Attach(target, resourcePath, localScale, sortingOrder,
                new Color(1f, 1f, 1f, 0.92f), new Vector3(0f, 0.1f, -0.28f), false, true);
        }

        public static void SetSprite(MissionPropArtAttachment attachment, string resourcePath)
        {
            if (attachment != null) attachment.SetResource(resourcePath);
        }

        private static bool TryGetWorldWidthScale(GameObject target, string resourcePath,
            float worldWidth, out Vector3 localScale)
        {
            localScale = Vector3.one;
            if (target == null) return false;
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return false;

            float worldScale = Mathf.Max(0.01f, worldWidth) /
                Mathf.Max(0.01f, sprite.bounds.size.x);
            Vector3 parentScale = target.transform.lossyScale;
            localScale = new Vector3(
                worldScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.x)),
                worldScale / Mathf.Max(0.01f, Mathf.Abs(parentScale.y)),
                1f);
            return true;
        }

        private static void DimGeneratedFallback(GameObject target, float maxAlpha)
        {
            var renderer = FindFallbackRenderer(target);
            if (renderer == null) return;
            var color = renderer.color;
            color.a = Mathf.Min(color.a, maxAlpha);
            renderer.color = color;
        }

        /// <summary>
        /// The generated marker renderer a prop attachment should dim: the root renderer when one
        /// exists, otherwise the actor's PlaceholderBody child (actor roots stay uniformly scaled
        /// and carry their rig one level down).
        /// </summary>
        public static SpriteRenderer FindFallbackRenderer(GameObject target)
        {
            if (target == null) return null;
            if (target.TryGetComponent<SpriteRenderer>(out var renderer)) return renderer;
            var body = target.transform.Find(ArenaArtCatalog.PlaceholderBodyName);
            return body != null && body.TryGetComponent(out renderer) ? renderer : null;
        }
    }
}
