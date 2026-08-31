using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class RoomLayoutTests
    {
        [Test]
        public void Build_BottomRowIsFloor_OtherBordersAreWall()
        {
            var cells = RoomLayout.Build(5, 5);

            // 바닥 한 줄(y==0)은 Floor — 모서리 포함
            Assert.AreEqual(TileKind.Floor, cells[new Vector2Int(0, 0)]);
            Assert.AreEqual(TileKind.Floor, cells[new Vector2Int(2, 0)]);
            Assert.AreEqual(TileKind.Floor, cells[new Vector2Int(4, 0)]);
            // 천장/좌우 벽은 Wall
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(2, 4)]);  // 천장 중앙
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(0, 2)]);  // 좌측 벽
            Assert.AreEqual(TileKind.Wall, cells[new Vector2Int(4, 2)]);  // 우측 벽
        }

        [Test]
        public void Build_InteriorIsEmpty_NotSolid()
        {
            var cells = RoomLayout.Build(5, 5);

            // 내부는 빈 공간 — 딕셔너리에 없음(플레이어가 지나다닐 공기)
            Assert.IsFalse(cells.ContainsKey(new Vector2Int(2, 2)), "중앙이 solid로 채워짐");
            Assert.IsFalse(cells.ContainsKey(new Vector2Int(1, 1)), "내부가 solid로 채워짐");
            Assert.IsFalse(cells.ContainsKey(new Vector2Int(3, 3)), "내부가 solid로 채워짐");
        }

        [Test]
        public void Build_ProducesOnlyPerimeterCells()
        {
            var cells = RoomLayout.Build(13, 9);
            // 테두리만: 2*W + 2*(H-2)
            Assert.AreEqual(2 * 13 + 2 * (9 - 2), cells.Count);
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
                Assert.AreEqual(border, cells.ContainsKey(new Vector2Int(x, y)),
                    $"({x},{y}) 셸 포함 여부가 테두리 여부와 불일치");
            }
        }
    }
}
