using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Help.EditorTools
{
    // 픽셀아트 PNG를 프로젝트 규격으로 저장/임포트하는 공용 지점.
    //
    // 플레이스홀더 생성기(SpriteGenerator)와 AI 아트 후처리(PixelArtPipeline)가 같은 설정을 써야
    // 두 경로에서 나온 스프라이트가 같은 격자에 정렬된다. 설정이 두 곳에 복제되면 반드시 어긋난다.
    public static class PixelImport
    {
        // 32px = 월드 1유닛 = 타일 한 칸. 이 값은 방 좌표계 전체의 기준이므로 바꾸지 않는다.
        // 캐릭터를 크게 보이고 싶으면 PPU가 아니라 스프라이트의 픽셀 크기를 키운다
        // (예: 보스 96x96 → 화면에서 3x3칸).
        public const int PixelsPerUnit = 32;

        public const string SpriteDir = "Assets/Sprites";
        public const string TileDir = "Assets/Tilemaps";

        public static Sprite SavePng(string name, Color32[] pixels, int width, int height,
                                     string directory = SpriteDir)
        {
            EnsureFolder(directory);

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();

            string path = $"{directory}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            ApplyImportSettings(path);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // 픽셀아트 임포트 규격. 하나라도 어긋나면 화면에서 흐려지거나 격자가 밀린다.
        public static void ApplyImportSettings(string assetPath, int pixelsPerUnit = PixelsPerUnit,
                                               SpriteImportMode mode = SpriteImportMode.Single)
        {
            var imp = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (imp == null) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = mode;
            imp.spritePixelsPerUnit = pixelsPerUnit;
            imp.filterMode = FilterMode.Point;              // 보간 금지 — 픽셀이 뭉개진다
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }

        // 타일 에셋을 없으면 만들고, 스프라이트를 물린다.
        public static Tile EnsureTile(string tileName, Sprite sprite, Tile.ColliderType collider = Tile.ColliderType.Grid)
        {
            EnsureFolder(TileDir);
            string path = $"{TileDir}/{tileName}.asset";

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.color = Color.white;      // 색 틴트 제거 — 텍스처 원색 표시
            tile.colliderType = collider;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
