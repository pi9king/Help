using NUnit.Framework;
using UnityEngine;
using Help.Combat;

namespace Tests.EditMode
{
    public class AimGeometryTests
    {
        [Test]
        public void ForwardCenterShouldFollowNormalizedAimDirection()
        {
            Vector2 center = AimGeometry.ForwardCenter(new Vector2(2f, 3f), new Vector2(1f, 1f), 2f);

            Assert.That(Vector2.Distance(center, new Vector2(2f, 3f) + new Vector2(1f, 1f).normalized * 2f),
                        Is.LessThan(0.001f));
        }

        [Test]
        public void AngleShouldUseWorldRightAsZeroDegrees()
        {
            Assert.That(AimGeometry.AngleDegrees(Vector2.right), Is.EqualTo(0f).Within(0.001f));
            Assert.That(AimGeometry.AngleDegrees(Vector2.up), Is.EqualTo(90f).Within(0.001f));
        }
    }
}
