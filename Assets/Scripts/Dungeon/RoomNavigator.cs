using Help.Crafting;

namespace Help.Dungeon
{
    // 방 이동 판정 순수 로직: 방향으로 인접 방을 찾아 진입 조건을 확인한다.
    // MonoBehaviour(RoomManager)가 이 로직을 호출해 실제 방 전환을 수행한다.
    public static class RoomNavigator
    {
        public static (int x, int y)? GetNeighbor(Room room, Direction dir)
        {
            return dir switch
            {
                Direction.North => room.North,
                Direction.South => room.South,
                Direction.East => room.East,
                Direction.West => room.West,
                _ => null
            };
        }

        public static RoomEntryResult TryEnter(
            Room current,
            Direction dir,
            DungeonMap map,
            Help.Inventory.Inventory inventory,
            RecipeDatabase database,
            out Room target)
        {
            target = null;

            var neighbor = GetNeighbor(current, dir);
            if (neighbor == null) return RoomEntryResult.NoDoor;

            var room = map.GetRoom(neighbor.Value.x, neighbor.Value.y);
            if (room == null) return RoomEntryResult.NoDoor;

            if (!EntryRequirementChecker.CanEnter(room, inventory, database))
                return RoomEntryResult.Blocked;

            target = room;
            return RoomEntryResult.Entered;
        }
    }
}
