using UnityEditor;
using UnityEngine;

public class SpritePostProcessor : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath.Contains("Assets/Sprites/"))
        {
            TextureImporter textureImporter = (TextureImporter)assetImporter;
            textureImporter.textureType = TextureImporterType.Sprite;
            textureImporter.spriteImportMode = SpriteImportMode.Single;
            textureImporter.alphaIsTransparency = true;
        }
    }
}

