using NUnit.Framework;
using UnityEngine;
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

        // --- 템플릿 마커 스폰 (옵트인) ---

        [Test]
        public void TemplateMarkerSpawn_IsOffByDefault()
        {
            // 기본이 켜져 있으면 기존에 검증된 손배치 콘텐츠 경로가 조용히 바뀐다.
            var lib = ScriptableObject.CreateInstance<RoomContentLibrary>();
            try
            {
                lib.AddEntry(RoomType.Combat, null);
                Assert.IsFalse(lib.UsesTemplateMarkers(RoomType.Combat));
                Assert.IsFalse(lib.UsesTemplateMarkers(RoomType.Boss), "등록조차 없는 유형도 꺼져 있어야 한다");
            }
            finally { Object.DestroyImmediate(lib); }
        }

        [Test]
        public void TemplateMarkerSpawn_CanBeEnabledPerRoomType()
        {
            var lib = ScriptableObject.CreateInstance<RoomContentLibrary>();
            try
            {
                lib.SetUsesTemplateMarkers(RoomType.Combat, true);
                Assert.IsTrue(lib.UsesTemplateMarkers(RoomType.Combat));
                Assert.IsFalse(lib.UsesTemplateMarkers(RoomType.PurePuzzle), "유형별로 하나씩 옮길 수 있어야 한다");
            }
            finally { Object.DestroyImmediate(lib); }
        }

        [Test]
        public void PickForMarker_ReturnsNullForUnregisteredSymbol()
        {
            var lib = ScriptableObject.CreateInstance<RoomContentLibrary>();
            try
            {
                // 'p'(플레이어 시작)처럼 스폰 대상이 없는 마커는 조용히 건너뛰어야 한다
                Assert.IsNull(lib.PickForMarker('p', 0));
            }
            finally { Object.DestroyImmediate(lib); }
        }

        [Test]
        public void PickForMarker_IsDeterministicForSameSeed()
        {
            var lib = ScriptableObject.CreateInstance<RoomContentLibrary>();
            var a = new GameObject("a");
            var b = new GameObject("b");
            try
            {
                lib.AddMarkerPrefab('e', a);
                lib.AddMarkerPrefab('e', b);
                Assert.AreSame(lib.PickForMarker('e', 77), lib.PickForMarker('e', 77));
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(lib);
            }
        }
    }
}
