using System;
using System.Collections.Generic;
using UnityEngine;
using Help.Player;

namespace Help.Dungeon
{
    public sealed class TemplateValidation
    {
        public List<string> Errors { get; } = new();
        public bool Ok => Errors.Count == 0;

        // 뷰어 오버레이용 — 이 방에서 실제로 갈 수 있는 칸들.
        public ReachabilitySet Reachable { get; internal set; }
        public Dictionary<Direction, Vector2Int> DoorAccess { get; } = new();
    }

    // 방 템플릿이 "돌 수 있는 방"인지 검사한다.
    //
    // FloorValidator가 지키는 "재료 보장 불변식"에 이어지는 두 번째 불변식이다:
    //   **어느 문으로 들어오든 다른 모든 문과 모든 스폰 지점에 갈 수 있어야 한다.**
    // 둘이 합쳐져야 "생성된 던전은 반드시 클리어 가능하다"가 성립한다.
    public static class RoomTemplateValidator
    {
        // 확률 칸이 어떻게 굴러도 막히지 않아야 하므로 양극단을 모두 본다.
        public static TemplateValidation ValidateAllChanceExtremes(RoomTemplate template, PlatformerMetrics metrics)
        {
            var solid = Validate(template, metrics, ChanceMode.AllSolid);
            var empty = Validate(template, metrics, ChanceMode.AllEmpty);

            var merged = new TemplateValidation { Reachable = empty.Reachable };
            foreach (var e in solid.Errors) merged.Errors.Add("[확률칸 전부 채움] " + e);
            foreach (var e in empty.Errors) merged.Errors.Add("[확률칸 전부 비움] " + e);
            foreach (var kv in empty.DoorAccess) merged.DoorAccess[kv.Key] = kv.Value;
            return merged;
        }

        public static TemplateValidation Validate(RoomTemplate template, PlatformerMetrics metrics,
                                                  ChanceMode mode, int seed = 0)
        {
            var room = RoomLayout.Resolve(template, mode, seed);
            return Validate(room, template.Name, metrics);
        }

        public static TemplateValidation Validate(ResolvedRoom room, string name, PlatformerMetrics metrics)
        {
            var result = new TemplateValidation();
            var options = new AnalyzerOptions();
            var world = new StandingQuery(room, options);

            // 1) 문마다 두 지점을 잡는다.
            //    진입(entry) = 그 문으로 들어와 착지하는 칸. 떨어져 들어오는 건 자연스러우므로 낙하를 허용한다.
            //    퇴장(exit)  = 그 문을 쓰려면 서 있어야 하는 칸(문 개구부 바로 옆). 낙하를 허용하지 않는다.
            //    천장 문은 이 둘이 갈린다 — 떨어져 들어올 수는 있어도 올라가지 못하면 나갈 수 없다.
            var exitAccess = new Dictionary<Direction, Vector2Int>();
            foreach (var kv in room.Doors)
            {
                if (TryFindAccess(room, world, kv.Key, kv.Value, out var entry))
                    result.DoorAccess[kv.Key] = entry;
                else
                    result.Errors.Add($"[{name}] {kv.Key} 문 안쪽에 설 자리가 없습니다.");

                if (TryFindExit(world, kv.Key, kv.Value, out var exit))
                    exitAccess[kv.Key] = exit;
                else
                    result.Errors.Add($"[{name}] {kv.Key} 문에 올라설 자리가 없습니다 — 들어올 수는 있어도 나갈 수 없는 문입니다.");
            }
            if (result.Errors.Count > 0) return result;

            // 2) 아무 문에서 출발해도 나머지 문과 모든 스폰 지점에 닿아야 한다.
            //    (실제 던전에서 어느 문이 연결될지는 방마다 다르므로 전 조합을 요구한다)
            foreach (var from in result.DoorAccess)
            {
                var set = ReachabilityAnalyzer.Analyze(room.Tiles, from.Value, metrics, options);
                if (from.Key == FirstDirection(result.DoorAccess)) result.Reachable = set;

                // 목적지는 상대 문의 **퇴장 지점**이다. 착지 지점만 보면
                // "떨어진 자리끼리는 오갈 수 있다"는 약한 조건이 되어, 못 올라가는 문을 놓친다.
                foreach (var to in exitAccess)
                {
                    if (to.Key == from.Key) continue;
                    if (!set.Contains(to.Value))
                        result.Errors.Add($"[{name}] {from.Key} 문에서 {to.Key} 문으로 갈 수 없습니다.");
                }

                foreach (var marker in room.Markers)
                {
                    if (set.Contains(marker.Cell)) continue;
                    // 공중에 찍힌 마커는 아래로 떨어져 자리를 잡는다 — 그 자리가 닿으면 통과.
                    if (TrySettle(world, marker.Cell, out var settled) && set.Contains(settled)) continue;
                    result.Errors.Add(
                        $"[{name}] {from.Key} 문에서 마커 '{marker.Symbol}'({marker.Cell.x},{marker.Cell.y})에 도달할 수 없습니다.");
                }
            }

            return result;
        }

