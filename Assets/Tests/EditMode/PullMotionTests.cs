using NUnit.Framework;
using Help.Enemy;

namespace Tests.EditMode
{
    // 끌어당김의 운동학 — 적의 "의지"(EnemyAI)가 아니라 외력이므로 별도 순수 로직으로 분리.
    public class PullMotionTests
    {
        private const float Speed = 8f;
        private const float Stop = 0.5f;

        [Test]
        public void AnchorToTheRight_PullsWithPositiveVelocity()
        {
            Assert.AreEqual(Speed, PullMotion.VelocityX(0f, 5f, Speed, Stop), "오른쪽 앵커 → +x로 끌림");
        }

        [Test]
        public void AnchorToTheLeft_PullsWithNegativeVelocity()
        {
            Assert.AreEqual(-Speed, PullMotion.VelocityX(0f, -5f, Speed, Stop), "왼쪽 앵커 → -x로 끌림");
        }

        [Test]
        public void WithinStopDistance_ProducesZeroVelocity_ToAvoidJitter()
        {
            Assert.AreEqual(0f, PullMotion.VelocityX(0f, 0.3f, Speed, Stop), "정지 거리 안에선 멈춤(진동 방지)");
        }

        [Test]
        public void ExactlyAtAnchor_ProducesZeroVelocity_WithoutNaN()
        {
            float v = PullMotion.VelocityX(2f, 2f, Speed, Stop);
            Assert.AreEqual(0f, v);
            Assert.IsFalse(float.IsNaN(v), "0 거리에서 NaN이 나오면 안 됨");
        }

        [Test]
        public void PullVelocity_IsIndependentOfDistance()
        {
            // 끌기 속도는 거리에 비례하지 않는 등속 — 적의 이동속도와도 무관하다
            Assert.AreEqual(PullMotion.VelocityX(0f, 2f, Speed, Stop),
                            PullMotion.VelocityX(0f, 20f, Speed, Stop),
                            "거리와 무관하게 일정한 끌기 속도");
        }

        [Test]
        public void WithinStopDistance_ReportsComplete()
        {
            Assert.IsTrue(PullMotion.IsComplete(0f, 0.4f, Stop), "정지 거리 안이면 끌기 완료");
            Assert.IsFalse(PullMotion.IsComplete(0f, 3f, Stop), "아직 멀면 미완료");
        }
    }
}
