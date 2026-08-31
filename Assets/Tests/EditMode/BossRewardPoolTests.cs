using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Help.Item;

namespace Tests.EditMode
{
    public class BossRewardPoolTests
    {
        private static ItemDefinition Make(string id, ItemType type)
        {
            var it = ScriptableObject.CreateInstance<ItemDefinition>();
            it.Id = id; it.Word = id; it.Type = type;
            return it;
        }

        [Test]
        public void CandidateAcceptsEquipmentRejectsMaterialAndConsumable()
        {
            Assert.IsTrue(BossRewardPool.IsRewardCandidate(ItemType.Weapon));
            Assert.IsTrue(BossRewardPool.IsRewardCandidate(ItemType.Accessory));
            Assert.IsFalse(BossRewardPool.IsRewardCandidate(ItemType.Material));
            Assert.IsFalse(BossRewardPool.IsRewardCandidate(ItemType.Consumable));
        }

        [Test]
        public void PickReturnsOnlyEquipmentCandidates()
        {
            var items = new List<ItemDefinition>
            {
                Make("axe", ItemType.Weapon),
                Make("a", ItemType.Material),
                Make("ring", ItemType.Accessory),
                Make("potion", ItemType.Consumable),
            };
            var picks = BossRewardPool.Pick(items, 10, 1);
            Assert.AreEqual(2, picks.Count, "장착 가능 후보(axe, ring)만");
            foreach (var p in picks) Assert.IsTrue(BossRewardPool.IsRewardCandidate(p.Type));
        }

        [Test]
        public void PickLimitsToCount()
        {
            var items = new List<ItemDefinition>
            {
                Make("w1", ItemType.Weapon), Make("w2", ItemType.Weapon),
                Make("w3", ItemType.Weapon), Make("w4", ItemType.Weapon),
            };
            Assert.AreEqual(3, BossRewardPool.Pick(items, 3, 5).Count);
        }

        [Test]
        public void PickIsDeterministicForSameSeed()
        {
            var items = new List<ItemDefinition>
            {
                Make("w1", ItemType.Weapon), Make("w2", ItemType.Weapon),
                Make("w3", ItemType.Weapon), Make("w4", ItemType.Weapon),
            };
            var a = BossRewardPool.Pick(items, 2, 42);
            var b = BossRewardPool.Pick(items, 2, 42);
            CollectionAssert.AreEqual(a, b, "같은 seed는 같은 결과");
        }

        [Test]
        public void PickHandlesFewerCandidatesThanCount()
        {
            var items = new List<ItemDefinition> { Make("w1", ItemType.Weapon) };
            Assert.AreEqual(1, BossRewardPool.Pick(items, 3, 0).Count);
        }
    }
}