        private static Direction FirstDirection(Dictionary<Direction, Vector2Int> access)
        {
            foreach (Direction d in Enum.GetValues(typeof(Direction)))
                if (access.ContainsKey(d)) return d;
            return Direction.North;
        }

        // 문으로 들어온 플레이어가 실제로 착지하는 칸. 문은 벽 위의 표식이므로
        // 방 안쪽 한 칸에서 시작해 아래로 떨어뜨려 본다.
        private static bool TryFindAccess(ResolvedRoom room, StandingQuery world,
                                          Direction dir, Vector2Int door, out Vector2Int access)
        {
            var inside = dir switch
            {
                Direction.North => new Vector2Int(door.x, door.y - 1),
                Direction.South => new Vector2Int(door.x, door.y + 1),
                Direction.East => new Vector2Int(door.x - 1, door.y),
                _ => new Vector2Int(door.x + 1, door.y),
            };
            return TrySettle(world, inside, out access);
        }

        // 문을 실제로 쓰려면 서 있어야 하는 칸. 문 개구부 바로 안쪽이거나, 그 한 칸 아래까지만 인정한다.
        // (한 칸 여유는 문이 벽 높이에 걸쳐 있을 때를 위한 것 — 그 이상 떨어지면 못 올라가는 문이다.)
        private static bool TryFindExit(StandingQuery world, Direction dir, Vector2Int door, out Vector2Int exit)
        {
            var inside = dir switch
            {
                Direction.North => new Vector2Int(door.x, door.y - 1),
                Direction.South => new Vector2Int(door.x, door.y + 1),
                Direction.East => new Vector2Int(door.x - 1, door.y),
                _ => new Vector2Int(door.x + 1, door.y),
            };

            for (int dy = 0; dy <= 1; dy++)
            {
                int y = inside.y - dy;
                if (y < 1) break;
                if (world.IsStandingNode(inside.x, y)) { exit = new Vector2Int(inside.x, y); return true; }
            }

            exit = inside;
            return false;
        }

        // 주어진 칸에서 아래로 내려가며 처음 만나는 설 자리. 없으면 위로도 한 칸씩 올려 본다.
        private static bool TrySettle(StandingQuery world, Vector2Int from, out Vector2Int settled)
        {
            for (int y = from.y; y >= 1; y--)
                if (world.IsStandingNode(from.x, y)) { settled = new Vector2Int(from.x, y); return true; }

            for (int y = from.y + 1; y < world.Height; y++)
                if (world.IsStandingNode(from.x, y)) { settled = new Vector2Int(from.x, y); return true; }

            settled = from;
            return false;
        }

        // 분석기와 같은 "설 자리" 판정을 검증기에서도 쓰기 위한 얇은 질의 객체.
        private sealed class StandingQuery
        {
            private readonly ResolvedRoom _room;
            private readonly AnalyzerOptions _o;
            public int Height => _room.Height;

            public StandingQuery(ResolvedRoom room, AnalyzerOptions o) { _room = room; _o = o; }

            // D-12: 피해 바닥은 설 자리로 친다(아플 뿐 서 있을 수 있다).
            // ReachabilityAnalyzer.IsStandingNode와 같은 규칙이어야 검증과 분석이 어긋나지 않는다.
            public bool IsStandingNode(int cx, int cy)
            {
                var below = _room.TileAt(cx, cy - 1);
                if (!ResolvedRoom.IsStandable(below)) return false;

                int y1 = Mathf.FloorToInt(cy + _o.BodyHeight - 1e-4f);
                for (int y = cy; y <= y1; y++)
                    if (ResolvedRoom.BlocksMovement(_room.TileAt(cx, y))) return false;

                return true;
            }
        }
    }
}
