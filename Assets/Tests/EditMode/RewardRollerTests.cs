using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Help.Enemy;
using Help.Item;

namespace Tests.EditMode
{
    // 몬스터의 알파벳 외 보상(골드/포션) 굴림.
    // 알파벳(DropEntry)과 달리 이쪽은 순수 확률 보너스 — 층 예산과 무관하다.
    public class RewardRollerTests
    {
        private static RewardEntry Entry(RewardKind kind, int amount, float chance) =>
            new RewardEntry { Kind = kind, Amount = amount, Chance = chance };

        [Test]
        public void Roll_AlwaysIncludesCertainReward()
        {
            var results = RewardRoller.Roll(new[] { Entry(RewardKind.Gold, 12, 1f) }, () => 0.99f);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(RewardKind.Gold, results[0].Kind);
            Assert.AreEqual(12, results[0].Amount);
        }

        [Test]
        public void Roll_SkipsZeroChance()
        {
            var results = RewardRoller.Roll(new[] { Entry(RewardKind.Potion, 1, 0f) }, () => 0f);
            Assert.IsEmpty(results);
        }

        [Test]
        public void Roll_RespectsProbability()
        {
            var table = new[] { Entry(RewardKind.Gold, 5, 0.5f) };

            Assert.AreEqual(1, RewardRoller.Roll(table, () => 0.49f).Count, "굴림이 확률보다 작으면 획득");
            Assert.AreEqual(0, RewardRoller.Roll(table, () => 0.5f).Count, "굴림이 확률 이상이면 미획득");
        }

        [Test]
        public void Roll_SkipsNonPositiveAmount()
        {
            var results = RewardRoller.Roll(new[] { Entry(RewardKind.Gold, 0, 1f) }, () => 0f);
            Assert.IsEmpty(results);
        }

        [Test]
        public void Roll_HandlesEmptyTable()
        {
            Assert.IsEmpty(RewardRoller.Roll(null, () => 0f));
            Assert.IsEmpty(RewardRoller.Roll(new List<RewardEntry>(), () => 0f));
        }

        [Test]
        public void Roll_KeepsEveryHitFromMixedTable()
        {
            var results = RewardRoller.Roll(new[]
            {
                Entry(RewardKind.Gold, 7, 1f),
                Entry(RewardKind.Potion, 1, 1f),
            }, () => 0f);

            Assert.AreEqual(2, results.Count);
            CollectionAssert.AreEquivalent(
                new[] { RewardKind.Gold, RewardKind.Potion },
                results.Select(r => r.Kind).ToArray());
        }
    }
}
