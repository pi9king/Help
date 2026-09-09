using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    // 콘텐츠 프리팹(K·Y 줍기, 적, 장애물)은 "방 바닥"을 원점으로 저작한다.
    //
    // 실제 사고(2026-09): 방 크기 등급(Small 25×15 등)이 들어오기 전 방은 13×9였고,
    // 콘텐츠 프리팹이 방 중심(원점) 기준 y=-2.5 같은 값을 하드코딩하고 있었다.
    // 방이 높아지자 바닥은 내려갔는데 콘텐츠 좌표는 그대로여서 K·Y와 잠긴 문이
    // 공중에 3타일 떠버렸다 — 첫 방 튜토리얼이 통째로 깨졌다.
    // 바닥선을 방 높이에서 계산하게 만들어 그 재발을 막는다.
    public class RoomGeometryTests
    {
        // 셸의 바닥 한 줄은 레이아웃 y==0 → 셀 y = -(height/2).
        // 그 칸의 윗면(=디딤면)이 콘텐츠 원점이다.
        [Test]
        public void ContentOriginShouldSitOnFloorSurface()
        {
            Assert.AreEqual(-6f, RoomGeometry.ContentOriginY(15), 0.0001f); // Small 25×15
        }

        [Test]
        public void ContentOriginShouldFollowRoomHeight()
        {
            Assert.AreEqual(-14f, RoomGeometry.ContentOriginY(31), 0.0001f); // Tall 25×31
        }

        // 방 크기 등급이 생기기 전의 13×9 방. 당시 손으로 맞춘 값과 일치해야
        // "예전엔 맞았다"는 사실이 검증된다(회귀의 기준점).
        [Test]
        public void ContentOriginShouldMatchLegacyRoomHeight()
        {
            Assert.AreEqual(-3f, RoomGeometry.ContentOriginY(9), 0.0001f);
        }

        // 방이 높아질수록 바닥은 내려간다 — 부호가 뒤집히면 콘텐츠가 천장에 붙는다.
        [Test]
        public void TallerRoomShouldPushFloorDown()
        {
            Assert.Less(RoomGeometry.ContentOriginY(31), RoomGeometry.ContentOriginY(15));
        }

        // 모든 등급에서 바닥이 방 안에 있어야 한다(셸 바깥으로 새면 콘텐츠가 벽 밖에 스폰된다).
        [Test]
        public void ContentOriginShouldStayInsideRoom()
        {
            foreach (var sizeClass in new[] { RoomSizeClass.Small, RoomSizeClass.Wide, RoomSizeClass.Tall })
            {
                var dim = RoomDimensions.Of(sizeClass);
                float y = RoomGeometry.ContentOriginY(dim.Height);
                Assert.Greater(y, -dim.Height / 2f, $"{sizeClass}: 바닥이 방 아래로 샘");
                Assert.Less(y, dim.Height / 2f, $"{sizeClass}: 바닥이 천장 위로 감");
            }
        }

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
