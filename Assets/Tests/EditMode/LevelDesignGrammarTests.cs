using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;
using Help.Player;

namespace Tests.EditMode
{
    // 레벨 디자인 문법(Docs/LEVEL_DESIGN.md)의 수치를 못 박는다.
    //
    // 물리를 튜닝하면 이 테스트가 먼저 깨진다 — 그게 목적이다.
    // 깨졌다면 값을 고치는 게 아니라, **문서와 기존 템플릿을 함께 다시 봐야 한다**는 신호다.
    public class LevelDesignGrammarTests
    {
        private static TileKind[,] Grid(string[] rows)
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
                            : TileKind.Empty;
                }
            return t;
        }

        private static int MaxCrossableGap(bool dash)
        {
            var m = PlatformerMetrics.PlayerDefault;
            int best = 0;
            for (int gap = 1; gap <= 16; gap++)
            {
                int w = gap + 8;
                var rows = new string[10];
                for (int row = 0; row < 10; row++)
                {
                    int y = 9 - row;
                    rows[row] = y == 0 ? "====" + new string('.', gap) + "===="
                                       : "#" + new string('.', w - 2) + "#";
                }
                var set = ReachabilityAnalyzer.Analyze(Grid(rows), new Vector2Int(1, 1), m,
                    new AnalyzerOptions { AllowDash = dash });
                if (set.Contains(new Vector2Int(w - 2, 1))) best = gap;
            }
            return best;
        }

        private static int MaxClimbableLedge()
        {
            var m = PlatformerMetrics.PlayerDefault;
            int best = 0;
            for (int rise = 1; rise <= 8; rise++)
            {
                int h = rise + 6;
                var rows = new string[h];
                for (int row = 0; row < h; row++)
                {
                    int y = (h - 1) - row;
                    rows[row] = y == 0 ? new string('=', 16)
                              : y <= rise ? "#......=========" : "#..............#";
                }
                var set = ReachabilityAnalyzer.Analyze(Grid(rows), new Vector2Int(2, 1), m,
                    new AnalyzerOptions { AllowDash = true });
                if (set.Contains(new Vector2Int(10, rise + 1))) best = rise;
            }
            return best;
        }

        [Test]
        public void JumpHeightShouldBeThreeAndAHalfTiles()
        {
            Assert.AreEqual(3.51f, PlatformerMetrics.PlayerDefault.MaxJumpHeight, 0.02f);
        }

        [Test]
        public void MaxClimbableLedgeShouldBeThreeTiles()
        {
            Assert.AreEqual(3, MaxClimbableLedge(),
                "단차 3타일까지 오르고 4타일부터는 벽 — 레벨 문법의 기준선");
        }

        [Test]
        public void MaxGapShouldBeFiveWithoutDashAndSevenWithDash()
        {
            Assert.AreEqual(5, MaxCrossableGap(false), "대시 없이 건널 수 있는 최대 갭");
            Assert.AreEqual(7, MaxCrossableGap(true), "대시까지 써서 건널 수 있는 최대 갭");
        }

        [Test]
        public void DesignTargetsShouldSitInsideThePhysicalLimit()
        {
            // 설계 권장치(CanClearGap)는 물리 한계보다 보수적이어야 한다 —
            // 프레임 단위로 완벽히 눌러야 겨우 닿는 지형은 만들지 않는다.
            var m = PlatformerMetrics.PlayerDefault;
            for (int gap = 1; gap <= 16; gap++)
            {
                if (m.CanClearGap(gap, withDash: false))
                    Assert.LessOrEqual(gap, MaxCrossableGap(false), $"권장치가 물리 한계를 넘었다(갭 {gap})");
                if (m.CanClearGap(gap, withDash: true))
                    Assert.LessOrEqual(gap, MaxCrossableGap(true), $"권장치가 물리 한계를 넘었다(갭 {gap}, 대시)");
            }
        }

        [Test]
        public void CorridorsNeedTwoTilesOfHeadroom()
        {
            Assert.AreEqual(2, PlatformerMetrics.PlayerDefault.RequiredHeadroomTiles);
        }
    }
}
