using UnityEngine;

namespace CheddarAndCocoa.Game
{
    public static class SpriteShapeCache
    {
        private static Sprite _whiteSquare;

        public static Sprite WhiteSquare
        {
            get
            {
                if (_whiteSquare != null) return _whiteSquare;
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = "RuntimeWhiteSquareTexture",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply(false, false);
                _whiteSquare = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 1f);
                _whiteSquare.name = "RuntimeWhiteSquareSprite";
                return _whiteSquare;
            }
        }
    }
}
