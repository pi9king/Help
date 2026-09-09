namespace Help.Dungeon
{
    // 방의 목표가 출구를 잠글 자격이 있는가 (순수).
    //
    // 불변식: **능력 장애물로 출구를 잠그려면, 플레이어가 그 능력을 확보할 길이 보장돼야 한다.**
    // 길은 둘 중 하나다:
    //   1) 진입 조건이 있는 방 — 레이어1이 "만들 수 있는가"를 이미 판정했다
    //   2) 스스로 해결 수단을 제공하는 방 — 예: KEY 튜토리얼(방 안에 K·Y가 있다)
    //
    // 둘 다 아니면 잠그면 안 된다. 도구 없이 들어가 갇힌다(2026-09-08 Play에서 실제 발생).
    // 적 전멸은 능력이 필요 없다(아무 무기로나 죽는다) — 이 제약을 받지 않는다.
    public static class RoomGating
    {
        public static bool CapabilityGatesExit(bool roomHasEntryConditions, bool selfContained) =>
            roomHasEntryConditions || selfContained;

        public static bool EnemyClearGatesExit(bool roomHasEntryConditions) => true;

        // 출구를 실제로 잠글지. **목표가 하나도 없으면 잠그지 않는다** —
        // 목표 0개짜리 퍼즐은 영원히 해결되지 않아(SolveTracker가 IsSolved=false로 둔다)
        // 방이 통째로 갇힌다. 튜토리얼이 이 함정에 걸렸다.
        public static bool ShouldLockExit(int objectiveCount, bool solved) =>
            objectiveCount > 0 && !solved;
    }
}
