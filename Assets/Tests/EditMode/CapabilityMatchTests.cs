using System.Collections.Generic;
using NUnit.Framework;
using Help.Item;
using Help.Puzzle;

namespace Tests.EditMode
{
    public class CapabilityMatchTests
    {
        [Test]
        public void NoneRequirement_AlwaysResolves()
        {
            Assert.IsTrue(CapabilityMatch.Resolves(Capability.None, new List<Capability>()));
            Assert.IsTrue(CapabilityMatch.Resolves(Capability.None, (IReadOnlyCollection<Capability>)null));
            Assert.IsTrue(CapabilityMatch.Resolves(Capability.None, Capability.Melt));
        }

        [Test]
        public void NewCapabilityValues_DoNotShiftExistingIndices()
        {
            // 프리팹(CapabilityTarget._requiredCapability)이 enum을 정수로 직렬화한다.
            // 새 값은 반드시 끝에만 추가할 것 — 중간 삽입 시 기존 장애물이 조용히 다른 능력을 요구하게 된다.
            Assert.AreEqual(0, (int)Capability.None);
            Assert.AreEqual(1, (int)Capability.BreakWall, "BreakableWall.prefab이 1로 직렬화됨");
            Assert.AreEqual(2, (int)Capability.CrossGap);
            Assert.AreEqual(3, (int)Capability.Melt, "IceWall.prefab이 3으로 직렬화됨");
            Assert.AreEqual(4, (int)Capability.Conduct);
        }

        [Test]
        public void Resolves_WhenAppliedContainsRequired()
        {
            var applied = new List<Capability> { Capability.Melt, Capability.BreakWall };
            Assert.IsTrue(CapabilityMatch.Resolves(Capability.BreakWall, applied));
        }

        [Test]
        public void DoesNotResolve_WhenAppliedLacksRequired()
        {
            var applied = new List<Capability> { Capability.Melt };
            Assert.IsFalse(CapabilityMatch.Resolves(Capability.BreakWall, applied));
        }

        [Test]
        public void DoesNotResolve_WhenAppliedEmptyOrNull()
        {
            Assert.IsFalse(CapabilityMatch.Resolves(Capability.BreakWall, new List<Capability>()));
            Assert.IsFalse(CapabilityMatch.Resolves(Capability.BreakWall, (IReadOnlyCollection<Capability>)null));
        }

        [Test]
        public void SingleOverload_MatchesExactly()
        {
            Assert.IsTrue(CapabilityMatch.Resolves(Capability.BreakWall, Capability.BreakWall));
            Assert.IsFalse(CapabilityMatch.Resolves(Capability.BreakWall, Capability.Melt));
            Assert.IsTrue(CapabilityMatch.Resolves(Capability.None, Capability.None));
        }
    }
}
