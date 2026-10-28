using UnityEditor;
using UnityEngine;

// Makes every texture under any Resources folder crisp (point filtered, uncompressed, readable).
public class PixelImport : AssetPostprocessor {
    void OnPreprocessTexture() {
        if (!assetPath.Contains("/Resources/")) return;
        TextureImporter t = (TextureImporter)assetImporter;
        t.textureType = TextureImporterType.Default;
        t.filterMode = FilterMode.Point;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.mipmapEnabled = false; t.alphaIsTransparency = true; t.npotScale = TextureImporterNPOTScale.None;
        t.maxTextureSize = 8192; t.isReadable = true; t.wrapMode = TextureWrapMode.Clamp;
    }
}
