using System;
using System.Collections.Generic;
using Help.Crafting;

namespace Help.Dungeon
{
    public enum DoorState { None, Open, Locked }

    // 순수 로직: 현재 방의 각 방향 문 상태를 판정한다.
    // 연결 없음=None, 진입 가능=Open, 조건 불충족=Locked (EntryRequirementChecker 재사용).
    // "왜 잠겼는지"는 노출하지 않는다(DESIGN: 가능/불가만 표시).
    public static class DoorStateEvaluator
    {
        public static Dictionary<Direction, DoorState> Evaluate(
            Room current, DungeonMap map, Help.Inventory.Inventory inventory, RecipeDatabase database)
        {
            var result = new Dictionary<Direction, DoorState>();
            foreach (Direction dir in Enum.GetValues(typeof(Direction)))
            {
                var n = RoomNavigator.GetNeighbor(current, dir);
                Room neighbor = n == null ? null : map.GetRoom(n.Value.x, n.Value.y);
                if (neighbor == null) { result[dir] = DoorState.None; continue; }
                result[dir] = EntryRequirementChecker.CanEnter(neighbor, inventory, database)
                    ? DoorState.Open : DoorState.Locked;
            }
            return result;
        }
    }
}
