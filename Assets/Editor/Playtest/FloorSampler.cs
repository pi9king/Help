using System;
using System.Collections.Generic;
using System.Linq;
using Help.Dungeon;

namespace Help.Editor.Playtest
{
    public sealed class FloorSampleStats
    {
        public int Floors;
        public int Rooms;
        public int ConditionalRooms;
        public int Enemies;
        public int EmptyHandedEnemies;

        private readonly Dictionary<RoomType, int> _roomsOf = new();
        private readonly Dictionary<RoomType, int> _floorsWith = new();
        private readonly Dictionary<RoomType, int> _roomsWithoutLetters = new();

        public int RoomsOf(RoomType t) => _roomsOf.TryGetValue(t, out var n) ? n : 0;
        public int FloorsWithout(RoomType t) => Floors - (_floorsWith.TryGetValue(t, out var n) ? n : 0);
        public int RoomsWithoutLetters(RoomType t) => _roomsWithoutLetters.TryGetValue(t, out var n) ? n : 0;

        internal void AddRoom(RoomType t, bool hasLetters)
        {
            Rooms++;
            Bump(_roomsOf, t);
            if (!hasLetters) Bump(_roomsWithoutLetters, t);
        }

        internal void AddFloorPresence(IEnumerable<RoomType> typesOnFloor)
        {
            Floors++;
            foreach (var t in typesOnFloor.Distinct()) Bump(_floorsWith, t);
        }

        private static void Bump(Dictionary<RoomType, int> d, RoomType t) =>
            d[t] = d.TryGetValue(t, out var n) ? n + 1 : 1;
    }

    // 생성된 층 여러 장을 모아 "플레이어가 실제로 마주칠 분포"를 잰다 — 체크리스트 P1-6·P1-7의 측정 쪽.
    // enemyCountOf: 방 유형 → 그 방 콘텐츠의 적 수(에디터가 콘텐츠 프리팹에서 센다).
    public static class FloorSampler
    {
        public static FloorSampleStats Measure(IEnumerable<DungeonMap> maps, Func<RoomType, int> enemyCountOf)
        {
            var stats = new FloorSampleStats();
            foreach (var map in maps)
            {
                stats.AddFloorPresence(map.Rooms.Values.Select(r => r.Type));
                foreach (var room in map.Rooms.Values)
                {
                    int letters = room.GuaranteedLoot.Sum(m => m.Count) + room.BonusLoot.Sum(m => m.Count);
                    stats.AddRoom(room.Type, letters > 0);
                    if (!room.IsFreeEntry) stats.ConditionalRooms++;

                    int enemies = enemyCountOf(room.Type);
                    if (enemies <= 0) continue;
                    // 배분 규칙을 복제하지 않는다 — 런타임과 같은 LootDistribution으로 나눠 본다.
                    // (빈손 적의 수는 시드와 무관하다. 누가 빈손인지만 시드에 따라 바뀐다.)
                    var shares = LootDistribution.Assign(letters, enemies, 0);
                    stats.Enemies += enemies;
                    stats.EmptyHandedEnemies += shares.Count(s => s == 0);
                }
            }
            return stats;
        }
    }
}
