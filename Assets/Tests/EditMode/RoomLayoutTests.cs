using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class RoomLayoutTests
    {
        [Test]
        public void Build_AllBordersAreWalls()
        {
            var cells = RoomLayout.Build(5, 5);

            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(0, 0)]);
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(2, 0)]);
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(4, 0)]);
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(2, 4)]);  // 천장 중앙
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(0, 2)]);  // 좌측 벽
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(4, 2)]);  // 우측 벽
        }

        [Test]
        public void Build_InteriorIsWalkableFloor()
        {
            var cells = RoomLayout.Build(5, 5);

            Assert.AreEqual(TileKind.Floor, cells[new Vector2Int(2, 2)]);
            Assert.AreEqual(TileKind.Floor, cells[new Vector2Int(1, 1)]);
            Assert.AreEqual(TileKind.Floor, cells[new Vector2Int(3, 3)]);
        }

        [Test]
        public void Build_ProducesEveryRoomCell()
        {
            var cells = RoomLayout.Build(13, 9);
            Assert.AreEqual(13 * 9, cells.Count);
        }

        [Test]
        public void Build_AllBorderCellsPresent_AndSolid()
        {
            const int W = 7, H = 7;
            var cells = RoomLayout.Build(W, H);
            for (int x = 0; x < W; x++)
            for (int y = 0; y < H; y++)
            {
                bool border = x == 0 || x == W - 1 || y == 0 || y == H - 1;
                Assert.IsTrue(cells.ContainsKey(new Vector2Int(x, y)));
                Assert.AreEqual(border ? TileKind.Wall : TileKind.Floor,
                                cells[new Vector2Int(x, y)]);
            }
        }
    }
}
