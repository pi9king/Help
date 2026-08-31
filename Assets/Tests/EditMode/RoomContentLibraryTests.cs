using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    public class RoomContentLibraryTests
    {
        [Test]
        public void SelectIndex_DeterministicAndInRange()
        {
            Assert.AreEqual(RoomContentLibrary.SelectIndex(5, 3), RoomContentLibrary.SelectIndex(5, 3), "같은 seed=같은 index");
            for (int s = -200; s < 200; s++)
            {
                int i = RoomContentLibrary.SelectIndex(s, 4);
                Assert.IsTrue(i >= 0 && i < 4, $"index 범위 내 (seed={s})");
            }
        }

        [Test]
        public void SelectIndex_SingleCandidate_AlwaysZero()
        {
            Assert.AreEqual(0, RoomContentLibrary.SelectIndex(12345, 1));
            Assert.AreEqual(0, RoomContentLibrary.SelectIndex(-7, 1));
        }

        [Test]
        public void SelectIndex_ZeroCount_Safe()
        {
            Assert.AreEqual(0, RoomContentLibrary.SelectIndex(3, 0));
        }
    }
}
