using NUnit.Framework;
using Help.Combat;

namespace Tests.EditMode
{
    // 공격 중 몸통이 조준 방향으로 밀렸다 되돌아오는 곡선.
    // Windup에 뒤로 당기고 -> Active에 앞으로 내지르고 -> Recovery에 복귀.
    public class AttackLungeTests
    {
        private const float Windup = 0.1f;
        private const float Active = 0.2f;
        private const float Recovery = 0.2f;
        private const float Pull = 0.1f;
        private const float Reach = 0.4f;

        private static float At(float elapsed) =>
            AttackLunge.Offset(elapsed, Windup, Active, Recovery, Pull, Reach);

        [Test]
        public void ShouldStartAtRest()
        {
            Assert.That(At(0f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ShouldPullBackwardDuringWindup()
        {
            Assert.That(At(Windup * 0.5f), Is.LessThan(0f));
            Assert.That(At(Windup * 0.99f), Is.LessThan(At(Windup * 0.25f)));
        }

        [Test]
        public void ShouldReachFullPullAtEndOfWindup()
        {
            Assert.That(At(Windup), Is.EqualTo(-Pull).Within(0.01f));
        }

        [Test]
        public void ShouldThrustForwardAndPeakAtEndOfActive()
        {
            Assert.That(At(Windup + Active), Is.EqualTo(Reach).Within(0.01f));
        }

        [Test]
        public void ShouldCrossRestWhileStriking()
        {
            // 뒤 -> 앞으로 부호가 바뀌는 순간이 Active 안에 있어야 한다.
            Assert.That(At(Windup + Active * 0.05f), Is.LessThan(0f));
            Assert.That(At(Windup + Active * 0.95f), Is.GreaterThan(0f));
        }

        [Test]
        public void ShouldReturnToRestWhenDone()
        {
            float total = Windup + Active + Recovery;
            Assert.That(At(total), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(At(total + 1f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ShouldStayWithinPullAndReachBounds()
        {
            float total = Windup + Active + Recovery;
            for (float t = 0f; t <= total; t += total / 200f)
            {
                float offset = At(t);
                Assert.That(offset, Is.GreaterThanOrEqualTo(-Pull - 0.0001f), $"t={t}");
                Assert.That(offset, Is.LessThanOrEqualTo(Reach + 0.0001f), $"t={t}");
            }
        }

        [Test]
        public void ShouldHandleZeroLengthPhasesWithoutDividingByZero()
        {
            Assert.That(AttackLunge.Offset(0f, 0f, 0f, 0f, Pull, Reach), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(AttackLunge.Offset(0.05f, 0f, 0.1f, 0f, Pull, Reach), Is.GreaterThan(-Pull - 0.0001f));
            Assert.That(AttackLunge.Offset(0.5f, 0f, 0f, 0f, Pull, Reach), Is.EqualTo(0f).Within(0.0001f));
        }

        // 클립 길이를 무기 타이밍에 맞추는 재생 속도.
        [Test]
        public void ShouldKeepHorizontalLungeDistance()
        {
            UnityEngine.Vector2 moved = AttackLunge.Displacement(UnityEngine.Vector2.right, 0.25f, 0.35f);
            Assert.That(moved.x, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(moved.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void ShouldCompressVerticalLungeInQuarterView()
        {
            // 쿼터뷰에서 위아래 이동은 발이 지면에서 미끄러지며 다리가 늘어나 보인다.
            UnityEngine.Vector2 moved = AttackLunge.Displacement(UnityEngine.Vector2.down, 0.25f, 0.35f);
            Assert.That(moved.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(moved.y, Is.EqualTo(-0.0875f).Within(0.0001f));
        }
    }
}
