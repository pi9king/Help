using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Help.EditorTools
{
    // AI가 뽑은 그림 → 게임에 쓸 픽셀 스프라이트.
    //
    // 사용법
    //   1. 생성한 이미지를 프로젝트 루트의 ArtSource/raw/ 에 넣는다 (Assets 밖이라 Unity가 임포트하지 않는다)
    //   2. 파일명으로 목표 크기를 지정한다:  player_E@32x48.png
    //      - '@WxH' 생략 시 32x32
    //      - 이름 끝에 '-o'를 붙이면 1픽셀 아웃라인:  grunt@32x48-o.png
    //   3. Help ▸ Art ▸ Process Art Source
    //   4. 결과가 Assets/Sprites/{이름}.png 로 저장되고 임포트 설정까지 맞춰진다
    //
    // 도구에 무관하다 — 전용 픽셀아트 AI든 범용 이미지 AI든 같은 파이프라인을 탄다.
    // 자세한 프롬프트는 Docs/ART_PIPELINE.md.
    public static class PixelArtPipeline
    {
        private const string SourceDir = "ArtSource/raw";
        private const string PalettePath = "ArtSource/palette.png";

        // 파일명 규약: name[@WxH][-o]
        private static readonly Regex NamePattern =
            new Regex(@"^(?<name>.+?)(?:@(?<w>\d+)x(?<h>\d+))?(?<outline>-o)?$", RegexOptions.Compiled);

        [MenuItem("Help/Art/Process Art Source")]
        public static void ProcessAll()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            string src = Path.Combine(root, SourceDir);

            if (!Directory.Exists(src))
            {
                Directory.CreateDirectory(src);
                Debug.LogWarning($"[Help] {SourceDir} 폴더를 만들었습니다. AI로 만든 이미지를 여기에 넣고 다시 실행하세요.\n" +
                                 "프롬프트는 Docs/ART_PIPELINE.md 참고.");
                return;
            }

            var palette = LoadPalette(Path.Combine(root, PalettePath));
            var files = Directory.GetFiles(src)
                .Where(f => f.EndsWith(".png") || f.EndsWith(".jpg") || f.EndsWith(".jpeg"))
                .OrderBy(f => f).ToList();

            if (files.Count == 0)
            {
                Debug.LogWarning($"[Help] {SourceDir}에 처리할 이미지가 없습니다.");
                return;
            }

            int done = 0;
            foreach (var file in files)
            {
                if (Process(file, palette)) done++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Help] 아트 {done}/{files.Count}개 처리 완료 → {PixelImport.SpriteDir}" +
                      (palette == null ? "\n(팔레트 없음 — ArtSource/palette.png를 두면 색을 통일합니다)" : ""));
        }

        // Assets/Sprites의 PNG 전부에 픽셀아트 임포트 규격을 다시 적용한다.
        // 파이프라인을 거치지 않고 직접 넣은 파일(외부 툴·스크립트 산출물)을 정리할 때 쓴다.
        [MenuItem("Help/Art/Fix Sprite Import Settings")]
        public static void FixImportSettings()
        {
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { PixelImport.SpriteDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".png"))
                .ToList();

            foreach (var p in paths) PixelImport.ApplyImportSettings(p);

            AssetDatabase.Refresh();
            Debug.Log($"[Help] 스프라이트 {paths.Count}개의 임포트 설정을 규격(PPU {PixelImport.PixelsPerUnit}, Point, 무압축)으로 맞췄습니다.");
        }

        private static bool Process(string path, IReadOnlyList<Color32> palette)
        {
            var raw = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!raw.LoadImage(File.ReadAllBytes(path)))
            {
                Debug.LogError($"[Help] 읽을 수 없는 이미지: {path}");
                Object.DestroyImmediate(raw);
                return false;
            }

            var match = NamePattern.Match(Path.GetFileNameWithoutExtension(path));
            string name = match.Groups["name"].Value;
            int targetW = match.Groups["w"].Success ? int.Parse(match.Groups["w"].Value) : PixelImport.PixelsPerUnit;
            int targetH = match.Groups["h"].Success ? int.Parse(match.Groups["h"].Value) : PixelImport.PixelsPerUnit;
            bool outline = match.Groups["outline"].Success;

            var px = raw.GetPixels32();
            int w = raw.width, h = raw.height;
            Object.DestroyImmediate(raw);

            // 1) 배경 제거 — 이미 투명하면 건드리지 않는다.
            if (!HasTransparency(px))
            {
                if (HasColor(px, PixelArtOps.MagentaKey)) PixelArtOps.KeyOutColor(px, PixelArtOps.MagentaKey);
                else PixelArtOps.FloodFillBackground(px, w, h);
            }

            // 2) 여백 제거 — 안 하면 축소할 때 캐릭터만 작아진다.
            px = PixelArtOps.Trim(px, w, h, out w, out h);

            // 3) 아웃라인은 축소 전에 넣으면 뭉개진다 → 축소 후에.
            int gridW = outline ? targetW - 2 : targetW;
            int gridH = outline ? targetH - 2 : targetH;
            if (gridW < 1 || gridH < 1) { Debug.LogError($"[Help] {name}: 목표 크기가 너무 작습니다."); return false; }

            px = PixelArtOps.DownscaleMode(px, w, h, gridW, gridH);
            w = gridW; h = gridH;

            // 4) 팔레트 통일 — 여러 에셋을 한 게임의 그림으로 묶어 주는 단계.
            PixelArtOps.QuantizeToPalette(px, palette);

            if (outline)
            {
                px = PixelArtOps.AddOutline(px, w, h, new Color32(16, 16, 24, 255), out w, out h);
            }

            // Unity 텍스처는 아래에서 위로 저장된다 — 원본이 위에서 아래이므로 뒤집는다.
            px = FlipVertical(px, w, h);

            PixelImport.SavePng(name, px, w, h);
            return true;
        }

        private static bool HasTransparency(Color32[] px)
        {
            foreach (var c in px) if (c.a < 250) return true;
            return false;
        }

        private static bool HasColor(Color32[] px, Color32 target, int tolerance = 40)
        {
            int hits = 0;
            foreach (var c in px) if (PixelArtOps.Distance(c, target) <= tolerance) hits++;
            // 몇 픽셀 우연히 일치한 것과 "배경이 이 색"인 것을 구분한다.
            return hits > px.Length / 20;
        }

        private static Color32[] FlipVertical(Color32[] px, int w, int h)
        {
            var outPx = new Color32[px.Length];
            for (int y = 0; y < h; y++)
                System.Array.Copy(px, y * w, outPx, (h - 1 - y) * w, w);
            return outPx;
        }

        // 팔레트는 가로 한 줄짜리 색 띠 PNG. 이미지 편집기로 직접 갈아 끼울 수 있다.
        private static IReadOnlyList<Color32> LoadPalette(string path)
        {
            if (!File.Exists(path)) return null;

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(File.ReadAllBytes(path))) { Object.DestroyImmediate(tex); return null; }

            var seen = new HashSet<int>();
            var colors = new List<Color32>();
            foreach (var c in tex.GetPixels32())
            {
                if (c.a < 128) continue;
                int key = (c.r << 16) | (c.g << 8) | c.b;
                if (seen.Add(key)) colors.Add(c);
            }
            Object.DestroyImmediate(tex);
            return colors.Count > 0 ? colors : null;
        }
    }
}
