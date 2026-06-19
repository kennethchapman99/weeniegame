using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>Creates cosmetic final-art overlays without changing gameplay renderers or bounds.</summary>
    public static class RuntimeArtSpriteFactory
    {
        public static SpriteRenderer AddOverlay(Transform parent, string name, string resourcePath,
            Vector3 localPosition, Vector3 localScale, int sortingOrder, Color? tint = null)
        {
            if (parent == null) return null;
            Transform existing = parent.Find(name);
            if (existing != null) return existing.GetComponent<SpriteRenderer>();
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (sprite == null) return null;

            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = tint ?? Color.white;
            return renderer;
        }

        public static SpriteRenderer AddWorldOverlay(Transform root, string name, string resourcePath,
            Vector3 worldPosition, float worldWidth, int sortingOrder)
        {
            Sprite sprite = FinalGameplayArt.Load(resourcePath);
            if (root == null || sprite == null) return null;
            Transform existing = root.Find(name);
            if (existing != null) return existing.GetComponent<SpriteRenderer>();

            var go = new GameObject(name);
            go.transform.SetParent(root);
            go.transform.position = worldPosition;
            float scale = sprite.bounds.size.x > 0.001f ? worldWidth / sprite.bounds.size.x : 1f;
            go.transform.localScale = Vector3.one * scale;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
