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

        public TileKind TileAt(int x, int y) =>
            x < 0 || y < 0 || x >= Width || y >= Height ? TileKind.Wall : Tiles[x, y];

        public static bool IsWalkable(TileKind k) =>
            k == TileKind.Floor || k == TileKind.Hazard;

        // 쿼터뷰에서 바닥은 이동 공간이고 벽만 이동을 막는다.
        public static bool BlocksMovement(TileKind k) => k == TileKind.Wall || k == TileKind.Empty;

        // 밟으면 아픈 칸. **막는 칸이 아니다** — 길을 막는 건 벽의 일이고,
        // 위험 지형은 비용이다(D-12). 도달성은 통과를 허용한다.
        public static bool IsHazard(TileKind k) => k == TileKind.Hazard;
    }
}
