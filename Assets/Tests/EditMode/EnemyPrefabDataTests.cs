using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Help.Combat;
using Help.Enemy;

namespace Tests.EditMode
{
    // 프리팹의 직렬화 값은 코드에 드러나지 않아 잘못된 값이 조용히 굳는다.
    // 실제 사고: 데모용 Fire 잠금이 베이스 Enemy.prefab에 굳은 뒤 파생 3종+보스로 전파 →
    // Fire 무기(SABER) 외 모든 공격이 DamageCalculator에서 ×0.1(최소 1)로 깎여
    // 보스(HP 320)를 320대 때려야 죽는 상태가 됐다. 그 재발을 막는다.
    public class EnemyPrefabDataTests
    {
        private const string PrefabDir = "Assets/Prefabs";

        private static List<GameObject> LoadEnemyPrefabs()
        {
            var result = new List<GameObject>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null && go.GetComponent<EnemyBase>() != null) result.Add(go);
            }
            return result;
        }

        [Test]
        public void EnemyPrefabs_Exist()
        {
            Assert.IsNotEmpty(LoadEnemyPrefabs(), $"{PrefabDir} 에 EnemyBase 프리팹이 없음");
        }

        // DESIGN.md: "일반 적 = 속성 잠금 없음, 아무 무기 가능. 속성은 조건부 방·특수 적에만 의미."
        // 속성 잠금 적이 필요하면 전용 배리언트 프리팹을 따로 만든다 — 기본형에 박지 않는다.
        [Test]
        public void EnemyPrefabs_HaveNoElementLock()
        {
            foreach (var go in LoadEnemyPrefabs())
            {
                var so = new SerializedObject(go.GetComponent<EnemyBase>());
                int locked = so.FindProperty("_lockedElement").enumValueIndex;
                Assert.AreEqual((int)ElementType.None, locked,
                    $"{go.name}: 기본 적에 속성 잠금({(ElementType)locked})이 걸려 있음 — " +
                    "일치 속성 무기 외 모든 공격이 ×0.1(최소 1)로 깎인다");
            }
        }

        // 알파벳은 층 드랍 테이블(FloorLootPlan)이 정한 총량만 나와야 한다.
        // 프리팹에 정적 알파벳 드랍이 박혀 있으면 예산 밖에서 글자가 새어 나와 다시 난잡해진다 —
        // 적이 무엇을 떨굴지는 방 로드 시 RoomManager가 배분해 주입한다(AddGuaranteedDrop).
        [Test]
        public void EnemyPrefabs_HaveNoStaticAlphabetDrop()
        {
            foreach (var go in LoadEnemyPrefabs())
            {
                var so = new SerializedObject(go.GetComponent<EnemyBase>());
                Assert.AreEqual(0, so.FindProperty("_drops").arraySize,
                    $"{go.name}: 프리팹에 알파벳 드랍이 박혀 있음 — 층 예산 밖에서 글자가 샌다");
            }
        }

        // 알파벳이 안 나오는 적도 있어야 하지만(희소 배분), 그렇다고 처치 보상이 완전히 0이면
        // 전투가 무의미해진다. 재화(골드/포션) 테이블이 그 공백을 메운다.
        [Test]
        public void EnemyPrefabs_HaveRewardTable()
        {
            foreach (var go in LoadEnemyPrefabs())
            {
                var so = new SerializedObject(go.GetComponent<EnemyBase>());
                Assert.Greater(so.FindProperty("_rewardDrops").arraySize, 0,
                    $"{go.name}: 재화 보상 테이블이 비어 있음 — 처치 보상이 0이 될 수 있다");
            }
        }
    }
}
