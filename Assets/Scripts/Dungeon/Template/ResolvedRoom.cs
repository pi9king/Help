using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // 확률 칸을 어떻게 확정할지.
    // AllSolid/AllEmpty는 검증용 극단값 — 도달성 테스트는 이 두 경우를 모두 통과해야
    // "시드가 어떻게 굴러도 막히지 않는 방"이 보장된다.
    public enum ChanceMode { Seeded, AllSolid, AllEmpty }

    // 확률 칸까지 확정된 방 한 장. 렌더·콜라이더·스폰·도달성 검증이 모두 이걸 본다.
    public sealed class ResolvedRoom
    {
        public int Width { get; }
        public int Height { get; }
        public TileKind[,] Tiles { get; }
        public IReadOnlyList<RoomMarker> Markers { get; }
        public IReadOnlyDictionary<Direction, Vector2Int> Doors { get; }

        public ResolvedRoom(int width, int height, TileKind[,] tiles,
                            IReadOnlyList<RoomMarker> markers,
                            IReadOnlyDictionary<Direction, Vector2Int> doors)
        {
            Width = width;
            Height = height;
            Tiles = tiles;
            Markers = markers;
            Doors = doors;
        }

        // 방 밖은 벽. 단 **격자 아래(y < 0)는 빈 공간**이다 —
        // 벽으로 돌려주면 IsStandable(Wall)==true라서 바닥에 뚫린 구멍이
        // "밟고 설 수 있는 자리"로 보인다(ReachabilityAnalyzer.World.At과 같은 규칙).
        // 같은 판정이 두 곳에 구현돼 있으므로 한쪽만 고치면 시뮬과 검증기가 서로 다른 것을 본다.
        public TileKind TileAt(int x, int y) =>
            y < 0 ? TileKind.Empty
            : x < 0 || x >= Width || y >= Height ? TileKind.Wall
            : Tiles[x, y];

        // 서 있을 수 있는(=위에 올라설 수 있는) 지형인가
        // D-12: 피해 바닥도 설 수 있다 — 못 서면 빠진 곳에서 걸어 나올 수가 없다.
        public static bool IsStandable(TileKind k) =>
            k == TileKind.Floor || k == TileKind.Wall || k == TileKind.Platform || k == TileKind.Hazard;

        // 몸이 통과할 수 없는 지형인가 (발판은 위에서만 막으므로 통과 가능으로 본다)
        public static bool BlocksMovement(TileKind k) => k == TileKind.Floor || k == TileKind.Wall;

        // 밟으면 아픈 칸. **막는 칸이 아니다** — 길을 막는 건 벽의 일이고,
        // 위험 지형은 비용이다(D-12). 도달성은 통과를 허용한다.
        public static bool IsHazard(TileKind k) => k == TileKind.Hazard;
    }
}
