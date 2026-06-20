using System.Collections.Generic;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Runtime extraction bridge from final transparent art or draft sheets to actual gameplay sprites.
    /// If final art exists under Resources/ArenaFinal it wins. Otherwise the factory falls back to draft
    /// sheet crops, and then to null so generated gameplay geometry remains safe.
    /// </summary>
    public static class RuntimeArtSpriteFactory
    {
        /// <summary>
        /// Compatibility helper for the bark/juice layer: attaches a cosmetic SpriteRenderer child loaded
        /// from a final resource path, without touching gameplay renderers or bounds. Returns null when the
        /// sprite is absent so callers can fall back to draft badges.
        /// </summary>
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

        public enum RuntimeSpriteId
        {
            Squirrel,
            SquirrelSteal,
            SquirrelScared,
            EagleThreat,
            PredatorAttack,
            CoyoteThreat,
            BackyardBush,
            BackyardFence,
            BackyardRock,
            GrassPatch,
            DigSpot,
            DogBowl,
            BarkBurst,
            BarkRing,
            PickupSparkle,
            SuccessPop,
            WarningAlert,
            RescueBurst,
            FailPuff,
            RopeToy,
            RopeComplete,
            WeenieCollectible
        }

        private static readonly Dictionary<RuntimeSpriteId, Sprite> Cache = new Dictionary<RuntimeSpriteId, Sprite>();

        public static Sprite Get(RuntimeSpriteId id)
        {
            if (Cache.TryGetValue(id, out var cached)) return cached;

            Sprite sprite = Build(id);
            Cache[id] = sprite;
            return sprite;
        }

        public static bool Has(RuntimeSpriteId id) => Get(id) != null;

        public static void ClearCacheForTests() => Cache.Clear();

        private static Sprite Build(RuntimeSpriteId id)
        {
            Sprite final = FinalGameplayArt.Load(id);
            if (final != null) return final;

            switch (id)
            {
                case RuntimeSpriteId.Squirrel:
                case RuntimeSpriteId.SquirrelSteal:
                case RuntimeSpriteId.SquirrelScared:
                    return FullSprite(ArenaDraftArt.SpriteId.SquirrelCharacter, id, 220f, new Vector2(0.5f, 0.25f));
                case RuntimeSpriteId.EagleThreat:
                case RuntimeSpriteId.PredatorAttack:
                    return FullSprite(ArenaDraftArt.SpriteId.EagleReference, id, 230f, new Vector2(0.5f, 0.35f));
                case RuntimeSpriteId.CoyoteThreat:
                    return FullSprite(ArenaDraftArt.SpriteId.CoyoteReference, id, 230f, new Vector2(0.5f, 0.25f));
                case RuntimeSpriteId.BackyardBush:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.05f, 0.48f, 0.22f, 0.28f), 220f, new Vector2(0.5f, 0.2f));
                case RuntimeSpriteId.BackyardFence:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.28f, 0.48f, 0.24f, 0.25f), 220f, new Vector2(0.5f, 0.2f));
                case RuntimeSpriteId.BackyardRock:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.54f, 0.48f, 0.18f, 0.2f), 220f, new Vector2(0.5f, 0.2f));
                case RuntimeSpriteId.GrassPatch:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.74f, 0.48f, 0.2f, 0.18f), 220f, new Vector2(0.5f, 0.18f));
                case RuntimeSpriteId.DigSpot:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.52f, 0.2f, 0.2f, 0.16f), 220f, new Vector2(0.5f, 0.25f));
                case RuntimeSpriteId.DogBowl:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.74f, 0.2f, 0.2f, 0.18f), 220f, new Vector2(0.5f, 0.25f));
                case RuntimeSpriteId.BarkBurst:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.03f, 0.58f, 0.25f, 0.28f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.BarkRing:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.03f, 0.28f, 0.25f, 0.24f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.PickupSparkle:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.3f, 0.58f, 0.2f, 0.25f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.SuccessPop:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.52f, 0.58f, 0.22f, 0.25f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.WarningAlert:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.75f, 0.58f, 0.2f, 0.25f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.RescueBurst:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.30f, 0.28f, 0.2f, 0.24f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.FailPuff:
                    return SheetSlice(ArenaDraftArt.SpriteId.Vfx, id, new Rect(0.52f, 0.28f, 0.22f, 0.24f), 220f, new Vector2(0.5f, 0.5f));
                case RuntimeSpriteId.RopeToy:
                case RuntimeSpriteId.RopeComplete:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.05f, 0.2f, 0.22f, 0.18f), 220f, new Vector2(0.5f, 0.35f));
                case RuntimeSpriteId.WeenieCollectible:
                    return SheetSlice(ArenaDraftArt.SpriteId.BackyardProps, id, new Rect(0.30f, 0.2f, 0.20f, 0.16f), 220f, new Vector2(0.5f, 0.35f));
                default:
                    return null;
            }
        }

        private static Sprite FullSprite(ArenaDraftArt.SpriteId source, RuntimeSpriteId id, float pixelsPerUnit, Vector2 pivot)
        {
            var texture = ArenaDraftArt.LoadTexture(source);
            if (texture == null) return null;
            if (!texture.isReadable)
            {
                var direct = ArenaDraftArt.LoadSprite(source);
                if (direct != null) return direct;
                return null;
            }
            return CreateKeyedSprite(texture, new RectInt(0, 0, texture.width, texture.height), id.ToString(), pixelsPerUnit, pivot);
        }

        private static Sprite SheetSlice(ArenaDraftArt.SpriteId source, RuntimeSpriteId id, Rect normalizedRect, float pixelsPerUnit, Vector2 pivot)
        {
            var texture = ArenaDraftArt.LoadTexture(source);
            if (texture == null || !texture.isReadable) return null;

            int x = Mathf.Clamp(Mathf.RoundToInt(normalizedRect.x * texture.width), 0, texture.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(normalizedRect.y * texture.height), 0, texture.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(normalizedRect.width * texture.width), 1, texture.width - x);
            int h = Mathf.Clamp(Mathf.RoundToInt(normalizedRect.height * texture.height), 1, texture.height - y);
            return CreateKeyedSprite(texture, new RectInt(x, y, w, h), id.ToString(), pixelsPerUnit, pivot);
        }

        private static Sprite CreateKeyedSprite(Texture2D source, RectInt rect, string name, float pixelsPerUnit, Vector2 pivot)
        {
            Color[] pixels = source.GetPixels(rect.x, rect.y, rect.width, rect.height);
            Color key = EstimateCornerKey(pixels, rect.width, rect.height);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float distance = Mathf.Sqrt(
                    (c.r - key.r) * (c.r - key.r) +
                    (c.g - key.g) * (c.g - key.g) +
                    (c.b - key.b) * (c.b - key.b));
                float saturation = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                c.a = Mathf.Clamp01(Mathf.InverseLerp(0.035f, 0.16f, distance + saturation * 0.24f));
                pixels[i] = c;
            }

            var tex = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false)
            {
                name = $"{name}_RuntimeTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, rect.width, rect.height), pivot, pixelsPerUnit, 0, SpriteMeshType.Tight);
            sprite.name = $"{name}_RuntimeSprite";
            return sprite;
        }

        private static Color EstimateCornerKey(Color[] pixels, int width, int height)
        {
            Color a = pixels[0];
            Color b = pixels[Mathf.Max(0, width - 1)];
            Color c = pixels[Mathf.Max(0, (height - 1) * width)];
            Color d = pixels[Mathf.Max(0, height * width - 1)];
            return (a + b + c + d) * 0.25f;
        }
    }
}
