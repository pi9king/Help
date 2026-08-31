using NUnit.Framework;
using Help.Combat;

namespace Tests.EditMode
{
    public class AttackMotionClockTests
    {
        [Test]
        public void ShouldBeInWindupBeforeActive()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            var r = c.Tick(0.05f);
            Assert.AreEqual(AttackPhase.Windup, r.Phase);
            Assert.IsFalse(r.IsActive, "예비동작 중엔 타격 판정이 열리면 안 됨");
        }

        [Test]
        public void ShouldOpenHitWindowDuringActive()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            c.Tick(0.1f);            // 예비 종료 직후
            var r = c.Tick(0.05f);   // t=0.15 → Active
            Assert.AreEqual(AttackPhase.Active, r.Phase);
            Assert.IsTrue(r.IsActive);
            Assert.That(r.ActiveProgress, Is.GreaterThan(0f).And.LessThan(1f));
        }

        [Test]
        public void ShouldEnterRecoveryAfterActive()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            c.Tick(0.31f); // t=0.31 → windup+active(0.3) 초과, total(0.4) 미만
            var r = c.Tick(0f);
            Assert.AreEqual(AttackPhase.Recovery, r.Phase);
            Assert.IsFalse(r.IsActive);
        }

        [Test]
        public void ShouldFinishAfterTotalDuration()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            c.Start();
            var r = c.Tick(0.5f); // total(0.4) 초과
            Assert.AreEqual(AttackPhase.Done, r.Phase);
            Assert.IsFalse(c.Running);
        }

        [Test]
        public void ShouldReportDoneWhenNotStarted()
        {
            var c = new AttackMotionClock(0.1f, 0.2f, 0.1f);
            var r = c.Tick(0.1f);
            Assert.AreEqual(AttackPhase.Done, r.Phase);
        }

        [Test]
        public void TotalShouldSumPhases()
        {
            var c = new AttackMotionClock(0.03f, 0.12f, 0.10f);
            Assert.AreEqual(0.25f, c.Total, 1e-5f);
        }
    }
}
