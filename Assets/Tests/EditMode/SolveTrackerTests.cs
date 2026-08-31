using NUnit.Framework;
using Help.Puzzle;

namespace Tests.EditMode
{
    public class SolveTrackerTests
    {
        [Test]
        public void Empty_IsNotSolved()
        {
            var t = new SolveTracker();
            Assert.IsFalse(t.IsSolved);
        }

        [Test]
        public void SingleObjective_SolvesWhenMet_FiresOnce()
        {
            var t = new SolveTracker();
            var key = new object();
            t.Register(key);
            int fired = 0;
            t.OnSolved += () => fired++;

            Assert.IsFalse(t.IsSolved);
            t.SetMet(key, true);

            Assert.IsTrue(t.IsSolved);
            Assert.AreEqual(1, fired);

            // 이미 solved 상태에서 재설정해도 재발화 없음
            t.SetMet(key, true);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void MultipleObjectives_SolveOnlyWhenAllMet()
        {
            var t = new SolveTracker();
            var a = new object();
            var b = new object();
            t.Register(a);
            t.Register(b);
            int fired = 0;
            t.OnSolved += () => fired++;

            t.SetMet(a, true);
            Assert.IsFalse(t.IsSolved, "하나만 충족 → 미해결");
            Assert.AreEqual(0, fired);

            t.SetMet(b, true);
            Assert.IsTrue(t.IsSolved);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(2, t.MetCount);
        }

        [Test]
        public void SetMet_AutoRegisters()
        {
            var t = new SolveTracker();
            var key = new object();
            t.SetMet(key, true); // Register 없이도 등록+충족
            Assert.AreEqual(1, t.Count);
            Assert.IsTrue(t.IsSolved);
        }

        [Test]
        public void Reset_ClearsSolved_AndCanFireAgain()
        {
            var t = new SolveTracker();
            var key = new object();
            t.Register(key);
            int fired = 0;
            t.OnSolved += () => fired++;

            t.SetMet(key, true);
            Assert.IsTrue(t.IsSolved);

            t.Reset();
            Assert.IsFalse(t.IsSolved);
            Assert.AreEqual(0, t.MetCount);

            t.SetMet(key, true);
            Assert.IsTrue(t.IsSolved);
            Assert.AreEqual(2, fired, "리셋 후 다시 충족되면 재발화");
        }
    }
}
