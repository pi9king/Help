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
            NormalizeObstacles();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Help] Building block prefabs created in Assets/Prefabs (BreakableWall, IceWall, Enemy, RoomPuzzle).");
        }

        // 능력 장애물을 **지형**으로 만든다. 씬 데모가 이미 제거돼(RoomContentSetup) SaveFromScene이
        // 벽을 다시 못 만들므로, 기존 에셋을 제자리에서 고친다. RoomContentSetup도 이걸 호출한다 —
        // 어느 메뉴를 돌리든 같은 상태가 되게.
        //
        // 고치는 두 가지 (2026-09-10 끼임 사고):
        //   1) Ground 레이어 — 안 그러면 밟고 서도 PlayerController가 접지로 안 본다
        //   2) 세로 2칸 + 밑면을 바닥에 붙임 — 뜬 틈이 있으면 위 발판과의 사이에 플레이어가 낀다
        public static void NormalizeObstacles()
        {
            int ground = LayerMask.NameToLayer("Ground");
            if (ground < 0)
            {
                Debug.LogError("[Help] Ground 레이어가 없음 — 장애물 레이어 정규화 생략");
                return;
            }

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                // 프리팹 에셋은 열어서 고치고 다시 저장한다(직접 변형은 저장 보장이 없다).
                var root = PrefabUtility.LoadPrefabContents(path);
                if (root == null) continue;
                try
                {
                    bool changed = false;
                    foreach (var target in root.GetComponentsInChildren<Help.Puzzle.CapabilityTarget>(true))
                    {
                        // 네스티드 프리팹 인스턴스(예: Room_Puzzle 안의 BreakableWall)는 건드리지 않는다.
                        // 원본을 고치면 따라오므로, 여기서 고치면 불필요한 오버라이드만 남는다.
                        if (PrefabUtility.IsPartOfPrefabInstance(target.gameObject)) continue;
                        changed |= NormalizeObstacle(target, ground);
                    }

                    if (changed)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        Debug.Log($"[Help] 장애물 정규화: {path}");
                    }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        // 장애물 하나를 규약에 맞춘다. 이미 맞으면 아무것도 바꾸지 않는다(false 반환).
        static bool NormalizeObstacle(Help.Puzzle.CapabilityTarget target, int groundLayer)
        {
            bool changed = false;
            var go = target.gameObject;

            if (go.layer != groundLayer) { go.layer = groundLayer; changed = true; }

            var box = go.GetComponent<BoxCollider2D>();
            if (box == null) return changed;

            // 밑면이 배치 높이만큼 내려와야 바닥에 선다. 스케일로 늘린 경우도 있으니 스케일까지 본다.
            float scaleY = go.transform.localScale.y;
            float bottom = (box.offset.y - box.size.y * 0.5f) * scaleY;
            if (Mathf.Abs(bottom + Help.Dungeon.RoomGeometry.ObstacleLocalY) < 0.001f) return changed;

            // 스프라이트도 같이 늘어나도록 크기는 스케일로 준다(콜라이더 size는 1×1 유지).
            box.offset = Vector2.zero;
            box.size = Vector2.one;
            var s = go.transform.localScale;
            s.y = Help.Dungeon.RoomGeometry.ObstacleLocalY * 2f;
            go.transform.localScale = s;
            return true;
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
