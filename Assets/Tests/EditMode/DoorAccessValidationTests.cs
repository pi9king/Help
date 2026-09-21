using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class DoorAccessValidationTests
    {
        [Test]
        public void OpenPlanarRoomShouldConnectAllFourDoors()
        {
            RoomTemplate template = ParseRoom(blockCenter: false);

            TemplateValidation result = RoomTemplateValidator.ValidateAllChanceExtremes(template);

            Assert.IsTrue(result.Ok, string.Join("\n", result.Errors));
            Assert.AreEqual(4, result.DoorAccess.Count);
        }

        [Test]
        public void DividingWallShouldReportDisconnectedDoors()
        {
            RoomTemplate template = ParseRoom(blockCenter: true);

            TemplateValidation result = RoomTemplateValidator.ValidateAllChanceExtremes(template);

            Assert.IsFalse(result.Ok);
        }

        [Test]
        public void OpenBorderOutsideReservedDoorShouldFail()
        {
            RoomTemplate template = ParseRoom(blockCenter: false);
            TileKind[,] tiles = template.CopyTiles();
            tiles[1, 0] = TileKind.Floor;
            var room = new ResolvedRoom(template.Width, template.Height, tiles,
                                        template.Markers, template.Doors);

            TemplateValidation result = RoomTemplateValidator.Validate(room, "open-border");

            Assert.IsFalse(result.Ok);
        }

        private static RoomTemplate ParseRoom(bool blockCenter)
        {
            RoomDim d = RoomDimensions.Of(RoomSizeClass.Small);
            var rows = new string[d.Height];
            for (int row = 0; row < d.Height; row++)
            {
                int y = d.Height - 1 - row;
                var chars = new char[d.Width];
                for (int x = 0; x < d.Width; x++)
                {
                    bool border = x == 0 || x == d.Width - 1 || y == 0 || y == d.Height - 1;
                    chars[x] = border ? '#' : '.';
                    if (blockCenter && x == d.Width / 2) chars[x] = '#';
                }
                if (y == d.Height - 1 || y == 0) chars[d.Width / 2] = 'D';
                if (y == d.Height / 2) { chars[0] = 'D'; chars[d.Width - 1] = 'D'; }
                rows[row] = new string(chars);
            }

            RoomTemplateParseResult parsed = RoomTemplateParser.Parse(rows, "planar");
            Assert.IsTrue(parsed.Success, string.Join("\n", parsed.Errors));
            return parsed.Template;
        }
    }
}
