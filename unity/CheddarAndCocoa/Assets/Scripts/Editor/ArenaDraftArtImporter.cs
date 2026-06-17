using UnityEditor;

namespace CheddarAndCocoa.EditorTools
{
    /// <summary>
    /// Keeps the first DRAFT import lightweight and consistent without touching unrelated textures.
    /// </summary>
    public sealed class ArenaDraftArtImporter : AssetPostprocessor
    {
        private const string DraftArtRoot = "Assets/Art/Resources/ArenaDraft/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(DraftArtRoot, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 256f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
