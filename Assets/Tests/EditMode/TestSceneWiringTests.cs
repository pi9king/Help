using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Help.Combat;
using Help.Dungeon;
using Help.Player;
using Help.UI;
using UnityEngine.EventSystems;

namespace Tests.EditMode
{
    // 씬 수준 배선 회귀 방지: 컴파일/EditMode 로직 테스트로는 잡히지 않는
    // "씬에 필요한 오브젝트가 실제로 존재하는가"를 검증한다.
    // (과거 Hitbox가 씬에 없어 공격이 무효였던 결함 D-A, RoomManager 미배치 결함 D-B 재발 방지)
    public class TestSceneWiringTests
    {
        private const string ScenePath = "Assets/Scenes/TestScene/TestScene.unity";

        [Test]
        public void TestScene_PlayerHasHitbox_ForAttackToDealDamage()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var player = GameObject.Find("Player");
                Assert.IsNotNull(player, "씬에 Player가 없음");
                Assert.IsNotNull(player.GetComponentInChildren<Hitbox>(),
                    "Player에 Hitbox가 없음 — 공격이 적에게 데미지를 줄 수 없음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void TestScene_HasRoomManager_ForDungeonToRunAtRuntime()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.IsNotNull(Object.FindFirstObjectByType<RoomManager>(),
                    "씬에 RoomManager가 없음 — 런타임에 던전/방 진입 시스템이 시작되지 않음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // 적/퍼즐은 씬 손배치 → 데이터 스폰(RoomContentLibrary)으로 전환됨.
        // 따라서 (a) RoomManager에 콘텐츠 라이브러리가 배선됐는지, (b) Enemy 빌딩블록 프리팹이
        // Hurtbox를 가져 공격을 받을 수 있는지를 검증한다.
        [Test]
        public void TestScene_RoomContentLibraryWired_AndEnemyPrefabHasHurtbox()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var rm = Object.FindFirstObjectByType<RoomManager>();
                Assert.IsNotNull(rm, "씬에 RoomManager 없음");
                Assert.IsNotNull(new SerializedObject(rm).FindProperty("_contentLibrary").objectReferenceValue,
                    "RoomManager._contentLibrary 미배선 — 방 콘텐츠(적/퍼즐)가 스폰되지 않음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }

            var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            Assert.IsNotNull(enemyPrefab, "Enemy 빌딩블록 프리팹 없음");
            Assert.IsNotNull(enemyPrefab.GetComponentInChildren<Hurtbox>(true),
                "Enemy 프리팹에 Hurtbox가 없음 — 공격을 받을 수 없음");
        }

        // 오브젝트 "존재"뿐 아니라 직렬화 값(마스크/참조)의 올바른 배선까지 검증한다.
        // (과거 _groundLayer 마스크가 비어 점프가 불능이던 결함 D1 재발 방지)
        [Test]
        public void TestScene_PlayerController_GroundLayerAndCheckAreWired()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var pc = Object.FindFirstObjectByType<PlayerController>();
                Assert.IsNotNull(pc, "씬에 PlayerController 없음");

                var so = new SerializedObject(pc);
                Assert.AreNotEqual(0, so.FindProperty("_groundLayer").intValue,
                    "_groundLayer 마스크가 비어 있음 — 접지 판정이 항상 false가 되어 점프 불능");
                Assert.IsNotNull(so.FindProperty("_groundCheck").objectReferenceValue,
                    "_groundCheck 참조가 비어 있음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // 인벤토리/크래프팅 UI 컴포넌트가 정확히 1개씩만 존재하는지 검증한다.
        // (과거 InventoryUI/CraftingUI가 Canvas와 패널에 중복 부착되어 I/C 토글 이벤트가
        //  이중 호출→상쇄되어 패널이 열리지 않던 결함 재발 방지)
        [Test]
        public void TestScene_HasExactlyOneInventoryUI_AndOneCraftingUI()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var invs = Object.FindObjectsByType<InventoryGridUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(1, invs.Length,
                    "InventoryGridUI 컴포넌트가 1개가 아님 — 중복 시 토글 이벤트 이중 호출로 인벤토리가 열리지 않음");

                var crafts = Object.FindObjectsByType<CraftingBenchUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Assert.AreEqual(1, crafts.Length,
                    "CraftingBenchUI 컴포넌트가 1개가 아님 — 중복 시 토글 이벤트 이중 호출로 크래프팅이 열리지 않음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // UI 컴포넌트는 항상 활성인 오브젝트(Canvas)에 있어야 Start()가 실행되어 토글 입력을 구독한다.
        // 비활성 패널에 두면 Start()가 처음 활성화될 때까지 실행되지 않아(구독 실패) 열리지 않는다.
        [Test]
        public void TestScene_InventoryAndCraftingUI_OnActiveObjects_WithPanelWired()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var inv = Object.FindFirstObjectByType<InventoryGridUI>();
                Assert.IsNotNull(inv, "씬에 InventoryGridUI 없음");
                Assert.IsTrue(inv.gameObject.activeInHierarchy,
                    "InventoryGridUI가 비활성 오브젝트에 있음 — Start()가 실행되지 않아 I 입력을 구독하지 못함");
                Assert.IsNotNull(new SerializedObject(inv).FindProperty("_panel").objectReferenceValue,
                    "InventoryGridUI._panel 참조가 비어 있음");

                var craft = Object.FindFirstObjectByType<CraftingBenchUI>();
                Assert.IsNotNull(craft, "씬에 CraftingBenchUI 없음");
                Assert.IsTrue(craft.gameObject.activeInHierarchy,
                    "CraftingBenchUI가 비활성 오브젝트에 있음 — Start()가 실행되지 않아 C 입력을 구독하지 못함");
                Assert.IsNotNull(new SerializedObject(craft).FindProperty("_panel").objectReferenceValue,
                    "CraftingBenchUI._panel 참조가 비어 있음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }

        // UI 클릭/포인터 이벤트에 EventSystem이 필수다. (과거 EventSystem 부재로 버튼 클릭이 무효였음)
        [Test]
        public void TestScene_HasEventSystem_ForUIInput()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                Assert.IsNotNull(Object.FindFirstObjectByType<EventSystem>(),
                    "씬에 EventSystem이 없음 — UI 클릭/포인터 이벤트가 동작하지 않음");
            }
            finally { EditorSceneManager.CloseScene(scene, true); }
        }
    }
}
