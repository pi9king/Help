using NUnit.Framework;
using Help.Enemy;

namespace Tests.EditMode
{
    public class EnemyCounterTests
    {
        [Test]
        public void EmptyCounterIsNotAllDead()
        {
            var c = new EnemyCounter();
            Assert.IsFalse(c.AllDead, "등록된 적이 없으면 AllDead가 아니어야 함(빈 방 오클리어 방지)");
        }

        [Test]
        public void NotAllDeadUntilEveryEnemyDies()
        {
            var c = new EnemyCounter();
            c.Register(); c.Register(); c.Register();
            c.MarkDead(); c.MarkDead();
            Assert.IsFalse(c.AllDead, "2/3 처치는 아직 전멸 아님");
            c.MarkDead();
            Assert.IsTrue(c.AllDead, "3/3 처치 시 전멸");
        }

        [Test]
        public void MarkDeadDoesNotExceedTotal()
        {
            var c = new EnemyCounter();
            c.Register();
            c.MarkDead();
            c.MarkDead(); // 중복 방어
            Assert.AreEqual(1, c.Dead);
            Assert.IsTrue(c.AllDead);
        }

        [Test]
        public void ResetClearsCounts()
        {
            var c = new EnemyCounter();
            c.Register(); c.MarkDead();
            c.Reset();
            Assert.AreEqual(0, c.Total);
            Assert.AreEqual(0, c.Dead);
            Assert.IsFalse(c.AllDead);
        }

        [Test]
        public void SingleEnemyDeathIsAllDead()
        {
            var c = new EnemyCounter();
            c.Register();
            Assert.IsFalse(c.AllDead);
            c.MarkDead();
            Assert.IsTrue(c.AllDead);
        }
    }
}
