using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // Floor/Hazard는 이동 가능하고 Wall/Empty는 이동 불가다.
    // Hazard는 이동을 막지 않고 접촉 피해만 준다.
    public enum TileKind { Floor, Wall, Empty, Hazard }

    // 순수 로직: 쿼터뷰 방 셸을 계산한다.
    // 테두리는 벽이고 내부 전체는 이동 가능한 바닥이다.
    // 문은 시각 마커 + E 상호작용으로 처리하므로 셸에 구멍을 뚫지 않는다(RoomManager가 오버레이).
    // MonoBehaviour(RoomManager)가 이 결과를 Tilemap에 그리고 콜라이더를 붙인다.
    public static class RoomLayout
    {
        public static Dictionary<Vector2Int, TileKind> Build(int width, int height)
        {
            var cells = new Dictionary<Vector2Int, TileKind>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    bool border = x == 0 || x == width - 1 || y == 0 || y == height - 1;
                    cells[new Vector2Int(x, y)] = border ? TileKind.Wall : TileKind.Floor;
                }
            }

            return cells;
        }

        // 템플릿의 확률 칸을 확정해 실제 방 한 장을 만든다.
        // 같은 (템플릿, 시드)는 항상 같은 방이 된다 — 방을 나갔다 와도 지형이 바뀌면 안 되기 때문.
        public static ResolvedRoom Resolve(RoomTemplate template, int seed) =>
            Resolve(template, ChanceMode.Seeded, seed);

        public static ResolvedRoom Resolve(RoomTemplate template, ChanceMode mode, int seed = 0)
        {
            var tiles = template.CopyTiles();
            var markers = new List<RoomMarker>(template.Markers);
            var rng = new System.Random(seed);

            foreach (var cell in template.ChanceWalls)
                tiles[cell.x, cell.y] = Roll(mode, rng) ? TileKind.Wall : TileKind.Floor;

            foreach (var cell in template.ChanceEnemies)
                if (Roll(mode, rng)) markers.Add(new RoomMarker('e', cell));

            return new ResolvedRoom(template.Width, template.Height, tiles, markers, template.Doors);
        }

        private static bool Roll(ChanceMode mode, System.Random rng) => mode switch
        {
            ChanceMode.AllSolid => true,
            ChanceMode.AllEmpty => false,
            _ => rng.Next(2) == 0,
        };
    }
}
