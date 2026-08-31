using NUnit.Framework;
using Help.Enemy;

namespace Tests.EditMode
{
    // 적 상태이상(속박 등)의 지속시간 관리 — 순수 로직이라 시간을 Tick(dt)로 주입해 검증한다.
    public class EnemyStatusTests
    {
        [Test]
        public void FreshStatus_HasNoEffects_IsNotRestrained()
        {
            var status = new EnemyStatus();
            Assert.IsFalse(status.IsRestrained, "갓 만든 상태는 어떤 상태이상도 없음");
        }

        [Test]
        public void ApplyBind_MakesRestrained_ImmediatelyAfterApply()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 1.5f);
            Assert.IsTrue(status.IsRestrained, "속박 적용 즉시 구속 상태");
        }

        [Test]
        public void BindExpires_AfterDurationTicked_ReleasesRestraint()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 1f);

            status.Tick(0.6f);
            Assert.IsTrue(status.IsRestrained, "지속시간이 남았으면 유지");

            status.Tick(0.6f);
            Assert.IsFalse(status.IsRestrained, "지속시간을 넘겨 틱하면 해제");
        }

        [Test]
        public void ReapplyBind_WithLongerDuration_ExtendsRemaining()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 1f);
            status.Tick(0.9f);
            status.Apply(StatusEffectType.Bind, 2f);

            status.Tick(1f);
            Assert.IsTrue(status.IsRestrained, "더 긴 속박을 덮어쓰면 남은 시간이 늘어남");
        }

        [Test]
        public void ReapplyBind_WithShorterDuration_DoesNotShortenRemaining()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 3f);
            status.Apply(StatusEffectType.Bind, 0.1f);

            status.Tick(0.5f);
            Assert.IsTrue(status.IsRestrained, "짧은 속박이 긴 속박을 깎아내면 안 됨");
        }

        [Test]
        public void TickWithZeroDelta_DoesNotExpireEffect()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 0.5f);

            status.Tick(0f);
            Assert.IsTrue(status.IsRestrained, "dt=0은 시간을 소모하지 않음(일시정지 안전)");
        }

        [Test]
        public void Reset_ClearsActiveEffects_ForRunReset()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 5f);

            status.Reset();
            Assert.IsFalse(status.IsRestrained, "런 리셋 시 상태이상 전부 해제");
        }

        [Test]
        public void ApplyNone_DoesNothing()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.None, 5f);
            Assert.IsFalse(status.IsRestrained, "None은 아무 효과도 주지 않음");
        }

        // --- 끌어당김 — 속박과 별개 축(속박은 결정 봉인, 끌기는 운동학) ---

        [Test]
        public void ApplyPull_MakesPulled_AndExpiresIndependently()
        {
            var status = new EnemyStatus();
            status.Apply(StatusEffectType.Bind, 3f);
            status.ApplyPull(0.5f);
            Assert.IsTrue(status.IsPulled, "끌기 적용 즉시 끌림 상태");

            status.Tick(0.6f);
            Assert.IsFalse(status.IsPulled, "끌기는 먼저 끝나고");
            Assert.IsTrue(status.IsRestrained, "속박은 남아 있다");
        }

        [Test]
        public void Reset_ClearsPull_ForRunReset()
        {
            var status = new EnemyStatus();
            status.ApplyPull(5f);
            status.Reset();
            Assert.IsFalse(status.IsPulled, "런 리셋 시 끌기도 해제");
        }
    }
}
