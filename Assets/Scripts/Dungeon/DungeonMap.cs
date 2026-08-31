using System.Collections.Generic;

namespace Help.Dungeon
{
    public class DungeonMap
    {
        private readonly Dictionary<(int, int), Room> _rooms = new();

        public IReadOnlyDictionary<(int, int), Room> Rooms => _rooms;
        public (int x, int y) StartPosition { get; }
        public (int x, int y) BossPosition { get; private set; }

        // 이 층의 드랍 테이블 = "층의 알파벳을 다 모으면 만들 수 있는 아이템들"(생성기가 확정).
        // 방들에 뿌린 글자의 총량이 곧 이 목록의 레시피 합이다.
        public List<Help.Item.ItemDefinition> FloorRecipes { get; } = new();

        public DungeonMap((int x, int y) start)
        {
            StartPosition = start;
        }

        public void AddRoom(Room room)
        {
            _rooms[(room.X, room.Y)] = room;
            if (room.Type == RoomType.Boss)
                BossPosition = (room.X, room.Y);
        }

        public Room GetRoom(int x, int y) =>
            _rooms.TryGetValue((x, y), out var room) ? room : null;

        public IEnumerable<Room> FreeRooms()
        {
            foreach (var room in _rooms.Values)
                if (room.IsFreeEntry) yield return room;
        }

        public IEnumerable<Room> ConditionalRooms()
        {
            foreach (var room in _rooms.Values)
                if (!room.IsFreeEntry) yield return room;
        }
    }
}
