using UnityEditor;
public class PixelImport : AssetPostprocessor {
  void OnPreprocessTexture() {
    var t = (TextureImporter)assetImporter;
    t.textureType = TextureImporterType.Default; t.filterMode = UnityEngine.FilterMode.Point;
    t.textureCompression = TextureImporterCompression.Uncompressed; t.mipmapEnabled = false;
    t.alphaIsTransparency = true; t.npotScale = TextureImporterNPOTScale.None; t.maxTextureSize = 8192;
  }
}
