using NUnit.Framework;
using UnityEngine;
using Help.Combat;

namespace Tests.EditMode
{
    // 팔 리그의 2마디 IK. Docs/ARM_RIG_PLAN.md.
    public class TwoBoneIkTests
    {
        [Test]
        public void IkShouldPutTheHandOnAReachableTarget()
        {
            var target = new Vector2(8f, -12f);
            TwoBoneIk.Solve(target, 11f, 9f, out float upper, out float fore);
            Vector2 hand = Dir(upper) * 11f + Dir(fore) * 9f;
            Assert.That(Vector2.Distance(hand, target), Is.LessThan(0.01f));
        }

        [Test]
        public void IkShouldStretchTowardATargetOutOfReach()
        {
            TwoBoneIk.Solve(new Vector2(100f, 0f), 11f, 9f, out float upper, out float fore);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(upper, 0f)), Is.LessThan(3f));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(fore, 0f)), Is.LessThan(3f));
        }

        [Test]
        public void IkShouldLetTheElbowHangDown()
        {
            // 수평으로 뻗은 목표: 팔꿈치는 위로 꺾이지 않고 아래로 처진다.
            TwoBoneIk.Solve(new Vector2(15f, 0f), 11f, 9f, out float upper, out _);
            Assert.That((Dir(upper) * 11f).y, Is.LessThan(0f));
        }

        [Test]
        public void IkFlipShouldBendTheElbowTheOtherWayAndStillReach()
        {
            // 주먹을 몸쪽으로 당긴 자세처럼 "아래로 처지는 팔꿈치"가 반대로 꺾여 보일 때 다른 해를 고른다.
            var target = new Vector2(15f, 0f);
            TwoBoneIk.Solve(target, 11f, 9f, true, out float upper, out float fore);
            Assert.That((Dir(upper) * 11f).y, Is.GreaterThan(0f));
            Assert.That(Vector2.Distance(Dir(upper) * 11f + Dir(fore) * 9f, target), Is.LessThan(0.01f));
        }

        private static Vector2 Dir(float degrees) =>
            new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
    }
}
