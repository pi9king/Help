using System.Collections.Generic;
using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    // 방의 템플릿·콘텐츠를 고르는 시드(2026-09-20 결정): **런마다 바뀌고, 런 안에서는 고정**.
    // 예전엔 좌표만으로 정해져, 같은 좌표·같은 유형이면 매 런 같은 방이 나왔다(정적 재미 리포트).
    public class RoomSeedTests
    {
        // 나갔다 돌아왔는데 방이 바뀌면 안 된다.
        [Test]
        public void SameRunAndCoordinateShouldGiveSameSeed()
        {
            Assert.AreEqual(RoomSeeds.For(1234, 1, 2, -1), RoomSeeds.For(1234, 1, 2, -1));
        }

        [Test]
        public void DifferentCoordinatesInOneRunShouldGiveDifferentSeeds()
        {
            Assert.AreNotEqual(RoomSeeds.For(1234, 1, 0, 0), RoomSeeds.For(1234, 1, 1, 0));
        }

        // 시작 방(0,0)은 예전엔 항상 시드 0이었다. 후보 3개면 런에 따라 셋 다 나와야 한다.
        [Test]
        public void SameCoordinateShouldReachEveryCandidateAcrossRuns()
        {
            var picked = new HashSet<int>();
            for (int run = 0; run < 100; run++)
                picked.Add(RoomContentLibrary.SelectIndex(RoomSeeds.For(run, 1, 0, 0), 3));

            Assert.AreEqual(3, picked.Count);
        }

        // 층 번호도 시드에 들어간다 — 같은 런 시드로 층만 다를 때도 방이 갈린다.
        [Test]
        public void FloorShouldChangeTheSeed()
        {
            Assert.AreNotEqual(RoomSeeds.For(1234, 1, 0, 0), RoomSeeds.For(1234, 2, 0, 0));
        }

        // 랜덤 런(Seed=-1)도 실제로 쓴 시드를 남긴다 — 그 시드로 같은 층을 다시 만들 수 있어야
        // 방 시드가 런 안에서 고정되고, 버그 난 런도 재현된다.
        [Test]
        public void RandomRunShouldRecordASeedThatReproducesTheFloor()
        {
            var gen = new DungeonGenerator();
            var first = gen.Generate(new DungeonConfig { Seed = -1 });
            var again = gen.Generate(new DungeonConfig { Seed = first.Seed });

            Assert.AreEqual(first.Rooms.Count, again.Rooms.Count);
            foreach (var kv in first.Rooms)
                Assert.AreEqual(kv.Value.Type, again.Rooms[kv.Key].Type, $"{kv.Key} 유형이 다름");
        }

        [Test]
        public void FixedSeedShouldBeRecordedAsIs()
        {
            var map = new DungeonGenerator().Generate(new DungeonConfig { Seed = 42 });
            Assert.AreEqual(42, map.Seed);
        }
    }
}
