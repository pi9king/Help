using UnityEngine;

namespace Help.Enemy
{
    // EnemyBase(판정) → Animator(표시) 사이의 얇은 어댑터.
    //
    // 원칙: **Animator는 게임플레이를 좌우하지 않는다.** 공격 타이밍의 진실은 EnemyMeleeAttack,
    // 이동 결정의 진실은 EnemyAI가 쥐고, 여기서는 그 결과를 그리기만 한다.
    // 애니메이션 이벤트로 데미지를 주는 식으로 뒤집으면 EditMode 테스트가 닿지 않는 곳에
    // 판정이 숨어버린다(순수 로직 분리 원칙).
    //
    // Animator가 없는 프리팹(기존 placeholder 사각형 적)에서는 조용히 아무것도 안 한다.
    public class EnemyAnimatorDriver : MonoBehaviour
    {
        private Animator _animator;
        private EnemyAnimClip _current;
        private bool _played;

        private void Awake() => _animator = GetComponentInChildren<Animator>();

        public void Apply(bool isAlive, bool inHitstun, MeleePhase phase, float moveDir)
        {
            if (_animator == null) return;

            var clip = EnemyAnimation.Select(isAlive, inHitstun, phase, moveDir);
            if (_played && clip == _current) return; // 같은 클립을 매 틱 재시작하지 않도록

            _current = clip;
            _played = true;
            _animator.Play(EnemyAnimation.NameOf(clip), 0, 0f);
        }

        // 런 리셋으로 되살아난 적이 Dead 포즈로 굳어 있지 않게 한다.
        public void ResetToIdle()
        {
            _played = false;
            Apply(true, false, MeleePhase.Ready, 0f);
        }
    }
}
