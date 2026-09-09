using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class RoomLayoutFromTemplateTests
    {
        // 바닥 위에 확률 발판 4칸과 확률 적 2칸을 둔 Small 방
        private static RoomTemplate Template()
        {
            var d = RoomDimensions.Of(RoomSizeClass.Small);
            int w = d.Width, h = d.Height;
            var rows = new string[h];
            for (int row = 0; row < h; row++)
            {
                int y = (h - 1) - row;
                var sb = new System.Text.StringBuilder(w);
                for (int x = 0; x < w; x++)
                {
                    bool border = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    char c = !border ? '.' : (y == 0 ? '=' : '#');
                    if (y == 0 && x == w / 2) c = 'D';
                    else if (y == h - 1 && x == w / 2) c = 'D';
                    else if (x == 0 && y == 1) c = 'D';
                    else if (x == w - 1 && y == 1) c = 'D';
                    else if (y == 4 && x >= 5 && x <= 8) c = '?';
                    else if (y == 1 && (x == 10 || x == 14)) c = '%';
                    sb.Append(c);
                }
                rows[row] = sb.ToString();
            }
            var r = RoomTemplateParser.Parse(rows, "chance");
            Assert.IsTrue(r.Success, string.Join(" / ", r.Errors));
            return r.Template;
        }

        [Test]
        public void ShouldBeDeterministicForSameSeed()
        {
            var t = Template();
            var a = RoomLayout.Resolve(t, 4242);
            var b = RoomLayout.Resolve(t, 4242);
            for (int x = 0; x < t.Width; x++)
                for (int y = 0; y < t.Height; y++)
                    Assert.AreEqual(a.Tiles[x, y], b.Tiles[x, y], $"({x},{y})가 시드에 대해 결정적이지 않다");
            CollectionAssert.AreEqual(a.Markers.Select(m => m.Cell).ToList(),
                                      b.Markers.Select(m => m.Cell).ToList());
        }

        [Test]
        public void DifferentSeedsShouldProduceDifferentRooms()
        {
            var t = Template();
            bool anyDifference = false;
            for (int seed = 0; seed < 20 && !anyDifference; seed++)
            {
                var a = RoomLayout.Resolve(t, 0);
                var b = RoomLayout.Resolve(t, seed + 1);
                foreach (var c in t.ChancePlatforms)
                    if (a.Tiles[c.x, c.y] != b.Tiles[c.x, c.y]) anyDifference = true;
            }
            Assert.IsTrue(anyDifference, "템플릿 하나가 시드마다 다른 방이 되어야 한다");
        }

        [Test]
        public void AllSolidModeShouldFillEveryChancePlatform()
        {
            var t = Template();
            var r = RoomLayout.Resolve(t, ChanceMode.AllSolid);
            foreach (var c in t.ChancePlatforms)
                Assert.AreEqual(TileKind.Platform, r.Tiles[c.x, c.y]);
        }

        [Test]
        public void AllEmptyModeShouldClearEveryChancePlatform()
        {
            var t = Template();
            var r = RoomLayout.Resolve(t, ChanceMode.AllEmpty);
            foreach (var c in t.ChancePlatforms)
                Assert.AreEqual(TileKind.Empty, r.Tiles[c.x, c.y]);
        }

        [Test]
        public void AllSolidShouldSpawnEveryChanceEnemy()
        {
            var t = Template();
            var solid = RoomLayout.Resolve(t, ChanceMode.AllSolid);
            var empty = RoomLayout.Resolve(t, ChanceMode.AllEmpty);
            Assert.AreEqual(t.ChanceEnemies.Count, solid.Markers.Count(m => m.Symbol == 'e'));
            Assert.AreEqual(0, empty.Markers.Count(m => m.Symbol == 'e'));
        }

        [Test]
        public void FixedTerrainShouldSurviveResolution()
        {
            var t = Template();
            var r = RoomLayout.Resolve(t, 7);
            Assert.AreEqual(TileKind.Floor, r.Tiles[1, 0], "바닥은 확률과 무관하다");
            Assert.AreEqual(TileKind.Wall, r.Tiles[0, 5], "벽은 확률과 무관하다");
            Assert.AreEqual(4, r.Doors.Count);
        }

        [Test]
        public void ShouldKeepAuthoredMarkers()
        {
            var t = Template();
            var r = RoomLayout.Resolve(t, ChanceMode.AllEmpty);
            // 확정 마커가 없는 템플릿이므로 확률 적을 끄면 마커도 비어야 한다
            CollectionAssert.IsEmpty(r.Markers);
        }
    }
}
