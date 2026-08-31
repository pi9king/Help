using System;

namespace Help.Enemy
{
    // 적의 상태이상 지속시간을 관리하는 순수 로직(EnemyStats의 형제 — UnityEngine 비의존으로 EditMode 테스트 가능).
    // 시간은 Tick(dt)로 주입받는다. MonoBehaviour(EnemyBase)가 매 FixedUpdate에 틱하고,
    // 결과 플래그는 EnemyPerception을 통해 EnemyAI의 판단 재료가 된다.
    // 확장: Slow/Stun 등은 여기에 타이머 + Apply의 switch 분기를 더한다.
    public class EnemyStatus
    {
        private float _bindRemaining;
        private float _pullRemaining;

        // 속박 중 — 이동/공격이 봉인된다(결정 레이어)
        public bool IsRestrained => _bindRemaining > 0f;
        // 끌림 중 — 외력이 AI 의도를 덮어쓴다(물리 레이어). 속박과 독립적으로 만료된다.
        public bool IsPulled => _pullRemaining > 0f;

        public void Apply(StatusEffectType type, float duration)
        {
            switch (type)
            {
                case StatusEffectType.Bind:
                    // 더 긴 효과만 덮어쓴다 — 약한 재적용이 남은 시간을 깎지 않도록
                    if (duration > _bindRemaining) _bindRemaining = duration;
                    break;
            }
        }

        public void ApplyPull(float duration)
        {
            if (duration > _pullRemaining) _pullRemaining = duration;
        }

        public void Tick(float deltaTime)
        {
            if (_bindRemaining > 0f) _bindRemaining = Math.Max(0f, _bindRemaining - deltaTime);
            if (_pullRemaining > 0f) _pullRemaining = Math.Max(0f, _pullRemaining - deltaTime);
        }

        // 런 리셋 시 전부 해제
        public void Reset()
        {
            _bindRemaining = 0f;
            _pullRemaining = 0f;
        }
    }
}
