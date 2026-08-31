using NUnit.Framework;
using Help.Enemy;

namespace Tests.EditMode
{
    // 순수 로직 적 AI 상태머신 검증. MonoBehaviour/물리 없이 지각→결정만 테스트.
    public class EnemyAITests
    {
        private static EnemyAIConfig Cfg(float anchor = 0f, float half = 3f) => new EnemyAIConfig
        {
            AggroRange = 5f,
            DeAggroRange = 7f,
            AttackRange = 1.3f,
            PatrolAnchorX = anchor,
            PatrolHalfWidth = half,
        };

        private static EnemyPerception Target(float selfX, float targetX, float dist) => new EnemyPerception
        {
            HasTarget = true,
            SelfX = selfX,
            TargetX = targetX,
            DistanceToTarget = dist,
        };

        private static EnemyPerception NoTarget(float selfX) => new EnemyPerception
        {
            HasTarget = false,
            SelfX = selfX,
        };

        [Test]
        public void NoTarget_Patrols_OscillatesBetweenBounds()
        {
            var ai = new EnemyAI();
            var cfg = Cfg(anchor: 0f, half: 3f);

            var d = ai.Tick(NoTarget(0f), cfg, true);
            Assert.AreEqual(EnemyState.Patrol, d.State);
            Assert.AreEqual(1f, d.MoveDir, "초기 정찰 방향은 +");

            d = ai.Tick(NoTarget(3f), cfg, true); // 오른쪽 끝 → 반전
            Assert.AreEqual(-1f, d.MoveDir);

            d = ai.Tick(NoTarget(-3f), cfg, true); // 왼쪽 끝 → 반전
            Assert.AreEqual(1f, d.MoveDir);
        }

        [Test]
        public void NoTarget_ZeroPatrolWidth_Idles()
        {
            var ai = new EnemyAI();
            var d = ai.Tick(NoTarget(0f), Cfg(0f, 0f), true);
            Assert.AreEqual(EnemyState.Idle, d.State);
            Assert.AreEqual(0f, d.MoveDir);
        }

        [Test]
        public void EntersChase_WhenTargetWithinAggro()
        {
            var ai = new EnemyAI();
            var d = ai.Tick(Target(0f, 4f, 4f), Cfg(), false);
            Assert.AreEqual(EnemyState.Chase, d.State);
        }

        [Test]
        public void StaysPatrol_WhenTargetBeyondAggro()
        {
            var ai = new EnemyAI();
            var d = ai.Tick(Target(0f, 6f, 6f), Cfg(0f, 3f), false);
            Assert.AreEqual(EnemyState.Patrol, d.State);
        }

        [Test]
        public void Chase_MovesTowardTarget_OnBothSides()
        {
            var right = new EnemyAI().Tick(Target(0f, 4f, 4f), Cfg(), false);
            Assert.AreEqual(1f, right.MoveDir, "오른쪽 타깃 → +이동");

            var left = new EnemyAI().Tick(Target(0f, -4f, 4f), Cfg(), false);
            Assert.AreEqual(-1f, left.MoveDir, "왼쪽 타깃 → -이동");
        }

        [Test]
        public void EntersAttack_WithinAttackRange_StopsAndAttacks()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // 먼저 Chase 진입
            var d = ai.Tick(Target(0f, 1f, 1f), Cfg(), true);
            Assert.AreEqual(EnemyState.Attack, d.State);
            Assert.AreEqual(0f, d.MoveDir, "공격 중엔 정지");
            Assert.IsTrue(d.Attack);
        }

        [Test]
        public void Attack_OnlyTriggersWhenReady()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase

            var notReady = ai.Tick(Target(0f, 1f, 1f), Cfg(), false);
            Assert.AreEqual(EnemyState.Attack, notReady.State);
            Assert.IsFalse(notReady.Attack, "쿨다운 중엔 공격 안 함");

