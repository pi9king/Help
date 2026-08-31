namespace Help.Enemy
{
    // 적 AI 상태. enum + switch 상태관리(CLAUDE.md 아키텍처 원칙).
    public enum EnemyState
    {
        Idle,    // 타깃 없음, 정지
        Patrol,  // 타깃 없음, 정찰 구간 왕복
        Chase,   // 타깃 추격
        Attack,  // 사거리 내, 쿨다운마다 공격
    }
}
