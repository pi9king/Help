using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Help.Dungeon;

namespace Help.EditorTools
{
    // Assets/Rooms의 ASCII 지형 템플릿을 게임에 연결한다.
    //
    // 규약(D-13·D-17): Assets/Rooms/Floor{N}/{RoomType}_{SizeClass}_{NN}[_draft].txt
    //   층    = 폴더명        (예: Floor2/)
    //   유형  = 파일명 첫 토큰 (예: Combat)
    //   작업중 = _draft 접미사 — 등록도 검증도 건너뛴다
    // 파일명의 크기(Small)는 사람이 읽기 위한 것이고, 실제 크기는 격자를 파싱해서 나온다(D-11).
    //
    // 규약 덕분에 새 템플릿을 폴더에 넣고 이 메뉴를 다시 돌리면 바로 던전에 등장한다.
    public static class RoomTemplateSetup
    {
        private const string RoomsDir = "Assets/Rooms";
        private const string LibraryPath = "Assets/ScriptableObjects/RoomTemplateLibrary.asset";

        [MenuItem("Help/Setup/Setup Room Templates")]
        public static void Run()
        {
            var library = BuildLibrary();
            if (library == null) return;

            WireScene(library);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
        }

        private static RoomTemplateLibrary BuildLibrary()
        {
            if (!AssetDatabase.IsValidFolder(RoomsDir))
            {
                Debug.LogError($"[Help] {RoomsDir} 폴더가 없습니다. Tools/gen_rooms.py로 템플릿을 만드세요.");
                return null;
            }

            PixelImport.EnsureFolder("Assets/ScriptableObjects");
            var library = AssetDatabase.LoadAssetAtPath<RoomTemplateLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<RoomTemplateLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            library.Clear();

            // FindAssets는 하위 폴더까지 훑으므로 Floor1/Floor2/… 가 한 번에 들어온다.
            var paths = AssetDatabase.FindAssets("t:TextAsset", new[] { RoomsDir })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".txt"))
                .OrderBy(p => p)
                .ToList();

            int added = 0, skipped = 0, drafts = 0;
            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset == null) continue;

                // 작업 중인 방은 등록하지 않는다 — 검증 테스트도 같은 규칙으로 건너뛴다.
                if (RoomTemplateNaming.IsDraft(asset.name)) { drafts++; continue; }

                if (!TryResolveType(asset.name, out var type))
                {
                    Debug.LogWarning($"[Help] '{asset.name}'의 방 유형을 알 수 없습니다. " +
                                     "파일명은 {RoomType}_{SizeClass}_{NN} 형식이어야 합니다.");
                    skipped++;
                    continue;
                }

                // 깨진 템플릿을 라이브러리에 넣으면 런타임에야 알게 된다 — 여기서 걸러낸다.
                var parsed = RoomTemplateParser.Parse(asset.text, asset.name);
                if (!parsed.Success)
                {
                    Debug.LogError($"[Help] 템플릿 오류로 등록하지 않음:\n{string.Join("\n", parsed.Errors)}");
                    skipped++;
                    continue;
                }

                library.AddEntry(RoomTemplateNaming.FloorOf(path), type, asset);
                added++;
            }

            EditorUtility.SetDirty(library);
            Debug.Log($"[Help] 방 템플릿 {added}개 등록" +
                      (drafts > 0 ? $", 작업중 {drafts}개 제외" : "") +
                      (skipped > 0 ? $", 오류로 건너뜀 {skipped}개" : "") +
                      $" → {LibraryPath}");
            return library;
        }

        private static bool TryResolveType(string fileName, out RoomType type)
        {
            type = RoomType.Combat;
            int i = fileName.IndexOf('_');
            if (i <= 0) return false;
            return System.Enum.TryParse(fileName.Substring(0, i), out type);
        }

        // 씬의 RoomManager에 템플릿 라이브러리와 위험 바닥 타일을 연결한다.
        private static void WireScene(RoomTemplateLibrary library)
        {
            var manager = Object.FindFirstObjectByType<RoomManager>(FindObjectsInactive.Include);
            if (manager == null)
            {
                Debug.LogWarning("[Help] 씬에 RoomManager가 없습니다. 씬을 연 상태에서 다시 실행하세요.");
                return;
            }

            var so = new SerializedObject(manager);
            SetRef(so, "_templateLibrary", library);
            SetRef(so, "_hazardTile", LoadTile("HazardTile"));
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(manager);
            Debug.Log("[Help] RoomManager에 템플릿 라이브러리와 지형 타일을 연결했습니다.");
        }

        private static TileBase LoadTile(string name) =>
            AssetDatabase.LoadAssetAtPath<Tile>($"{PixelImport.TileDir}/{name}.asset");

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogWarning($"[Help] RoomManager.{field} 필드를 찾지 못했습니다."); return; }
            if (value == null) { Debug.LogWarning($"[Help] {field}에 넣을 에셋이 없습니다. " +
                                                  "먼저 Help/Setup/Generate Placeholder Sprites를 실행하세요."); return; }
            prop.objectReferenceValue = value;
        }
    }
}
