using NUnit.Framework;
using UnityEngine;
using Help.Combat;

namespace Tests.EditMode
{
    // 팔 리그 모션 = 방향별 화면 키 자세 4개(장전·힘 모으기·타격 중간·끝). 손은 IK가 따라가고,
    // 무기 각도는 화면 기준이라 팔이 접혀도 뒤집히지 않는다. Docs/ARM_RIG_PLAN.md.
    public class ArmKeyMotionTests
    {
        private const float Windup = 0.1f;
        private const float Active = 0.2f;
        private const float Recovery = 0.2f;
        private static float StrikeEnd => Windup + Active * ArmMotion.StrikeShare;

        private static readonly ArmKey Rest = new ArmKey { Hand = new Vector2(1f, -19f), Weapon = -70f, Front = true };

        private static ArmKeyTrack Track(ArmStrikeCurve curve = ArmStrikeCurve.Smooth) => new ArmKeyTrack
        {
            Loaded = new ArmKey { Hand = new Vector2(-9f, -4f), Weapon = 165f, Front = false },
            Cocked = new ArmKey { Hand = new Vector2(-11f, -3f), Weapon = 175f, Front = false },
            Mid = new ArmKey { Hand = new Vector2(14f, -6f), Weapon = 5f, Front = true },
            Through = new ArmKey { Hand = new Vector2(10f, -12f), Weapon = -40f, Front = true,
                                   Shoulder = new Vector2(-1f, -3f), ElbowFlip = true },
            Curve = curve,
        };

        private static ArmKey At(float elapsed, ArmStrikeCurve curve = ArmStrikeCurve.Smooth) =>
            ArmKeyMotion.Evaluate(Track(curve), Rest, elapsed, Windup, Active, Recovery);

        private static void Near(ArmKey actual, ArmKey expected)
        {
            Assert.That(Vector2.Distance(actual.Hand, expected.Hand), Is.LessThan(0.01f));
            Assert.That(actual.Weapon, Is.EqualTo(expected.Weapon).Within(0.01f));
        }

        [Test]
        public void ShouldStartAtRest() => Near(At(0f), Rest);

        [Test]
        public void ShouldReachTheLoadedPoseAtSeventyPercentOfWindup() =>
            Near(At(Windup * (1f - ArmMotion.HoldShare)), Track().Loaded);

        [Test]
        public void ShouldReachTheCockedPoseAtTheEndOfWindup() => Near(At(Windup), Track().Cocked);

        [Test]
        public void ShouldPassThroughTheMidPoseHalfwayThroughTheStrike() =>
            Near(At(Windup + Active * ArmMotion.StrikeShare * 0.5f), Track().Mid);

        [Test]
        public void ShouldHoldTheEndPoseUntilTheActiveWindowEnds()
        {
            Near(At(StrikeEnd), Track().Through);
            Near(At(Windup + Active - 0.001f), Track().Through);
        }

        [Test]
        public void ShouldReturnToRestAfterRecovery() => Near(At(Windup + Active + Recovery), Rest);

        [Test]
        public void WeaponShouldSweepTheWayTheKeysAreWrittenNotTheShortestWay()
        {
            // 175° -> 5°: 화면 위쪽을 지나 앞으로 온다. 최단 경로(아래쪽)로 돌면 안 된다.
            float early = At(Windup + Active * ArmMotion.StrikeShare * 0.2f).Weapon;
            Assert.That(early, Is.InRange(5f, 175f));
        }

        [Test]
        public void FrontShouldFollowTheNearerKey()
        {
            Assert.That(At(Windup + 0.001f).Front, Is.False);
            Assert.That(At(Windup + Active * ArmMotion.StrikeShare * 0.5f).Front, Is.True);
        }

        [Test]
        public void ShoulderShouldPushOutWithTheStrikeAndReturnToRest()
        {
            // 찌르기에 어깨까지 쓴다: 키에 적은 어깨 이동을 따라가고, 휴식에서는 제자리.
            Assert.That(Vector2.Distance(At(StrikeEnd).Shoulder, new Vector2(-1f, -3f)), Is.LessThan(0.01f));
            Assert.That(At(0f).Shoulder.magnitude, Is.LessThan(0.01f));
            Assert.That(At(Windup + Active + Recovery).Shoulder.magnitude, Is.LessThan(0.01f));
        }

        [Test]
        public void ElbowFlipShouldFollowTheNearerKey()
        {
            Assert.That(At(Windup + 0.001f).ElbowFlip, Is.False);
            Assert.That(At(StrikeEnd).ElbowFlip, Is.True);
        }

        [Test]
        public void AcceleratingStrikeShouldStillBeNearTheStartAtHalfTime()
        {
            // 내려찍기용 가속 곡선: 타격 시간의 절반에서는 아직 중간 자세에 못 미친다.
            float t = Windup + Active * ArmMotion.StrikeShare * 0.5f;
            float smooth = Vector2.Distance(At(t, ArmStrikeCurve.Smooth).Hand, Track().Mid.Hand);
            float accelerating = Vector2.Distance(At(t, ArmStrikeCurve.Accelerate).Hand, Track().Mid.Hand);
            Assert.That(accelerating, Is.GreaterThan(smooth + 3f));
        }
    }
}
