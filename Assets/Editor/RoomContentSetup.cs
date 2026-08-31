using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Help.Dungeon;

namespace Help.Editor
{
    // 데이터 기반 방 콘텐츠 파이프라인 셋업: 콘텐츠 프리팹(방 유형별) + RoomContentLibrary 생성,
    // RoomManager에 연결, 그리고 씬에 손배치된 데모(Enemy/BreakableWall/IceWall)를 제거해
    // "콘텐츠는 데이터에서 스폰"으로 전환한다. (빌딩블록 프리팹이 먼저 있어야 함)
    public static class RoomContentSetup
    {
        const string Dir = "Assets/Prefabs/RoomContent";
        const string LibPath = "Assets/ScriptableObjects/RoomContentLibrary.asset";

        [MenuItem("Help/Setup/Setup Data-Driven Room Content")]
        public static void Setup()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/Prefabs", "RoomContent");

            var enemy = Load("Assets/Prefabs/Enemy.prefab");
            var breakable = Load("Assets/Prefabs/BreakableWall.prefab");
            var ice = Load("Assets/Prefabs/IceWall.prefab");
            if (enemy == null || breakable == null || ice == null)
            {
                Debug.LogError("[Help] 빌딩블록 프리팹 없음 — 'Create Building Block Prefabs' 먼저 실행.");
                return;
            }

            // 전투 방: 3종(그런트/아처/브루트)을 배치. 유형 프리팹이 없으면 기본 Enemy로 대체.
            var grunt = Load("Assets/Prefabs/Enemy_Grunt.prefab") ?? enemy;
            var archer = Load("Assets/Prefabs/Enemy_Archer.prefab") ?? enemy;
            var brute = Load("Assets/Prefabs/Enemy_Brute.prefab") ?? enemy;
            if (grunt == enemy)
                Debug.LogWarning("[Help] 적 유형 프리팹 없음 — 'Create Enemy Type Prefabs' 먼저 실행하면 3종이 배치됩니다.");

            // 전투 방: 적 전멸 게이트(EnemyClearObjective + RoomPuzzle)를 루트에 부착 → 다 죽여야 출구 열림.
            var combat = BuildContent("Room_Combat", new (GameObject, Vector3)[] {
                (grunt, new Vector3(2f, -2f, 0f)),
                (archer, new Vector3(6f, -2f, 0f)),
                (brute, new Vector3(-3f, -2f, 0f)),
            }, withClearGate: true);
            var puzzle = BuildContent("Room_Puzzle", new (GameObject, Vector3)[] {
                (breakable, new Vector3(2f, -2f, 0f)),
                (ice, new Vector3(-2f, -2f, 0f)),
            });

            // 보스 방: 보스 1기 + 적 전멸 게이트(보스 처치=방 클리어). 보스 프리팹 없으면 브루트로 대체.
            var boss = Load("Assets/Prefabs/Enemy_Boss.prefab") ?? brute;
            var bossRoom = BuildContent("Room_Boss", new (GameObject, Vector3)[] {
                (boss, new Vector3(0f, -1.5f, 0f)),
            }, withClearGate: true);

            // 보물/상점 방: 빈 방 방지용 재료 픽업 몇 개(상점 시스템 전까지 임시 보상 방).
            var treasure = BuildPickupContent("Room_Treasure", new (Help.Item.AlphabetMaterial, Vector3)[] {
                (Help.Item.AlphabetMaterial.A, new Vector3(-2f, -2.5f, 0f)),
                (Help.Item.AlphabetMaterial.S, new Vector3(0f, -2.5f, 0f)),
                (Help.Item.AlphabetMaterial.R, new Vector3(2f, -2.5f, 0f)),
            });

            // 튜토리얼 첫 방: 전투 없이 K·Y 글자를 주워 KEY를 만들도록 유도
            var tutorial = BuildTutorialContent("Room_Tutorial");

            // 특수방: 비-E 아이템 보상 상자 + 특수 제작대(그 앞에서만 E 없는 단어 조합 가능)
            var secret = BuildSecretContent("Room_Secret");

