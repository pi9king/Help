using NUnit.Framework;
using Help.Combat;

namespace Tests.EditMode
{
    // 공격 시트 6프레임(중립·예비·치켜듦·타격·팔로스루·회수)을 무기의 단계 타이밍에 맞춘다.
    // 균일 재생에서는 예비동작 그림이 판정 시작 뒤까지 남아 타격과 그림이 어긋났다.
    public class AttackFrameTimingTests
    {
        private const float Windup = 0.1f;
        private const float Active = 0.2f;
        private const float Recovery = 0.2f;

        private static int At(float elapsed) =>
            AttackFrameTiming.FrameAt(elapsed, Windup, Active, Recovery);

        [Test]
        public void ShouldShowAnticipationAtStartOfWindup()
        {
            Assert.That(At(0f), Is.EqualTo(AttackFrameTiming.Anticipation));
        }

        [Test]
        public void ShouldRaiseWeaponLateInWindup()
        {
            Assert.That(At(Windup * 0.9f), Is.EqualTo(AttackFrameTiming.WindUp));
        }

        [Test]
        public void ShouldShowImpactTheMomentActiveStarts()
        {
            Assert.That(At(Windup + 0.001f), Is.EqualTo(AttackFrameTiming.Impact));
        }

        [Test]
        public void ShouldHoldImpactForMostOfActive()
        {
            Assert.That(At(Windup + Active * 0.5f), Is.EqualTo(AttackFrameTiming.Impact));
        }

        [Test]
        public void ShouldFollowThroughAtEndOfActive()
        {
            Assert.That(At(Windup + Active * 0.95f), Is.EqualTo(AttackFrameTiming.FollowThrough));
        }

        [Test]
        public void ShouldShowRecoveryDuringRecovery()
        {
            Assert.That(At(Windup + Active + Recovery * 0.5f), Is.EqualTo(AttackFrameTiming.Recovery));
        }

        [Test]
        public void ShouldNotSkipImpactWhenWindupIsZero()
        {
            Assert.That(AttackFrameTiming.FrameAt(0f, 0f, Active, Recovery),
                Is.EqualTo(AttackFrameTiming.Impact));
        }

        [Test]
        public void ShouldStayOnRecoveryAfterTheAttackEnds()
        {
            Assert.That(At(Windup + Active + Recovery + 1f), Is.EqualTo(AttackFrameTiming.Recovery));
        }

        [Test]
        public void ShouldSampleTheMiddleOfAFrameInTheClip()
        {
            // 클립은 6프레임 + 마지막 프레임 유지 키 1개 = 7칸 길이다.
            Assert.That(AttackFrameTiming.ClipTime(0), Is.EqualTo(0.5f / 7f).Within(0.0001f));
            Assert.That(AttackFrameTiming.ClipTime(5), Is.EqualTo(5.5f / 7f).Within(0.0001f));
        }
    }
}
