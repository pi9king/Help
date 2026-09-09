using NUnit.Framework;
using Help.Enemy;

namespace Tests.EditMode
{
    // 애니메이션 선택은 순수 로직으로 분리한다.
    // 이유: Animator는 "표시"일 뿐이고 타이밍의 진실은 EnemyAI/EnemyMeleeAttack이 쥔다.
    // 여기서 클립을 고르는 규칙만 못 박아두면, 애셋(LayerLab)을 갈아끼워도 규칙은 안 흔들린다.
    public class EnemyAnimationTests
    {
        [Test]
        public void ShouldPlayIdleWhenStandingStill()
        {
            Assert.AreEqual(EnemyAnimClip.Idle,
                EnemyAnimation.Select(isAlive: true, inHitstun: false, phase: MeleePhase.Ready, moveDir: 0f));
        }

        [Test]
        public void ShouldPlayWalkWhenMoving()
        {
            Assert.AreEqual(EnemyAnimClip.Walk,
                EnemyAnimation.Select(true, false, MeleePhase.Ready, 1f));
            Assert.AreEqual(EnemyAnimClip.Walk,
                EnemyAnimation.Select(true, false, MeleePhase.Ready, -1f));
        }

        // 예비동작(회피 가능 창)이 곧 공격 모션이다 — 플레이어가 읽고 피할 수 있어야 한다.
        [Test]
        public void ShouldPlayAttackDuringWindup()
        {
            Assert.AreEqual(EnemyAnimClip.Attack,
                EnemyAnimation.Select(true, false, MeleePhase.Windup, 0f));
        }

        // 타격 후 회복 중엔 공격 모션을 붙들지 않는다(다음 행동이 읽혀야 하므로).
        [Test]
        public void ShouldNotPlayAttackDuringRecover()
        {
            Assert.AreEqual(EnemyAnimClip.Idle,
                EnemyAnimation.Select(true, false, MeleePhase.Recover, 0f));
        }

        [Test]
        public void ShouldPlayHitWhileInHitstun()
        {
            Assert.AreEqual(EnemyAnimClip.Hit,
                EnemyAnimation.Select(true, true, MeleePhase.Ready, 0f));
        }

        [Test]
        public void ShouldPlayDeadWhenNotAlive()
        {
            Assert.AreEqual(EnemyAnimClip.Dead,
                EnemyAnimation.Select(false, false, MeleePhase.Ready, 0f));
        }

        // 우선순위: 죽음 > 피격 > 공격 > 이동 > 정지.
        // 넉백으로 밀리는 중에 Walk가 나오거나, 죽는 순간 Hit가 나오면 연출이 깨진다.
        [Test]
        public void ShouldPreferDeadOverEverythingElse()
        {
            Assert.AreEqual(EnemyAnimClip.Dead,
                EnemyAnimation.Select(false, true, MeleePhase.Windup, 1f));
        }

        [Test]
        public void ShouldPreferHitOverAttackAndWalk()
        {
            Assert.AreEqual(EnemyAnimClip.Hit,
                EnemyAnimation.Select(true, true, MeleePhase.Windup, 1f));
        }

        // 클립 이름은 LayerLab 컨트롤러의 상태명과 정확히 일치해야 Animator.Play가 먹는다.
        [Test]
        public void ClipNamesShouldMatchControllerStateNames()
        {
            Assert.AreEqual("Idle", EnemyAnimation.NameOf(EnemyAnimClip.Idle));
            Assert.AreEqual("Walk", EnemyAnimation.NameOf(EnemyAnimClip.Walk));
            Assert.AreEqual("Attack", EnemyAnimation.NameOf(EnemyAnimClip.Attack));
            Assert.AreEqual("Hit", EnemyAnimation.NameOf(EnemyAnimClip.Hit));
            Assert.AreEqual("Dead", EnemyAnimation.NameOf(EnemyAnimClip.Dead));
        }
    }
}
