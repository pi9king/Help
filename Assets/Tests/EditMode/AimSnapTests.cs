using NUnit.Framework;
using UnityEngine;
using Help.Combat;

namespace Tests.EditMode
{
    // 조준각을 n분할로 스냅하는 규칙. 4방향 스프라이트와 360° 조준을 비교하기 위한 토글의 기반.
    public class AimSnapTests
    {
        private static void AssertDirection(Vector2 actual, Vector2 expected)
        {
            Assert.That(Vector2.Distance(actual, expected.normalized), Is.LessThan(0.001f),
                        $"expected {expected.normalized} but was {actual}");
        }

        [Test]
        public void ShouldReturnUnchangedDirectionWhenStepsIsZero()
        {
            Vector2 raw = new Vector2(3f, 1f);

            AssertDirection(AimGeometry.Snap(raw, 0), raw);
        }

        [Test]
        public void ShouldSnapToNearestAxisWithFourSteps()
        {
            AssertDirection(AimGeometry.Snap(new Vector2(1f, 0.3f), 4), Vector2.right);
            AssertDirection(AimGeometry.Snap(new Vector2(0.3f, 1f), 4), Vector2.up);
            AssertDirection(AimGeometry.Snap(new Vector2(-1f, -0.3f), 4), Vector2.left);
            AssertDirection(AimGeometry.Snap(new Vector2(-0.3f, -1f), 4), Vector2.down);
        }

        [Test]
        public void ShouldKeepExactAxesWithFourSteps()
        {
            AssertDirection(AimGeometry.Snap(Vector2.right, 4), Vector2.right);
            AssertDirection(AimGeometry.Snap(Vector2.up, 4), Vector2.up);
            AssertDirection(AimGeometry.Snap(Vector2.left, 4), Vector2.left);
            AssertDirection(AimGeometry.Snap(Vector2.down, 4), Vector2.down);
        }

        [Test]
        public void ShouldNotProduceDiagonalsWithFourSteps()
        {
            // 4방향 스냅은 대각선을 만들지 않는다 — 시트가 4방향뿐이므로.
            for (int deg = 0; deg < 360; deg += 7)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector2 snapped = AimGeometry.Snap(new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)), 4);

                bool onAxis = Mathf.Abs(snapped.x) < 0.001f || Mathf.Abs(snapped.y) < 0.001f;
                Assert.That(onAxis, Is.True, $"{deg}도가 축 위로 스냅되지 않았다: {snapped}");
            }
        }

        [Test]
        public void ShouldAllowDiagonalsWithEightSteps()
        {
            AssertDirection(AimGeometry.Snap(new Vector2(1f, 0.9f), 8), new Vector2(1f, 1f));
            AssertDirection(AimGeometry.Snap(new Vector2(-1f, 1.1f), 8), new Vector2(-1f, 1f));
        }

        [Test]
        public void ShouldFallBackToDefaultDirectionWhenInputIsZero()
        {
            AssertDirection(AimGeometry.Snap(Vector2.zero, 4), Vector2.down);
            AssertDirection(AimGeometry.Snap(Vector2.zero, 0), Vector2.down);
        }
    }
}