            // 라이브러리 구성(유형→콘텐츠)
            var lib = AssetDatabase.LoadAssetAtPath<RoomContentLibrary>(LibPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<RoomContentLibrary>(); AssetDatabase.CreateAsset(lib, LibPath); }
            else { // 재실행 시 중복 방지 위해 새로 만들기
                lib = ScriptableObject.CreateInstance<RoomContentLibrary>();
                AssetDatabase.DeleteAsset(LibPath);
                AssetDatabase.CreateAsset(lib, LibPath);
            }
            lib.AddEntry(RoomType.Combat, combat);
            lib.AddEntry(RoomType.EnvironmentPuzzle, puzzle);
            lib.AddEntry(RoomType.CombatPuzzle, puzzle);
            lib.AddEntry(RoomType.PurePuzzle, puzzle);
            lib.AddEntry(RoomType.Tutorial, tutorial);
            lib.AddEntry(RoomType.Boss, bossRoom);
            lib.AddEntry(RoomType.Treasure, treasure);
            lib.AddEntry(RoomType.Shop, treasure); // 상점 시스템 전까지 보물 방과 동일
            lib.AddEntry(RoomType.Secret, secret);
            EditorUtility.SetDirty(lib);

            // 씬 RoomManager에 연결 + 손배치 데모 제거
            var rm = Object.FindFirstObjectByType<RoomManager>();
            if (rm != null)
            {
                var so = new SerializedObject(rm);
                so.FindProperty("_contentLibrary").objectReferenceValue = lib;
                so.ApplyModifiedProperties();
            }

            // 튜토리얼: 빈 인벤토리로 시작(시딩 끔) → 첫 방에서 K·Y를 주워 KEY를 제작하게 유도
            var gm = Object.FindFirstObjectByType<Help.Core.GameManager>();
            if (gm != null)
            {
                var gso = new SerializedObject(gm);
                var seedProp = gso.FindProperty("_seedStarterMaterials");
                if (seedProp != null) seedProp.boolValue = false;
                gso.ApplyModifiedProperties();
            }
            RemoveSceneObject("Enemy");
            RemoveSceneObject("BreakableWall");
            RemoveSceneObject("IceWall");

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[Help] Data-driven room content set up: Room_Combat/Room_Puzzle prefabs + RoomContentLibrary, linked to RoomManager, hand-placed demos removed.");
        }

