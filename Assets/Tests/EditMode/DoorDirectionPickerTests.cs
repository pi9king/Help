using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class DoorDirectionPickerTests
    {
        [Test] public void PicksEastWhenRightIsDominant() => Assert.AreEqual(Direction.East, DoorDirectionPicker.Nearest(5f, 1f));
        [Test] public void PicksWestWhenLeftIsDominant() => Assert.AreEqual(Direction.West, DoorDirectionPicker.Nearest(-5f, 1f));
        [Test] public void PicksNorthWhenUpIsDominant() => Assert.AreEqual(Direction.North, DoorDirectionPicker.Nearest(1f, 5f));
        [Test] public void PicksSouthWhenDownIsDominant() => Assert.AreEqual(Direction.South, DoorDirectionPicker.Nearest(1f, -5f));

        [Test]
        public void NearestAmong_PicksMostTowardCandidate()
        {
            // 플레이어가 우상단(5,1)인데 후보가 West/North뿐이면 North(위쪽)를 고름
            Assert.AreEqual(Direction.North, DoorDirectionPicker.NearestAmong(5f, 1f, new[] { Direction.West, Direction.North }));
            // East 후보가 있으면 East
            Assert.AreEqual(Direction.East, DoorDirectionPicker.NearestAmong(5f, 1f, new[] { Direction.East, Direction.West }));
        }

        [Test]
        public void NearestAmong_ReturnsNullWhenNoCandidates()
        {
            Assert.IsNull(DoorDirectionPicker.NearestAmong(5f, 1f, new Direction[0]));
        }

        [Test]
        public void Opposite_IsMutualAcrossAxes()
        {
            Assert.AreEqual(Direction.West, DoorDirectionPicker.Opposite(Direction.East));
            Assert.AreEqual(Direction.East, DoorDirectionPicker.Opposite(Direction.West));
            Assert.AreEqual(Direction.South, DoorDirectionPicker.Opposite(Direction.North));
            Assert.AreEqual(Direction.North, DoorDirectionPicker.Opposite(Direction.South));
        }

        // ── 문을 "장소"로 취급하는 선택 ──
        // 예전 NearestAmong은 방향 점수만 봤다(오른쪽에 서 있으면 무조건 동문).
        // 그래서 방 어디서 E를 눌러도 문이 열려, 문 앞까지 걸어갈 이유가 없었다.
        // 지형과 문을 엮으려면 "문 근처에 있는가"가 조건이어야 한다.

        private static Dictionary<Direction, Vector2> Doors(params (Direction d, float x, float y)[] items)
        {
            var map = new Dictionary<Direction, Vector2>();
            foreach (var (d, x, y) in items) map[d] = new Vector2(x, y);
            return map;
        }

        [Test]
        public void ShouldReturnNullWhenNoDoorIsNear()
        {
            var doors = Doors((Direction.East, 10f, 0f));
            Assert.IsNull(DoorDirectionPicker.NearestWithin(0f, 0f, doors, 2.5f));
        }

        [Test]
        public void ShouldPickDoorInsideRadius()
        {
            var doors = Doors((Direction.East, 2f, 0f));
            Assert.AreEqual(Direction.East, DoorDirectionPicker.NearestWithin(0f, 0f, doors, 2.5f));
        }

        [Test]
        public void ShouldPickTheNearerOfTwoDoorsInRange()
        {
            var doors = Doors((Direction.East, 2f, 0f), (Direction.North, 0f, 1f));
            Assert.AreEqual(Direction.North, DoorDirectionPicker.NearestWithin(0f, 0f, doors, 2.5f));
        }

        // 천장 문은 퇴장 지점이 문에서 2칸 아래다(문 칸이 벽이라 머리가 걸림).
        // 반경이 그보다 좁으면 사다리를 다 올라가고도 못 나간다.
        [Test]
        public void ShouldReachCeilingDoorFromTwoTilesBelow()
        {
            var doors = Doors((Direction.North, 0f, 2f));
            Assert.AreEqual(Direction.North, DoorDirectionPicker.NearestWithin(0f, 0f, doors, 2.5f));
        }

        [Test]
        public void ShouldIncludeDoorExactlyAtRadius()
        {
            var doors = Doors((Direction.South, 0f, -2.5f));
            Assert.AreEqual(Direction.South, DoorDirectionPicker.NearestWithin(0f, 0f, doors, 2.5f));
        }

        [Test]
        public void ShouldReturnNullWhenThereAreNoDoors()
        {
            Assert.IsNull(DoorDirectionPicker.NearestWithin(0f, 0f, Doors(), 2.5f));
        }
    }
}
