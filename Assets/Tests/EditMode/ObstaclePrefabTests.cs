using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Help.Dungeon;
using Help.Puzzle;

namespace Tests.EditMode
{
    // 능력 장애물(부서지는 벽/얼음벽/잠긴 문)이 **지형으로서** 지켜야 할 두 가지.
    //
    // 실제 사고 (2026-09-10 Play): 부서지는 벽 위에 올라선 플레이어가 점프를 못 해
    // 발판 사이에 낀 채 진행 불가가 됐다. 원인이 두 겹이었다.
    //   1) 장애물이 Default 레이어라 PlayerController._groundLayer(Ground)에 안 걸림
    //      → OverlapCircle이 아무것도 못 찾아 접지 실패 → 점프도 공중 대시 충전도 죽는다
    //   2) 콜라이더가 바닥에서 0.5 떠 있어(1×1을 로컬 y=1 중심에 둠), 위 발판 밑면과의
    //      틈이 딱 플레이어가 낄 크기였다
    //
    // 이 테스트는 그 두 가지만 못 박는다. 장애물의 크기·모양을 규정하려는 게 아니다.
    public class ObstaclePrefabTests
    {
        private const string PrefabDir = "Assets/Prefabs";

        // 프리팹 트리 어디에 있든 능력 장애물을 전부 모은다
        // (BreakableWall/IceWall처럼 단독 프리팹인 것도, Room_Tutorial의 LockedDoor처럼 자식인 것도).
        private static List<CapabilityTarget> LoadObstacles()
        {
            var result = new List<CapabilityTarget>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (go == null) continue;
                foreach (var t in go.GetComponentsInChildren<CapabilityTarget>(true))
                    if (!result.Contains(t)) result.Add(t);
            }
            return result;
        }

        [Test]
        public void ObstaclePrefabs_Exist()
        {
            Assert.IsNotEmpty(LoadObstacles(), $"{PrefabDir} 에 CapabilityTarget 프리팹이 없음");
        }

        // 밟고 설 수 있는 물건은 플레이어의 접지 판정에 걸려야 한다.
        [Test]
        public void Obstacles_AreOnGroundLayer()
        {
            int ground = LayerMask.NameToLayer("Ground");
            Assert.GreaterOrEqual(ground, 0, "Ground 레이어가 프로젝트에 없음");

            foreach (var t in LoadObstacles())
                Assert.AreEqual(ground, t.gameObject.layer,
                    $"'{t.name}' 이 Ground 레이어가 아님 — 그 위에 올라서면 점프가 죽는다 " +
                    "(PlayerController._groundLayer 는 Ground 만 본다)");
        }

        // 장애물은 배치 높이(RoomGeometry.ObstacleLocalY)에 놓이고, 콜라이더 밑면이 방 바닥에 닿아야 한다.
        // 뜨면 그 틈에 플레이어가 낀다.
        [Test]
        public void ObstacleColliders_RestOnFloor()
        {
            foreach (var t in LoadObstacles())
            {
                var box = t.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(box, $"'{t.name}' 에 BoxCollider2D 가 없음");

                float bottom = (box.offset.y - box.size.y * 0.5f) * t.transform.localScale.y;
                Assert.AreEqual(-RoomGeometry.ObstacleLocalY, bottom, 0.001f,
                    $"'{t.name}' 콜라이더 밑면이 바닥에 안 닿음(밑면={bottom}). " +
                    "배치 높이만큼 아래로 내려와야 바닥에 선다");
            }
        }
    }
}