        static GameObject Load(string path)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) Debug.LogWarning($"[Help] prefab not found: {path}");
            return go;
        }

        // 콘텐츠 프리팹 = 방 중심(원점) 기준 자식 배치. 빌딩블록을 네스티드 프리팹 인스턴스로 담는다.
        // withClearGate=true면 루트에 적 전멸 게이트(EnemyClearObjective+RoomPuzzle)를 붙여 전투 방 출구를 잠근다.
        static GameObject BuildContent(string name, (GameObject prefab, Vector3 localPos)[] items, bool withClearGate = false)
        {
            var root = new GameObject(name);
            if (withClearGate)
            {
                root.AddComponent<Help.Enemy.EnemyClearObjective>();
                root.AddComponent<Help.Puzzle.RoomPuzzle>(); // 목표(EnemyClearObjective)를 자동 수집해 출구 게이팅
            }
            foreach (var (prefab, pos) in items)
            {
                var child = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = pos;
            }
            string path = $"{Dir}/{name}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Help] saved content prefab: {path}");
            return asset;
        }

        // 재료 픽업만 배치한 콘텐츠(보물/상점 방 등 전투 없는 보상 방).
        static GameObject BuildPickupContent(string name, (Help.Item.AlphabetMaterial mat, Vector3 pos)[] items)
        {
            var root = new GameObject(name);
            foreach (var (mat, pos) in items)
                CreatePickup(root.transform, mat, pos);
            string path = $"{Dir}/{name}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Help] saved content prefab: {path}");
            return asset;
        }

        // 튜토리얼 콘텐츠 = 방 중심 기준으로 K·Y 글자 줍기 + 잠긴 문(KEY로 열기)을 배치.
        // 흐름: K·Y 주움 → KEY 제작·장착 → 문에 Use(F/우클릭)로 Unlock 적용 → 출구 잠금 해제 → E로 다음 방.
        static GameObject BuildTutorialContent(string name)
        {
            var root = new GameObject(name);
            CreatePickup(root.transform, Help.Item.AlphabetMaterial.K, new Vector3(-2.5f, -2.5f, 0f));
            CreatePickup(root.transform, Help.Item.AlphabetMaterial.Y, new Vector3(2.5f, -2.5f, 0f));

            // 잠긴 문(Unlock 요구) + RoomPuzzle(문 해제 전까지 방 출구 잠금)
            var door = CreateLockedDoor(root.transform, new Vector3(4f, -2.5f, 0f));
            CreateRoomPuzzle(root.transform, door);

            string path = $"{Dir}/{name}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Help] saved content prefab: {path}");
            return asset;
        }

        // 특수방 콘텐츠 = 비-E 아이템 보상 상자 + 특수 제작대.
        // 특수방 자체가 확률 등장이라 클리어에 필요한 것은 절대 넣지 않는다(순수 보너스).
        static GameObject BuildSecretContent(string name)
        {
            var root = new GameObject(name);

            CreateMarker(root.transform, "RewardChest", new Vector3(-2.5f, -2.5f, 0f),
                "상자", new Color(1f, 0.84f, 0.2f), new Vector2(1.5f, 2f))
                .AddComponent<Help.Dungeon.SpecialRewardChest>();

            CreateMarker(root.transform, "CraftingStation", new Vector3(2.5f, -2.5f, 0f),
                "특수 제작대", new Color(0.6f, 0.8f, 1f), new Vector2(3f, 3f))
                .AddComponent<Help.Dungeon.SpecialCraftingStation>();

            string path = $"{Dir}/{name}.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[Help] saved content prefab: {path}");
            return asset;
        }

        // 트리거 콜라이더 + 라벨을 가진 상호작용 오브젝트(픽업/상자/제작대 공통 골격).
        static GameObject CreateMarker(Transform parent, string name, Vector3 localPos,
            string label, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = size;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = label;
            tm.characterSize = 0.15f;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

            return go;
        }

        // 잠긴 문: 능력 타깃(Unlock 요구) + 솔리드 콜라이더(플레이어가 문 앞에서 막혀 서게 해 F가 확실히 맞도록;
        // 해제 시 오브젝트가 비활성돼 통과 가능) + 글자 표시.
        // ※ 트리거로 두면 플레이어가 문을 뚫고 지나가 벽까지 걸어가서 전방 Use 스캔이 문을 놓친다(2026-07-23 수정).
        static Help.Puzzle.CapabilityTarget CreateLockedDoor(Transform parent, Vector3 localPos)
        {
            var go = new GameObject("LockedDoor");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = false; // 솔리드 — 플레이어를 막아 세운다
            col.size = new Vector2(1f, 2f);

            var target = go.AddComponent<Help.Puzzle.CapabilityTarget>();
            var so = new SerializedObject(target);
            var capProp = so.FindProperty("_requiredCapability");
            if (capProp != null) capProp.enumValueIndex = (int)Help.Item.Capability.Unlock; // 값==인덱스(연속 enum)
            so.ApplyModifiedPropertiesWithoutUndo();

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = "잠긴 문";
            tm.characterSize = 0.15f;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.4f, 0.4f);
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10;

            return target;
        }

        // 방 퍼즐: 잠긴 문을 목표로 등록 → 미해결 시 방 출구를 잠근다(RoomManager는 런타임 자동 탐색).
        static void CreateRoomPuzzle(Transform parent, Help.Puzzle.CapabilityTarget target)
        {
            var go = new GameObject("RoomPuzzle");
            go.transform.SetParent(parent, false);

            var puzzle = go.AddComponent<Help.Puzzle.RoomPuzzle>();
            var so = new SerializedObject(puzzle);
            var targets = so.FindProperty("_targets");
            targets.arraySize = 1;
            targets.GetArrayElementAtIndex(0).objectReferenceValue = target;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // 글자 줍기 오브젝트: 트리거 콜라이더 + MaterialPickup + 글자 표시(TextMesh).
        static void CreatePickup(Transform parent, Help.Item.AlphabetMaterial mat, Vector3 localPos)
        {
            var go = new GameObject("Pickup_" + mat);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.5f, 3f); // 세로로 길게 — 걸어 지나가면 확실히 줍히도록

            var pickup = go.AddComponent<Help.Item.MaterialPickup>();
            pickup.Material = mat;
            EditorUtility.SetDirty(pickup);

            var textGo = new GameObject("Letter");
            textGo.transform.SetParent(go.transform, false);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = mat.ToString();
            tm.characterSize = 0.2f;
            tm.fontSize = 64;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.yellow;
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 10; // 타일맵 위에 보이도록
        }

        static void RemoveSceneObject(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) { Object.DestroyImmediate(go); Debug.Log($"[Help] removed hand-placed '{name}' from scene"); }
        }
    }
}
