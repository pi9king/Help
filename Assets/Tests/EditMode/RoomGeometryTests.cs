using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    // 콘텐츠 프리팹은 쿼터뷰 방 중앙을 원점으로 저작한다.
    public class RoomGeometryTests
    {
        // ── 바닥 루팅 가로 배치 ──
        // 실제 사고(2026-09): 배치가 ((i % 5) - 2) * 1.6 이었다. 글자가 6개를 넘으면
        // 6번째가 1번째와 **정확히 같은 자리**에 겹쳐 쌓였다. 나눗셈 나머지로 자리를 정하면
        // 개수가 늘어나는 순간 반드시 겹친다.

        [Test]
        public void SingleLootShouldSitAtCenter()
        {
            Assert.AreEqual(0f, RoomGeometry.SpreadX(0, 1, 1.8f), 0.0001f);
        }

        [Test]
        public void LootShouldSpreadSymmetrically()
        {
            Assert.AreEqual(-0.9f, RoomGeometry.SpreadX(0, 2, 1.8f), 0.0001f);
            Assert.AreEqual(+0.9f, RoomGeometry.SpreadX(1, 2, 1.8f), 0.0001f);
        }

        [Test]
        public void OddCountShouldPutMiddleAtCenter()
        {
            Assert.AreEqual(-1.8f, RoomGeometry.SpreadX(0, 3, 1.8f), 0.0001f);
            Assert.AreEqual(0f, RoomGeometry.SpreadX(1, 3, 1.8f), 0.0001f);
            Assert.AreEqual(+1.8f, RoomGeometry.SpreadX(2, 3, 1.8f), 0.0001f);
        }

        // 회귀의 핵심: 몇 개가 오든 두 글자가 같은 자리에 놓이면 안 된다.
        [Test]
        public void NoTwoLootShouldShareTheSameSpot()
        {
            for (int count = 1; count <= 12; count++)
            {
                var seen = new System.Collections.Generic.HashSet<float>();
                for (int i = 0; i < count; i++)
                    Assert.IsTrue(seen.Add(RoomGeometry.SpreadX(i, count, 1.8f)),
                        $"글자 {count}개 중 {i}번이 앞의 글자와 같은 자리에 겹침");
            }
        }

        // 간격은 픽업 폭(0.9)보다 넓어야 콜라이더가 서로 물리지 않는다.
        [Test]
        public void AdjacentLootShouldNotOverlap()
        {
            for (int i = 1; i < 6; i++)
            {
                float gap = RoomGeometry.SpreadX(i, 6, 1.8f) - RoomGeometry.SpreadX(i - 1, 6, 1.8f);
                Assert.Greater(gap, 0.9f, "인접 글자 간격이 픽업 폭보다 좁음");
            }
        }
    }
}
