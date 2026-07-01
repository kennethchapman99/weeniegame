using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public static class MissionPropArt
    {
        public static MissionPropArtAttachment Attach(GameObject target, string resourcePath,
            Vector3 localScale, int sortingOrder, Color? tint = null, Vector3? localPosition = null,
            bool shadow = true)
        {
            if (target == null || string.IsNullOrEmpty(resourcePath)) return null;
            var attachment = target.GetComponent<MissionPropArtAttachment>();
            if (attachment == null) attachment = target.AddComponent<MissionPropArtAttachment>();
            bool loaded = attachment.Init(resourcePath, localPosition ?? new Vector3(0f, 0.12f, -0.28f),
                localScale, sortingOrder, tint ?? Color.white, shadow);
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

        public static MissionPropArtAttachment AttachPad(GameObject target, string resourcePath,
            float scale = 0.013f, int sortingOrder = 12)
        {
            return Attach(target, resourcePath, Vector3.one * scale, sortingOrder,
                new Color(1f, 1f, 1f, 0.92f), new Vector3(0f, 0.1f, -0.28f), false);
        }

        public static void SetSprite(MissionPropArtAttachment attachment, string resourcePath)
        {
            if (attachment != null) attachment.SetResource(resourcePath);
        }

        private static void DimGeneratedFallback(GameObject target, float maxAlpha)
        {
            if (target == null || !target.TryGetComponent<SpriteRenderer>(out var renderer)) return;
            var color = renderer.color;
            color.a = Mathf.Min(color.a, maxAlpha);
            renderer.color = color;
        }
    }
}
