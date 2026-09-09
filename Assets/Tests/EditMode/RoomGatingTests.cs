using NUnit.Framework;
using Help.Dungeon;

namespace Tests.EditMode
{
    // 불변식: 능력 장애물로 출구를 잠그려면 플레이어가 그 능력을 확보할 길이 보장돼야 한다.
    //
    // 실제 사고 두 건(2026-09-08 Play):
    //  1) PurePuzzle 방에 진입 조건이 없는데 콘텐츠는 얼음벽·부서지는 벽을 넣어 도구 없이 갇혔다.
    //  2) 그걸 막으려고 "조건 없으면 게이트 안 함"으로 고쳤더니 **튜토리얼이 깨졌다** —
    //     문이 목표에서 빠져 목표 0개가 됐는데, 출구 잠금 재평가는 목표 수를 안 보고 잠갔다.
    public class RoomGatingTests
    {
        [Test]
        public void CapabilityObstacleShouldGateWhenRoomIsConditional()
        {
            Assert.IsTrue(RoomGating.CapabilityGatesExit(roomHasEntryConditions: true, selfContained: false));
        }

        [Test]
        public void CapabilityObstacleShouldNotGateFreeEntryRoom()
        {
            Assert.IsFalse(RoomGating.CapabilityGatesExit(roomHasEntryConditions: false, selfContained: false),
                "조건 없는 방에서 능력 장애물이 출구를 잠그면 도구 없이 갇힌다");
        }

        // KEY 튜토리얼: 진입 조건은 없지만 방 안에 K·Y가 있어 스스로 풀 수 있다.
        [Test]
        public void SelfContainedRoomShouldGateWithoutEntryCondition()
        {
            Assert.IsTrue(RoomGating.CapabilityGatesExit(roomHasEntryConditions: false, selfContained: true),
                "방 안에서 해결 수단을 주는 방(튜토리얼)은 조건 없이도 잠글 수 있어야 한다");
        }

        [Test]
        public void EnemyClearShouldGateRegardlessOfConditions()
        {
            Assert.IsTrue(RoomGating.EnemyClearGatesExit(roomHasEntryConditions: false));
            Assert.IsTrue(RoomGating.EnemyClearGatesExit(roomHasEntryConditions: true));
        }

        // ★ 핵심 회귀: 목표가 0개면 절대 잠그지 않는다.
        // SolveTracker는 목표 0개일 때 IsSolved를 false로 두므로(빈 방 오발화 방지),
        // 잠그는 순간 아무도 풀 수 없는 방이 된다.
        [Test]
        public void ShouldNeverLockWithoutObjectives()
        {
            Assert.IsFalse(RoomGating.ShouldLockExit(objectiveCount: 0, solved: false),
                "목표 0개인데 잠그면 영원히 못 나간다");
        }

        [Test]
        public void ShouldLockWhileObjectivesRemain()
        {
            Assert.IsTrue(RoomGating.ShouldLockExit(objectiveCount: 1, solved: false));
        }

        [Test]
        public void ShouldUnlockOnceSolved()
        {
            Assert.IsFalse(RoomGating.ShouldLockExit(objectiveCount: 1, solved: true));
        }
    }
}
