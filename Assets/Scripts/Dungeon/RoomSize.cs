namespace Help.Dungeon
{
    // 방의 크기 등급. 새 값은 반드시 끝에 추가한다(int 직렬화 방어 — RoomType.cs 선례).
    public enum RoomSizeClass
    {
        Small,  // 한 화면에 다 들어온다 — 카메라 고정
        Wide,   // 가로로 두 배 — 카메라가 좌우로 따라간다
        Tall    // 세로로 두 배 — 카메라가 위아래로 따라간다
    }

    public readonly struct RoomDim
    {
        public int Width { get; }
        public int Height { get; }
        public RoomDim(int width, int height) { Width = width; Height = height; }
    }

    // 크기 등급 → 타일 수. 순수 로직이라 EditMode에서 그대로 검증된다.
    public static class RoomDimensions
    {
        // 카메라 직교 크기. Small 방 높이(15타일)와 정확히 맞물린다.
        public const float CameraOrthoSize = 7.5f;

        // 폭·높이는 모두 홀수 — 방을 원점 중심으로 그리므로(RoomManager.RenderRoom)
        // 짝수면 중심이 반 타일 어긋나 문·스폰 좌표가 비대칭이 된다.
        public static RoomDim Of(RoomSizeClass sizeClass) => sizeClass switch
        {
            RoomSizeClass.Wide => new RoomDim(51, 15),
            RoomSizeClass.Tall => new RoomDim(25, 31),
            _ => new RoomDim(25, 15),   // Small = 기본값 겸 폴백
        };
    }
}
