using NUnit.Framework;
using Help.Player;

namespace Tests.EditMode
{
    // D-3: 공중 대시는 착지할 때까지 1회.
    //
    // 예전엔 쿨다운(0.8초)만 조건이라, 체공이 길어지는 상황 — 높은 곳에서 낙하,
    // Tall 31칸 방 — 에서는 한 번 뜬 채로 대시를 연속으로 쓸 수 있었다.
    // 착지 기준으로 바꾸면 "갭 하나당 대시 하나"가 보장돼 지형 설계가 예측 가능해진다.
    public class DashRuleTests
    {
        [Test]
        public void ShouldDashOnGroundWhenCooldownReady()
        {
            Assert.IsTrue(DashRule.CanDash(grounded: true, airDashUsed: false, cooldownTimer: 0f));
        }

        [Test]
        public void ShouldNotDashWhileCooldownRunning()
        {
            Assert.IsFalse(DashRule.CanDash(true, false, 0.3f));
        }

        [Test]
        public void ShouldDashOnceInAir()
        {
            Assert.IsTrue(DashRule.CanDash(grounded: false, airDashUsed: false, cooldownTimer: 0f));
        }

        // 핵심 회귀: 공중에서 두 번째 대시는 쿨다운이 다 돌았어도 안 된다.
        [Test]
        public void ShouldNotDashTwiceInAirEvenAfterCooldown()
        {
            Assert.IsFalse(DashRule.CanDash(grounded: false, airDashUsed: true, cooldownTimer: 0f));
        }

        // 착지하면 다시 쓸 수 있어야 한다(플래그가 남아 지상 대시까지 막으면 안 된다).
        [Test]
        public void LandingShouldRestoreDash()
        {
            Assert.IsTrue(DashRule.CanDash(grounded: true, airDashUsed: false, cooldownTimer: 0f));
        }

        // 쿨다운은 공중/지상 공통으로 우선한다 — 착지 직후 연타를 막는 건 여전히 쿨다운의 몫.
        [Test]
        public void CooldownShouldOverrideGroundedState()
        {
            Assert.IsFalse(DashRule.CanDash(true, false, 0.01f));
            Assert.IsFalse(DashRule.CanDash(false, false, 0.01f));
        }
    }
}
