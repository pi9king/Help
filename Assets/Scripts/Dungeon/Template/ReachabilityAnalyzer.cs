using System;
using System.Collections.Generic;
using UnityEngine;
using Help.Player;

namespace Help.Dungeon
{
    public sealed class AnalyzerOptions
    {
        // 대시를 쓸 수 있다고 보고 분석할지. 끄면 "기본 조작만으로 갈 수 있는 곳"이 나온다.
        public bool AllowDash = true;

        // 플레이어 몸통(타일 단위). 실제 콜라이더(0.5 x 1.0)보다 조금 크게 잡아
        // 판정이 실제보다 관대해지지 않게 한다 — 갈 수 있다고 했는데 못 가는 게 최악이다.
        public float BodyWidth = 0.6f;
        public float BodyHeight = 1.2f;

        public float TimeStep = 0.02f;      // Unity FixedUpdate 기본값과 동일
        public float MaxFlightTime = 3f;    // 한 동작을 이만큼까지 따라가 본다
    }

    public sealed class ReachabilitySet
    {
        private readonly HashSet<Vector2Int> _nodes;
        public ReachabilitySet(HashSet<Vector2Int> nodes) { _nodes = nodes; }

        public bool Contains(Vector2Int cell) => _nodes.Contains(cell);
        public int Count => _nodes.Count;
        public IEnumerable<Vector2Int> Nodes => _nodes;
    }

    // 방 지형에서 "플레이어가 실제로 설 수 있는 칸"의 집합을 구한다.
    //
    // 닫힌 수식으로 근사하지 않고 **실제 물리를 그대로 시뮬레이션**한다 —
    // 중력·점프력·대시가 PlayerController와 같은 값(PlatformerMetrics)에서 나오므로,
    // 물리를 튜닝하면 판정도 자동으로 따라온다. 천장에 막히거나 벽에 걸리는 경우도
    // 근사식과 달리 저절로 맞는다.
    //
    // 판정은 보수적이어야 한다: "갈 수 있는데 못 간다"고 하면 방이 조금 답답할 뿐이지만,
    // "못 가는데 갈 수 있다"고 하면 클리어 불가능한 방이 배포된다.
    public static class ReachabilityAnalyzer
    {
        private const float Eps = 1e-4f;

        public static ReachabilitySet Analyze(TileKind[,] tiles, Vector2Int start,
                                              PlatformerMetrics metrics, AnalyzerOptions options)
            => Analyze(tiles, new[] { start }, metrics, options);

        public static ReachabilitySet Analyze(TileKind[,] tiles, IEnumerable<Vector2Int> starts,
                                              PlatformerMetrics metrics, AnalyzerOptions options)
        {
            options ??= new AnalyzerOptions();
            var world = new World(tiles, options);

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            foreach (var s in starts)
                if (visited.Add(s)) queue.Enqueue(s);

            var landed = new List<Vector2Int>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                foreach (var action in Actions(metrics, options))
                {
                    landed.Clear();
                    Simulate(world, node, action, metrics, options, landed);
                    foreach (var cell in landed)
                        if (visited.Add(cell)) queue.Enqueue(cell);
                }
            }

            return new ReachabilitySet(visited);
        }

        // --- 동작 목록 -------------------------------------------------------

        private readonly struct Action
        {
            public readonly int Dir;          // -1, 0, +1
            public readonly bool Jump;
            public readonly float DashAt;     // 대시 시작 시각(초). 음수면 대시 없음.
            public Action(int dir, bool jump, float dashAt) { Dir = dir; Jump = jump; DashAt = dashAt; }
        }

