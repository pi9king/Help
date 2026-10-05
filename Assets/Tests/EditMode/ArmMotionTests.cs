using NUnit.Framework;
using UnityEngine;
using Help.Combat;

namespace Tests.EditMode
{
    // 팔 리그 기본 모션(베기·찌르기·내려찍기). Docs/ARM_RIG_PLAN.md.
    // 각도 규약: 리그 각도 0° = 팔이 아래로 늘어진 자세, + = 반시계. 조준각은 0° = 오른쪽, + = 반시계.
    // 리듬: 예비동작 뒤 30%는 힘 모으기(거의 정지) -> 타격은 타격 구간 앞 45% 안에 끝 -> 오버슈트에서 멈춤 -> 회수.
    public class ArmMotionTests
    {
        private const float Windup = 0.1f;
        private const float Active = 0.2f;
        private const float Recovery = 0.2f;

        private static ArmRigPose At(ArmMotionKind kind, float aim, float elapsed) =>
            ArmMotion.Evaluate(kind, aim, elapsed, Windup, Active, Recovery);

        // 무기 팔이 가리키는 월드 각도와 기대 각도의 차이.
        private static float Off(ArmRigPose pose, float expected) =>
            Mathf.Abs(Mathf.DeltaAngle(ArmMotion.PointingDegrees(pose.MainShoulder), expected));

        [TestCase(ArmMotionKind.Slash)]
        [TestCase(ArmMotionKind.Thrust)]
        [TestCase(ArmMotionKind.Smash)]
        public void ShouldStartFromRest(ArmMotionKind kind)
        {
            ArmRigPose pose = At(kind, 30f, 0f);
            Assert.That(pose.MainShoulder, Is.EqualTo(ArmMotion.Rest.MainShoulder).Within(0.01f));
            Assert.That(pose.MainElbow, Is.EqualTo(ArmMotion.Rest.MainElbow).Within(0.01f));
        }

        [TestCase(ArmMotionKind.Slash)]
        [TestCase(ArmMotionKind.Thrust)]
        [TestCase(ArmMotionKind.Smash)]
        public void ShouldReturnToRestAfterRecovery(ArmMotionKind kind)
        {
            ArmRigPose pose = At(kind, 30f, Windup + Active + Recovery);
            Assert.That(Mathf.DeltaAngle(pose.MainShoulder, ArmMotion.Rest.MainShoulder), Is.EqualTo(0f).Within(0.5f));
            Assert.That(pose.MainElbow, Is.EqualTo(ArmMotion.Rest.MainElbow).Within(0.5f));
            Assert.That(pose.WeaponAngle, Is.EqualTo(ArmMotion.Rest.WeaponAngle).Within(0.5f));
            Assert.That(pose.Reach, Is.EqualTo(0f).Within(0.001f));
            Assert.That(pose.BodyLean, Is.EqualTo(0f).Within(0.01f));
            Assert.That(pose.BodyDrop, Is.EqualTo(0f).Within(0.01f));
        }

        [TestCase(ArmMotionKind.Slash)]
        [TestCase(ArmMotionKind.Smash)]
        public void ShouldHoldTheAnticipationBeforeStriking(ArmMotionKind kind)
        {
            float loaded = At(kind, 0f, Windup * 0.7f).MainShoulder;
            float cocked = At(kind, 0f, Windup).MainShoulder;
            Assert.That(Mathf.Abs(cocked - loaded), Is.LessThan(8f));
        }

        [TestCase(0f)]
        [TestCase(90f)]
        [TestCase(200f)]
        public void SlashShouldSweepAcrossTheAimAndOvershoot(float aim)
        {
            Assert.That(Off(At(ArmMotionKind.Slash, aim, Windup * 0.7f), aim + 75f), Is.LessThan(2f));
            Assert.That(Off(At(ArmMotionKind.Slash, aim, Windup + Active * 0.225f), aim), Is.LessThan(15f));
            // 타격은 타격 구간 앞 45%에 끝나고, 목표(-75°)보다 더 나간 자리에서 멈춘다.
            Assert.That(Off(At(ArmMotionKind.Slash, aim, Windup + Active * 0.45f), aim - 85f), Is.LessThan(2f));
            Assert.That(Off(At(ArmMotionKind.Slash, aim, Windup + Active), aim - 85f), Is.LessThan(2f));
        }

        [TestCase(ArmMotionKind.Slash)]
        [TestCase(ArmMotionKind.Smash)]
        public void WristShouldCockBackThenSnapThrough(ArmMotionKind kind)
        {
            Assert.That(At(kind, 0f, Windup).WeaponAngle, Is.GreaterThan(200f));
            Assert.That(At(kind, 0f, Windup + Active * 0.45f).WeaponAngle, Is.LessThan(180f));
        }

