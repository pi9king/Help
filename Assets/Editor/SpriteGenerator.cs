using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Help.Editor
{
    // 임시 내장 스프라이트(색 틴트된 사각형)를 실제 PNG 스프라이트 에셋으로 교체.
    // 프로시저럴 픽셀아트(32x32, Point 필터, PPU 32). 테마: 플레이어 = 알파벳 E.
    public static class SpriteGenerator
    {
        const int S = 32;
        const string Dir = "Assets/Sprites";

        [MenuItem("Help/Setup/Generate Placeholder Sprites")]
        public static void GenerateAll()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets", "Sprites");

            var player = SaveSprite("player_E", BuildPlayerE());
            var enemy = SaveSprite("enemy_blob", BuildEnemy());
            var floor = SaveSprite("tile_floor", BuildFloor());
            var wall = SaveSprite("tile_wall", BuildWall());
            var doorOpen = SaveSprite("tile_door_open", BuildDoor(true));
            var doorLocked = SaveSprite("tile_door_locked", BuildDoor(false));

            AssignTile("Assets/Tilemaps/FloorTile.asset", floor);
            AssignTile("Assets/Tilemaps/WallTile.asset", wall);
            AssignTile("Assets/Tilemaps/DoorOpenTile.asset", doorOpen);
            AssignTile("Assets/Tilemaps/DoorLockedTile.asset", doorLocked);

            AssignSceneSprite("Player", player);
            AssignSceneEnemy(enemy);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Help] Placeholder sprites generated and assigned (player_E, enemy, floor, wall, doors).");
        }

        // --- 픽셀 드로잉 ---

        static Color32[] New(Color32 fill)
        {
            var px = new Color32[S * S];
            for (int i = 0; i < px.Length; i++) px[i] = fill;
            return px;
        }

        static void Rect(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
        {
            for (int y = Mathf.Max(0, y0); y <= Mathf.Min(S - 1, y1); y++)
                for (int x = Mathf.Max(0, x0); x <= Mathf.Min(S - 1, x1); x++)
                    px[y * S + x] = c;
        }

        static void Disc(Color32[] px, float cx, float cy, float r, Color32 c)
        {
            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x - cx, dy = y - cy;
                    if (dx * dx + dy * dy <= r * r) px[y * S + x] = c;
                }
        }

        static readonly Color32 Clear = new Color32(0, 0, 0, 0);

        static Color32[] BuildPlayerE()
        {
            var px = New(Clear);
            var outline = new Color32(20, 60, 90, 255);
            var body = new Color32(79, 195, 247, 255); // sky blue
            // E: 두꺼운 글리프 (외곽선 먼저, 안쪽 채움)
            // 세로 기둥
            Rect(px, 7, 3, 13, 28, outline);
            Rect(px, 8, 4, 12, 27, body);
            // 위/중간/아래 가로 바 (외곽선+채움)
            Rect(px, 7, 23, 25, 28, outline); Rect(px, 8, 24, 24, 27, body); // top
            Rect(px, 7, 13, 22, 18, outline); Rect(px, 8, 14, 21, 17, body); // middle
            Rect(px, 7, 3, 25, 8, outline);   Rect(px, 8, 4, 24, 7, body);   // bottom
            return px;
        }

        static Color32[] BuildEnemy()
        {
            var px = New(Clear);
            var outline = new Color32(90, 15, 15, 255);
            var body = new Color32(229, 57, 53, 255); // red
            Disc(px, 15.5f, 14f, 13f, outline);
            Disc(px, 15.5f, 14f, 11.5f, body);
            // 눈 (흰자 + 검은 눈동자) — 성난 인상
            Rect(px, 9, 16, 13, 20, new Color32(255, 255, 255, 255));
            Rect(px, 18, 16, 22, 20, new Color32(255, 255, 255, 255));
            Rect(px, 11, 16, 13, 18, new Color32(20, 20, 20, 255));
            Rect(px, 18, 16, 20, 18, new Color32(20, 20, 20, 255));
            // 찡그린 입
            Rect(px, 11, 9, 20, 10, new Color32(90, 15, 15, 255));
            return px;
        }

        static Color32[] BuildFloor()
        {
            var px = New(new Color32(109, 76, 65, 255)); // brown dirt
            Rect(px, 0, 25, 31, 31, new Color32(56, 142, 60, 255)); // grass top
            Rect(px, 0, 24, 31, 24, new Color32(46, 110, 48, 255)); // grass shadow line
            // 흙 얼룩
            var dark = new Color32(84, 58, 50, 255);
            Rect(px, 4, 6, 6, 8, dark); Rect(px, 20, 12, 23, 14, dark);
            Rect(px, 13, 4, 15, 6, dark); Rect(px, 26, 18, 28, 20, dark);
            return px;
        }

        static Color32[] BuildWall()
        {
            var px = New(new Color32(90, 90, 102, 255)); // stone gray
            var mortar = new Color32(58, 58, 66, 255);
            // 벽돌 줄눈: 가로 줄
            Rect(px, 0, 10, 31, 11, mortar);
            Rect(px, 0, 21, 31, 22, mortar);
            // 세로 줄눈 (엇갈림)
            Rect(px, 15, 0, 16, 10, mortar);
            Rect(px, 7, 11, 8, 21, mortar); Rect(px, 23, 11, 24, 21, mortar);
            Rect(px, 15, 22, 16, 31, mortar);
            return px;
        }

        static Color32[] BuildDoor(bool open)
        {
            var px = New(Clear);
            var frame = open ? new Color32(76, 175, 80, 255) : new Color32(198, 40, 40, 255);
            var inner = open ? new Color32(30, 70, 32, 255) : new Color32(80, 20, 20, 255);
            // 아치형 문틀
            Rect(px, 6, 2, 25, 29, frame);
            Rect(px, 9, 2, 22, 25, inner);
            Disc(px, 15.5f, 25f, 7f, frame);
            Disc(px, 15.5f, 25f, 5f, inner);
            if (open)
                Rect(px, 14, 12, 17, 20, new Color32(160, 240, 160, 255)); // 열린 빛줄기
            else
            {
                // 자물쇠(고리+몸통)
                Rect(px, 13, 14, 18, 20, new Color32(240, 220, 120, 255));
                Rect(px, 14, 20, 17, 23, new Color32(240, 220, 120, 255));
                Rect(px, 15, 16, 16, 18, new Color32(80, 20, 20, 255));
            }
            return px;
        }

        // --- PNG 저장 + 임포트 ---

        static Sprite SaveSprite(string name, Color32[] px)
        {
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            string path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spritePixelsPerUnit = S;      // 32px = 1 world unit (타일 1칸)
            imp.filterMode = FilterMode.Point; // 픽셀아트 선명하게
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void AssignTile(string tilePath, Sprite spr)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null) { Debug.LogWarning($"[Help] tile not found: {tilePath}"); return; }
            tile.sprite = spr;
            tile.color = Color.white; // 색 틴트 제거 — 텍스처 원색 표시
            EditorUtility.SetDirty(tile);
        }

        static void AssignSceneSprite(string tag, Sprite spr)
        {
            var go = GameObject.FindWithTag(tag);
            if (go == null) { Debug.LogWarning($"[Help] scene object tag '{tag}' not found"); return; }
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { Debug.LogWarning($"[Help] '{tag}' has no SpriteRenderer"); return; }
            sr.sprite = spr;
            sr.color = Color.white;
            EditorUtility.SetDirty(go);
        }

        static void AssignSceneEnemy(Sprite spr)
        {
            var enemy = Object.FindFirstObjectByType<Help.Enemy.EnemyBase>(FindObjectsInactive.Include);
            if (enemy == null) { Debug.LogWarning("[Help] scene EnemyBase not found"); return; }
            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr == null) { Debug.LogWarning("[Help] enemy has no SpriteRenderer"); return; }
            sr.sprite = spr;
            sr.color = Color.white;
            EditorUtility.SetDirty(enemy.gameObject);
        }
    }
}
