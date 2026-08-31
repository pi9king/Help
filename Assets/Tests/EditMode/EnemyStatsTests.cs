using NUnit.Framework;
using Help.Enemy;
using Help.Combat;

namespace Tests.EditMode
{
    public class EnemyStatsTests
    {
        [Test]
        public void TakeDamage_ReducesCurrentHp()
        {
            var s = new EnemyStats(30, 5);
            s.TakeDamage(10);
            Assert.AreEqual(20, s.CurrentHp);
            Assert.IsTrue(s.IsAlive);
        }

        [Test]
        public void TakeDamage_ClampsAtZero_AndIsNotAlive()
        {
            var s = new EnemyStats(30, 5);
            s.TakeDamage(100);
            Assert.AreEqual(0, s.CurrentHp, "HP는 0 미만으로 내려가지 않음");
            Assert.IsFalse(s.IsAlive);
        }

        [Test]
        public void OnDied_FiresOnce_WhenHpReachesZero()
        {
            var s = new EnemyStats(20, 5);
            int died = 0;
            s.OnDied += () => died++;

            s.TakeDamage(10); // 아직 생존
            Assert.AreEqual(0, died);

            s.TakeDamage(10); // 0 도달 → 사망 1회
            Assert.AreEqual(1, died);
        }

        [Test]
        public void Ctor_StoresLockedElement()
        {
            var s = new EnemyStats(30, 5, ElementType.Fire);
            Assert.AreEqual(ElementType.Fire, s.LockedElement);
        }

        [Test]
        public void TakeDamage_AfterDeath_DoesNotRefireOnDied()
        {
            var s = new EnemyStats(20, 5);
            int died = 0;
            s.OnDied += () => died++;

            s.TakeDamage(20); // 사망(0)
            s.TakeDamage(20); // 시체 재타격
            s.TakeDamage(20);

            Assert.AreEqual(1, died, "사망 후 재타격에도 OnDied는 1회만");
            Assert.AreEqual(0, s.CurrentHp);
        }

        [Test]
        public void Reset_RestoresFullHp()
        {
            var s = new EnemyStats(30, 5);
            s.TakeDamage(20);
            s.Reset();
            Assert.AreEqual(30, s.CurrentHp);
            Assert.IsTrue(s.IsAlive);
        }
    }
}
