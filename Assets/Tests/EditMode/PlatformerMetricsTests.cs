using NUnit.Framework;
using Help.Player;

namespace Tests.EditMode
{
    // 레벨 디자인 "문법"의 근거가 되는 수치들.
    // 이 값이 바뀌면 방 템플릿의 도달성 검증 결과도 함께 바뀐다 — 물리 튜닝의 단일 진실.
    public class PlatformerMetricsTests
    {
        private static PlatformerMetrics Tuned() => PlatformerMetrics.PlayerDefault;

        [Test]
        public void ShouldComputeJumpHeightFromPhysics()
        {
            // h = v0^2 / (2 * g * gravityScale) = 14^2 / (2 * 9.81 * 2.85)
            var m = Tuned();
            Assert.AreEqual(3.5f, m.MaxJumpHeight, 0.05f);
        }

        [Test]
        public void ShouldComputeRiseTime()
        {
            // t = v0 / (g * gravityScale) = 14 / 27.96
            var m = Tuned();
            Assert.AreEqual(0.50f, m.RiseTime, 0.02f);
        }

        [Test]
        public void FallShouldBeFasterThanRiseWhenMultiplierAboveOne()
        {
            var m = Tuned();
            Assert.Less(m.FallTime, m.RiseTime, "낙하 중력 배수가 1보다 크면 내려오는 게 더 빨라야 한다");
            Assert.AreEqual(m.RiseTime + m.FallTime, m.AirTime, 1e-4f);
        }

        [Test]
        public void ShouldClearThreeTileLedgeButNotFour()
        {
            var m = Tuned();
            Assert.IsTrue(m.CanClearLedge(3), "3타일 단차는 점프로 올라갈 수 있어야 한다");
            Assert.IsFalse(m.CanClearLedge(4), "4타일 단차는 벽으로 취급 — 레벨 디자인의 경계");
        }

        [Test]
        public void ShouldClearShortGapWithoutDash()
        {
            var m = Tuned();
            Assert.IsTrue(m.CanClearGap(3, withDash: false));
        }

        [Test]
        public void ShouldNotClearWideGapWithoutDash()
        {
            var m = Tuned();
            Assert.IsFalse(m.CanClearGap(4, withDash: false), "4타일 갭은 대시가 있어야 건넌다");
        }

        [Test]
        public void DashShouldExtendGapReach()
        {
            var m = Tuned();
            Assert.IsTrue(m.CanClearGap(5, withDash: true));
            Assert.IsFalse(m.CanClearGap(6, withDash: true), "6타일 이상은 CrossGap 능력(ROPE) 전용 게이팅");
        }

        [Test]
        public void ShouldComputeDashDistance()
        {
            // DashForce 20 * DashDuration 0.15
            var m = Tuned();
            Assert.AreEqual(3.0f, m.DashDistance, 1e-4f);
        }

        [Test]
        public void SafeRunShouldBeShorterThanTheoreticalRun()
        {
            var m = Tuned();
            Assert.Less(m.SafeJumpRun, m.MaxJumpRun, "설계 권장치는 이론 최대보다 보수적이어야 한다");
        }

        [Test]
        public void HeadroomShouldCoverPlayerHeight()
        {
            var m = Tuned();
            Assert.GreaterOrEqual(m.RequiredHeadroomTiles, 1);
            Assert.GreaterOrEqual((float)m.RequiredHeadroomTiles, m.PlayerHeightTiles);
        }

        [Test]
        public void HeavierGravityShouldLowerJump()
        {
            var light = PlatformerMetrics.FromPhysics(jumpForce: 14f, moveSpeed: 7f, dashForce: 20f,
                dashDuration: 0.15f, gravityScale: 1f, fallMultiplier: 1f);
            var heavy = PlatformerMetrics.FromPhysics(jumpForce: 14f, moveSpeed: 7f, dashForce: 20f,
                dashDuration: 0.15f, gravityScale: 4f, fallMultiplier: 1f);
            Assert.Less(heavy.MaxJumpHeight, light.MaxJumpHeight);
        }

        [Test]
        public void CurrentSceneGravityWouldBeUnusable()
        {
            // 회귀 방어: gravityScale 1.0(수정 전 값)이면 점프가 방 높이(15타일)에 육박해 지형이 무의미해진다.
            var before = PlatformerMetrics.FromPhysics(jumpForce: 14f, moveSpeed: 7f, dashForce: 20f,
                dashDuration: 0.15f, gravityScale: 1f, fallMultiplier: 1f);
            Assert.Greater(before.MaxJumpHeight, 9f);
            Assert.Less(Tuned().MaxJumpHeight, 4f, "튜닝 후에는 방 안에 단차를 설계할 수 있어야 한다");
        }
    }
}
