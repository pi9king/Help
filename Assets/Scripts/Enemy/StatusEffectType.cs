namespace Help.Enemy
{
    // 적에게 걸리는 상태이상 종류. 확장은 값 추가 + EnemyStatus의 switch 분기(enum + switch 컨벤션).
    public enum StatusEffectType
    {
        None,   // 효과 없음
        Bind,   // 속박 — 이동/공격 불가 (로프 등)
    }
}
