using NUnit.Framework;
using Help.Enemy;

namespace Tests.EditMode
{
    public class EnemyMeleeAttackTests
    {
        [Test]
        public void ShouldStartInReadyPhase()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            Assert.AreEqual(MeleePhase.Ready, atk.Phase);
        }

        [Test]
        public void ShouldEnterWindupOnAttackIntent_WithoutStrikingImmediately()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            var r = atk.Advance(0.1f, true);
            Assert.IsTrue(r.StartedWindup, "예비동작이 시작되어야 함");
            Assert.IsFalse(r.Strike, "예비동작 시작 틱에 즉발 타격이 있으면 안 됨");
            Assert.AreEqual(MeleePhase.Windup, atk.Phase);
        }

        [Test]
        public void ShouldStrikeOnceAfterWindupElapses()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            atk.Advance(0.1f, true);          // Ready → Windup
            var mid = atk.Advance(0.2f, true); // 아직 예비동작 중
            Assert.IsFalse(mid.Strike);
            var hit = atk.Advance(0.3f, true); // 누적 0.5 > 0.4 → 타격
            Assert.IsTrue(hit.Strike, "예비동작 종료 시 타격이 발생해야 함");
            Assert.AreEqual(MeleePhase.Recover, atk.Phase);
        }

        [Test]
        public void ShouldNotStrikeAgainDuringRecover()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            atk.Advance(0.1f, true); // Ready → Windup(0.4)
            atk.Advance(0.4f, true); // 예비동작 소진 → Strike, Recover(1s)
            var again = atk.Advance(0.5f, true); // 회복 중 재공격 시도
            Assert.IsFalse(again.Strike, "회복 중에는 다시 타격하면 안 됨");
            Assert.IsFalse(again.StartedWindup);
            Assert.AreEqual(MeleePhase.Recover, atk.Phase);
        }

        [Test]
        public void ShouldReturnToReadyAfterRecover()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            atk.Advance(0.1f, true); // Ready → Windup(0.4)
            atk.Advance(0.4f, true); // 예비동작 소진 → Strike, Recover(1s)
            atk.Advance(1.0f, false); // 회복 종료
            Assert.AreEqual(MeleePhase.Ready, atk.Phase);
            var r = atk.Advance(0.1f, true); // 다시 공격 가능
            Assert.IsTrue(r.StartedWindup);
        }

        [Test]
        public void ShouldStayReadyWhenNoAttackIntent()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            var r = atk.Advance(0.5f, false);
            Assert.AreEqual(MeleePhase.Ready, atk.Phase);
            Assert.IsFalse(r.StartedWindup);
            Assert.IsFalse(r.Strike);
        }

        [Test]
        public void ResetShouldReturnToReadyFromAnyPhase()
        {
            var atk = new EnemyMeleeAttack(0.4f, 1f);
            atk.Advance(0.4f, true); // Windup
            atk.Reset();
            Assert.AreEqual(MeleePhase.Ready, atk.Phase);
        }
    }
}
