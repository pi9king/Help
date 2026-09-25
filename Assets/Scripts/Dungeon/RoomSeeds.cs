namespace Help.Dungeon
{
    // 방 하나의 결정적 시드 — 템플릿·콘텐츠·적 글자 배분을 고를 때 쓴다(순수 로직).
    //
    // 2026-09-20 결정: **런마다 바뀌고, 런 안에서는 고정.**
    //   - 런 시드(DungeonMap.Seed)와 층 번호가 들어가므로 새 런이면 같은 좌표라도 다른 방이 나온다.
    //   - 같은 런·같은 좌표면 항상 같은 값이라, 나갔다 돌아와도 방이 바뀌지 않는다.
    // 예전엔 좌표만 썼다 — 시작 방(0,0)은 매 런 시드 0이었다.
    public static class RoomSeeds
    {
        public static int For(int runSeed, int floor, int x, int y)
        {
            unchecked
            {
                int h = (x * 73856093) ^ (y * 19349663);
                h ^= runSeed * 83492791;
                h ^= floor * 50331653;
                return h;
            }
        }
    }
}