        [TestCase(ArmMotionKind.Slash)]
        [TestCase(ArmMotionKind.Thrust)]
        [TestCase(ArmMotionKind.Smash)]
        public void BodyShouldPullBackThenCommitIntoTheStrike(ArmMotionKind kind)
        {
            Assert.That(At(kind, 0f, Windup).BodyLean, Is.LessThan(0f));
            Assert.That(At(kind, 0f, Windup + Active * 0.45f).BodyLean, Is.GreaterThan(0f));
        }

        [Test]
        public void ThrustShouldPullBackThenExtend()
        {
            ArmRigPose loaded = At(ArmMotionKind.Thrust, 0f, Windup);
            ArmRigPose struck = At(ArmMotionKind.Thrust, 0f, Windup + Active * 0.45f);
            Assert.That(loaded.MainElbow, Is.GreaterThan(90f));
            Assert.That(loaded.Reach, Is.LessThan(0f));
            Assert.That(struck.MainElbow, Is.LessThan(5f));
            Assert.That(struck.Reach, Is.GreaterThan(0.12f));
            Assert.That(Off(struck, 0f), Is.LessThan(5f));
        }

        [Test]
        public void SmashShouldRiseThenComeDownOntoTheAimAndCrouch()
        {
            ArmRigPose raised = At(ArmMotionKind.Smash, 0f, Windup * 0.7f);
            ArmRigPose landed = At(ArmMotionKind.Smash, 0f, Windup + Active * 0.45f);
            Assert.That(Off(raised, 150f), Is.LessThan(2f));
            Assert.That(raised.BodyDrop, Is.LessThan(0f));
            Assert.That(Off(landed, -18f), Is.LessThan(2f));
            Assert.That(landed.BodyDrop, Is.GreaterThan(0f));
        }

        [Test]
        public void SmashShouldAccelerateIntoTheHit()
        {
            float start = At(ArmMotionKind.Smash, 0f, Windup).MainShoulder;
            float end = At(ArmMotionKind.Smash, 0f, Windup + Active * 0.45f).MainShoulder;
            float half = At(ArmMotionKind.Smash, 0f, Windup + Active * 0.225f).MainShoulder;
            Assert.That(Mathf.Abs(half - start), Is.LessThan(Mathf.Abs(end - start) * 0.5f));
        }

        [Test]
        public void StrikeShouldBeFlaggedOnlyDuringTheSwing()
        {
            Assert.That(ArmMotion.StrikeStarted(Windup - 0.001f, Windup), Is.False);
            Assert.That(ArmMotion.StrikeStarted(Windup, Windup), Is.True);
        }

        [Test]
        public void SteppedTimeShouldHoldEachPoseForOneSpriteFrame()
        {
            // 스프라이트 애니메이션과 같은 박자(12fps)로 자세를 끊어 보여 준다.
            Assert.That(ArmMotion.StepTime(0.05f, 12f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ArmMotion.StepTime(0.09f, 12f), Is.EqualTo(1f / 12f).Within(0.0001f));
            Assert.That(ArmMotion.StepTime(0.25f, 12f), Is.EqualTo(3f / 12f).Within(0.0001f));
        }

        [Test]
        public void StrikeShouldBeSteppedWithMoreFramesThanTheRest()
        {
            // 휘두르는 순간만 프레임을 늘린다(12 -> 24fps). 그 밖은 스프라이트 박자 그대로.
            const float start = 0.12f, end = 0.2f;
            Assert.That(ArmMotion.StepTime(0.05f, start, end, 12f, 24f), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(ArmMotion.StepTime(0.135f, start, end, 12f, 24f), Is.EqualTo(3f / 24f).Within(0.0001f));
            Assert.That(ArmMotion.StepTime(0.17f, start, end, 12f, 24f), Is.EqualTo(4f / 24f).Within(0.0001f));
            Assert.That(ArmMotion.StepTime(0.25f, start, end, 12f, 24f), Is.EqualTo(3f / 12f).Within(0.0001f));
        }

        [Test]
        public void StrikeStepShouldNotShowAPoseFromBeforeTheStrike()
        {
            // 24fps 칸이 타격 시작보다 앞이면 타격 첫 자세를 보여 준다(예비동작으로 되돌아가지 않게).
            Assert.That(ArmMotion.StepTime(0.121f, 0.12f, 0.2f, 12f, 24f), Is.EqualTo(0.12f).Within(0.0001f));
        }

        [Test]
        public void SteppedTimeShouldPassThroughWhenFpsIsZero()
        {
            Assert.That(ArmMotion.StepTime(0.123f, 0f), Is.EqualTo(0.123f).Within(0.0001f));
        }

        [Test]
        public void PointingDegreesShouldTreatHangingArmAsDown()
        {
            Assert.That(ArmMotion.PointingDegrees(0f), Is.EqualTo(-90f).Within(0.01f));
            Assert.That(ArmMotion.ShoulderFor(0f), Is.EqualTo(90f).Within(0.01f));
        }
    }
}
