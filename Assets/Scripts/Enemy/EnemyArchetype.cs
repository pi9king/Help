namespace Help.Enemy
{
    // 적 행동 유형. enum + switch 분기(CLAUDE.md 아키텍처 원칙).
    public enum EnemyArchetype
    {
        Melee,   // 근접: 사거리까지 추격 후 멈춰 공격 (기본값)
        Ranged,  // 원거리: 스탠드오프 거리 유지 — 너무 가까우면 후퇴하며 원거리 공격(카이팅)
    }
}
