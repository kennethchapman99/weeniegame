using System.Collections.Generic;
using UnityEngine;
using CheddarAndCocoa.Game;

namespace CheddarAndCocoa.Dogs
{
    /// <summary>
    /// Converts the imported draft pose atlases into tightly cropped, transparent runtime sprites.
    /// The source sheets use a near-white background, so a small color-distance key removes only
    /// that backdrop while preserving fur, outlines, and soft contact shadows.
    /// </summary>
    public static class ArenaDogPoseSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite For(DogId dog, DogReadabilityFeedback.Pose pose)
        {
            string key = $"{dog}_{pose}";
            if (Cache.TryGetValue(key, out var cached)) return cached;

            var atlasId = dog == DogId.Cheddar
                ? ArenaDraftArt.SpriteId.CheddarPoses
                : ArenaDraftArt.SpriteId.CocoaPoses;
            Texture2D atlas = ArenaDraftArt.LoadTexture(atlasId);
            if (atlas == null || !atlas.isReadable) return null;

            RectInt rect = RectFor(dog, pose);
            rect.x = Mathf.Clamp(rect.x, 0, atlas.width - 1);
            rect.y = Mathf.Clamp(rect.y, 0, atlas.height - 1);
            rect.width = Mathf.Clamp(rect.width, 1, atlas.width - rect.x);
            rect.height = Mathf.Clamp(rect.height, 1, atlas.height - rect.y);

            Color[] pixels = atlas.GetPixels(rect.x, rect.y, rect.width, rect.height);
            var background = new Color(0.965f, 0.957f, 0.945f, 1f);
            for (int i = 0; i < pixels.Length; i++)
            {
                Color c = pixels[i];
                float distance = Mathf.Sqrt(
                    (c.r - background.r) * (c.r - background.r) +
                    (c.g - background.g) * (c.g - background.g) +
                    (c.b - background.b) * (c.b - background.b));
                float saturation = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));
                c.a = Mathf.Clamp01(Mathf.InverseLerp(0.025f, 0.13f, distance + saturation * 0.22f));
                pixels[i] = c;
            }

            var texture = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false)
            {
                name = $"{key}_Texture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0, 0, rect.width, rect.height),
                new Vector2(0.5f, 0.28f), 300f, 0, SpriteMeshType.Tight);
            sprite.name = $"{key}_AuthoredPose";
            Cache[key] = sprite;
            return sprite;
        }

        private static RectInt RectFor(DogId dog, DogReadabilityFeedback.Pose pose)
        {
            if (dog == DogId.Cheddar)
            {
                return pose switch
                {
                    DogReadabilityFeedback.Pose.Idle => new RectInt(20, 700, 460, 350),
                    DogReadabilityFeedback.Pose.Run => new RectInt(470, 675, 520, 375),
                    DogReadabilityFeedback.Pose.Bark => new RectInt(965, 675, 520, 375),
                    DogReadabilityFeedback.Pose.Tug => new RectInt(0, 315, 510, 410),
                    DogReadabilityFeedback.Pose.Stunned => new RectInt(760, 0, 620, 355),
                    DogReadabilityFeedback.Pose.Rescued => new RectInt(950, 300, 535, 420),
                    DogReadabilityFeedback.Pose.Proud => new RectInt(950, 300, 535, 420),
                    DogReadabilityFeedback.Pose.Sad => new RectInt(170, 0, 610, 370),
                    _ => new RectInt(20, 700, 460, 350)
                };
            }

            return pose switch
            {
                DogReadabilityFeedback.Pose.Idle => new RectInt(20, 610, 355, 405),
                DogReadabilityFeedback.Pose.Run => new RectInt(350, 610, 390, 405),
                DogReadabilityFeedback.Pose.Bark => new RectInt(710, 610, 380, 405),
                DogReadabilityFeedback.Pose.Tug => new RectInt(1030, 610, 418, 405),
                DogReadabilityFeedback.Pose.Stunned => new RectInt(1030, 45, 418, 520),
                DogReadabilityFeedback.Pose.Rescued => new RectInt(350, 45, 390, 520),
                DogReadabilityFeedback.Pose.Proud => new RectInt(350, 45, 390, 520),
                DogReadabilityFeedback.Pose.Sad => new RectInt(700, 45, 390, 520),
                _ => new RectInt(20, 610, 355, 405)
            };
        }
    }
}
