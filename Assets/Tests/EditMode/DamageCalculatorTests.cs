using NUnit.Framework;
using Help.Combat;
using Help.Enemy;

namespace Tests.EditMode
{
    public class DamageCalculatorTests
    {
        [Test]
        public void ShouldDealFullDamageWhenEnemyHasNoElementLock()
        {
            var enemy = new EnemyStats(100, 10, ElementType.None);
            int result = DamageCalculator.Calculate(20, ElementType.Fire, enemy);
            Assert.AreEqual(20, result);
        }

        [Test]
        public void ShouldDealFullDamageWhenElementMatches()
        {
            var enemy = new EnemyStats(100, 10, ElementType.Fire);
            int result = DamageCalculator.Calculate(20, ElementType.Fire, enemy);
            Assert.AreEqual(20, result);
        }

        [Test]
        public void ShouldDealReducedDamageWhenElementMismatches()
        {
            var enemy = new EnemyStats(100, 10, ElementType.Fire);
            int result = DamageCalculator.Calculate(20, ElementType.Ice, enemy);
            Assert.Less(result, 20);
            Assert.Greater(result, 0);  // 완전 면역은 아님
        }

        [Test]
        public void ShouldGuaranteeAtLeastOneDamageWhenMismatchWithLowBaseDamage()
        {
            // base * 0.1 이 0으로 절삭돼 완전 면역이 되면 안 된다 (DESIGN: 면역 아님)
            var enemy = new EnemyStats(100, 10, ElementType.Fire);
            int result = DamageCalculator.Calculate(5, ElementType.Ice, enemy);
            Assert.AreEqual(1, result);
        }

        [Test]
        public void ShouldDealFullDamageWithNoElementVsNoLock()
        {
            var enemy = new EnemyStats(100, 10, ElementType.None);
            int result = DamageCalculator.Calculate(15, ElementType.None, enemy);
            Assert.AreEqual(15, result);
        }
    }
}
