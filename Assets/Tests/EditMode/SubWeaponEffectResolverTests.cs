using System.Collections.Generic;
using NUnit.Framework;
using Help.Combat;
using Help.Enemy;
using Help.Item;

namespace Tests.EditMode
{
    // 서브무기 "사용(use)"이 전투에서 내는 효과 결정 — 능력 → 상태이상 매핑(CapabilityMatch의 전투판 미러).
    public class SubWeaponEffectResolverTests
    {
        [Test]
        public void NoCapabilities_ResolvesToNoEffect()
        {
            var e = SubWeaponEffectResolver.Resolve(new List<Capability>());
            Assert.IsFalse(e.HasEffect, "능력이 없으면 전투 효과 없음");
        }

        [Test]
        public void NullCapabilityList_ResolvesToNoEffect_WithoutThrowing()
        {
            var e = SubWeaponEffectResolver.Resolve(null);
            Assert.IsFalse(e.HasEffect, "null도 예외 없이 무효과");
        }

        [Test]
        public void BindCapability_ResolvesToBindEffect_WithPositiveDuration()
        {
            var e = SubWeaponEffectResolver.Resolve(new List<Capability> { Capability.Bind });
            Assert.AreEqual(StatusEffectType.Bind, e.Status);
            Assert.Greater(e.Duration, 0f, "속박은 양의 지속시간을 가져야 함");
        }

        [Test]
        public void MeltCapability_ResolvesToNoEffect_BecauseItIsPuzzleOnly()
        {
            var e = SubWeaponEffectResolver.Resolve(new List<Capability> { Capability.Melt });
            Assert.IsFalse(e.HasEffect, "Melt는 퍼즐 전용 — 적에겐 효과 없음");
        }

        [Test]
        public void MultipleCapabilities_WithBind_ResolvesToBind()
        {
            var e = SubWeaponEffectResolver.Resolve(
                new List<Capability> { Capability.CrossGap, Capability.Bind });
            Assert.AreEqual(StatusEffectType.Bind, e.Status, "전투 효과가 있는 능력을 골라냄");
        }
    }
}
