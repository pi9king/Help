using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // 방 한 칸의 지형 종류. 새 값은 끝에 추가한다.
    // Floor/Wall = 막힌 지형, Platform = 아래에서 통과되는 일방통행 발판,
    // Hazard = 밟으면 아픈 바닥(통과는 된다), Empty = 공기
    //
    // D-12: 예전엔 Spike(가시)와 Pit(구덩이)이 따로 있었다. 구덩이가 "빠지면 입구로
    // 순간이동"에서 "바닥에서 걸어 나온다"로 바뀌면서 둘의 차이가 피해량뿐이 되어 합쳤다.
    // "구덩이"는 이제 타일 종류가 아니라 **지형을 움푹 파고 바닥에 이 타일을 깐 모양**이다.
    public enum TileKind { Floor, Wall, Empty, Platform, Hazard }

    // 순수 로직: 사이드뷰 플랫포머용 방 셸을 계산한다.
    // 테두리만 solid — 바닥 한 줄(y==0)은 Floor, 나머지 테두리(천장/좌우 벽)는 Wall.
    // 내부는 빈 공간(공기)이라 딕셔너리에 포함하지 않는다(플레이어가 바닥을 딛고 벽에 막힘).
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
                    if (!border) continue; // 내부는 빈 공간
                    cells[new Vector2Int(x, y)] = (y == 0) ? TileKind.Floor : TileKind.Wall;
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

            foreach (var cell in template.ChancePlatforms)
                if (Roll(mode, rng)) tiles[cell.x, cell.y] = TileKind.Platform;

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
