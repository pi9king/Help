using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Help.Dungeon;

namespace Tests.EditMode
{
    // RoomManager가 방을 실제로 Tilemap에 그리는지 검증 (play 모드 없이).
    // GameManager에 의존하는 Start()는 호출하지 않고 LoadMap/EnterRoom을 직접 부른다.
    public class RoomManagerRenderTests
    {
        private const int W = 13, H = 9;

        private static int Occupied(Tilemap tilemap)
        {
            tilemap.CompressBounds();
            int n = 0;
            foreach (var p in tilemap.cellBounds.allPositionsWithin)
                if (tilemap.HasTile(p)) n++;
            return n;
        }

        [Test]
        public void LoadMap_PaintsShell_FloorAtBottomWallsAround_EmptyInterior()
        {
            var tmGo = new GameObject("tm", typeof(Grid), typeof(Tilemap));
            var tilemap = tmGo.GetComponent<Tilemap>();
            var rmGo = new GameObject("rm");
            var rm = rmGo.AddComponent<RoomManager>();
            var floorTile = ScriptableObject.CreateInstance<Tile>();
            var wallTile = ScriptableObject.CreateInstance<Tile>();
            SetPrivate(rm, "_tilemap", tilemap);
            SetPrivate(rm, "_floorTile", floorTile);
            SetPrivate(rm, "_wallTile", wallTile);

            var map = new DungeonMap((0, 0));
            map.AddRoom(new Room(0, 0, RoomType.Combat));

            try
            {
                rm.LoadMap(map);

                // 셸(테두리)만 칠해짐: 2*W + 2*(H-2)
                Assert.AreEqual(2 * W + 2 * (H - 2), Occupied(tilemap), "셸(테두리)만 칠해져야 함");
                // 내부는 빈 공간(플레이어가 지나다니는 공기)
                Assert.IsNull(tilemap.GetTile(new Vector3Int(0, 0, 0)), "내부 중앙은 빈 공간이어야 함");
                // 바닥 중앙 = 바닥 타일 (원점 중심 좌표에서 바닥행 = -H/2)
                Assert.AreEqual(floorTile, tilemap.GetTile(new Vector3Int(0, -H / 2, 0)), "바닥 중앙이 바닥 타일이 아님");
                // 천장 모서리 = 벽
                Assert.AreEqual(wallTile, tilemap.GetTile(new Vector3Int(-W / 2, (H - 1) - H / 2, 0)), "천장 모서리가 벽이 아님");
            }
            finally
            {
                Object.DestroyImmediate(tmGo);
                Object.DestroyImmediate(rmGo);
                Object.DestroyImmediate(floorTile);
                Object.DestroyImmediate(wallTile);
            }
        }

        [Test]
        public void RenderRoom_ClearsPreviousTilesOnReenter_NoAccumulation()
        {
            var tmGo = new GameObject("tm", typeof(Grid), typeof(Tilemap));
            var tilemap = tmGo.GetComponent<Tilemap>();
            var rmGo = new GameObject("rm");
            var rm = rmGo.AddComponent<RoomManager>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            SetPrivate(rm, "_tilemap", tilemap);
            SetPrivate(rm, "_floorTile", tile);
            SetPrivate(rm, "_wallTile", tile);

            var map = new DungeonMap((0, 0));
            map.AddRoom(new Room(0, 0, RoomType.Combat));

            try
            {
                rm.LoadMap(map);
                rm.EnterRoom(0, 0); // 재진입 — 누적되지 않고 동일 개수
                Assert.AreEqual(2 * W + 2 * (H - 2), Occupied(tilemap));
            }
            finally
            {
                Object.DestroyImmediate(tmGo);
                Object.DestroyImmediate(rmGo);
                Object.DestroyImmediate(tile);
            }
        }

        [Test]
        public void RenderRoom_PlacesOpenDoorTileAtConnectedDoorGap()
        {
            var tmGo = new GameObject("tm", typeof(Grid), typeof(Tilemap));
            var tilemap = tmGo.GetComponent<Tilemap>();
            var rmGo = new GameObject("rm");
            var rm = rmGo.AddComponent<RoomManager>();
            var tile = ScriptableObject.CreateInstance<Tile>();
            var doorTile = ScriptableObject.CreateInstance<Tile>();
            SetPrivate(rm, "_tilemap", tilemap);
            SetPrivate(rm, "_floorTile", tile);
            SetPrivate(rm, "_wallTile", tile);
            SetPrivate(rm, "_doorOpenTile", doorTile);

            var map = new DungeonMap((0, 0));
            map.AddRoom(new Room(0, 0, RoomType.Combat) { East = (1, 0) });
            map.AddRoom(new Room(1, 0, RoomType.Treasure)); // 자유 방 → 열린 문

            try
            {
                rm.LoadMap(map);
                // 동쪽 문 위치 = (W-1 - W/2, SideDoorRow - H/2) = (6, 1-4) = (6, -3) — 바닥 바로 위
                Assert.AreEqual(doorTile, tilemap.GetTile(new Vector3Int(6, 1 - H / 2, 0)),
                    "연결된 동쪽 문에 열린 문 타일이 표시되지 않음");
            }
            finally
            {
                Object.DestroyImmediate(tmGo);
                Object.DestroyImmediate(rmGo);
                Object.DestroyImmediate(tile);
                Object.DestroyImmediate(doorTile);
            }
        }

        // 콜라이더 연동 회귀: EnsureColliders가 Tilemap에 물리 콜라이더 세트를 올바르게 부착하는지.
        // (방 셸이 시각용에 그치고 콜라이더가 없어 플레이어가 지형에 막히지 않던 문제 재발 방지)
        [Test]
        public void EnsureColliders_AttachesCompositeTilemapCollider_AsStaticBody()
        {
            var tmGo = new GameObject("tm", typeof(Grid), typeof(Tilemap));
            var tilemap = tmGo.GetComponent<Tilemap>();
            var rmGo = new GameObject("rm");
            var rm = rmGo.AddComponent<RoomManager>();
            SetPrivate(rm, "_tilemap", tilemap);

            try
            {
                typeof(RoomManager)
                    .GetMethod("EnsureColliders", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(rm, null);

                Assert.IsNotNull(tmGo.GetComponent<TilemapCollider2D>(), "TilemapCollider2D 미부착");
                var composite = tmGo.GetComponent<CompositeCollider2D>();
                Assert.IsNotNull(composite, "CompositeCollider2D 미부착");
                Assert.AreEqual(CompositeCollider2D.GeometryType.Polygons, composite.geometryType);
                var rb = tmGo.GetComponent<Rigidbody2D>();
                Assert.IsNotNull(rb, "Rigidbody2D 미부착");
                Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType, "정적 바디가 아님");

                int gl = LayerMask.NameToLayer("Ground");
                if (gl >= 0)
                    Assert.AreEqual(gl, tmGo.layer, "Tilemap이 Ground 레이어로 이동되지 않음");
            }
            finally
            {
                Object.DestroyImmediate(tmGo);
                Object.DestroyImmediate(rmGo);
            }
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            f.SetValue(target, value);
        }
    }
}
