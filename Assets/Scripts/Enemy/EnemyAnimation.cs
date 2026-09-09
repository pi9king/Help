namespace Help.Enemy
{
    // 적이 재생해야 할 애니메이션 클립.
    // 이름은 LayerLab 컨트롤러(Ant.controller 등)의 상태명과 1:1로 맞춰져 있다.
    public enum EnemyAnimClip { Idle, Walk, Attack, Hit, Dead }

    // 상태 → 클립 선택(순수). Animator는 표시 전용이고, 타이밍의 진실은
    // EnemyAI / EnemyMeleeAttack이 쥔다 — 여기서는 "그래서 무엇을 그릴까"만 정한다.
    public static class EnemyAnimation
    {
        // 우선순위: 죽음 > 피격 > 공격(예비동작) > 이동 > 정지
        public static EnemyAnimClip Select(bool isAlive, bool inHitstun, MeleePhase phase, float moveDir)
        {
            if (!isAlive) return EnemyAnimClip.Dead;
            if (inHitstun) return EnemyAnimClip.Hit;
            if (phase == MeleePhase.Windup) return EnemyAnimClip.Attack;
            return moveDir != 0f ? EnemyAnimClip.Walk : EnemyAnimClip.Idle;
        }

        public static string NameOf(EnemyAnimClip clip)
        {
            switch (clip)
            {
                case EnemyAnimClip.Walk:   return "Walk";
                case EnemyAnimClip.Attack: return "Attack";
                case EnemyAnimClip.Hit:    return "Hit";
                case EnemyAnimClip.Dead:   return "Dead";
                default:                   return "Idle";
            }
        }
    }
}
