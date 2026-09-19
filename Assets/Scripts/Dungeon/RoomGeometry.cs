namespace Help.Dungeon
{
    // 방 좌표계의 단일 진실. 순수 로직이라 EditMode에서 그대로 검증된다.
    //
    // 방은 원점 중심으로 그려진다(RoomManager.ToCell). 그래서 방이 높아지면
    // 바닥선도 같이 내려간다 — 콘텐츠 좌표를 하드코딩하면 크기 등급이 바뀌는 순간
    // 전부 공중에 뜬다(2026-09 첫 방 튜토리얼 회귀). 콘텐츠 프리팹은 이 원점을
    // 기준으로 저작하고, 배치는 RoomManager가 이 값으로 한다.
    public static class RoomGeometry
    {
        // 바닥 칸의 윗면(=플레이어가 딛는 높이).
        // 바닥 줄은 레이아웃 y==0 → 셀 y = -(height/2), 셀 중심은 +0.5, 윗면은 다시 +0.5.
        public static float ContentOriginY(int roomHeight) => -(roomHeight / 2) + 1f;

        // 능력 장애물(벽/문)을 세우는 로컬 높이. 콘텐츠 원점이 바닥 윗면이므로
        // **콜라이더 밑면이 이 값만큼 아래로 내려와야** 바닥에 선다.
        // 1×1 콜라이더를 그냥 여기 놓으면 0.5 떠서, 위 발판과의 틈에 플레이어가 낀다(2026-09-10 사고).
        public const float ObstacleLocalY = 1f;

        // 바닥에 뿌리는 루팅 글자의 가로 오프셋(방 중앙 기준, 좌우 대칭).
        //
        // 나머지 연산(i % 5)으로 자리를 정하면 개수가 그 주기를 넘는 순간 글자가
        // 정확히 같은 자리에 겹쳐 쌓인다 — 실제로 그렇게 났던 버그다.
        // 개수를 알고 균등 분배하면 몇 개가 오든 겹치지 않는다.
        public static float SpreadX(int index, int count, float spacing) =>
            count <= 1 ? 0f : (index - (count - 1) * 0.5f) * spacing;
    }
}
