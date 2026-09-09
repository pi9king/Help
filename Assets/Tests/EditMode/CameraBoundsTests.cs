using NUnit.Framework;
using UnityEngine;
using Help.Core;

namespace Tests.EditMode
{
    public class CameraBoundsTests
    {
        // 25x15 방을 원점 중심으로. 카메라 뷰는 26.67 x 15.
        private static readonly Rect Small = new Rect(-12.5f, -7.5f, 25f, 15f);
        private static readonly Rect Wide = new Rect(-25.5f, -7.5f, 51f, 15f);
        private const float HalfW = 26.67f / 2f;
        private const float HalfH = 7.5f;

        [Test]
        public void ShouldCenterWhenRoomFitsInView()
        {
            var p = CameraBounds.Clamp(new Vector2(10f, 3f), Small, HalfW, HalfH);
            Assert.AreEqual(Small.center.x, p.x, 1e-3f, "방이 뷰보다 작으면 플레이어를 따라가지 않는다");
            Assert.AreEqual(Small.center.y, p.y, 1e-3f);
        }

        [Test]
        public void ShouldFollowTargetInsideWideRoom()
        {
            var p = CameraBounds.Clamp(new Vector2(5f, 0f), Wide, HalfW, HalfH);
            Assert.AreEqual(5f, p.x, 1e-3f);
        }

        [Test]
        public void ShouldClampAtLeftEdge()
        {
            var p = CameraBounds.Clamp(new Vector2(-100f, 0f), Wide, HalfW, HalfH);
            Assert.AreEqual(Wide.xMin + HalfW, p.x, 1e-3f, "방 밖(빈 공간)이 보이면 안 된다");
        }

        [Test]
        public void ShouldClampAtRightEdge()
        {
            var p = CameraBounds.Clamp(new Vector2(100f, 0f), Wide, HalfW, HalfH);
            Assert.AreEqual(Wide.xMax - HalfW, p.x, 1e-3f);
        }

        [Test]
        public void ShouldClampVerticallyInTallRoom()
        {
            var tall = new Rect(-12.5f, -15.5f, 25f, 31f);
            var top = CameraBounds.Clamp(new Vector2(0f, 100f), tall, HalfW, HalfH);
            Assert.AreEqual(tall.yMax - HalfH, top.y, 1e-3f);
            var bottom = CameraBounds.Clamp(new Vector2(0f, -100f), tall, HalfW, HalfH);
            Assert.AreEqual(tall.yMin + HalfH, bottom.y, 1e-3f);
        }

        [Test]
        public void ShouldReturnRoomBoundsForSizeClass()
        {
            var r = CameraBounds.RoomRect(25, 15);
            Assert.AreEqual(0f, r.center.x, 1e-3f, "방은 원점 중심");
            Assert.AreEqual(0f, r.center.y, 1e-3f);
            Assert.AreEqual(25f, r.width, 1e-3f);
            Assert.AreEqual(15f, r.height, 1e-3f);
        }
    }
}
