using System;
using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    // 순수 로직: 방 중심(원점) 기준 플레이어 로컬 좌표에서 가장 가까운(향한) 문 방향.
    // 방 간 이동 입력 시 어느 문으로 나갈지 결정하는 데 사용.
    public static class DoorDirectionPicker
    {
        public static Direction Nearest(float x, float y)
        {
            return Math.Abs(x) >= Math.Abs(y)
                ? (x >= 0 ? Direction.East : Direction.West)
                : (y >= 0 ? Direction.North : Direction.South);
        }

        // 후보(실제 존재하는 문) 중 플레이어가 가장 향해 있는 방향. 후보 없으면 null.
        public static Direction? NearestAmong(float x, float y, IEnumerable<Direction> candidates)
        {
            Direction? best = null;
            float bestScore = float.NegativeInfinity;
            foreach (var d in candidates)
            {
                float s = Score(d, x, y);
                if (s > bestScore) { bestScore = s; best = d; }
            }
            return best;
        }

        // 문을 **장소**로 취급하는 선택: 실제 문 위치에서 radius 안에 있을 때만 그 문을 쓸 수 있다.
        //
        // NearestAmong은 방향 점수만 보므로(오른쪽에 있으면 동문) 방 어디서든 E가 먹혔고,
        // 그래서 지형과 문이 아무 관계가 없었다 — 천장 문에 못 올라가도 그냥 나가졌다.
        // 거리를 조건으로 걸어야 "문까지 가는 길"이 레벨 디자인의 문제가 된다.
        public static Direction? NearestWithin(float px, float py,
            IReadOnlyDictionary<Direction, Vector2> doors, float radius)
        {
            Direction? best = null;
            float bestSqr = radius * radius + 1e-4f; // 경계값 포함
            foreach (var kv in doors)
            {
                float dx = kv.Value.x - px, dy = kv.Value.y - py;
                float sqr = dx * dx + dy * dy;
                if (sqr <= bestSqr) { bestSqr = sqr; best = kv.Key; }
            }
            return best;
        }

        public static Direction Opposite(Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East => Direction.West,
            _ => Direction.East, // West
        };

        private static float Score(Direction d, float x, float y) => d switch
        {
            Direction.East => x,
            Direction.West => -x,
            Direction.North => y,
            _ => -y, // South
        };
    }
}
