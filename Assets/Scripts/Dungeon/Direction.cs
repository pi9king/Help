namespace Help.Dungeon
{
    // 방 간 이동 방향 (사이드뷰: 좌/우 = 옆방, 위 = 윗방, 아래 = 아랫방)
    public enum Direction { North, South, East, West }

    // 방 진입 시도 결과
    public enum RoomEntryResult
    {
        NoDoor,   // 그 방향에 연결된 방이 없음
        Blocked,  // 방은 있으나 진입 조건 불충족
        Entered   // 진입 성공
    }
}
