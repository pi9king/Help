using UnityEditor;
using UnityEngine;

namespace Help.EditorTools
{
    // Assets/Resources/ArmRig/의 텍스처는 ArmRigPreview가 런타임에 Texture2D로 읽어 Sprite.Create 한다.
    // 압축·밉맵이 걸리면 픽셀이 뭉개지므로, 임포트될 때마다 점 필터·무압축으로 맞춘다.
    public class ArmRigTextureImport : AssetPostprocessor
    {
        private const string Folder = "Assets/Resources/ArmRig/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
        }
    }
}
