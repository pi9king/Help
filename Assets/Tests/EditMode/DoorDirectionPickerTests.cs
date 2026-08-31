using NUnit.Framework;
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
    }
}
