using System.Linq;
using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    // 방의 재료를 적들에게 나눠주는 규칙.
    // 노림수: "잡아도 글자가 안 나올 수 있다"를 확률이 아니라 배분의 희소성으로 만든다 —
    // 확률로 굴리면 "모든 몬스터를 잡으면 방 몫을 전부 얻는다"가 깨지기 때문이다.
    public class LootDistributionTests
    {
        [Test]
        public void Assign_PreservesTotal()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                var shares = LootDistribution.Assign(lootCount: 5, enemyCount: 3, seed: seed);
                Assert.AreEqual(3, shares.Length);
                Assert.AreEqual(5, shares.Sum(), "전멸하면 방 몫 전량을 얻어야 한다");
            }
        }

        [Test]
        public void Assign_LeavesSomeEnemiesEmptyWhenLootIsScarce()
        {
            var shares = LootDistribution.Assign(lootCount: 1, enemyCount: 3, seed: 4);

            Assert.AreEqual(1, shares.Sum());
            Assert.AreEqual(2, shares.Count(s => s == 0), "재료보다 적이 많으면 빈손인 적이 생겨야 한다");
        }

        [Test]
        public void Assign_SpreadsEvenlyWhenLootExceedsEnemies()
        {
            var shares = LootDistribution.Assign(lootCount: 5, enemyCount: 2, seed: 2);

            Assert.AreEqual(5, shares.Sum());
            Assert.LessOrEqual(shares.Max() - shares.Min(), 1, "몫 차이는 1을 넘지 않아야 한다");
        }

        [Test]
        public void Assign_IsDeterministic()
        {
            CollectionAssert.AreEqual(
                LootDistribution.Assign(4, 5, seed: 11),
                LootDistribution.Assign(4, 5, seed: 11));
        }

        [Test]
        public void Assign_HandlesNoEnemies()
        {
            Assert.AreEqual(0, LootDistribution.Assign(3, 0, seed: 1).Length);
        }
    }
}
