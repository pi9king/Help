using System.Collections.Generic;
using NUnit.Framework;
using Help.Enemy;
using Help.Item;

namespace Tests.EditMode
{
    public class DropRollerTests
    {
        private static DropEntry Entry(AlphabetMaterial m, int count, float chance)
            => new DropEntry { Material = m, Count = count, Chance = chance };

        [Test]
        public void GuaranteedEntryAlwaysDrops()
        {
            var entries = new List<DropEntry> { Entry(AlphabetMaterial.A, 2, 1f) };
            var res = DropRoller.Roll(entries, () => 0.99f); // 어떤 난수든
            Assert.AreEqual(1, res.Count);
            Assert.AreEqual(AlphabetMaterial.A, res[0].Material);
            Assert.AreEqual(2, res[0].Count);
        }

        [Test]
        public void ZeroChanceNeverDrops()
        {
            var entries = new List<DropEntry> { Entry(AlphabetMaterial.B, 1, 0f) };
            var res = DropRoller.Roll(entries, () => 0f);
            Assert.AreEqual(0, res.Count);
        }

        [Test]
        public void HalfChanceDropsWhenRngBelowThreshold()
        {
            var entries = new List<DropEntry> { Entry(AlphabetMaterial.C, 1, 0.5f) };
            Assert.AreEqual(1, DropRoller.Roll(entries, () => 0.4f).Count, "0.4 < 0.5 → 드랍");
            Assert.AreEqual(0, DropRoller.Roll(entries, () => 0.6f).Count, "0.6 >= 0.5 → 미드랍");
        }

        [Test]
        public void SkipsNonPositiveCount()
        {
            var entries = new List<DropEntry> { Entry(AlphabetMaterial.D, 0, 1f) };
            Assert.AreEqual(0, DropRoller.Roll(entries, () => 0f).Count);
        }

        [Test]
        public void RollsMultipleEntriesIndependently()
        {
            var entries = new List<DropEntry>
            {
                Entry(AlphabetMaterial.A, 1, 1f),
                Entry(AlphabetMaterial.B, 1, 0f),
                Entry(AlphabetMaterial.C, 3, 1f),
            };
            var res = DropRoller.Roll(entries, () => 0.5f);
            Assert.AreEqual(2, res.Count);
            Assert.AreEqual(AlphabetMaterial.A, res[0].Material);
            Assert.AreEqual(AlphabetMaterial.C, res[1].Material);
            Assert.AreEqual(3, res[1].Count);
        }

        [Test]
        public void NullEntriesReturnsEmpty()
        {
            Assert.AreEqual(0, DropRoller.Roll(null, () => 0f).Count);
        }
    }
}
