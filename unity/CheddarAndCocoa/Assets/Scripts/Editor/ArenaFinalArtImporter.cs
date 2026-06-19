using UnityEditor;
using UnityEngine;

namespace CheddarAndCocoa.EditorTools
{
    /// <summary>Consistent, runtime-safe import settings for extracted transparent final sprites.</summary>
    public sealed class ArenaFinalArtImporter : AssetPostprocessor
    {
        private const string Root = "Assets/Art/Resources/ArenaFinal/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            if (assetPath.Contains("/Motion/", System.StringComparison.Ordinal))
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0.0625f);
                importer.SetTextureSettings(settings);
            }
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
