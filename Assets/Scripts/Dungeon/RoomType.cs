namespace Help.Dungeon
{
    public enum RoomType
    {
        Combat,
        EnvironmentPuzzle,
        CombatPuzzle,
        PurePuzzle,
        Treasure,
        Shop,
        Boss,
        Secret,
        Tutorial    // 시작 방 전용: 전투 없는 안전한 첫 방(튜토리얼). 새 값은 끝에 추가(int 직렬화 방어).
    }
}
