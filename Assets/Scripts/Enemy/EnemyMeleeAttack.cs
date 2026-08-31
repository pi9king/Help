namespace Help.Enemy
{
    public enum MeleePhase { Ready, Windup, Recover }

    public struct MeleeAttackResult
    {
        public MeleePhase Phase;
        public bool StartedWindup; // 이번 틱에 예비동작이 시작됨(텔레그래프 트리거)
        public bool Strike;        // 이번 틱에 실제 타격 판정이 발생함
    }

    // 근접 공격 타이밍 상태머신(순수 로직 — UnityEngine 비의존, EditMode 테스트 가능).
    // Ready →(공격 의도) Windup(예비동작, 회피 가능 창) → Strike → Recover(쿨다운) → Ready.
    // 예비동작을 둬서 "즉발 데미지"가 아니라 플레이어가 보고 피할 수 있게 만든다.
    public class EnemyMeleeAttack
    {
        private readonly float _windup;
        private readonly float _recover;
        private MeleePhase _phase;
        private float _timer;

        public MeleePhase Phase => _phase;

        public EnemyMeleeAttack(float windup, float recover)
        {
            _windup = windup;
            _recover = recover;
            _phase = MeleePhase.Ready;
        }

        public void Reset()
        {
            _phase = MeleePhase.Ready;
            _timer = 0f;
        }

        // dt만큼 진행. wantsAttack은 AI가 이번 틱에 공격을 원하는지(사거리 내 등).
        public MeleeAttackResult Advance(float dt, bool wantsAttack)
        {
            var r = new MeleeAttackResult();

            switch (_phase)
            {
                case MeleePhase.Ready:
                    if (wantsAttack)
                    {
                        _phase = MeleePhase.Windup;
                        _timer = _windup;
                        r.StartedWindup = true;
                    }
                    break;

                case MeleePhase.Windup:
                    _timer -= dt;
                    if (_timer <= 0f)
                    {
                        r.Strike = true;
                        _phase = MeleePhase.Recover;
                        _timer = _recover;
                    }
                    break;

                case MeleePhase.Recover:
                    _timer -= dt;
                    if (_timer <= 0f) _phase = MeleePhase.Ready;
                    break;
            }

            r.Phase = _phase;
            return r;
        }
    }
}
