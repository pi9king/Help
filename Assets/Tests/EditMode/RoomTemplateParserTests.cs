using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class RoomTemplateParserTests
    {
        // 유효한 최소 셸을 프로그램으로 만든다 — 테스트에 25x15 아스키를 박아 넣으면
        // 규격이 바뀔 때마다 전부 손봐야 한다.
        private static string[] Shell(RoomSizeClass sizeClass)
        {
            var d = RoomDimensions.Of(sizeClass);
            int w = d.Width, h = d.Height;
            var rows = new string[h];
            for (int row = 0; row < h; row++)
            {
                int y = (h - 1) - row;                 // 텍스트 첫 줄 = 방 천장
                var sb = new System.Text.StringBuilder(w);
                for (int x = 0; x < w; x++)
                {
                    bool border = x == 0 || x == w - 1 || y == 0 || y == h - 1;
                    char c = !border ? '.' : '#';

                    if (y == 0 && x == w / 2) c = 'D';                       // South
                    else if (y == h - 1 && x == w / 2) c = 'D';              // North
                    else if (x == 0 && y == h / 2) c = 'D';                  // West
                    else if (x == w - 1 && y == h / 2) c = 'D';              // East
                    sb.Append(c);
                }
                rows[row] = sb.ToString();
            }
            return rows;
        }

        private static string[] Replace(string[] rows, int row, int col, char c)
        {
            var copy = (string[])rows.Clone();
            var chars = copy[row].ToCharArray();
            chars[col] = c;
            copy[row] = new string(chars);
            return copy;
        }

        [Test]
        public void ShouldParseValidShell()
        {
            var r = RoomTemplateParser.Parse(Shell(RoomSizeClass.Small), "small");
            Assert.IsTrue(r.Success, string.Join(" / ", r.Errors));
            Assert.AreEqual(RoomSizeClass.Small, r.Template.SizeClass);
            Assert.AreEqual(25, r.Template.Width);
            Assert.AreEqual(15, r.Template.Height);
        }

        [Test]
        public void ShouldInferSizeClassFromDimensions()
        {
            Assert.AreEqual(RoomSizeClass.Wide,
                RoomTemplateParser.Parse(Shell(RoomSizeClass.Wide), "wide").Template.SizeClass);
            Assert.AreEqual(RoomSizeClass.Tall,
                RoomTemplateParser.Parse(Shell(RoomSizeClass.Tall), "tall").Template.SizeClass);
        }

        [Test]
        public void FirstTextRowShouldBeTopOfRoom()
        {
            // 텍스트 첫 줄이 북쪽이므로 배열에서는 가장 높은 y가 된다.
            var t = RoomTemplateParser.Parse(Shell(RoomSizeClass.Small), "s").Template;
            Assert.AreEqual(TileKind.Wall, t.TileAt(1, 0));
            Assert.AreEqual(TileKind.Wall, t.TileAt(1, t.Height - 1));
        }

        [Test]
        public void ShouldRejectRaggedGrid()
        {
            var rows = Shell(RoomSizeClass.Small);
            rows[3] = rows[3] + "#";
            var r = RoomTemplateParser.Parse(rows, "ragged");
            Assert.IsFalse(r.Success);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("길이")), string.Join(" / ", r.Errors));
        }

        [Test]
        public void ShouldRejectUnknownCharacter()
        {
            var rows = Replace(Shell(RoomSizeClass.Small), 5, 5, 'Z');
            var r = RoomTemplateParser.Parse(rows, "bad");
            Assert.IsFalse(r.Success);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("'Z'")), string.Join(" / ", r.Errors));
        }

        [Test]
        public void ShouldRejectSizeThatMatchesNoClass()
        {
            var r = RoomTemplateParser.Parse(new[] { "###", "#.#", "###" }, "tiny");
            Assert.IsFalse(r.Success);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("크기")), string.Join(" / ", r.Errors));
        }

        [Test]
        public void ShouldRequireAllFourDoors()
        {
            var rows = Shell(RoomSizeClass.Small);
            // 북쪽 문을 벽으로 메운다
            rows[0] = rows[0].Replace('D', '#');
            var r = RoomTemplateParser.Parse(rows, "nodoor");
            Assert.IsFalse(r.Success);
            Assert.IsTrue(r.Errors.Any(e => e.Contains("North")), string.Join(" / ", r.Errors));
        }

        [Test]
        public void ShouldClassifyDoorsByBorder()
        {
            var t = RoomTemplateParser.Parse(Shell(RoomSizeClass.Small), "s").Template;
            Assert.AreEqual(4, t.Doors.Count);
            Assert.AreEqual(0, t.Doors[Direction.West].x);
            Assert.AreEqual(t.Width - 1, t.Doors[Direction.East].x);
            Assert.AreEqual(t.Height - 1, t.Doors[Direction.North].y);
            Assert.AreEqual(0, t.Doors[Direction.South].y);
        }

        [Test]
        public void ShouldIgnoreCommentLines()
        {
            var rows = new[] { "; 이 방은 튜토리얼용", "; author: claude" }
                .Concat(Shell(RoomSizeClass.Small)).ToArray();
            var r = RoomTemplateParser.Parse(rows, "commented");
            Assert.IsTrue(r.Success, string.Join(" / ", r.Errors));
            Assert.AreEqual(15, r.Template.Height);
        }

        [Test]
        public void ShouldCollectMarkersWithoutMakingThemSolid()
        {
            var rows = Replace(Shell(RoomSizeClass.Small), 13, 4, 'e');
            var t = RoomTemplateParser.Parse(rows, "marker").Template;

            Assert.AreEqual(1, t.Markers.Count);
            Assert.AreEqual('e', t.Markers[0].Symbol);
            Assert.AreEqual(new Vector2Int(4, 1), t.Markers[0].Cell);
            Assert.AreEqual(TileKind.Floor, t.TileAt(4, 1), "마커 아래는 이동 가능한 바닥이다");
        }

        [Test]
        public void ShouldRecordChanceCellsSeparately()
        {
            var rows = Replace(Shell(RoomSizeClass.Small), 8, 6, '?');
            var t = RoomTemplateParser.Parse(rows, "chance").Template;
            Assert.AreEqual(1, t.ChanceWalls.Count);
            Assert.AreEqual(TileKind.Floor, t.TileAt(6, 6), "확률 장애물 칸의 기본값은 바닥");
        }

        [Test]
        public void ShouldParseLegacyFloorAndHazardTiles()
        {
            var rows = Shell(RoomSizeClass.Small);
            rows = Replace(rows, 9, 5, '-');
            rows = Replace(rows, 9, 6, '^');
            rows = Replace(rows, 9, 7, '~');
            var t = RoomTemplateParser.Parse(rows, "terrain").Template;
            Assert.AreEqual(TileKind.Floor, t.TileAt(5, 5));
            Assert.AreEqual(TileKind.Hazard, t.TileAt(6, 5));
            Assert.AreEqual(TileKind.Hazard, t.TileAt(7, 5)); // '~'는 레거시 별칭 → 같은 Hazard
        }
    }
}
