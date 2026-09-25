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

        // DESIGN.md 2026-09-10: "환경 퍼즐방은 층마다 정확히 1개 보장".
        // 랜덤 배정이던 때는 표본 200층 중 36%에 환경 퍼즐방이 없었다(정적 재미 리포트).
        [Test]
        public void ShouldPlaceExactlyOneEnvironmentPuzzlePerFloor()
        {
            for (int seed = 0; seed < 200; seed++)
            {
                var map = _gen.Generate(new DungeonConfig { Seed = seed });
                int env = 0;
                foreach (var room in map.Rooms.Values)
                    if (room.Type == RoomType.EnvironmentPuzzle) env++;
                Assert.AreEqual(1, env, $"seed {seed}: 환경 퍼즐방 {env}개");
            }
        }

        // 같은 결정의 후반부: "그래도 없으면 보스방은 무조건 열린다" — 놓을 자리가 없는 층도 생성은 돼야 한다.
        // 시작 방·보스 방뿐인 층에는 환경 퍼즐방을 놓을 자리가 없다.
        [Test]
        public void ShouldGenerateFloorWithoutEnvironmentPuzzleWhenNoRoomIsAvailable()
        {
            var map = _gen.Generate(new DungeonConfig { Seed = 3, MinRooms = 2, MaxRooms = 2 });

            Assert.AreEqual(2, map.Rooms.Count);
            foreach (var room in map.Rooms.Values)
                Assert.AreNotEqual(RoomType.EnvironmentPuzzle, room.Type);
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
