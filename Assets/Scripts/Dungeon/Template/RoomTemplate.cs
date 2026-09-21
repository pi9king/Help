using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // 템플릿 안의 스폰 표식. 지형이 아니라 "여기에 무언가를 놓아라"는 지시다.
    // 무엇을 놓을지는 RoomContentLibrary가 방 유형에 따라 결정한다.
    public readonly struct RoomMarker
    {
        public char Symbol { get; }
        public Vector2Int Cell { get; }
        public RoomMarker(char symbol, Vector2Int cell) { Symbol = symbol; Cell = cell; }
    }

    // ASCII 텍스트에서 파싱된 방 한 장. 좌표계는 RoomLayout과 같은 평면 좌표다.
    // 불변 객체이며, 확률 칸은 아직 확정되지 않은 채로 남는다(RoomLayout.Resolve가 시드로 확정).
    public sealed class RoomTemplate
    {
        public string Name { get; }
        public int Width { get; }
        public int Height { get; }
        public RoomSizeClass SizeClass { get; }

        private readonly TileKind[,] _tiles;

        public IReadOnlyList<RoomMarker> Markers { get; }
        public IReadOnlyDictionary<Direction, Vector2Int> Doors { get; }

        // 시드에 따라 벽 또는 바닥이 되는 칸들. 템플릿 하나가 여러 방이 되게 하는 장치.
        public IReadOnlyList<Vector2Int> ChanceWalls { get; }
        // 시드에 따라 적이 스폰되거나 비는 칸들.
        public IReadOnlyList<Vector2Int> ChanceEnemies { get; }

        internal RoomTemplate(string name, int width, int height, RoomSizeClass sizeClass,
                              TileKind[,] tiles, IReadOnlyList<RoomMarker> markers,
                              IReadOnlyDictionary<Direction, Vector2Int> doors,
                              IReadOnlyList<Vector2Int> chanceWalls,
                              IReadOnlyList<Vector2Int> chanceEnemies)
        {
            Name = name;
            Width = width;
            Height = height;
            SizeClass = sizeClass;
            _tiles = tiles;
            Markers = markers;
            Doors = doors;
            ChanceWalls = chanceWalls;
            ChanceEnemies = chanceEnemies;
        }

        public TileKind TileAt(int x, int y) =>
            x < 0 || y < 0 || x >= Width || y >= Height ? TileKind.Wall : _tiles[x, y];

        // 확률 칸을 확정하기 전의 원본 지형 사본.
        public TileKind[,] CopyTiles()
        {
            var copy = new TileKind[Width, Height];
            System.Array.Copy(_tiles, copy, _tiles.Length);
            return copy;
        }
    }
}
