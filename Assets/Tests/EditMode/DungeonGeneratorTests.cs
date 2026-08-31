using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class DungeonGeneratorTests
    {
        private DungeonGenerator _gen;

        [SetUp]
        public void SetUp() => _gen = new DungeonGenerator();

        [Test]
        public void ShouldGenerateRoomCountWithinBounds()
        {
            var config = new DungeonConfig { MinRooms = 8, MaxRooms = 12, Seed = 42 };
            var map = _gen.Generate(config);
            int count = 0;
            foreach (var _ in map.Rooms) count++;
            Assert.GreaterOrEqual(count, config.MinRooms);
            Assert.LessOrEqual(count, config.MaxRooms);
        }

        [Test]
        public void ShouldAlwaysHaveStartRoom()
        {
            var config = new DungeonConfig { Seed = 1 };
            var map = _gen.Generate(config);
            Assert.IsNotNull(map.GetRoom(0, 0));
        }

        [Test]
        public void ShouldAlwaysHaveBossRoom()
        {
            var config = new DungeonConfig { Seed = 7 };
            var map = _gen.Generate(config);
            var boss = map.GetRoom(map.BossPosition.x, map.BossPosition.y);
            Assert.IsNotNull(boss);
            Assert.AreEqual(RoomType.Boss, boss.Type);
        }

        [Test]
        public void ShouldProduceSameDungeonForSameSeed()
        {
            var config = new DungeonConfig { Seed = 999, MinRooms = 8, MaxRooms = 12 };
            var map1 = _gen.Generate(config);
            var map2 = _gen.Generate(config);
            Assert.AreEqual(map1.Rooms.Count, map2.Rooms.Count);
        }

        [Test]
        public void ShouldConnectAdjacentRooms()
        {
            var config = new DungeonConfig { Seed = 5 };
            var map = _gen.Generate(config);
            bool foundConnection = false;
            foreach (var kv in map.Rooms)
            {
                var room = kv.Value;
                if (room.North != null || room.South != null ||
                    room.East != null || room.West != null)
                {
                    foundConnection = true;
                    break;
                }
            }
            Assert.IsTrue(foundConnection);
        }
    }
}
