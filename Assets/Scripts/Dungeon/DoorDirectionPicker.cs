using System;
using System.Collections.Generic;

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
