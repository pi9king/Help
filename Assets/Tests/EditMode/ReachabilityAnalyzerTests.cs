using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;
using Help.Player;

namespace Tests.EditMode
{
    public class ReachabilityAnalyzerTests
    {
        // 테스트용 미니 격자. 첫 줄이 천장(y가 큰 쪽).
        private static TileKind[,] Grid(params string[] rows)
        {
            int h = rows.Length, w = rows[0].Length;
            var t = new TileKind[w, h];
            for (int row = 0; row < h; row++)
                for (int x = 0; x < w; x++)
                {
                    int y = (h - 1) - row;
                    char c = rows[row][x];
                    // D-12 이후 '~'는 없다. 갭(구멍)은 바닥이 **비어 있는 것**(.)으로,
                    // 피해 지형은 '^'로 쓴다 — 예전엔 '~'가 두 뜻으로 섞여 쓰였다.
                    t[x, y] = c == '#' ? TileKind.Wall
                            : c == '=' ? TileKind.Floor
                            : c == '-' ? TileKind.Platform
                            : c == '^' ? TileKind.Hazard
                            : TileKind.Empty;
                }
            return t;
        }

        private static PlatformerMetrics M { get { return PlatformerMetrics.PlayerDefault; } }

        private static bool Reaches(TileKind[,] grid, Vector2Int from, Vector2Int to, bool dash = false)
        {
            var set = ReachabilityAnalyzer.Analyze(grid, from, M, new AnalyzerOptions { AllowDash = dash });
            return set.Contains(to);
        }

        // 바닥만 있는 방
        private static TileKind[,] Flat(int w = 12, int h = 8)
        {
            var rows = new string[h];
            for (int row = 0; row < h; row++)
            {
                int y = (h - 1) - row;
                rows[row] = y == 0 ? new string('=', w) : "#" + new string('.', w - 2) + "#";
            }
            return Grid(rows);
        }

        [Test]
        public void ShouldWalkAcrossFlatGround()
        {
            Assert.IsTrue(Reaches(Flat(), new Vector2Int(1, 1), new Vector2Int(10, 1)));
        }

        [Test]
        public void StartNodeShouldAlwaysBeReachable()
        {
            var set = ReachabilityAnalyzer.Analyze(Flat(), new Vector2Int(1, 1), M, new AnalyzerOptions());
            Assert.IsTrue(set.Contains(new Vector2Int(1, 1)));
        }

        [Test]
        public void ShouldNotStandInsideSolidTile()
        {
            var set = ReachabilityAnalyzer.Analyze(Flat(), new Vector2Int(1, 1), M, new AnalyzerOptions());
            Assert.IsFalse(set.Contains(new Vector2Int(1, 0)), "바닥 타일 안에는 설 수 없다");
        }

        [Test]
        public void ShouldClimbThreeTileLedge()
        {
            var g = Grid(
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#.....======",
                "#.....======",
                "#.....======",
                "============");
            Assert.IsTrue(Reaches(g, new Vector2Int(2, 1), new Vector2Int(8, 4)),
                "3타일 단차는 점프로 올라갈 수 있어야 한다");
        }

        [Test]
        public void ShouldNotClimbFiveTileLedge()
        {
            var g = Grid(
                "#..........#",
                "#..........#",
                "#.....======",
                "#.....======",
                "#.....======",
                "#.....======",
                "#.....======",
                "============");
            Assert.IsFalse(Reaches(g, new Vector2Int(2, 1), new Vector2Int(8, 6)),
                "점프 높이(3.5타일)를 넘는 단차는 벽이어야 한다");
        }

        [Test]
        public void ShouldJumpNarrowGap()
        {
            var g = Grid(
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "====..======");
            Assert.IsTrue(Reaches(g, new Vector2Int(1, 1), new Vector2Int(10, 1)), "2칸 갭은 점프로 건넌다");
        }

        [Test]
        public void ShouldNotCrossVeryWideGap()
        {
            var g = Grid(
                "#....................#",
                "#....................#",
                "#....................#",
                "#....................#",
                "#....................#",
                "#....................#",
                "#....................#",
                "==..................==");
            Assert.IsFalse(Reaches(g, new Vector2Int(1, 1), new Vector2Int(20, 1)),
                "18칸 갭은 어떤 조작으로도 건널 수 없다");
        }

