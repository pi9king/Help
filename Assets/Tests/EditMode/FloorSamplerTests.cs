using System.Collections.Generic;
using NUnit.Framework;
using Help.Dungeon;
using Help.Editor.Playtest;
using Help.Item;

namespace Tests.EditMode
{
    // 정적 재미 리포트의 층 표본 통계(체크리스트 P1-6·P1-7)가 올바르게 세는지만 검증한다.
    public class FloorSamplerTests
    {
        private static DungeonMap Map(params Room[] rooms)
        {
            var map = new DungeonMap((0, 0));
            foreach (var r in rooms) map.AddRoom(r);
            return map;
        }

        private static Room RoomWithLetters(int x, RoomType type, int letters)
        {
            var room = new Room(x, 0, type);
            for (int i = 0; i < letters; i++)
                room.GuaranteedLoot.Add(new MaterialRequirement(AlphabetMaterial.A, 1));
            return room;
        }

        // 글자가 적보다 적으면 빈손 적이 생긴다 — 배분 규칙은 LootDistribution을 그대로 따른다.
        [Test]
        public void ShouldCountEnemiesThatDropNothing()
        {
            var map = Map(new Room(0, 0, RoomType.Tutorial), RoomWithLetters(1, RoomType.Combat, 2));

            var stats = FloorSampler.Measure(new[] { map }, type => type == RoomType.Combat ? 3 : 0);

            Assert.AreEqual(3, stats.Enemies);
            Assert.AreEqual(1, stats.EmptyHandedEnemies);
        }

        [Test]
        public void ShouldCountFloorsMissingARoomType()
        {
            var withEnv = Map(new Room(0, 0, RoomType.Tutorial), new Room(1, 0, RoomType.EnvironmentPuzzle));
            var without = Map(new Room(0, 0, RoomType.Tutorial), new Room(1, 0, RoomType.Combat));

            var stats = FloorSampler.Measure(new[] { withEnv, without }, _ => 0);

            Assert.AreEqual(2, stats.Floors);
            Assert.AreEqual(1, stats.FloorsWithout(RoomType.EnvironmentPuzzle));
            Assert.AreEqual(2, stats.FloorsWithout(RoomType.Shop));
        }

        [Test]
        public void ShouldCountRoomsWithoutLettersPerType()
        {
            var map = Map(RoomWithLetters(0, RoomType.Treasure, 0),
                          RoomWithLetters(1, RoomType.Treasure, 2),
                          RoomWithLetters(2, RoomType.Combat, 0));

            var stats = FloorSampler.Measure(new[] { map }, _ => 0);

            Assert.AreEqual(2, stats.RoomsOf(RoomType.Treasure));
            Assert.AreEqual(1, stats.RoomsWithoutLetters(RoomType.Treasure));
            Assert.AreEqual(1, stats.RoomsWithoutLetters(RoomType.Combat));
        }
    }
}
