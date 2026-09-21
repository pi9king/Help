using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class PlanarReachabilityTests
    {
        [Test]
        public void ShouldReachEveryConnectedFloorCell()
        {
            var tiles = Filled(5, 5, TileKind.Floor);
            Border(tiles, TileKind.Wall);

            var set = PlanarReachabilityAnalyzer.Analyze(tiles, new Vector2Int(1, 1));

            Assert.IsTrue(set.Contains(new Vector2Int(3, 3)));
            Assert.IsFalse(set.Contains(new Vector2Int(0, 0)));
        }

        [Test]
        public void ShouldNotCutDiagonallyThroughTwoWalls()
        {
            var tiles = Filled(4, 4, TileKind.Wall);
            tiles[1, 1] = TileKind.Floor;
            tiles[2, 2] = TileKind.Floor;

            var set = PlanarReachabilityAnalyzer.Analyze(tiles, new Vector2Int(1, 1));

            Assert.IsFalse(set.Contains(new Vector2Int(2, 2)));
        }

        private static TileKind[,] Filled(int width, int height, TileKind value)
        {
            var tiles = new TileKind[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    tiles[x, y] = value;
            return tiles;
        }

        private static void Border(TileKind[,] tiles, TileKind value)
        {
            int maxX = tiles.GetLength(0) - 1;
            int maxY = tiles.GetLength(1) - 1;
            for (int x = 0; x <= maxX; x++) { tiles[x, 0] = value; tiles[x, maxY] = value; }
            for (int y = 0; y <= maxY; y++) { tiles[0, y] = value; tiles[maxX, y] = value; }
        }
    }
}
