namespace Help.Enemy
{
    // 적이 지각하는 상황(순수 값 — UnityEngine 비의존으로 EditMode 테스트 가능)
    public struct EnemyPerception
    {
        public bool HasTarget;         // 추적할 플레이어가 존재하는가
        public float SelfX;            // 적의 x
        public float TargetX;          // 플레이어의 x
        public float DistanceToTarget; // 적-플레이어 거리(2D)
        public bool IsRestrained;      // 속박 중(EnemyStatus) — 이동/공격 봉인
    }

    // AI 파라미터
    public struct EnemyAIConfig
    {
        public float AggroRange;      // 이 거리 안이면 추격 시작
        public float DeAggroRange;    // 이 거리 밖이면 추격 포기(>= AggroRange, 이력현상)
        public float AttackRange;     // 이 거리 안이면 공격
        public float PatrolAnchorX;   // 정찰 중심(보통 스폰 위치)
        public float PatrolHalfWidth; // 정찰 반경(0이면 제자리 Idle)
        public EnemyArchetype Archetype; // 행동 유형 (기본 Melee)
        public float StandoffRange;   // Ranged 전용: 이 거리보다 가까우면 후퇴(카이팅). 보통 < AttackRange
    }

    // AI가 이번 틱에 원하는 행동
    public struct EnemyDecision
    {
        public EnemyState State;
        public float MoveDir; // 수평 이동 의도: -1 / 0 / +1
        public bool Attack;   // 이번 틱에 공격을 발동할지
    }

    // 순수 로직 상태머신: 지각+설정으로 다음 상태와 이동/공격 의도를 계산한다.
    // Idle/Patrol → (Aggro 감지) Chase → (AttackRange) Attack.
    // 이력현상(hysteresis): 추격 이탈은 DeAggroRange, 공격 이탈은 AttackRange 초과에서만 →
    // 경계선에서 상태가 깜빡이지 않는다. MonoBehaviour(EnemyBase)가 결과를 물리/공격으로 적용한다.
    public class EnemyAI
    {
        public EnemyState State { get; private set; } = EnemyState.Patrol;
        private float _patrolDir = 1f;

        // 런 리셋 시 상태 초기화(정찰로 복귀)
        public void Reset()
        {
            State = EnemyState.Patrol;
            _patrolDir = 1f;
        }

        public EnemyDecision Tick(EnemyPerception p, EnemyAIConfig cfg, bool attackReady)
        {
            // 속박 → 행동 봉인. 타깃 유무보다 우선(정찰도 막는다).
            // State는 일부러 갈아엎지 않는다 — 해제 즉시 이전 상태(Chase 등)로 복귀시키기 위함.
            if (p.IsRestrained)
                return new EnemyDecision { State = State, MoveDir = 0f, Attack = false };

            // 타깃 없음 → 정찰
            if (!p.HasTarget)
            {
                State = cfg.PatrolHalfWidth > 0f ? EnemyState.Patrol : EnemyState.Idle;
                return PatrolDecision(p, cfg);
            }

            // 상태 전이 (진입=Aggro/Attack, 이탈=DeAggro/AttackRange 초과)
            switch (State)
            {
                case EnemyState.Attack:
                    if (p.DistanceToTarget > cfg.AttackRange) State = EnemyState.Chase;
                    break;
                case EnemyState.Chase:
                    if (p.DistanceToTarget <= cfg.AttackRange) State = EnemyState.Attack;
                    else if (p.DistanceToTarget > cfg.DeAggroRange) State = EnemyState.Patrol;
                    break;
                default: // Idle / Patrol
                    if (p.DistanceToTarget <= cfg.AggroRange) State = EnemyState.Chase;
                    break;
            }

            // 상태별 행동
            switch (State)
            {
                case EnemyState.Attack:
                    return AttackDecision(p, cfg, attackReady);
                case EnemyState.Chase:
                    return new EnemyDecision { State = State, MoveDir = ChaseMove(p), Attack = false };
                default:
                    return PatrolDecision(p, cfg);
            }
        }

        // 공격 상태의 이동 의도.
        // Melee: 멈춰서 공격. Ranged: 스탠드오프보다 가까우면 후퇴하며 공격(카이팅).
        private static EnemyDecision AttackDecision(EnemyPerception p, EnemyAIConfig cfg, bool attackReady)
        {
            float move = 0f;
            if (cfg.Archetype == EnemyArchetype.Ranged && p.DistanceToTarget < cfg.StandoffRange)
                move = -ChaseMove(p); // 타깃 반대 방향으로 후퇴
            return new EnemyDecision { State = EnemyState.Attack, MoveDir = move, Attack = attackReady };
        }

        // 수평 추격 방향. x가 거의 같으면(예: 플레이어가 머리 위) 정지 → 좌우 진동 방지.
        private const float AlignDeadzone = 0.15f;

        private static float ChaseMove(EnemyPerception p)
        {
            float dx = p.TargetX - p.SelfX;
            if (dx > AlignDeadzone) return 1f;
            if (dx < -AlignDeadzone) return -1f;
            return 0f;
        }

        private EnemyDecision PatrolDecision(EnemyPerception p, EnemyAIConfig cfg)
        {
            if (cfg.PatrolHalfWidth <= 0f)
                return new EnemyDecision { State = EnemyState.Idle, MoveDir = 0f, Attack = false };

            float min = cfg.PatrolAnchorX - cfg.PatrolHalfWidth;
            float max = cfg.PatrolAnchorX + cfg.PatrolHalfWidth;
            if (p.SelfX <= min) _patrolDir = 1f;
            else if (p.SelfX >= max) _patrolDir = -1f;

            return new EnemyDecision { State = EnemyState.Patrol, MoveDir = _patrolDir, Attack = false };
        }
    }
}
