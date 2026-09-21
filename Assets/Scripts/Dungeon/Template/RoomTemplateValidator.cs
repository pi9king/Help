using System;
using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    public sealed class TemplateValidation
    {
        public List<string> Errors { get; } = new();
        public bool Ok => Errors.Count == 0;
        public ReachabilitySet Reachable { get; internal set; }
        public Dictionary<Direction, Vector2Int> DoorAccess { get; } = new();
    }

    /// <summary>
    /// 쿼터뷰 방에서 모든 문과 필수 마커가 하나의 평면 이동 영역으로 연결되는지 검사한다.
    /// </summary>
    public static class RoomTemplateValidator
    {
        public static TemplateValidation ValidateAllChanceExtremes(RoomTemplate template)
        {
            var solid = Validate(template, ChanceMode.AllSolid);
            var empty = Validate(template, ChanceMode.AllEmpty);
            var merged = new TemplateValidation { Reachable = empty.Reachable };
            foreach (string error in solid.Errors) merged.Errors.Add("[확률칸 전부 채움] " + error);
            foreach (string error in empty.Errors) merged.Errors.Add("[확률칸 전부 비움] " + error);
            foreach (var pair in empty.DoorAccess) merged.DoorAccess[pair.Key] = pair.Value;
            return merged;
        }

        public static TemplateValidation Validate(RoomTemplate template, ChanceMode mode, int seed = 0) =>
            Validate(RoomLayout.Resolve(template, mode, seed), template.Name);

        public static TemplateValidation Validate(ResolvedRoom room, string name)
        {
            var result = new TemplateValidation();
            ValidateBorder(room, name, result);
            foreach (var pair in room.Doors)
            {
                Vector2Int access = Inside(pair.Key, pair.Value);
                if (!ResolvedRoom.IsWalkable(room.TileAt(access.x, access.y)))
                    result.Errors.Add($"[{name}] {pair.Key} 문 안쪽 {access}가 이동 가능한 바닥이 아닙니다.");
                else
                    result.DoorAccess[pair.Key] = access;
            }

            if (result.Errors.Count > 0 || result.DoorAccess.Count == 0) return result;

            Direction first = FirstDirection(result.DoorAccess);
            ReachabilitySet reachable = PlanarReachabilityAnalyzer.Analyze(room.Tiles, result.DoorAccess[first]);
            result.Reachable = reachable;

            foreach (var pair in result.DoorAccess)
                if (!reachable.Contains(pair.Value))
                    result.Errors.Add($"[{name}] {first} 문에서 {pair.Key} 문으로 갈 수 없습니다.");

            foreach (RoomMarker marker in room.Markers)
            {
                if (!ResolvedRoom.IsWalkable(room.TileAt(marker.Cell.x, marker.Cell.y)))
                    result.Errors.Add($"[{name}] 마커 '{marker.Symbol}'({marker.Cell.x},{marker.Cell.y})가 바닥 위에 있지 않습니다.");
                else if (!reachable.Contains(marker.Cell))
                    result.Errors.Add($"[{name}] {first} 문에서 마커 '{marker.Symbol}'({marker.Cell.x},{marker.Cell.y})에 도달할 수 없습니다.");
            }

            return result;
        }

        private static void ValidateBorder(ResolvedRoom room, string name, TemplateValidation result)
        {
            var doorCells = new HashSet<Vector2Int>(room.Doors.Values);
            for (int x = 0; x < room.Width; x++)
                for (int y = 0; y < room.Height; y++)
                {
                    bool border = x == 0 || y == 0 || x == room.Width - 1 || y == room.Height - 1;
                    if (!border) continue;
                    var cell = new Vector2Int(x, y);
                    if (doorCells.Contains(cell)) continue;
                    if (room.TileAt(x, y) != TileKind.Wall)
                        result.Errors.Add($"[{name}] 테두리 {cell}가 벽으로 닫혀 있지 않습니다.");
                }
        }

        public static Vector2Int Inside(Direction direction, Vector2Int door) => direction switch
        {
            Direction.North => door + Vector2Int.down,
            Direction.South => door + Vector2Int.up,
            Direction.East => door + Vector2Int.left,
            _ => door + Vector2Int.right,
        };

        private static Direction FirstDirection(Dictionary<Direction, Vector2Int> access)
        {
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                if (access.ContainsKey(direction)) return direction;
            return Direction.North;
        }
    }
}