        private static IEnumerable<Action> Actions(PlatformerMetrics m, AnalyzerOptions o)
        {
            // 걷기/난간에서 떨어지기 — 지상 이동은 이 시뮬레이션이 통째로 처리한다.
            yield return new Action(-1, false, -1f);
            yield return new Action(1, false, -1f);

            // 점프 (수직, 좌, 우)
            yield return new Action(0, true, -1f);
            yield return new Action(-1, true, -1f);
            yield return new Action(1, true, -1f);

            if (!o.AllowDash) yield break;

            // 지상 대시
            yield return new Action(-1, false, 0f);
            yield return new Action(1, false, 0f);

            // 점프 후 정점에서 대시 — 갭을 가장 멀리 건너는 조작
            yield return new Action(-1, true, m.RiseTime);
            yield return new Action(1, true, m.RiseTime);
        }

        // --- 시뮬레이션 -------------------------------------------------------

        private static void Simulate(World world, Vector2Int startNode, Action action,
                                     PlatformerMetrics m, AnalyzerOptions o, List<Vector2Int> landed)
        {
            float px = startNode.x + 0.5f;
            float py = startNode.y;
            float vx = action.Dir * m.MoveSpeed;
            float vy = action.Jump ? m.JumpForce : 0f;
            bool grounded = !action.Jump;

            float dt = o.TimeStep;
            int steps = Mathf.CeilToInt(o.MaxFlightTime / dt);

            for (int i = 0; i < steps; i++)
            {
                float t = i * dt;

                // 대시: 수평 속도를 덮어쓰고 수직 속도를 0으로 만든다(PlayerController.StartDash와 동일).
                bool dashing = action.DashAt >= 0f && t >= action.DashAt && t < action.DashAt + m.DashDuration;
                if (dashing)
                {
                    if (t < action.DashAt + dt) vy = 0f;     // 대시 시작 프레임에만 수직 속도 제거
                    vx = action.Dir * m.DashForce;
                    grounded = false;                        // 대시 중엔 접지 보정을 하지 않는다
                }
                else if (action.DashAt >= 0f && t >= action.DashAt)
                {
                    vx = action.Dir * m.MoveSpeed;
                }

                if (!grounded)
                    vy -= (vy > 0f ? m.RiseGravity : m.FallGravity) * dt;

                // 수평 이동 — 벽에 막히면 위치만 멈추고 속도는 유지한다.
                // (실제 조작에서도 방향키를 누른 채 벽을 타고 올라가면 난간 위로 넘어간다)
                bool hitWall = false;
                if (vx != 0f)
                {
                    float nx = px + vx * dt;
                    if (world.Blocked(nx, py)) hitWall = true;
                    else px = nx;
                }

                if (grounded)
                {
                    // 지상: 발밑 지지를 확인하고, 지지가 사라지면 그때부터 공중이다.
                    if (world.TrySupport(px, py, out float supportTop))
                    {
                        // D-12: 피해 바닥 위도 걸어서 지나간다 — 위험 지형은 장벽이 아니라 비용이다.
                        py = supportTop;
                        vy = 0f;
                        Record(world, landed, px, py);
                        if (hitWall) return;                 // 벽에 붙어 멈췄다 — 더 볼 것 없음
                        continue;
                    }
                    grounded = false;
                }

                float ny = py + vy * dt;

                if (vy > 0f)
                {
                    if (world.Blocked(px, ny)) vy = 0f;      // 천장
                    else py = ny;
                }
                else
                {
                    if (world.TryLand(px, py, ny, out float landTop))
                    {
                        // D-12: 피해 바닥에 착지해도 경로는 이어진다 — 아플 뿐 계속 갈 수 있다.
                        py = landTop;
                        vy = 0f;
                        grounded = true;
                        Record(world, landed, px, py);
                        continue;
                    }
                    py = ny;
                }

                // D-12: 피해 바닥은 경로를 죽이지 않는다 — 아플 뿐 지나갈 수 있다.
                // 길을 막는 건 벽의 일이다(OutOfBounds/Blocked가 담당).
                if (world.OutOfBounds(px, py)) return;
            }
        }

