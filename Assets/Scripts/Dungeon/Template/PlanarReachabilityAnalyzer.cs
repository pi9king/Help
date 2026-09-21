using System.Collections.Generic;
using UnityEngine;

namespace Help.Dungeon
{
    /// <summary>
    /// 쿼터뷰 방의 평면 도달성을 검사한다. 8방향 이동을 허용하되,
    /// 두 벽 사이 대각선 틈으로 비집고 지나가는 코너 커팅은 금지한다.
    /// </summary>
    public static class PlanarReachabilityAnalyzer
    {
        private static readonly Vector2Int[] Directions =
        {
            new(1, 0), new(-1, 0), new(0, 1), new(0, -1),
            new(1, 1), new(1, -1), new(-1, 1), new(-1, -1),
        };

        public static ReachabilitySet Analyze(TileKind[,] tiles, Vector2Int start)
        {
            var visited = new HashSet<Vector2Int>();
            if (!Walkable(tiles, start)) return new ReachabilitySet(visited);

            var queue = new Queue<Vector2Int>();
            visited.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                foreach (Vector2Int direction in Directions)
                {
                    Vector2Int next = current + direction;
                    if (visited.Contains(next) || !Walkable(tiles, next)) continue;
                    if (direction.x != 0 && direction.y != 0 && !CanCrossCorner(tiles, current, direction)) continue;
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return new ReachabilitySet(visited);
        }

        private static bool CanCrossCorner(TileKind[,] tiles, Vector2Int from, Vector2Int direction) =>
            Walkable(tiles, from + new Vector2Int(direction.x, 0)) &&
            Walkable(tiles, from + new Vector2Int(0, direction.y));

        private static bool Walkable(TileKind[,] tiles, Vector2Int cell)
        {
            if (cell.x < 0 || cell.y < 0 ||
                cell.x >= tiles.GetLength(0) || cell.y >= tiles.GetLength(1)) return false;
            return ResolvedRoom.IsWalkable(tiles[cell.x, cell.y]);
        }
    }
}
