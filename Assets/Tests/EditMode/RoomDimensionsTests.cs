using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class RoomDimensionsTests
    {
        [Test]
        public void SmallShouldFitOneScreen()
        {
            var d = RoomDimensions.Of(RoomSizeClass.Small);
            Assert.AreEqual(25, d.Width);
            Assert.AreEqual(15, d.Height);
        }

        [Test]
        public void AllSizesShouldBeOddSoRoomsAreSymmetricAboutOrigin()
        {
            // 방은 원점 중심으로 그려진다(RoomManager.RenderRoom). 폭/높이가 짝수면
            // 중심이 반 타일 어긋나 문·스폰 좌표가 비대칭이 된다.
            foreach (RoomSizeClass c in System.Enum.GetValues(typeof(RoomSizeClass)))
            {
                var d = RoomDimensions.Of(c);
                Assert.AreEqual(1, d.Width % 2, $"{c} 폭은 홀수여야 한다");
                Assert.AreEqual(1, d.Height % 2, $"{c} 높이는 홀수여야 한다");
            }
        }

        [Test]
        public void WideShouldExtendHorizontallyOnly()
        {
            var s = RoomDimensions.Of(RoomSizeClass.Small);
            var w = RoomDimensions.Of(RoomSizeClass.Wide);
            Assert.Greater(w.Width, s.Width);
            Assert.AreEqual(s.Height, w.Height);
        }

        [Test]
        public void TallShouldExtendVerticallyOnly()
        {
            var s = RoomDimensions.Of(RoomSizeClass.Small);
            var t = RoomDimensions.Of(RoomSizeClass.Tall);
            Assert.AreEqual(s.Width, t.Width);
            Assert.Greater(t.Height, s.Height);
        }

        [Test]
        public void SmallShouldNotExceedCameraView()
        {
            // ortho 7.5 → 세로 15유닛, 16:9에서 가로 26.67유닛. Small 방은 스크롤 없이 다 보여야 한다.
            var d = RoomDimensions.Of(RoomSizeClass.Small);
            Assert.LessOrEqual(d.Height, RoomDimensions.CameraOrthoSize * 2f);
            Assert.LessOrEqual(d.Width, RoomDimensions.CameraOrthoSize * 2f * (16f / 9f));
        }

        [Test]
        public void UnknownSizeShouldFallBackToSmall()
        {
            var d = RoomDimensions.Of((RoomSizeClass)99);
            Assert.AreEqual(RoomDimensions.Of(RoomSizeClass.Small).Width, d.Width);
        }
    }
}