        private static void Record(World world, List<Vector2Int> landed, float px, float py)
        {
            int cx = Mathf.FloorToInt(px);
            int cy = Mathf.RoundToInt(py);
            if (!world.IsStandingNode(cx, cy)) return;
            var cell = new Vector2Int(cx, cy);
            if (!landed.Contains(cell)) landed.Add(cell);
        }

        // --- 격자 질의 -------------------------------------------------------

        private sealed class World
        {
            private readonly TileKind[,] _tiles;
            private readonly AnalyzerOptions _o;
            public readonly int Width, Height;

            public World(TileKind[,] tiles, AnalyzerOptions o)
            {
                _tiles = tiles;
                _o = o;
                Width = tiles.GetLength(0);
                Height = tiles.GetLength(1);
            }

            // 방 밖은 벽으로 본다 — 플레이어가 방을 빠져나가는 경로가 생기면 안 된다.
            public TileKind At(int x, int y) =>
                x < 0 || y < 0 || x >= Width || y >= Height ? TileKind.Wall : _tiles[x, y];

            private int MinCol(float px) => Mathf.FloorToInt(px - _o.BodyWidth * 0.5f);
            private int MaxCol(float px) => Mathf.FloorToInt(px + _o.BodyWidth * 0.5f - Eps);

            public bool Blocked(float px, float py)
            {
                int y0 = Mathf.FloorToInt(py);
                int y1 = Mathf.FloorToInt(py + _o.BodyHeight - Eps);
                for (int x = MinCol(px); x <= MaxCol(px); x++)
                    for (int y = y0; y <= y1; y++)
                        if (ResolvedRoom.BlocksMovement(At(x, y))) return true;
                return false;
            }


            public bool OutOfBounds(float px, float py) =>
                px < -1f || px > Width + 1f || py < -1f || py > Height + 1f;

            // 발밑(py 바로 아래 행)에 딛고 설 지형이 있는가.
            public bool TrySupport(float px, float py, out float top)
            {
                top = py;
                int y = Mathf.FloorToInt(py + Eps) - 1;
                bool found = false;
                for (int x = MinCol(px); x <= MaxCol(px); x++)
                {
                    if (!ResolvedRoom.IsStandable(At(x, y))) continue;
                    found = true;
                }
                if (found) top = y + 1;
                return found;
            }

            // py(위) → ny(아래)로 내려오는 사이에 밟게 되는 가장 높은 지면.
            public bool TryLand(float px, float py, float ny, out float top)
            {
                top = 0f;
                bool found = false;

                int yLow = Mathf.FloorToInt(ny) - 1;
                int yHigh = Mathf.FloorToInt(py);
                for (int x = MinCol(px); x <= MaxCol(px); x++)
                {
                    for (int y = yHigh; y >= yLow; y--)
                    {
                        var k = At(x, y);
                        if (!ResolvedRoom.IsStandable(k)) continue;

                        float candidate = y + 1;
                        // 발이 이미 그 면보다 아래였으면 이번 낙하로 밟은 게 아니다(발판 아래에서 통과 중)
                        if (candidate > py + Eps) continue;
                        if (candidate < ny - Eps) break;      // 이번 스텝에 닿지 않는 깊이

                        if (!found || candidate > top)
                        {
                            top = candidate;
                            found = true;
                        }
                        break;
                    }
                }
                return found;
            }

            // 이 칸이 "서 있을 수 있는 자리"인가 — 발밑이 안전한 지형이고 몸이 들어갈 공간이 있다.
            public bool IsStandingNode(int cx, int cy)
            {
                var below = At(cx, cy - 1);
                if (!ResolvedRoom.IsStandable(below)) return false;

                // D-12: 발밑이 피해 바닥이어도 설 자리로 친다 —
                // 못 서면 움푹 판 곳에 빠졌을 때 걸어 나올 수가 없다.
                float px = cx + 0.5f;
                if (Blocked(px, cy)) return false;
                return true;
            }
        }
    }
}