        [Test]
        public void DashShouldExtendGapCrossing()
        {
            int noDash = MaxCrossableGap(false);
            int withDash = MaxCrossableGap(true);
            Assert.Greater(noDash, 0, "대시 없이도 어느 정도 갭은 건너야 한다");
            Assert.Greater(withDash, noDash, "대시는 건널 수 있는 갭을 넓혀야 한다");
        }

        private static int MaxCrossableGap(bool dash)
        {
            int best = 0;
            for (int gap = 1; gap <= 14; gap++)
            {
                int w = gap + 6;
                var rows = new string[8];
                for (int row = 0; row < 8; row++)
                {
                    int y = 7 - row;
                    rows[row] = y == 0
                        ? "===" + new string('.', gap) + "==="
                        : "#" + new string('.', w - 2) + "#";
                }
                if (Reaches(Grid(rows), new Vector2Int(1, 1), new Vector2Int(w - 2, 1), dash)) best = gap;
            }
            return best;
        }

        // D-12: 위험 지형은 **장벽이 아니라 비용**이다. 길을 막는 건 벽의 일이다.
        // (예전엔 이 두 테스트가 정반대를 단언했다 — 인터뷰에서 규칙이 뒤집혔다)
        [Test]
        public void ShouldWalkThroughHazardAtACost()
        {
            // 천장이 낮아 점프로 넘을 수 없다. 그래도 피해 바닥을 밟고 지나갈 수 있어야 한다.
            var g = Grid(
                "############",
                "############",
                "############",
                "############",
                "############",
                "#..........#",
                "#..........#",
                "====^^^^====");
            Assert.IsTrue(Reaches(g, new Vector2Int(1, 1), new Vector2Int(10, 1)),
                "피해 바닥은 아플 뿐 지나갈 수 있어야 한다");
        }

        [Test]
        public void ShouldStandOnHazard()
        {
            var g = Grid(
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "====^^^^====");
            var set = ReachabilityAnalyzer.Analyze(g, new Vector2Int(1, 1), M, new AnalyzerOptions());
            Assert.IsTrue(set.Contains(new Vector2Int(5, 1)),
                "피해 바닥에 설 수 없으면 움푹 판 곳에서 걸어 나올 수 없다");
        }

        [Test]
        public void ShouldLandOnOneWayPlatform()
        {
            var g = Grid(
                "#..........#",
                "#..........#",
                "#..........#",
                "#..........#",
                "#...---....#",
                "#..........#",
                "#..........#",
                "============");
            Assert.IsTrue(Reaches(g, new Vector2Int(1, 1), new Vector2Int(5, 4)),
                "일방통행 발판 위에 올라설 수 있어야 한다");
        }

        [Test]
        public void ShouldNotSqueezeThroughOneTileCorridor()
        {
            // 좌우 방을 잇는 유일한 길이 높이 1칸(y=3)이라 몸이 들어가지 않는다.
            var g = Grid(
                "############",
                "############",
                "############",
                "############",
                "#####..#####",
                "#....##....#",
                "#....##....#",
                "============");
            Assert.IsFalse(Reaches(g, new Vector2Int(1, 1), new Vector2Int(10, 1)),
                "높이 1칸 통로는 통과 불가(웅크리기 없음)");
        }

        [Test]
        public void ShouldReportUnreachableCells()
        {
            var g = Grid(
                "#..........#",
                "#..........#",
                "#.....######",
                "#.....#....#",
                "#.....#....#",
                "#.....#....#",
                "#.....#....#",
                "============");
            var set = ReachabilityAnalyzer.Analyze(g, new Vector2Int(1, 1), M, new AnalyzerOptions());
            Assert.IsTrue(set.Contains(new Vector2Int(2, 1)));
            Assert.IsFalse(set.Contains(new Vector2Int(9, 1)), "벽으로 막힌 구역은 도달 불가");
        }
    }
}
