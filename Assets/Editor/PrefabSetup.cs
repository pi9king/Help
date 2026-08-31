using UnityEditor;
using UnityEngine;

namespace Help.Editor
{
    // 콘텐츠 개발용 빌딩블록 프리팹 생성. 씬의 검증된 오브젝트를 프리팹으로 저장 →
    // 이후 방/퍼즐을 프리팹 드래그로 배치하거나 RoomTemplate에 담을 수 있다.
    public static class PrefabSetup
    {
        const string Dir = "Assets/Prefabs";

        [MenuItem("Help/Setup/Create Building Block Prefabs")]
        public static void CreateAll()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            SaveFromScene("BreakableWall");
            SaveFromScene("IceWall");
            SaveFromScene("Enemy");
            CreateRoomPuzzlePrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Help] Building block prefabs created in Assets/Prefabs (BreakableWall, IceWall, Enemy, RoomPuzzle).");
        }

        // 씬의 검증된 오브젝트를 그대로 프리팹 에셋으로 저장(설정·자식 포함).
        static void SaveFromScene(string name)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                Debug.LogWarning($"[Help] scene object '{name}' not found — 프리팹 생략. (해당 데모가 씬에 있어야 저장됨)");
                return;
            }
            string path = $"{Dir}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log($"[Help] saved prefab: {path}");
        }

        // 빈 RoomPuzzle 템플릿(목표 리스트는 인스턴스별로 채움).
        static void CreateRoomPuzzlePrefab()
        {
            string path = $"{Dir}/RoomPuzzle.prefab";
            var go = new GameObject("RoomPuzzle");
            go.AddComponent<Help.Puzzle.RoomPuzzle>();
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            Debug.Log($"[Help] saved prefab: {path}");
        }
    }
}
