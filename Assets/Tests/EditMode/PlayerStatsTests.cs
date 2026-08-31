using NUnit.Framework;
using Help.Player;

namespace Tests.EditMode
{
    public class PlayerStatsTests
    {
        // 첫 방에서 무기를 만들 수 있다는 보장이 없으므로(재료 배치는 절차 생성에 달림),
        // 맨손 기본 공격력만으로도 기본 적(HP 30)을 감당할 수 있어야 한다 — 2타 이내.
        [Test]
        public void DefaultAttack_ClearsBasicEnemyWithinTwoHits()
        {
            const int basicEnemyHp = 30;
            var stats = new PlayerStats();
            Assert.LessOrEqual(basicEnemyHp, stats.AttackPower * 2,
                $"맨손 공격력 {stats.AttackPower}로는 기본 적(HP {basicEnemyHp})에 3타 이상 필요");
        }

        [Test]
        public void ShouldReduceHpWhenTakingDamage()
        {
            var stats = new PlayerStats(maxHp: 100);
            stats.TakeDamage(30);
            Assert.AreEqual(70, stats.CurrentHp);
        }

        [Test]
        public void ShouldNotGoBelowZeroHp()
        {
            var stats = new PlayerStats(maxHp: 100);
            stats.TakeDamage(999);
            Assert.AreEqual(0, stats.CurrentHp);
        }

        [Test]
        public void ShouldFireOnDiedWhenHpReachesZero()
        {
            var stats = new PlayerStats(maxHp: 50);
            bool died = false;
            stats.OnDied += () => died = true;
            stats.TakeDamage(50);
            Assert.IsTrue(died);
        }

        [Test]
        public void ShouldHealWithinMaxHp()
        {
            var stats = new PlayerStats(maxHp: 100);
            stats.TakeDamage(40);
            stats.Heal(20);
            Assert.AreEqual(80, stats.CurrentHp);
        }

        [Test]
        public void ShouldNotExceedMaxHpOnHeal()
        {
            var stats = new PlayerStats(maxHp: 100);
            stats.Heal(50);
            Assert.AreEqual(100, stats.CurrentHp);
        }

        [Test]
        public void ShouldApplyDefenseToReduceDamage()
        {
            var stats = new PlayerStats(maxHp: 100, defense: 10);
            stats.TakeDamage(30);
            Assert.AreEqual(80, stats.CurrentHp);
        }

        [Test]
        public void ShouldNotRefireOnDied_WhenDamagedAfterDeath()
        {
            var stats = new PlayerStats(maxHp: 50);
            int died = 0;
            stats.OnDied += () => died++;
            stats.TakeDamage(50); // 사망
            stats.TakeDamage(50); // 사후 재타격
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Heal_DoesNotRevive_WhenDead()
        {
            var stats = new PlayerStats(maxHp: 50);
            stats.TakeDamage(50); // 사망(0)
            stats.Heal(20);
            Assert.AreEqual(0, stats.CurrentHp, "사망 상태는 Heal로 부활하지 않음");
            Assert.IsFalse(stats.IsAlive);
        }

        [Test]
        public void Reset_RevivesToFullHp_AfterDeath()
        {
            var stats = new PlayerStats(maxHp: 50);
            stats.TakeDamage(50); // 사망
            Assert.IsFalse(stats.IsAlive);

            stats.Reset();

            Assert.AreEqual(50, stats.CurrentHp);
            Assert.IsTrue(stats.IsAlive);
        }

        [Test]
        public void EquipmentBonus_ApplyThenRemove_ReturnsToBase()
        {
            var stats = new PlayerStats(attack: 10, defense: 0);
            stats.ApplyEquipmentBonus(5, 3);
            Assert.AreEqual(15, stats.AttackPower);
            Assert.AreEqual(3, stats.Defense);

            stats.RemoveEquipmentBonus(5, 3);
            Assert.AreEqual(10, stats.AttackPower, "장비 해제 후 공격력이 base로 복귀해야 함");
            Assert.AreEqual(0, stats.Defense);
        }

        [Test]
        public void Reset_RestoresAttackAndDefenseToBase_EvenIfBonusStillApplied()
        {
            // 런 리셋의 "완전 초기화" 불변식: 장비 해제 이벤트가 누락돼도 Reset이 능력치를 base로 되돌린다
            var stats = new PlayerStats(attack: 10, defense: 0);
            stats.ApplyEquipmentBonus(5, 3); // 보너스만 적용(해제 없음)
            stats.TakeDamage(999);           // 사망

            stats.Reset();

            Assert.AreEqual(10, stats.AttackPower, "Reset이 공격력을 base로 복원해야 함");
            Assert.AreEqual(0, stats.Defense, "Reset이 방어력을 base로 복원해야 함");
        }
    }
}
