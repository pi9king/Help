using NUnit.Framework;
using Help.Combat;

namespace Tests.EditMode
{
    // 무기의 AttackSpeedMult가 공격 간격과 모션을 같은 배수로 줄인다(2026-09-20 결정).
    // 간격만 줄이면 모션이 끝나기 전에 다음 공격이 시작돼 연출이 끊긴다 — 그래서 한 규칙을 양쪽이 쓴다.
    public class AttackSpeedTests
    {
        [Test]
        public void ShouldShortenDurationByAttackSpeedMult()
        {
            Assert.AreEqual(0.2f, AttackSpeed.Scale(0.3f, 1.5f), 1e-5f);
            Assert.AreEqual(0.3f, AttackSpeed.Scale(0.3f, 1f), 1e-5f);
        }

        // 데이터가 0이나 음수면(입력 실수) 배수를 무시한다 — 0으로 나눠 공격이 무한히 빨라지면 안 된다.
        [Test]
        public void ShouldIgnoreNonPositiveMult()
        {
            Assert.AreEqual(0.3f, AttackSpeed.Scale(0.3f, 0f), 1e-5f);
            Assert.AreEqual(0.3f, AttackSpeed.Scale(0.3f, -2f), 1e-5f);
        }

        // 모양(사거리·범위·각도)은 그대로, 타이밍만 빨라진다.
        [Test]
        public void ScaledMotionShouldKeepShapeAndShortenEveryPhase()
        {
            var baseDef = AttackMotionDef.Default();

            var fast = baseDef.ScaledBy(2f);

            Assert.AreEqual(baseDef.Windup / 2f, fast.Windup, 1e-5f);
            Assert.AreEqual(baseDef.Active / 2f, fast.Active, 1e-5f);
            Assert.AreEqual(baseDef.Recovery / 2f, fast.Recovery, 1e-5f);
            Assert.AreEqual(baseDef.Reach, fast.Reach);
            Assert.AreEqual(baseDef.HitboxSize, fast.HitboxSize);
            Assert.AreEqual(baseDef.ArcStartDeg, fast.ArcStartDeg);
            Assert.AreEqual(AttackMotionDef.Default().Windup, baseDef.Windup, "원본은 바뀌지 않는다");
        }
    }
}
