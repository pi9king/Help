namespace Help.Dungeon
{
    // 방 좌표계의 단일 진실. 순수 로직이라 EditMode에서 그대로 검증된다.
    //
    // 방과 콘텐츠는 모두 방 중앙을 원점으로 사용하는 평면 좌표계로 저작한다.
    public static class RoomGeometry
    {
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
