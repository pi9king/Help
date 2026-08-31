namespace Help.Combat
{
    public enum AttackPhase { Windup, Active, Recovery, Done }

    public struct AttackTick
    {
        public AttackPhase Phase;
        public bool IsActive;        // 타격 판정이 열려 있는 구간인가
        public float ActiveProgress; // Active 구간 진행도 0~1 (연출 스윕용). Active 밖에선 0 또는 1로 클램프
    }

    // 공격 한 번의 타이밍(순수 로직 — UnityEngine 비의존, EditMode 테스트 가능).
    // Windup(예비) → Active(타격 창) → Recovery(후딜) → Done.
    // 근접/원거리/마법 모두 이 타이밍 엔진을 공유하고, "무엇을 내보내는가"(Kind)만 드라이버에서 달라진다.
    public class AttackMotionClock
    {
        private readonly float _windup;
        private readonly float _active;
        private readonly float _recovery;
        private float _t;
        private bool _running;

        public AttackMotionClock(float windup, float active, float recovery)
        {
            _windup = windup;
            _active = active;
            _recovery = recovery;
        }

        public float Total => _windup + _active + _recovery;
        public bool Running => _running;

        public void Start()
        {
            _t = 0f;
            _running = true;
        }

        public AttackTick Tick(float dt)
        {
            if (!_running)
                return new AttackTick { Phase = AttackPhase.Done, IsActive = false, ActiveProgress = 1f };

            _t += dt;

            var r = new AttackTick();
            if (_t < _windup)
            {
                r.Phase = AttackPhase.Windup;
                r.ActiveProgress = 0f;
            }
            else if (_t < _windup + _active)
            {
                r.Phase = AttackPhase.Active;
                r.IsActive = true;
                r.ActiveProgress = _active > 0f ? (_t - _windup) / _active : 1f;
            }
            else if (_t < Total)
            {
                r.Phase = AttackPhase.Recovery;
                r.ActiveProgress = 1f;
            }
            else
            {
                r.Phase = AttackPhase.Done;
                r.ActiveProgress = 1f;
                _running = false;
            }
            return r;
        }
    }
}