            var ready = ai.Tick(Target(0f, 1f, 1f), Cfg(), true);
            Assert.IsTrue(ready.Attack);
        }

        [Test]
        public void Hysteresis_KeepsChasing_BetweenAggroAndDeAggro()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase (4 <= aggro 5)
            var d = ai.Tick(Target(0f, 6f, 6f), Cfg(), false); // 5 < 6 <= deaggro 7
            Assert.AreEqual(EnemyState.Chase, d.State, "이력현상: 경계 사이에선 추격 유지");
        }

        [Test]
        public void LosesTarget_BeyondDeAggro_ReturnsToPatrol()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase
            var d = ai.Tick(Target(0f, 8f, 8f), Cfg(0f, 3f), false); // 8 > deaggro 7
            Assert.AreEqual(EnemyState.Patrol, d.State);
        }

        [Test]
        public void Attack_StaysUntilBeyondAttackRange()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase
            ai.Tick(Target(0f, 1f, 1f), Cfg(), true);  // Attack

            var still = ai.Tick(Target(0f, 1.2f, 1.2f), Cfg(), false); // 1.2 <= 1.3
            Assert.AreEqual(EnemyState.Attack, still.State);

            var left = ai.Tick(Target(0f, 2f, 2f), Cfg(), false); // 2 > 1.3
            Assert.AreEqual(EnemyState.Chase, left.State);
        }

        // --- 경계값 고정(<= vs < 회귀 방지) ---

        [Test]
        public void EntersChase_AtExactAggroBoundary()
        {
            var ai = new EnemyAI(); // dist == AggroRange(5) → 진입(<=)
            Assert.AreEqual(EnemyState.Chase, ai.Tick(Target(0f, 5f, 5f), Cfg(), false).State);
        }

        [Test]
        public void KeepsChasing_AtExactDeAggroBoundary()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase
            // dist == DeAggroRange(7) → 이탈 조건은 >7 이므로 유지
            Assert.AreEqual(EnemyState.Chase, ai.Tick(Target(0f, 7f, 7f), Cfg(), false).State);
        }

        [Test]
        public void EntersAttack_AtExactAttackRangeBoundary()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase
            // dist == AttackRange(1.3) → 진입(<=)
            Assert.AreEqual(EnemyState.Attack, ai.Tick(Target(0f, 1.3f, 1.3f), Cfg(), true).State);
        }

        [Test]
        public void Chase_HoldsStill_WhenTargetNearlyVerticallyAligned()
        {
            // 같은 x·위쪽(거리 3): aggro 안이라 Chase지만 x 정렬 → 진동 방지로 정지
            var aligned = new EnemyAI().Tick(Target(0f, 0.1f, 3f), Cfg(), false);
            Assert.AreEqual(EnemyState.Chase, aligned.State);
            Assert.AreEqual(0f, aligned.MoveDir, "데드존 안 → 정지");

            var offset = new EnemyAI().Tick(Target(0f, 0.2f, 3f), Cfg(), false);
            Assert.AreEqual(1f, offset.MoveDir, "데드존 밖 → 이동");
        }

        [Test]
        public void Reset_ReturnsToPatrol_FromAttack()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase
            ai.Tick(Target(0f, 1f, 1f), Cfg(), true);  // Attack
            Assert.AreEqual(EnemyState.Attack, ai.State);

            ai.Reset();

            Assert.AreEqual(EnemyState.Patrol, ai.State);
            // 정찰 방향도 초기(+)로 복귀
            Assert.AreEqual(1f, ai.Tick(NoTarget(0f), Cfg(0f, 3f), false).MoveDir);
        }

        // --- Ranged 아키타입(카이팅) ---

        private static EnemyAIConfig RangedCfg() => new EnemyAIConfig
        {
            AggroRange = 8f,
            DeAggroRange = 10f,
            AttackRange = 6f,
            PatrolAnchorX = 0f,
            PatrolHalfWidth = 3f,
            Archetype = EnemyArchetype.Ranged,
            StandoffRange = 3f,
        };

        [Test]
        public void Ranged_Approaches_WhenTargetBeyondAttackRange()
        {
            // 어그로 안(dist 7 <= 8)이지만 사거리 밖(7 > 6) → 접근
            var d = new EnemyAI().Tick(Target(0f, 7f, 7f), RangedCfg(), false);
            Assert.AreEqual(EnemyState.Chase, d.State);
            Assert.AreEqual(1f, d.MoveDir, "사거리 밖 → 타깃 쪽으로 접근");
        }

        [Test]
        public void Ranged_HoldsAndFires_InStandoffBand()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 7f, 7f), RangedCfg(), false); // Chase
            // standoff(3) <= dist(4) <= attackRange(6) → 편안한 밴드: 정지 후 발사
            var d = ai.Tick(Target(0f, 4f, 4f), RangedCfg(), true);
            Assert.AreEqual(EnemyState.Attack, d.State);
            Assert.AreEqual(0f, d.MoveDir, "밴드 안에선 정지");
            Assert.IsTrue(d.Attack);
        }

        [Test]
        public void Ranged_Retreats_WhenTargetTooClose()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 7f, 7f), RangedCfg(), false); // Chase
            ai.Tick(Target(0f, 4f, 4f), RangedCfg(), true);  // Attack (밴드)
            // dist 2 < standoff 3 → 후퇴하며 계속 공격. 타깃이 오른쪽 → 왼쪽으로 후퇴
            var d = ai.Tick(Target(0f, 2f, 2f), RangedCfg(), true);
            Assert.AreEqual(EnemyState.Attack, d.State);
            Assert.AreEqual(-1f, d.MoveDir, "너무 가까우면 타깃 반대로 후퇴");
            Assert.IsTrue(d.Attack, "후퇴 중에도 발사");
        }

        [Test]
        public void Ranged_RetreatDirection_BothSides()
        {
            var r = new EnemyAI();
            r.Tick(Target(0f, 7f, 7f), RangedCfg(), false);
            r.Tick(Target(0f, 4f, 4f), RangedCfg(), true);
            Assert.AreEqual(-1f, r.Tick(Target(0f, 2f, 2f), RangedCfg(), false).MoveDir, "오른쪽 타깃 → 왼쪽 후퇴");

            var l = new EnemyAI();
            l.Tick(Target(0f, -7f, 7f), RangedCfg(), false);
            l.Tick(Target(0f, -4f, 4f), RangedCfg(), true);
            Assert.AreEqual(1f, l.Tick(Target(0f, -2f, 2f), RangedCfg(), false).MoveDir, "왼쪽 타깃 → 오른쪽 후퇴");
        }

        [Test]
        public void Melee_DoesNotRetreat_WhenTargetClose()
        {
            // 대조군: Melee는 아무리 가까워도 멈춰서 공격(후퇴 없음)
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase
            var d = ai.Tick(Target(0f, 0.5f, 0.5f), Cfg(), true);
            Assert.AreEqual(EnemyState.Attack, d.State);
            Assert.AreEqual(0f, d.MoveDir, "Melee는 후퇴하지 않음");
        }

        // --- 속박(Bind) — 상태이상이 AI 결정을 덮어쓴다 ---

        private static EnemyPerception Restrained(EnemyPerception p)
        {
            p.IsRestrained = true;
            return p;
        }

        [Test]
        public void Restrained_WithTargetInAttackRange_DoesNotAttack()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false);  // Chase 진입
            ai.Tick(Target(0f, 1f, 1f), Cfg(), true);   // Attack 진입

            var d = ai.Tick(Restrained(Target(0f, 1f, 1f)), Cfg(), true);
            Assert.IsFalse(d.Attack, "속박 중엔 공격 불가");
            Assert.AreEqual(0f, d.MoveDir, "속박 중엔 이동 불가");
        }

        [Test]
        public void Restrained_WithTargetInChaseRange_DoesNotMove()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false);

            var d = ai.Tick(Restrained(Target(0f, 4f, 4f)), Cfg(), false);
            Assert.AreEqual(0f, d.MoveDir, "추격 중 속박되면 멈춤");
        }

        [Test]
        public void Restrained_WithNoTarget_DoesNotPatrol()
        {
            var ai = new EnemyAI();
            var d = ai.Tick(Restrained(NoTarget(0f)), Cfg(), true);
            Assert.AreEqual(0f, d.MoveDir, "속박은 타깃 유무와 무관 — 정찰도 막는다");
        }

        [Test]
        public void Restrained_PreservesPreviousState_ForResumeAfterRelease()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false); // Chase

            var d = ai.Tick(Restrained(Target(0f, 4f, 4f)), Cfg(), false);
            Assert.AreEqual(EnemyState.Chase, d.State, "속박은 상태를 갈아엎지 않음(해제 후 즉시 복귀)");
        }

        [Test]
        public void ReleasedFromRestraint_ResumesChase_WithoutReAggro()
        {
            var ai = new EnemyAI();
            ai.Tick(Target(0f, 4f, 4f), Cfg(), false);
            ai.Tick(Restrained(Target(0f, 4f, 4f)), Cfg(), false);

            var d = ai.Tick(Target(0f, 4f, 4f), Cfg(), false);
            Assert.AreEqual(EnemyState.Chase, d.State, "해제되면 재감지 없이 추격 재개");
            Assert.AreEqual(1f, d.MoveDir, "이동도 즉시 재개");
        }
    }
}
