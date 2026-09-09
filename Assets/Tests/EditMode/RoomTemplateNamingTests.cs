using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    // 템플릿 파일 규약: Assets/Rooms/Floor{N}/{RoomType}_{SizeClass}_{NN}[_draft].txt
    //   층    = 폴더명 (D-13)
    //   작업중 = 파일명 _draft 접미사 (D-17)
    //
    // 이 판정은 **에디터 셋업과 검증 테스트 양쪽이 같은 규칙을 써야** 한다.
    // 한쪽만 draft를 걸러내면 "테스트는 통과하는데 라이브러리엔 들어간" 방이 생긴다.
    public class RoomTemplateNamingTests
    {
        [Test]
        public void ShouldDetectDraftSuffix()
        {
            Assert.IsTrue(RoomTemplateNaming.IsDraft("Combat_Small_04_draft"));
        }

        [Test]
        public void ShouldNotFlagFinishedTemplate()
        {
            Assert.IsFalse(RoomTemplateNaming.IsDraft("Combat_Small_01"));
        }

        // "draft"가 이름 가운데 들어간 방(예: Draftsman)을 오탐하면 안 된다 — 접미사만 본다.
        [Test]
        public void ShouldOnlyMatchSuffixNotSubstring()
        {
            Assert.IsFalse(RoomTemplateNaming.IsDraft("Combat_draft_Small_01"));
        }

        [Test]
        public void DraftDetectionShouldIgnoreCase()
        {
            Assert.IsTrue(RoomTemplateNaming.IsDraft("Combat_Small_04_Draft"));
        }

        [Test]
        public void ShouldReadFloorFromFolder()
        {
            Assert.AreEqual(2, RoomTemplateNaming.FloorOf("Assets/Rooms/Floor2/Combat_Small_01.txt"));
            Assert.AreEqual(3, RoomTemplateNaming.FloorOf("Assets/Rooms/Floor3/Boss_Wide_01.txt"));
        }

        // 층 폴더 없이 Assets/Rooms 바로 아래 있는 파일은 1층으로 본다
        // (규약 도입 전 파일이 남아 있어도 게임이 멈추지 않게).
        [Test]
        public void ShouldDefaultToFirstFloorWithoutFolder()
        {
            Assert.AreEqual(1, RoomTemplateNaming.FloorOf("Assets/Rooms/Combat_Small_01.txt"));
        }

        [Test]
        public void ShouldIgnoreUnparsableFolderName()
        {
            Assert.AreEqual(1, RoomTemplateNaming.FloorOf("Assets/Rooms/Scratch/Combat_Small_01.txt"));
        }

        // 윈도우 경로(역슬래시)로 들어와도 같은 결과여야 한다 — AssetDatabase와 Directory API가 다르다.
        [Test]
        public void ShouldAcceptBackslashPaths()
        {
            Assert.AreEqual(2, RoomTemplateNaming.FloorOf(@"Assets\Rooms\Floor2\Combat_Small_01.txt"));
        }
    }
}
