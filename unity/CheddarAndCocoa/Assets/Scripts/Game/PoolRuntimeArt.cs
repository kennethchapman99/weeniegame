using System.Collections.Generic;
using UnityEngine;

namespace CheddarAndCocoa.Game
{
    /// <summary>
    /// Runtime-generated pool sprites. Couch test #4: the floaties were drawn with the BarkRing
    /// paw-print VFX sprite and read as bark targets, not pool toys — this draws an actual
    /// inner-tube donut (colored tube, darker seam wedges, gloss arc) instead. Also provides the
    /// translucent waterline band that sits over a swimming dog so they read as *in* the water.
    /// Cached like <see cref="SpriteShapeCache"/>; deterministic, no runtime RNG.
    /// </summary>
    public static class PoolRuntimeArt
    {
        private static readonly Dictionary<Color, Sprite> _donuts = new Dictionary<Color, Sprite>();
        private static Sprite _waterBand;

        public static Sprite Donut(Color tube)
        {
            if (_donuts.TryGetValue(tube, out Sprite cached) && cached != null) return cached;

            const int size = 256;
            const float outer = 0.98f;
            const float inner = 0.44f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "PoolDonutTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color seam = Color.Lerp(tube, new Color(0.1f, 0.1f, 0.14f), 0.35f);
            Color gloss = Color.Lerp(tube, Color.white, 0.55f);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / size * 2f - 1f;
                    float ny = (y + 0.5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    // Soft alpha edges (~2px) at both tube boundaries.
                    float edge = 2f / (size * 0.5f);
                    float alpha = Mathf.Clamp01((outer - r) / edge) * Mathf.Clamp01((r - inner) / edge);
                    if (alpha <= 0f)
                    {
                        pixels[y * size + x] = Color.clear;
                        continue;
                    }

                    // Tube shading: darker toward both rims so it reads as an inflated torus.
                    float mid = (outer + inner) * 0.5f;
                    float half = (outer - inner) * 0.5f;
                    float bulge = 1f - Mathf.Pow(Mathf.Abs(r - mid) / half, 2f) * 0.35f;

                    // Four classic seam wedges at the diagonals.
                    float angle = Mathf.Atan2(ny, nx) * Mathf.Rad2Deg + 180f;
                    float wedge = Mathf.Abs(Mathf.DeltaAngle(angle % 90f, 45f));
                    Color body = wedge < 14f ? seam : tube;

                    // Gloss arc on the upper-left of the tube.
                    float glossAngle = Mathf.Abs(Mathf.DeltaAngle(angle, 315f));
                    if (glossAngle < 38f && r > mid)
                        body = Color.Lerp(body, gloss, (1f - glossAngle / 38f) * 0.7f);

                    body *= bulge;
                    pixels[y * size + x] = new Color(body.r, body.g, body.b, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = "PoolDonutSprite";
            _donuts[tube] = sprite;
            return sprite;
        }

        /// <summary>Soft horizontal ellipse laid over a swimming dog's lower body as the waterline.</summary>
        public static Sprite WaterBand()
        {
            if (_waterBand != null) return _waterBand;

            const int w = 128, h = 64;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                name = "PoolWaterBandTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w * 2f - 1f;
                    float ny = (y + 0.5f) / h * 2f - 1f;
                    float d = nx * nx + ny * ny;
                    float alpha = Mathf.Clamp01(1f - d) * 0.85f;
                    pixels[y * w + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            _waterBand = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 64f);
            _waterBand.name = "PoolWaterBandSprite";
            return _waterBand;
        }
    }
}
